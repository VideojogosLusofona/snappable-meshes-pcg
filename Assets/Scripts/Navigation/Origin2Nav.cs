/*
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
using System;

namespace SnapMeshPCG.Navigation
{
    /// <summary>
    /// Record of an origin point and it's respective navigation point.
    /// </summary>
    [Serializable]
    public struct Origin2Nav
    {
        public Vector3 origin;
        public Vector3 nav;

        public Origin2Nav(Vector3 origin, Vector3 nav)
        {
            this.origin = origin;
            this.nav = nav;
        }
    }
}