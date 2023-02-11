using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using static RcdtcsUnityUtils;

namespace SnapMeshPCG
{
    public class AutoPieceGeneration : MonoBehaviour
    {
        [Expandable]
        public AutoPieceGenerationConfig config;

        class SamplePoint
        {
            public int      sampleId;
            public Vector3  position;
            public Vector3  normal;
            public int      sourceEdgeId;
            public bool     canStart;
            public bool     canEnd;
        }
        struct SampleList
        {
            public bool isCW;
            public List<SamplePoint> points;
        }
        private List<SampleList> samplePoints;

        [System.Serializable]
        struct Edge
        {
            public Vector3      p0;
            public Vector3      p1;
            public Vector3      normal;
            public Vector3      perp;
            [System.NonSerialized] 
            public List<Edge>   originalEdges;


            public Vector3  center => (p0 + p1) * 0.5f;
            public float    length => (p1 - p0).magnitude;

            public void     RecomputePerpAndNormal()
            {
                // Get basic direction
                var dir = (p1 - p0).normalized;
                // Compute the perpendicular
                var newPerp = Vector3.Cross(dir, normal);
                // Check if the perpendicular direction is preserved
                if (Vector3.Dot(newPerp, perp) < 0)
                {
                    // Invert it
                    newPerp = -newPerp;
                }
                // Assign it
                perp = newPerp;
            }

            public Plane GetHistoryPlaneWorldSpace(Matrix4x4 matrix)
            {
                Vector3 historyNormal = Vector3.zero;
                Vector3 historyCenter = Vector3.zero;

                foreach (var edge in originalEdges)
                {
                    historyNormal += edge.perp;
                    historyCenter += edge.center;
                }

                historyNormal.Normalize();
                historyCenter /= originalEdges.Count;
                historyCenter = matrix * new Vector4(historyCenter.x, historyCenter.y, historyCenter.z, 1);

                float d = Vector3.Dot(historyCenter, historyNormal);

                return new Plane(historyNormal, d);
            }
        }

        [SerializeField, HideInInspector] private List<Edge> connectorEdges;

        MeshOctree  meshOctree;
        Mesh        navMesh;

        RecastMeshParams navMeshParams;

        [Button("Build")]
        public bool Run()
        {
            if (!config) return false;

            DebugGizmo.Clear();

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            long t0 = 0;

            t0 = stopwatch.ElapsedMilliseconds;

            MeshFilter sourceMeshFilter = GetComponentInChildren<MeshFilter>();
            Mesh sourceMesh = (sourceMeshFilter) ? (sourceMeshFilter.sharedMesh) : (null);
            Matrix4x4 sourceMeshMatrix = sourceMeshFilter.transform.GetLocalMatrix();

            if (sourceMesh != null)
            {
                UnityEditor.EditorUtility.DisplayProgressBar("Building...", "Creating mesh octree...", 0.0f);

                meshOctree = sourceMesh.GetOctree(sourceMeshMatrix);

                var t1 = stopwatch.ElapsedMilliseconds;
                //Debug.Log($"Mesh octree construction time = {t1 - t0} ms");

                UnityEditor.EditorUtility.DisplayProgressBar("Building...", "Creating nav mesh...", 0.1f);

                navMeshParams = new RecastMeshParams();
                navMeshParams.m_cellSize = config.agentRadius / 4.0f;// 1.0f / config.voxelDensity;
                navMeshParams.m_cellHeight = navMeshParams.m_cellSize / 2.0f; // 1.0f / (config.voxelDensity * config.verticalDensityMultiplier);

                navMeshParams.m_agentHeight = navMeshParams.m_cellHeight;
                navMeshParams.m_agentRadius = config.agentRadius;
                navMeshParams.m_agentMaxClimb = config.agentStep;
                navMeshParams.m_agentMaxSlope = config.agentMaxSlope;

                //Debug.Log($"Voxel size = {navMeshParams.m_cellSize}, {navMeshParams.m_cellHeight}, {navMeshParams.m_cellSize}");
                //Debug.Log($"Agent size = {navMeshParams.m_agentRadius}/{navMeshParams.m_agentHeight}");

                navMeshParams.m_regionMinSize = (navMeshParams.m_agentRadius * config.minAreaInAgents) / navMeshParams.m_cellSize; // config.minArea;
                navMeshParams.m_regionMergeSize = navMeshParams.m_regionMinSize * 5.0f; // config.minArea * 10.0f;
                navMeshParams.m_monotonePartitioning = false;

                //Debug.Log($"Minimum region size (voxels^2) = {navMeshParams.m_regionMinSize * navMeshParams.m_regionMinSize}");

                navMeshParams.m_edgeMaxLen = navMeshParams.m_agentRadius * 8; // 0.5f;
                navMeshParams.m_edgeMaxError = 1.3f;
                navMeshParams.m_vertsPerPoly = 6;
                navMeshParams.m_detailSampleDist = 1;
                navMeshParams.m_detailSampleMaxError = 1;

                SystemHelper recast = new SystemHelper();

                recast.SetNavMeshParams(navMeshParams);
                recast.ClearComputedData();
                recast.ClearMesh();
                recast.AddMesh(sourceMesh, sourceMeshFilter.gameObject);
                recast.ComputeSystem();

                var t2 = stopwatch.ElapsedMilliseconds;
                //Debug.Log("Navmesh generation = " + (t2 - t1));
                //Debug.Log("Navmesh generation = " + (t2 - t1));

                navMesh = recast.GetPolyMesh(transform.worldToLocalMatrix);

                DebugGizmo.AddMesh($"Type=NavMesh", navMesh, new Color(0.1f, 0.8f, 0.1f, 0.5f), transform.localToWorldMatrix, DebugGizmo.MeshDrawSolid | DebugGizmo.MeshDrawWire);

                UnityEditor.EditorUtility.DisplayProgressBar("Building...", "Detecting connector edges...", 0.25f);
                 
                long dioc0 = stopwatch.ElapsedMilliseconds;
                DetectConnectorEdges();
                long dioc1 = stopwatch.ElapsedMilliseconds;
                //Debug.Log($"Detect In Out Time = {dioc1 - dioc0} ms");

                UnityEditor.EditorUtility.DisplayProgressBar("Building...", "Matching source geometry...", 0.50f);

                MergeEdges(transform.localToWorldMatrix);

                for (int i = 0; i < connectorEdges.Count; i++)
                {
                    var edge = connectorEdges[i];
                    DebugGizmo.AddLine($"Type=CandidateEdge;edgeId={i}", edge.p0, edge.p1, Color.magenta, transform.localToWorldMatrix);
                    DebugGizmo.AddLine($"Type=CandidateEdge;edgeId={i}", edge.center, edge.center + edge.perp, Color.blue, transform.localToWorldMatrix);
                }

                long msg0 = stopwatch.ElapsedMilliseconds;
                MatchSourceGeometry(sourceMesh, sourceMeshMatrix);
                long msg1 = stopwatch.ElapsedMilliseconds;
                //Debug.Log($"Match source geometry = {msg1 - msg0} ms");

                MergeEdges(transform.localToWorldMatrix);

                // Remove short edges - need to account for the sourceMeshMatrix because of eventual scaling
                connectorEdges.RemoveAll((edge) => (edge.length < config.agentRadius));

                if (config.connectorStrategy != AutoPieceGenerationConfig.ConnectorStrategy.None)
                {
                    UnityEditor.EditorUtility.DisplayProgressBar("Building...", "Generating connectors...", 0.75f);
                    ResetMapPiece();
                    GenerateConnectors();
                }

                if (config.boundingVolumeStrategy != AutoPieceGenerationConfig.BoundingVolumeStrategy.None)
                {
                    UnityEditor.EditorUtility.DisplayProgressBar("Building...", "Generating bounding volumes...", 0.80f);
                    ResetBoundingVolumes();
                    GenerateBoundingVolumes(sourceMesh, sourceMeshMatrix);
                }

                UnityEditor.EditorUtility.ClearProgressBar();
            }

            stopwatch.Stop();
            //Debug.Log("Mesh generation time = " + (stopwatch.ElapsedMilliseconds - t0));

            return true;
        }

