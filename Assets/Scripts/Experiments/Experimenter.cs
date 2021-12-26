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
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using NaughtyAttributes;
using SnapMeshPCG.SelectionMethods;

namespace SnapMeshPCG.Experiments
{
    public class Experimenter : MonoBehaviour
    {

        private IDictionary<string, IDictionary<string, object>> _experiments =
            new Dictionary<string, IDictionary<string, object>>()
            {
                ["(a)"] = new Dictionary<string, object>()
                {
                    ["_useSeed"] = true,
                    ["_seed"] = -2095385667,
                    ["_pieceDistance"] = 0.0001f,
                    ["_maxFailures"] = (uint)10,
                    ["_checkOverlaps"] = true,
                    ["_matchingRules"] = SnapRules.Colours | SnapRules.Pins,
                    ["_pinCountTolerance"] = (uint)0,
                    ["_starterConTol"] = (uint)0,
                    ["_selectionMethod"] = nameof(ArenaSMConfig),
                    ["_selectionParams"] = new Dictionary<string, object>()
                    {
                        ["_maxPieces"] = 12
                    },
                //     [""] = ,
                //     [""] = ,
                //     [""] = ,
                //     [""] = ,
                //     [""] = ,
                //     [""] = ,




                },
                ["(b)"] = null,
                ["(c)"] = null,
                ["(d)"] = null,
                ["(e)"] = null,
                ["(f)"] = null,
                ["(g)"] = null,
                ["(h)"] = null,
            };


        /// <summary>
        /// Setup for experiment (a)
        /// </summary>
        [Button("test", enabledMode: EButtonEnableMode.Editor)]
        private void SetupA()
        {
            GenerationManager gmInstance = GetComponentInParent<GenerationManager>();

            Type gmType = typeof(GenerationManager);

            foreach (KeyValuePair<string, object> settings in _experiments["(a)"])
            {
                if (settings.Key.Equals("_selectionParams") || settings.Key.Equals("_selectionMethod"))
                {

                    continue;
                }
                FieldInfo field = gmType.GetField(settings.Key, BindingFlags.NonPublic | BindingFlags.Instance);
                field.SetValue(gmInstance, settings.Value);
            }
            // foreach (string s in _experiments.Keys)
            // {
            //     Debug.Log(s);
            // }

        }

    }
}