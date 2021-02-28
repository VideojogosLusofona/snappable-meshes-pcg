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
    public sealed class ArenaGM : AbstractGM
    {
        private readonly int _maxPieces;

        public ArenaGM(int maxPieces)
        {
            _maxPieces = maxPieces;
        }

        public override MapPiece SelectStartPiece(
            List<MapPiece> starterList, int starterConTol = 0)
        {
            // Assumes that the list is sorted by number of connectors where
            // [0] is the index with most connectors
            int topConnectorCount = starterList[0].ConnectorCount;

            int minimumAllowed = topConnectorCount - starterConTol;
            List<MapPiece> possibles = new List<MapPiece>();
            foreach(MapPiece g in starterList)
            {
                if(g.ConnectorCount >= minimumAllowed)
                    possibles.Add(g);
            }

            int rng = UnityEngine.Random.Range(0, possibles.Count - 1);
            // Upper limit is exclusive
            MapPiece chosen = possibles[rng];
            _firstPiece = chosen;
            //_lastGuideSelected = _firstPiece;
            return chosen;
        }

        protected override MapPiece DoSelectGuidePiece(
            List<MapPiece> piecesInMap, MapPiece lastPlaced)
        {
            if (_lastGuideSelected == null)
                _lastGuideSelected = piecesInMap[0];

            if(piecesInMap.Count > _maxPieces)
                return null;

            if(_lastGuideSelected.IsFull())
            {
                //_lastGuideSelected = lastPlaced;
                int i = piecesInMap.FindIndex(
                    a => a.gameObject.name == _lastGuideSelected.gameObject.name);
                _lastGuideSelected = piecesInMap[i + 1];
                return _lastGuideSelected;
            }

            return _lastGuideSelected;
        }
    }
}