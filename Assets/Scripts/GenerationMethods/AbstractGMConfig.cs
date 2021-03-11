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

namespace SnapMeshPCG.GenerationMethods
{
    /// <summary>
    /// The base class for all generation method configurators.
    /// </summary>
    public abstract class AbstractGMConfig : ScriptableObject
    {
        // Location of the generation method configurators (i.e., of the
        // serialized scriptable objects representing the configurators)
        private const string gmFolder = "GMs";

        /// <summary>
        /// Returns the configured generation method.
        /// </summary>
        public abstract AbstractGM Method { get; }

        /// <summary>
        /// Returns an instance of the generation method configurator.
        /// </summary>
        /// <param name="type">The concrete type of the configurator.</param>
        /// <returns>An instance of the generation method configurator.</returns>
        public static AbstractGMConfig GetInstance(Type type)
        {
            // Try to load a saved configurator of this type
            AbstractGMConfig gmConfig =
                Resources.Load<AbstractGMConfig>($"{gmFolder}/{type.Name}");

            // If there's no saved configurator of this type, create a new one
            if (gmConfig is null)
            {
                // Create an instance of the configurator
                gmConfig = CreateInstance(type) as AbstractGMConfig;

                // Set this configurator as an asset for later loading
                AssetDatabase.CreateAsset(
                    gmConfig, $"Assets/Resources/{gmFolder}/{type.Name}.asset");
            }

            // Return the instance of the generation method configurator
            return gmConfig;
        }
    }
}