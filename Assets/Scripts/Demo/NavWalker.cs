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

namespace SnapMeshPCG.Demo
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavWalker : MonoBehaviour
    {
        private NavMeshAgent _agent;

        private float _initialRadius = 30;
        private float _radiusIncrement = 5;

        private float _maxIncrements = 3;

        [HideInInspector]
        public MapPiece[] mapPieces;

        private Vector3[] _possibleSpots
        {
            get
            {
                Vector3[] spots = new Vector3[mapPieces.Length];
                for(int i = 0; i < mapPieces.Length; i++)
                    spots[i] = mapPieces[i].transform.position;
                return spots;
            }
            
        }

        // Start is called before the first frame update
        void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            
            if(_agent == null)
                _agent = gameObject.AddComponent<NavMeshAgent>();
            
            Vector3 point = FindPointInNavMesh(transform.position);
            bool warp = _agent.Warp(point);
            if(warp)
            {
                _agent.enabled = false;
                transform.position =  point;
            } 
                
            _agent.enabled = true;
            print($"Agent Warped? -> {warp}");
            _agent.updateRotation = true;
            _agent.updateUpAxis = true;
            GetNewPath();
            
        }

        // Update is called once per frame
        void Update()
        {
            NavMeshPathStatus pathStatus = _agent.pathStatus;

            // If Path is anything but being able to reach destination
            // get a new one
            if(pathStatus != NavMeshPathStatus.PathComplete)
                GetNewPath();
            
            // If walker has reached destiantion, get a new path
            if(Mathf.RoundToInt(_agent.remainingDistance) == 0)
                GetNewPath();


        }

        private Vector3 FindPointInNavMesh(Vector3 origin)
        {
            NavMeshHit hit;
            float searchRadius;
            bool foundSpot; 
            int counter = 0;
            do
            {
                searchRadius = _initialRadius + _radiusIncrement * counter;
                foundSpot = NavMesh.SamplePosition(origin, out hit, searchRadius, NavMesh.AllAreas);
                counter++;

            }while(!foundSpot && counter < _maxIncrements);

            if(foundSpot)
            {
                Debug.DrawLine(transform.position, hit.position, Color.green, 10);
            //    print($"Found point on NavMesh at {hit.position}");
                return hit.position;
            }
            else
            {
             //   print($"No point on NavMesh found.");
                return transform.position;
            }

        }

        private void GetNewPath()
        {
            print($"Possible locations: {_possibleSpots.Length}");
            _agent.ResetPath();
            Vector3 newCenter = _possibleSpots[Random.Range(0, _possibleSpots.Length)];
            Vector3 newPoint = newCenter + Random.insideUnitSphere * _initialRadius;
            Vector3 newTarget = FindPointInNavMesh(newPoint);
            _agent.SetDestination(newTarget);
            

        }
    }
}