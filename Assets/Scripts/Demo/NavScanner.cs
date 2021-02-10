
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace SnapMeshPCG.Demo
{

    
    public class NavScanner: MonoBehaviour
    {

        [SerializeField]
        private int maxScans;

        private float _initialSearchRadius;
        private float _radiusIncrement;
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
                NavMesh.CalculatePath(start.transform.position, point, NavMesh.AllAreas, storedPath);

                NavMeshPath dummyPath = storedPath;
                if(storedPath.status != NavMeshPathStatus.PathComplete)
                {
                    
                    _badPaths.Add(dummyPath);
                }
                else
                {

                    _goodPaths.Add(dummyPath);

                }
            }

            print($"Scanner: Found {_goodPaths.Count} / {_goodPaths.Count + _badPaths.Count} good Paths.");

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
                foundSpot = NavMesh.SamplePosition(origin, out hit, searchRadius, NavMesh.AllAreas);
                counter++;

            }while(!foundSpot && counter < _maxIncrements);

            if(foundSpot)
            {
                Debug.DrawLine(transform.position, hit.position, Color.green, 10);
                print($"Found point on NavMesh at {hit.position}");
                return hit.position;
            }
            else
            {
                print($"No point on NavMesh found.");
                return transform.position;
            }

        }


    }
}