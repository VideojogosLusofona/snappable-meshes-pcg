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
    public sealed class StarGM : AbstractGM
    {
        private readonly int _armLength;
        private readonly int _armLengthVariance;

        public StarGM(int armLength, int armLengthVariance)
        {
            _armLength = armLength;
            _armLengthVariance = armLengthVariance;
        }

        protected override MapPiece DoSelectStartPiece(
            List<MapPiece> starterList, int starterConTol = 0)
        {

            // Assumes that the list is sorted by number of connectors where
            // [0] is the index with most connectors
            int topConnectorCount = starterList[0].ConnectorCount;

            int minimumAllowed = topConnectorCount - starterConTol;
            List<MapPiece> possibles = new List<MapPiece>();

            foreach (MapPiece g in starterList)
            {
                if (g.ConnectorCount >= minimumAllowed)
                    possibles.Add(g);
            }

            // Upper limit is exclusive
            int rng = UnityEngine.Random.Range(0, possibles.Count - 1);
            // Upper limit is exclusive
            MapPiece chosen = possibles[rng];
            _lastGuideSelected = _firstPiece;
            return chosen;
        }

        protected override MapPiece DoSelectGuidePiece(
            List<MapPiece> piecesInMap, MapPiece lastPlaced)
        {
            int rng = UnityEngine.Random.Range(0, _armLengthVariance + 1);
            int chosenVar = rng;
            int[] mults = { -1, 1 };
            rng = UnityEngine.Random.Range(0, mults.Length);
            int chosenMult = mults[rng];
            int currentArmLength = _armLength + (chosenVar * chosenMult);

            // Check if its time to jump back to the starter piece.
            // -1 takes out the starter piece from the equation.
            if ((piecesInMap.Count - 1) % currentArmLength == 0)
            {
                // A number of pieces equal to the armLength have been placed
                // Return the first piece if it still has unused connectors
                return piecesInMap[0].IsFull() ? null : piecesInMap[0];
            }

            _lastGuideSelected = lastPlaced;
            if (_lastGuideSelected.IsFull())
                return null;
            return _lastGuideSelected;
        }
    }
}