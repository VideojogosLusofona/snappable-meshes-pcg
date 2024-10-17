using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.ProBuilder;

internal class StraightSkeletonState
{
    public enum EventType { None, EdgeCollapse, VertexSplit };

    public struct Event
    {
        public float            t;
        public EventType        type;
        public int              edgeId;
        public int              vertexId;
    }

    public class Edge
    {
        public int i1;
        public int i2;

        public bool Uses(int index) => (i1 == index) || (i2 == index);
        public Edge Clone() => new Edge() { i1 = i1, i2 = i2 };

        internal float Length(List<Vertex> vertices)
        {
            return Vector3.Distance(vertices[i1].startPos, vertices[i2].startPos);
        }
    }

    public class Vertex
    {
        public Vector3 startPos;
        public Vector3 normal;
        public bool    isCW;
        public Vector3 innerDirection;

        public Vertex Clone() => new Vertex() { startPos = startPos, normal = normal, innerDirection = innerDirection };

        internal bool IsOnEdge(Vertex p0, Vertex p1)
        {
            // Vector from A to P and from A to B
            Vector3 AP = startPos - p0.startPos;
            Vector3 AB = p1.startPos - p0.startPos;

            // 1. Check if AP and AB are collinear using cross product
            Vector3 crossProduct = Vector3.Cross(AP, AB);
            if (crossProduct != Vector3.zero)
            {
                return false; // Not collinear
            }

            // 2. Check if P lies between A and B using dot product
            float dotProduct1 = Vector3.Dot(AP, AB);
            float dotProduct2 = Vector3.Dot(AB, AB);

            if (dotProduct1 < 0 || dotProduct1 > dotProduct2)
            {
                return false; // P is not between A and B
            }

            // P is on the line segment
            return true;
        }
    }

    public float                        startTime;
    public List<Vertex>                 vertices;
    public List<Edge>                   edges;
    public StraightSkeletonState             parentState;
    public List<Event>                  processedEvents;
    public Dictionary<int, (int, int)>  splitEdges;

    Vector3 ProjectOntoPlane(Vector3 vector, Vector3 planeNormal)
    {
        return vector - Vector3.Dot(vector, planeNormal) * planeNormal;
    }

    static (float?, float?) Roots(float A, float B, float C)
    {
        if (Mathf.Abs(A) < 1e-3)
        {
            // This is linear
            if (Mathf.Abs(B) < 1e-3)
            {
                // No solution
                return (null, null);
            }
            return (-C / B, -C / B);
        }

        return QuadricRoots(A, B, C);
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

    void ComputeEdgeCollapseTime(List<Event> events)
    {
        // Find first edge collapse event
        for (int i = 0; i < edges.Count; i++)
        {
            var edge = edges[i];
            if (edge == null) continue;

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
                if (t > 0)
                {
                    events.Add(new Event()
                    {
                        t = t + startTime,
                        type = EventType.EdgeCollapse,
                        edgeId = i
                    });
                }
            }
        }
    }

