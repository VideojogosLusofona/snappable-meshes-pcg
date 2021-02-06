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
    public abstract class AbstractGM
    {
        //TODO: Make some sort of container for all the parameters of a given
        //method

        protected MapPiece _firstPiece;
        protected MapPiece _lastGuideSelected;

        public abstract MapPiece SelectStartPiece(
            List<MapPiece> starterList, int starterConTol = 0);

        /// <summary>
        /// Select piece in the world for evaluation.
        /// If it returns null, generation is over.
        /// </summary>
        /// <param name="worldPieceList">Placed Geometry to select Guide from
        /// </param>
        /// <param name="lastPlaced">Last successfully placed geometry</param>
        /// <returns>The guide piece or null if generation is over.</returns>
        public abstract MapPiece SelectGuidePiece(
            List<MapPiece> worldPieceList, MapPiece lastPlaced);

        protected virtual MapPiece SelectEndPiece(
            List<MapPiece> enderList = null) => null;
    }
}