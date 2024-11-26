using SnapMeshPCG;
using UnityEngine;
using NaughtyAttributes;
using Mono.Cecil;

[RequireComponent(typeof(LocalNavMesh))]
[RequireComponent(typeof(TopologyComponent))]
[RequireComponent(typeof(GeodesicDistanceComponent))]
public class LSDSkeletonTest : MonoBehaviour
{
    [SerializeField] private float  polyhedronExtrusionAmount = 0.1f;
    [SerializeField] private int    subdivisions = 1;

    [SerializeField] private bool displayContours;
    [SerializeField] private bool displayContourGroup;
    [SerializeField] private bool displaySkeleton = true;
    [SerializeField] private bool displayMesh = true;

    [SerializeField, HideInInspector]
    LevelSetDiagram levelSetDiagram;

    Mesh workMesh;

    [Button("Rebuild")]
    void Rebuild()
    {
        var navMesh = GetComponent<LocalNavMesh>();

        workMesh = navMesh.GetMesh(false);

        for (int i = 0; i < subdivisions; i++)
        {
            workMesh = MeshTools.SubdivideMidpoint(workMesh, MeshTools.MidpointStrategy.Divide4);
        }

        var shellMesh = MeshTools.ExtrudeMesh(workMesh, Vector3.up * polyhedronExtrusionAmount, Vector3.down * polyhedronExtrusionAmount);

        workMesh = shellMesh;

        var topologyComponent = GetComponent<TopologyComponent>();
        topologyComponent.Build(workMesh, transform.localToWorldMatrix);
        var topology = topologyComponent.topology;

        var geodesicDistanceComponent = GetComponent<GeodesicDistanceComponent>();
        if (geodesicDistanceComponent.hasSourcePoint)
        {
            geodesicDistanceComponent.Build(topology, null);
        }
        else
        {
            var connector = GetComponentInChildren<Connector>();
            if (connector != null)
            {
                geodesicDistanceComponent.Build(topology, connector.transform.position);
            }
            else
            {
                Debug.LogError("No source point defined!");
                return;
            }
        }
        var geodesicDistance = geodesicDistanceComponent.geodesicDistance;

        levelSetDiagram = new LevelSetDiagram();
        levelSetDiagram.topology = topology;
        levelSetDiagram.comparisonOperator = geodesicDistance;
        levelSetDiagram.Build();
    }

    static Color[] ContourLinesColors = { Color.red, Color.yellow, Color.cyan, Color.green, Color.blue, Color.magenta, Color.white, Color.black, Color.grey };

    private void OnDrawGizmos()
    {
        if (!enabled) return;
        if (levelSetDiagram == null) return;
        if (levelSetDiagram.topology == null) return;

        if (displayContourGroup)
        {
            var topology = levelSetDiagram.topology;
            var vertices = topology.vertices;

            for (int i = 0; i < vertices.Count; i++)
            {
                int contourId = levelSetDiagram.GetVertexContour(i);
                Gizmos.color = ContourLinesColors[contourId % ContourLinesColors.Length];
                Gizmos.DrawSphere(vertices[i].position, 0.1f);
            }
        }

        if (displayContours)
        {
            if (levelSetDiagram.contours != null)
            {
                var contours = levelSetDiagram.contours;
                if (contours != null)
                {
                    for (int i = 0; i < contours.Count; i++)
                    {
                        var contourLine = contours[i];
                        Gizmos.color = ContourLinesColors[i % ContourLinesColors.Length];
                        for (int j = 0; j < contourLine.Count; j++)
                        {
                            if (contourLine[j].Count == 1)
                                Gizmos.DrawSphere(contourLine[j][0], 0.3f);
                            else
                                contourLine[j].DrawGizmos();
                        }
                    }
                }
            }
        }

        if (displaySkeleton)
        {
            LevelSetDiagram.SingleContour rootContour = levelSetDiagram.rootContour;

            if (rootContour != null)
            {
                DrawSkeleton(rootContour.polyline.GetCenter(), rootContour);
            }
        }

        if ((displayMesh) && (workMesh != null))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireMesh(workMesh);
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
            Gizmos.DrawMesh(workMesh);
        }
    }

    void DrawSkeleton(Vector3 prevPos, LevelSetDiagram.SingleContour contour)
    {
        Vector3 centerPos = contour.nodePos;

        if (centerPos != prevPos)
        {
            UnityEditor.Handles.DrawBezier(prevPos, centerPos, prevPos, centerPos, Color.magenta, null, 5.0f);
        }

        if (contour.children != null)
        {
            foreach (var child in contour.children)
            {
                DrawSkeleton(centerPos, child);
            }
        }
    }
}
