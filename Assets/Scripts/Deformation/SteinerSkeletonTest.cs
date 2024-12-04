using SnapMeshPCG;
using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;

[RequireComponent(typeof(LocalNavMesh))]
[RequireComponent(typeof(TopologyComponent))]
public class SteinerSkeletonTest : MonoBehaviour
{
    [SerializeField] private int subdivisions = 1;
    [SerializeField] private bool displayGraph;
    [SerializeField, ShowIf(nameof(displayGraph))] private bool displayNodeID;
    [SerializeField, ShowIf(nameof(displayGraph))] private bool displayWeights;
    [SerializeField, ShowIf(nameof(displayGraph))] private float nodeRadius = 0.1f;

    Graph<Vector3> graph;

    [Button("Build Skeleton")]
    void BuildSkeleton()
    {
        var navMeshComponent = GetComponent<LocalNavMesh>();
        var topologyComponent = GetComponent<TopologyComponent>();

        var navMesh = navMeshComponent.GetMesh();
        if (subdivisions > 0)
        {
            for (int i = 0; i < subdivisions; i++)
            {
                navMesh = MeshTools.SubdivideMidpoint(navMesh, MeshTools.MidpointStrategy.Divide4);
            }
        }

        topologyComponent.Build(navMesh, Matrix4x4.identity);

        var topology = topologyComponent.topology;

        graph = new Graph<Vector3>(false);
        for (int i = 0; i < topology.triangleCount; i++)
        {
            var triCenter = topology.GetTriangleCenter(i);
            graph.Add(triCenter);
        }

        int nBase = graph.nodeCount;

        var terminalNodes = new List<int>();
        var connectors = GetComponentsInChildren<Connector>();
        foreach (var connector in connectors)
        {
            int connectorId = graph.Add(connector.transform.position);
            terminalNodes.Add(connectorId);

            int triIndex = topology.GetClosestTriangle(connector.transform.position, out float u, out float v, out float w);

            graph.Add(connectorId, triIndex, Vector3.Distance(graph.GetNode(triIndex), connector.transform.position));
        }

        for (int i = 0; i < topology.triangleCount; i++)
        {
            var neighbours = topology.GetTriangleNeighbours(i);
            foreach (var n in neighbours)
            {
                graph.Add(i, n, Vector3.Distance(graph.GetNode(i), graph.GetNode(n)));
            }
        }

        graph = SteinerTree.Build(graph, terminalNodes);
    }

    private void OnDrawGizmosSelected()
    {
        if ((graph != null) && (displayGraph))
        {
            Gizmos.color = new Color(0.0f, 1.0f, 0.0f, 0.5f);
            for (int i = 0; i < graph.nodeCount; i++)
            {
                var node = graph.GetNode(i);
                if (node == null) continue;

                Gizmos.DrawSphere(node, nodeRadius);
                if (displayNodeID)
                {
                    DebugHelpers.DrawTextAt(node, Vector3.zero, 16, Color.white, $"{i}", true);
                }
            }

            Gizmos.color = Color.cyan;
            for (int j = 0; j < graph.edgeCount; j++)
            {
                var edge = graph.GetEdge(j);
                if (edge == null) continue;
                var n1 = graph.GetNode(edge.i1);
                var n2 = graph.GetNode(edge.i2);

                Vector3 delta = n2 - n1;
                float deltaMag = delta.magnitude;
                Vector3 dir = delta / deltaMag;

                Vector3 p1 = n1 + dir * nodeRadius;
                Vector3 p2 = n2 - dir * nodeRadius;

                if (graph.isDirected)
                {
                    Vector3 d = (p2 - p1);
                    float mag = d.magnitude;
                    d /= mag;
                    DebugHelpers.DrawArrow(p1, d, mag, 0.05f * mag, 45.0f);
                }
                else
                {
                    Gizmos.DrawLine(p1, p2);
                }

                if (displayWeights)
                {
                    DebugHelpers.DrawTextAt((p1 + p2) * 0.5f, Vector3.zero, 14, Color.blue, $"{edge.weight}", false);
                }
            }
        }
    }
}
