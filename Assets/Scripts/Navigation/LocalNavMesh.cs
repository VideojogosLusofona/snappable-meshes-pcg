using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Detour;
using static Recast;

namespace SnapMeshPCG
{
    public class LocalNavMesh : MonoBehaviour
    {
        [SerializeField]
        private NavMeshGeneratorConfig navMeshConfig;
        [SerializeField]
        private bool                   displayNavmesh;
        [SerializeField]
        private bool                   displayWireframeTriangles = true;
        [SerializeField, ShowIf(nameof(canShowNormals))]
        private bool                   displayNormals = true;
        [SerializeField]
        private bool                    displayShellMesh;        

        RcdtcsUnityUtils.SystemHelper       recast;
        RcdtcsUnityUtils.RecastMeshParams   navMeshParams;
        Mesh                                navigationMesh;
        Mesh                                shellMesh;

        private bool canShowNormals => displayNavmesh && (!displayWireframeTriangles);
        public bool isInit => recast != null;

        [Button("Build")]
        public void Build(bool worldSpace = true)
        {
            if (navMeshConfig == null) return;

            var sourceMeshFilters = GetComponentsInChildren<MeshFilter>();

            UnityEditor.EditorUtility.DisplayProgressBar("Building...", "Creating nav mesh...", 0.1f);

            navMeshParams = new RcdtcsUnityUtils.RecastMeshParams();
            navMeshParams.m_cellSize = navMeshConfig.agentRadius / 4.0f;// 1.0f / config.voxelDensity;
            navMeshParams.m_cellHeight = navMeshParams.m_cellSize / 2.0f; // 1.0f / (config.voxelDensity * config.verticalDensityMultiplier);

            navMeshParams.m_agentHeight = navMeshParams.m_cellHeight;
            navMeshParams.m_agentRadius = navMeshConfig.agentRadius;
            navMeshParams.m_agentMaxClimb = navMeshConfig.agentStep;
            navMeshParams.m_agentMaxSlope = navMeshConfig.agentMaxSlope;

            //Debug.Log($"Voxel size = {navMeshParams.m_cellSize}, {navMeshParams.m_cellHeight}, {navMeshParams.m_cellSize}");
            //Debug.Log($"Agent size = {navMeshParams.m_agentRadius}/{navMeshParams.m_agentHeight}");

            navMeshParams.m_regionMinSize = (navMeshParams.m_agentRadius * navMeshConfig.minAreaInAgents) / navMeshParams.m_cellSize; // config.minArea;
            navMeshParams.m_regionMergeSize = navMeshParams.m_regionMinSize * 5.0f; // config.minArea * 10.0f;
            navMeshParams.m_monotonePartitioning = false;

            //Debug.Log($"Minimum region size (voxels^2) = {navMeshParams.m_regionMinSize * navMeshParams.m_regionMinSize}");

            navMeshParams.m_edgeMaxLen = navMeshParams.m_agentRadius * 8; // 0.5f;
            navMeshParams.m_edgeMaxError = 1.3f;
            navMeshParams.m_vertsPerPoly = 6;
            navMeshParams.m_detailSampleDist = 1;
            navMeshParams.m_detailSampleMaxError = 1;

            recast = new RcdtcsUnityUtils.SystemHelper();

            recast.SetNavMeshParams(navMeshParams);
            recast.ClearComputedData();
            recast.ClearMesh();

            foreach (var meshFilter in sourceMeshFilters)
            {
                Mesh sourceMesh = meshFilter.sharedMesh;
                if (sourceMesh == null) continue;

                recast.AddMesh(sourceMesh, meshFilter.gameObject);
            }
            recast.ComputeSystem();

            navigationMesh = recast.GetPolyMesh((worldSpace) ? (Matrix4x4.identity) : (transform.worldToLocalMatrix));

            UnityEditor.EditorUtility.ClearProgressBar();
        }

        public void SetNavMeshConfig(NavMeshGeneratorConfig config)
        {
            navMeshConfig = config;
        }

        public Mesh GetMesh(bool worldSpace = true)
        {
            if (navigationMesh == null)
            {
                Build(worldSpace);
            }

            return navigationMesh;
        }

