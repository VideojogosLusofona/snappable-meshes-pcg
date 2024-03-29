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

namespace SnapMeshPCG.Navigation
{
    /// <summary>
    /// Provides a simplified view for an object providing navigation info.
    /// </summary>
    public interface INavInfo
    {
        /// <summary>
        /// Read-only accessor to the list of navigation points, ordered by
        /// number of connections (descending).
        /// </summary>
        IReadOnlyList<NavPoint> NavPoints { get; }

        /// <summary>
        /// Read-only accessor to the list of nav point clusters, ordered by
        /// cluster size (descending).
        /// </summary>
        IReadOnlyList<Cluster> Clusters { get; }
    }
}