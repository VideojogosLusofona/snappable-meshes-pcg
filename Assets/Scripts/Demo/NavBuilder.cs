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
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine.Events;

namespace SnapMeshPCG.Demo
{
    /// <summary>
    /// Build a navigation mesh on the generated map.
    /// </summary>
    [RequireComponent(typeof(NavMeshSurface))]
    public class NavBuilder : MonoBehaviour
    {
        // The character that will move around the map when we enter play mode
        [SerializeField]
        private NavWalker demoCharacter = null;

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
            // Check for the existence of the NavMeshSurface component on the
            // game object
            // If it's not there, add it
            NavMeshSurface dummyNav = GetComponent<NavMeshSurface>();

            if (dummyNav == null) gameObject.AddComponent<NavMeshSurface>();

            // Invoke co-routine to perform the actual navmesh creation
            StartCoroutine(BuildNavMeshCR(pieces));
        }

        /// <summary>
        /// Co-routine to perform the actual navmesh creation.
        /// </summary>
        /// <param name="pieces">
        /// The map pieces to create the navmesh on.
        /// </param>
        /// <returns>The co-routine's IEnumerator.</returns>
        /// <remarks>
        /// This does not technically need to be a co-routine since we're
        /// creating the navmesh in editor mode. However, if this code gets
        /// used for runtime navmesh creation, we need to skip one frame before
        /// navmesh creation can begin, and one way to do this is with a
        /// co-routine.
        /// </remarks>
        private IEnumerator BuildNavMeshCR(IReadOnlyList<MapPiece> pieces)
        {
            // Skip first frame (only needed if this is used in play mode)
            yield return null;

            // Get starting piece
            GameObject topPiece = pieces[0].gameObject;

            NavMeshSurface unWantedNav = topPiece.GetComponent<NavMeshSurface>();
            if(unWantedNav != null)
                Destroy(unWantedNav);
            NavMeshSurface nav = topPiece.AddComponent<NavMeshSurface>();
            nav.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            print($"Building NavMesh at parent piece: {nav.gameObject.name}");
            nav.BuildNavMesh();

            //demoCharacter.mapPieces = pieces.ToArray();
            Instantiate(demoCharacter.gameObject, new Vector3(0, 10, 0), Quaternion.identity);
            //go.GetComponent<NavWalker>().mapPieces = pieces;

            OnNavMeshReady.Invoke(pieces);
        }
    }
}

