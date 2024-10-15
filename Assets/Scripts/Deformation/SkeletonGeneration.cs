using UnityEngine;
using SnapMeshPCG;
using NaughtyAttributes;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.ProBuilder;
using static UnityEditor.Searcher.Searcher.AnalyticsEvent;
using System;

public class SkeletonGeneration : MonoBehaviour
{
    public enum SkeletonGenerationType { Straight };

    // Needed for the navmesh generation configuration
    [SerializeField] 
    private NavMeshGeneratorConfig navMeshConfig;
    [SerializeField] 
    private SkeletonGenerationType    genType = SkeletonGenerationType.Straight;
    [SerializeField, Header("Debug Options")]
    private bool displayNavmesh = false;
    [SerializeField]
    private bool displayBoundary = false;
    [SerializeField]
    private bool displayVertices = false;
    [SerializeField]
    private bool displayBorderOverTime = false;
    [SerializeField, ShowIf("displayBorderOverTime")]
    private float debugTime = 0.0f;
    [SerializeField, ShowIf("displayBorderOverTime")]
    private int debugEdge = -1;
    [SerializeField, ShowIf("displayBorderOverTime")]
    private int debugVertex = -1;


    private Mesh        navigationMesh;
    private Topology    meshTopology;
    private Boundary    navigationBoundaries;

    struct Edge
    {
        public int i1;
        public int i2;

        public bool Uses(int index) => (i1 == index) || (i2 == index);
    }

    class Vertex
    {
        public Vector3      startPos;
        public Vector3      normal;
        public Vector3      innerDirection;
    }

    List<Vertex>     vertices;
    List<Edge>       edges;

    [Button("Setup Deformation")]
    public void SetupDeformation()
    {
        var navMesh = GetComponent<LocalNavMesh>();
        if (navMesh == null)
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
                Debug.LogError("Can't setup deformation - nav mesh properties are not setup!");
                return;
            }

