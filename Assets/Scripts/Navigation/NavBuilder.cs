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
using UnityEngine.AI;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine.Events;

namespace SnapMeshPCG.Navigation
{
    /// <summary>
    /// Build a navigation mesh on the generated map.
    /// </summary>
    [RequireComponent(typeof(NavMeshSurface))]
    public class NavBuilder : MonoBehaviour
    {
        // The character that will move around the map when we enter play mode
        [SerializeField]
        private NavWalker walker = null;

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
            // Get starting piece which is also the parent (top) piece of all
            // the others
            GameObject topPiece = pieces[0].gameObject;

            // Get the bounds of the starting piece
            Bounds startBounds = topPiece.GetComponentInChildren<MeshRenderer>().bounds;

            // Get the NavMeshSurface component on the NavController game object
            NavMeshSurface nav = GetComponent<NavMeshSurface>();

            // Build navmesh
            nav.BuildNavMesh();

            // Create a walker character to walk around in our map and initially
            // place it at the center of the starting piece
            Instantiate(walker.gameObject, startBounds.center, Quaternion.identity);

            // Notify listeners that the navmesh has been baked
            OnNavMeshReady.Invoke(pieces);
        }
    }
}

