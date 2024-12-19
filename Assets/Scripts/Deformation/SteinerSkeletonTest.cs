using SnapMeshPCG;
using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using System;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;

[RequireComponent(typeof(LocalNavMesh))]
public class SteinerSkeletonTest : MonoBehaviour
{
    public enum SourceMode { TriangleNavMesh, NavMesh, DetailMesh };
    public enum ComputeCentralityMode { Degree, Closeness, Betweenness, Eigenvector, WeightedEigenvector, Katz, WeightedKatz, Harmonic };

    [SerializeField] 
    private SourceMode source = SourceMode.TriangleNavMesh;
    [SerializeField, ShowIf(nameof(source), SourceMode.NavMesh)]
    private bool useSkips;
    [SerializeField, ShowIf(nameof(source), SourceMode.NavMesh)]
    private bool useCentroid;
    [SerializeField, ShowIf(nameof(supportSubdivs))] 
    private int subdivisions = 1;
    [SerializeField] 
    private bool simplifyWithLOS = false;
    [SerializeField, ShowIf(nameof(simplifyWithLOS))]
    private ComputeCentralityMode centralityMode = ComputeCentralityMode.Betweenness;
    [SerializeField]
    private bool rebalanceTree = true;
    [SerializeField]
    private bool useCenterPoint = false;
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

    [SerializeField, HideInInspector]
    Vector3 centerPos;
    LocalNavMesh navMeshComponent;

    List<int> terminalNodes;

    private bool supportSubdivs => (source == SourceMode.TriangleNavMesh) || (source == SourceMode.DetailMesh);

    [Button("Build Base Graph")]
    public void BuildBaseGraph()
    {
        navMeshComponent = GetComponent<LocalNavMesh>();
        if (!navMeshComponent.isInit) navMeshComponent.Build();

        tree = null;
        graph = null;

        terminalNodes = new List<int>();

        switch (source)
        {
            case SourceMode.TriangleNavMesh:
                BuildGraphFromTriangleNavMesh(terminalNodes, false);
                break;
            case SourceMode.NavMesh:
                BuildGraphFromNavMesh(terminalNodes, useSkips, false);
                break;
            case SourceMode.DetailMesh:
                BuildGraphFromTriangleNavMesh(terminalNodes, true);
                break;
            default:
                break;
        }

        if (useCenterPoint)
        {
            centerPos = Vector3.zero;
            foreach (var node in terminalNodes)
            {
                centerPos += graph.GetNode(node).pos;
            }
            centerPos /= terminalNodes.Count;

            var nodeId = GetClosestNodeId(centerPos);

            if (nodeId != -1)
            {
                // Add the node closer to the center node to the terminal nodes list
                centerPos = graph.GetNode(nodeId).pos;
                terminalNodes.Add(nodeId);
            }
        }
    }

    [Button("Build Skeleton")]
    public void BuildSkeleton()
    {
        BuildBaseGraph();

        graph = SteinerTree.Build(graph, terminalNodes);

        if (simplifyWithLOS)
        {
            SimplifyLOS(navMeshComponent);
            if (rebalanceTree)
            {
                tree.Balance();
            }
        }
    }

    private int GetClosestNodeId(Vector3 pos)
    {
        float minDist = float.MaxValue;
        int nodeId = -1;

        for (int i = 0; i < graph.nodeCount; i++)
        {
            float d = Vector3.Distance(pos, graph.GetNode(i).pos);
            if (d < minDist)
            {
                minDist = d;
                nodeId = i;
            }
        }

        return nodeId;
    }

