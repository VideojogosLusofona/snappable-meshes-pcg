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
    /// The arena generation method aims to create maps that sprawl in
    /// all directions, covering a large area with geometry.
    /// </summary>
    public sealed class ArenaGM : AbstractGM
    {
        // Maximum number of pieces the method will use to create an arena
        private readonly int _maxPieces;

        // Index of current guide piece in placed pieces array
        private int _guideIndex;

        /// <summary>
        /// Creates a new arena generation method instance.
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
            IList<MapPiece> starterList, int starterConTol)
        {
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
        /// For the arena generation method, if the current guide piece has
        /// free connectors it remains the guide piece. Otherwise, the piece
        /// placed immediately after the current guide piece was placed in the
        /// map is selected as the next guide piece. This process continues
        /// until a maximum number of pieces has been placed in the map.
        /// </remarks>
        protected override MapPiece DoSelectGuidePiece(IList<MapPiece> piecesInMap)
        {
            // Select the guide piece to return
            if (piecesInMap.Count > _maxPieces)
            {
                // If we're over the maximum number of pieces, return null to
                // signal the end of the map generation
                return null;
            }
            else if (LastGuide.Full)
            {
                // If the current guide piece has no connectors left, select a
                // new guide piece which will be the piece placed after the
                // current guide piece was placed
                return piecesInMap[++_guideIndex];
            }
            else
            {
                // Otherwise, return the last returned guide piece
                return LastGuide;
            }
        }
    }
}