    static bool CheckIntersection(Vector3 P0, Vector3 D0, Vector3 P1, Vector3 D1, Vector3 P2, Vector3 D2, out float T, out float theta)
    {
        // Initialize outputs
        T = 0f;
        theta = 0f;

        // Try X = Z
        int i1 = 0, i2 = 2, i3 = 1;
        (float A, float B, float C) = ComputeIntersectionCoeffs(P0, D0, P1, D1, P2, D2, i1, i2);

        var roots = Roots(A, B, C);
        if (roots.Item1 == null)
        {
            // Failed with X = Z, try X = Y
            i1 = 0; i2 = 1; i3 = 2;
            (A, B, C) = ComputeIntersectionCoeffs(P0, D0, P1, D1, P2, D2, i1, i2);

            roots = Roots(A, B, C);
            if (roots.Item1 == null)
            {
                // Failed with X = Z, try Y = Z
                i1 = 1; i2 = 2; i3 = 0;
                (A, B, C) = ComputeIntersectionCoeffs(P0, D0, P1, D1, P2, D2, i1, i2);

                roots = Roots(A, B, C);
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

    void ComputeVertexSplitTime(List<Event> events)
    {
        // Check for vertex split events
        for (int eIndex = 0; eIndex < edges.Count; eIndex++)
        {
            var edge = edges[eIndex];
            if (edge == null) continue;

            for (int index = 0; index < vertices.Count; index++)
            {
                if (edge.Uses(index)) continue;

                var vertex = vertices[index];
                if (vertex == null) continue;

                // Need a check with positions, because when I have a split, at time T=0, the new vertex will be colliding with the 
                // old edge (and the old vertex will be colliding with the new edge)
                float d = Vector3.Distance(vertex.startPos, vertices[edge.i1].startPos);
                if (d < 1e-3) continue;
                d = Vector3.Distance(vertex.startPos, vertices[edge.i2].startPos);
                if (d < 1e-3) continue;

                // Compute a plane normal to use as projection plane
                Vector3 planeNormal = (vertex.normal + vertices[edge.i1].normal + vertices[edge.i2].normal).normalized;

                // Project points and directions onto plane
                Vector3 p0 = ProjectOntoPlane(vertex.startPos, planeNormal);
                Vector3 d0 = ProjectOntoPlane(vertex.innerDirection, planeNormal);
                Vector3 p1 = ProjectOntoPlane(vertices[edge.i1].startPos, planeNormal);
                Vector3 d1 = ProjectOntoPlane(vertices[edge.i1].innerDirection, planeNormal);
                Vector3 p2 = ProjectOntoPlane(vertices[edge.i2].startPos, planeNormal);
                Vector3 d2 = ProjectOntoPlane(vertices[edge.i2].innerDirection, planeNormal);//*/

                // Find intersection
                if (CheckIntersection(p0, d0, p1, d1, p2, d2, out float t, out float theta))
                {
                    if (t >= 0)
                    {
                        events.Add(new Event()
                        {
                            t = t + startTime,
                            type = EventType.VertexSplit,
                            edgeId = eIndex,
                            vertexId = index
                        });
                    }
                }
            }
        }
    }

    public void BuildVertices(Boundary navigationBoundaries)
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
                    isCW = boundary.isCW(),
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
        foreach (var vertices in vertices)
        {
            if (vertices == null) continue;

            vertices.innerDirection = Vector3.zero;
        }
        foreach (var edge in edges)
        {
            if (edge == null) continue;

            var v1 = vertices[edge.i1];
            var v2 = vertices[edge.i2];

            Vector3 edgeTangent = (v2.startPos - v1.startPos).normalized;
            v1.innerDirection += ((v1.isCW) ? (-1.0f) : (1.0f)) * Vector3.Cross(v1.normal, edgeTangent);
            v2.innerDirection += ((v2.isCW) ? (-1.0f) : (1.0f)) * Vector3.Cross(v2.normal, edgeTangent);
        }
    }

    public void GetNextEvent(List<Event> events)
    {
        // Calculate number of vertices
        int nVertex = 0;
        foreach (var vertex in vertices) if (vertex != null) nVertex++;
        if (nVertex == 0) return;
        
        int nEdge = 0;
        foreach (var edge in edges) if (edge != null) nEdge++;
        if (nEdge == 0) return;

        ComputeEdgeCollapseTime(events);
        ComputeVertexSplitTime(events);

        events.Sort((event1, event2) => event1.t.CompareTo(event2.t));
    }

    public StraightSkeletonState CreateNewState(List<Event> events)
    {
        if (events.Count == 0) return null;

        // Fetch first event time
        float nextEvent = events[0].t;

        Debug.Log($"<color=#00FF00>Step at time {nextEvent}:<color=#A0A0A0>");

        StraightSkeletonState newState = Clone();
        newState.splitEdges = new();
        newState.parentState = this;
        newState.processedEvents = new();
        newState.FastForward(nextEvent - startTime);

        for (int i = 0; i < events.Count; i++)
        {
            var evt = events[i];
            if (evt.t > nextEvent) break;

            Debug.Log($"<color=#00FF00>Event: Type={evt.type} => Edge={evt.edgeId} / Vertex={evt.vertexId}<color=#A0A0A0>");

            switch (evt.type)
            {
                case EventType.None:
                    break;
                case EventType.EdgeCollapse:
                    newState.ActionCollapseEdge(evt.edgeId);
                    break;
                case EventType.VertexSplit:
                    newState.ActionSplitEdge(evt.edgeId, evt.vertexId);
                    break;
                default:
                    break;
            }

            newState.processedEvents.Add(evt);
        }

        newState.CleanupCollapsedEdges();

        return newState;
    }

    public StraightSkeletonState Clone()
    {
        StraightSkeletonState clonedState = new StraightSkeletonState();
        clonedState.vertices = new List<Vertex>();
        foreach (var vertex in vertices)
        {
            if (vertex == null) clonedState.vertices.Add(null);
            else clonedState.vertices.Add(vertex.Clone());
        }
        clonedState.edges = new List<Edge>();
        foreach (var edge in edges)
        {
            if (edge == null) clonedState.edges.Add(null);
            else clonedState.edges.Add(edge.Clone());
        }
        clonedState.startTime = startTime;

        return clonedState;
    }

    void FastForward(float deltaT)
    {
        startTime += deltaT;

        foreach (var vertex in vertices)
        {
            if (vertex == null) continue;

            vertex.startPos += vertex.innerDirection * deltaT;
        }
    }

    (int, int) FindEdgesThatUses(int vertexId)
    {
        int e1 = -1;
        int e2 = -1;
        int eIndex = 0;
        for (eIndex = 0; eIndex < edges.Count; eIndex++)
        {
            var edge = edges[eIndex];
            if (edge == null) continue;

            if (edge.i1 == vertexId) e2 = eIndex;
            if (edge.i2 == vertexId) e1 = eIndex;
        }

        return (e1, e2);
    }

    void ActionSplitEdge(int originalEdgeId, int vertexId)
    {
        // Before we act, we need to check if this is still a valid event
        if (IsEdgeCollapsed(originalEdgeId)) return;
        if (vertices[vertexId] == null) return;

        // Target edge might have been split by a previous operation into other edges, retrieve the new edges
        List<int> edgeIds = new();
        GetSplitEdges(originalEdgeId, edgeIds);

        foreach (int edgeId in edgeIds)
        {
            // Check if this edge is being intersected by itself (it may happen when we change things in previous events)        
            var currentEdge = edges[edgeId];
            if (currentEdge.Uses(vertexId)) continue;

            // Check if the vertex splits this particular edge
            var baseVertex = (parentState != null) ? (parentState.vertices[vertexId]) : (vertices[vertexId]);
            var p1 = vertices[currentEdge.i1];
            var p2 = vertices[currentEdge.i2];
            if (!CheckIntersection(baseVertex.startPos, baseVertex.innerDirection, p1.startPos, Vector3.zero, p2.startPos, Vector3.zero, out float t, out float theta))
            {
                // Doesn't intersect this segment, skip this edge
                continue;
            }
            if ((t - startTime) > 1e-3)
            {
                // Intersects the segment in the future, skip this edge
                continue;
            }

            Debug.Log($"    Edge {edgeId} is being split by vertex {vertexId}...");
            // Find the edges that uses the given vertex (2 of them)
            (int e1, int e2) = FindEdgesThatUses(vertexId);
            // Create a new point from the intersection point, the previous one will be used for the other edge
            Vertex newVertex = vertices[vertexId].Clone();
            int indexNewVertex = vertices.Count; vertices.Add(newVertex);
            Debug.Log($"    New vertex {vertices.Count - 1} = Vertex {vertexId}");
            // The edge (a, b) that is intersected needs to be split in 2 edges - (a, oldVertex) and (newVertex, b), previous edge is removed
            int a = currentEdge.i1;
            int b = currentEdge.i2;
            edges[edgeId] = null;
            Debug.Log($"    Removed Edge {edgeId}");
            edges.Add(new Edge() { i1 = a, i2 = vertexId });
            Debug.Log($"    New Edge {edges.Count - 1} = ({a}, {vertexId})");
            edges.Add(new Edge() { i1 = indexNewVertex, i2 = b });
            Debug.Log($"    New edge {edges.Count - 1} = ({indexNewVertex}, {b})");

            splitEdges.Add(edgeId, (edges.Count - 2, edges.Count - 1));

            // Change edge (X, vertexId) => (X, indexNewVertex)
            Edge oldEdge = edges[e1];
            Debug.Log($"    Change edge ({oldEdge.i1}, {oldEdge.i2}) = ({oldEdge.i1}, {indexNewVertex})");
            oldEdge.i2 = indexNewVertex;

            // Cleanup collapsed edges
            CleanupCollapsedEdges();
            // Cleanup vertices
            CleanupVertices();
        }

        // Rebuild direction vectors
        ComputeVertexDirection();
    }

    void CleanupCollapsedEdges()
    {
        for (int i = 0; i < edges.Count; i++)
        {
            var edge = edges[i];
            if (edge == null) continue;

            if (edge.Length(vertices) < 1e-3)
            {
                // Collapse the edge and restart
                Debug.Log($"<color=#008000>    Edge {i} collapsed, removing it...<color=#A0A0A0>");
                CollapseEdge(i);
                i = 0;
            }                
        }

        // Check if any edges overlap another edge, while being colinear - this should remove all null-area polygons
        for (int i = 0; i < edges.Count; i++)
        {
            var edge1 = edges[i];
            if (edge1 == null) continue;

            Vertex v1 = vertices[edge1.i1];
            Vertex v2 = vertices[edge1.i2];

            Vector3 edgeDir1 = (v2.startPos - v1.startPos).normalized;

            for (int j = 0; j < edges.Count; j++)
            {
                if (i == j) continue;

                var edge2 = edges[j];
                if (edge2 == null) continue;

                Vertex v3 = vertices[edge2.i1];
                Vertex v4 = vertices[edge2.i2];

                Vector3 edgeDir2 = (v4.startPos - v3.startPos).normalized;

                // Check if edges are colinear
                if (Vector3.Dot(edgeDir1, edgeDir2) > (1 - 1e-3))
                {
                    // Check if v1 and v2 are on the edge, and if it's not an endpoint                    
                    if ((v1.IsOnEdge(v3, v4) && (Vector3.Distance(v1.startPos, v3.startPos) > 1e-3) && (Vector3.Distance(v1.startPos, v4.startPos) > 1e-3)) ||
                        (v2.IsOnEdge(v3, v4) && (Vector3.Distance(v2.startPos, v3.startPos) > 1e-3) && (Vector3.Distance(v2.startPos, v4.startPos) > 1e-3)))
                    {
                        // Collapse the edge and restart
                        Debug.Log($"<color=#008000>    Edges {i} and {j} are colinear, removing them...<color=#A0A0A0>");

                        // Remove these edges
                        CollapseEdge(i);
                        CollapseEdge(j);
                        break;
                    }
                }
            }
            if (edges[i] == null) continue;
        }

        RemoveNullEdges();
    }

    void RemoveNullEdges()
    {
        List<Edge> edgesToProcess = new List<Edge>();
        foreach (var edge in edges) { if (edge != null) edgesToProcess.Add(edge); }

        while (edgesToProcess.Count > 0)
        {
            List<Edge> polygon = new();

            // Start with first available edge
            polygon.Add(edgesToProcess[0]);
            edgesToProcess.RemoveAt(0);

            while (true)
            {
                var nextEdge = FindEdgeWithStart(edgesToProcess,polygon[polygon.Count - 1].i2);
                if (nextEdge == null)
                {
                    // This polygon is not closed, remove all the edges that are on this polygon from the edge list
                    Debug.Log($"<color=#008000>    Polygon is not closed, removing edges.<color=#A0A0A0>");
                    RemovePolygonFromEdgeList(polygon);
                    break;
                }
                else
                {
                    edgesToProcess.Remove(nextEdge);
                    polygon.Add(nextEdge);
                    if (nextEdge.i2 == polygon[0].i1)
                    {
                        // Found the termination vertex, it's a closed polygon, leave it be
                        if (polygon.Count < 3)
                        {
                            // Needs at least three edges to make a valid polygon, remove it otherwise
                            Debug.Log($"<color=#008000>    Polygon has less than 3 sides, removing edges.<color=#A0A0A0>");
                            RemovePolygonFromEdgeList(polygon);
                        }
                        else
                        {
                            // Need to check area here
                            Polyline polyline = new Polyline();
                            foreach (var p in polygon) polyline.Add(vertices[p.i1].startPos, vertices[p.i1].normal);

                            float area = polyline.ComputeArea();
                            if (area < 1e-3)
                            {
                                Debug.Log($"<color=#008000>    Polygon has null area, removing edges.<color=#A0A0A0>");
                                RemovePolygonFromEdgeList(polygon);
                            }
                        }

                        break;
                    }
                }
            }
        }
    }

    void RemovePolygonFromEdgeList(List<Edge> polygon)
    {
        foreach (var e in polygon)
        {
            int edgeId = edges.IndexOf(e);
            if (edgeId != -1)
            {
                Debug.Log($"<color=#008000>    Removing edge {edgeId} ({e.i1}, {e.i2}).<color=#A0A0A0>");
                edges[edgeId] = null;
            }
        }
    }

    void CleanupVertices()
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            if (vertices[i] == null) continue;

            bool inUse = false;
            foreach (var edge in edges)
            {
                if (edge != null)
                {
                    if (edge.Uses(i))
                    {
                        inUse = true;
                        break;
                    }
                }
            }

            if (!inUse)
            {
                vertices[i] = null;
                Debug.Log($"    Vertex {i} is no longer in use, removing it...");
            }
        }
    }

