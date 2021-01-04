using System.Collections.Generic;
using System;
using UnityEngine;

namespace trinityGen
{
    public sealed class ArenaGM : GenerationMethod
    {
        public int MaxPieces;
        private int _placedPieces;


        public ArenaGM(int maxPieces)
        {
            this.MaxPieces = maxPieces; 
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

            int rng = UnityEngine.Random.Range(0, possibles.Count - 1);
            // Upper limit is exclusive
            ArenaPiece chosen = possibles[rng];
            _firstPiece = chosen;
            //_lastGuideSelected = _firstPiece;
            return chosen;
        }

        public override ArenaPiece SelectGuidePiece(List<ArenaPiece> worldPieceList, ArenaPiece lastPlaced)
        {
            if (_lastGuideSelected == null)
                _lastGuideSelected = worldPieceList[0];
            _placedPieces = worldPieceList.Count;

            if(_placedPieces > MaxPieces)
                return null;

            if(_lastGuideSelected.IsFull())
            {
                //_lastGuideSelected = lastPlaced;
                int i = worldPieceList.FindIndex(a => a.gameObject.name == _lastGuideSelected.gameObject.name);
                _lastGuideSelected = worldPieceList[i + 1];
                return _lastGuideSelected;
            }


            return _lastGuideSelected;
        }


    }
}