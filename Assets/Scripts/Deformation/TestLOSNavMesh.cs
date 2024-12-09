using SnapMeshPCG;
using UnityEngine;

public class TestLOSNavMesh : MonoBehaviour
{
    [SerializeField] private Transform startPos;
    [SerializeField] private Transform endPos;

    private void OnDrawGizmos()
    {
        if (startPos == null) return;
        if (endPos == null) return;

        LocalNavMesh localNavMesh = GetComponent<LocalNavMesh>();
        if (localNavMesh == null) return;
        if (!localNavMesh.isInit) return;

        Gizmos.color = Color.green;

        var start = localNavMesh.GetPointInNavmesh(startPos.position);
        var end = localNavMesh.GetPointInNavmesh(endPos.position);

        if (localNavMesh.HasLOS(start, end))
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = Color.red;
        }

        Gizmos.DrawLine(start, end);
    }
}
