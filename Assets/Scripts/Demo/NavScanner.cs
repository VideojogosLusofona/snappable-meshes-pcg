
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace SnapMeshPCG.Demo
{

    public class NavScanner: MonoBehaviour
    {

        [SerializeField]
        private int maxScans;

        [SerializeField]
        private float _initialSearchRadius;

        [SerializeField]
        private float _radiusIncrement;

        [SerializeField]
        private float _maxIncrements;

        private List<NavMeshPath> _goodPaths;
        private List<NavMeshPath> _badPaths;
        public void ScanMesh(MapPiece[] map)
        {
            _goodPaths = new List<NavMeshPath>();
            _badPaths = new List<NavMeshPath>();
            print("Scanning NavMesh for paths...");
            MapPiece start = map[0];
            Vector3 startPoint = FindPointInNavMesh(start.transform.position);
            
            for(int i = 0; i < maxScans; i++)
            {
                Vector3 point = FindPointInNavMesh(map[Random.Range(0, map.Length)].transform.position);
                NavMeshPath storedPath = new NavMeshPath();
                bool path =NavMesh.CalculatePath(start.transform.position, point, NavMesh.AllAreas, storedPath);

                
                NavMeshPath dummyPath = storedPath;
                bool repeatPath = false;
                if(!path ||  storedPath.status != NavMeshPathStatus.PathComplete)
                {
                    //print($"Found bad path on NavMesh at {point}");
                    //Debug.DrawLine(transform.position, point, Color.red, 10);
                    foreach(NavMeshPath bp in _badPaths)
                    {
                        if(bp.corners[bp.corners.Length - 1] == dummyPath.corners[dummyPath.corners.Length - 1])
                        {
                            repeatPath = true;
                            break;
                        }
                       
                            
                    }

                     if(!repeatPath)
                    {
                        _badPaths.Add(dummyPath);
                            
                    }
                }
                else
                {
                    //print($"Found good path on NavMesh at {point} - last corner: {dummyPath.corners[dummyPath.corners.Length - 1]}");
                    //Debug.DrawLine(transform.position, point, Color.green, 10);
                    foreach(NavMeshPath gp in _goodPaths)
                    {
                        if(gp.corners[gp.corners.Length - 1] == dummyPath.corners[dummyPath.corners.Length - 1])
                        {
                            repeatPath = true;
                            break;
                        }   
                    }
                    if(!repeatPath)
                    {
                        _goodPaths.Add(dummyPath);
                            
                    }
                

                }
            }

            float percentPassable = 100 * _goodPaths.Count/(_goodPaths.Count + _badPaths.Count);
            print($"Scanner: Found {_goodPaths.Count} / {_goodPaths.Count + _badPaths.Count} good Paths. -> {percentPassable.ToString("n1")}%");

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
            if(_goodPaths == null && _badPaths == null)
            {
                return;
            }
           foreach(NavMeshPath gp in _goodPaths)
           {
               Gizmos.color = Color.green;
               Gizmos.DrawSphere(gp.corners[gp.corners.Length - 1], 0.5f);
               //print(gp.corners[gp.corners.Length - 1]);
           }
           foreach(NavMeshPath bp in _badPaths)
           {
               Gizmos.color = Color.red;
               Gizmos.DrawSphere(bp.corners[bp.corners.Length - 1], 0.5f);
           }

        }

    }
}