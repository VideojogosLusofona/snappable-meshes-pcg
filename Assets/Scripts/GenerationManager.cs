using System.Collections.Generic;
using UnityEngine;

namespace trinityGen
{

    public class GenerationManager : MonoBehaviour
    {
    


        [Header("----- Content Settings ------")]
        [SerializeField] private List<ArenaPiece> piecesForGeneration;

        [SerializeField] private bool defineSeed;
        [SerializeField] private int seed;


        [Header("Starting Piece Settings ---")]
        [SerializeField] private bool _setStartingPiece = false;
        [SerializeField] private List<ArenaPiece> _possibleStartingPieces;
        [SerializeField] private uint _connectorCountTolerance = 0;

        [Header("------ General Generation Settings --------")]

        [SerializeField] private GenerationTypes _generationMethod;
        [SerializeField] private uint _maxPieceCount;
        [SerializeField] private uint _pinCountTolerance = 0;

        
        [SerializeField] private bool _useClippingCorrection = false;
        [SerializeField] private float _pieceDistance = 0.0001f;

        [Header("Star & Branch Generation Settings --------")]
        [SerializeField] private uint _branchPieceCount;
        [SerializeField] private int _branchSizeVariance = 0;

        [Header("---")]
        [SerializeField] private uint _branchGenPieceSkipping = 0;
        [SerializeField] private uint _PieceSkippingVariance = 0;

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


        private List<ArenaPiece> _placedPieces;
        private int _currentSeed;


        private List<List<ArenaPiece>> _sortedPieces;


        /// Already placed piece being used to judge others
        private ArenaPiece _selectedPiece;

        /// Piece being evaluated against selectedPiece
        private ArenaPiece _evaluatingPiece;


        private int largestGroup;
        
        // Start is called before the first frame update
        void Awake()
        {
            if(_autoCreate)
            {
                Debug.Log("ATENTION: AUTO CREATE IS ON. TURN OFF FOR GAMEPLAY");
                Create();
            }
                

        }

        public void Create()
        {
            if(defineSeed)
                _currentSeed = seed;
            else
                _currentSeed = System.Environment.TickCount;
            
            Random.InitState(_currentSeed);
            _sortedPieces = new List<List<ArenaPiece>>();

            foreach (ArenaPiece a in piecesForGeneration)
                a.Setup(_useClippingCorrection);

            piecesForGeneration.Sort();
            largestGroup = 
                piecesForGeneration[0].ConnectorsCount;


            
            // Seperate pieces into seperate lists based on largest group
            _sortedPieces = SplitList();

            // Place the first piece
            PickFirstPiece();

            // Make base level of Arena and add those pieces to the list
            _placedPieces.AddRange(MakeHorizontalArena(_placedPieces[0]));

            Debug.Log("Generated with seed: " + _currentSeed);

            /*if(!_autoCreate)
                InitArenas();*/
        }

       /* private void InitArenas()
        {
            foreach (ArenaPiece piece in _placedPieces)
            {
                piece.Initialize();
            }
        }*/