        [Button("Reset Piece")]
        void ResetMapPiece()
        {
            var mapPiece = GetComponent<MapPiece>();
            if (mapPiece == null)
            {
                gameObject.AddComponent<MapPiece>();
            }

            var connectors = GetComponentsInChildren<Connector>();
            if (connectors != null)
            {
                foreach (var connector in connectors)
                {
                    connector.gameObject.Delete();
                }
            }
        }

        bool CanReachInfinitePerpendicular(MeshOctree meshOctree, Vector3 pt0, Vector3 pt1, float range, Vector3 forwardDir)
        {
            if (meshOctree == null) return true;

            Vector3 pt = pt0;
            Vector3 ptInc = (pt1 - pt0) / config.probeCount;

            for (int i = 0; i < config.probeCount; i++)
            {
                Triangle triangle = null;
                float t = float.MaxValue;

                bool b = meshOctree.Raycast(pt, forwardDir, range, ref triangle, ref t);

                DebugGizmo.AddLine($"Type=InfinitePerpendicularRaycast", pt, pt + forwardDir * range, (b) ? (Color.red) : (Color.green), transform.localToWorldMatrix);

                if (b) return false;

                pt += ptInc;
            }

            return true;
        }

        bool CircularRaycast(MeshOctree mesh, Vector3 pt, float range, Vector3 edgeDir, Vector3 perpDir, bool isFirst, bool isLast, int sampleId)
        {
            Vector3 cosDir = edgeDir;
            Vector3 sinDir = perpDir;

            float startAngle = 0;
            float endAngle = 360;
            float incAngle = (endAngle - startAngle) / config.probeCount;
            float angle = startAngle;
            for (int j = 0; j < config.probeCount; j++)
            {
                float l = Mathf.Max(range, config.agentRadius * config.probeRadiusScale);
                Vector3 probeEnd = pt + cosDir * l * Mathf.Cos(Mathf.Deg2Rad * angle) + sinDir * l * Mathf.Sin(Mathf.Deg2Rad * angle);

                angle += incAngle;

                if ((isFirst || isLast) && (edgeDir.magnitude > 0))
                {
                    if ((isFirst) && (Vector3.Dot(probeEnd - pt, edgeDir) < 0)) continue;
                    if ((isLast) && (Vector3.Dot(probeEnd - pt, edgeDir) > 0)) continue;
                }

                if (mesh != null)
                {
                    Triangle triangle = null;
                    float t = float.MaxValue;
                    var d = probeEnd - pt;
                    d /= l;
                    bool b = mesh.Raycast(pt, d, l, ref triangle, ref t);

                    DebugGizmo.AddLine($"Type=CircularRaycast;sampleId={sampleId};isFirst={isFirst};isLast={isLast}", pt, pt + d * l, (b) ? (Color.red) : (Color.green), transform.localToWorldMatrix);

                    if (b) return false;
                }
            }

            return true;
        }

