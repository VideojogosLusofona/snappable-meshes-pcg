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
        private const string contentSettings = ":: Content settings ::";
        private const string worldSettings = ":: World settings ::";
        private const string connectionSettings = ":: Connection settings ::";
        private const string generationSettings = ":: Generation settings ::";
        private const string testingSettings = ":: Testing settings ::";

        [BoxGroup(contentSettings)]
        [SerializeField]
        private List<ArenaPiece> _piecesList;

        [BoxGroup(contentSettings)]
        [Label("Starter connector count tolerance")]
        [SerializeField]
        private uint _starterConTol;

        [BoxGroup(contentSettings)]
        [Label("Use starter piece list")]
        [SerializeField]
        private bool _useStarter;

        [BoxGroup(contentSettings)]
        [SerializeField]
        [ShowIf(nameof(_useStarter))]
        private List<ArenaPiece> _startingPieceList;

        [BoxGroup(worldSettings)]
        [SerializeField]
        private bool _useSeed;

        [BoxGroup(worldSettings)]
        [SerializeField]
        [ShowIf(nameof(_useSeed))]
        private int _seed;

        [BoxGroup(worldSettings)]
        [SerializeField]
        private bool _useClippingCorrection = false;

        [BoxGroup(worldSettings)]
        [SerializeField]
        private float _pieceDistance = 0.0001f;

        [BoxGroup(connectionSettings)]
        [SerializeField]
        [EnumFlags]
        private SnapRules _matchingRules;

        [BoxGroup(connectionSettings)]
        [SerializeField]
        private uint _pinCountTolerance = 0;

        [BoxGroup(connectionSettings)]
        [SerializeField]
        [Tooltip("Rows refer to the guide piece, columns to the tentative piece")]
        private Array2DBool _colorMatrix;

        [BoxGroup(generationSettings)]
        [SerializeField]
        [Dropdown(nameof(GenMethods))]
        [OnValueChanged("OnChangeGMName")]
        private string _generationMethod;

        [BoxGroup(generationSettings)]
        [SerializeField]
        [Expandable]
        [OnValueChanged(nameof(OnChangeGMType))]
        private GMConfig _generationParams;

        [BoxGroup(generationSettings)]
        [SerializeField]
        private uint _maxFailures = 10;

        [BoxGroup(testingSettings)]
        [Tooltip("Generate Arena on scene start automatically. (DANGEROUS)")]
        [SerializeField]
        private bool _autoCreate = false;

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
                _generationMethod = GenMethodManager.Instance.GetNameFromType(
                    _generationParams.GetType());
            }
        }

        // Callback invoked when user changes generation method name in editor
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

        // Create a new map
        public GameObject Create()
        {
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

            // Work on a copy and not in the original field, since we will sort
            // this list and we don't want this to be reflected in the editor
            _piecesForGenerationWorkList =
                new List<ArenaPiece>(_piecesList);

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
            if (_useStarter)
            {
                started = _chosenMethod.SelectStartPiece(
                    _startingPieceList, (int)_starterConTol);
            }
            else
            {
                started = _chosenMethod.SelectStartPiece(
                    _piecesForGenerationWorkList,
                    (int)_starterConTol);
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
                        _colorMatrix.GetCells());

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
                    lastConsidered - _starterConTol)
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
