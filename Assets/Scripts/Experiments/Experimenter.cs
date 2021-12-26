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
        private IExperiment _exp = new BenchmarkExperiment();


        /// <summary>
        /// Setup for experiment (a)
        /// </summary>
        [Button("test", enabledMode: EButtonEnableMode.Editor)]
        private void SetupA()
        {

            GenerationManager gmInstance = FindObjectOfType<GenerationManager>();

            Type gmType = typeof(GenerationManager);

            Type smType = null;
            IDictionary<string, object> smParams = null;

            string savedGm = JsonUtility.ToJson(gmInstance);

            string savedSmCfg = null;

            object gmSmParams = null;

            foreach (KeyValuePair<string, object> settings in _exp.Runs["(a)"])
            {
                if (settings.Key.Equals("_selectionMethod"))
                {

                    smType = settings.Value as Type;
                }
                else if (settings.Key.Equals("_selectionParams"))
                {
                    smParams = settings.Value as IDictionary<string, object>;
                }
                else
                {
                    FieldInfo gmField = gmType.GetField(
                        settings.Key,
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    gmField.SetValue(gmInstance, settings.Value);
                }
            }

            if (smType != null)
            {
                AbstractSMConfig smCfgInstance =
                    AbstractSMConfig.GetInstance(smType);

                FieldInfo gmFieldSmName = gmType.GetField(
                    "_selectionMethod",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                FieldInfo gmFieldSmParams = gmType.GetField(
                    "_selectionParams",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                gmSmParams = gmFieldSmParams.GetValue(gmInstance);
                savedSmCfg = JsonUtility.ToJson(gmSmParams);

                gmFieldSmName.SetValue(
                    gmInstance,
                    SMManager.Instance.GetNameFromType(smType));

                gmFieldSmParams.SetValue(gmInstance, smCfgInstance);

                if (smParams != null)
                {
                    foreach (KeyValuePair<string, object> smSettings in smParams)
                    {
                        FieldInfo smField = smType.GetField(
                            smSettings.Key,
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        smField.SetValue(smCfgInstance, smSettings.Value);

                    }
                }
            }

            MethodInfo genMeth = gmType.GetMethod("GenerateMap", BindingFlags.NonPublic | BindingFlags.Instance);

            genMeth.Invoke(gmInstance, null);

            if (savedSmCfg != null)
            {
                JsonUtility.FromJsonOverwrite(savedSmCfg, gmSmParams);
            }

            JsonUtility.FromJsonOverwrite(savedGm, gmInstance);

        }

    }
}