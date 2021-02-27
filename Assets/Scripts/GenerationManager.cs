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
        private const string worldParams = ":: World parameters ::";
        private const string connectionParams = ":: Connection parameters ::";
        private const string generationParams = ":: Generation parameters ::";
        private const string testingParams = ":: Testing parameters ::";
        private const string navMeshParams = ":: NavMesh parameters ::";
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

        // //////////////// //
        // World parameters //
        // //////////////// //

        [BoxGroup(worldParams)]
        [SerializeField]
        private bool _useSeed = false;

        [BoxGroup(worldParams)]
        [SerializeField]
        [ShowIf(nameof(_useSeed))]
        private int _seed = 0;

        [BoxGroup(worldParams)]
        [SerializeField]
        private bool _useClippingCorrection = false;

        [BoxGroup(worldParams)]
        [SerializeField]
        private float _pieceDistance = 0.0001f;

        [BoxGroup(worldParams)]
        [SerializeField]
        private bool _intersectionTests;

        [BoxGroup(worldParams), ShowIf("_intersectionTests")]
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
        [SerializeField]
        private uint _maxFailures = 10;

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
        private UnityEvent<MapPiece[]> OnGenerationFinish = null;

        [Foldout(events)]
        [SerializeField]
        private UnityEvent<MapPiece> OnConnectionMade = null;

        [Foldout(events)]
        [SerializeField]
        private UnityEvent OnGenerationClear = null;

        // ///////////////////////////////////// //
        // Instance variables not used in editor //
        // ///////////////////////////////////// //

        // Seed for random number generator
        private int _currentSeed;

        // List of map pieces which will be manipulated by the generator,
        // initially copied from _piecesList
        private List<MapPiece> _piecesWorkList;

        // Pieces placed in the map
        private List<MapPiece> _placedPieces;

        // The generation method
        private AbstractGM _chosenMethod;

        // Maximum number of connectors in existing pieces
        private int _maxConnectors;

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
                _currentSeed = _seed;
            else
                _currentSeed = (int)System.DateTime.Now.Ticks;

            // Notify user of seed used for generating this map
            Debug.Log($"Generating with seed = {_currentSeed}.");

            // Initialize random number generator
            Random.InitState(_currentSeed);

            // Invoke generation starting events
            OnGenerationBegin.Invoke();

            // Work on a copy and not in the original field, since we will sort
            // this list and we don't want this to be reflected in the editor
            _piecesWorkList = new List<MapPiece>(_piecesList);

            // Get chosen generation method (strategy pattern)
            _chosenMethod = _generationParams.Method;

            // Sort list of pieces to use according to the pieces natural order
            // (descending number of connectors)
            _piecesWorkList.Sort();

            // Get the number of connectors from the piece with the most
            // connectors
            _maxConnectors = _piecesWorkList[0].ConnectorCount;

            // Initialize list of pieces already placed in the map
            _placedPieces = new List<MapPiece>();

            // Get first piece to place in the map
            if (_useStarter)
            {
                // Get first piece from list of starting pieces
                starterPiecePrototype = _chosenMethod.SelectStartPiece(
                    _startingPieceList, (int)_starterConTol);
            }
            else
            {
                // Get first piece from list of all pieces
                starterPiecePrototype = _chosenMethod.SelectStartPiece(
                    _piecesWorkList, (int)_starterConTol);
            }

            // Get the starter piece by cloning the prototype piece selected for
            // this purpose
            starterPieceGObj =
                starterPiecePrototype.ClonePiece(_useClippingCorrection);

            // Rename piece so it's easier to determine that it's the
            // starter piece
            starterPieceGObj.name += " : Starter Piece";

            // Add starter piece script component to list of placed pieces
            _placedPieces.Add(starterPieceGObj.GetComponent<MapPiece>());

            // Initially, the guide piece is the starting piece
            guidePiece = _placedPieces[0];

            // Make base level of Arena and add those pieces to the list
            int placement = 0;
            do
            {
                // Number of failed attempts
                int failCount = 0;

                // Result of trying two snap two pieces together
                bool snapResult;

                // Pick a tentative piece to evaluate against our guide piece
                do
                {
                    // Randomly get a tentative piece prototype
                    int rng = Random.Range(0, _piecesWorkList.Count);
                    tentPiecePrototype = _piecesWorkList[rng];

                    // Get a tentative piece by cloning the tentative piece prototype
                    GameObject tentPieceGObj = tentPiecePrototype.ClonePiece(_useClippingCorrection);

                    // Get the script associated with the tentative piece
                    MapPiece tentPiece = tentPieceGObj.GetComponent<MapPiece>();

                    // Try and snap the tentative piece with the current guide piece
                    snapResult = guidePiece.TrySnapWith(
                        _matchingRules,
                        tentPiece,
                        _intersectionTests,
                        _collidersLayer,
                        _pieceDistance,
                        _pinCountTolerance,
                        _colorMatrix.GetCells());

                    // Was the snap successful?
                    if (snapResult)
                    {
                        placement++;
                        tentPieceGObj.name += $" - {placement}";
                        tentPieceGObj.transform.SetParent(guidePiece.transform);
                        _placedPieces.Add(tentPiece);
                        OnConnectionMade.Invoke(tentPiece);
                    }
                    else
                    {
                        // No valid connections
                        Debug.Log(string.Format(
                            "With {0} placed pieces: no valid connections between '{1}' (guide) and '{2}' (tentative)",
                            _placedPieces.Count, guidePiece.name, tentPieceGObj.name));

                        // Destroy tentative piece game object
                        DestroyImmediate(tentPieceGObj);

                        // Increase count of failed attempts
                        failCount++;

                        // Notify user if max failures is reached
                        if (failCount >= _maxFailures)
                        {
                            Debug.Log(string.Format(
                                "Couldn't find a valid piece to connect with guide piece '{0}'",
                                guidePiece.name));
                        }
                    }
                }
                while (!snapResult && failCount < _maxFailures);

                // Select next guide piece
                guidePiece = _chosenMethod.SelectGuidePiece(
                    _placedPieces, _placedPieces[_placedPieces.Count - 1]);

            }
            while (guidePiece != null);

            // If we performed intersection tests...
            if (_intersectionTests)
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

            if (!_useClippingCorrection)
                OnGenerationFinish.Invoke(_placedPieces.ToArray());
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

        /// <summary>
        /// Simulate a physics step in the editor
        /// </summary>
        [Button("(Clipping Correction) Simulate Physics Step", enabledMode: EButtonEnableMode.Editor)]
        private void SimulatePhysics()
        {
            if (!_useClippingCorrection)
                Debug.LogWarning("This should only be used when clipping" +
                "correction was ON at time of generation.");

            Physics.autoSimulation = false;
            Physics.Simulate(Time.fixedDeltaTime);
            Physics.autoSimulation = true;
        }

        /// <summary>
        /// Manually call the OnGenerationFinish event.
        /// </summary>
        [Button("(Clipping Correction) Call Generation End Events", enabledMode: EButtonEnableMode.Editor)]
        private void CallFinishGenerationEvents()
        {
            if (!_useClippingCorrection)
            {
                Debug.LogWarning("This should only be used when clipping" +
                "correction was ON at time of generation.");
                return;
            }

            if (_placedPieces == null || _placedPieces.Count == 0)
            {
                Debug.LogWarning("No generated pieces found. Generate a map first!");
            }
            OnGenerationFinish.Invoke(_placedPieces.ToArray());
        }

    }
}
