using System.Collections.Generic;
using System;

namespace trinityGen
{
    [System.Serializable]
    public sealed class BranchGM : GenerationMethod
    {

        public int maxBranches;
        public int branchLength;
        public int branchLengthVariance;
        public int pieceJumpSize;

        private int _branchesMade;
        private int _currentBranchLength;
        private int _currentBranchPlaced;


        public override ArenaPiece SelectStartPiece(List<ArenaPiece> starterList, int starterConTol = 0)
        {
            // Assumes that the list is sorted by number of connectors where 
            // [0] is the index with least connectors
            int botConnectorCount = starterList[0].ConnectorsCount;

            int maximumAllowed = botConnectorCount + starterConTol;
            List<ArenaPiece> possibles = new List<ArenaPiece>();
            foreach(ArenaPiece g in starterList)
            {
                if(g.ConnectorsCount <= maximumAllowed)
                    possibles.Add(g);

            }

            Random rng = new Random();
            // Upper limit is exclusive
            ArenaPiece chosen = possibles[rng.Next(starterList.Count)];
            if(_firstPiece == null)
                _firstPiece = chosen;

            StartBranch();
            _lastGuideSelected = chosen;
            return chosen;
        }

        public override ArenaPiece SelectGuidePiece(List<ArenaPiece> worldPieceList, ArenaPiece lastPlaced)
        {
            ArenaPiece chosen;
            Random rng = new Random();

            if(_branchesMade >= maxBranches)
                return null;

            if(_currentBranchPlaced >= _currentBranchLength)
            {
                int boundJump = pieceJumpSize * _branchesMade;

                // clamp the jump size to within list size
                boundJump = (boundJump > worldPieceList.Count - 1)?
                    worldPieceList.Count - 1 : boundJump;

                // Select what piece to jump to
                chosen = worldPieceList[boundJump];

                if(chosen.IsFull())
                {
                    chosen = worldPieceList[rng.Next(1, _currentBranchLength)];

                }


                // Start a new branch from there
                StartBranch();
                
                _lastGuideSelected = chosen;
                return chosen;


            }
            
            _lastGuideSelected = lastPlaced;
            return _lastGuideSelected;
                

            
        }

        public void StartBranch()
        {
            Random rng = new Random();
            int chosenVar = rng.Next(0, branchLengthVariance + 1);
            int[] mults = {-1, 1};
            int chosenMult = mults[rng.Next(0, mults.Length)];
            _currentBranchLength = branchLength + (chosenVar * chosenMult);
            _currentBranchPlaced = 0;
            _branchesMade ++;

        }



        
    }
}