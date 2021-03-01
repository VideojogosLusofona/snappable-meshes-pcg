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

namespace SnapMeshPCG.GenerationMethods
{
    public sealed class BranchGM : AbstractGM
    {
        private readonly int _maxBranches;
        private readonly int _branchLength;
        private readonly int _branchLengthVariance;
        private readonly int _pieceJumpSize;

        private int _branchesMade;
        private int _currentBranchLength;
        private int _currentBranchPlaced;

        public BranchGM(int maxBranches, int branchLength,
            int branchLengthVariance, int pieceJumpSize)
        {
            _maxBranches = maxBranches;
            _branchLength = branchLength;
            _branchLengthVariance = branchLengthVariance;
            _pieceJumpSize = pieceJumpSize;

            _pieceJumpSize = branchLength / maxBranches;
        }

        protected override MapPiece DoSelectStartPiece(
            List<MapPiece> starterList, int starterConTol = 0)
        {
            // Assumes that the list is sorted by number of connectors where
            // [0] is the index with most connectors
            int botConnectorCount =
                starterList[starterList.Count - 1].ConnectorCount;

            int maximumAllowed = botConnectorCount + starterConTol;
            List<MapPiece> possibles = new List<MapPiece>();
            foreach(MapPiece g in starterList)
            {
                if(g.ConnectorCount <= maximumAllowed)
                    possibles.Add(g);
            }

            int rng = UnityEngine.Random.Range(0, possibles.Count - 1);
            // Upper limit is exclusive
            MapPiece chosen = possibles[rng];
            if (_firstPiece == null)
                _firstPiece = chosen;

            StartBranch();
            _lastGuideSelected = chosen;
            return chosen;
        }

        protected override MapPiece DoSelectGuidePiece(
            List<MapPiece> piecesInMap, MapPiece lastPlaced)
        {
            MapPiece chosen;
            //Random rng = new Random();

            if(_branchesMade > _maxBranches)
                return null;

            if(_currentBranchPlaced > _currentBranchLength)
            {
                int boundJump = _pieceJumpSize * _branchesMade;

                // clamp the jump size to within list size
                boundJump = (boundJump > piecesInMap.Count - 1)?
                    piecesInMap.Count - 1 : boundJump;

                // Select what piece to jump to
                chosen = piecesInMap[boundJump];

                if(chosen.IsFull())
                {
                    int index =
                        piecesInMap.FindIndex(
                            a => a.gameObject.name == chosen.gameObject.name)
                        + _pieceJumpSize;
                    if (index < piecesInMap.Count)
                        chosen = piecesInMap[index];
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
            int rng = UnityEngine.Random.Range(0, _branchLengthVariance + 1);
            int chosenVar = rng;
            int[] mults = {-1, 1};
            rng = UnityEngine.Random.Range(0, mults.Length);
            int chosenMult = mults[rng];
            _currentBranchLength = _branchLength + (chosenVar * chosenMult);
            _currentBranchPlaced = 0;
            _branchesMade++;
        }
    }
}