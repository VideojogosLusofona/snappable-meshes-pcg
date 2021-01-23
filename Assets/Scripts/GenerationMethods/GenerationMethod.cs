using System;
using System.Collections.Generic;


namespace TrinityGen.GenerationMethods
{

    public abstract class GenerationMethod
    {
        //TODO: Make some sort of container for all the parameters of a given
        //method


        protected ArenaPiece _firstPiece;
        protected ArenaPiece _lastGuideSelected;

        public abstract ArenaPiece SelectStartPiece(List<ArenaPiece> starterList, int starterConTol = 0);

        /// <summary>
        /// Select piece in the world for evaluation.
        /// If it returns null, generation is over.
        /// </summary>
        /// <param name="worldPieceList">Placed Geometry to select Guide from
        /// </param>
        /// <param name="lastPlaced">Last succesfully placed geometry</param>
        /// <returns></returns>
        public abstract ArenaPiece SelectGuidePiece(List<ArenaPiece> worldPieceList, ArenaPiece lastPlaced);
        protected virtual ArenaPiece SelectEndPiece
        (List<ArenaPiece> enderList = null) => null;


    }
}