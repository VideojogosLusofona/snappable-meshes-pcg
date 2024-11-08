using UnityEngine;
using SnapMeshPCG;
using NaughtyAttributes;
using System.Collections.Generic;
using Mono.Cecil;
using System.IO;

public class PathSkeletonGeneration : MonoBehaviour
{
    // Needed for the navmesh generation configuration
    [SerializeField] 
    private NavMeshGeneratorConfig navMeshConfig;
    [SerializeField] bool runSimulationOnStart = true;
    [SerializeField] float gravityConstant = 1.0f;
    [SerializeField] float maxVelocity = float.MaxValue;
    [SerializeField] float planarAngularTolerance = 10.0f;
    [SerializeField] bool useLOS = true;
    [SerializeField] bool useSegmentIntegrity = false;
    [SerializeField] int nSubsteps = 5;
    [SerializeField] float minDist = 1e-3f;
    [SerializeField] float maxDist = float.MaxValue;
    [SerializeField] float mergeDist = 1e-3f;
    [SerializeField] float timeStep = 0.01f;
    [SerializeField] int maxSteps = 1000;

    [SerializeField, Header("Debug Options")]
    private bool showPaths = true;
    [SerializeField, ShowIf("showPaths")]
    private int debugPath = -1;
    [SerializeField]
    private bool showCenter = true;

    private Mesh            navigationMesh;
    private LocalNavMesh    localNavMesh;
    private Vector3?        center;

    class Path
    {
        public Connector startConnector;
        public Connector endConnector;
        public Polyline  path;
    }
    List<Path>      paths;

    [Button("Setup")]
    public void Setup()
    {
        localNavMesh = GetComponent<LocalNavMesh>();
        if (localNavMesh == null)
        {
            var config = navMeshConfig;
            if (config == null)
            {
                var autoPieceGeneration = GetComponent<AutoPieceGeneration>();
                if ((autoPieceGeneration != null) && (autoPieceGeneration.config != null))
                {
                    config = autoPieceGeneration.config.navMeshProperties;
                }
            }
            if (config == null)
            {
                Debug.Log("<color=#FFFF00>Can't setup deformation - nav mesh properties are not setup!<color=#A0A0A0>");
                return;
            }

            localNavMesh = gameObject.AddComponent<LocalNavMesh>();
            localNavMesh.SetNavMeshConfig(config);
        }

        localNavMesh.Build();

        navigationMesh = localNavMesh.GetMesh();

        BuildSkeleton();
    }

    [Button("Path Skeleton Generation: Build")]
    void BuildSkeleton()
    {
        GeneratePaths();
        ComputeCenter();

        Debug.Log($"Finished bulding path skeleton...");
    }

    [Button("Path Skeleton Generation: Generate Paths")]
    void GeneratePaths()
    {
        if (localNavMesh == null) return;

        paths = new();

        var connectors = GetComponentsInChildren<Connector>();
        for (int i = 0; i < connectors.Length; i++)
        {
            for (int j = i + 1; j < connectors.Length; j++)
            {
                Path path = new Path();
                path.startConnector = connectors[i];
                path.endConnector = connectors[j];
                path.path = localNavMesh.GetPath(path.startConnector.transform.position, path.endConnector.transform.position, true, true);

                paths.Add(path);
            }
        }
    }

    [Button("Path Skeleton Generation: Compute Center")]
    void ComputeCenter()
    {
        center = Vector3.zero;

        int nPaths = 0;
        foreach (var path in paths)
        {
            var polyline = path.path;
            if (polyline == null) continue;

            center += polyline.GetCenter();
            nPaths++;
        }
        if (nPaths > 0)
        {
            center = center / nPaths;

            // Project center onto navmesh
            center = localNavMesh.GetPointInNavmesh(center.Value);
        }
    }

