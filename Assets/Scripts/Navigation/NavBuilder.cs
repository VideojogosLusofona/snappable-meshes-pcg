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
            // Vertical offset required for when we place the walker
            float up;

            // Start position for the walker
            Vector3 start;

            // Check for the existence of the NavMeshSurface component on the
            // game object
            // If it's not there, add it
            NavMeshSurface dummyNav = GetComponent<NavMeshSurface>();
            if (dummyNav == null) gameObject.AddComponent<NavMeshSurface>();

            // Get starting piece which is also the parent (top) piece of all
            // the others
            GameObject topPiece = pieces[0].gameObject;

            // Get the bounds of the top piece
            Bounds startBounds = topPiece.GetComponentInChildren<MeshRenderer>().bounds;

            // If the top piece has the navmesh surface component, remove it
            NavMeshSurface unWantedNav = topPiece.GetComponent<NavMeshSurface>();
            if(unWantedNav != null) DestroyImmediate(unWantedNav);

            // Add a navmesh component to the top piece and use the geometry
            // from the render meshes
            NavMeshSurface nav = topPiece.AddComponent<NavMeshSurface>();
            nav.useGeometry = NavMeshCollectGeometry.RenderMeshes;

            // Build navmesh at top piece and consequently to all the child
            // pieces
            //Debug.Log($"Building NavMesh at parent piece: {nav.gameObject.name}");
            nav.BuildNavMesh();

            // Create a walker character to walk around in our map and initially
            // place it on top of the starting piece
            up = walker.gameObject.GetComponent<MeshRenderer>().bounds.extents.y;
            start = startBounds.center + Vector3.up * (startBounds.extents.y + up);
            Instantiate(walker.gameObject, start, Quaternion.identity);

            // Notify listeners that the navmesh has been baked
            OnNavMeshReady.Invoke(pieces);
        }
    }
}