        /// <summary>
        /// Populates a horizontal level of the arena
        /// </summary>
        /// <param name="startingPiece"> The first piece of the arena</param>
        /// <returns> The pieces placed during this method's process</returns>
        private List<ArenaPiece> MakeHorizontalArena(ArenaPiece startingPiece)
        {
            int placedAmount = 0;
            int jumpsTaken = 0;
            
            uint maxFailures = _maxPieceCount;
            // Pieces placed by this method call
            List<ArenaPiece> arena = new List<ArenaPiece>(); 
            //List<ArenaPiece> spawnedArena = new List<ArenaPiece>();

            // Check what list of the sorted list the selected belongs to
            int myPieceList = 0;

            for (int i = -1; i < arena.Count; i++)
            {
                int failureCount = 0;
                if(i == -1)
                    _selectedPiece = startingPiece;
                else
                {
                    _selectedPiece = arena[i];
                    //placedAmount++;
                }
                    
                // Pick a piece to evaluate against our selected placed one
                selectPiece:

                int rng;
                int wideRng = Random.Range(0,_sortedPieces.Count);
                myPieceList = wideRng;
                if(_sortedPieces[myPieceList].Count != 0)
                    rng = Random.Range(0,_sortedPieces[myPieceList].Count);
                else
                {
                    goto selectPiece;
                
                }

        
                _evaluatingPiece = _sortedPieces[myPieceList][rng];
                
                

                GameObject spawnedPiece = Instantiate(_evaluatingPiece).gameObject;
                ArenaPiece spawnedScript = spawnedPiece.GetComponent<ArenaPiece>();

                (bool valid, Transform trn) evaluationResult =
                    _selectedPiece.EvaluatePiece(spawnedScript, 
                    _pieceDistance, 
                    _pinCountTolerance);

                // If things worked out, spawn the piece in the correct position
                if(evaluationResult.valid)
                {
                    spawnedPiece.name += $" - {i}" ;
                    arena.Add(spawnedScript);
                    placedAmount++;
                    spawnedScript.gameObject.transform.SetParent(_selectedPiece.transform);

                    if(arena.Count >= _maxPieceCount)
                        return arena;

                }
                else
                {
                    if (failureCount > maxFailures)
                        continue;

                    // No valid connectors in the given piece
                    Destroy(spawnedPiece);
                    failureCount++;
                    
                    goto selectPiece;

                } 

                
                // if this one has no more free connectors, move on to the next 
                // placed piece
                switch(_generationMethod)
                {
                    case GenerationTypes.CORRIDOR:
                    continue;

                    // For some reason the first branch gets double pieces
                    case GenerationTypes.STAR:
                        int[] multi = new int[] {-1, 1};
                        int variance = 
                            multi[Random.Range(0,2)] * _branchSizeVariance;

                        if(placedAmount < _branchPieceCount + variance)
                        {
                            
                            continue;
                        }
                            

                        else if(placedAmount >= _branchPieceCount + variance &&
                                !startingPiece.IsFull())
                        {
                            //print($"placed: {placedAmount}, arena: {arena.Count}");
                            placedAmount = 0;
                            _selectedPiece = startingPiece;

                            // it works dont ask me why
                            i += 1;
                            //print($"Selected piece is now {_selectedPiece.gameObject} - index {i}");
                            goto selectPiece;
                        }
                        return arena;

                    case GenerationTypes.BRANCH:
                        int[] mult = new int[] {-1, 1};
                        int multiplier =  
                            mult[Random.Range(0,2)] * _branchSizeVariance;

                        if(placedAmount < _branchPieceCount + multiplier)
                            continue;
                        else if(placedAmount > _branchPieceCount + multiplier)
                        {
                            int variableVariance;
                            variableVariance =mult[(int)Random.Range(0,
                                _PieceSkippingVariance + 1)];

                            int dist = 
                            (int)_branchGenPieceSkipping + variableVariance * jumpsTaken;

                            int jump = (int)Mathf.Clamp(dist,
                            1, arena.Count - 1);

                            if(!arena[0 + jump].IsFull())
                            {
                                //print($"{jumpsTaken} - {jump}");
                                placedAmount = 0;
                                //_selectedPiece = arena[jump];
                                i =  jump;
                                //i = Mathf.Clamp(i, 0, arena.Count - 1);
                                jumpsTaken++;
                                continue;

                            }
                                
                        }
                        break;
                }


                if(_selectedPiece.IsFull())
                    continue;
                else // else choose another piece to evaluate for this one
                    goto selectPiece;
            }

            return arena;
        }

        /// <summary>
        /// Select and place the first piece of the arena
        /// </summary>
        private void PickFirstPiece()
        {
            // To be safe if the starters list fails
            chooseStarter:

            ArenaPiece choosen = null;
            
            
            int choosenIndex = 0;
            _placedPieces = new List<ArenaPiece>();

            if (_setStartingPiece)
            {
                if(_possibleStartingPieces.Count == 0)
                {
                    Debug.LogError(
                        "'Set starting piece' is on but no ArenaPieces were given");
                    _setStartingPiece = false;
                    goto chooseStarter;
                }

                choosenIndex = Random.Range(0, _possibleStartingPieces.Count);
                choosen =     _possibleStartingPieces[choosenIndex];    
        
            }
            else
            {
                
                if(_generationMethod == GenerationTypes.CORRIDOR ||
                _generationMethod == GenerationTypes.BRANCH)
                {

                    choosenIndex = Random.Range(
                        0, _sortedPieces[_sortedPieces.Count - 1].Count);
                        
                    choosen = _sortedPieces[_sortedPieces.Count - 1][choosenIndex];

                }
                    
                else
                {

                    choosenIndex = Random.Range(0, _sortedPieces[0].Count);
                    choosen = _sortedPieces[0][choosenIndex];


                }
                    
            }
            GameObject piece = Instantiate(choosen.gameObject);
            _placedPieces.Add(piece.GetComponent<ArenaPiece>());
            
        }

        /// <summary>
        /// Seperate pieces into seperate lists based on largest group
        /// </summary>
        private List<List<ArenaPiece>> SplitList()
        {
            int lastConsidered = largestGroup + 1;
            List<ArenaPiece> considererdList = new List<ArenaPiece>();
            List<List<ArenaPiece>> sortedList = new List<List<ArenaPiece>>();

            
            for (int i = 0; i < piecesForGeneration.Count; i++)
            {
                // Piece belongs in a new list made for its size            
                if (piecesForGeneration[i].ConnectorsCount < lastConsidered)
                {
                    considererdList = new List<ArenaPiece>();
                    considererdList.Add(piecesForGeneration[i]);
                    lastConsidered = piecesForGeneration[i].ConnectorsCount;
                    sortedList.Add(considererdList);

                }
                // piece belongs in the already made list
                else if (piecesForGeneration[i].ConnectorsCount >= 
                lastConsidered - _connectorCountTolerance)
                {

                    considererdList.Add(piecesForGeneration[i]);

                }

            }

            return sortedList;
        }
    }
}