    [Button("Path Skeleton Generation: Run Gravity Clustering")]
    void RunGravityClustering()
    {
        UnityEditor.EditorUtility.DisplayProgressBar("Building...", "Gravity Clustering: Setup, generating mesh octree", 0.0f);

        MeshOctreeComponent meshOctree = null;

        if ((useLOS) || (useSegmentIntegrity))
        {
            meshOctree = GetComponent<MeshOctreeComponent>();
            if (meshOctree == null)
            {
                meshOctree = gameObject.AddComponent<MeshOctreeComponent>();
            }
            meshOctree.Build();
        }

        UnityEditor.EditorUtility.DisplayProgressBar("Building...", "Gravity Clustering: Setup, setup simulation", 0.0f);

        var simulation = new GravitySimulation();
        simulation.gravityConstant = gravityConstant;
        simulation.maxVelocity = maxVelocity;
        simulation.minDist = minDist;
        simulation.maxDist = maxDist;
        simulation.mergeDistance = mergeDist;
        simulation.groupSelfInfluence = false;
        simulation.planarAngularTolerance = planarAngularTolerance;
        if (useLOS || useSegmentIntegrity)
        {
            simulation.validPairCallback = (p1, p2) =>
            {
                Triangle hitInfo = null;
                float hitT = float.MaxValue;
                return !meshOctree.Linecast(p1.position + Vector3.up * 0.05f, p2.position + Vector3.up * 0.05f, ref hitInfo, ref hitT);
            };
        }
        if (useSegmentIntegrity)
        {
            simulation.canMoveCallback = (pt, deltaTime) =>
            {
                Triangle hitInfo = null;
                float hitT = float.MaxValue;
                var allPoints = simulation.GetPointsInSameGroup(pt.groupId, true);
                foreach (var otherP in allPoints)
                {
                    if (otherP == pt) continue;

                    if (meshOctree.Linecast(pt.position + pt.velocity * deltaTime + Vector3.up * 0.05f, otherP.position + Vector3.up * 0.05f, ref hitInfo, ref hitT))
                    {
                        return false;
                    }
                }
                return true;
            };
        }

        int nPaths = 0;
        foreach (var path in paths)
        {
            var polyline = path.path;
            if (polyline == null) continue;

            bool locked = true;
            foreach (var p in polyline)
            {
                simulation.AddPoint(p.position, p.normal, nPaths, 1, locked);
                locked = false;
            }
            simulation.SetLocked(simulation.nPoints - 1, true);

            nPaths++;
        }

        int currentSteps = 0;
        while (currentSteps < maxSteps)
        {
            UnityEditor.EditorUtility.DisplayProgressBar("Building...", $"Gravity Clustering: Running ({currentSteps}/{maxSteps} steps)", (float)currentSteps/(float)maxSteps);

            currentSteps++;
            simulation.Step(timeStep, Mathf.Max(1, nSubsteps));
            if (simulation.totalDelta < 1e-3)
            {
                break;
            }
            if (simulation.totalDelta < 1e-3)
            {
                break;
            }
        }

        UnityEditor.EditorUtility.ClearProgressBar();

        if (currentSteps < maxSteps)
        {
            Debug.Log($"Completed gravity clustering in {currentSteps} steps...");
        }
        else
        {
            Debug.LogWarning($"Failed to complete gravity clustering after {currentSteps} steps...");
        }
    }

    [Button("Path Skeleton Generation: Build Gravity Clustering Experiment")]
    void BuildGravityClusteringExperiment()
    {
        // Find an object with the right name
        GameObject go = GameObject.Find($"{name} - Experiment");
        if (go != null)
        {
            DestroyImmediate(go);
        }
        go = new GameObject();
        go.name = $"{name} - Experiment";
        GravityTest experiment = go.AddComponent<GravityTest>();
        experiment.gravityConstant = gravityConstant;
        experiment.maxVelocity = maxVelocity;
        experiment.planarAngularTolerance = planarAngularTolerance;
        if (useLOS)
        {
            if (useSegmentIntegrity) experiment.intersectionMode = GravityTest.IntersectionMode.SegmentIntegrity;
            else experiment.intersectionMode = GravityTest.IntersectionMode.LOS;
        }
        else experiment.intersectionMode = GravityTest.IntersectionMode.None;
        experiment.nSubsteps = nSubsteps;
        experiment.minDist = minDist;
        experiment.maxDist = maxDist;
        experiment.mergeDist = mergeDist;
        experiment.timeStep = timeStep;
        experiment.autoMaxSteps = maxSteps;

        int nPaths = 0;
        foreach (var path in paths)
        {
            var polyline = path.path;
            if (polyline == null) continue;

            GravityPointForTesting prevPoint = null;

            for (int i = 0; i < polyline.Count; i++)
            {                
                GameObject pointObj = new GameObject();
                pointObj.name = $"Point {i}/Segment {nPaths}";
                pointObj.transform.SetParent(go.transform);
                pointObj.transform.position = polyline[i];
                pointObj.transform.rotation = Quaternion.LookRotation(Vector3.forward, polyline.GetNormal(i));
                var gravityPoint = pointObj.AddComponent<GravityPointForTesting>();
                gravityPoint.group = nPaths;
                gravityPoint.mass = 1.0f;
                gravityPoint.locked = (i == 0) || (i == polyline.Count - 1);
                
                if (prevPoint)
                {
                    prevPoint.AddLink(gravityPoint);
                }

                prevPoint = gravityPoint;
            }

            nPaths++;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if ((showPaths) && (paths != null))
        {
            for (int i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                if (path == null) continue;

                if ((debugPath == -1) || (debugPath == i))
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(path.startConnector.transform.position, 0.25f);
                    Gizmos.DrawSphere(path.endConnector.transform.position, 0.25f);

                    if (path.path != null)
                    {
                        for (int j = 1; j < path.path.Count - 1; j++)
                        {
                            Gizmos.color = Color.Lerp(Color.cyan, Color.blue, (float)j / (float)(path.path.Count - 1));
                            Gizmos.DrawSphere(path.path[j], 0.125f);
                        }
                        for (int j = 0; j < path.path.Count - 1; j++)
                        {
                            Gizmos.color = Color.yellow;
                            Gizmos.DrawLine(path.path[j], path.path[j + 1]);
                        }
                    }
                }
            }
        }
        if ((showCenter) && (center.HasValue))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(center.Value, 0.25f);
        }
    }
}