        void DetectConnectorEdges()
        {
            connectorEdges = null;

            Boundary boundary;

            var topology = new Topology(navMesh, Matrix4x4.identity);
            topology.ComputeTriangleNormals();
            boundary = topology.GetBoundary();

            if (boundary == null) return;

            connectorEdges = new List<Edge>();

            float step = config.agentRadius;
            float angleTolerance = Mathf.Cos(config.sampleDirTolerance * Mathf.Deg2Rad);

            int totalEdges = 0;
            for (int boundaryIndex = 0; boundaryIndex < boundary.Count; boundaryIndex++)
            {
                var polyline = boundary.Get(boundaryIndex);
                totalEdges += polyline.Count;
            }

            int edgeId = 0;
            int sampleId = 0;

            samplePoints = new List<SampleList>();

            for (int boundaryIndex = 0; boundaryIndex < boundary.Count; boundaryIndex++)
            {
                var polyline = boundary.Get(boundaryIndex);

                bool isCW = polyline.isCW();

                var currentSamplePointList = new List<SamplePoint>();

                for (int edgeCount = 0; edgeCount < polyline.Count; edgeCount++)
                {
                    edgeId++;

                    UnityEditor.EditorUtility.DisplayProgressBar("Building...", $"Sampling connector edge ({edgeId} of {totalEdges})", 0.25f + 0.20f * (float)edgeId / totalEdges);

                    var p1 = polyline[edgeCount];
                    var p2 = polyline[(edgeCount + 1) % polyline.Count];
                    var n1 = polyline.GetNormal(edgeCount);
                    var n2 = polyline.GetNormal((edgeCount + 1) % polyline.Count);

                    DebugGizmo.AddLine($"Type=Edge;edgeId={edgeId}", p1, p2, Color.yellow, transform.localToWorldMatrix);

                    var edgeDir = p2 - p1;
                    var edgeLength = edgeDir.magnitude;
                    if (edgeLength == 0) continue;
                    edgeDir = edgeDir / edgeLength;

                    var nInc = (n2 - n1) / edgeLength;

                    var pt = p1;
                    var n = n1;
                    var nNorm = n.normalized;

                    while (Vector3.Dot((pt - p2).normalized, edgeDir) < 0)
                    {
                        sampleId++;

                        Vector3     raycastPos = pt + config.agentStep * nNorm;
                        float       t = float.MaxValue;
                        Triangle    triangle = new Triangle();
                        Vector3     raycastTriNormal = Vector3.zero;

                        DebugGizmo.AddSphere($"Type=SamplePoint;subtype=RaycastPos;sampleId={sampleId};edgeId={edgeId}", pt, 0.025f, new Color(1.0f, 0.5f, 0.0f, 1.0f), transform.localToWorldMatrix);

                        if (meshOctree.Raycast(raycastPos, -nNorm, config.agentStep * 4.0f, ref triangle, ref t))
                        {
                            raycastTriNormal = triangle.normal;
                            raycastPos = raycastPos - nNorm * t;
                        }
                        else if (meshOctree.Raycast(raycastPos, nNorm, config.agentStep * 4.0f, ref triangle, ref t))
                        {
                            raycastTriNormal = triangle.normal;
                            raycastPos = raycastPos + nNorm * t;
                        }
                        else
                        {
                            Debug.LogError("Can't raycast to surface!");

                            DebugGizmo.AddSphere($"Type=SamplePoint;subtype=RaycastError;sampleId={sampleId};edgeId={edgeId}", raycastPos, 0.025f, new Color(1.0f, 0.5f, 0.0f, 1.0f), transform.localToWorldMatrix);
                            DebugGizmo.AddLine($"Type=SamplePoint;subtype=RaycastError;sampleId={sampleId};edgeId={edgeId}", raycastPos, raycastPos - nNorm * config.agentStep * 4.0f, new Color(1.0f, 1.0f, 0.0f, 1.0f), transform.localToWorldMatrix);
                            DebugGizmo.AddLine($"Type=SamplePoint;subtype=RaycastError;sampleId={sampleId};edgeId={edgeId}", raycastPos, raycastPos + nNorm * config.agentStep * 4.0f, new Color(1.0f, 0.0f, 0.0f, 1.0f), transform.localToWorldMatrix);
                        }

                        raycastPos = raycastPos + raycastTriNormal * config.sampleHeight;

                        // Correct edge dir to be on the plane of the raycast tri normal
                        Vector3 correctedEdgeDir = (edgeDir - Vector3.Dot(edgeDir, raycastTriNormal) * raycastTriNormal).normalized;

                        bool canStart = CircularRaycast(meshOctree, raycastPos, step, correctedEdgeDir, Vector3.Cross(correctedEdgeDir, raycastTriNormal), true, false, sampleId);
                        bool canEnd = CircularRaycast(meshOctree, raycastPos, step, correctedEdgeDir, Vector3.Cross(correctedEdgeDir, raycastTriNormal), false, true, sampleId);

                        currentSamplePointList.Add(new SamplePoint
                        {
                            sampleId = sampleId,
                            position = raycastPos,
                            normal = raycastTriNormal,
                            sourceEdgeId = edgeId,
                            canStart = canStart,
                            canEnd = canEnd
                        });

                        Color color = Color.red;
                        if ((canStart) && (canEnd)) color = Color.green;
                        else if ((canStart) && (!canEnd)) color = new Color(1.0f, 0.5f, 0.0f, 1.0f);
                        else if ((!canStart) && (canEnd)) color = new Color(1.0f, 1.0f, 0.0f, 1.0f);

                        DebugGizmo.AddSphere($"Type=SamplePoint;subType=Actual;sampleId={sampleId};edgeId={edgeId};canStart={canStart};canEnd={canEnd}", raycastPos, 0.1f, color, transform.localToWorldMatrix);
                        DebugGizmo.AddLine($"Type=SamplePoint;subType=Normal;sampleId={sampleId};edgeId={edgeId};canStart={canStart};canEnd={canEnd}", raycastPos, raycastPos + raycastTriNormal * config.agentStep, Color.magenta, transform.localToWorldMatrix);
                        DebugGizmo.AddLine($"Type=SamplePoint;subType=Raycast;sampleId={sampleId};edgeId={edgeId};canStart={canStart};canEnd={canEnd}", pt, pt - nNorm * config.agentStep * 4.0f, Color.blue, transform.localToWorldMatrix);

                        pt = pt + edgeDir * config.sampleLength;
                        n = n + nInc * config.sampleLength;
                        nNorm = n.normalized;
                    }
                }

                if (currentSamplePointList.Count > 0)
                {
                    samplePoints.Add(new SampleList() { points = currentSamplePointList, isCW = isCW });
                }
            }

            int sampleCount = sampleId;
            sampleId = 0;

            // Sampling done
            // Run through the lists and create candidate edges
            foreach (var sampleList in samplePoints)
            {
                SamplePoint startPoint = null;
                SamplePoint endPoint = null;

                for (int i = 0; i <= sampleList.points.Count; i++)
                {
                    sampleId++;

                    UnityEditor.EditorUtility.DisplayProgressBar("Building...", "Detecting connector edge...", 0.45f + 0.05f * sampleId / sampleCount);

                    var currentSample = sampleList.points[i % sampleList.points.Count];

                    if (startPoint != null)
                    {
                        if (endPoint != null)
                        {
                            var edgeDir = (endPoint.position - startPoint.position).normalized;
                            var currentDir = (currentSample.position - endPoint.position).normalized;

                            if (Vector3.Dot(currentDir, edgeDir) < angleTolerance)
                            {
                                // There was a shift in direction on this point, add the valid edge
                                AddConnectorEdge(meshOctree, startPoint.position, endPoint.position, startPoint.normal, endPoint.normal, sampleList.isCW);

                                // The endpoint could have been a start point as well, so we continue from there
                                startPoint = endPoint;
                                endPoint = null;
                            }
                            else
                            {
                                // Check if we can reach infinity from this edge
                                Vector3 main_dir = (currentSample.position - startPoint.position).normalized;
                                Vector3 normal = ((startPoint.normal + currentSample.normal) * 0.5f).normalized;
                                Vector3 perp_dir = Vector3.Cross(main_dir, normal);

                                if (!CanReachInfinitePerpendicular(meshOctree, startPoint.position, currentSample.position, 4 * config.agentRadius, perp_dir))
                                {
                                    // Can't reach on the new sample point, so add this one and skip to next point
                                    AddConnectorEdge(meshOctree, startPoint.position, endPoint.position, startPoint.normal, endPoint.normal, sampleList.isCW);

                                    // The endpoint could have been a start point as well, so we continue from there
                                    startPoint = endPoint;
                                    endPoint = null;
                                }
                            }
                        }

                        if (currentSample.canStart)
                        {
                            if (currentSample.canEnd)
                            {
                                // This point can continue the edge (can be start and end)
                                endPoint = currentSample;
                            }
                            else
                            {
                                // It could be a start point, but it can't be an end point, so create the edge that was already built
                                // and start a new one
                                if (endPoint != null)
                                {
                                    AddConnectorEdge(meshOctree, startPoint.position, endPoint.position, startPoint.normal, endPoint.normal, sampleList.isCW);
                                }

                                startPoint = currentSample;
                                endPoint = null;
                            }
                        }
                        else
                        {
                            if (currentSample.canEnd)
                            {
                                // This point can end the edge, so set it as an endpoint
                                endPoint = currentSample;
                            }
                            // Add the edge (which can include the above point or not)
                            if (endPoint != null)
                            {
                                AddConnectorEdge(meshOctree, startPoint.position, endPoint.position, startPoint.normal, endPoint.normal, sampleList.isCW);
                            }
                            // Start a new edge
                            startPoint = endPoint = null;
                        }
                    }
                    else
                    {
                        if (currentSample.canStart)
                        {
                            // Start the edge
                            startPoint = currentSample;
                        }
                    }
                }

                // There was a "left over" start/end, even if the loop is done, need to add this one
                if ((startPoint != null) && (endPoint != null))
                {
                    AddConnectorEdge(meshOctree, startPoint.position, endPoint.position, startPoint.normal, endPoint.normal, sampleList.isCW);
                }
            }
        }

