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
    /// The arena generation method.
    /// </summary>
    public sealed class ArenaGM : AbstractGM
    {
        // Maximum number of pieces the method will use to create an arena
        private readonly int _maxPieces;

        // Index of current guide piece in placed pieces array
        private int _guideIndex;

        /// <summary>
        /// Creates a new arena generation method.
        /// </summary>
        /// <param name="maxPieces">
        /// Maximum number of pieces the method will use to create an arena.
        /// </param>
        public ArenaGM(int maxPieces)
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
        /// For the arena generation method, the piece with most connectors is
        /// selected. If there are multiple pieces with the same highest number
        /// of connectors, one of them is selected at random.
        /// </remarks>
        protected override MapPiece DoSelectStartPiece(
            List<MapPiece> starterList, int starterConTol = 0)
        {
            // Index of selected starting piece in starting piece list
            int startingPieceIndex;

            // Get the number of connectors in the piece with the most
            // connectors
            int maxConnectorCount = starterList[0].ConnectorCount;

            // Determine the minimum amount of connectors a piece must have in
            // order to be selected as the starting piece
            int minAllowed = maxConnectorCount - starterConTol;

            // Determine index of piece with the minimum allowed number of
            // connectors
            int minAllowedIndex = 0;
            for (int i = 1; i < starterList.Count; i++)
            {
                if (starterList[i].ConnectorCount >= minAllowed)
                    minAllowedIndex = i;
                else
                    break;
            }

            // Get the index of the starting piece
            startingPieceIndex = Random.Range(0, minAllowedIndex + 1);

            // Return the starting piece
            return starterList[startingPieceIndex];
        }

        /// <summary>
        /// Selects the next guide piece according to the generation method.
        /// </summary>
        /// <param name="piecesInMap">Placed Geometry to select Guide from
        /// </param>
        /// <param name="lastPlaced">Last successfully placed geometry</param>
        /// <returns>
        /// The next guide piece or null if the generation is finished.
        /// </returns>
        /// <remarks>
        /// For the arena generation method, if the current guide piece has
        /// free connectors it remains the guide piece. Otherwise, the piece
        /// placed immediately after the current guide piece was placed in the
        /// map is selected as the next guide piece.
        /// </remarks>
        protected override MapPiece DoSelectGuidePiece(
            List<MapPiece> piecesInMap, MapPiece lastPlaced)
        {
            // The guide piece to return
            MapPiece guidePiece;

            // Select the guide piece to return
            if (piecesInMap.Count > _maxPieces)
            {
                // If we're over the maximum number of pieces, return null to
                // signal the end of the map generation
                guidePiece = null;
            }
            else if (LastGuide.IsFull())
            {
                // If the current guide piece has no connectors left, select a
                // new guide piece which will be the piece placed after the
                // current guide piece was placed
                guidePiece = piecesInMap[++_guideIndex];
            }
            else
            {
                // Otherwise, return the last returned guide piece
                guidePiece = LastGuide;
            }

            // Return the selected guide piece
            return guidePiece;
        }
    }
}