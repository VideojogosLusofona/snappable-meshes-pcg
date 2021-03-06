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
using UnityEngine.AI;

namespace SnapMeshPCG.Navigation
{
    /// <summary>
    /// Determine valid percentage of paths in the generated map.
    /// </summary>
    public class NavScanner : MonoBehaviour
    {
        // Number of navigation points to use for validating navmesh
        [SerializeField]
        private int _navPointCount = 400;

        // Initial search radius from map piece origin to nav point in navmesh
        [SerializeField]
        private float _initialSearchRadius = 0.1f;

        // Search radius increment when no nav point is found
        [SerializeField]
        private float _radiusIncrement = 0.1f;

        // Internal class for representing a navigation point
        private class NavPoint
        {
            public Vector3 point;
            public int connections;
        }

        // List of navigation points
        private List<NavPoint> _navPoints;

        /// <summary>
        /// Scan the navmesh for paths between randomized navigation points and
        /// print the percentage of valid paths between them.
        /// </summary>
        /// <param name="map">The map pieces.</param>
        public void ScanMesh(IReadOnlyList<MapPiece> map)
        {
            // Number of successful paths and attempts to find them
            int success = 0;
            int tries = 0;
            float percentPassable;

            // Where to store the calculated paths
            NavMeshPath storedPath = new NavMeshPath();

            // Initialize list of navigation points
            _navPoints = new List<NavPoint>();

            // Find random points in the navmesh and add them to the list
            for (int i = 0; i < _navPointCount; i++)
            {
                // Get a random map piece
                MapPiece piece = map[Random.Range(0, map.Count)];

                // Find nearest point in navmesh from the position of the
                // map piece
                Vector3 point1 = FindPointInNavMesh(piece.transform.position);

                // Add found navigation point to list
                _navPoints.Add(
                    new NavPoint() { point = point1, connections = 0 });
            }

            // Compare each navigation point to all others and check for a
            // valid connection
            for (int i = 0; i < _navPoints.Count; i++)
            {
                for (int j = i + 1; j < _navPoints.Count; j++)
                {
                    // Try and calculate a path between the two current points
                    bool path = NavMesh.CalculatePath(
                        _navPoints[i].point,
                        _navPoints[j].point,
                        NavMesh.AllAreas,
                        storedPath);

                    // Increment number of tries
                    tries++;

                    // Has a complete path been found?
                    if (path && storedPath.status == NavMeshPathStatus.PathComplete)
                    {
                        _navPoints[i].connections += 1;
                        _navPoints[j].connections += 1;
                        success++;
                    }
                }
            }

            // Determine percentage of passable/valid paths
            percentPassable = (float)success / tries;

            Debug.Log(string.Format(
                "Scanner: Evaluated {0} paths from {1} points, found {2} good paths. -> {3:p2}",
                tries, _navPointCount, success, percentPassable));
        }

        /// <summary>
        /// Find a navigation point in the navmesh near the given position.
        /// </summary>
        /// <param name="origin">Origin position of a map piece.</param>
        /// <returns>
        /// A navigation point near the origin position of the map piece.
        /// </returns>
        private Vector3 FindPointInNavMesh(Vector3 origin)
        {
            NavMeshHit hit;
            float searchRadius;
            bool foundSpot;
            int counter = 0;

            // From the origin position of the map piece, search for a point in
            // the navmesh by iteratively increasing the search radius
            do
            {
                searchRadius = _initialSearchRadius + _radiusIncrement * counter;
                foundSpot = NavMesh.SamplePosition(
                    origin + Random.insideUnitSphere * _initialSearchRadius,
                    out hit,
                    searchRadius,
                    NavMesh.AllAreas);

                counter++;

            } while (!foundSpot);

            return hit.position;
        }

        /// <summary>
        /// This method should be invoked when the generated map is cleared.
        /// </summary>
        public void ClearScanner()
        {
            // Discard the previously found navigation points, if any
            _navPoints = null;
        }

        /// <summary>
        /// Show navigation points if gizmos are on.
        /// </summary>
        private void OnDrawGizmos()
        {
            // Don't show anything if the generated map has been cleared
            if (_navPoints == null)
            {
                return;
            }

            // Draw gizmos on navigation points
            foreach (NavPoint np in _navPoints)
            {
                // If nav points have connections, show them in green
                // Otherwise show them in red
                Gizmos.color = (np.connections > 0) ? Color.green : Color.red;
                Gizmos.DrawSphere(np.point, 0.3f);
            }
        }
    }
}