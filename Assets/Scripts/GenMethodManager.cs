/*
 * Copyright 2021 TrinityGenerator_Standalone contributors
 * (https://github.com/RafaelCS-Aula/TrinityGenerator_Standalone)
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
using System.Linq;
using System.Collections.Generic;
using TrinityGen.GenerationMethods;

namespace TrinityGen
{
    /// <summary>
    /// Singleton class used for finding and keeping a record of existing
    /// generation methods.
    /// </summary>
    public class GenMethodManager
    {
        // Unique instance of this class, instantiated lazily
        private static readonly Lazy<GenMethodManager> instance =
            new Lazy<GenMethodManager>(() => new GenMethodManager());

        // Known generation methods
        private readonly IDictionary<string, Type> genMethCfgTable;

        /// <summary>
        /// Returns the singleton instance of this class.
        /// </summary>
        /// <value>The singleton instance of this class.</value>
        public static GenMethodManager Instance => instance.Value;

        /// <summary>
        /// Array of generation method names.
        /// </summary>
        /// <value>Names of known generation methods.</value>
        public string[] GenMethodNames => genMethCfgTable.Keys.ToArray();

        /// <summary>
        /// Does the given generation method configurator FQN correspond to a
        /// known generation method configurator class?
        /// </summary>
        /// <param name="genMethCfgFQN">
        /// Fully qualified name of generation method configurator class.
        /// </param>
        /// <returns>
        /// `true` if the generation method configurator class exists in the
        /// loaded assemblies, `false` otherwise.
        /// </returns>
        public bool IsKnown(string genMethCfgFQN) =>
            genMethCfgTable.ContainsKey(genMethCfgFQN);

        /// <summary>
        /// Get generation method configurator type from its fully qualified
        /// name.
        /// </summary>
        /// <param name="genMethCfgFQN">
        /// Fully qualified name of generation method configurator class.
        /// </param>
        /// <returns>
        /// The generation method configurator's type.
        /// </returns>
        public Type GetTypeFromFQN(string genMethCfgFQN) =>
            genMethCfgTable[genMethCfgFQN];

        // Private constructor
        private GenMethodManager()
        {
            // Get a reference to the generation method configurator type
            Type typeGMConfig = typeof(GMConfig);

            // Get known methods, i.e. classes which extends GMConfig,
            // and are not abstract
            genMethCfgTable = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeGMConfig) && !t.IsAbstract)
                .ToDictionary(t => t.FullName, t => t);
        }
    }
}