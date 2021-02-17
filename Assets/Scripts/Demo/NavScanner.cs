
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace SnapMeshPCG.Demo
{

    public class NavScanner : MonoBehaviour
    {

        [SerializeField]
        private int maxScans;

        [SerializeField]
        private float _initialSearchRadius;

        [SerializeField]
        private float _radiusIncrement;

        [SerializeField]
        private float _maxIncrements;

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