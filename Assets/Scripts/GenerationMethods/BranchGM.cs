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

namespace TrinityGen.GenerationMethods
{
    public sealed class BranchGM : GenerationMethod
    {
        private readonly int maxBranches;
        private readonly int branchLength;
        private readonly int branchLengthVariance;
        private readonly int pieceJumpSize;

        private int _branchesMade;
        private int _currentBranchLength;
        private int _currentBranchPlaced;

        public BranchGM(int maxBranches, int branchLength,
            int branchLengthVariance, int pieceJumpSize)
        {
            this.maxBranches = maxBranches;
            this.branchLength = branchLength;
            this.branchLengthVariance = branchLengthVariance;
            this.pieceJumpSize = pieceJumpSize;

            this.pieceJumpSize = branchLength / maxBranches;
        }

        public override ArenaPiece SelectStartPiece(
            List<ArenaPiece> starterList, int starterConTol = 0)
        {
            // Assumes that the list is sorted by number of connectors where
            // [0] is the index with most connectors
            int botConnectorCount =
                starterList[starterList.Count - 1].ConnectorsCount;

            int maximumAllowed = botConnectorCount + starterConTol;
            List<ArenaPiece> possibles = new List<ArenaPiece>();
            foreach(ArenaPiece g in starterList)
            {
                if(g.ConnectorsCount <= maximumAllowed)
                    possibles.Add(g);
            }

            int rng = UnityEngine.Random.Range(0, possibles.Count - 1);
            // Upper limit is exclusive
            ArenaPiece chosen = possibles[rng];
            if (_firstPiece == null)
                _firstPiece = chosen;

            StartBranch();
            _lastGuideSelected = chosen;
            return chosen;
        }

        public override ArenaPiece SelectGuidePiece(
            List<ArenaPiece> worldPieceList, ArenaPiece lastPlaced)
        {
            ArenaPiece chosen;
            //Random rng = new Random();

            if(_branchesMade > maxBranches)
                return null;

            if(_currentBranchPlaced > _currentBranchLength)
            {
                int boundJump = pieceJumpSize * _branchesMade;

                // clamp the jump size to within list size
                boundJump = (boundJump > worldPieceList.Count - 1)?
                    worldPieceList.Count - 1 : boundJump;

                // Select what piece to jump to
                chosen = worldPieceList[boundJump];

                if(chosen.IsFull())
                {
                    int index =
                        worldPieceList.FindIndex(
                            a => a.gameObject.name == chosen.gameObject.name)
                        + pieceJumpSize;
                    if (index < worldPieceList.Count)
                        chosen = worldPieceList[index];
                    else
                        return null;
                }

                // Start a new branch from there
                StartBranch();

                _lastGuideSelected = chosen;
                return chosen;

            }

            _lastGuideSelected = lastPlaced;
            _currentBranchPlaced++;
            return _lastGuideSelected;

        }

        public void StartBranch()
        {
            int rng = UnityEngine.Random.Range(0, branchLengthVariance + 1);
            int chosenVar = rng;
            int[] mults = {-1, 1};
            rng = UnityEngine.Random.Range(0, mults.Length);
            int chosenMult = mults[rng];
            _currentBranchLength = branchLength + (chosenVar * chosenMult);
            _currentBranchPlaced = 0;
            _branchesMade ++;
        }
    }
}