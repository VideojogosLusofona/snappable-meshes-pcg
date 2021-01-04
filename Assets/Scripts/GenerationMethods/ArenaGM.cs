using System.Collections.Generic;
using System;

namespace trinityGen
{
    [System.Serializable]
    public sealed class ArenaGM : GenerationMethod
    {
        public int MaxPieces;
        private int _placedPieces;

        public override ArenaPiece SelectStartPiece(List<ArenaPiece> starterList, int starterConTol = 0)
        {
            // Assumes that the list is sorted by number of connectors where 
            // [0] is the index with least connectors
            int topConnectorCount = starterList[starterList.Count - 1].ConnectorsCount;

            int minimumAllowed = topConnectorCount - starterConTol;
            List<ArenaPiece> possibles = new List<ArenaPiece>();
            foreach(ArenaPiece g in starterList)
            {
                if(g.ConnectorsCount >= minimumAllowed)
                    possibles.Add(g);

            }

            Random rng = new Random();
            // Upper limit is exclusive
            ArenaPiece chosen = possibles[rng.Next(starterList.Count)];
            _firstPiece = chosen;
            _lastGuideSelected = _firstPiece;
            return chosen;
        }

        public override ArenaPiece SelectGuidePiece(List<ArenaPiece> worldPieceList, ArenaPiece lastPlaced)
        {
            _placedPieces = worldPieceList.Count;

            if(_placedPieces >= MaxPieces)
                return null;

            if(_lastGuideSelected.IsFull())
            {
                return lastPlaced;
            }

            return _lastGuideSelected;
        }


    }
}