        public Polyline GetPath(Vector3 start, Vector3 end, bool includeEndpoints, bool computeNormals)
        {
            if ((recast == null) || (recast.m_navQuery == null))
            {
                Build();
                if (recast == null) return null;
            }
            var path = RcdtcsUnityUtils.ComputeSmoothPath(recast.m_navQuery, start, end);
            if (path == null) return null;

            Polyline polyline = new Polyline();
            for (int i = 0; i < path.m_nsmoothPath * 3; i+=3)
            {
                polyline.Add(new Vector3(path.m_smoothPath[i], path.m_smoothPath[i + 1], path.m_smoothPath[i + 2]));
            }

            if (includeEndpoints)
            {
                if (Vector3.Distance(start, polyline[0]) > 1e-3)
                {
                    polyline.Insert(0, start);
                }
                if (Vector3.Distance(end, polyline[polyline.Count - 1]) > 1e-3)
                {
                    polyline.Add(end);
                }
            }

            if (computeNormals)
            {
                polyline.normalCount = polyline.Count;

                var navQuery = recast.m_navQuery;
                var filter = new Detour.dtQueryFilter();
                var extents = new float[3] { 10.0f, 10.0f, 10.0f };
                var polyMesh = recast.m_pmesh;
                var bmin = new Vector3(recast.m_cfg.bmin[0], recast.m_cfg.bmin[1], recast.m_cfg.bmin[2]);

                for (int i = 0; i < polyline.Count; i++)
                {
                    Vector3 pos = polyline[i];
                    var p = new float[3] { pos.x, pos.y, pos.z };

                    // Step 1: Find the nearest polygon
                    uint    nearestRef = 0;
                    var     nearestPoint = new float[3] { 0.0f, 0.0f, 0.0f };
                    uint    polyRef = navQuery.findNearestPoly(p, extents, filter, ref nearestRef, ref nearestPoint);

                    if (polyRef == 0)
                    {
                        polyline.SetNormal(i, Vector3.up);
                        continue;
                    }

                    // Step 2: Get the vertices of the polygon
                    Vector3[] vertices = recast.GetPoly(polyRef);

                    if (vertices.Length < 3)
                    {
                        polyline.SetNormal(i, Vector3.up); // Default to an upward normal if not enough vertices
                        continue;
                    }

                    // Step 3: Calculate the normal
                    Vector3 v0 = vertices[0];
                    Vector3 v1 = vertices[1];
                    Vector3 v2 = vertices[2];

                    Vector3 edge1 = v1 - v0;
                    Vector3 edge2 = v2 - v0;

                    Vector3 normal = Vector3.Cross(edge1, edge2).normalized;

                    // Step 4: Add the position and normal to the polyline
                    polyline.SetNormal(i, normal);
                }
            }

            return polyline;
        }

        public Vector3 GetPointInNavmesh(Vector3 center)
        {
            var p = new float[] { center.x, center.y, center.z };
            p = RcdtcsUnityUtils.GetClosestPointOnNavMesh(recast.m_navQuery, p);

            return new Vector3(p[0], p[1], p[2]);
        }

        // Temporary variables for HasLOS
        float[] hitNormal = new float[3];
        uint[] path = new uint[1024];

        public bool HasLOS(Vector3 start, Vector3 end)
        {
            if ((recast == null) || (recast.m_navQuery == null))
            {
                Build();
                if (recast == null) return false;
            }

            var navQuery = recast.m_navQuery;
            var filter = new Detour.dtQueryFilter();
            var extents = new float[3] { navMeshParams.m_agentRadius, navMeshParams.m_agentHeight, navMeshParams.m_agentRadius };

            // Convert start and end to float arrays
            var startArray = new float[] { start.x, start.y, start.z };
            var endArray = new float[] { end.x, end.y, end.z };

            // Find nearest polygons to start and end points
            uint startRef = 0, endRef = 0;
            float[] nearestStart = new float[3];
            float[] nearestEnd = new float[3];
            navQuery.findNearestPoly(startArray, extents, filter, ref startRef, ref nearestStart);
            navQuery.findNearestPoly(endArray, extents, filter, ref endRef, ref nearestEnd);

            if (startRef == 0 || endRef == 0)
            {
                return false; // One or both points are outside the navmesh
            }

            // Perform a raycast
            float   t = 0.0f;
            int     pathCount = 0;
            navQuery.raycast(startRef, nearestStart, nearestEnd, filter, ref t, hitNormal, path, ref pathCount, path.Length);

            // Check if the ray reached the target
            return t >= 1.0f;
        }

