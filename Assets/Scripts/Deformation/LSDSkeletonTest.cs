using SnapMeshPCG;
using UnityEngine;
using NaughtyAttributes;
using Mono.Cecil;

[RequireComponent(typeof(LocalNavMesh))]
[RequireComponent(typeof(TopologyComponent))]
[RequireComponent(typeof(GeodesicDistanceComponent))]
public class LSDSkeletonTest : MonoBehaviour
{
    [SerializeField] private bool displayContours;
    [SerializeField] private bool displayContourGroup;
    [SerializeField] private bool displaySkeleton;

    [SerializeField, HideInInspector]
    LevelSetDiagram levelSetDiagram;

    [Button("Rebuild")]
    void Rebuild()
    {
        var navMesh = GetComponent<LocalNavMesh>();        

        var topologyComponent = GetComponent<TopologyComponent>();
        topologyComponent.Build(navMesh.GetMesh(false), transform.localToWorldMatrix);
        var topology = topologyComponent.topology;

        var geodesicDistanceComponent = GetComponent<GeodesicDistanceComponent>();
        geodesicDistanceComponent.Build(topology);
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
    }

    void DrawSkeleton(Vector3 prevPos, LevelSetDiagram.SingleContour contour)
    {
        Vector3 centerPos = contour.polyline.GetCenter();

        if (centerPos != prevPos)
        {
            UnityEditor.Handles.DrawBezier(prevPos, centerPos, prevPos, centerPos, Color.cyan, null, 5.0f);
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