    Edge FindEdgeWithStart(List<Edge> edges, int vertexId) => edges.Find((e) => (e != null) && (e.i1 == vertexId));
    Edge FindEdgeWithEnd(List<Edge> edges, int vertexId) => edges.Find((e) => (e != null) && (e.i2 == vertexId));

    int GetEdgeId(Edge edge) => edges.IndexOf(edge);

    void ActionCollapseEdge(int edgeId)
    {
        CollapseEdge(edgeId);

        // Cleanup collapsed edges
        CleanupCollapsedEdges();
        // Cleanup vertices
        CleanupVertices();
        // Rebuild direction vectors
        ComputeVertexDirection();
    }

    void CollapseEdge(int edgeId)
    {
        Edge edge = edges[edgeId];
        // Check if edge was already collapsed by another previous operation
        if (IsEdgeCollapsed(edgeId)) return;

        Edge e1 = FindEdgeWithEnd(edges, edge.i1);
        Edge e2 = FindEdgeWithStart(edges, edge.i2);

        if ((e1 != null) && (e2 != null))
        {
            Debug.Log($"    Changed edge {GetEdgeId(e1)} => ({e1.i1},{e1.i2}) => ({e1.i1}, {e2.i1})");
            e1.i2 = e2.i1;
        }

        // Remove edge
        Debug.Log($"    Removed edge {edgeId}");
        edges[edgeId] = null;
    }

