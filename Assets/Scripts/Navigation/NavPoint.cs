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

using System;
using UnityEngine;

namespace SnapMeshPCG.Navigation
{
    /// <summary>
    /// This class represents a random navigation point in the navmesh.
    /// </summary>
    [Serializable]
    public class NavPoint : IComparable<NavPoint>
    {
        /// <summary>The location of the navigation point.</summary>
        public Vector3 Point { get; }

        /// <summary>Number of connections with other navigation points.</summary>
        public int Connections { get; private set; }

        /// <summary>
        /// Create a new navigation point with zero connections.
        /// </summary>
        /// <param name="point">Location of the navigation point.</param>
        public NavPoint(Vector3 point)
        {
            Point = point;
            Connections = 0;
        }

        /// <summary>
        /// Increment the number of connections in this navigation point.
        /// </summary>
        public void IncConnections()
        {
            Connections++;
        }

        /// <summary>
        /// Implementation of the IComparable{T} interface. Orders points by
        /// descending number of connections.
        /// </summary>
        /// <param name="other">
        /// The other point to compare with this one.
        /// </param>
        /// <returns>
        /// Less than zero if this point has more connections than
        /// <paramref name="other"/>, zero if both points have the same number
        /// of connections or more than zero if this point has less connections
        /// than <paramref name="other"/>.
        /// </returns>
        public int CompareTo(NavPoint other) =>
            other.Connections.CompareTo(Connections);
    }
}