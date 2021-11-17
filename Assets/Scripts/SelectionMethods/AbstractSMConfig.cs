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

namespace SnapMeshPCG.SelectionMethods
{
    /// <summary>
    /// The base class for all selection method configurators.
    /// </summary>
    public abstract class AbstractSMConfig : ScriptableObject
    {
        // Location of the selection method configurators (i.e., of the
        // serialized scriptable objects representing the configurators)
        private const string gmFolder = "SMs";

        /// <summary>
        /// Returns the configured selection method.
        /// </summary>
        public abstract AbstractSM Method { get; }

        /// <summary>
        /// Returns an instance of the selection method configurator.
        /// </summary>
        /// <param name="type">The concrete type of the configurator.</param>
        /// <returns>An instance of the selection method configurator.</returns>
        public static AbstractSMConfig GetInstance(Type type)
        {
            // Try to load a saved configurator of this type
            AbstractSMConfig gmConfig =
                Resources.Load<AbstractSMConfig>($"{gmFolder}/{type.Name}");

            // If there's no saved configurator of this type, create a new one
            if (gmConfig is null)
            {
                // Create an instance of the configurator
                gmConfig = CreateInstance(type) as AbstractSMConfig;

                // Set this configurator as an asset for later loading
                AssetDatabase.CreateAsset(
                    gmConfig, $"Assets/Resources/{gmFolder}/{type.Name}.asset");
            }

            // Return the instance of the selection method configurator
            return gmConfig;
        }
    }
}