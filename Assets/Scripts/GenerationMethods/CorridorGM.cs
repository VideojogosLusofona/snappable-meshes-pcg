using System.Collections.Generic;
using System;

namespace trinityGen
{
    [System.Serializable]
    public sealed class CorridorGM : GenerationMethod
    {

        public int MaxPieces;
        private int _placedPieces;

        public bool pinchEnd;
        public bool useEndPiece;
        public List<ArenaPiece> enderList;

        public override ArenaPiece SelectStartPiece(List<ArenaPiece> starterList, int starterConTol = 0)
        {
            // Assumes that the list is sorted by number of connectors where 
            // [0] is the index with least connectors
            int botConnectorCount = starterList[0].ConnectorsCount;

            int maximumAllowed = botConnectorCount + starterConTol;
            List<ArenaPiece> possibles = new List<ArenaPiece>();
            foreach(ArenaPiece g in starterList)
            {
                if(g.ConnectorsCount <= maximumAllowed)
                    possibles.Add(g);

            }

            Random rng = new Random();
            // Upper limit is exclusive
            ArenaPiece chosen = possibles[rng.Next(starterList.Count)];
            if(_firstPiece == null)
                _firstPiece = chosen;

            _lastGuideSelected = chosen;
            return chosen;

        }
        
        public override ArenaPiece SelectGuidePiece(List<ArenaPiece> worldPieceList, ArenaPiece lastPlaced)
        {
            _placedPieces = worldPieceList.Count;

            if(_placedPieces >= MaxPieces)
                return null;

            // Select a special piece for the final piece
            else if(_placedPieces == MaxPieces - 1)
            {
                if(useEndPiece && enderList.Count > 0)
                {
                    if(pinchEnd)
                        return SelectStartPiece(enderList);
                    else
                        return SelectEndPiece(enderList);

                }
                    
            }
            _lastGuideSelected = lastPlaced;
            return lastPlaced;

        }

        protected override ArenaPiece SelectEndPiece(List<ArenaPiece> enderList = null)
        {
            Random rng = new Random();
            // Upper limit is exclusive
            ArenaPiece chosen = enderList[rng.Next(enderList.Count)];
            _lastGuideSelected = chosen;
            return chosen;

        }
    }
}