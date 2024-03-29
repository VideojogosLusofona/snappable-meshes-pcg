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

using System;
using System.Collections.Generic;

namespace SnapMeshPCG.SelectionMethods
{
    /// <summary>
    /// The base class for all selection methods.
    /// </summary>
    public abstract class AbstractSM
    {
        // Free connectors in the last guide piece returned
        private int _lastGuideFreeConnectors;

        /// <summary>
        /// Last guide piece returned by the selection method.
        /// </summary>
        protected MapPiece LastGuide { get; private set; }

        /// <summary>Select the starting piece.</summary>
        /// <param name="starterList">
        /// List where to get the starting piece from. This list is assumed to
        /// be sorted in descending order by number of connectors.
        /// </param>
        /// <param name="starterConTol">Connector count tolerance.</param>
        /// <returns>The starting piece.</returns>
        /// <remarks>
        /// Concrete selection methods must override this method in order to
        /// define how to select the starting piece.
        /// </remarks>
        protected abstract MapPiece DoSelectStartPiece(
            IList<MapPiece> starterList, int starterConTol);

        /// <summary>
        /// Selects the next guide piece according to the selection method.
        /// </summary>
        /// <param name="piecesInMap">Pieces already place in the map.</param>
        /// <returns>
        /// The next guide piece or null if the generation is finished.
        /// </returns>
        /// <remarks>
        /// Concrete selection methods must override this method in order to
        /// define how to select the next guide piece.
        /// </remarks>
        protected abstract MapPiece DoSelectGuidePiece(IList<MapPiece> piecesInMap);

        /// <summary>Select the starting piece.</summary>
        /// <param name="starterList">
        /// List where to get the starting piece from. This list is assumed to
        /// be sorted in descending order by number of connectors.
        /// </param>
        /// <param name="starterConTol">Connector count tolerance.</param>
        /// <returns>The starting piece.</returns>
        public MapPiece SelectStartPiece(
            IList<MapPiece> starterList, int starterConTol)
        {
            // The starting piece to return
            MapPiece startingPiece;

            // If the starter list does not contain any pieces, throw an
            // exception
            if ((starterList?.Count ?? 0) == 0)
                throw new ArgumentException("List of starting pieces is empty.");

            // Get the starting piece from the concrete selection method
            startingPiece = DoSelectStartPiece(starterList, starterConTol);

            // Keep the number of free connectors
            _lastGuideFreeConnectors = startingPiece?.FreeConnectorCount ?? 0;

            // Return the starting piece
            return startingPiece;
        }

        /// <summary>
        /// Selects the next guide piece according to the selection method.
        /// </summary>
        /// <param name="piecesInMap">Pieces already place in the map.</param>
        /// <returns>
        /// The next guide piece or null if the generation is finished.
        /// </returns>
        public MapPiece SelectGuidePiece(IList<MapPiece> piecesInMap)
        {
            // The guide piece to return
            MapPiece newGuide;

            // If the piecesInMap list does not contain any pieces, throw an
            // exception
            if ((piecesInMap?.Count ?? 0) == 0)
                throw new ArgumentException("List of map pieces is empty.");

            // If there's no last guide piece, we're in the first iteration, so
            // we can assume the last placed piece is the current guide piece
            if (LastGuide is null)
            {
                LastGuide = piecesInMap[0];
            }

            // Invoke the concrete method for selecting the next guide piece
            newGuide = DoSelectGuidePiece(piecesInMap);

            // Check the guide piece returned by the concrete selection method
            if (newGuide != LastGuide)
            {
                // If it's a different piece, update our cached information
                // accordingly
                LastGuide = newGuide;
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

            // Return the guide piece
            return newGuide;
        }
    }
}