        bool AddConnectorEdge(MeshOctree mesh, Vector3 p0, Vector3 p1, Vector3 n1, Vector3 n2, bool isCW)
        {
            Vector3 main_dir = (p1 - p0).normalized;
            Vector3 normal = ((n1 + n2) * 0.5f).normalized;
            Vector3 perp_dir = Vector3.Cross(main_dir, normal);

            if (!CanReachInfinitePerpendicular(meshOctree, p0, p1, 4 * config.agentRadius, perp_dir))
            {
                return false;
            }

            if (config.edgeMaxAngleWithXZ < 90)
            {
                Vector3 vec = (p1 - p0).normalized;
                Vector3 projVec = vec.x0z().normalized;

                float dp = Mathf.Abs(Vector3.Dot(projVec, vec));
                if (dp <= Mathf.Cos(Mathf.Deg2Rad * config.edgeMaxAngleWithXZ))
                {
                    return false;
                }
            }

            if (perp_dir.magnitude == 0.0f)
            {
                normal = ((n1 + n2) * 0.5f).normalized;

                main_dir = (p1 - p0).normalized;
                perp_dir = Vector3.Cross(main_dir, normal);
                if (isCW) perp_dir = -perp_dir;
            }

            DebugGizmo.AddLine($"Type=ConnectorEdge;connectorEdge={connectorEdges.Count + 1}", p0, p1, Color.magenta, transform.localToWorldMatrix);

            connectorEdges.Add(new Edge() { p0 = p0, p1 = p1, normal = (n1 + n2) * 0.5f, perp = perp_dir.normalized });

            return true;
        }

