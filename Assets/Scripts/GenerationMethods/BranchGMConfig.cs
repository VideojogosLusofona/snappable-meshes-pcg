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

namespace SnapMeshPCG.GenerationMethods
{
    /// <summary>
    /// Configures the branch generation method.
    /// </summary>
    public class BranchGMConfig : AbstractGMConfig
    {
        // Number of branches to be created
        [SerializeField]
        private uint _branchCount = 0;

        // The average amount of pieces a branch will have
        [SerializeField]
        private uint _branchLength = 0;

        // The maximum variation from branchLength in each branch
        [SerializeField]
        private uint _branchLengthVar = 0;

        /// <summary>
        /// Returns the configured branch generation method.
        /// </summary>
        public override AbstractGM Method =>
            new BranchGM(_branchCount, _branchLength, _branchLengthVar);
    }
}