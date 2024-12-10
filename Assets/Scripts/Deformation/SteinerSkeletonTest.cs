using SnapMeshPCG;
using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using System;
using JetBrains.Annotations;

[RequireComponent(typeof(LocalNavMesh))]
public class SteinerSkeletonTest : MonoBehaviour
{
    public enum SourceMode { TriangleNavMesh, NavMesh };

    [SerializeField] 
    private SourceMode source = SourceMode.TriangleNavMesh;
    [SerializeField, ShowIf(nameof(source), SourceMode.NavMesh)]
    private bool useSkips;
    [SerializeField, ShowIf(nameof(source), SourceMode.TriangleNavMesh)] 
    private int subdivisions = 1;
    [SerializeField] 
    private bool simplifyWithLOS = false;
    [SerializeField] 
    private bool displayGraph;
    [SerializeField, ShowIf(nameof(displayGraph))] private bool displayNodeID;
    [SerializeField, ShowIf(nameof(displayGraph))] private bool displayWeights;
    [SerializeField, ShowIf(nameof(displayGraph))] private float nodeRadius = 0.1f;

    [Serializable]
    struct Node : IEquatable<Node>
    {
        public Node(Vector3 p, Vector3 n) { pos = p; normal = n; }

        public Vector3 pos;
        public Vector3 normal;

        public bool Equals(Node other)
        {
            return (pos == other.pos) && (normal == other.normal);
        }
    }

    [SerializeField, HideInInspector]
    Graph<Node> graph;
    [SerializeField, HideInInspector]
    Tree<Node> tree;

    [Button("Build Skeleton")]
    public void BuildSkeleton()
    {
        var navMeshComponent = GetComponent<LocalNavMesh>();
        if (!navMeshComponent.isInit) navMeshComponent.Build();

        tree = null;
        graph = null;

        var terminalNodes = new List<int>();

        switch (source)
        {
            case SourceMode.TriangleNavMesh:
                BuildGraphFromTriangleNavMesh(terminalNodes);
                break;
            case SourceMode.NavMesh:
                BuildGraphFromNavMesh(terminalNodes, useSkips);
                break;
            default:
                break;
        }

        graph = SteinerTree.Build(graph, terminalNodes);

        if (simplifyWithLOS)
        {
            SimplifyLOS(navMeshComponent);
        }
    }

    private void BuildGraphFromTriangleNavMesh(List<int> terminalNodes)
    {
        var navMeshComponent = GetComponent<LocalNavMesh>();
        if (!navMeshComponent.isInit) navMeshComponent.Build();

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

        graph = new Graph<Node>(false);
        for (int i = 0; i < topology.triangleCount; i++)
        {
            var triCenter = topology.GetTriangleCenter(i);
            graph.Add(new Node(triCenter, topology.GetTriangleNormal(i)));
        }

        int nBase = graph.nodeCount;

        var connectors = GetComponentsInChildren<Connector>();
        foreach (var connector in connectors)
        {
            int connectorId = graph.Add(new Node(connector.transform.position, connector.transform.up));
            terminalNodes.Add(connectorId);

            int triIndex = topology.GetClosestTriangle(connector.transform.position, out float u, out float v, out float w);

            graph.Add(connectorId, triIndex, Vector3.Distance(graph.GetNode(triIndex).pos, connector.transform.position));
        }

        for (int i = 0; i < topology.triangleCount; i++)
        {
            var neighbours = topology.GetTriangleNeighbours(i);
            foreach (var n in neighbours)
            {
                graph.Add(i, n, Vector3.Distance(graph.GetNode(i).pos, graph.GetNode(n).pos));
            }
        }
    }

    private void BuildGraphFromNavMesh(List<int> terminalNodes, bool buildSkips)
    {
        var navMeshComponent = GetComponent<LocalNavMesh>();

        graph = new Graph<Node>(false);
        for (uint i = 0; i < navMeshComponent.GetPolyCount(); i++)
        {
            var polyCenter = navMeshComponent.GetPolyBoundCenter(i);
            graph.Add(new Node(polyCenter, navMeshComponent.GetPolyNormal(i)));
        }

        for (uint i = 0; i < navMeshComponent.GetPolyCount(); i++)
        {
            var neighbours = navMeshComponent.GetNeighbours(i);
            foreach (var n in neighbours)
            {
                graph.Add((int)i, (int)n, Vector3.Distance(graph.GetNode((int)i).pos, graph.GetNode((int)n).pos));
            }
        }

        if (buildSkips)
        {
            // Generate graph from LOS information
            for (int i = 0; i < navMeshComponent.GetPolyCount(); i++)
            {
                Vector3 p1 = navMeshComponent.GetPointInNavmesh(graph.GetNode(i).pos);
                Vector3 n1 = navMeshComponent.GetPointInNavmesh(graph.GetNode(i).normal);
                for (int j = i + 1; j < navMeshComponent.GetPolyCount(); j++)
                {
                    if (graph.HasLink(i, j)) continue;

                    Vector3 p2 = navMeshComponent.GetPointInNavmesh(graph.GetNode(j).pos);
                    Vector3 n2 = navMeshComponent.GetPointInNavmesh(graph.GetNode(j).normal);
                    if (Vector3.Angle(n1, n2) < 10.0f)
                    {
                        if (navMeshComponent.HasLOS(p1, p2))
                        {
                            graph.Add(i, j, Vector3.Distance(p1, p2));
                        }
                    }
                }
            }
        }

        var connectors = GetComponentsInChildren<Connector>();
        foreach (var connector in connectors)
        {
            int connectorId = graph.Add(new Node(connector.transform.position, connector.transform.up));
            terminalNodes.Add(connectorId);

            // Find closest node
            int     closest = -1;
            float   minDist = float.MaxValue;
            for (int i = 0; i < graph.nodeCount - 1; i++)
            {
                float d = Vector3.Distance(graph.GetNode(i).pos, connector.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    closest = i;
                }
            }

            graph.Add(connectorId, closest, Vector3.Distance(graph.GetNode(closest).pos, connector.transform.position));
        }
    }

