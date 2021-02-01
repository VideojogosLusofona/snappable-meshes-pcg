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
using System;

namespace TrinityGen.GenerationMethods
{
    public sealed class CorridorGM : GenerationMethod
    {
        private readonly int maxPieces;
        private readonly bool pinchEnd;
        private readonly bool useEndPiece;
        private readonly List<ArenaPiece> enderList;

        private int _placedPieces;

        public CorridorGM(int maxPieces)
        {
            this.maxPieces = maxPieces;
        }

        public override ArenaPiece SelectStartPiece(
            List<ArenaPiece> starterList, int starterConTol = 0)
        {
            // Assumes that the list is sorted by number of connectors where
            // [0] is the index with most connectors
            int botConnectorCount =
                starterList[starterList.Count - 1].ConnectorsCount;

            int maximumAllowed = botConnectorCount + starterConTol;
            List<ArenaPiece> possibles = new List<ArenaPiece>();
            foreach (ArenaPiece g in starterList)
            {
                if (g.ConnectorsCount <= maximumAllowed)
                    possibles.Add(g);
            }

            int rng = UnityEngine.Random.Range(0, possibles.Count - 1);
            // Upper limit is exclusive
            ArenaPiece chosen = possibles[rng];
            if (_firstPiece == null)
                _firstPiece = chosen;

            _lastGuideSelected = chosen;
            return chosen;

        }

        public override ArenaPiece SelectGuidePiece(
            List<ArenaPiece> worldPieceList, ArenaPiece lastPlaced)
        {
            _placedPieces = worldPieceList.Count;

            if (_placedPieces >= maxPieces)
            {
                return null;
            }
            // Select a special piece for the final piece
            else if (_placedPieces == maxPieces - 1)
            {
                if (useEndPiece && enderList.Count > 0)
                {
                    if (pinchEnd)
                        return SelectStartPiece(enderList);
                    else
                        return SelectEndPiece(enderList);

                }

            }
            _lastGuideSelected = lastPlaced;
            return lastPlaced;
        }

        protected override ArenaPiece SelectEndPiece(
            List<ArenaPiece> enderList = null)
        {
            Random rng = new Random();
            // Upper limit is exclusive
            ArenaPiece chosen = enderList[rng.Next(enderList.Count)];
            _lastGuideSelected = chosen;
            return chosen;
        }
    }
}