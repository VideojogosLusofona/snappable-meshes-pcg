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

using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using NaughtyAttributes;
using Array2DEditor;
using SnapMeshPCG.GenerationMethods;

namespace SnapMeshPCG
{
    public class GenerationManager : MonoBehaviour
    {
        // ///////// //
        // Constants //
        // ///////// //

        private const string contentParams = ":: Content parameters ::";
        private const string generalParams = ":: General parameters ::";
        private const string connectionParams = ":: Connection parameters ::";
        private const string generationParams = ":: Generation parameters ::";
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

        // ///////////////////// //
        // Generation parameters //
        // ///////////////////// //

        [BoxGroup(generationParams)]
        [SerializeField]
        [Dropdown(nameof(GenMethods))]
        [OnValueChanged("OnChangeGMName")]
        private string _generationMethod;

        [BoxGroup(generationParams)]
        [SerializeField]
        [Expandable]
        [OnValueChanged(nameof(OnChangeGMType))]
        private AbstractGMConfig _generationParams;

        [BoxGroup(generationParams)]
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
        [SerializeField] [HideInInspector]
        private List<MapPiece> _placedPieces;

        // Names of known generation methods
        [System.NonSerialized]
        private string[] _genMethods;

        // ////////// //
        // Properties //
        // ////////// //

        // Get generation method names
        private ICollection<string> GenMethods
        {
            get
            {
                // Did we initialize gen. method names already?
                if (_genMethods is null)
                {
                    // Get gen. method names
                    _genMethods = GMManager.Instance.GenMethodNames;
                    // Sort them
                    System.Array.Sort(_genMethods);
                }

                // Return existing methods
                return _genMethods;
            }
        }

        /// <summary>
        /// Get map piece placed in the map, in the order they were placed.
        /// </summary>
        public IReadOnlyList<MapPiece> PlacedPieces => _placedPieces;

        // /////// //
        // Methods //
        // ////// //

        // Callback invoked when user changes generation method type in editor
        private void OnChangeGMType()
        {
            if (_generationParams is null)
            {
                // Cannot allow this field to be empty, so set it back to what
                // is specified in the generation method name
                Debug.Log(
                    $"The {nameof(_generationParams)} field cannot be empty");
                OnChangeGMName();
            }
            else
            {
                // Update generation method name accordingly to what is now set
                // in the generation configurator fields
                _generationMethod = GMManager.Instance.GetNameFromType(
                    _generationParams.GetType());
            }
        }

        // Callback invoked when user changes generation method name in editor
        private void OnChangeGMName()
        {
            // Make sure gen. method type is updated accordingly
            System.Type gmConfig =
                GMManager.Instance.GetTypeFromName(_generationMethod);
            _generationParams = AbstractGMConfig.GetInstance(gmConfig);
        }

