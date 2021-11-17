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

namespace SnapMeshPCG.SelectionMethods
{
    /// <summary>
    /// The corridor selection method aims to create long, narrow maps
    /// where the geometry seemingly follows a line.
    /// </summary>
    public sealed class CorridorSM : AbstractSM
    {
        // Maximum number of pieces the method will use to create a
        // corridor-like map
        private readonly int _maxPieces;

        /// <summary>
        /// Creates a new corridor selection method instance.
        /// </summary>
        /// <param name="maxPieces">
        /// Maximum number of pieces the method will use to create a map.
        /// </param>
        public CorridorSM(int maxPieces)
        {
            _maxPieces = maxPieces;
        }

        /// <summary>Select the starting piece.</summary>
        /// <param name="starterList">
        /// List where to get the starting piece from. This list is assumed to
        /// be sorted in descending order by number of connectors.
        /// </param>
        /// <param name="starterConTol">Connector count tolerance.</param>
        /// <returns>The starting piece.</returns>
        /// <remarks>
        /// For the corridor selection method, the piece with less connectors
        /// is selected. If there are multiple pieces with the same lowest
        /// number of connectors, one of them is selected at random.
        /// </remarks>
        protected override MapPiece DoSelectStartPiece(
            IList<MapPiece> starterList, int starterConTol)
        {
            return Helpers.GetPieceWithLessConnectors(starterList, starterConTol);
        }

        /// <summary>
        /// Selects the next guide piece according to the selection method.
        /// </summary>
        /// <param name="piecesInMap">Pieces already place in the map.</param>
        /// <returns>
        /// The next guide piece or null if the generation is finished.
        /// </returns>
        /// <remarks>
        /// For the arena selection method, the last placed piece is always
        /// returned as the guide piece. This process continues until a maximum
        /// number of pieces has been placed in the map.
        /// </remarks>
        protected override MapPiece DoSelectGuidePiece(IList<MapPiece> piecesInMap)
        {
            return piecesInMap.Count < _maxPieces ? piecesInMap.Last() : null;
        }
    }
}