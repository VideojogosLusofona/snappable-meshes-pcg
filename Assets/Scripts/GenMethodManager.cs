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
        /// Get generation method configurator type from simplified name.
        /// </summary>
        /// <param name="name">
        /// Simple name of generation method configurator class.
        /// </param>
        /// <returns>
        /// The generation method configurator's type.
        /// </returns>
        public Type GetTypeFromName(string name) => genMethCfgTable[name];

        /// <summary>
        /// Get simplified name from type.
        /// </summary>
        /// <param name="type">Type of generation method configurator.</param>
        /// <returns>Simplified name of generation method configurator.</returns>
        public string GetNameFromType(Type type)
        {
            foreach (KeyValuePair<string, Type> kvp in genMethCfgTable)
            {
                if (kvp.Value.Equals(type))
                {
                    return kvp.Key;
                }
            }
            return null;
        }

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
                .ToDictionary(t => SimpleName(t.FullName), t => t);
        }

        /// <summary>
        /// Simplify the name of a generation method by removing the namespace
        /// and the "GMConfig" substring in the end.
        /// </summary>
        /// <param name="fqName">
        /// The fully qualified name of the generation method.
        /// </param>
        /// <returns>
        /// The simplified name of the generation method.
        /// </returns>
        private string SimpleName(string fqName)
        {
            string simpleName = fqName;

            // Strip namespace
            if (simpleName.Contains("."))
            {
                simpleName = fqName.Substring(fqName.LastIndexOf(".") + 1);
            }

            // Strip "GMConfig"
            if (simpleName.EndsWith("GMConfig"))
            {
                simpleName = simpleName.Substring(
                    0, simpleName.Length - "GMConfig".Length);
            }

            // Return simple name
            return simpleName;
        }
    }
}