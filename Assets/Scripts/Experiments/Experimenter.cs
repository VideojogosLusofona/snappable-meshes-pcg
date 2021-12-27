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
using System.Linq;
using System.Reflection;
using UnityEngine;
using NaughtyAttributes;
using SnapMeshPCG.Navigation;
using SnapMeshPCG.SelectionMethods;

namespace SnapMeshPCG.Experiments
{
    public class Experimenter : MonoBehaviour
    {
        // ///////// //
        // Constants //
        // ///////// //

        private const string experimentSelect = ":: Experiment selection ::";
        private const string experimentParams = ":: Experiment parameters ::";

        private IExperiment _experiment;

        private IDictionary<string, Type> _experiments;

        private Func<int, int> _navSeedStrategy;

        [SerializeField]
        [Dropdown(nameof(Experiment))]
        [BoxGroup(experimentSelect)]
        [OnValueChanged(nameof(OnChangeExperiment))]
        private string _experimentName;

        [SerializeField]
        [BoxGroup(experimentParams)]
        [Dropdown(nameof(Scenarios))]
        private string _scenario;

        [SerializeField]
        [BoxGroup(experimentParams)]
        [Dropdown(nameof(NavParamSets))]
        private string _navParamSet;

        [SerializeField]
        [BoxGroup(experimentParams)]
        private int _runsPerScenarioNavCombo = 1;

        [NonSerialized]
        private string[] _experimentNames;

        [NonSerialized]
        private string[] _scenarios;

        [NonSerialized]
        private string[] _navParamSets;

        private ICollection<string> Experiment
        {
            get
            {
                if (_experimentNames is null)
                {
                    const string toRm = "Experiment";

                    // Get a reference to the class type
                    Type expIType = typeof(IExperiment);

                    // Get classes which extend or implement T and are not abstract
                    _experiments = AppDomain.CurrentDomain
                        .GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .Where(t => !t.IsAbstract && expIType.IsAssignableFrom(t))
                        .ToDictionary(
                            e => e.Name.EndsWith(toRm)
                                ? e.Name.Substring(0, e.Name.Length - toRm.Length)
                                : e.Name,
                            e => e);

                    _experimentNames = _experiments.Keys.ToArray();

                    Array.Sort(_experimentNames);
                }

                return _experimentNames;
            }
        }

        private ICollection<string> Scenarios
        {
            get
            {
                if (_scenarios is null)
                {
                    OnChangeExperiment();
                }
                return _scenarios;
            }
        }

        private ICollection<string> NavParamSets
        {
            get
            {
                if (_navParamSets is null)
                {
                    OnChangeExperiment();
                }
                return _navParamSets;
            }
        }

        private void OnChangeExperiment()
        {
            _experiment = _experiments[_experimentName]
                .GetConstructor(Type.EmptyTypes)
                .Invoke(null)
                as IExperiment;

            _scenarios = _experiment.GenParamSet.Keys.ToArray();
            Array.Sort(_scenarios);
            _scenario = _scenarios[0];

            _navParamSets = _experiment.NavParamSet.Keys.ToArray();
            Array.Sort(_navParamSets);
            _navParamSet = _navParamSets[0];
        }

