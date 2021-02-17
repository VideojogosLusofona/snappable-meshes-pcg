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
        private const string events = ":: Events ::";

        // ////////////////// //
        // Content parameters //
        // ////////////////// //

        [BoxGroup(contentParams)]
        [SerializeField]
        private List<MapPiece> _piecesForGeneration;

        [BoxGroup(contentParams)]
        [ReorderableList]
        [SerializeField]
        private List<MapPiece> _piecesList;

        [BoxGroup(contentParams)]
        [Label("Use Starter Piece List")]
        [SerializeField]
        private bool _useStarter;

        [BoxGroup(contentParams)]
        [ReorderableList]
        [SerializeField]
        [ShowIf(nameof(_useStarter))]
        private List<MapPiece> _startingPieceList;

        // //////////////// //
        // World parameters //
        // //////////////// //

        [BoxGroup(worldParams)]
        [SerializeField]
        private bool _useSeed;

        [BoxGroup(worldParams)]
        [SerializeField]
        [ShowIf(nameof(_useSeed))]
        private int _seed;

        [BoxGroup(worldParams)]
        [SerializeField]
        private bool _useClippingCorrection = false;

        [BoxGroup(worldParams)]
        [SerializeField]
        private float _pieceDistance = 0.0001f;

        // ///////////////////// //
        // Connection parameters //
        // ///////////////////// //

        [BoxGroup(connectionParams)]
        [SerializeField]
        [EnumFlags]
        private SnapRules _matchingRules;

        [BoxGroup(connectionParams)]
        [SerializeField]
        private uint _pinCountTolerance = 0;

        [BoxGroup(connectionParams)]
        [SerializeField]
        [Tooltip("Rows refer to the guide piece, columns to the tentative piece")]
        private Array2DBool _colorMatrix;

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
        private uint _starterConTol;

        // ////////////////// //
        // Testing parameters //
        // ////////////////// //

        [BoxGroup(testingParams)]
        [Tooltip("Generate Arena on scene start automatically. (DANGEROUS)")]
        [SerializeField]
        private bool _autoCreate = false;

        // ////// //
        // Events //
        // ////// //

        [Foldout(events)]
        [SerializeField]
        private UnityEvent OnGenerationBegin;

        [Foldout(events)]
        [SerializeField]
        private UnityEvent<MapPiece[]> OnGenerationFinish;

        [Foldout(events)]
        [SerializeField]
        private UnityEvent<MapPiece> OnConnectionMade;

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

        //
        private IList<List<MapPiece>> _sortedPieces;

        // Already placed piece being used to judge others
        private MapPiece _guidePiece;

        // Piece being evaluated against guide piece
        private MapPiece _tentativePiece;

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

        // Start is called before the first frame update
        private void Start()
        {
            if (_autoCreate)
            {
                Debug.Log("ATTENTION: AUTO CREATE IS ON. TURN OFF FOR GAMEPLAY");
                ClearEditorGeneration();
                Create();
            }
        }

        // Create a new map
        public GameObject Create()
        {
            // First piece to be placed in the map
            MapPiece started;

            // Starter piece
            GameObject starterPiece;

            // If we're using a starting piece, the starting piece list cannot
            // be empty
            if (_useStarter
                && (_startingPieceList is null || _startingPieceList.Count == 0))
            {
                EditorUtility.DisplayDialog(
                    "Warning",
                    "Starting piece list is empty, aborting map generation.",
                    "Ok");
                return null;
            }

            // Use predefined seed if set or one based on current time
            if (_useSeed)
                _currentSeed = _seed;
            else
                _currentSeed = System.DateTime.Now.Millisecond;

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

            // Separate pieces into separate lists based on largest group
            _sortedPieces = SplitList();

            // Initialize list of pieces already placed in the map
            _placedPieces = new List<MapPiece>();

            // Get first piece to place in the map
            if (_useStarter)
            {
                // Get first piece from list of starting pieces
                started = _chosenMethod.SelectStartPiece(
                    _startingPieceList, (int)_starterConTol);
            }
            else
            {
                // Get first piece from list of all pieces
                started = _chosenMethod.SelectStartPiece(
                    _piecesWorkList, (int)_starterConTol);
            }

            // Get the starter piece by cloning the prototype piece selected for
            // this purpose
            starterPiece = started.ClonePiece(_useClippingCorrection);

            // Rename piece so it's easier to determine that it's the
            // starter piece
            starterPiece.name += " : Starter Piece ";

            // Add starter piece script component to list of placed pieces
            _placedPieces.Add(starterPiece.GetComponent<MapPiece>());

            // Initially, the guide piece is the starting piece
            _guidePiece = _placedPieces[0];

            // Make base level of Arena and add those pieces to the list
            int placement = 0;
            do
            {
                int failureCount = 0;

                // Pick a tentative piece to evaluate against our guide piece
                while (true)
                {
                    int rng;

                    // Check what list of the sorted list the selected belongs to
                    int myPieceList = Random.Range(0, _sortedPieces.Count);

                    if (_sortedPieces[myPieceList].Count != 0)
                    {
                        rng = Random.Range(0, _sortedPieces[myPieceList].Count);
                    }
                    else
                    {
                        continue;
                    }

                    _tentativePiece = _sortedPieces[myPieceList][rng];

                    GameObject spawnedPiece =
                        _tentativePiece.ClonePiece(_useClippingCorrection);
                    MapPiece spawnedScript =
                        spawnedPiece.GetComponent<MapPiece>();

                    (bool valid, Transform trn) evaluationResult =
                        _guidePiece.EvaluatePiece(_matchingRules, spawnedScript,
                        _pieceDistance,
                        _pinCountTolerance,
                        _colorMatrix.GetCells());

                    // If things worked out, spawn the piece in the correct
                    // position
                    if (evaluationResult.valid)
                    {
                        placement++;
                        spawnedPiece.name += $" - {placement}";
                        spawnedPiece.transform.SetParent(_guidePiece.transform);
                        _placedPieces.Add(spawnedScript);
                        OnConnectionMade.Invoke(spawnedScript);
                    }
                    else
                    {
                        print("No valid found");
                        // No valid connectors in the given piece
                        if (Application.isPlaying)
                            Destroy(spawnedPiece);
                        else
                            DestroyImmediate(spawnedPiece);
                        failureCount++;
                        if (failureCount > _maxFailures)
                            break;
                        continue;
                    }

                    _guidePiece = _chosenMethod.SelectGuidePiece(
                        _placedPieces, _placedPieces[_placedPieces.Count - 1]);

                    break;
                } // select tentative piece
            }
            while (_guidePiece != null);

            Debug.Log($"Generated with Seed: {_currentSeed}.");
            if(_useClippingCorrection)
                SimulatePhysics();
            OnGenerationFinish.Invoke(_placedPieces.ToArray());
            return starterPiece;
        }

        // Separate pieces into separate lists based on number of connectors
        private IList<List<MapPiece>> SplitList()
        {
            int lastConsidered = _maxConnectors + 1;
            List<MapPiece> consideredList = new List<MapPiece>();
            IList<List<MapPiece>> sortedList = new List<List<MapPiece>>();

            for (int i = 0; i < _piecesWorkList.Count; i++)
            {
                // Piece belongs in a new list made for its size
                if (_piecesWorkList[i].ConnectorCount < lastConsidered)
                {
                    consideredList = new List<MapPiece>();
                    consideredList.Add(_piecesWorkList[i]);
                    lastConsidered = _piecesWorkList[i].ConnectorCount;
                    sortedList.Add(consideredList);
                }
                // Piece belongs in the already made list
                else if (_piecesWorkList[i].ConnectorCount >=
                    lastConsidered - _starterConTol)
                {
                    consideredList.Add(_piecesWorkList[i]);
                }
            }

            return sortedList;
        }

        [Button("Generate")]
        private void EditorGenerate()
        {
            ClearEditorGeneration();

            GameObject initialPiece = Create();

            // Add a component to identify this case, so we can delete it on a
            // new generation
            initialPiece?.AddComponent<EditorGenerationPiece>();
        }

        [Button("Clear")]
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
        }

        [Button]
        private void SimulatePhysics()
        {
            Physics.autoSimulation = false;
            bool settled = false;

/*            do
            {
                settled = true;
                foreach(MapPiece mP in _placedPieces)
                {
                    Rigidbody pieceBody = 
                        mP.gameObject.GetComponent<Rigidbody>();
                    if(pieceBody = null)
                    {
                        pieceBody = mP.GetComponentInChildren<Rigidbody>();
                    }
                    if(pieceBody != null)
                    {
                        if(pieceBody.velocity.sqrMagnitude >= 0.001f)
                        {
                            settled = false;
                        }
                        
                    }
                    
                }
                
            }while(!settled);*/

            Physics.Simulate(Time.fixedDeltaTime);
            Physics.autoSimulation = true;
                
        }
    }
}
