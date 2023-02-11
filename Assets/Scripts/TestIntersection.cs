using SnapMeshPCG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class TestIntersection : MonoBehaviour
{
    public MapPiece piece1;
    public MapPiece piece2;
    public float    scale1 = 1.0f;
    public int      intersectionTest = -1;

    private static List<BoxCollider> GetBoxColliders(MapPiece go)
    {
        List<BoxCollider> ret = new List<BoxCollider>();

        foreach (Transform child in go.transform)
        {
            BoxCollider[] colliders = child.GetComponents<BoxCollider>();
            foreach (var collider in colliders)
            {
                ret.Add(collider);
            }
        }

        return ret;
    }

    [Button("Test Box Colliders")]
    void Test()
    {
        DebugGizmo.Clear();

        if (piece1 == null) return;
        if (piece2 == null) return;

        var boxes1 = GetBoxColliders(piece1);
        var boxes2 = GetBoxColliders(piece2);

        int     count = 0;
        bool    b = false;

        foreach (var box1 in boxes1)
        {
            OBB obb1 = new OBB(box1.center, box1.size);
            obb1.Transform(box1.transform.localToWorldMatrix);

            foreach (var box2 in boxes2)
            {
                OBB obb2 = new OBB(box2.center, box2.size);
                obb2.Transform(box2.transform.localToWorldMatrix);

                DebugGizmo.AddWireOBB($"Type=OBBTest;Subtype=1;TestId={count}", obb1, Color.cyan);
                DebugGizmo.AddWireOBB($"Type=OBBTest;Subtype=2;TestId={count}", obb2, Color.magenta);

                bool test = obb1.Intersect(obb2);
                b |= test;
                Debug.Log($"Test {count} = {test}");

                count++;
            }
        }

        Debug.Log($"Intersection = {b} ({count} tests).");
    }

    [Button("Test Voxel Trees")]
    void TestVoxelTrees()
    {
        if (piece1 == null) return;
        if (piece2 == null) return;

        VoxelCollider vc1 = piece1.GetComponentInChildren<VoxelCollider>();
        VoxelCollider vc2 = piece2.GetComponentInChildren<VoxelCollider>();

        if (vc1 == null) return;
        if (vc2 == null) return;

        VoxelTree vt1 = vc1.voxelTree;
        VoxelTree vt2 = vc2.voxelTree;

        if (vt1 == null) return;
        if (vt2 == null) return;

        OBB obb1 = null, obb2 = null;
        bool b = vt1.Intersect(piece1.transform, vt2, piece2.transform, ref obb1, ref obb2, scale1, 1.0f);

        DebugGizmo.Clear("Col1");
        DebugGizmo.Clear("Col2");
        if (b)
        {
            DebugGizmo.AddWireOBB("Col1", obb1, Color.red, piece1.transform.localToWorldMatrix);
            DebugGizmo.AddWireOBB("Col2", obb2, Color.cyan, piece2.transform.localToWorldMatrix);
        }

        Debug.Log($"Intersection = {b}");
    }

    [Button("Debug Collider")]
    void DebugCollider()
    {
        BoxCollider collider = GetComponent<BoxCollider>();
        Debug.Log($"Center={collider.center}");
        Debug.Log($"Size={collider.size}");

        var obb = new OBB(collider.center, collider.size);
    }

    private void OnDrawGizmos()
    {
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider == null) return;

        var obb = new OBB(collider.center, collider.size);
        obb.Transform(transform.localToWorldMatrix);

        Gizmos.color = Color.cyan;
        obb.DrawGizmo();
    }
}
