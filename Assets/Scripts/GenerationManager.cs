/*
 * Copyright 2021 Snappable Meshes PCG contributors
 * (https://github.com/VideojogosLusofona/snappable-meshes-pcg)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using NaughtyAttributes;
using Array2DEditor;
using SnapMeshPCG.SelectionMethods;

// Avoid conflict with System.Diagnostics.Debug
using Debug = UnityEngine.Debug;

namespace SnapMeshPCG
{
    /// <summary>
    /// Manages the map generation process and keeps track of all the
    /// generation parameters.
    /// </summary>
    public class GenerationManager : MonoBehaviour
    {
        // ///////// //
        // Constants //
        // ///////// //

        private const string contentParams = ":: Content parameters ::";
        private const string generalParams = ":: General parameters ::";
        private const string connectionParams = ":: Connection parameters ::";
        private const string selectionParams = ":: Selection parameters ::";
        private const string events = ":: Events ::";

        // ////////////////// //
        // Content parameters //
        // ////////////////// //

        [BoxGroup(contentParams)]
        [ReorderableList]
        [SerializeField]
        private List<MapPiece> _piecesList = null;

        [BoxGroup(contentParams)]
        [Label("Use Starter Piece List")]
        [SerializeField]
        private bool _useStarter = false;

        [BoxGroup(contentParams)]
        [ReorderableList]
        [SerializeField]
        [ShowIf(nameof(_useStarter))]
        private List<MapPiece> _startingPieceList = null;

        // ////////////////// //
        // General parameters //
        // ////////////////// //

        [BoxGroup(generalParams)]
        [SerializeField]
        private bool _useSeed = false;

        [BoxGroup(generalParams)]
        [SerializeField]
        [ShowIf(nameof(_useSeed))]
        private int _seed = 0;

        [BoxGroup(generalParams)]
        [SerializeField]
        private float _pieceDistance = 0.0001f;

        [BoxGroup(generalParams)]
        [SerializeField]
        private uint _maxFailures = 10;

        [BoxGroup(generalParams)]
        [SerializeField]
        private bool _checkOverlaps;

        [BoxGroup(generalParams), ShowIf("_checkOverlaps")]
        [SerializeField]
        private LayerMask _collidersLayer;

        // ///////////////////// //
        // Connection parameters //
        // ///////////////////// //

        [BoxGroup(connectionParams)]
        [SerializeField]
        [EnumFlags]
        private SnapRules _matchingRules = SnapRules.None;

        [BoxGroup(connectionParams)]
        [SerializeField]
        private uint _pinCountTolerance = 0;

        [BoxGroup(connectionParams)]
        [SerializeField]
        [Tooltip("Rows refer to the guide piece, columns to the tentative piece")]
        private Array2DBool _colorMatrix = null;

        // //////////////////// //
        // Selection parameters //
        // //////////////////// //

        [BoxGroup(selectionParams)]
        [SerializeField]
        [Dropdown(nameof(SelMethods))]
        [OnValueChanged("OnChangeSMName")]
        private string _selectionMethod;

        [BoxGroup(selectionParams)]
        [SerializeField]
        [Expandable]
        [OnValueChanged(nameof(OnChangeSMType))]
        private AbstractSMConfig _selectionParams;

        [BoxGroup(selectionParams)]
        [Label("Starter Connector Count Tolerance")]
        [SerializeField]
        private uint _starterConTol = 0;

        // ////// //
        // Events //
        // ////// //

        [Foldout(events)]
        [SerializeField]
        private UnityEvent OnGenerationBegin = null;

        [Foldout(events)]
        [SerializeField]
        private UnityEvent<IReadOnlyList<MapPiece>> OnGenerationFinish = null;

        [Foldout(events)]
        [SerializeField]
        private UnityEvent<MapPiece> OnConnectionMade = null;

        [Foldout(events)]
        [SerializeField]
        private UnityEvent OnGenerationClear = null;

        // ///////////////////////////////////// //
        // Instance variables not used in editor //
        // ///////////////////////////////////// //

        // Pieces placed in the map
        [SerializeField]
        [HideInInspector]
        private List<MapPiece> _placedPieces;

        // Names of known selection methods
        [System.NonSerialized]
        private string[] _selMethods;

        // Step by step generation
        private int     genStep = -1;              // Current step of generation, -1 not started, 0 is initialized, rest is steps in generation
        private bool    isComplete = false;     // If generation is complete
        private bool    isStepActive = false;   // If function should return when a step is complete

        // ////////// //
        // Properties //
        // ////////// //

        // Get selection method names
        private ICollection<string> SelMethods
        {
            get
            {
                // Did we initialize gen. method names already?
                if (_selMethods is null)
                {
                    // Get gen. method names
                    _selMethods = SMManager.Instance.SelMethodNames;
                    // Sort them
                    System.Array.Sort(_selMethods);
                }

                // Return existing methods
                return _selMethods;
            }
        }

        /// <summary>
        /// Get map piece placed in the map, in the order they were placed.
        /// </summary>
        public IReadOnlyList<MapPiece> PlacedPieces => _placedPieces;

        /// <summary>
        /// How long did the last generation process took, in milliseconds?
        /// </summary>
        public int GenTimeMillis { get; private set; }

        // /////// //
        // Methods //
        // /////// //

        // Callback invoked when user changes selection method type in editor
        private void OnChangeSMType()
        {
            if (_selectionParams is null)
            {
                // Cannot allow this field to be empty, so set it back to what
                // is specified in the selection method name
                Debug.Log(
                    $"The {nameof(_selectionParams)} field cannot be empty");
                OnChangeSMName();
            }
            else
            {
                // Update selection method name accordingly to what is now set
                // in the generation configurator fields
                _selectionMethod = SMManager.Instance.GetNameFromType(
                    _selectionParams.GetType());
            }
        }

        // Callback invoked when user changes selection method name in editor
        private void OnChangeSMName()
        {
            // Make sure gen. method type is updated accordingly
            System.Type gmConfig =
                SMManager.Instance.GetTypeFromName(_selectionMethod);
            _selectionParams = AbstractSMConfig.GetInstance(gmConfig);
        }

        struct CreateMapData
        {
            // Stater piece prototype
            public MapPiece         starterPiecePrototype;
            // Starter piece game object
            public GameObject       starterPieceGObj;
            // Current guide piece
            public MapPiece         guidePiece;
            // Tentative piece prototype
            public MapPiece         tentPiecePrototype;
            // Get chosen selection method (strategy pattern)
            public AbstractSM       selMethod;
            // Seed for random number generator
            public int              currentSeed;
            // List of map pieces which will be manipulated by the generator,
            // initially copied from _piecesList
            public List<MapPiece>   piecesWorkList;
            // Create a map generation log
            public StringBuilder    log;
            // Position where to place log summary, right after log header
            public int              logSummaryLoc;
            // Measure elapsed time for the generation process
            public Stopwatch        stopwatch;
        }

        private CreateMapData createMapData;

        /// <summary>
        /// Create a new map.
        /// </summary>
        /// <remarks>
        /// This method performs actual map creation, but should only be invoked
        /// indirectly via the <see cref="GenerateMap()"/> method.
        /// </remarks>
        private void CreateMap()
        {
            if (genStep == -1)
            {
                createMapData = new CreateMapData();
                // Get chosen selection method (strategy pattern)
                createMapData.selMethod = _selectionParams.Method;
                createMapData.log = new StringBuilder();
                createMapData.stopwatch = Stopwatch.StartNew();
                // Use predefined seed if set or one based on current time
                if (_useSeed)
                    createMapData.currentSeed = _seed;
                else
                    createMapData.currentSeed = (int)System.DateTime.Now.Ticks;

                // Log seed used for generating this map
                createMapData.log.AppendFormat(
                    "---- Map Generation Log (seed = {0}) ----", createMapData.currentSeed);

                // Position where to place log summary, right after log header
                createMapData.logSummaryLoc = createMapData.log.Length;

                // Initialize random number generator
                Random.InitState(createMapData.currentSeed);

                // Work on a copy and not in the original field, since we will sort
                // this list and we don't want this to be reflected in the editor
                createMapData.piecesWorkList = new List<MapPiece>(_piecesList);

                // Sort list of pieces to use according to the pieces natural order
                // (descending number of connectors)
                createMapData.piecesWorkList.Sort();

                // Initialize list of pieces already placed in the map
                _placedPieces = new List<MapPiece>();

                // Get first piece to place in the map
                if (_useStarter)
                {
                    // Get first piece from list of starting pieces
                    createMapData.starterPiecePrototype = createMapData.selMethod.SelectStartPiece(
                        _startingPieceList, (int)_starterConTol);
                }
                else
                {
                    // Get first piece from list of all pieces
                    createMapData.starterPiecePrototype = createMapData.selMethod.SelectStartPiece(
                        createMapData.piecesWorkList, (int)_starterConTol);
                }

                // Get the starter piece by cloning the prototype piece selected for
                // this purpose
                createMapData.starterPieceGObj = createMapData.starterPiecePrototype.ClonePiece();

                // Rename piece so it's easier to determine that it's the
                // starter piece
                createMapData.starterPieceGObj.name += " : Starter Piece";

                // Set generation manager as parent of the starter piece
                createMapData.starterPieceGObj.transform.SetParent(transform);

                // Add starter piece script component to list of placed pieces
                _placedPieces.Add(createMapData.starterPieceGObj.GetComponent<MapPiece>());

                // Initially, the guide piece is the starting piece
                createMapData.guidePiece = _placedPieces[0];

                // Log starting piece
                createMapData.log.AppendFormat(
                    "\n\tStarting piece is '{0}' with {1} free connectors",
                    createMapData.starterPieceGObj.name,
                    createMapData.starterPieceGObj.GetComponent<MapPiece>().FreeConnectorCount);

                // Get the initial piece, which is also the parent piece of all
                // others
                GameObject initialPiece = _placedPieces[0].gameObject;
                // Set the position/rotation of the initial piece as the same as GenerationManager
                // Don't touch scale, because it might influence the size of pieces, etc
                initialPiece.transform.position = transform.position;
                initialPiece.transform.rotation = transform.rotation;

                // Add a component to identify the initial piece so we can delete
                // it when a clear map operation is requested
                initialPiece?.AddComponent<GeneratedObject>();

                genStep = 0;
                if (isStepActive) return;
            }

            if (isStepActive)
            {
                if (createMapData.guidePiece)
                {
                    Debug.Log($"Current guide piece = {createMapData.guidePiece.name}");
                }
                else
                {
                    Debug.LogWarning("No guide piece!");
                }
            }

            // Enter main generation loop
            do
            {
                // Number of failed attempts for current guide piece
                int failCount = 0;

                // Result of trying two snap two pieces together
                bool snapResult = false;

                // Log for current guide piece
                StringBuilder logFailures = new StringBuilder();

                // Log successful snap, if any
                string logSuccess = "";

                // Tentative piece selection and placement loop
                do
                {
                    var     potentialPieces = new List<MapPiece>(createMapData.piecesWorkList);

                    while (potentialPieces.Count > 0)
                    {
                        // Randomly get a tentative piece prototype
                        int rng = Random.Range(0, potentialPieces.Count);
                        createMapData.tentPiecePrototype = potentialPieces[rng];

                        // Remove this piece from the list, we won't need it again - if it 
                        // fails once, it will fail again
                        potentialPieces.Remove(createMapData.tentPiecePrototype);

                        // Get a tentative piece by cloning the tentative piece prototype
                        GameObject tentPieceGObj =
                            createMapData.tentPiecePrototype.ClonePiece();

                        // Get the script associated with the tentative piece
                        MapPiece tentPiece = tentPieceGObj.GetComponent<MapPiece>();

                        // Try and snap the tentative piece with the current guide piece
                        snapResult = createMapData.guidePiece.TrySnapWith(
                            _matchingRules,
                            tentPiece,
                            _checkOverlaps,
                            _collidersLayer,
                            _pieceDistance,
                            _pinCountTolerance,
                            _colorMatrix.GetCells(),
                            _placedPieces,
                            genStep);

                        // Was the snap successful?
                        if (snapResult)
                        {
                            tentPieceGObj.name += $" - {_placedPieces.Count}";
                            tentPieceGObj.transform.SetParent(createMapData.guidePiece.transform);
                            _placedPieces.Add(tentPiece);
                            OnConnectionMade.Invoke(tentPiece);

                            logSuccess = string.Format(
                                "\n\t\tSnap successful with '{0}' (piece no. {1} in the map)",
                                tentPieceGObj.name,
                                _placedPieces.Count);

                            // Break out of the inner loop, since we don't need to test another
                            // piece
                            break;
                        }
                        else
                        {
                            // No valid connections

                            // Log occurrence
                            if (logFailures.Length > 0) logFailures.Append(", ");
                            logFailures.AppendFormat(
                                "'{0}'", tentPieceGObj.name);

                            // Destroy tentative piece game object
                            DestroyImmediate(tentPieceGObj);

                            // Increase count of failed attempts
                            failCount++;

                            // If max failures is reached, log occurrence
                            if (failCount >= _maxFailures)
                            {
                                logFailures.AppendFormat(
                                    " and gave up after {0} attempts",
                                    failCount);
                                
                                // Break out of the inner loop, since max failures has 
                                // already been exceeded
                                break;
                            }
                        }
                    }
                    if (failCount >= _maxFailures)
                    {
                        // Break again, since max failures has been exceeded
                        break;
                    }
                }
                while (!snapResult && failCount < _maxFailures);

                // Add failures log to main log
                if (logFailures.Length > 0)
                {
                    createMapData.log.AppendFormat(
                        "\n\t\tNo valid connections with the following tentatives: {0}",
                        logFailures);
                }

                // Add success log to main log
                createMapData.log.Append(logSuccess);

                // Select next guide piece
                createMapData.guidePiece = createMapData.selMethod.SelectGuidePiece(_placedPieces);

                genStep++;

                // Log new guide piece
                if (createMapData.guidePiece is null)
                {
                    createMapData.log.Append("\n\tGuide piece is null, generation over");
                }
                else
                {
                    createMapData.log.AppendFormat(
                        "\n\tGuide piece is '{0}' with {1} free connectors",
                        createMapData.guidePiece.name,
                        createMapData.guidePiece.FreeConnectorCount);

                    if (isStepActive) return;
                }
            }
            while (createMapData.guidePiece != null);

            // Are we checking for overlaps?
            if (_checkOverlaps)
            {
                // Remove all colliders used for the generation process
                foreach (BoxCollider boxCollider in FindObjectsByType<BoxCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (boxCollider == null) continue;

                    if (((1 << boxCollider.gameObject.layer) & (_collidersLayer.value)) != 0)
                    {
                        GameObject go = boxCollider.gameObject;

                        DestroyImmediate(boxCollider);

                        if (go.GetComponents<Component>().Length == 1)
                        {
                            // Object was just a container for the box colliders, delete it
                            DestroyImmediate(go);
                        }
                    }
                }
                foreach (VoxelCollider voxelCollider in FindObjectsByType<VoxelCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (voxelCollider == null) continue;

                    if (((1 << voxelCollider.gameObject.layer) & (_collidersLayer.value)) != 0)
                    {
                        GameObject go = voxelCollider.gameObject;

                        DestroyImmediate(voxelCollider);

                        if (go.GetComponents<Component>().Length == 1)
                        {
                            // Object was just a container for the voxel colliders, delete it
                            DestroyImmediate(go);
                        }
                    }
                }
            }

            // Stop stopwatch
            createMapData.stopwatch.Stop();

            // Keep elapsed time in property
            GenTimeMillis = (int)createMapData.stopwatch.ElapsedMilliseconds;

            // Log number of pieces placed and elapsed time
            createMapData.log.Insert(createMapData.logSummaryLoc, string.Format(
                "\n\tPlaced {0} pieces in {1} ms, as follows:",
                _placedPieces.Count, GenTimeMillis));


            // Show piece placing log
            Debug.Log(createMapData.log);

            isComplete = true;
        }

        /// <summary>
        /// Generates a new map in editor mode. Method invoked when user presses
        /// the "Generate" button in the editor.
        /// </summary>
        [Button("Generate", enabledMode: EButtonEnableMode.Editor)]
        private void GenerateMap()
        {
            // Clear any previously generated map
            ClearMap();

            isStepActive = false;


            try
            {
                // Invoke generation starting events
                OnGenerationBegin.Invoke();

                // Generate a new map
                CreateMap();
            }
            catch (System.Exception ex)
            {
                // Inform user of error during map generation
                Debug.LogErrorFormat("{0} ({1}):\n{2}",
                    ex.Message, ex.GetType().Name, ex.StackTrace);
                EditorUtility.DisplayDialog(
                    $"Error : {ex.GetType().Name}",
                    $"{ex.Message}\nDetailed information shown in the console.",
                    "Ok");

                // Try to clean-up as best as possible
                ClearMap();
                foreach (var obj in FindObjectsByType<MapPiece>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    DestroyImmediate(obj);
                }
            }
        }

        [Button("Start Generation")]
        private void StartGeneration()
        {
            ClearMap();

            Debug.Log($"Starting generation...");

            genStep = -1;
            isComplete = false;
            isStepActive = true;

            OnGenerationBegin.Invoke();

            CreateMap();
        }

        [Button("Generate Next Step")]
        private void NextStep()
        {
            if ((!isStepActive) || (genStep < 0))
            {
                Debug.LogError($"Need to start the process with the Start Generation button!");
                return;
            }
            if (isComplete)
            {
                Debug.LogWarning($"Process was already complete (in {genStep} steps), restart it with Start Generation button!");
                return;
            }

            Debug.Log($"Running step {genStep}");

            CreateMap();

            if (isComplete)
            {
                // Notify listeners that map generations is finished
                OnGenerationFinish.Invoke(PlacedPieces);

                Debug.Log($"Process complete in {genStep} steps!");
            }
        }

        /// <summary>
        /// Clears a map generated in editor mode.
        /// </summary>
        [Button("Clear", enabledMode: EButtonEnableMode.Editor)]
        private void ClearMap()
        {
            // Find any pieces with the GeneratedObject component and
            // deletes them (and all of its children)
            foreach (var obj in FindObjectsByType<GeneratedObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                DestroyImmediate(obj.gameObject);
            }

            // Clear list of placed pieces
            _placedPieces = null;

            // Reset steps
            genStep = -1;
            isComplete = false;

            // Raise clear map event
            OnGenerationClear.Invoke();
        }
    }
}
