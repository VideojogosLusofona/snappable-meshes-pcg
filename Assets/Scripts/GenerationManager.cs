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

using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using TrinityGen.GenerationMethods;

namespace TrinityGen
{
    public class GenerationManager : MonoBehaviour
    {
        [Header("----- Content Settings ------")]

        [SerializeField] private List<ArenaPiece> piecesForGeneration;

        [Header("Starting Piece Settings ---")]

        [SerializeField] private bool _setStartingPiece = false;
        [SerializeField] private List<ArenaPiece> _possibleStartingPieces;
        [SerializeField] private uint _connectorCountTolerance = 0;

        [Header("------ World Settings --------")]

        [SerializeField] private bool defineSeed;
        [SerializeField] private int seed;
        [SerializeField] private bool _useClippingCorrection = false;
        [SerializeField] private float _pieceDistance = 0.0001f;

        [Header("------ Connection Settings --------")]

        [SerializeField] private ConnectorMatchingRules _matchingRules;
        [SerializeField] private uint _pinCountTolerance = 0;

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
        [SerializeField]
        private bool[,] _colorMatchMatrix = {
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
        };

        [Header("----------- Generation Settings --------")]

        [SerializeField] private GenerationTypes _generationMethod;

        [Header("Arena & Corridor Generation Settings --------")]
        [SerializeField] private uint _maxPieceCount;

        [Header("Star Generation Settings --------")]
        [SerializeField] private uint _spokePieceCount;
        [SerializeField] private int _spokeSizeVariance = 0;

        [Header("Branch Generation Settings --------")]
        [SerializeField] private uint _branchCount;
        [SerializeField] private uint _branchPieceCount;
        [SerializeField] private int _branchSizeVariance = 0;
        [SerializeField] private uint PieceSkippingVariance = 0;

        [Header("------ Testing Settings -------")]

        [Tooltip("Generate Arena on scene start automaticly. (DANGEROUS)")]
        [SerializeField] private bool _autoCreate = false;

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

        // Dont expose this to have branch calculate the jumping
        private uint _branchGenPieceSkipping = 0;

        private List<ArenaPiece> _placedPieces;
        private int _currentSeed;
        private IList<List<ArenaPiece>> _sortedPieces;

        // Already placed piece being used to judge others
        private ArenaPiece _guidePiece;

        // Piece being evaluated against selectedPiece
        private ArenaPiece _tentativePiece;

        private GenerationMethod _chosenMethod;

        private int largestGroup;

        // Start is called before the first frame update
        private void Start()
        {
            if (_autoCreate)
            {
                Debug.Log("ATENTION: AUTO CREATE IS ON. TURN OFF FOR GAMEPLAY");
                ClearEditorGeneration();
                Create();
            }
        }

        public GameObject Create()
        {
            if (defineSeed)
                _currentSeed = seed;
            else
                _currentSeed = System.Environment.TickCount;

            Random.InitState(_currentSeed);
            _sortedPieces = new List<List<ArenaPiece>>();

            foreach (ArenaPiece a in piecesForGeneration)
                a.Setup(_useClippingCorrection);

            switch (_generationMethod)
            {
                case GenerationTypes.ARENA:
                    _chosenMethod = new ArenaGM((int)_maxPieceCount);
                    break;
                case GenerationTypes.CORRIDOR:
                    _chosenMethod = new CorridorGM((int)_maxPieceCount);
                    break;
                case GenerationTypes.STAR:
                    _chosenMethod = new StarGM(
                        (int)_spokePieceCount, (int)_spokeSizeVariance);
                    break;
                case GenerationTypes.BRANCH:
                    _chosenMethod = new BranchGM(
                        (int)_branchCount, (int)_branchPieceCount,
                        _branchSizeVariance, (int)_branchGenPieceSkipping);
                    break;
            }

            piecesForGeneration.Sort();
            largestGroup =
                piecesForGeneration[0].ConnectorsCount;

            // Seperate pieces into seperate lists based on largest group
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
                    piecesForGeneration, (int)_connectorCountTolerance);
            }

            GameObject inst = Instantiate(started.gameObject);
            _placedPieces.Add(inst.GetComponent<ArenaPiece>());
            inst.name += " - START ";

            GameObject initialPiece = inst;
            _guidePiece = _placedPieces[0];

            // Make base level of Arena and add those pieces to the list
            int placement = 0;
            do
            {

                int maxFailures = 10;
                int failureCount = 0;

                // Check what list of the sorted list the selected belongs to
                int myPieceList = 0;

                // Pick a piece to evaluate against our selected placed one
                while (true)
                {
                    int rng;
                    myPieceList = Random.Range(0, _sortedPieces.Count);
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
                        Instantiate(_tentativePiece).gameObject;
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
                        if (failureCount > maxFailures)
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
            int lastConsidered = largestGroup + 1;
            List<ArenaPiece> consideredList = new List<ArenaPiece>();
            List<List<ArenaPiece>> sortedList = new List<List<ArenaPiece>>();


            for (int i = 0; i < piecesForGeneration.Count; i++)
            {
                // Piece belongs in a new list made for its size
                if (piecesForGeneration[i].ConnectorsCount < lastConsidered)
                {
                    consideredList = new List<ArenaPiece>();
                    consideredList.Add(piecesForGeneration[i]);
                    lastConsidered = piecesForGeneration[i].ConnectorsCount;
                    sortedList.Add(consideredList);

                }
                // piece belongs in the already made list
                else if (piecesForGeneration[i].ConnectorsCount >=
                lastConsidered - _connectorCountTolerance)
                {
                    consideredList.Add(piecesForGeneration[i]);
                }
            }

            return sortedList;
        }

        [Button("Generate")]
        private void EditorGenerate()
        {
            ClearEditorGeneration();

            var initialPiece = Create();

            // Add a component to identify this case, so we can delete it on a
            // new generation
            initialPiece.AddComponent<EditorGenerationPiece>();
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
            foreach (var obj in editorGenerations)
            {
                DestroyImmediate(obj.gameObject);
            }
        }
    }
}
