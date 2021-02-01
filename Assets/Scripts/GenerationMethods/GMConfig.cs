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

using System;
using UnityEngine;
using UnityEditor;

namespace TrinityGen.GenerationMethods
{
    public abstract class GMConfig : ScriptableObject
    {
        private const string gmFolder = "GMs";

        public abstract GenerationMethod Method { get; }

        public static GMConfig GetInstance(Type type)
        {
            GMConfig gmConfig =
                Resources.Load<GMConfig>($"{gmFolder}/{type.Name}");
            if (gmConfig is null)
            {
                gmConfig = CreateInstance(type) as GMConfig;
                AssetDatabase.CreateAsset(
                    gmConfig, $"Assets/Resources/{gmFolder}/{type.Name}.asset");
            }
            return gmConfig;
        }
    }
}