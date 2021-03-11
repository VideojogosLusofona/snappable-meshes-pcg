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
        // Reference to the navmesh agent component
        private NavMeshAgent _agent;

        // Reference to the nav point list
        private IReadOnlyList<NavPoint> _navPoints;

        // Start is called before the first frame update
        private void Start()
        {
            // Disable main camera, since we want to use the walker cam for the demo
            Camera.main.gameObject.SetActive(false);

            // Get a reference to the nav points list
            _navPoints = GameObject
                .Find("NavController")
                .GetComponent<NavScanner>()
                .NavPoints;

            // Get the NavMeshAgent component, and if we don't find it, add
            // a new one
            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
                _agent = gameObject.AddComponent<NavMeshAgent>();

            // Use the most connected nav point for placing the agent
            Vector3 point = _navPoints[0].Point;

            // Place the agent on the obtained point and configure it
            bool warp = _agent.Warp(point);
            if (warp)
            {
                _agent.enabled = false;
                transform.position = point;
            }
            _agent.enabled = true;
            _agent.updateRotation = true;
            _agent.updateUpAxis = true;

            // Get a new path for the agent
            GetNewPath();
        }

        // Update is called once per frame
        private void Update()
        {
            NavMeshPathStatus pathStatus = _agent.pathStatus;

            // If current path is unable to be complete, get a new one
            if (pathStatus != NavMeshPathStatus.PathComplete)
                GetNewPath();

            // If walker has reached destination, get a new path
            if (Mathf.RoundToInt(_agent.remainingDistance) == 0)
                GetNewPath();
        }

        // Determines a new path for the agent
        private void GetNewPath()
        {
            // Clear the current path
            _agent.ResetPath();

            // Get a new point from the list of navigation points
            Vector3 newTarget =
                _navPoints[Random.Range(0, _navPoints.Count)].Point;

            // Draw a debug line
            Debug.DrawLine(transform.position, newTarget, Color.green, 10);

            // Set the agent's destination to it's new target
            _agent.SetDestination(newTarget);
        }
    }
}