        void MatchSourceGeometry(Mesh srcMesh, Matrix4x4 srcMeshMatrix)
        {
            if (meshOctree == null) return;

            var sourceEdges = connectorEdges;
            connectorEdges = new List<Edge>();

            UnityEditor.EditorUtility.DisplayProgressBar("Building...", $"Generating source mesh topology...", 0.50f);

            var topology = new Topology(srcMesh, srcMeshMatrix);
            topology.ComputeTriangleNormals();

            float cosSlopeTolerance = Mathf.Cos(Mathf.Deg2Rad * config.agentMaxSlope);
            float cosDirectionTolerance = Mathf.Cos(config.directionAngleTolerance * Mathf.Deg2Rad);
            float cosNormalTolerance = Mathf.Cos(config.normalAngleTolerance * Mathf.Deg2Rad);
            float toleranceRange = 15.0f * config.agentRadius;

            int edgeId = 0;

            //Debug.Log($"{sourceEdges.Count} source edges, {topology.nEdges} edges in original geometry ");

            foreach (var edge in sourceEdges)
            {
                edgeId++;

                UnityEditor.EditorUtility.DisplayProgressBar("Building...", $"Matching source geometry... ({edgeId} of {sourceEdges.Count})", 0.50f + 0.25f * (float)edgeId / sourceEdges.Count);

                float score = -float.MaxValue;
                Vector3 candidateStart = Vector3.zero;
                Vector3 candidateEnd = Vector3.zero;
                Vector3 candidateNormal = Vector3.zero;
                Vector3 candidatePerp = Vector3.zero;

                Vector3 edgeStart = edge.p0;
                Vector3 edgeEnd = edge.p1;
                float edgeLength = Vector3.Distance(edgeStart, edgeEnd);
                Vector3 edgeNormal = edge.normal;
                Vector3 edgeCenter = (edgeStart + edgeEnd) * 0.5f;

                // Find candidates
                var edgeDir = (edge.p1 - edge.p0).normalized;

                DebugGizmo.AddLine($"Type=MatchGeom;Subtype=MatchEdge;edgeId={edgeId}", edge.p0, edge.p1, Color.blue, transform.localToWorldMatrix);

                List<(int, Vector3, int, Vector3)> candidateEdges = new List<(int, Vector3, int, Vector3)>();

                for (int i = 0; i < topology.nEdges; i++)
                {
                    var otherEdgeStruct = topology.GetEdgeStruct(i);
                    // Check if this is a valid edge to consider
                    if (otherEdgeStruct.triangles.Count > 1)
                    {
                        Vector3 baseNormal = otherEdgeStruct.triangles[0].normal;
                        bool isBoundary = true;
                        for (int k = 1; k < otherEdgeStruct.triangles.Count; k++)
                        {
                            if (Vector3.Dot(baseNormal, otherEdgeStruct.triangles[k].normal) > cosSlopeTolerance)
                            {
                                isBoundary = false;
                                break;
                            }
                        }
                        if (!isBoundary) continue;
                    }

                    var otherEdge = topology.GetEdgeWithIndices(i);
                    var e1 = otherEdge.Item2;
                    var e2 = otherEdge.Item4;

                    if (Vector3.Distance(e1, edgeStart) > Vector3.Distance(e2, edgeStart))
                    {
                        // Switch edges
                        e1 = otherEdge.Item4;
                        e2 = otherEdge.Item2;

                        int swap = otherEdge.Item1;
                        otherEdge.Item1 = otherEdge.Item3;
                        otherEdge.Item3 = swap;
                        otherEdge.Item2 = e1;
                        otherEdge.Item4 = e2;
                    }

                    Vector3 closestPoint = Line.GetClosestPoint(e1, e2, edgeCenter);
                    if (Vector3.Distance(closestPoint, edgeCenter) > toleranceRange) continue;

                    DebugGizmo.AddLine($"Type=MatchGeom;Subtype=PreCandidateEdge;edgeId={edgeId}", e1, e2, new Color(1.0f, 0.5f, 0.0f, 1.0f), transform.localToWorldMatrix);

                    // Remove edges that exceed a certain vertical threshould (we never want this, I believe, even if the heuristic then 
                    // throws it away
                    if (Mathf.Abs(edgeCenter.y - (e1.y + e2.y) * 0.5f) > config.agentStep * 2.0f) continue;

                    var eC = (e1 + e2) * 0.5f;

                    // Check direction tolerance
                    float dp = Mathf.Abs(Vector3.Dot((e2 - e1).normalized, edgeDir));
                    if (dp >= cosDirectionTolerance)
                    {
                        // Check normal tolerance
                        dp = Mathf.Abs(Vector3.Dot(topology.GetEdgeNormal(i), edge.normal));

                        if (dp >= cosNormalTolerance)
                        {
                            // Check if center of candidate is in front of the current edge
                            if (Vector3.Dot((eC - edgeCenter).normalized, edge.perp) > 0)
                            {
                                candidateEdges.Add(otherEdge);
                                DebugGizmo.AddLine($"Type=MatchGeom;Subtype=CandidateEdge;edgeId={edgeId}", e1, e2, Color.yellow, transform.localToWorldMatrix);
                            }
                        }
                    }
                }

                // Merge candidates
                //Debug.Log($"Candidate Edges = {candidateEdges.Count}");
                MergeCandidateEdges(candidateEdges);
                //Debug.Log($"Candidate Edges = {candidateEdges.Count}");

                int subEdgeId = 0;

                foreach (var otherEdge in candidateEdges)
                {
                    subEdgeId++;

                    var e1 = otherEdge.Item2;
                    var e2 = otherEdge.Item4;

                    DebugGizmo.AddLine($"Type=MatchGeom;Subtype=CandidateEdge;edgeId={edgeId}", e1, e2, Color.yellow, transform.localToWorldMatrix);

                    var eC = (e1 + e2) * 0.5f;
                    var eD = (e2 - e1).normalized;

                    // Project both edges to the XZ plane
                    var edgeStartXZ = edgeStart.x0z();
                    var edgeEndXZ = edgeEnd.x0z();
                    var perp = edge.perp.x0z();
                    var e1XZ = e1.x0z();
                    var e2XZ = e2.x0z();
                    Vector3 intersection;
                    float tRay;
                    float tLine;
                    float intersectionScore = 0.0f;

                    if (Line.Raycast(edgeStartXZ, perp, toleranceRange, e1XZ, e2XZ, out intersection, out tRay, out tLine))
                    {
                        DebugGizmo.AddLine($"Type=MatchGeom;Subtype=Raycasts;edgeId={edgeId}", edgeStart, edgeStart + perp * toleranceRange, Color.green, transform.localToWorldMatrix);

                        var pt = Line.GetClosestPoint(e1, e2, edgeStart + tRay * edge.perp);
                        if (Vector3.Dot(eD, edgeDir) > 0)
                            e1 = pt;
                        else
                            e2 = pt;

                        intersectionScore += 3;
                    }
                    else
                    {
                        DebugGizmo.AddLine($"Type=MatchGeom;Subtype=Raycasts;edgeId={edgeId}", edgeStart, edgeStart + perp * toleranceRange, Color.red, transform.localToWorldMatrix);

                        // Compute distance fom ray (how close the raycast was of hitting)
                        if (tLine < 0) tLine = Mathf.Abs(tLine) * (e2XZ - e1XZ).magnitude;
                        else if (tLine > 1) tLine = (tLine - 1) * (e2XZ - e1XZ).magnitude;
                        tLine = 1 - Mathf.Clamp01(tLine / config.agentRadius);

                        intersectionScore += 3 * tLine;
                    }
                    if (Line.Raycast(edgeEndXZ, perp, toleranceRange, e1XZ, e2XZ, out intersection, out tRay, out tLine))
                    {
                        DebugGizmo.AddLine($"Type=MatchGeom;Subtype=Raycasts;edgeId={edgeId}", edgeEnd, edgeEnd + perp * toleranceRange, Color.green, transform.localToWorldMatrix);

                        var pt = Line.GetClosestPoint(e1, e2, edgeEnd + tRay * edge.perp);
                        if (Vector3.Dot(eD, edgeDir) > 0)
                            e2 = pt;
                        else
                            e1 = pt;

                        intersectionScore += 3;
                    }
                    else
                    {
                        DebugGizmo.AddLine($"Type=MatchGeom;Subtype=Raycasts;edgeId={edgeId}", edgeEnd, edgeEnd + perp * toleranceRange, Color.red, transform.localToWorldMatrix);

                        // Compute distance fom ray (how close the raycast was of hitting)
                        if (tLine < 0) tLine = Mathf.Abs(tLine) * (e2XZ - e1XZ).magnitude;
                        else if (tLine > 1) tLine = (tLine - 1) * (e2XZ - e1XZ).magnitude;
                        tLine = 1 - Mathf.Clamp01(tLine / config.agentRadius);

                        intersectionScore += 3 * tLine;
                    }

                    // The larger the distance, the larger the score
                    float distanceScore = Vector3.Dot(eC.x0z() - edgeStartXZ, edge.perp);
                    // The larger the difference in Y, the lower the score
                    float yScore = -Mathf.Abs(eC.y - edgeCenter.y);
                    // Penalize differences in length, until the maximum size
                    float lengthScore = Mathf.Min(0, (Vector3.Distance(e1, e2) - edgeLength));
                    // The larger the angular difference, the lower the score (we can use the perp with the direction, because we want it 
                    // to be larger with the difference, not with the similarity)
                    float angleScore = -Mathf.Abs(Vector3.Dot(perp, (e2XZ - e1XZ).normalized)) * 20.0f;

                    float edgeScore = intersectionScore + (distanceScore * -1) + (yScore * 10) + lengthScore + angleScore;

                    // Check what is the angle between the perp line on the edge and the direction of movement to 
                    // "move" to the new edge
                    // Find center of geometry edge to compute angle
                    Vector3 closestPoint = Line.GetClosestPoint(e1, e2, edge.center);
                    if ((closestPoint == e1) || (closestPoint == e2))
                    {
                        Vector3 candidateCenter = (e1 + e2) * 0.5f;
                        float angle = Mathf.Acos(Vector3.Dot(edge.perp, (candidateCenter - edge.center).normalized));
                        if (angle > (config.directionAngleTolerance * Mathf.Deg2Rad))
                        {
                            if (edgeId == 1)
                            {
                                DebugGizmo.AddLine($"Type=MatchGeom;Subtype=AngularTest;edgeId={edgeId};", edge.center, candidateCenter, Color.cyan, transform.localToWorldMatrix);
                            }

                            edgeScore = -float.MaxValue;
                        }
                    }

                    //Debug.Log($"Edge {edgeId}, Matching To {subEdgeId}, Scores = {intersectionScore}/{distanceScore}/{yScore}/{lengthScore}/{angleScore}, Total={edgeScore}");

                    if (score < edgeScore)
                    {
                        candidateStart = e1;
                        candidateEnd = e2;
                        candidateNormal = edge.normal;
                        candidatePerp = edge.perp;
                        score = edgeScore;
                    }
                }

                if (score > -float.MaxValue)
                {
                    var newEdge = new Edge() { p0 = candidateStart, p1 = candidateEnd, normal = candidateNormal, perp = candidatePerp };
                    // Recompute edge perpendicular decision (instead of using the average previously computed in the merge stages)
                    newEdge.RecomputePerpAndNormal();
                    // Keep history to be able to decide on merging later
                    if (newEdge.originalEdges == null) newEdge.originalEdges = new List<Edge>();
                    newEdge.originalEdges.Add(edge);

                    connectorEdges.Add(newEdge);

                    DebugGizmo.AddLine($"Type=MatchGeom;Subtype=SourceEdge;edgeId={edgeId}", edge.p0, edge.p1, Color.cyan, transform.localToWorldMatrix);
                    DebugGizmo.AddLine($"Type=MatchGeom;Subtype=Displacement;edgeId={edgeId}", edge.center, newEdge.center, Color.magenta, transform.localToWorldMatrix);
                    DebugGizmo.AddLine($"Type=MatchGeom;Subtype=TargetEdge;edgeId={edgeId}", newEdge.p0, newEdge.p1, Color.cyan, transform.localToWorldMatrix);
                }
            }
        }

