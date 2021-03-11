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

namespace SnapMeshPCG
{
    /// <summary>
    /// Useful extension methods for the SnapRules enumeration.
    /// </summary>
    public static class SnapRulesExtensions
    {
        /// <summary>
        /// Is the use of colours specified in this ruleset?
        /// </summary>
        /// <param name="rules">The ruleset to check for color usage.</param>
        /// <returns>True if this ruleset uses colors, false otherwise.</returns>
        public static bool UseColours(this SnapRules rules)
            => (rules & SnapRules.Colours) == SnapRules.Colours;

        /// <summary>
        /// Is the use of pins specified in this ruleset?
        /// </summary>
        /// <param name="rules">The ruleset to check for pin usage.</param>
        /// <returns>True if this ruleset uses pins, false otherwise.</returns>
        public static bool UsePins(this SnapRules rules)
            => (rules & SnapRules.Pins) == SnapRules.Pins;
    }
}