        [Button("Set Scenario Params in GenerationManager", enabledMode: EButtonEnableMode.Editor)]
        private void SetScenarioConfig()
        {
            GenerationManager gmInstance = FindObjectOfType<GenerationManager>();

            Type gmType = typeof(GenerationManager);

            Type smType = null;
            IDictionary<string, object> smConfig = null;

            foreach (KeyValuePair<string, object> settings in _experiment.GenParamSet[_scenario])
            {
                if (settings.Key.Equals("_selectionMethod"))
                {

                    smType = settings.Value as Type;
                }
                else if (settings.Key.Equals("_selectionParams"))
                {
                    smConfig = settings.Value as IDictionary<string, object>;
                }
                else
                {
                    FieldInfo gmField = gmType.GetField(
                        settings.Key,
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (gmField is null)
                    {
                        Debug.LogWarning($"Unknown {nameof(GenerationManager)} field: '{settings.Key}'");
                    }
                    else
                    {
                        gmField.SetValue(gmInstance, settings.Value);
                    }
                }
            }

            if (smType != null)
            {
                AbstractSMConfig smCfgInstance =
                    AbstractSMConfig.GetInstance(smType);

                FieldInfo gmFieldSmName = gmType.GetField(
                    "_selectionMethod",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                FieldInfo gmFieldSmCfg = gmType.GetField(
                    "_selectionParams",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                gmFieldSmName.SetValue(
                    gmInstance,
                    SMManager.Instance.GetNameFromType(smType));

                gmFieldSmCfg.SetValue(gmInstance, smCfgInstance);

                if (smConfig != null)
                {
                    foreach (KeyValuePair<string, object> smSettings in smConfig)
                    {
                        FieldInfo smField = smType.GetField(
                            smSettings.Key,
                            BindingFlags.NonPublic | BindingFlags.Instance);

                        if (smField is null)
                        {
                            Debug.LogWarning($"Unknown {smCfgInstance.GetType().Name} field: '{smSettings.Key}'");
                        }
                        else
                        {
                            smField.SetValue(smCfgInstance, smSettings.Value);
                        }
                    }
                }
            }
        }

        [Button("Set Nav Params in NavController", enabledMode: EButtonEnableMode.Editor)]
        private void SetNavParams()
        {
            NavScanner nsInstance = FindObjectOfType<NavScanner>();

            Type nsType = typeof(NavScanner);

            _navSeedStrategy = null;

            foreach (KeyValuePair<string, object> settings in _experiment.NavParamSet[_navParamSet])
            {
                if (settings.Key.Equals("seedStrategy"))
                {
                    _navSeedStrategy = settings.Value as Func<int, int>;
                }
                else
                {
                    FieldInfo nsField = nsType.GetField(
                        settings.Key,
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    if (nsField is null)
                    {
                        Debug.LogWarning($"Unknown {nameof(NavScanner)} field: '{settings.Key}'");
                    }
                    else
                    {
                        nsField.SetValue(nsInstance, settings.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Star currently selected experiment.
        /// </summary>
        [Button("Start Experiment", enabledMode: EButtonEnableMode.Editor)]
        private void StartExperiment()
        {
            string currentScenario = _scenario;
            string currentNavParamSet = _navParamSet;

            GenerationManager gmInstance = FindObjectOfType<GenerationManager>();
            NavScanner nsInstance = FindObjectOfType<NavScanner>();

            Type gmType = typeof(GenerationManager);
            Type nsType = typeof(NavScanner);

            string savedGm = JsonUtility.ToJson(gmInstance);

            FieldInfo gmFieldSmCfg = gmType.GetField(
                "_selectionParams",
                BindingFlags.NonPublic | BindingFlags.Instance);

            object gmSmConfig = gmFieldSmCfg.GetValue(gmInstance);
            string savedSmCfg = JsonUtility.ToJson(gmSmConfig);

            string savedNs = JsonUtility.ToJson(nsInstance);

            MethodInfo genMeth = gmType.GetMethod(
                "GenerateMap",
                BindingFlags.NonPublic | BindingFlags.Instance);

            FieldInfo nsSeed = nsType.GetField(
                "_seed", BindingFlags.NonPublic | BindingFlags.Instance);

            int navInitSeed = (int)nsSeed.GetValue(nsInstance);

            foreach (string scenario in _scenarios)
            {
                _scenario = scenario;
                foreach (string navParamSet in _navParamSets)
                {
                    _navParamSet = navParamSet;
                    SetScenarioConfig();
                    SetNavParams();

                    for (int i = 0; i < _runsPerScenarioNavCombo; i++)
                    {
                        int navSeed;

                        if (_navSeedStrategy is null)
                        {
                            navSeed = (int)nsSeed.GetValue(nsInstance);
                        }
                        else
                        {
                            navSeed = _navSeedStrategy.Invoke(navInitSeed + i);
                            nsSeed.SetValue(nsInstance, navSeed);
                        }

                        genMeth.Invoke(gmInstance, null);

                        Debug.Log($"=== EXP LOG ({_scenario}, {_navParamSet}, {i}) ===\n"
                            + $"Generation took {gmInstance.GenTimeMillis} ms\n"
                            + $"Nav validation took {nsInstance.ValidationTimeMillis} ms "
                            + $"(mean conns {nsInstance.MeanValidConnections:p}, "
                            + $"largest cluster {nsInstance.RelAreaLargestCluster:p}) "
                            + $"with seed = {navSeed}");
                    }
                }
            }

            JsonUtility.FromJsonOverwrite(savedNs, nsInstance);

            if (savedSmCfg != null)
            {
                JsonUtility.FromJsonOverwrite(savedSmCfg, gmSmConfig);
            }

            JsonUtility.FromJsonOverwrite(savedGm, gmInstance);

            _scenario = currentScenario;
            _navParamSet = currentNavParamSet;
        }
    }
}