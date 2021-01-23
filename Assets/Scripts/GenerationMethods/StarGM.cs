using System.Collections.Generic;

namespace TrinityGen.GenerationMethods
{

    public sealed class StarGM : GenerationMethod
    {
        private int spokeLength;
        private int spokeLengthVariance;

        public StarGM(int spokeLength, int spokeLengthVariance)
        {
            this.spokeLength = spokeLength;
            this.spokeLengthVariance = spokeLengthVariance;

        }

        public override ArenaPiece SelectStartPiece(List<ArenaPiece> starterList, int starterConTol = 0)
        {

            // Assumes that the list is sorted by number of connectors where
            // [0] is the index with most connectors
            int topConnectorCount = starterList[0].ConnectorsCount;

            int minimumAllowed = topConnectorCount - starterConTol;
            List<ArenaPiece> possibles = new List<ArenaPiece>();
            foreach(ArenaPiece g in starterList)
            {
                if(g.ConnectorsCount >= minimumAllowed)
                    possibles.Add(g);

            }

            // Upper limit is exclusive
            int rng = UnityEngine.Random.Range(0, possibles.Count - 1);
            // Upper limit is exclusive
            ArenaPiece chosen = possibles[rng];
            _lastGuideSelected = _firstPiece;
            return chosen;
        }

        public override ArenaPiece SelectGuidePiece(List<ArenaPiece> worldPieceList, ArenaPiece lastPlaced)
        {
            int rng = UnityEngine.Random.Range(0, spokeLengthVariance + 1);
            int chosenVar = rng;
            int[] mults = { -1, 1 };
            rng = UnityEngine.Random.Range(0, mults.Length);
            int chosenMult = mults[rng];
            int currentSpokeLength = spokeLength + (chosenVar * chosenMult);

            // Check if its time to jump back to the starter piece.
            // -1 takes out the starter piece from the equation.
            if ((worldPieceList.Count - 1) % currentSpokeLength == 0)
            {
                // A number of pieces equal to the spokeLength have been placed
                // Return the first piece if it still has unused connectors
                return (worldPieceList[0].IsFull()) ? null : worldPieceList[0];

            }

         _lastGuideSelected = lastPlaced;
            if (_lastGuideSelected.IsFull())
                return null;
         return _lastGuideSelected;
        }


    }
}