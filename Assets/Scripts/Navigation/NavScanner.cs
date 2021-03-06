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

        // List of navigation points
        [SerializeField]
        [HideInInspector]
        private List<NavPoint> _navPoints;

        /// <summary>
        /// Read-only accessor to the list of navigation points, ordered by
        /// number of connections.
        /// </summary>
        public IReadOnlyList<NavPoint> NavPoints => _navPoints;

        // In how many volumes should we divide a map piece bounding box in
        // order to search for near navmesh points?
        private const int _DivideVol = 20;

        /// <summary>
        /// Scan the navmesh for paths between randomized navigation points and
        /// print the percentage of valid paths between them.
        /// </summary>
        /// <param name="mapPieces">
        /// The map pieces, sorted by placing order.
        /// </param>
        public void ScanMesh(IReadOnlyList<MapPiece> mapPieces)
        {
            // Number of successful paths and attempts to find them
            int success = 0;
            int tries = 0;
            float percentPassable;

            // Where to store the calculated paths
            NavMeshPath storedPath = new NavMeshPath();

            IReadOnlyList<(float sumVol, Bounds bounds)> locations =
                CalculateLocations(mapPieces);

            // Initialize list of navigation points
            _navPoints = new List<NavPoint>();

            // Find random points in the navmesh and add them to the list
            for (int i = 0; i < _navPointCount; i++)
            {
                // Find nearest point in navmesh from the position of the
                // map piece
                Vector3? point1 = FindPointInNavMesh(locations);

                // Add found navigation point to list
                if (point1.HasValue)
                    _navPoints.Add(new NavPoint(point1.Value));
                else
                    Debug.Log($"No navmesh point found");
            }

            // Compare each navigation point to all others and check for a
            // valid connection
            for (int i = 0; i < _navPoints.Count; i++)
            {
                for (int j = i + 1; j < _navPoints.Count; j++)
                {
                    // Try and calculate a path between the two current points
                    bool path = NavMesh.CalculatePath(
                        _navPoints[i].Point,
                        _navPoints[j].Point,
                        NavMesh.AllAreas,
                        storedPath);

                    // Increment number of tries
                    tries++;

                    // Has a complete path been found?
                    if (path && storedPath.status == NavMeshPathStatus.PathComplete)
                    {
                        _navPoints[i].IncConnections();
                        _navPoints[j].IncConnections();
                        success++;
                    }
                }
            }

            // Determine percentage of passable/valid paths
            percentPassable = (float)success / tries;

            Debug.Log(string.Format(
                "Scanner: Evaluated {0} paths from {1} points, found {2} good paths. -> {3:p2}",
                tries, _navPointCount, success, percentPassable));

            // Sort nav point list by number of connections before exiting
            _navPoints.Sort();
        }

        /// <summary>
        /// Find a navigation point in the navmesh near the given origin
        /// position by searching increasingly larger radiuses.
        /// </summary>
        /// <returns>
        /// A navigation point in the navmesh or null if no point is found.
        /// </returns>
        private Vector3? FindPointInNavMesh(
            IReadOnlyList<(float sumVol, Bounds bounds)> locations)
        {
            NavMeshHit hit;
            float searchRadius;
            bool foundSpot;
            int counter = 0;
            Vector3? pointInMesh = null;
            Bounds bounds;
            float totalVol = locations[locations.Count - 1].sumVol;

            float randomVol = Random.Range(0, totalVol);

            int lowerIdx = 0, upperIdx = locations.Count - 1;

            while (lowerIdx < upperIdx)
            {
                int midIdx = (lowerIdx + upperIdx) / 2;

                if (locations[midIdx].sumVol < randomVol)
                {
                    lowerIdx = midIdx + 1;
                }
                else
                {
                    upperIdx = midIdx;
                }
            }

            bounds = locations[upperIdx].bounds;

            Vector3 origin = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z));

            float radius = bounds.size.magnitude / _DivideVol;

            do
            {
                searchRadius = (1 + counter) * radius;
                foundSpot = NavMesh.SamplePosition(
                    origin,
                    out hit,
                    searchRadius,
                    NavMesh.AllAreas);

                counter++;

            } while (!foundSpot && counter < _DivideVol);

            if (foundSpot) pointInMesh = hit.position;

            return pointInMesh;
        }

        private IReadOnlyList<(float sumVol, Bounds bounds)> CalculateLocations(
            IReadOnlyCollection<MapPiece> mapPieces)
        {
            List<(float sumVol, Bounds bounds)> locations =
                new List<(float sumVol, Bounds bounds)>(mapPieces.Count);

            float runningVol = 0;

            foreach (MapPiece piece in mapPieces)
            {
                // Get the bounding box of the map piece
                Bounds bounds = piece.GetComponentInChildren<MeshRenderer>().bounds;

                // Running volume
                runningVol += bounds.size.magnitude;

                // Add location
                locations.Add((runningVol, bounds));
            }

            return locations;
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
                Gizmos.color = (np.Connections > 0) ? Color.green : Color.red;
                Gizmos.DrawSphere(np.Point, 0.3f);
            }
        }
    }
}