    private int SimplifyLOS_SelectPointFromCandidates(Graph<Node> graph, List<int> candidates)
    {
        Vector3 center = Vector3.zero;
        for (int i = 0; i < graph.nodeCount; i++)
        {
            center += graph.GetNode(i).pos;
        }
        center /= graph.nodeCount;

        int ret = -1;
        float minDist = float.MaxValue;
        foreach (var candidate in candidates)
        {
            float d = Vector3.Distance(center, graph.GetNode(candidate).pos);
            if (d < minDist)
            {
                minDist = d;
                ret = candidate;
            }
        }

        return ret;
    }

    public void SimplifyLOS(LocalNavMesh navMeshComponent)
    {
        var trees = graph.BuildTrees(Graph<Node>.TreeBuildMode.HighestDegree, SimplifyLOS_SelectPointFromCandidates);

        if (trees.Count == 0)
        {
            Debug.LogError("Can't generate trees from graph!");
            return;
        }
        if (trees.Count > 1)
        {
            Debug.LogError($"Disjointed trees not supported ({trees.Count} generated!)");
            return;
        }

        tree = trees[0];

        tree.Simplify((tree, grandParentId, parentId, nodeId) => 
                        (Vector3.Angle(tree.GetNode(grandParentId).normal, tree.GetNode(parentId).normal) < 10.0f) &&
                        (Vector3.Angle(tree.GetNode(parentId).normal, tree.GetNode(nodeId).normal) < 10.0f) &&
                        (Vector3.Angle(tree.GetNode(grandParentId).normal, tree.GetNode(nodeId).normal) < 10.0f) &&
                        (navMeshComponent.HasLOS(tree.GetNode(grandParentId).pos, tree.GetNode(nodeId).pos)));

        graph = null;
    }

    static Color[] TreeLeveLColors = { Color.red, Color.yellow, Color.cyan, Color.green, Color.magenta, Color.white };

    private void OnDrawGizmosSelected()
    {
        if (displayGraph)
        {
            if (graph != null)
            {
                Gizmos.color = new Color(0.0f, 1.0f, 0.0f, 0.5f);
                for (int i = 0; i < graph.nodeCount; i++)
                {
                    var node = graph.GetNode(i);

                    Gizmos.DrawSphere(node.pos, nodeRadius);
                    if (displayNodeID)
                    {
                        DebugHelpers.DrawTextAt(node.pos, Vector3.zero, 16, Color.white, $"{i}", true);
                    }
                }

                Gizmos.color = Color.cyan;
                for (int j = 0; j < graph.edgeCount; j++)
                {
                    var edge = graph.GetEdge(j);
                    if (edge == null) continue;
                    var n1 = graph.GetNode(edge.i1);
                    var n2 = graph.GetNode(edge.i2);

                    Vector3 delta = n2.pos - n1.pos;
                    float deltaMag = delta.magnitude;
                    Vector3 dir = delta / deltaMag;

                    Vector3 p1 = n1.pos + dir * nodeRadius;
                    Vector3 p2 = n2.pos - dir * nodeRadius;

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
            if ((tree != null) && (tree.rootNodeId != -1) && (tree.rootNodeId < tree.nodeCount))
            {
                DrawTreeNode(tree.rootNodeId, 0);
            }
        }
    }

    void DrawTreeNode(int nodeId, int depth)
    {
        float radius = nodeRadius;
        if (tree.rootNodeId == nodeId) radius *= 2.0f;

        var node = tree.GetNode(nodeId);

        Gizmos.color = TreeLeveLColors[depth % TreeLeveLColors.Length].ChangeAlpha(0.5f);
        Gizmos.DrawSphere(node.pos, radius);

        foreach (var childNodeId in tree.GetChildren(nodeId))
        {
            var childNode = tree.GetNode(childNodeId);
            Gizmos.color = TreeLeveLColors[depth % TreeLeveLColors.Length];
            Gizmos.DrawLine(node.pos, childNode.pos);

            DrawTreeNode(childNodeId, depth + 1);
        }
    }
}
