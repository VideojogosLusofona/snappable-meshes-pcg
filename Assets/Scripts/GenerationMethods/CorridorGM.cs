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
using UnityEngine;

namespace SnapMeshPCG.GenerationMethods
{
    /// <summary>
    /// The corridor generation method aims to create long, narrow maps
    /// where the geometry seemingly follows a line.
    /// </summary>
    public sealed class CorridorGM : AbstractGM
    {
        // Maximum number of pieces the method will use to create a
        // corridor-like map
        private readonly int _maxPieces;

        /// <summary>
        /// Creates a new corridor generation method instance.
        /// </summary>
        /// <param name="maxPieces">
        /// Maximum number of pieces the method will use to create a map.
        /// </param>
        public CorridorGM(int maxPieces)
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
        /// For the corridor generation method, the piece with less connectors
        /// is selected. If there are multiple pieces with the same lowest
        /// number of connectors, one of them is selected at random.
        /// </remarks>
        protected override MapPiece DoSelectStartPiece(
            List<MapPiece> starterList, int starterConTol = 0)
        {
            // Index of selected starting piece in starting piece list
            int startingPieceIndex;

            // Get the number of connectors in the piece with the least
            // connectors
            int minConnectorCount =
                starterList[starterList.Count - 1].ConnectorCount;

            // Determine the maximum amount of connectors a piece may have in
            // order to be selected as the starting piece
            int maxAllowed = minConnectorCount + starterConTol;

            // Determine index of piece with the maximum allowed number of
            // connectors
            int maxAllowedIndex = starterList.Count - 1;
            for (int i = maxAllowedIndex - 1; i >= 0; i--)
            {
                if (starterList[i].ConnectorCount <= maxAllowed)
                    maxAllowedIndex = i;
                else
                    break;
            }

            // Get the index of the starting piece
            startingPieceIndex = Random.Range(maxAllowedIndex, starterList.Count);

            // Return the starting piece
            return starterList[startingPieceIndex];
        }

        /// <summary>
        /// Selects the next guide piece according to the generation method.
        /// </summary>
        /// <param name="piecesInMap">Pieces already place in the map.</param>
        /// <param name="lastPlaced">Last piece placed in the map.</param>
        /// <returns>
        /// The next guide piece or null if the generation is finished.
        /// </returns>
        /// <remarks>
        /// For the arena generation method, the last placed piece is always
        /// returned as the guide piece. This process continues until a maximum
        /// number of pieces has been placed in the map.
        /// </remarks>
        protected override MapPiece DoSelectGuidePiece(
            List<MapPiece> piecesInMap, MapPiece lastPlaced)
        {
            return piecesInMap.Count < _maxPieces ? lastPlaced : null;
        }
    }
}