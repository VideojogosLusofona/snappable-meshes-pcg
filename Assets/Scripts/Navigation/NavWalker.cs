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
    /// Represents the AI bot that will navigate the map as a demonstration.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavWalker : MonoBehaviour
    {
        [SerializeField]
        private float _initRadius = 30;

        [SerializeField]
        private float _radiusInc = 5;

        [SerializeField]
        private int _maxIncs = 3;

        // Reference to the navmesh agent component
        private NavMeshAgent _agent;

        // Reference to the placed map pieces, sorted by placing order
        private IReadOnlyList<MapPiece> _mapPieces;

        // Reference to the NavScanner component
        private NavScanner _navScanner;

        // Possible spots where the agent can move
        private Vector3[] _possibleSpots;

        // Start is called before the first frame update
        private void Start()
        {
            // Disable main camera, since we want to use the walker cam for the demo
            Camera.main.gameObject.SetActive(false);

            // Get a reference to the placed map pieces, sorted by placing order
            _mapPieces = GameObject
                .Find("GenerationManager")
                .GetComponent<GenerationManager>()
                .PlacedPieces;

            // Get a reference to the NavScanner
            _navScanner = GameObject
                .Find("NavController")
                .GetComponent<NavScanner>();

            // Initialize possible spots
            _possibleSpots = new Vector3[_mapPieces.Count];
            for (int i = 0; i < _mapPieces.Count; i++)
                _possibleSpots[i] = _mapPieces[i].transform.position;

            // Get the NavMeshAgent component, and if we don't find it, add
            // a new one
            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
                _agent = gameObject.AddComponent<NavMeshAgent>();

            // TODO We should use the most valid point when building the nav
            // mesh
            Vector3 point = _navScanner.NavPoints[0].Point;
            // Vector3 point = _navScanner.FindPointInNavMesh(
            //     transform.position, _initRadius, _radiusInc, _maxIncs)
            //     .Value;

            bool warp = _agent.Warp(point);
            if (warp)
            {
                _agent.enabled = false;
                transform.position = point;
            }

            _agent.enabled = true;
            _agent.updateRotation = true;
            _agent.updateUpAxis = true;
            GetNewPath();
        }

        // Update is called once per frame
        private void Update()
        {
            NavMeshPathStatus pathStatus = _agent.pathStatus;

            // If Path is anything but being able to reach destination
            // get a new one
            if (pathStatus != NavMeshPathStatus.PathComplete)
                GetNewPath();

            // If walker has reached destination, get a new path
            if (Mathf.RoundToInt(_agent.remainingDistance) == 0)
                GetNewPath();
        }



        private void GetNewPath()
        {
            //print($"Possible locations: {_possibleSpots.Length}");
            _agent.ResetPath();
            Vector3 newCenter = _possibleSpots[Random.Range(0, _possibleSpots.Length)];
            Vector3 newPoint = newCenter + Random.insideUnitSphere * _initRadius;
            Vector3 newTarget = _navScanner.FindPointInNavMesh(
                newPoint, _initRadius, _radiusInc, _maxIncs)
                .Value;

            Debug.DrawLine(transform.position, newTarget, Color.green, 10);
            _agent.SetDestination(newTarget);
        }
    }
}