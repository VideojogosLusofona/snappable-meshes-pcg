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
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;
using NaughtyAttributes;

// Avoid conflict with System.Diagnostics.Debug
using Debug = UnityEngine.Debug;

namespace SnapMeshPCG.Navigation
{
    /// <summary>
    /// Determine valid percentage of paths in the generated map.
    /// </summary>
    public class NavScanner : MonoBehaviour
    {
        // Enum used internally to defined if and how to reinitialize the RNG
        private enum ReInitRNG { No, Randomly, Seed }

        // ///////// //
        // Constants //
        // ///////// //

        private const string navPointsLabel = ":: Navigation Points ::";
        private const string rngLabel = ":: Random Number Generator ::";

        // ////////////////////////////// //
        // Nav setup and debug parameters //
        // ////////////////////////////// //

        // Number of navigation points to use for validating navmesh
        [SerializeField]
        [BoxGroup(navPointsLabel)]
        private int _navPointCount = 250;

        // Show nav points on navmesh
        [SerializeField]
        [BoxGroup(navPointsLabel)]
        private bool _showNavPoints = true;

        // Show origin points for each nav point?
        [SerializeField]
        [BoxGroup(navPointsLabel)]
        private bool _showOriginPoints = false;

        // Reinitialize RNG after map generation? And how?
        [SerializeField]
        [BoxGroup(rngLabel)]
        private ReInitRNG _reinitializeRNG = ReInitRNG.No;

        // Seed to use if user selected to reinitialize RNG with a specific seed
        [SerializeField]
        [BoxGroup(rngLabel)]
        [EnableIf(nameof(UseSeed))]
        private int _seed = 0;

        // ///////////////////////////////////// //
        // Instance variables not used in editor //
        // ///////////////////////////////////// //

        // List of navigation points
        [SerializeField]
        [HideInInspector]
        private List<NavPoint> _navPoints;

        // List of origin points and respective nav points, for debug purposes
        [SerializeField]
        [HideInInspector]
        private List<Origin2Nav> _navOriginsDebug;

        // List of navigation point clusters
        [SerializeField]
        [HideInInspector]
        private List<Cluster> _clusters;

        // Volume of largest piece
        [SerializeField]
        [HideInInspector]
        private float _maxPieceVol;

        // ////////// //
        // Properties //
        // ////////// //

        // Indicates if the RNG is to be initialized with a specified seed
        private bool UseSeed => _reinitializeRNG == ReInitRNG.Seed;

        /// <summary>
        /// Read-only accessor to the list of navigation points, ordered by
        /// number of connections (descending).
        /// </summary>
        public IReadOnlyList<NavPoint> NavPoints => _navPoints;

        /// <summary>
        /// Read-only accessor to the list of nav point clusters, ordered by
        /// cluster size (descending).
        /// </summary>
        public IReadOnlyList<Cluster> Clusters => _clusters;

        // /////// //
        // Methods //
        // ////// //

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

            // Initialize navigation logger
            StringBuilder log = new StringBuilder("---- Nav Validation Log ");

            // Reinitialize RNG if requested by user
            if (_reinitializeRNG != ReInitRNG.No)
            {
                int seed = _reinitializeRNG == ReInitRNG.Randomly
                    ? System.Guid.NewGuid().GetHashCode()
                    : _seed;
                Random.InitState(seed);
                log.AppendFormat("(seed = {0})", seed);
            }
            else
            {
                log.Append("(no RNG reseed)");
            }
            log.Append(" ----\n");

            // Failures in finding navigation points
            int navPointFindFailures = 0;

            // Measure how long the navigation scanning takes
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Initialize list of navigation points
            _navPoints = new List<NavPoint>(_navPointCount);

            // Initialize list for debugging origin and nav points
            _navOriginsDebug = new List<Origin2Nav>(_navPointCount);

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
                    navPointFindFailures++;
            }

