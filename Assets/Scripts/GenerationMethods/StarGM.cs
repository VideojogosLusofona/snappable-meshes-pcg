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
    /// The star generation method is a mix of the arena and corridor generation
    /// methods, creating corridors sprawling from the starting piece and ending
    /// when that piece has no empty connectors.
    /// </summary>
    public sealed class StarGM : AbstractGM
    {
        // The base amount of pieces an arm of the star will have
        private readonly int _armLength;
        // The maximum variation from _armLength in each arm
        private readonly int _armLengthVar;

        // Length of current arm
        private int _currArmLength;
        // Maximum length for current arm
        private int _maxArmLength;

        /// <summary>
        /// Creates a new star generation method instance.
        /// </summary>
        /// <param name="armLength">
        /// The base amount of pieces an arm of the star will have.
        /// </param>
        /// <param name="armLengthVar">
        /// The maximum variation from <paramref name="_armLength"/> in each arm.
        /// </param>
        public StarGM(uint armLength, uint armLengthVar)
        {
            _armLength = (int)armLength;
            _armLengthVar = (int)armLengthVar;
        }

        /// <summary>Select the starting piece.</summary>
        /// <param name="starterList">
        /// List where to get the starting piece from. This list is assumed to
        /// be sorted in descending order by number of connectors.
        /// </param>
        /// <param name="starterConTol">Connector count tolerance.</param>
        /// <returns>The starting piece.</returns>
        /// <remarks>
        /// For the star generation method, the piece with most connectors is
        /// selected. If there are multiple pieces with the same highest number
        /// of connectors, one of them is selected at random.
        /// </remarks>
        protected override MapPiece DoSelectStartPiece(
            List<MapPiece> starterList, int starterConTol)
        {
            NewArm();
            return Helpers.GetPieceWithMostConnectors(starterList, starterConTol);
        }

        /// <summary>
        /// Selects the next guide piece according to the generation method.
        /// </summary>
        /// <param name="piecesInMap">Pieces already place in the map.</param>
        /// <returns>
        /// The next guide piece or null if the generation is finished.
        /// </returns>
        /// <remarks>
        /// For the star generation method the starting piece acts as the
        /// central star hub. For each connector it has, an arm is grown, with
        /// a minimum of one and a maximum of maxArmLength pieces. Therefore,
        /// when a new arm begins to form, the value of maxArmLength is
        /// randomly obtained from the interval armLength±armLengthVar. If the
        /// length of the current arm is less than maxArmLength, the last placed
        /// piece is returned as the guide piece in order to keep growing the
        /// arm. Otherwise, the starting piece is returned as the guide piece,
        /// in order to start a new arm in one of the remaining free connectors.
        /// </remarks>
        protected override MapPiece DoSelectGuidePiece(List<MapPiece> piecesInMap)
        {
            // Last piece placed in the map
            MapPiece lastPlaced = piecesInMap.Last();

            // Assume the last piece was placed successfully (that may not have
            // been the case) and grow the size of the arm
            _currArmLength++;

            // Determine the guide piece to return
            if (_currArmLength >= _maxArmLength || lastPlaced.IsFull())
            {
                // If we reached the maximum arm size or the last placed piece
                // is full, start a new arm and return the starting piece
                NewArm();
                return piecesInMap[0].IsFull() ? null : piecesInMap[0];
            }
            else
            {
                // The arm can still grow, return the last placed piece
                return lastPlaced;
            }
        }

        // Helper method for starting a new arm
        private void NewArm()
        {
            // Set new arm length to zero
           _currArmLength = 0;

           // Set the maximum arm length; although this value can be zero or
           // negative, arms will in practice have a minimum size of 1, due to
           // the way the algorithm works
           _maxArmLength = Random.Range(
               _armLength - _armLengthVar, _armLength + _armLengthVar + 1);
        }
    }
}