        /// <summary>
        /// Create a new map.
        /// </summary>
        private void Create()
        {
            // Stater piece prototype
            MapPiece starterPiecePrototype;

            // Starter piece game object
            GameObject starterPieceGObj;

            // Current guide piece
            MapPiece guidePiece;

            // Tentative piece prototype
            MapPiece tentPiecePrototype;

            // Get chosen generation method (strategy pattern)
            AbstractGM genMethod = _generationParams.Method;

            // Seed for random number generator
            int currentSeed;

            // List of map pieces which will be manipulated by the generator,
            // initially copied from _piecesList
            List<MapPiece> piecesWorkList;

            // Create a map generation log
            StringBuilder log = new StringBuilder("=== Map generation log ===");

            // If we're using a starting piece, the starting piece list cannot
            // be empty
            if (_useStarter
                && (_startingPieceList is null || _startingPieceList.Count == 0))
            {
                EditorUtility.DisplayDialog(
                    "Warning",
                    "Starting piece list is empty, aborting map generation.",
                    "Ok");
                return;
            }

            // Use predefined seed if set or one based on current time
            if (_useSeed)
                currentSeed = _seed;
            else
                currentSeed = (int)System.DateTime.Now.Ticks;

            // Log seed used for generating this map
            log.AppendFormat("\n\tGenerating with seed = {0}", currentSeed);

            // Initialize random number generator
            Random.InitState(currentSeed);

            // Invoke generation starting events
            OnGenerationBegin.Invoke();

            // Work on a copy and not in the original field, since we will sort
            // this list and we don't want this to be reflected in the editor
            piecesWorkList = new List<MapPiece>(_piecesList);

            // Sort list of pieces to use according to the pieces natural order
            // (descending number of connectors)
            piecesWorkList.Sort();

            // Initialize list of pieces already placed in the map
            _placedPieces = new List<MapPiece>();

            // Get first piece to place in the map
            if (_useStarter)
            {
                // Get first piece from list of starting pieces
                starterPiecePrototype = genMethod.SelectStartPiece(
                    _startingPieceList, (int)_starterConTol);
            }
            else
            {
                // Get first piece from list of all pieces
                starterPiecePrototype = genMethod.SelectStartPiece(
                    piecesWorkList, (int)_starterConTol);
            }

            // Get the starter piece by cloning the prototype piece selected for
            // this purpose
            starterPieceGObj =
                starterPiecePrototype.ClonePiece();

            // Rename piece so it's easier to determine that it's the
            // starter piece
            starterPieceGObj.name += " : Starter Piece";

            // Add starter piece script component to list of placed pieces
            _placedPieces.Add(starterPieceGObj.GetComponent<MapPiece>());

            // Initially, the guide piece is the starting piece
            guidePiece = _placedPieces[0];

            // Log starting piece
            log.AppendFormat(
                "\n\tStarting piece is '{0}' with {1} free connectors",
                starterPieceGObj.name,
                starterPieceGObj.GetComponent<MapPiece>().FreeConnectorCount);

            // Enter main generation loop
            do
            {
                // Number of failed attempts for current guide piece
                int failCount = 0;

                // Result of trying two snap two pieces together
                bool snapResult;

                // Log for current guide piece
                StringBuilder logFailures = new StringBuilder();

                // Log successful snap, if any
                string logSuccess = "";

                // Tentative piece selection and placement loop
                do
                {
                    // Randomly get a tentative piece prototype
                    int rng = Random.Range(0, piecesWorkList.Count);
                    tentPiecePrototype = piecesWorkList[rng];

                    // Get a tentative piece by cloning the tentative piece prototype
                    GameObject tentPieceGObj =
                        tentPiecePrototype.ClonePiece();

                    // Get the script associated with the tentative piece
                    MapPiece tentPiece = tentPieceGObj.GetComponent<MapPiece>();

                    // Try and snap the tentative piece with the current guide piece
                    snapResult = guidePiece.TrySnapWith(
                        _matchingRules,
                        tentPiece,
                        _checkOverlaps,
                        _collidersLayer,
                        _pieceDistance,
                        _pinCountTolerance,
                        _colorMatrix.GetCells());

                    // Was the snap successful?
                    if (snapResult)
                    {
                        tentPieceGObj.name += $" - {_placedPieces.Count}";
                        tentPieceGObj.transform.SetParent(guidePiece.transform);
                        _placedPieces.Add(tentPiece);
                        OnConnectionMade.Invoke(tentPiece);

                        logSuccess = string.Format(
                            "\n\t\tSnap successful with '{0}' (piece no. {1} in the map)",
                            tentPieceGObj.name,
                            _placedPieces.Count);
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
                        }
                    }
                }
                while (!snapResult && failCount < _maxFailures);

                // Add failures log to main log
                if (logFailures.Length > 0)
                {
                    log.AppendFormat(
                            "\n\t\tNo valid connections with the following tentatives: {0}",
                            logFailures);
                }

                // Add success log to main log
                log.Append(logSuccess);

                // Select next guide piece
                guidePiece = genMethod.SelectGuidePiece(_placedPieces);

                // Log new guide piece
                if (guidePiece is null)
                {
                    log.Append("\n\tGuide piece is null, generation over");
                }
                else
                {
                    log.AppendFormat(
                        "\n\tGuide piece is '{0}' with {1} free connectors",
                        guidePiece.name,
                        guidePiece.FreeConnectorCount);
                }
            }
            while (guidePiece != null);

            // Show piece placing log
            Debug.Log(log);

            // If we checked for overlaps...
            if (_checkOverlaps)
            {
                // ...remove all colliders used for the generation process
                foreach (BoxCollider boxCollider in FindObjectsOfType<BoxCollider>())
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
            }

            // Notify listeners that map generations is finished
            OnGenerationFinish.Invoke(PlacedPieces);
        }

        [Button("Generate", enabledMode: EButtonEnableMode.Editor)]
        private void EditorGenerate()
        {
            ClearEditorGeneration();
            Create();
            GameObject initialPiece = _placedPieces[0].gameObject;

            // Add a component to identify this case, so we can delete it on a
            // new generation
            initialPiece?.AddComponent<EditorGenerationPiece>();
        }

        [Button("Clear", enabledMode: EButtonEnableMode.Editor)]
        private void ClearEditorGeneration()
        {
            // Find any pieces with the EditorGenerationPiece component
            // This component is indicative of a piece that was generated in the
            // editor and has to be deleted (and all it's children) when we
            // regenerate from the editor
            // The script also deletes on Start, so it doesn't "survive" play
            // mode
            EditorGenerationPiece[] editorGenerations =
                FindObjectsOfType<EditorGenerationPiece>();
            foreach (EditorGenerationPiece obj in editorGenerations)
            {
                DestroyImmediate(obj.gameObject);
            }
            OnGenerationClear.Invoke();
        }

    }
}
