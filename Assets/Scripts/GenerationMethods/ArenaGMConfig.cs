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
    /// Configures the arena generation method.
    /// </summary>
    public class ArenaGMConfig : AbstractGMConfig
    {
        // Maximum number of pieces the method will use to create an arena
        [SerializeField]
        private uint _maxPieces = 0;

        /// <summary>
        /// Returns the configured arena generation method.
        /// </summary>
        public override AbstractGM Method => new ArenaGM((int)_maxPieces);
    }
}