            navMesh = gameObject.AddComponent<LocalNavMesh>();
            navMesh.SetNavMeshConfig(config);
        }

        navMesh.Build();

        navigationMesh = navMesh.GetMesh(transform.worldToLocalMatrix);

        meshTopology = new Topology(navigationMesh, Matrix4x4.identity);
        meshTopology.ComputeTriangleNormals();
        navigationBoundaries = meshTopology.GetBoundaries();

        BuildSkeleton();
    }

    void BuildSkeleton()
    {
        BuildVertices();

        ComputeEventTime();
    }

    Vector3 ProjectOntoPlane(Vector3 vector, Vector3 planeNormal)
    {
        return vector - Vector3.Dot(vector, planeNormal) * planeNormal;
    }

    bool CalculateEdgeCollapseTime(Vector3 p1Start, Vector3 p1Dir, Vector3 p2Start, Vector3 p2Dir, out float t)
    {
        // Relative position and direction
        Vector3 P0 = p2Start - p1Start;
        Vector3 D = p2Dir - p1Dir;

        // Check if P0 and D are parallel (cross product should be zero)
        if (Vector3.Cross(P0, D) == Vector3.zero)
        {
            // Calculate the time to collapse
            t = -Vector3.Dot(P0, D) / D.sqrMagnitude;
            
            return true;
        }

        t = float.MaxValue;
        return false;
    }

    void ComputeEventTime()
    {
        float edgeCollapseTime = ComputeEdgeCollapseTime();
        float vertexSplitTime = ComputeVertexSplitTime();
    }

    float ComputeEdgeCollapseTime()
    {
        float tMinCollapse = float.MaxValue;

        // Find first edge collapse event
        for (int i = 0; i < edges.Count; i++)
        {
            var edge = edges[i];

            if (i == debugEdge)
            {
                int a = 10;
            }

            // Get the vertices of the edge
            Vertex v1 = vertices[edge.i1];
            Vertex v2 = vertices[edge.i2];

            // Compute the vector between the two vertices
            Vector3 edgeVector = v2.startPos - v1.startPos;

            // Compute the plane normal (using cross product of edge vector and innerDirection)
            // Not sure if this v1.innerDirection should be used, or if the average of the two innerDirections
            Vector3 planeNormal = Vector3.Cross(edgeVector, v1.innerDirection).normalized;

            // Project the start positions and inner directions onto the plane
            Vector3 p1 = ProjectOntoPlane(v1.startPos, planeNormal);
            Vector3 d1 = ProjectOntoPlane(v1.innerDirection, planeNormal);

            Vector3 p2 = ProjectOntoPlane(v2.startPos, planeNormal);
            Vector3 d2 = ProjectOntoPlane(v2.innerDirection, planeNormal);

            if (CalculateEdgeCollapseTime(p1, d1, p2, d2, out float t))
            {
                if ((t > 0) && (t < tMinCollapse))
                {
                    tMinCollapse = t;
                }
            }
        }

        Debug.Log($"Next edge collapse = {tMinCollapse}");

        return tMinCollapse;
    }

    static (float?, float?) QuadricRoots(float A, float B, float C)
    {
        if (Mathf.Abs(A) < 1e-3)
        {
            // No roots available
            return (null, null);
        }
        float discriminant = (B * B) - 4 * A * C;
        if (discriminant < 0)
        {
            // Only imaginary roots
            return (null, null);
        }
        float D = Mathf.Sqrt(discriminant);

        return ((-B - D) / (2 * A), (-B + D) / (2 * A));
    }
  
    static (float A, float B, float C) ComputeIntersectionCoeffs(Vector3 P0, Vector3 D0, Vector3 P1, Vector3 D1, Vector3 P2, Vector3 D2, int i1, int i2)
    {
        var A = (P1 - P0);
        var B = (P2 - P1);
        var C = (D0 - D1);
        var D = (D1 - D2);

        float Ax = A[i1], Ay = A[i2];
        float Bx = B[i1], By = B[i2];
        float Cx = C[i1], Cy = C[i2];
        float Dx = D[i1], Dy = D[i2];

        float cA = Bx * Dy - By * Dx;
        float cB = Ax * Dy + Bx * Cy - Ay * Dx - By * Cx;
        float cC = Ax * Cy - Ay * Cx;

        return (cA, cB, cC);
    }

    static bool CheckIntersection(Vector3 P0, Vector3 D0, Vector3 P1, Vector3 D1, Vector3 P2, Vector3 D2, out float T, out float theta)
    {
        // Initialize outputs
        T = 0f;
        theta = 0f;

        // Try X = Z
        int i1 = 0, i2 = 2, i3 = 1;
        (float A, float B, float C) = ComputeIntersectionCoeffs(P0, D0, P1, D1, P2, D2, i1, i2);

        var roots = QuadricRoots(A, B, C);
        if (roots.Item1 == null)
        {
            // Failed with X = Z, try X = Y
            i1 = 0; i2 = 1; i3 = 2;
            (A, B, C) = ComputeIntersectionCoeffs(P0, D0, P1, D1, P2, D2, i1, i2);

            roots = QuadricRoots(A, B, C);
            if (roots.Item1 == null)
            {
                // Failed with X = Z, try Y = Z
                i1 = 1; i2 = 2; i3 = 0;
                (A, B, C) = ComputeIntersectionCoeffs(P0, D0, P1, D1, P2, D2, i1, i2);

                roots = QuadricRoots(A, B, C);
                if (roots.Item1 == null)
                {
                    return false;
                }
            }
        }

        float t1 = -float.MaxValue;
        if ((roots.Item1.HasValue) && (roots.Item1.Value >= 0) && (roots.Item1.Value <= 1.0f))
        {
            int idx = i1;
            float th = roots.Item1.Value;
            t1 = D0[idx] - D1[idx] - th * (D2[idx] - D1[idx]);
            if (Mathf.Abs(t1) < 1e-3) { idx = i2; t1 = D0[idx] - D1[idx] - th * (D2[idx] - D1[idx]); }
            if (Mathf.Abs(t1) < 1e-3) { idx = i3; t1 = D0[idx] - D1[idx] - th * (D2[idx] - D1[idx]); }
            if (Mathf.Abs(t1) < 1e-3) t1 = -float.MaxValue;
            else t1 = (P1[idx] - P0[idx] + th * (P2[idx] - P1[idx])) / t1;
        }

        float t2 = -float.MaxValue;
        if ((roots.Item2.HasValue) && (roots.Item2.Value >= 0) && (roots.Item2.Value <= 1.0f))
        {
            int idx = i1;
            float th = roots.Item2.Value;
            t2 = D0[idx] - D1[idx] - th * (D2[idx] - D1[idx]);
            if (Mathf.Abs(t2) < 1e-3) { idx = i2; t2 = D0[idx] - D1[idx] - th * (D2[idx] - D1[idx]); }
            if (Mathf.Abs(t2) < 1e-3) { idx = i3; t2 = D0[idx] - D1[idx] - th * (D2[idx] - D1[idx]); }
            if (Mathf.Abs(t2) < 1e-3) t2 = -float.MaxValue;
            else t2 = (P1[idx] - P0[idx] + th * (P2[idx] - P1[idx])) / t2;
        }

        if (t1 != -float.MaxValue)
        {
            if (t2 != -float.MaxValue)
            {
                if (t1 < t2)
                {
                    theta = roots.Item1.Value;
                    T = t1;
                }
                else
                {
                    theta = roots.Item2.Value;
                    T = t2;
                }
            }
            else
            {
                theta = roots.Item1.Value;
                T = t1;
            }
        }
        else
        {
            if (t2 == -float.MaxValue)
            {
                return false;
            }
            else
            {
                theta = roots.Item2.Value;
                T = t2;
            }
        }

        return true; // Solution found
    }

    float ComputeVertexSplitTime()
    { 
        float tMinIntersection = float.MaxValue;

        // Check for vertex split events
        for (int eIndex = 0; eIndex < edges.Count; eIndex++)
        {
            var edge = edges[eIndex];
            for (int index = 0; index < vertices.Count; index++) 
            {
                if (edge.Uses(index)) continue;

                // Compute a plane normal to use as projection plane
                Vector3 planeNormal = (vertices[index].normal + vertices[edge.i1].normal + vertices[edge.i2].normal).normalized;

                // Project points and directions onto plane
                Vector3 p0 = ProjectOntoPlane(vertices[index].startPos, planeNormal);
                Vector3 d0 = ProjectOntoPlane(vertices[index].innerDirection, planeNormal);
                Vector3 p1 = ProjectOntoPlane(vertices[edge.i1].startPos, planeNormal);
                Vector3 d1 = ProjectOntoPlane(vertices[edge.i1].innerDirection, planeNormal);
                Vector3 p2 = ProjectOntoPlane(vertices[edge.i2].startPos, planeNormal);
                Vector3 d2 = ProjectOntoPlane(vertices[edge.i2].innerDirection, planeNormal);//*/

                if ((debugEdge == eIndex) && 
                    (debugVertex == index))
                {
                    int a = 10;
                }

                // Find intersection
                //if (CheckIntersection(P0, D0, P2, D2, out float t, out float theta))
                if (CheckIntersection(p0, d0, p1, d1, p2, d2, out float t, out float theta))
                {
                    if (t >= 0.0f)
                    {
                        Debug.Log($"Edge {eIndex} ({edge.i1}, {edge.i2}) intersects vertex {index} at t = {t}");
                    }
                    if ((t < tMinIntersection) && (t >= 0))
                    {
                        tMinIntersection = t;
                    }
                }
            }
        }

        Debug.Log($"tMinIntersection = {tMinIntersection}");
        
        return tMinIntersection;
    }

    void BuildVertices()
    {
        vertices = new();
        edges = new();

        // Create vertex list with edge list
        for (int bCounter = 0; bCounter < navigationBoundaries.Count; bCounter++)
        {
            var boundary = navigationBoundaries[bCounter];

            int baseIndex = vertices.Count;

            for (int vCounter = 0; vCounter < boundary.Count; vCounter++)
            {
                var vertex = new Vertex
                {
                    startPos = boundary[vCounter],
                    normal = boundary.GetNormal(vCounter),
                    innerDirection = Vector3.zero
                };

                edges.Add(new Edge { i1 = baseIndex + vCounter, i2 = baseIndex + (vCounter + 1) % boundary.Count });

                vertices.Add(vertex);
            }
        }

        ComputeVertexDirection();
    }

    void ComputeVertexDirection()
    {
        foreach (var edge in edges)
        {
            Vector3 edgeTangent = (vertices[edge.i2].startPos - vertices[edge.i1].startPos).normalized;
            vertices[edge.i1].innerDirection += Vector3.Cross(vertices[edge.i1].normal, edgeTangent);
            vertices[edge.i2].innerDirection += Vector3.Cross(vertices[edge.i2].normal, edgeTangent);
        }

        foreach (var vertex in vertices)
        {
            vertex.innerDirection.Normalize();
        }
    }

    [SerializeField]
    public bool debugIntersection;
    public int debugI1;
    public int debugI2;
    public int debugI0;

    private void OnDrawGizmosSelected()
    {
        if ((displayNavmesh) && (navigationMesh))
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireMesh(navigationMesh);
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
            Gizmos.DrawMesh(navigationMesh);
        }
        if ((displayBoundary) && (navigationBoundaries != null))
        {
            Gizmos.color = Color.yellow;
            int count = navigationBoundaries.Count;

            for (int i = 0; i < count; i++)
            {
                var polyline = navigationBoundaries.Get(i);

                for (int j = 0; j < polyline.Count; j++)
                {
                    Gizmos.DrawLine(polyline[j], polyline[(j + 1) % polyline.Count]);
                }
            }
        }
        if ((displayVertices) && (vertices != null))
        {
            foreach (var vertex in vertices)
            {
                Gizmos.color = new Color(0.0f, 1.0f, 1.0f, 0.5f);
                Gizmos.DrawSphere(vertex.startPos, 0.05f);
                Gizmos.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                Gizmos.DrawLine(vertex.startPos, vertex.startPos + vertex.normal * 0.1f);
                Gizmos.color = new Color(0.0f, 1.0f, 1.0f, 1.0f);
                Gizmos.DrawLine(vertex.startPos, vertex.startPos + vertex.innerDirection * 0.1f);
            }
        }
        if ((displayBorderOverTime) && (vertices != null) && (edges != null))
        {
            for (int i = 0; i < edges.Count; i++)
            {
                var edge = edges[i];
                Gizmos.color = Color.yellow;
                if (debugEdge == i)
                {
                    Gizmos.color = Color.red;
                }

                var v1 = vertices[edge.i1];
                var v2 = vertices[edge.i2];
                var p1 = v1.startPos + v1.innerDirection * debugTime;
                var p2 = v2.startPos + v2.innerDirection * debugTime;

                Gizmos.DrawLine(p1, p2);
            }
            if ((debugVertex >= 0) && (debugVertex < vertices.Count))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(vertices[debugVertex].startPos, 0.1f);
                Gizmos.DrawLine(vertices[debugVertex].startPos, vertices[debugVertex].startPos + vertices[debugVertex].innerDirection * 100);
            }
        }
        if ((debugIntersection) && (vertices != null) && (edges != null))
        {
            if ((debugI1 < 0) || (debugI1 >= vertices.Count)) return;
            if ((debugI2 < 0) || (debugI2 >= vertices.Count)) return;
            if ((debugI0 < 0) || (debugI0 >= vertices.Count)) return;

            Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.5f);
            Gizmos.DrawSphere(vertices[debugI1].startPos, 0.05f);
            Gizmos.DrawSphere(vertices[debugI2].startPos, 0.05f);
            Gizmos.color = new Color(1.0f, 0.0f, 1.0f, 0.5f);
            Gizmos.DrawSphere(vertices[debugI0].startPos, 0.05f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(vertices[debugI1].startPos, vertices[debugI1].startPos + vertices[debugI1].innerDirection * 1000.0f);
            Gizmos.DrawLine(vertices[debugI2].startPos, vertices[debugI2].startPos + vertices[debugI2].innerDirection * 1000.0f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(vertices[debugI0].startPos, vertices[debugI0].startPos + vertices[debugI0].innerDirection * 1000.0f);

            // Compute a plane normal to use as projection plane
            Vector3 planeNormal = (vertices[debugI0].normal + vertices[debugI1].normal + vertices[debugI2].normal).normalized;

            // Project points and directions onto plane
            /*Vector3 p0 = ProjectOntoPlane(vertices[debugI0].startPos, planeNormal);
            Vector3 d0 = ProjectOntoPlane(vertices[debugI0].innerDirection, planeNormal);
            Vector3 p1 = ProjectOntoPlane(vertices[debugI1].startPos, planeNormal);
            Vector3 d1 = ProjectOntoPlane(vertices[debugI1].innerDirection, planeNormal);
            Vector3 p2 = ProjectOntoPlane(vertices[debugI2].startPos, planeNormal);
            Vector3 d2 = ProjectOntoPlane(vertices[debugI2].innerDirection, planeNormal);*/
            
            Vector3 p0 = vertices[debugI0].startPos;
            Vector3 d0 = vertices[debugI0].innerDirection;
            Vector3 p1 = vertices[debugI1].startPos;
            Vector3 d1 = vertices[debugI1].innerDirection;
            Vector3 p2 = vertices[debugI2].startPos;
            Vector3 d2 = vertices[debugI2].innerDirection;

            Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.5f);
            Gizmos.DrawSphere(p1, 0.05f);
            Gizmos.DrawSphere(p2, 0.05f);
            Gizmos.color = new Color(1.0f, 0.0f, 1.0f, 0.5f);
            Gizmos.DrawSphere(p0, 0.05f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(p1, p1 + d1 * 1000.0f);
            Gizmos.DrawLine(p2, p2 + d2 * 1000.0f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(p0, p0 + d0 * 1000.0f);

            // Pin p1, so all movement is relative to it
            Vector3 P0 = p0 - p1;
            Vector3 D0 = d0 - d1;
            Vector3 P2 = p2 - p1;
            Vector3 D2 = d2 - d1;

            //if (CheckIntersection(P0, D0, P2, D2, out float t, out float theta))
            if (CheckIntersection(p0, d0, p1, d1, p2, d2, out float t, out float theta))
            {
                /*Vector3 iP1 = vertices[debugI1].startPos + t * vertices[debugI1].innerDirection;
                Vector3 iP2 = vertices[debugI2].startPos + t * vertices[debugI2].innerDirection;
                Vector3 iP3 = vertices[debugVertex].startPos + t * vertices[debugVertex].innerDirection;*/
                Vector3 iP1 = p1 + t * d1;
                Vector3 iP2 = p2 + t * d2;
                Vector3 iP3 = p0 + t * d0;

                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(iP1, iP2);
                Gizmos.color = new Color(1.0f, 0.0f, 1.0f, 0.5f);
                Gizmos.DrawSphere(iP3, 0.1f);
            }
        }
    }
}