        void MergeEdges(Matrix4x4 matrix)
        {
            float cosTolerance = Mathf.Cos(Mathf.Deg2Rad * config.mergeAngleTolerance);
            float dist = config.mergeDistanceTolerance * config.agentRadius;
            bool retry = true;

            while (retry)
            {
                retry = false;

                for (int i = 0; i < connectorEdges.Count; i++)
                {
                    var currentEdge = connectorEdges[i];

                    for (int j = i + 1; j < connectorEdges.Count; j++)
                    {
                        var otherEdge = connectorEdges[j];

                        // Check if the angles are different. Compare perpendicular angles, it's the same as direction in this case
                        // If not, skip this candidate
                        if (Vector3.Dot(currentEdge.perp, otherEdge.perp) < cosTolerance) continue;

                        // Compute endpoints, need to account for transform (because of scales) for the distance calculation
                        Vector3 p00 = matrix * new Vector4(currentEdge.p0.x, currentEdge.p0.y, currentEdge.p0.z, 1);
                        Vector3 p01 = matrix * new Vector4(currentEdge.p1.x, currentEdge.p1.y, currentEdge.p1.z, 1);
                        Vector3 p10 = matrix * new Vector4(otherEdge.p0.x, otherEdge.p0.y, otherEdge.p0.z, 1);
                        Vector3 p11 = matrix * new Vector4(otherEdge.p1.x, otherEdge.p1.y, otherEdge.p1.z, 1);

                        // Check if endpoints are close
                        if (Vector3.Distance(p00, p10) < dist)
                        {
                            // p0 of both edges "match", build merge edge and remove the other edge
                            currentEdge.p0 = otherEdge.p1;
                            retry = true;
                        }
                        else if (Vector3.Distance(p01, p10) < dist)
                        {
                            // p1 of one edge matches p0 of other edge
                            currentEdge.p1 = otherEdge.p1;
                            retry = true;
                        }
                        else if (Vector3.Distance(p00, p11) < dist)
                        {
                            // p0 of one edge matches p1 of other edge
                            currentEdge.p0 = otherEdge.p0;
                            retry = true;
                        }
                        else if (Vector3.Distance(p01, p11) < dist)
                        {
                            // p1 of both edges match
                            currentEdge.p1 = otherEdge.p0;
                            retry = true;
                        }

                        if (retry)
                        {
                            // This edge is a candidate for merging
                            // Check if these edges have a history from where they comes
                            if ((currentEdge.originalEdges != null) && (otherEdge.originalEdges != null))
                            {
                                // Assume it can't be merged
                                retry = false;

                                // Compute plane for edges
                                var plane1 = currentEdge.GetHistoryPlaneWorldSpace(matrix);
                                var plane2 = otherEdge.GetHistoryPlaneWorldSpace(matrix);

                                // Check if normal of source planes are roughly in the same direction
                                if (Vector3.Dot(plane1.normal, plane2.normal) >= cosTolerance)
                                {
                                    // Check if distance is below a threshould (one quarter of the merge distance)
                                    if (Mathf.Abs(plane1.distance - plane2.distance) < config.mergeDistanceTolerance * 0.25f)
                                    {
                                        retry = true;
                                    }
                                }
                            }

                            if (retry)
                            {
                                currentEdge.perp = (currentEdge.perp + otherEdge.perp).normalized;
                                currentEdge.normal = (currentEdge.normal + otherEdge.normal).normalized;
                                if (currentEdge.originalEdges != null)
                                {
                                    if (otherEdge.originalEdges != null)
                                    {
                                        currentEdge.originalEdges.AddRange(otherEdge.originalEdges);
                                    }
                                }
                                else
                                {
                                    if (otherEdge.originalEdges != null)
                                    {
                                        currentEdge.originalEdges = otherEdge.originalEdges;
                                    }
                                }

                                currentEdge.RecomputePerpAndNormal();

                                connectorEdges[i] = currentEdge;
                                connectorEdges.RemoveAt(j);
                                break;
                            }
                        }
                    }

                    if (retry) break;
                }
            }
        }

