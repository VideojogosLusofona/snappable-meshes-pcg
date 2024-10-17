using SnapMeshPCG;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace SnapMeshPCG
{
    public class LocalNavMesh : MonoBehaviour
    {
        [SerializeField]
        private NavMeshGeneratorConfig navMeshConfig;
        [SerializeField]
        private bool                   displayNavmesh;

        RcdtcsUnityUtils.SystemHelper       recast;
        RcdtcsUnityUtils.RecastMeshParams   navMeshParams;
        Mesh                                navigationMesh;

        public void Build()
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

            navigationMesh = recast.GetPolyMesh(Matrix4x4.identity);

            UnityEditor.EditorUtility.ClearProgressBar();
        }

        public void SetNavMeshConfig(NavMeshGeneratorConfig config)
        {
            navMeshConfig = config;
        }

        public Mesh GetMesh()
        {
            if (navigationMesh == null)
            {
                Build();
            }

            return navigationMesh;
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
                    Gizmos.color = Color.black;
                    Gizmos.DrawWireMesh(navigationMesh);
                    Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
                    Gizmos.DrawMesh(navigationMesh);
                }
            }
        }

        public Polyline GetPath(Vector3 start, Vector3 end, bool includeEndpoints)
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

            return polyline;
        }

        public Vector3 GetPointInNavmesh(Vector3 center)
        {
            var p = new float[] { center.x, center.y, center.z };
            p = RcdtcsUnityUtils.GetClosestPointOnNavMesh(recast.m_navQuery, p);

            return new Vector3(p[0], p[1], p[2]);
        }
    }
}