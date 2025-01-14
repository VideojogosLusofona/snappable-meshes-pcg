using SnapMeshPCG;
using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using System;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;
using UnityEngine.Rendering;
using static UnityEditor.PlayerSettings;
using UnityEditor;

public class SteinerRiverTest : MonoBehaviour
{
    public enum SpawnPointGenerationMode { Uniform, Centered };

    [SerializeField]
    private int resolution = 20;
    [SerializeField]
    private SpawnPointGenerationMode spawnPointGenerationMode;
    [SerializeField]
    private int nSpawnLocations = 5;
    [SerializeField] 
    private bool simplify = false;
    [SerializeField, Range(0.0f, 1.0f), ShowIf("simplify")]
    private float simplificationTolerance = 0.25f;
    [SerializeField, Header("Carve Options")]
    private float riverRadius = 5.0f;
    [SerializeField]
    private float riverDepth = 0.025f;
    [SerializeField]
    private float radiusScaleWithDepth = 0.9f;
    [SerializeField, Header("Debug Display")] 
    private bool displayGraph;
    [SerializeField, ShowIf(nameof(displayGraph))] private bool displayNodeID;
    [SerializeField, ShowIf(nameof(displayGraph))] private bool displayWeights;
    [SerializeField, ShowIf(nameof(displayGraph))] private bool displayTerminalNodes;
    [SerializeField, ShowIf(nameof(displayGraph))] private float nodeRadius = 0.1f;

    [Serializable]
    struct Node : IEquatable<Node>
    {
        public Node(Vector3 p) { pos = p; }

        public Vector3 pos;

        public bool Equals(Node other)
        {
            return (pos == other.pos);
        }
    }

    [SerializeField, HideInInspector]
    Graph<Node> originalGraph;
    [SerializeField, HideInInspector]
    Graph<Node> graph;
    [SerializeField, HideInInspector]
    Tree<Node> tree;

    [SerializeField, HideInInspector]
    Vector3 centerPos;

    List<int> terminalNodes;

    [Button("Build Base Graph")]
    public void BuildBaseGraph()
    {
        tree = null;
        graph = null;

        terminalNodes = new List<int>();

        BuildGraphFromTerrain();
    }

    [Button("Build Skeleton")]
    public void BuildSkeleton()
    {
        BuildBaseGraph();

        graph = SteinerTree.Build(graph, terminalNodes);

        if (simplify)
        {
            Simplify();
        }
    }

    [Button("Carve River")]
    void Carve()
    {
        Terrain terrain = GetComponent<Terrain>();
        if (terrain == null) return;

        TerrainData terrainData = terrain.terrainData;

        Undo.RegisterCompleteObjectUndo(terrainData, "Carve Riverbed");

        CarveRiverbed(terrainData, tree.rootNodeId, riverRadius);
    }

    void CarveRiverbed(TerrainData terrainData, int nodeId, float radius)
    {
        var node = tree.GetNode(nodeId);

        foreach (var childNodeId in tree.GetChildren(nodeId))
        {
            var childNode = tree.GetNode(childNodeId);
            CarveRiverbed(terrainData, node.pos, childNode.pos, radius * 0.5f, riverDepth);

            CarveRiverbed(terrainData, childNodeId, radius * radiusScaleWithDepth);
        }
    }

    public static void CarveRiverbed(TerrainData terrainData, Vector3 start, Vector3 end, float radius, float uDepth)
    {
        int heightmapWidth = terrainData.heightmapResolution;
        int heightmapHeight = terrainData.heightmapResolution;
        float[,] heights = terrainData.GetHeights(0, 0, heightmapWidth, heightmapHeight);

        // Convert world positions to heightmap positions
        Vector3 terrainSize = terrainData.size;
        Vector2 startNormalized = new Vector2(start.x / terrainSize.x, start.z / terrainSize.z);
        Vector2 endNormalized = new Vector2(end.x / terrainSize.x, end.z / terrainSize.z);

        int startX = Mathf.Clamp(Mathf.RoundToInt(startNormalized.x * (heightmapWidth - 1)), 0, heightmapWidth - 1);
        int startY = Mathf.Clamp(Mathf.RoundToInt(startNormalized.y * (heightmapHeight - 1)), 0, heightmapHeight - 1);
        int endX = Mathf.Clamp(Mathf.RoundToInt(endNormalized.x * (heightmapWidth - 1)), 0, heightmapWidth - 1);
        int endY = Mathf.Clamp(Mathf.RoundToInt(endNormalized.y * (heightmapHeight - 1)), 0, heightmapHeight - 1);

        // Iterate over the line segment using Bresenham's algorithm or linear interpolation
        int steps = Mathf.CeilToInt(Vector2.Distance(new Vector2(startX, startY), new Vector2(endX, endY)));
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            int currentX = Mathf.RoundToInt(Mathf.Lerp(startX, endX, t));
            int currentY = Mathf.RoundToInt(Mathf.Lerp(startY, endY, t));

            // Carve a U-shaped channel
            for (int y = -Mathf.CeilToInt(radius); y <= Mathf.CeilToInt(radius); y++)
            {
                for (int x = -Mathf.CeilToInt(radius); x <= Mathf.CeilToInt(radius); x++)
                {
                    int heightX = currentX + x;
                    int heightY = currentY + y;

                    if (heightX < 0 || heightX >= heightmapWidth || heightY < 0 || heightY >= heightmapHeight)
                        continue;

                    float distance = Mathf.Sqrt(x * x + y * y);
                    if (distance > radius) continue;

                    float uShape = Mathf.Clamp01(1 - (distance / radius * 2.0f));
                    float depth = Mathf.Lerp(start.y, end.y, t) / terrainSize.y;
                    heights[heightY, heightX] = Mathf.Min(heights[heightY, heightX], depth - uShape * uDepth);
                }
            }
        }