        public bool HasPlanarLOS(Vector3 start, Vector3 end, float toleranceDegrees = 20.0f)
        {
            if ((recast == null) || (recast.m_navQuery == null))
            {
                Build();
                if (recast == null) return false;
            }

            var navQuery = recast.m_navQuery;
            var filter = new Detour.dtQueryFilter();
            var extents = new float[3] { navMeshParams.m_agentRadius, navMeshParams.m_agentHeight, navMeshParams.m_agentRadius };

            // Convert start and end to float arrays
            var startArray = new float[] { start.x, start.y, start.z };
            var endArray = new float[] { end.x, end.y, end.z };

            // Find nearest polygons to start and end points
            uint startRef = 0, endRef = 0;
            float[] nearestStart = new float[3];
            float[] nearestEnd = new float[3];
            navQuery.findNearestPoly(startArray, extents, filter, ref startRef, ref nearestStart);
            navQuery.findNearestPoly(endArray, extents, filter, ref endRef, ref nearestEnd);

            if (startRef == 0 || endRef == 0)
            {
                return false; // One or both points are outside the navmesh
            }

            // Get the normals of the polygons
            Vector3 startNormal = GetPolygonNormal(startRef);
            Vector3 endNormal = GetPolygonNormal(endRef);

            float dp = Vector3.Dot(startNormal, endNormal);
            if (Mathf.Abs(dp) < Mathf.Cos(Mathf.Deg2Rad * toleranceDegrees))
            {
                return false;
            }

            // Perform a raycast
            float t = 0.0f;
            int pathCount = 0;
            navQuery.raycast(startRef, nearestStart, nearestEnd, filter, ref t, hitNormal, path, ref pathCount, path.Length);

            // Check if the ray reached the target
            return t >= 1.0f;
        }

        private Vector3 GetPolygonNormal(uint polyRef)
        {
            uint polyIndex = recast.m_navMesh.decodePolyIdPoly(polyRef);

            return recast.GetPolyNormal(polyIndex);        
        }

        [Button("Build Shell Mesh")]
        public void BuildShellMesh()
        {
            if (navigationMesh == null)
            {
                navigationMesh = GetMesh();
            }
            if (navigationMesh == null) return;

            shellMesh = MeshTools.ExtrudeMesh(navigationMesh, Vector3.up * 0.1f, Vector3.down * 0.1f);
        }

        private void OnDrawGizmosSelected()
        {
            if (displayNavmesh)
            {
                if (navigationMesh == null)
                {
                    navigationMesh = GetMesh();
                }
                if (navigationMesh != null)
                {
                    if (displayWireframeTriangles)
                    {
                        Gizmos.color = Color.black;
                        Gizmos.DrawWireMesh(navigationMesh);
                    }
                    else if ((recast != null) && (recast.m_pmesh != null))
                    {
                        for (uint i = 0; i < recast.m_pmesh.npolys; i++)
                        {
                            var vertices = recast.GetPoly(i);
                            var center = Vector3.zero;
                            for (int j = 0; j < vertices.Length; j++)
                            {
                                Gizmos.color = Color.black;
                                Gizmos.DrawLine(vertices[j], vertices[(j + 1) % vertices.Length]);
                                center += vertices[j];
                            }

                            center /= vertices.Length;

                            if (displayNormals)
                            {
                                var n = recast.GetPolyNormal(i);

                                Gizmos.color = Color.cyan;
                                Gizmos.DrawLine(center, center + n);
                            }
                        }
                    }
                    Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
                    Gizmos.DrawMesh(navigationMesh);
                }
            }
            if (displayShellMesh)
            {
                if (shellMesh != null)
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawWireMesh(shellMesh);
                    Gizmos.color = new Color(0.9f, 0.4f, 0.2f, 0.5f);
                    Gizmos.DrawMesh(shellMesh);
                }
            }
        }

        public int GetPolyCount()
        {
            rcPolyMesh polyMesh = recast.m_pmesh;
            return polyMesh.npolys;
        }

        public Vector3 GetPolyCentroid(uint i)
        {
            return recast.GetPolyCentroid(i);
        }
        public Vector3 GetPolyBoundCenter(uint i)
        {
            return recast.GetPolyBoundCenter(i);
        }

        public Vector3 GetPolyNormal(uint i)
        {
            return recast.GetPolyNormal(i);
        }

        internal List<uint> GetNeighbours(uint i)
        {
            return recast.GetNeighbours(i);
        }
    }
}