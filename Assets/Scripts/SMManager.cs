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
using System.Linq;
using System.Collections.Generic;
using SnapMeshPCG.SelectionMethods;

namespace SnapMeshPCG
{
    /// <summary>
    /// Singleton class used for finding and keeping a record of existing
    /// selection methods (SMs).
    /// </summary>
    public class SMManager
    {
        // Unique instance of this class, instantiated lazily
        private static readonly Lazy<SMManager> instance =
            new Lazy<SMManager>(() => new SMManager());

        // Known selection methods
        private readonly IDictionary<string, Type> selMethCfgTable;

        /// <summary>
        /// Returns the singleton instance of this class.
        /// </summary>
        /// <value>The singleton instance of this class.</value>
        public static SMManager Instance => instance.Value;

        /// <summary>
        /// Array of selection method names.
        /// </summary>
        /// <value>Names of known selection methods.</value>
        public string[] SelMethodNames => selMethCfgTable.Keys.ToArray();

        /// <summary>
        /// Get selection method configurator type from simplified name.
        /// </summary>
        /// <param name="name">
        /// Simple name of selection method configurator class.
        /// </param>
        /// <returns>
        /// The selection method configurator's type.
        /// </returns>
        public Type GetTypeFromName(string name) => selMethCfgTable[name];

        /// <summary>
        /// Get simplified name from type.
        /// </summary>
        /// <param name="type">Type of selection method configurator.</param>
        /// <returns>Simplified name of selection method configurator.</returns>
        public string GetNameFromType(Type type)
        {
            foreach (KeyValuePair<string, Type> kvp in selMethCfgTable)
            {
                if (kvp.Value.Equals(type))
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        // Private constructor
        private SMManager()
        {
            // Get a reference to the selection method configurator type
            Type typeSMConfig = typeof(AbstractSMConfig);

            // Get known methods, i.e. classes which extends SMConfig,
            // and are not abstract
            selMethCfgTable = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeSMConfig) && !t.IsAbstract)
                .ToDictionary(t => SimpleName(t.FullName), t => t);
        }

        /// <summary>
        /// Simplify the name of a selection method by removing the namespace
        /// and the "SMConfig" substring in the end.
        /// </summary>
        /// <param name="fqName">
        /// The fully qualified name of the selection method.
        /// </param>
        /// <returns>
        /// The simplified name of the selection method.
        /// </returns>
        private string SimpleName(string fqName)
        {
            string simpleName = fqName;

            // Strip namespace
            if (simpleName.Contains("."))
            {
                simpleName = fqName.Substring(fqName.LastIndexOf(".") + 1);
            }

            // Strip "SMConfig"
            if (simpleName.EndsWith("SMConfig"))
            {
                simpleName = simpleName.Substring(
                    0, simpleName.Length - "SMConfig".Length);
            }

            // Return simple name
            return simpleName;
        }
    }
}