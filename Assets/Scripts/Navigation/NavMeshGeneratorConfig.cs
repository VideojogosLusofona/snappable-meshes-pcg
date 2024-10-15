using UnityEngine;

namespace SnapMeshPCG
{
    [CreateAssetMenu(menuName = "SnapMesh/NavMeshGen Config")]
    public class NavMeshGeneratorConfig : ScriptableObject
    {
        public float agentRadius = 0.4f;
        public float agentHeight = 2.0f;
        public float agentStep = 0.4f;
        public float agentMaxSlope = 60.0f;
        public float minAreaInAgents = 7.0f;
    }
}
