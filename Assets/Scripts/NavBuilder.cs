using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using TrinityGen;

public class NavBuilder : MonoBehaviour
{
    public void BuildNavMesh(ArenaPiece[] pieces)
    {
        GameObject topPiece = pieces[0].gameObject;
        NavMeshSurface nav = topPiece.AddComponent<NavMeshSurface>();
        nav.BuildNavMesh();
    }
}
