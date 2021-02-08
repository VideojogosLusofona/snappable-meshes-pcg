﻿/*
 * Copyright 2021 Snappable Meshes PCG contributors
 * (https://github.com/VideojogosLusofona/snappable-meshes-pcg)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;
using UnityEngine.AI;
using UnityEditor.AI;

using SnapMeshPCG;

[RequireComponent(typeof(NavMeshSurface))]
public class NavBuilder : MonoBehaviour
{
    /*
    // Give some nice defaults
    [SerializeField] private float bodyRadius = 0.5f;
    [SerializeField] private float bodyHeight = 2;

    [Range(0,60)]
    [SerializeField] private float maxSlope = 30;
    [SerializeField] private float maxStepHeight = 1;
    */

   //s [SerializeField] private NavWalker demoCharacter;


    
    public void BuildNavMesh(MapPiece[] pieces)
    {
        NavMeshSurface dummyNav = GetComponent<NavMeshSurface>();
        if(dummyNav == null)
            dummyNav = gameObject.AddComponent<NavMeshSurface>();

        GameObject topPiece = pieces[0].gameObject;
        NavMeshSurface nav = topPiece.AddComponent<NavMeshSurface>();

        print($"Building NavMesh at parent piece: {nav.gameObject.name}");
        nav.BuildNavMesh();

       // demoCharacter.mapPieces = pieces;
       // Instantiate(demoCharacter.gameObject, new Vector3(0,10,0),Quaternion.identity);


    }

    

}
