/*
 * Copyright 2021 TrinityGenerator_Standalone contributors
 * (https://github.com/RafaelCS-Aula/TrinityGenerator_Standalone)
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