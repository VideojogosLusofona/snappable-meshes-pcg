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

namespace SnapMeshPCG.Navigation
{
    /// <summary>
    /// This class is responsible for spawing the walker agent.
    /// </summary>
    public class NavWalkerSpawner : MonoBehaviour
    {
        // The character that will move around the map when we enter play mode
        [SerializeField]
        private NavWalker _walker = null;

        /// <summary>
        /// Spawn the walker agent.
        /// </summary>
        /// <param name="navInfo">The navigation info.</param>
        public void SpawnWalker(INavInfo navInfo)
        {
            // Create a walker character to walk around in our generated map and
            // set it as a child of the current game object (NavController)
            NavWalker realWalker = Instantiate(_walker, transform);

            // Get the NavMeshAgent component from the walker agent
            UnityEngine.AI.NavMeshAgent navMeshAgent = realWalker.GetComponent<UnityEngine.AI.NavMeshAgent>();

            // Select the most connected nav point as the agent's spawn point
            Vector3 spawnPoint =  navInfo.NavPoints[0].Point;

            // Place the agent on the obtained point and configure it
            bool warp = navMeshAgent.Warp(spawnPoint);
            if (warp)
            {
                navMeshAgent.enabled = false;
                transform.position = spawnPoint;
            }
            navMeshAgent.enabled = true;
            navMeshAgent.updateRotation = true;
            navMeshAgent.updateUpAxis = true;
        }
    }
}