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

            // Obtain list of map piece bounds with running sum box volume
            // This will be required to evenly distribute nav points through
            // the map
            IReadOnlyList<(float sumVol, Bounds bounds)> pieceBounds =
                CalculateLocations(mapPieces);

            // Dictionary of nav points and their clusters
            IDictionary<NavPoint, ISet<NavPoint>> navPointClusters =
                new Dictionary<NavPoint, ISet<NavPoint>>();

            // Initialize list of navigation points
            _navPoints = new List<NavPoint>();

            // Find random points in the navmesh and add them to the list
            for (int i = 0; i < _navPointCount; i++)
            {
                // Find nearest point in navmesh from the position of the
                // map piece
                Vector3? point = FindPointInNavMesh(pieceBounds);

                // Add found navigation point to list
                if (point.HasValue)
                    _navPoints.Add(new NavPoint(point.Value));
                else
                    Debug.Log($"No navmesh point found");
            }

            // Compare each navigation point to all others and check for a
            // valid connection
            for (int i = 0; i < _navPoints.Count; i++)
            {
                for (int j = i + 1; j < _navPoints.Count; j++)
                {
                    // Put nav points in locals vars, easier to work with
                    NavPoint p1 = _navPoints[i];
                    NavPoint p2 = _navPoints[j];

                    // Try and calculate a path between the two current points
                    bool path = NavMesh.CalculatePath(
                        p1.Point, p2.Point, NavMesh.AllAreas, storedPath);

                    // Increment number of tries
                    tries++;

                    // Has a complete path been found?
                    if (path && storedPath.status == NavMeshPathStatus.PathComplete)
                    {
                        // Cluster generation
                        if (p1.Isolated && p2.Isolated)
                        {
                            // If both points are isolated, create a new cluster
                            // for containing them
                            ISet<NavPoint> newCluster =
                                new HashSet<NavPoint>() { p1, p2 };

                            // Connect cluster to each point
                            navPointClusters.Add(p1, newCluster);
                            navPointClusters.Add(p2, newCluster);
                        }
                        else if (!p1.Isolated && p2.Isolated)
                        {
                            // If first point has cluster and the second does
                            // not, add second point to the first's cluster
                            navPointClusters[p1].Add(p2);
                            navPointClusters.Add(p2, navPointClusters[p1]);
                        }
                        else if (p1.Isolated && !p2.Isolated)
                        {
                            // If second point has cluster and the first does
                            // not, add first point to the seconds's cluster
                            navPointClusters[p2].Add(p1);
                            navPointClusters.Add(p1, navPointClusters[p2]);
                        }
                        else if (navPointClusters[p1] != navPointClusters[p2])
                        {
                            // If points are in different clusters, merge
                            // clusters
                            navPointClusters[p1].UnionWith(navPointClusters[p2]);
                            navPointClusters[p2] = navPointClusters[p1];
                        }

                        // Update points
                        p1.IncConnections();
                        p2.IncConnections();
                        success++;
                    }
                }
            }

            // Determine percentage of passable/valid paths
            percentPassable = (float)success / tries;

            Debug.Log(string.Format(
                "Scanner: Evaluated {0} paths from {1} points, found {2} good paths. -> {3:p2}",
                tries, _navPointCount, success, percentPassable));

            List<ISet<NavPoint>> clusters = new List<ISet<NavPoint>>(new HashSet<ISet<NavPoint>>(navPointClusters.Values));
            clusters.Sort((clust1, clust2) => clust2.Count - clust1.Count);
            System.Text.StringBuilder sb = new System.Text.StringBuilder($"There are {clusters.Count} clusters\n");
            for (int i = 0; i < clusters.Count; i++)
            {
                sb.AppendFormat("\tCluster {0:d2} has {1} points ({2:p2})\n",
                    i, clusters[i].Count, clusters[i].Count / (float)_navPoints.Count);
            }
            Debug.Log(sb.ToString());

            // Sort nav point list by number of connections before returning
            _navPoints.Sort();
        }

        /// <summary>
        /// Get list of map piece bounds with running sum box volume.
        /// </summary>
        /// <param name="mapPieces">List of map pieces.</param>
        /// <returns>
        /// A list of map piece bounds with running sum box volume.
        /// </returns>
        private IReadOnlyList<(float sumVol, Bounds bounds)> CalculateLocations(
            IReadOnlyCollection<MapPiece> mapPieces)
        {
            // Instantiate the list
            List<(float sumVol, Bounds bounds)> pieceBounds =
                new List<(float sumVol, Bounds bounds)>(mapPieces.Count);

            // Initialize running volume sum
            float runningVol = 0;

            foreach (MapPiece piece in mapPieces)
            {
                // Get the bounding box for the current map piece
                Bounds bounds = piece.GetComponentInChildren<MeshRenderer>().bounds;

                // Add the running volume
                runningVol += bounds.size.magnitude;

                // Keep bounds and running volume for the current map piece
                pieceBounds.Add((runningVol, bounds));
            }

            return pieceBounds;
        }

        /// <summary>
        /// Find a random navigation point in the navmesh.
        /// </summary>
        /// <param name="pieceBounds">
        /// List of map piece bounds with running sum box volume.
        /// </param>
        /// <returns>A random navigation point in the navmesh.</returns>
        private Vector3? FindPointInNavMesh(
            IReadOnlyList<(float sumVol, Bounds bounds)> pieceBounds)
        {
            // Location of the nav point in the nav mesh
            NavMeshHit hit;

            // Was a nav point found?
            bool pointFound;

            // Counter for quitting the nav point search loop
            int counter = 0;

            // Nullable where to return the nav point, if found
            Vector3? pointInMesh = null;

            // Bounds of the map piece where to search for a nav point
            Bounds bounds;

            // Random location in the map piece bounding box where to start
            // searching for the nav point
            Vector3 origin;

            // Initial nav point search radius from origin
            float radius;

            // Total "volume" of all map pieces bounding boxes
            float totalVol = pieceBounds[pieceBounds.Count - 1].sumVol;

            // Get a random "volume" between zero and the total "volume"
            // Pieces with more "volume" will have higher probability of having
            // nav points
            float randomVol = Random.Range(0, totalVol);

            // Perform a binary search for the map piece bounds with the
            // corresponding running sum volume
            int lowerIdx = 0, upperIdx = pieceBounds.Count - 1;

            while (lowerIdx < upperIdx)
            {
                int midIdx = (lowerIdx + upperIdx) / 2;

                if (pieceBounds[midIdx].sumVol < randomVol)
                {
                    lowerIdx = midIdx + 1;
                }
                else
                {
                    upperIdx = midIdx;
                }
            }

            // Get bounds for the map piece
            bounds = pieceBounds[upperIdx].bounds;

            // Get a random location within the map piece bounds
            origin = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z));

            // Determine initial radius for nav point search based on origin
            radius = bounds.size.magnitude / _DivideVol;

            // Search for nav point with increasing radius until a nav point is
            // found or the counter reaches the limit
            do
            {
                float searchRadius = (1 + counter) * radius;
                pointFound = NavMesh.SamplePosition(
                    origin,
                    out hit,
                    searchRadius,
                    NavMesh.AllAreas);

                counter++;

            } while (!pointFound && counter < _DivideVol);

            // If a point is found, save it in the nullable
            if (pointFound) pointInMesh = hit.position;

            return pointInMesh;
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