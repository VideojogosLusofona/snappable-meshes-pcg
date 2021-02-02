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
        private readonly int maxPieces;
        private int _placedPieces;

        public ArenaGM(int maxPieces)
        {
            this.maxPieces = maxPieces;
        }

        public override ArenaPiece SelectStartPiece(
            List<ArenaPiece> starterList, int starterConTol = 0)
        {
            // Assumes that the list is sorted by number of connectors where
            // [0] is the index with most connectors
            int topConnectorCount = starterList[0].ConnectorCount;

            int minimumAllowed = topConnectorCount - starterConTol;
            List<ArenaPiece> possibles = new List<ArenaPiece>();
            foreach(ArenaPiece g in starterList)
            {
                if(g.ConnectorCount >= minimumAllowed)
                    possibles.Add(g);
            }

            int rng = UnityEngine.Random.Range(0, possibles.Count - 1);
            // Upper limit is exclusive
            ArenaPiece chosen = possibles[rng];
            _firstPiece = chosen;
            //_lastGuideSelected = _firstPiece;
            return chosen;
        }

        public override ArenaPiece SelectGuidePiece(
            List<ArenaPiece> worldPieceList, ArenaPiece lastPlaced)
        {
            if (_lastGuideSelected == null)
                _lastGuideSelected = worldPieceList[0];
            _placedPieces = worldPieceList.Count;

            if(_placedPieces > maxPieces)
                return null;

            if(_lastGuideSelected.IsFull())
            {
                //_lastGuideSelected = lastPlaced;
                int i = worldPieceList.FindIndex(
                    a => a.gameObject.name == _lastGuideSelected.gameObject.name);
                _lastGuideSelected = worldPieceList[i + 1];
                return _lastGuideSelected;
            }

            return _lastGuideSelected;
        }
    }
}