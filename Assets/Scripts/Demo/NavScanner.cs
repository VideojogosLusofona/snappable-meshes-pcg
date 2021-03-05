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

namespace SnapMeshPCG.Demo
{
    public class NavScanner : MonoBehaviour
    {
        [SerializeField]
        private int maxScans = 400;

        [SerializeField]
        private float _initialSearchRadius = 0.0f;

        [SerializeField]
        private float _radiusIncrement = 0.0f;

        // Represents a navigation point
        private class NavPoints
        {
            public Vector3 point;
            public int connections;
        }

        // List of navigation points
        private List<NavPoints> _navPoints;

        public void ScanMesh(IReadOnlyList<MapPiece> map)
        {
            // Number of successful paths and attempts to find them
            int success = 0;
            int tries = 0;
            float percentPassable;

            // Initialize list of navigation points
            _navPoints = new List<NavPoints>();

            // Find random points in the navmesh and add them to the list
            for (int i = 0; i < maxScans; i++)
            {
                Vector3 point1 = FindPointInNavMesh(
                    map[Random.Range(0, map.Count)].transform.position);
                _navPoints.Add(
                    new NavPoints() { point = point1, connections = 0 });
            }

            // Compare each navigation point to all others and check for a
            // valid connection
            for (int i = 0; i < _navPoints.Count; i++)
            {
                for (int j = i + 1; j < _navPoints.Count; j++)
                {
                    NavMeshPath storedPath = new NavMeshPath();
                    bool path = NavMesh.CalculatePath(
                        _navPoints[i].point,
                        _navPoints[j].point,
                        NavMesh.AllAreas,
                        storedPath);

                    // Increment number of tries
                    tries++;

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

            Debug.Log($"Scanner: Evaluated {tries} paths from {maxScans} points, found {success} good paths. -> {percentPassable:p2}");
        }

        /// <summary>
        /// Find a navigation point in the navmesh.
        /// </summary>
        /// <param name="origin">Origin position of a map piece.</param>
        /// <returns>A navigation point the given map piece.</returns>
        private Vector3 FindPointInNavMesh(Vector3 origin)
        {
            NavMeshHit hit;
            float searchRadius;
            bool foundSpot;
            int counter = 0;
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

            if (foundSpot)
            {
                return hit.position;
            }
            else
            {
                print("No point on NavMesh found.");
                return transform.position;
            }

        }

        private void OnDrawGizmos()
        {
            // GIZMOS stay on after pressing clear on generator
            if (_navPoints == null)
            {
                return;
            }

            foreach (var np in _navPoints)
            {
                Gizmos.color = (np.connections > 0) ? (Color.green) : (Color.red);
                Gizmos.DrawSphere(np.point, 0.3f);
            }
        }

    }
}