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
    public abstract class AbstractGM
    {
        protected MapPiece _firstPiece;
        protected MapPiece _lastGuideSelected;

        private MapPiece _lastGuide;
        private int _lastGuideFreeConnectors;

        public abstract MapPiece SelectStartPiece(
            List<MapPiece> starterList, int starterConTol = 0);

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
        /// Concrete generation methods must override this method in order to
        /// define how to select the next guide piece.
        /// </remarks>
        protected abstract MapPiece DoSelectGuidePiece(
            List<MapPiece> piecesInMap, MapPiece lastPlaced);

        /// <summary>
        /// Selects the next guide piece according to the generation method.
        /// </summary>
        /// <param name="piecesInMap">Pieces already place in the map.</param>
        /// <param name="lastPlaced">Last piece placed in the map.</param>
        /// <returns>
        /// The next guide piece or null if the generation is finished.
        /// </returns>
        public MapPiece SelectGuidePiece(
            List<MapPiece> piecesInMap, MapPiece lastPlaced)
        {
            // Invoke the concrete method for selecting the next guide piece.
            MapPiece newGuide = DoSelectGuidePiece(piecesInMap, lastPlaced);

            // Check the guide piece returned by the concrete generation method
            if (newGuide != _lastGuide)
            {
                // If it's a different piece, update our cached information
                // accordingly
                _lastGuide = newGuide;
                _lastGuideFreeConnectors = newGuide?.FreeConnectorCount ?? 0;
            }
            else if (newGuide.FreeConnectorCount < _lastGuideFreeConnectors)
            {
                // If it's the same piece but with less free connectors,
                // update the respective cached information
                _lastGuideFreeConnectors = newGuide.FreeConnectorCount;
            }
            else
            {
                // If it's the same piece and with the same amount of free
                // connectors, then we may be entering an infinite loop and
                // generation should stop. Set guide piece to null to notify
                // caller that the generation is finished.
                newGuide = null;
            }

            return newGuide;
        }



        protected virtual MapPiece SelectEndPiece(
            List<MapPiece> enderList = null) => null;
    }
}