        void MergeCandidateEdges(List<(int, Vector3, int, Vector3)> candidates)
        {
            float cosTolerance = Mathf.Cos(Mathf.Deg2Rad * 5);
            bool retry = true;

            while (retry)
            {
                retry = false;

                for (int i = 0; i < candidates.Count; i++)
                {
                    var (currentI0, currentP0, currentI1, currentP1) = candidates[i];
                    var currentDir = (currentP1 - currentP0);
                    var currentLength = currentDir.magnitude;
                    currentDir /= currentLength;

                    for (int j = i + 1; j < candidates.Count; j++)
                    {
                        var (otherI0, otherP0, otherI1, otherP1) = candidates[j];

                        // Check if this edge can be merged (check indices)
                        if ((currentI0 != otherI0) &&
                            (currentI0 != otherI1) &&
                            (currentI1 != otherI0) &&
                            (currentI1 != otherI1)) continue;

                        var otherDir = (otherP1 - otherP0);
                        var otherLength = otherDir.magnitude;
                        otherDir /= otherLength;

                        // Check if the angles are different. 
                        // If not, skip this candidate
                        if (Mathf.Abs(Vector3.Dot(currentDir, otherDir)) < cosTolerance) continue;

                        if (currentI0 == otherI0)
                        {
                            currentI0 = otherI1;
                            currentP0 = otherP1;
                            retry = true;
                        }
                        else if (currentI0 == otherI1)
                        {
                            currentI0 = otherI0;
                            currentP0 = otherP0;
                            retry = true;
                        }
                        else if (currentI1 == otherI0)
                        {
                            currentI1 = otherI1;
                            currentP1 = otherP1;
                            retry = true;
                        }
                        else if (currentI1 == otherI1)
                        {
                            currentI1 = otherI0;
                            currentP1 = otherP1;
                            retry = true;
                        }

                        if (retry)
                        {
                            candidates[i] = (currentI0, currentP0, currentI1, currentP1);
                            candidates.RemoveAt(j);
                            break;
                        }
                    }

                    if (retry) break;
                }
            }
        }

