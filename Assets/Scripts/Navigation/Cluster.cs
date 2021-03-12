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
using UnityEngine;

namespace SnapMeshPCG.Navigation
{
    /// <summary>
    /// A list of navigation points.
    /// </summary>
    /// <remarks>
    /// This middleware class is required so we can serialize a list of
    /// clusters.
    /// </remarks>
    [System.Serializable]
    public class Cluster
    {
        // Points that constitute the cluster
        [SerializeField]
        private List<NavPoint> _points;

        /// <summary>
        /// Read-only list of the points in the cluster.
        /// </summary>
        public IReadOnlyList<NavPoint> Points => _points;

        /// <summary>
        /// Create a new cluster with the specified points.
        /// </summary>
        /// <param name="points">Points which make up the cluster.</param>
        public Cluster(IEnumerable<NavPoint> points)
        {
            _points = new List<NavPoint>(points);
        }
    }
}