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
                path.path = localNavMesh.GetPath(path.startConnector.transform.position, path.endConnector.transform.position, true);

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
