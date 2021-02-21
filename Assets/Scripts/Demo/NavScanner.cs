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
        private int maxScans = 0;

        [SerializeField]
        private float _initialSearchRadius = 0.0f;

        [SerializeField]
        private float _radiusIncrement = 0.0f;

        private List<Vector3> _goodPoints;
        private List<Vector3> _badPoints;

        public void ScanMesh(MapPiece[] map)
        {
            _goodPoints = new List<Vector3>();
            _badPoints = new List<Vector3>();
            print("Scanning NavMesh for paths...");
            MapPiece start = map[0];
            Vector3 startPoint = FindPointInNavMesh(start.transform.position);
            
            for(int i = 0; i < maxScans; i++)
            {
                Vector3 point = FindPointInNavMesh(map[Random.Range(0, map.Length)].transform.position);
                NavMeshPath storedPath = new NavMeshPath();
                bool path =NavMesh.CalculatePath(startPoint, point, NavMesh.AllAreas, storedPath);

                bool repeatPath = false;
                if(!path ||  storedPath.status != NavMeshPathStatus.PathComplete)
                {

                    //Debug.DrawLine(transform.position, point, Color.red, 10);
                    foreach(Vector3 bp in _badPoints)
                    {
                        if(bp == point)
                        {
                            repeatPath = true;
                            break;
                        }
                       
                            
                    }

                     if(!repeatPath)
                    {
                        _badPoints.Add(point);
                            
                    }
                }
                else
                {
                    //print($"Found good path on NavMesh at {point} - last corner: {dummyPath.corners[dummyPath.corners.Length - 1]}");
                    //Debug.DrawLine(transform.position, point, Color.green, 10);
                    foreach(Vector3 gp in _goodPoints)
                    {
                        if(gp == point)
                        {
                            repeatPath = true;
                            break;
                        }   
                    }
                    if(!repeatPath)
                    {
                        _goodPoints.Add(point);
                            
                    }
                

                }
            }

            float percentPassable = 100 * _goodPoints.Count/(_goodPoints.Count + _badPoints.Count);
            print($"Scanner: Found {_goodPoints.Count} / {_goodPoints.Count + _badPoints.Count} good Paths. -> {percentPassable.ToString("n1")}%");

        }
        private Vector3 FindPointInNavMesh(Vector3 origin)
        {
            NavMeshHit hit;
            float searchRadius;
            bool foundSpot;
            int counter = 0;
            do
            {
                
                searchRadius = _initialSearchRadius + _radiusIncrement * counter;
                foundSpot = NavMesh.SamplePosition(origin + Random.insideUnitSphere * _initialSearchRadius, out hit, searchRadius, NavMesh.AllAreas);
                        
                counter++;

            }while(!foundSpot );

            if(foundSpot)
            {
                
                
                return hit.position;
            }
            else
            {
                print($"No point on NavMesh found.");
                return transform.position;
            }

        }

        public void ClearScanner()
        {
            _goodPoints = null;
            _badPoints = null;
        }

        private void OnDrawGizmos() 
        {
            // GIZMOS stay on after pressing clear on generator
            if(_goodPoints == null && _badPoints == null)
            {
                return;
            }

           foreach(Vector3 gp in _goodPoints)
           {
               Gizmos.color = Color.green;
               Gizmos.DrawSphere(gp, 0.3f);
               //print(gp.corners[gp.corners.Length - 1]);
           }
           foreach(Vector3 bp in _badPoints)
           {
               Gizmos.color = Color.red;
               Gizmos.DrawSphere(bp, 0.3f);
           }

        }

    }
}