        // Apply modified heights back to the terrain
        terrainData.SetHeights(0, 0, heights);
    }

    [Button("Generate River Mesh")]
    public void GenerateRiverMesh()
    {
        Terrain terrain = GetComponent<Terrain>();
        if (terrain == null) return;

        TerrainData terrainData = terrain.terrainData;

        Mesh riverMesh = CreateRiverMesh(terrainData, tree.rootNodeId);

        MeshFilter meshFilter = GetComponentInChildren<MeshFilter>();
        MeshRenderer meshRenderer = GetComponentInChildren<MeshRenderer>();

        if ((meshFilter == null) && (meshRenderer == null))
        {
            GameObject riverObject = new GameObject("River Mesh");
            riverObject.transform.position = terrain.transform.position;
            riverObject.transform.SetParent(transform, true);

            meshFilter = riverObject.AddComponent<MeshFilter>();
            meshRenderer = riverObject.AddComponent<MeshRenderer>();
            //meshRenderer.material = riverMaterial;
        }

        meshFilter.mesh = riverMesh;
    }

    private Mesh CreateRiverMesh(TerrainData terrainData, int nodeId)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        GenerateRiverMeshRecursive(terrainData, nodeId, vertices, uvs, triangles, riverRadius);

        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            uv = uvs.ToArray(),
            triangles = triangles.ToArray()
        };

        mesh.RecalculateNormals();
        return mesh;
    }

    private void GenerateRiverMeshRecursive(
        TerrainData terrainData,
        int nodeId,
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<int> triangles,
        float radius)
    {
        var node = tree.GetNode(nodeId);

        foreach (var childNodeId in tree.GetChildren(nodeId))
        {
            var childNode = tree.GetNode(childNodeId);

            AddRiverSegment(terrainData, node.pos, childNode.pos, vertices, uvs, triangles, radius);

            GenerateRiverMeshRecursive(terrainData, childNodeId, vertices, uvs, triangles, radius * radiusScaleWithDepth);
        }
    }

    private void AddRiverSegment(
        TerrainData terrainData,
        Vector3 start,
        Vector3 end,
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<int> triangles, 
        float radius)
    {
        Vector3 direction = (end - start).normalized;
        Vector3 perpendicular = new Vector3(-direction.z, 0, direction.x) * radius;

        Vector3 v0 = start - perpendicular;
        Vector3 v1 = start + perpendicular;
        Vector3 v2 = end - perpendicular;
        Vector3 v3 = end + perpendicular;

        int index = vertices.Count;

        // Add vertices
        vertices.Add(v0);
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);

        // Add UVs
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(1, 1));

        // Add triangles
        triangles.Add(index);
        triangles.Add(index + 1);
        triangles.Add(index + 2);

        triangles.Add(index + 2);
        triangles.Add(index + 1);
        triangles.Add(index + 3);
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

    private void BuildGraphFromTerrain()
    {
        Terrain terrain = GetComponent<Terrain>();
        if (terrain == null) return;

        graph = new Graph<Node>(true);

        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainSize = terrainData.size;

        float nodeSizeX = terrainSize.x / (resolution - 1);
        float nodeSizeZ = terrainSize.z / (resolution - 1);
        int countX = resolution;
        int countZ = resolution;

        var heights = new Vector3[countX * countZ];

        int     lowestPoint = -1;
        float   lowestHeight = float.MaxValue;

        for (int z = 0; z < countZ; z++)
        {
            for (int x = 0; x < countX; x++)
            {
                float worldX = terrain.transform.position.x + x * nodeSizeX;
                float worldZ = terrain.transform.position.z + z * nodeSizeZ;

                float normalizedX = x / (float)(countX - 1);
                float normalizedZ = (z / (float)(countZ - 1));

                float height = terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
                Vector3 position = new Vector3(worldX, height, worldZ);

                graph.Add(new Node(position));

                heights[x + countX * z] = position;

                if (position.y < lowestHeight)
                {
                    lowestHeight = position.y;
                    lowestPoint = x + countX * z;
                }
            }
        }

        for (int z = 0; z < countZ; z++)
        {
            for (int x = 0; x < countX; x++)
            {
                int nodeId = x + countX * z;
                float h = heights[nodeId].y;

                for (int dz = -1; dz <= 1; dz++)
                {
                    if (z + dz < 0) continue;
                    if (z + dz >= countZ) continue;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if ((dx == 0) && (dz == 0)) continue;
                        if (x + dx < 0) continue;
                        if (x + dx >= countX) continue;

                        int otherNodeId = (x + dx) + countX * (z + dz);
                        float d = Vector3.Distance(heights[nodeId], heights[otherNodeId]);
                        float m1 = heights[otherNodeId].y / h;
                        float m2 = 1.0f / m1;

                        graph.Add(nodeId, otherNodeId, d * m1);
                        graph.Add(otherNodeId, nodeId, d * m2);
                    }
                }                
            }
        }

        int nNodes = graph.nodeCount;

        // Create spawn locations for the rivers
        terminalNodes = new();

        if (spawnPointGenerationMode == SpawnPointGenerationMode.Uniform)
        {
            for (int i = 0; i < nSpawnLocations; i++)
            {
                int r = UnityEngine.Random.Range(0, nNodes);

                if (terminalNodes.IndexOf(r) == -1)
                {
                    terminalNodes.Add(r);
                }
            }
        }
        else if (spawnPointGenerationMode == SpawnPointGenerationMode.Centered)
        {
            for (int i = 0; i < nSpawnLocations; i++)
            {
                Vector3 pos = new Vector3(terrainSize.x * 0.5f, 0.0f, terrainSize.z * 0.5f);
                pos += terrain.transform.position;
                Vector3 displacement = UnityEngine.Random.insideUnitSphere.x0z().normalized;
                displacement.x *= UnityEngine.Random.Range(terrainSize.x * 0.05f, terrainSize.x * 0.45f);
                displacement.z *= UnityEngine.Random.Range(terrainSize.z * 0.05f, terrainSize.z * 0.45f);
                pos = pos + displacement;
                pos.y = terrainData.GetInterpolatedHeight((pos.x - transform.position.x) / terrainSize.x, (pos.z - transform.position.z) / terrainSize.z);
                terminalNodes.Add(GetClosestNodeId(pos));
            }
        }

        // Add lowest point
        if (terminalNodes.IndexOf(lowestPoint) == -1)
        {
            terminalNodes.Add(lowestPoint);
        }

        originalGraph = graph;
    }

    private int Simplify_SelectPointFromCandidates(Graph<Node> graph, List<int> candidates)
    {
        // Just select the first, no criteria needed in this case
        return candidates[0];
    }

    public void Simplify()
    {
        graph.SetCentrality(ComputeCentralityFromHeight(graph, false));

        var trees = graph.BuildTrees(Graph<Node>.TreeBuildMode.Centrality, Simplify_SelectPointFromCandidates);

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

    private List<float> ComputeCentralityFromHeight(Graph<Node> graph, bool weighted)
    {
        List<float> ret = new();

        for (int i = 0; i < graph.nodeCount; i++)
        {
            var node = graph.GetNode(i);
            ret.Add(-node.pos.y);
        }

        return ret;
    }

    private bool CanSimplify(Tree<Node> tree, int grandParentId, int parentId, int nodeId)
    {
        Vector3 pos1 = tree.GetNode(nodeId).pos;
        Vector3 pos2 = tree.GetNode(parentId).pos;
        Vector3 pos3 = tree.GetNode(grandParentId).pos;

        Vector3 d1 = (pos1 - pos2).normalized;
        Vector3 d2 = (pos2 - pos3).normalized;

        float dp = Vector3.Dot(d1, d2);
        return (dp > (1.0f - simplificationTolerance));
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

                if ((displayTerminalNodes) && (originalGraph != null) && (originalGraph.nodeCount > 0) && (terminalNodes != null))
                {
                    Gizmos.color = new Color(1.0f, 1.0f, 0.0f, 1.0f);
                    foreach (var nodeId in terminalNodes)
                    {
                        var node = originalGraph.GetNode(nodeId);

                        Gizmos.DrawSphere(node.pos, nodeRadius * 1.1f);                    
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
