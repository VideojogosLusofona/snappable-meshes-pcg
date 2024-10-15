using UnityEngine;
using NaughtyAttributes;

namespace SnapMeshPCG
{
    [CreateAssetMenu(menuName = "SnapMesh/AutoGen Config")]
    public class AutoPieceGenerationConfig : ScriptableObject
    {
        public enum ConnectorStrategy { None, Simple, LengthBased };
        public enum BoundingVolumeStrategy { None, Box, Voxel };

        [Expandable]
        public NavMeshGeneratorConfig   navMeshProperties;

        [Header("Connector Generation")]
        public Vector3              upDirection = new Vector3(0.0f, 1.0f, 0.0f);
        public float                sampleLength = 0.05f;
        public float                sampleHeight = 0.1f;
        public float                sampleDirTolerance = 5;
        public float                edgeMaxAngleWithXZ = 180;
        public int                  probeCount = 16;
        [Range(0.5f, 2.0f)]
        public float                probeRadiusScale = 1.25f;
        public float                directionAngleTolerance = 10;
        public float                normalAngleTolerance = 50;
        public float                mergeAngleTolerance = 15;
        public float                mergeDistanceTolerance = 0.25f;
        public ConnectorStrategy    connectorStrategy = ConnectorStrategy.LengthBased;
        private bool                isConnectorActive => connectorStrategy != ConnectorStrategy.None;
        private bool                isSimple => connectorStrategy == ConnectorStrategy.Simple;
        private bool                isLength => connectorStrategy == ConnectorStrategy.LengthBased;
        [ShowIf("isConnectorActive")]
        public bool                 forceUpDirection = false;
        [ShowIf("isSimple")]
        public int                  pinCount = 1;
        [ShowIf("isLength")]
        public float                pinsPerUnit = 1;
        [ShowIf("isLength")]
        public int                  maxPinCount = 0;
        [ShowIf("hasMaxPinCount")]
        public bool                 deleteLargeConnectors = false;
        private bool                hasMaxPinCount => isLength && maxPinCount > 0;
        
        public BoundingVolumeStrategy   boundingVolumeStrategy = BoundingVolumeStrategy.None;
        private bool                    hasBoundingVolume => boundingVolumeStrategy != BoundingVolumeStrategy.None;
        private bool                    isBoxVolume => boundingVolumeStrategy == BoundingVolumeStrategy.Box;
        [ShowIf("hasBoundingVolume"), Layer]
        public int                      boundingVolumeLayer;
        [ShowIf("hasBoundingVolume")]
        public float                    boundingVolumeVoxelSize = 0.25f;
        [ShowIf("isBoxVolume")]
        public int                      boundingVolumeMaxDepth = 4;

        public float agentRadius => (navMeshProperties) ? (navMeshProperties.agentRadius) : 1.0f;
        public float agentStep => (navMeshProperties) ? (navMeshProperties.agentStep) : 0.4f;
        public float agentMaxSlope => (navMeshProperties) ? (navMeshProperties.agentMaxSlope) : 60.0f;
    }
}