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
     /// <summary>
    /// The branch generation method creates branches in the same manner as the
    /// star method. However it does not return to the starting piece, choosing
    /// instead a previously placed piece to start a new branch, repeating this
    /// until there are no more available pieces or branchLength as been
    /// reached.
    /// </summary>
   public sealed class BranchGM : AbstractGM
    {
        private readonly int _maxBranches;
        private readonly int _branchLength;
        private readonly int _branchLengthVar;
        private readonly int _pieceJumpSize;

        private int _branchesMade;
        private int _currentBranchLength;
        private int _currentBranchPlaced;

        public BranchGM(uint maxBranches, uint branchLength, uint branchLengthVar)
        {
            _maxBranches = (int)maxBranches;
            _branchLength = (int)branchLength;
            _branchLengthVar = (int)branchLengthVar;

            _pieceJumpSize = _branchLength / _maxBranches;
        }

        /// <summary>Select the starting piece.</summary>
        /// <param name="starterList">
        /// List where to get the starting piece from. This list is assumed to
        /// be sorted in descending order by number of connectors.
        /// </param>
        /// <param name="starterConTol">Connector count tolerance.</param>
        /// <returns>The starting piece.</returns>
        /// <remarks>
        /// For the branch generation method, the piece with less connectors
        /// is selected. If there are multiple pieces with the same lowest
        /// number of connectors, one of them is selected at random.
        /// </remarks>
        protected override MapPiece DoSelectStartPiece(
            List<MapPiece> starterList, int starterConTol)
        {
            return Helpers.GetPieceWithLessConnectors(starterList, starterConTol);
        }


        protected override MapPiece DoSelectGuidePiece(
            List<MapPiece> piecesInMap, MapPiece lastPlaced)
        {
            MapPiece chosen;

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

                return chosen;
            }

            _currentBranchPlaced++;
            return lastPlaced;
        }

        public void StartBranch()
        {
            int rng = UnityEngine.Random.Range(0, _branchLengthVar + 1);
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