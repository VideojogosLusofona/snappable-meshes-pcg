using System.Collections.Generic;
using System;

namespace trinityGen
{
    [System.Serializable]
    public sealed class StarGM : GenerationMethod
    {
        public int spokeLength;
        

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
            
            
            // Check if its time to jump back to the starter piece.
            // -1 takes out the starter piece from the equation.
            if(worldPieceList.Count - 1 % spokeLength == 0)
            {
                // A number of pieces equal to the spokeLength have been placed
                // Return the first piece if it still has unused connectors
                return (_firstPiece.IsFull()) ? null : _firstPiece;

            }

         _lastGuideSelected = lastPlaced;
         return _lastGuideSelected;
        }


    }
}