    private void BuildGraphFromTriangleNavMesh(List<int> terminalNodes, bool useDetail)
    {
        navMeshComponent = GetComponent<LocalNavMesh>();
        if (!navMeshComponent.isInit) navMeshComponent.Build();

        var topologyComponent = GetComponent<TopologyComponent>();

        var navMesh = (useDetail) ? (navMeshComponent.GetDetailMesh()) : (navMeshComponent.GetMesh());
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

    private void BuildGraphFromNavMesh(List<int> terminalNodes, bool buildSkips, bool directed)
    {
        navMeshComponent = GetComponent<LocalNavMesh>();

        graph = new Graph<Node>(directed);
        for (uint i = 0; i < navMeshComponent.GetPolyCount(); i++)
        {
            var polyCenter = (useCentroid) ? (navMeshComponent.GetPolyCentroid(i)) : (navMeshComponent.GetPolyBoundCenter(i));
            polyCenter = navMeshComponent.GetPointInNavmesh(polyCenter);
            graph.Add(new Node(polyCenter, navMeshComponent.GetPolyNormal(i)));
        }

        for (uint i = 0; i < navMeshComponent.GetPolyCount(); i++)
        {
            var neighbours = navMeshComponent.GetNeighbours(i);
            foreach (var n in neighbours)
            {
                float d = Vector3.Distance(graph.GetNode((int)i).pos, graph.GetNode((int)n).pos);
                graph.Add((int)i, (int)n, d);
                if (directed) graph.Add((int)n, (int)i, d);
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
                            float d = Vector3.Distance(p1, p2);

                            graph.Add(i, j, d);
                            if (directed) graph.Add(j, i, d);
                        }
                    }
                }
            }
        }

        int baseNodeCount = graph.nodeCount;

        var connectors = GetComponentsInChildren<Connector>();
        foreach (var connector in connectors)
        {
            int connectorId = graph.Add(new Node(connector.transform.position, connector.transform.up));
            terminalNodes.Add(connectorId);

            // Create links to all nodes that have LOS to the closest point on the surface
            /*Vector3 pStart = navMeshComponent.GetPointInNavmesh(connector.transform.position);
            for (int i = 0; i < baseNodeCount; i++)
            {
                Vector3 pEnd = navMeshComponent.GetPointInNavmesh(graph.GetNode(i).pos);
                if (navMeshComponent.HasLOS(pStart, pEnd))
                {
                    graph.Add(connectorId, i, Vector3.Distance(connector.transform.position, pEnd));
                }
            }//*/

            // Find closest node
            /*int     closest = -1;
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

            graph.Add(connectorId, closest, Vector3.Distance(graph.GetNode(closest).pos, connector.transform.position));//*/

            var polys = navMeshComponent.GetPolysInCircle(connector.transform.position, navMeshComponent.agentRadius * 2.0f);
            foreach (var p in polys)
            {
                graph.Add((int)p, connectorId, 2.0f * Vector3.Distance(graph.GetNode((int)p).pos, connector.transform.position));
            }//*/
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
        switch (centralityMode)
        {
            case ComputeCentralityMode.Degree:
                graph.ComputeCentrality(Graph<Node>.ComputeCentralityMode.Degree);
                break;
            case ComputeCentralityMode.Closeness:
                graph.ComputeCentrality(Graph<Node>.ComputeCentralityMode.Closeness);
                break;
            case ComputeCentralityMode.Betweenness:
                graph.ComputeCentrality(Graph<Node>.ComputeCentralityMode.Betweenness);
                break;
            case ComputeCentralityMode.Eigenvector:
                graph.SetCentrality(ComputeEigenVectorCentrality(graph, false));
                break;
            case ComputeCentralityMode.WeightedEigenvector:
                graph.SetCentrality(ComputeEigenVectorCentrality(graph, true));
                break;
            case ComputeCentralityMode.Katz:
                graph.SetCentrality(ComputeKatzCentrality(graph, 0.1f, 1.0f, false));
                break;
            case ComputeCentralityMode.WeightedKatz:
                graph.SetCentrality(ComputeKatzCentrality(graph, 0.1f, 1.0f, true));
                break;
            case ComputeCentralityMode.Harmonic:
                graph.ComputeCentrality(Graph<Node>.ComputeCentralityMode.Harmonic);
                break;
            default:
                break;
        }
        var trees = graph.BuildTrees(Graph<Node>.TreeBuildMode.Centrality, SimplifyLOS_SelectPointFromCandidates);

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

        // Just consider the difference between the grandparent and the parent - if they have approximately the same slope, they can be simplified.
        tree.Simplify(CanSimplify);
        graph = null;
    }

    private List<float> ComputeEigenVectorCentrality(Graph<Node> graph, bool weighted)
    {
        var adjacencyMatrix = Matrix<float>.Build.Dense(graph.nodeCount, graph.nodeCount);
        if (weighted)
        {
            for (int n1 = 0; n1 < graph.nodeCount; n1++)
            {
                for (int n2 = 0; n2 < graph.nodeCount; n2++)
                {
                    adjacencyMatrix[n1, n2] = graph.GetWeigth(n1, n2);
                }
            }
        }
        else
        {
            for (int n1 = 0; n1 < graph.nodeCount; n1++)
            {
                for (int n2 = 0; n2 < graph.nodeCount; n2++)
                {
                    if (graph.HasLink(n1, n2)) adjacencyMatrix[n1, n2] = 1;
                }
            }
        }

        // Step 2: Compute eigenvalues and eigenvectors
        var evd = adjacencyMatrix.Evd();
        var eigenvalues = evd.EigenValues.Real();
        var eigenvectors = evd.EigenVectors;

        // Step 3: Find the index of the largest eigenvalue
        int maxEigenIndex = 0;
        for (int i = 1; i < eigenvalues.Count; i++)
        {
            if (eigenvalues[i] > eigenvalues[maxEigenIndex]) maxEigenIndex = i;
        }

        // Step 4: Extract the corresponding eigenvector
        var centralityVector = eigenvectors.Column(maxEigenIndex);

        // Step 5: Normalize the eigenvector
        var normalizedCentrality = centralityVector / centralityVector.Sum();

        // Step 6: Convert to a list and return
        return normalizedCentrality.ToList();
    }

    private List<float> ComputeKatzCentrality(Graph<Node> graph, float alpha = 0.1f, float beta = 1.0f, bool weighted = false)
    {
        var adjacencyMatrix = Matrix<float>.Build.Dense(graph.nodeCount, graph.nodeCount);
        if (weighted)
        {
            for (int n1 = 0; n1 < graph.nodeCount; n1++)
            {
                for (int n2 = 0; n2 < graph.nodeCount; n2++)
                {
                    adjacencyMatrix[n1, n2] = graph.GetWeigth(n1, n2);
                }
            }
        }
        else
        {
            for (int n1 = 0; n1 < graph.nodeCount; n1++)
            {
                for (int n2 = 0; n2 < graph.nodeCount; n2++)
                {
                    if (graph.HasLink(n1, n2)) adjacencyMatrix[n1, n2] = 1;
                }
            }
        }

        // Step 2: Create the identity matrix
        var identityMatrix = Matrix<float>.Build.DenseIdentity(graph.nodeCount);

        // Step 3: Compute (I - alpha * A)
        var katzMatrix = identityMatrix - (adjacencyMatrix * alpha);

        // Step 4: Invert the matrix
        var invertedMatrix = katzMatrix.Inverse();

        // Step 5: Create the beta vector (constant value for all nodes)
        var betaVector = Vector<float>.Build.Dense(graph.nodeCount, beta);

        // Step 6: Compute Katz centrality: (I - alpha * A)^(-1) * beta
        var centralityVector = invertedMatrix * betaVector;

        // Step 7: Normalize the centrality vector
        var normalizedCentrality = centralityVector / centralityVector.Sum();

        // Step 8: Convert to a list and return
        return normalizedCentrality.ToList();
    }

    private bool CanSimplify(Tree<Node> tree, int grandParentId, int parentId, int nodeId)
    {
        Vector3 endNodePos = tree.GetNode(nodeId).pos;

        if (tree.IsLeaf(nodeId))
        {
            // Check if end point is on the navmesh, if not we can't simplify this segment
            if (Vector3.Distance(endNodePos, navMeshComponent.GetPointInNavmesh(endNodePos)) > 0.1f)
            {
                return false;
            }
        }

        return (Vector3.Angle(tree.GetNode(grandParentId).normal, tree.GetNode(parentId).normal) < 10.0f) &&
               (navMeshComponent.HasLOS(tree.GetNode(grandParentId).pos, endNodePos, 0.0f));
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

            if (useCenterPoint)
            {
                Gizmos.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                Gizmos.DrawSphere(centerPos, nodeRadius * 2.0f);
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
        if (displayNodeID)
        {
            DebugHelpers.DrawTextAt(node.pos, Vector3.zero, 16, Color.white, $"{nodeId}", true);
        }

        foreach (var childNodeId in tree.GetChildren(nodeId))
        {
            var childNode = tree.GetNode(childNodeId);
            Gizmos.color = TreeLeveLColors[depth % TreeLeveLColors.Length];
            Gizmos.DrawLine(node.pos, childNode.pos);

            DrawTreeNode(childNodeId, depth + 1);
        }
    }
}
