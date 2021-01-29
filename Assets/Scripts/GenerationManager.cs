/*
 * Copyright 2021 TrinityGenerator_Standalone contributors
 * (https://github.com/RafaelCS-Aula/TrinityGenerator_Standalone)
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
using UnityEditor;
using NaughtyAttributes;
using Array2DEditor;
using TrinityGen.GenerationMethods;

namespace TrinityGen
{
    public class GenerationManager : MonoBehaviour
    {
        [Header("----- Content Settings -----")]

        [SerializeField]
        private List<ArenaPiece> _piecesForGeneration;

        [Header("----- Starting Piece Settings -----")]

        [SerializeField]
        private bool _setStartingPiece = false;

        [SerializeField]
        private List<ArenaPiece> _possibleStartingPieces;

        [SerializeField]
        private uint _connectorCountTolerance = 0;

        [Header("----- World Settings -----")]

        [SerializeField]
        private bool defineSeed;

        [SerializeField]
        private int seed;

        [SerializeField]
        private bool _useClippingCorrection = false;

        [SerializeField]
        private float _pieceDistance = 0.0001f;

        [Header("----- Connection Settings -----")]

        [SerializeField]
        private ConnectorMatchingRules _matchingRules;

        [SerializeField]
        private uint _pinCountTolerance = 0;

        [SerializeField]
        private Array2DBool colorMatrix;

        /// <summary>
        ///     tentative  W, R, G, B, CYAN, ORNG, YLLW, PINK, PRPL, BRWN, BLACK, GREY
        ///     guide   W,
        ///             R,
        ///             G,
        ///             B,
        ///             CYAN
        ///             ORNG,
        ///             YLLW,
        ///             PINK,
        ///             PRPL,
        ///             BRWN,
        ///             BLACK,
        ///             GREY
        /// </summary>
        /// <value></value>
        [SerializeField]
        private bool[,] _colorMatchMatrix => colorMatrix.GetCells();
        /*{
// tentative  W, R, G, B, CYAN, ORNG, YLLW, PINK, PRPL, BRWN, BLACK, GREY
        {true, true, true, true, true, true, true, true, true, true, true, true},
        {true, true, false, false, false, false, false, false, false, false, false, false},

        {true, false, true, false, false, false, false, false, false, false, false, false},

        {true, false, false, true, false, false, false, false, false, false, false, false},

        {true, false, false, false, true, false, false, false, false, false, false, false},

        {true, false, false, false, false, true, false, false, false, false, false, false},

        {true, false, false, false, false, false, true, false, false, false, false, false},

        {true, false, false, false, false, false, false, true, false, false, false, false},

        {true, false, false, false, false, false, false, false, true, false, false, false},

        {true, false, false, false, false, false, false, false, false, true, false, false},

        {true, false, false, false, false, false, false, false, false, false, true, false},

        {true, false, false, false, false, false, false, false, false, false, false, true},


        };*/

        [Header("----- Generation Settings -----")]

        [SerializeField]
        [Dropdown("GenMethods")] [OnValueChanged("OnChangeGMName")]
        private string _generationMethod;

        [SerializeField] [Expandable] [OnValueChanged("OnChangeGMType")]
        private GMConfig _generationParams;

        [SerializeField]
        private uint _maxFailures = 10;


        [Header("----- Testing Settings -----")]

        [Tooltip("Generate Arena on scene start automatically. (DANGEROUS)")]
        [SerializeField]
        private bool _autoCreate = false;

        /*[Header("------ Vertical Level Settings --------")]
        [SerializeField] private bool _createUpperLevel;

        [Tooltip("Choose random pieces and put other pieces above them.")]
        [SerializeField] private bool _upperLevelIslandGeneration;
        [SerializeField] private int _upperIslandsCount = 1;

        [Header("--")]
        [SerializeField] private bool _createLowerLevel;

        [Tooltip("Choose random pieces and put other pieces under them.")]
        [SerializeField] private bool _lowerLevelIslandGeneration;
        [SerializeField] private int _lowerIslandsCount = 1;*/


        private List<ArenaPiece> _piecesForGenerationWorkList;
        private List<ArenaPiece> _placedPieces;
        private int _currentSeed;
        private IList<List<ArenaPiece>> _sortedPieces;

        // Already placed piece being used to judge others
        private ArenaPiece _guidePiece;

        // Piece being evaluated against selectedPiece
        private ArenaPiece _tentativePiece;

        private GenerationMethod _chosenMethod;

        private int _largestGroup;

        // Names of known generation method
        [System.NonSerialized]
        private string[] _genMethods;

        // Get generation method names
        private ICollection<string> GenMethods
        {
            get
            {
                // Did we initialize gen. method names already?
                if (_genMethods is null)
                {
                    // Get gen. method names
                    _genMethods = GenMethodManager.Instance.GenMethodNames;
                    // Sort them
                    System.Array.Sort(_genMethods);
                }

                // Return existing methods
                return _genMethods;
            }
        }

        // Callback invoked when user changes gen. method type in editor
        private void OnChangeGMType()
        {
            // Make sure gen. method name is updated accordingly
            _generationMethod = GenMethodManager.Instance.GetNameFromType(
                _generationParams.GetType());
        }

        // Callback invoked when user changes gen. method name in editor
        private void OnChangeGMName()
        {
            // Make sure gen. method type is updated accordingly
            System.Type gmConfig =
                GenMethodManager.Instance.GetTypeFromName(_generationMethod);
            _generationParams = GMConfig.GetInstance(gmConfig);
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

        public GameObject Create()
        {
            if (_generationParams is null)
            {
                EditorUtility.DisplayDialog(
                    "Warning", "Please select a generation method", "Ok");
                return null;
            }

            // Use predefined seed if set or one based on current time
            if (defineSeed)
                _currentSeed = seed;
            else
                _currentSeed = System.DateTime.Now.Millisecond;

            // Initialize random number generator
            Random.InitState(_currentSeed);

            // Work on a copy and not in the original field, since we will sort
            // this list and we don't want this to be reflected in the editor
            _piecesForGenerationWorkList =
                new List<ArenaPiece>(_piecesForGeneration);

            // Get chosen generation method (strategy pattern)
            _chosenMethod = _generationParams.Method;

            // Sort list of pieces to use according to the pieces natural order
            _piecesForGenerationWorkList.Sort();

            // Get the number of connectors from the piece with the most
            // connectors
            _largestGroup = _piecesForGenerationWorkList[0].ConnectorsCount;

            // Separate pieces into separate lists based on largest group
            _sortedPieces = SplitList();

            _placedPieces = new List<ArenaPiece>();
            ArenaPiece started;
            if (_setStartingPiece)
            {
                started = _chosenMethod.SelectStartPiece(
                    _possibleStartingPieces, (int)_connectorCountTolerance);
            }
            else
            {
                started = _chosenMethod.SelectStartPiece(
                    _piecesForGenerationWorkList,
                    (int)_connectorCountTolerance);
            }

            GameObject inst = started.ClonePiece(_useClippingCorrection);
            _placedPieces.Add(inst.GetComponent<ArenaPiece>());
            inst.name += " - START ";

            GameObject initialPiece = inst;
            _guidePiece = _placedPieces[0];

            // Make base level of Arena and add those pieces to the list
            int placement = 0;
            do
            {
                int failureCount = 0;

                // Pick a piece to evaluate against our selected placed one
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
                    ArenaPiece spawnedScript =
                        spawnedPiece.GetComponent<ArenaPiece>();

                    (bool valid, Transform trn) evaluationResult =
                        _guidePiece.EvaluatePiece(_matchingRules, spawnedScript,
                        _pieceDistance,
                        _pinCountTolerance,
                        _colorMatchMatrix);

                    // If things worked out, spawn the piece in the correct
                    // position
                    if (evaluationResult.valid)
                    {
                        placement++;
                        spawnedPiece.name += $" - {placement}";
                        spawnedPiece.transform.SetParent(_guidePiece.transform);
                        _placedPieces.Add(spawnedScript);
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
                } // selectPiece
            }
            while (_guidePiece != null);

            Debug.Log("Generated with seed: " + _currentSeed);

            return initialPiece;
        }

        /// <summary>
        /// Separate pieces into separate lists based on largest group
        /// </summary>
        private List<List<ArenaPiece>> SplitList()
        {
            int lastConsidered = _largestGroup + 1;
            List<ArenaPiece> consideredList = new List<ArenaPiece>();
            List<List<ArenaPiece>> sortedList = new List<List<ArenaPiece>>();

            for (int i = 0; i < _piecesForGenerationWorkList.Count; i++)
            {
                // Piece belongs in a new list made for its size
                if (_piecesForGenerationWorkList[i].ConnectorsCount
                    < lastConsidered)
                {
                    consideredList = new List<ArenaPiece>();
                    consideredList.Add(_piecesForGenerationWorkList[i]);
                    lastConsidered =
                        _piecesForGenerationWorkList[i].ConnectorsCount;
                    sortedList.Add(consideredList);

                }
                // piece belongs in the already made list
                else if (_piecesForGenerationWorkList[i].ConnectorsCount >=
                    lastConsidered - _connectorCountTolerance)
                {
                    consideredList.Add(_piecesForGenerationWorkList[i]);
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
    }
}