        private void GenerateConnectors()
        {
            int connectorCount = 1;

            foreach (var edge in connectorEdges)
            {
                int pinCount = 0;
                if (config.connectorStrategy == AutoPieceGenerationConfig.ConnectorStrategy.Simple)
                {
                    pinCount = config.pinCount;
                }
                else if (config.connectorStrategy == AutoPieceGenerationConfig.ConnectorStrategy.LengthBased)
                {
                    pinCount = Mathf.Max(1, Mathf.CeilToInt(edge.length * config.pinsPerUnit));
                }

                if ((config.maxPinCount > 0) && (pinCount > config.maxPinCount))
                {
                    if (config.deleteLargeConnectors) continue;

                    pinCount = config.maxPinCount;
                }

                Vector3 up = edge.normal;
                Vector3 perp = edge.perp;
                if (config.forceUpDirection)
                {
                    up = config.upDirection;
                    perp = (perp - Vector3.Dot(up, perp) * up).normalized;
                }

                // Create a connector
                GameObject go = new GameObject();
                go.transform.parent = transform;
                go.transform.localPosition = edge.center;
                go.transform.localRotation = Quaternion.LookRotation(perp, up);
                go.name = $"Connector{connectorCount}";
                Connector connector = go.AddComponent<Connector>();
                connector.Pins = pinCount;

                connectorCount++;
            }
        }

        private void OnDrawGizmosSelected()
        {
            /*if (navMeshParams != null)
            {
                var prevMatrixHandles = UnityEditor.Handles.matrix;
                var prevMatrixGizmos = Gizmos.matrix;

                UnityEditor.Handles.matrix = transform.localToWorldMatrix;
                Gizmos.matrix = transform.localToWorldMatrix;

                Vector3 size = new Vector3(navMeshParams.m_cellSize, navMeshParams.m_cellHeight, navMeshParams.m_cellSize);
                Vector3 pos = size * 0.5f;
                Gizmos.color = Color.yellow;
                Gizmos.DrawCube(pos, size);

                size = new Vector3(navMeshParams.m_agentRadius, navMeshParams.m_agentHeight, navMeshParams.m_agentRadius);
                Gizmos.color = Color.cyan;
                Gizmos.DrawCube(pos.x0z() * 2.0f + size * 0.5f, size);

                UnityEditor.Handles.matrix = prevMatrixHandles;
                Gizmos.matrix = prevMatrixGizmos;
            }//*/

            if (connectorEdges != null)
            {
                var prevMatrixHandles = UnityEditor.Handles.matrix;
                var prevMatrixGizmos = Gizmos.matrix;

                UnityEditor.Handles.matrix = transform.localToWorldMatrix;
                Gizmos.matrix = transform.localToWorldMatrix;

                foreach (var edge in connectorEdges)
                {
                    UnityEditor.Handles.DrawBezier(edge.p0, edge.p1, edge.p0, edge.p1, Color.red, null, 15.0f);

                    var center = (edge.p0 + edge.p1) * 0.5f;
                    Gizmos.color = Color.cyan;

                    float length = 0.1f;

                    Gizmos.DrawLine(center, center + edge.normal * length);

                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(center, center + edge.perp * length);
                }

                UnityEditor.Handles.matrix = prevMatrixHandles;
                Gizmos.matrix = prevMatrixGizmos;
            }
        }

        [Button("Reset Bounding Volume")]
        void ResetBoundingVolumes()
        {
            var boxColliders = GetComponentsInChildren<BoxCollider>();
            if (boxColliders != null)
            {
                foreach (var boxCollider in boxColliders)
                {
                    if (boxCollider != null)
                    {
                        if (boxCollider.gameObject.layer == config.boundingVolumeLayer)
                        {
                            boxCollider.gameObject.Delete();
                        }
                    }
                }
            }

            var voxelColliders = GetComponentsInChildren<VoxelCollider>();
            if (voxelColliders != null)
            {
                foreach (var voxelCollider in voxelColliders)
                {
                    if (voxelCollider != null)
                    {
                        if (voxelCollider.gameObject.layer == config.boundingVolumeLayer)
                        {
                            voxelCollider.gameObject.Delete();
                        }
                    }
                }
            }
        }

        void GenerateBoundingVolumes(Mesh srcMesh, Matrix4x4 srcMeshMatrix)
        {
            // Create parent object
            var go = gameObject.FindObjectInLayer(config.boundingVolumeLayer);
            if (go)
            {
                if (go.GetComponents<Component>().Length > 1)
                {
                    go = null;
                }
            }
            if (go == null)
            {
                go = new GameObject();
                go.name = LayerMask.LayerToName(config.boundingVolumeLayer);
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                go.layer = config.boundingVolumeLayer;
            }

            Mesh mesh = srcMesh.BakeTransform(srcMeshMatrix);
            VoxelData voxelData = VoxelTools.Voxelize(mesh, 1.0f / config.boundingVolumeVoxelSize, false, 1.01f, 1.01f, true);

            VoxelTree voxelTree = new VoxelTree(8, true, voxelData);

            if (config.boundingVolumeStrategy == AutoPieceGenerationConfig.BoundingVolumeStrategy.Box)
            {
                var bounds = voxelTree.ExtractBounds(config.boundingVolumeMaxDepth);
                if (bounds != null)
                {
                    foreach (var bound in bounds)
                    {
                        GameObject childObj = new GameObject();
                        childObj.name = go.name;
                        childObj.transform.SetParent(go.transform);
                        childObj.transform.localPosition = Vector3.zero;
                        childObj.transform.localRotation = Quaternion.identity;
                        childObj.transform.localScale = Vector3.one;
                        childObj.layer = config.boundingVolumeLayer;

                        var collider = childObj.AddComponent<BoxCollider>();
                        collider.center = bound.center;
                        collider.size = bound.size;
                    }
                }                
            }
            else if (config.boundingVolumeStrategy == AutoPieceGenerationConfig.BoundingVolumeStrategy.Voxel)
            {
                var collider = go.AddComponent<VoxelCollider>();

                // Experimental: remove all voxels from leaf nodes, and treat them as full - No improvement, more false
                // positives which mean more iterations of the algorithm
                //voxelTree.RemoveLeafVoxels(true);

                // Experimental: remove everything below a certain depth - Mixed improvement, much faster, but more
                // false positives, and in this case it meant that the algorithm failed earlier.
                // Clamping deeper gets the same issue as removing leaf voxels.
                // voxelTree.ClampDepth(5);

                Debug.Log($"Node count = {voxelTree.countNodes}");
                collider.voxelTree = voxelTree;

                
            }
        }
    }
}
