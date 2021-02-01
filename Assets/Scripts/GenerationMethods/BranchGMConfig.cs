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

using UnityEngine;

namespace TrinityGen.GenerationMethods
{
    public class BranchGMConfig : GMConfig
    {
        [SerializeField]
        private uint _branchCount;

        [SerializeField]
        private uint _branchPieceCount;

        [SerializeField]
        private int _branchSizeVariance = 0;

        [SerializeField]
        private uint PieceSkippingVariance = 0;

        // Don't expose this to have branch calculate the jumping
        private uint _branchGenPieceSkipping = 0;

        public override GenerationMethod Method =>
            new BranchGM((int)_branchCount, (int)_branchPieceCount,
                _branchSizeVariance, (int)_branchGenPieceSkipping);

    }
}