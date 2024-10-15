using SnapMeshPCG;
using UnityEngine;
using UnityEngine.UI;

namespace SnapMeshPCG
{
    public class LocalNavMesh : MonoBehaviour
    {
        [SerializeField]
        private NavMeshGeneratorConfig navMeshConfig;

        RcdtcsUnityUtils.SystemHelper       recast;
        RcdtcsUnityUtils.RecastMeshParams   navMeshParams;

        public void Build()
        {
            MeshFilter sourceMeshFilter = GetComponentInChildren<MeshFilter>();
            Mesh sourceMesh = (sourceMeshFilter) ? (sourceMeshFilter.sharedMesh) : (null);
            Matrix4x4 sourceMeshMatrix = sourceMeshFilter.transform.GetLocalMatrix();

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
            recast.AddMesh(sourceMesh, sourceMeshFilter.gameObject);
            recast.ComputeSystem();

            UnityEditor.EditorUtility.ClearProgressBar();
        }

        public void SetNavMeshConfig(NavMeshGeneratorConfig config)
        {
            navMeshConfig = config;
        }

        public Mesh GetMesh(Matrix4x4 matrix)
        {
            if (recast == null) return null;

            return recast.GetPolyMesh(matrix);
        }
    }
}