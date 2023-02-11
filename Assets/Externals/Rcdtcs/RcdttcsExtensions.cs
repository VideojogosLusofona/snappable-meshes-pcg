using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RcdtcsUnityUtils;
using static Recast;

public static class RcdttcsExtensions 
{
    public static Mesh GetPolyMesh(this SystemHelper recast, Matrix4x4 transform)
    {
        rcPolyMesh polyMesh = recast.m_pmesh;

        List<Vector3>   vertices = new List<Vector3>();
        List<int>       tris = new List<int>();
        List<int>       poly = new List<int>();

        Vector3 bmin = new Vector3(recast.m_cfg.bmin[0], recast.m_cfg.bmin[1], recast.m_cfg.bmin[2]);

        for (int i = 0; i < polyMesh.npolys; i++)
        {
            int pIndex = i * polyMesh.nvp * 2;

            poly.Clear();
            for (int j = 0; j < polyMesh.nvp; j++)
            {
                if (polyMesh.polys[pIndex + j] == Recast.RC_MESH_NULL_IDX)
                    break;

                int vIndex = polyMesh.polys[pIndex + j] * 3;

                Vector3 vertex = new Vector3(bmin.x + polyMesh.verts[vIndex + 0] * polyMesh.cs,
                                             bmin.y + polyMesh.verts[vIndex + 1] * polyMesh.ch + 0.001f,
                                             bmin.z + polyMesh.verts[vIndex + 2] * polyMesh.cs);
                int idx = -1;
                for (int k = 0; k < vertices.Count; k++)
                {
                    var v = vertices[k];

                    if ((vertex.x == v.x) &&
                        (vertex.y == v.y) &&
                        (vertex.z == v.z))
                    {
                        idx = k;
                        break;
                    }
                }

                if (idx == -1)
                {
                    vertices.Add(vertex);
                    idx = vertices.Count - 1;
                }

                poly.Add(idx);
            }

            for (int j = 2; j < poly.Count; j++)
            {
                tris.Add(poly[0]);
                tris.Add(poly[j - 1]);
                tris.Add(poly[j]);
            }
        }

        for (int j = 0; j < vertices.Count; j++)
        {
            vertices[j] = transform * vertices[j].xyz1();
        }

        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        return mesh;
    }

    public static Mesh GetDetailMesh(this SystemHelper recast, Matrix4x4 transform)
    {
        rcPolyMeshDetail polyMesh = recast.m_dmesh;

        List<Vector3> vertices = new List<Vector3>();
        List<int> tris = new List<int>();

        for (int i = 0; i < polyMesh.nverts; i++)
        {
            int vIndex = i * 3;
            Vector3 vertex = new Vector3(polyMesh.verts[vIndex + 0],
                                         polyMesh.verts[vIndex + 1] + 0.001f,
                                         polyMesh.verts[vIndex + 2]);

            vertices.Add(transform * vertex.xyz1());
        }

        for (int i = 0; i < polyMesh.nmeshes; i++)
        {
            int mIndex = i * 4;
            int bverts = (int)polyMesh.meshes[mIndex + 0];
            int btris = (int)polyMesh.meshes[mIndex + 2];
            int ntris = (int)polyMesh.meshes[mIndex + 3];
            int trisIndex = btris * 4;
            for (int j = 0; j < ntris; j++)
            {
                tris.Add(bverts + polyMesh.tris[trisIndex + j * 4 + 0]);
                tris.Add(bverts + polyMesh.tris[trisIndex + j * 4 + 1]);
                tris.Add(bverts + polyMesh.tris[trisIndex + j * 4 + 2]);
            }
        }

        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        return mesh;
    }
}
