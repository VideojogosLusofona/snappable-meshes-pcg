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

using System.Collections.Generic;
using SnapMeshPCG.SelectionMethods;

namespace SnapMeshPCG.Experiments
{
    public class TestExperiment : IExperiment
    {
        public IDictionary<string, IDictionary<string, object>> GenParamSet =>
            new Dictionary<string, IDictionary<string, object>>()
            {
                ["hello"] = new Dictionary<string, object>()
                {
                    ["_useSeed"] = true,
                    ["_seed"] = 100,
                    ["_invalid"] = "lala"
                },
                ["this"] = new Dictionary<string, object>()
                {
                    ["_pieceDistance"] = 0.000005f,
                    ["_maxFailures"] = (uint)10,
                },

                ["is"] = new Dictionary<string, object>()
                {
                    ["_checkOverlaps"] = false,
                    ["_matchingRules"] = SnapRules.Pins,
                },

                ["a"] = new Dictionary<string, object>()
                {
                    ["_pinCountTolerance"] = (uint)3,
                    ["_starterConTol"] = (uint)2,
                },

                ["test"] = new Dictionary<string, object>()
                {
                    ["_useSeed"] = false,
                    ["_starterConTol"] = (uint)1,
                    ["_selectionMethod"] = typeof(CorridorSMConfig),
                    ["_selectionParams"] = new Dictionary<string, object>()
                    {
                        ["_maxPieces"] = (uint)25,
                        ["_invalid"] = 111
                    },
                },

            };
        public IDictionary<string, IDictionary<string, object>> NavParamSet =>
            new Dictionary<string, IDictionary<string, object>>()
            {
                ["---"] = new Dictionary<string, object>()
                {
                    ["_invalid"] = 50
                }
            };
    }
}