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

using System.Linq;
using System.Collections.Generic;
using UnityEngine;

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
        // Number of branches to be created
        private readonly int _branchCount;

        // The average amount of pieces a branch will have
        private readonly int _branchLength;

        // The maximum variation from branchLength in each branch
        private readonly int _branchLengthVar;

        // Base jump size from starting piece when creating new branches
        private readonly int _baseJumpSize;

        // Maximum length (e.g. number of pieces) for current branch
        private int _maxBranchLength;

        // How many branches have been created so far
        private int _branchesCreated;

        // Length (e.g. number of pieces) of current branch
        private int _currBranchLength;

        /// <summary>
        /// Creates a new branch generation method instance.
        /// </summary>
        /// <param name="branchCount">Number of branches to create.</param>
        /// <param name="branchLength">
        /// Average number of pieces a branch will have.
        /// </param>
        /// <param name="branchLengthVar">
        /// The maximum variation from <paramref name="branchLength"/> in each
        /// branch.
        /// </param>
        public BranchGM(uint branchCount, uint branchLength, uint branchLengthVar)
        {
            // Keep parameters in instance variables
            _branchCount = (int)branchCount;
            _branchLength = (int)branchLength;
            _branchLengthVar = (int)branchLengthVar;

            // Determine base jump size from the starting piece
            _baseJumpSize = _branchLength / _branchCount;

            // If branch count is higher than branch length, jump size will be
            // zero, which makes little sense; thus, we set the minimum jump to
            // one piece
            if (_baseJumpSize == 0) _baseJumpSize = 1;
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
            IList<MapPiece> starterList, int starterConTol)
        {
            NewBranch();
            return Helpers.GetPieceWithLessConnectors(starterList, starterConTol);
        }

        /// <summary>
        /// Selects the next guide piece according to the generation method.
        /// </summary>
        /// <param name="piecesInMap">Pieces already place in the map.</param>
        /// <returns>
        /// The next guide piece or null if the generation is finished.
        /// </returns>
        /// <remarks>
        /// For the branch generation method, the next guide piece is returned
        /// as the guide piece if the current branch is still growing.
        /// Otherwise, another piece in the map is returned as the guide piece,
        /// effectively creating another branch,
        /// </remarks>
        protected override MapPiece DoSelectGuidePiece(IList<MapPiece> piecesInMap)
        {
            // Select the next guide piece based on the current scenario
            if (_currBranchLength >= _maxBranchLength)
            {
                // If we've reached the maximum size for the current branch,
                // let's create a new branch

                // Did we reach the branch count limit? If so return null and
                // end the generation process
                if (_branchesCreated >= _branchCount) return null;

                // Determine the next jump size
                int jump = _baseJumpSize * _branchesCreated;

                // This variable will contain the final jump size after a number
                // of checks
                int finalJump;

                // If the jump takes us out of the placed pieces list, chose a
                // random location within the last section of the list
                if (jump >= piecesInMap.Count)
                {
                    jump = Random.Range(
                        piecesInMap.Count - _baseJumpSize, piecesInMap.Count);

                    // This should not happen but make sure we don't have a
                    // negative index
                    if (jump < 0) jump = 0;
                }

                // Set the final jump value equal to jump
                finalJump = jump;

                // If the piece at the jump position is full, search for a
                // nearby piece that has available connectors
                // We search at most a number pieces equal to the base jump size
                for (int i = 2, j = 1;
                    i < _baseJumpSize + 2 && piecesInMap[finalJump].Full;
                    i++, j *= -1)
                {
                    // We search upwards and backwards, advancing one piece at
                    // a time on either side of the piece list
                    finalJump = jump + (i / 2 * j);

                    // Clamp the final jump value to valid values
                    if (finalJump < 0)
                        finalJump = 0;
                    if (finalJump >= piecesInMap.Count)
                        finalJump = piecesInMap.Count - 1;
                }

                // Start a new branch from there
                NewBranch();

                // Return the selected map piece after the jump; if it's full
                // even after our attempts to find a non-full piece, then
                // return null and end map generation
                return piecesInMap[finalJump].Full
                    ? null
                    : piecesInMap[finalJump];
            }
            else
            {
                // If we get here it means our branch is still growing, so
                // we'll increment the current branch length and return the
                // last placed piece as the guide piece
                _currBranchLength++;
                return piecesInMap.Last();
            }
        }

        // Helper method for starting a new branch
        private void NewBranch()
        {
            // Set new branch length to zero
            _currBranchLength = 0;

            // Set the maximum branch length; although this value can be zero or
            // negative, branches will in practice have a minimum size of 1, due
            // to the way the algorithm works
            _maxBranchLength = Random.Range(
                _branchLength - _branchLengthVar,
                _branchLength + _branchLengthVar + 1);

            // Increment number of created branches
            _branchesCreated++;
        }
    }
}