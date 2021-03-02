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
    /// Helper class for the generation methods.
    /// </summary>
    public static class Helpers
    {
        /// <summary>Get the piece with most connectors from a list.</summary>
        /// <param name="pieceList">
        /// List where to get the piece from. This list is assumed to be sorted
        /// in descending order by number of connectors.
        /// </param>
        /// <param name="conTol">Connector count tolerance.</param>
        /// <returns>
        /// The piece with most connectors, apart from a tolerance given by the
        /// <paramref name="conTol"/> parameter.</returns>
        public static MapPiece GetPieceWithMostConnectors(
            IList<MapPiece> pieceList, int conTol)
        {
            // Index of selected starting piece in starting piece list
            int startingPieceIndex;

            // Get the number of connectors in the piece with the most
            // connectors
            int maxConnectorCount = pieceList[0].ConnectorCount;

            // Determine the minimum amount of connectors a piece may have in
            // order to be selected as the starting piece
            int minAllowed = maxConnectorCount - conTol;

            // Determine index of piece with the minimum allowed number of
            // connectors
            int minAllowedIndex = 0;
            for (int i = 1; i < pieceList.Count; i++)
            {
                if (pieceList[i].ConnectorCount >= minAllowed)
                    minAllowedIndex = i;
                else
                    break;
            }

            // Get the index of the starting piece
            startingPieceIndex = Random.Range(0, minAllowedIndex + 1);

            // Return the starting piece
            return pieceList[startingPieceIndex];
        }

        /// <summary>Get the piece with less connectors from a list.</summary>
        /// <param name="pieceList">
        /// List where to get the piece from. This list is assumed to be sorted
        /// in descending order by number of connectors.
        /// </param>
        /// <param name="conTol">Connector count tolerance.</param>
        /// <returns>
        /// The piece with less connectors, apart from a tolerance given by the
        /// <paramref name="conTol"/> parameter.</returns>
        public static MapPiece GetPieceWithLessConnectors(
            IList<MapPiece> pieceList, int conTol)
        {
            // Index of selected starting piece in starting piece list
            int startingPieceIndex;

            // Get the number of connectors in the piece with the least
            // connectors
            int minConnectorCount = pieceList.Last().ConnectorCount;

            // Determine the maximum amount of connectors a piece may have in
            // order to be selected as the starting piece
            int maxAllowed = minConnectorCount + conTol;

            // Determine index of piece with the maximum allowed number of
            // connectors
            int maxAllowedIndex = pieceList.Count - 1;
            for (int i = maxAllowedIndex - 1; i >= 0; i--)
            {
                if (pieceList[i].ConnectorCount <= maxAllowed)
                    maxAllowedIndex = i;
                else
                    break;
            }

            // Get the index of the starting piece
            startingPieceIndex = Random.Range(maxAllowedIndex, pieceList.Count);

            // Return the starting piece
            return pieceList[startingPieceIndex];
        }
    }
}