            // Log any failures in finding nav points
            if (navPointFindFailures > 0)
            {
                log.AppendFormat("Unable to place {0} nav points out of {1}\n",
                    navPointFindFailures, _navPointCount);
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

            // If there are still isolated points, they should be placed in
            // their own cluster
            for (int i = 0; i < _navPoints.Count; i++)
            {
                NavPoint p = _navPoints[i];
                if (p.Isolated)
                {
                    ISet<NavPoint> newCluster = new HashSet<NavPoint>() { p };
                    navPointClusters.Add(p, newCluster);
                }
            }

            // Log good paths found vs total paths
            log.AppendFormat(
                "\tEvaluated {0} paths from {1} points, found {2} good paths ({3:p2} average), took {4} ms\n",
                tries, _navPoints.Count, success, (float)success / tries, stopwatch.ElapsedMilliseconds);

            // Get distinct clusters (sets), convert them to lists, sort them
            // by size (descending) and convert the resulting enumerable to a
            // list
            _clusters = navPointClusters.Values
                .Distinct()
                .Select(set => new Cluster(set))
                .OrderByDescending(clust => clust.Points.Count)
                .ToList();

            // Verify that the total number of points in clusters is the same
            // number of actually deployed points
            Assert.AreEqual(
                _navPoints.Count,
                _clusters.Select(clust => clust.Points.Count).Sum());

            // Log nav point clusters found
            log.AppendFormat(
                "\tA total of {0} navigation clusters were found:\n",
                _clusters.Count);
            for (int i = 0; i < _clusters.Count; i++)
            {
                log.AppendFormat("\t\tCluster {0:d2} has {1} points ({2:p2} of total)\n",
                    i,
                    _clusters[i].Points.Count,
                    _clusters[i].Points.Count / (float)_navPoints.Count);
            }

            // Show log
            Debug.Log(log.ToString());

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

            // Reset volume of largest piece
            _maxPieceVol = 0;

            foreach (MapPiece piece in mapPieces)
            {
                // Get the bounding box for the current map piece
                Bounds bounds = piece.GetComponentInChildren<MeshRenderer>().bounds;

                // Add the running volume
                runningVol += bounds.size.magnitude;

                // Does the current piece have a larger volume than the largest
                // piece volume so far?
                if (bounds.size.magnitude > _maxPieceVol)
                    _maxPieceVol = bounds.size.magnitude;

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

            // Set radius for searching nav point as the magnitude of the vector
            // that defines the width, length and height of the bounding box
            radius = bounds.size.magnitude;

            // Search for nav point using the specified radius
            pointFound = NavMesh.SamplePosition(origin, out hit, radius, NavMesh.AllAreas);

            // If a point is found, save it in the nullable
            if (pointFound)
            {
                pointInMesh = hit.position;

                // Add origin point and it's respective nav point to the
                // debug list
                _navOriginsDebug.Add(new Origin2Nav(origin, hit.position));
            }

            return pointInMesh;
        }

        /// <summary>
        /// This method should be invoked when the generated map is cleared.
        /// </summary>
        public void ClearScanner()
        {
            // Get reference to navmesh surface and remove its data
            NavMeshSurface navSurf = GetComponent<NavMeshSurface>();
            if (navSurf != null)
            {
                navSurf.RemoveData();
                navSurf.navMeshData = null;
            }

            // Discard the previously found navigation points and clusters
            _navPoints = null;
            _clusters = null;
        }

        /// <summary>
        /// Show navigation points if gizmos are on.
        /// </summary>
        private void OnDrawGizmos()
        {
            // These define the size of the debugging gizmos
            const float navSphereRadiusDiv = 130;
            const float originSphereRadiusDiv = navSphereRadiusDiv * 2;

            // Don't show anything if the generated map has been cleared
            if (_navPoints == null || _clusters == null ||
                _navPoints.Count == 0 || _clusters.Count == 0)
            {
                return;
            }

            // Show nav points?
            if (_showNavPoints)
            {
                // Are we looping through the first (largest) cluster?
                bool first = true;

                // Loop through clusters
                foreach (Cluster cluster in _clusters)
                {
                    // Points in first (largest) cluster will be green, other
                    // points will have red gizmos
                    Gizmos.color = first ? Color.green : Color.red;

                    // Loop through points in current cluster
                    foreach (NavPoint np in cluster.Points)
                    {
                        // Draw gizmo
                        Gizmos.DrawSphere(np.Point, _maxPieceVol / navSphereRadiusDiv);
                    }

                    // We're not in the first cluster anymore
                    first = false;
                }
            }

            // Show origin points?
            if (_showOriginPoints)
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < _navOriginsDebug.Count; i++)
                {
                    Gizmos.DrawSphere(
                        _navOriginsDebug[i].origin, _maxPieceVol / originSphereRadiusDiv);

                    // If we also show nav points, draw a line between each origin point
                    // and its respective nav point
                    if (_showNavPoints)
                    {
                        Gizmos.DrawLine(
                            _navOriginsDebug[i].origin, _navOriginsDebug[i].nav);
                    }
                }
            }
        }
    }
}