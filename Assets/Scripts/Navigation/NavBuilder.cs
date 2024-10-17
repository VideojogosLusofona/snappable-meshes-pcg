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
using Unity.AI.Navigation;
using UnityEngine.Events;
using System.Collections.Generic;
using NaughtyAttributes;

namespace SnapMeshPCG.Navigation
{
    /// <summary>
    /// Build a navigation mesh on the generated map.
    /// </summary>
    [RequireComponent(typeof(NavMeshSurface))]
    public class NavBuilder : MonoBehaviour
    {
        // Event raised after the navigation mesh has been built
        [Foldout(":: Events ::")]
        [SerializeField]
        private UnityEvent<IReadOnlyList<MapPiece>> OnNavMeshReady = null;

        /// <summary>
        /// Creates the navmesh on the procedurally generated map.
        /// </summary>
        /// <param name="pieces">
        /// The map pieces to create the navmesh on.
        /// </param>
        public void BuildNavMesh(IReadOnlyList<MapPiece> pieces)
        {
            // Get the NavMeshSurface component on the NavController game object
            NavMeshSurface nav = GetComponent<NavMeshSurface>();

            // Build navmesh
            nav.BuildNavMesh();

            // Notify listeners that the navmesh has been baked
            OnNavMeshReady.Invoke(pieces);
        }
    }
}