    bool IsEdgeCollapsed(int edgeId)
    {
        if (splitEdges.TryGetValue(edgeId, out var edges))
        {
            if (edges.Item1 == -1) return true;
        }
        return false;
    }

    void GetSplitEdges(int edgeId, List<int> ret)
    {
        if (splitEdges.TryGetValue(edgeId, out var edgeList))
        {
            if (edgeList.Item1 == -1) return;
            GetSplitEdges(edgeList.Item1, ret);
            GetSplitEdges(edgeList.Item2, ret);
            return;
        }
        if (edges[edgeId] != null)
        {
            ret.Add(edgeId);
        }
    }

    public void OnDrawGizmos(bool displayVertices, bool displayBorderOverTime, bool debugIntersection, 
                             int debugEdge, int debugVertex, float debugTime)
    {
        float elapseTime = debugTime - startTime;

        if ((displayVertices) && (vertices != null))
        {
            foreach (var vertex in vertices)
            {
                if (vertex == null) continue;
                Gizmos.color = new Color(0.0f, 1.0f, 1.0f, 0.25f);
                Gizmos.DrawSphere(vertex.startPos, 0.05f);
                Gizmos.color = new Color(0.0f, 1.0f, 1.0f, 0.5f);
                Gizmos.DrawLine(vertex.startPos, vertex.startPos + vertex.innerDirection * 0.1f);

                var currentPos = vertex.startPos + elapseTime * vertex.innerDirection;
                Gizmos.color = new Color(0.0f, 1.0f, 1.0f, 0.5f);
                Gizmos.DrawSphere(currentPos, 0.05f);
                Gizmos.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                Gizmos.DrawLine(currentPos, currentPos + vertex.normal * 0.1f);
                Gizmos.color = new Color(0.0f, 1.0f, 1.0f, 1.0f);
                Gizmos.DrawLine(currentPos, currentPos + vertex.innerDirection * 0.1f);

                Gizmos.color = new Color(1.0f, 0.0f, 1.0f, 0.5f);
                Gizmos.DrawLine(vertex.startPos, currentPos);
            }
        }
        if ((displayBorderOverTime) && (vertices != null) && (edges != null))
        {
            for (int i = 0; i < edges.Count; i++)
            {
                var edge = edges[i];
                if (edge == null) continue;

                Gizmos.color = Color.yellow;
                if (debugEdge == i)
                {
                    Gizmos.color = Color.red;
                }

                var v1 = vertices[edge.i1];
                var v2 = vertices[edge.i2];
                var p1 = v1.startPos + v1.innerDirection * elapseTime;
                var p2 = v2.startPos + v2.innerDirection * elapseTime;

                Gizmos.DrawLine(p1, p2);
            }
            if ((debugVertex >= 0) && (debugVertex < vertices.Count) && (vertices[debugVertex] != null))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(vertices[debugVertex].startPos, 0.1f);
                Gizmos.DrawLine(vertices[debugVertex].startPos, vertices[debugVertex].startPos + vertices[debugVertex].innerDirection * 100);
            }
        }
        if ((debugIntersection) && (vertices != null) && (edges != null))
        {
            if ((debugEdge >= 0) && (debugEdge < edges.Count) && (debugVertex >= 0) && (debugVertex < vertices.Count) && (edges[debugEdge] != null))
            {
                int debugI1 = edges[debugEdge].i1;
                int debugI2 = edges[debugEdge].i2;

                Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.5f);
                Gizmos.DrawSphere(vertices[debugI1].startPos, 0.05f);
                Gizmos.DrawSphere(vertices[debugI2].startPos, 0.05f);
                Gizmos.color = new Color(1.0f, 0.0f, 1.0f, 0.5f);
                Gizmos.DrawSphere(vertices[debugVertex].startPos, 0.05f);

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(vertices[debugI1].startPos, vertices[debugI1].startPos + vertices[debugI1].innerDirection * 1000.0f);
                Gizmos.DrawLine(vertices[debugI2].startPos, vertices[debugI2].startPos + vertices[debugI2].innerDirection * 1000.0f);

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(vertices[debugVertex].startPos, vertices[debugVertex].startPos + vertices[debugVertex].innerDirection * 1000.0f);

                // Compute a plane normal to use as projection plane
                Vector3 planeNormal = (vertices[debugVertex].normal + vertices[debugI1].normal + vertices[debugI2].normal).normalized;

                // Project points and directions onto plane
                /*Vector3 p0 = ProjectOntoPlane(vertices[debugI0].startPos, planeNormal);
                Vector3 d0 = ProjectOntoPlane(vertices[debugI0].innerDirection, planeNormal);
                Vector3 p1 = ProjectOntoPlane(vertices[debugI1].startPos, planeNormal);
                Vector3 d1 = ProjectOntoPlane(vertices[debugI1].innerDirection, planeNormal);
                Vector3 p2 = ProjectOntoPlane(vertices[debugI2].startPos, planeNormal);
                Vector3 d2 = ProjectOntoPlane(vertices[debugI2].innerDirection, planeNormal);*/

                Vector3 p0 = vertices[debugVertex].startPos;
                Vector3 d0 = vertices[debugVertex].innerDirection;
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
}

