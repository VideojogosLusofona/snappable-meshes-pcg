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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using NaughtyAttributes;
using SnapMeshPCG.Navigation;
using SnapMeshPCG.SelectionMethods;

// Avoid conflict with System.Diagnostics.Debug
using Debug = UnityEngine.Debug;

namespace SnapMeshPCG.Experiments
{
    /// <summary>
    /// Perform research experiments with snappable meshes scenarios.
    /// </summary>
    /// <remarks>
    /// This class is not required in a snappable mesh scene. It is used only
    /// for performing experiments, controlling the parameters of
    /// <see cref="GenerationManager"/> and <see cref="NavScanner"/>.
    /// </remarks>
    public class Experimenter : MonoBehaviour
    {
        // ///////// //
        // Constants //
        // ///////// //

        private const string experimentSelect = ":: Experiment selection ::";
        private const string experimentParams = ":: Experiment parameters ::";

        // ///////////////////////////////////////////// //
        // Experiment configuration parameters in editor //
        // ///////////////////////////////////////////// //

        // Dropdown with the names of all experiments found
        [SerializeField]
        [Dropdown(nameof(Experiment))]
        [BoxGroup(experimentSelect)]
        [OnValueChanged(nameof(OnChangeExperiment))]
        private string _experimentName;

        // Name of currently selected generation parameter set
        [SerializeField]
        [BoxGroup(experimentParams)]
        [Dropdown(nameof(GenParamSets))]
        private string _genParamSet;

        // Name of currently selected nav parameter set
        [SerializeField]
        [BoxGroup(experimentParams)]
        [Dropdown(nameof(NavParamSets))]
        private string _navParamSet;

        // How many runs to perform per gen+nav param set combination
        [SerializeField]
        [BoxGroup(experimentParams)]
        [Label("Runs per Gen+Nav combo")]
        private int _runsPerGenNavCombo = 1;

        // ///////////////////////////////////// //
        // Instance variables not used in editor //
        // ///////////////////////////////////// //

        // Current experiment data
        private IExperiment _experiment;

        // Dictionary all all found experiments, by name and concrete type
        private IDictionary<string, Type> _experiments;

        // Strategy to obtain seeds for the GenerationManager's PRNG
        private Func<int, int> _genSeedStrategy;

        // Strategy to obtain seeds for the NavScanner's PRNG
        private Func<int, int> _navSeedStrategy;

        // The names of all found experiments
        [NonSerialized]
        private string[] _experimentNames;

        // The names of all generation parameter sets in the currently selected
        // experiment
        [NonSerialized]
        private string[] _genParamSets;

        // The names of all nav parameter sets in the currently selected
        // experiment
        [NonSerialized]
        private string[] _navParamSets;

        // ////////// //
        // Properties //
        // ////////// //

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

        private ICollection<string> GenParamSets
        {
            get
            {
                if (_genParamSets is null)
                {
                    OnChangeExperiment();
                }
                return _genParamSets;
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

        // /////// //
        // Methods //
        // /////// //

        private void OnChangeExperiment()
        {
            _experiment = _experiments[_experimentName]
                .GetConstructor(Type.EmptyTypes)
                .Invoke(null)
                as IExperiment;

            _genParamSets = _experiment.GenParamSet.Keys.ToArray();
            Array.Sort(_genParamSets);
            if (!_genParamSets.Contains(_genParamSet))
                _genParamSet = _genParamSets[0];

            _navParamSets = _experiment.NavParamSet.Keys.ToArray();
            Array.Sort(_navParamSets);
            if (!_navParamSets.Contains(_navParamSet))
                _navParamSet = _navParamSets[0];
        }

        [Button("Set gen params in GenerationManager", enabledMode: EButtonEnableMode.Editor)]
        private void SetGenParams()
        {
            GenerationManager gmInstance = FindObjectOfType<GenerationManager>();

            Type gmType = typeof(GenerationManager);

            Type smType = null;
            IDictionary<string, object> smConfig = null;

            _genSeedStrategy = null;

            foreach (KeyValuePair<string, object> settings in _experiment.GenParamSet[_genParamSet])
            {
                if (settings.Key.Equals("seedStrategy"))
                {
                    _genSeedStrategy = settings.Value as Func<int, int>;
                }
                else if (settings.Key.Equals("_selectionMethod"))
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

        [Button("Set nav params in NavController", enabledMode: EButtonEnableMode.Editor)]
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
        /// Start currently selected experiment.
        /// </summary>
        [Button("Start experiment", enabledMode: EButtonEnableMode.Editor)]
        private void StartExperiment()
        {
            const long saveIntervalMillis = 30000; // Save only after 30 seconds without saving

            long lastSaveTime = 0;

            Stopwatch stopwatch = Stopwatch.StartNew();

            StringBuilder expResultPendingSave = new StringBuilder("run,genset,navset,tg,tv,c,ar,genseed,navseed\n");

            string currentGenParamSet = _genParamSet;
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

            MethodInfo clearMeth = gmType.GetMethod(
                "ClearMap",
                BindingFlags.NonPublic | BindingFlags.Instance);

            FieldInfo gmSeed = gmType.GetField(
                "_seed",
                BindingFlags.NonPublic | BindingFlags.Instance);

            FieldInfo nsSeed = nsType.GetField(
                "_seed", BindingFlags.NonPublic | BindingFlags.Instance);

            string expResultsFolder = Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                "experiments");

            string expResultsFile = Path.Combine(
                expResultsFolder,
                $"{_experimentName}-{DateTime.Now.ToString("yyyyMMddHHmmss", DateTimeFormatInfo.InvariantInfo)}.csv");

            int step = 0;
            float totalSteps = _genParamSets.Length * _navParamSets.Length * _runsPerGenNavCombo;
            bool cancelled = false;

            Debug.Log($"==== Starting experiment '{_experimentName}' ====");

            Directory.CreateDirectory(expResultsFolder);

            foreach (string genParamSet in _genParamSets)
            {
                _genParamSet = genParamSet;
                SetGenParams();
                int genInitSeed = (int)gmSeed.GetValue(gmInstance);

                foreach (string navParamSet in _navParamSets)
                {
                    _navParamSet = navParamSet;
                    SetNavParams();
                    int navInitSeed = (int)nsSeed.GetValue(nsInstance);

                    for (int i = 0; i < _runsPerGenNavCombo; i++)
                    {
                        int genSeed, navSeed;

                        if (EditorUtility.DisplayCancelableProgressBar(
                            $"Performing experiment '{_experimentName}'",
                            $"Running scenario {step}/{totalSteps}...",
                            step / totalSteps))
                        {
                            cancelled = true;
                        }

                        if (_genSeedStrategy is null)
                        {
                            genSeed = (int)gmSeed.GetValue(gmInstance);
                        }
                        else
                        {
                            genSeed = _genSeedStrategy.Invoke(genInitSeed + i);
                            gmSeed.SetValue(gmInstance, genSeed);
                        }

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

                        expResultPendingSave.AppendFormat(
                            CultureInfo.InvariantCulture,
                            "{0},{1},{2},{3},{4},{5},{6},{7},{8}\n",
                            i,
                            $"\"{_genParamSet}\"",
                            $"\"{_navParamSet}\"",
                            gmInstance.GenTimeMillis,
                            nsInstance.ValidationTimeMillis,
                            nsInstance.MeanValidConnections,
                            nsInstance.RelAreaLargestCluster,
                            genSeed,
                            navSeed);

                        if (stopwatch.ElapsedMilliseconds > lastSaveTime + saveIntervalMillis)
                        {
                            File.AppendAllText(
                                expResultsFile,
                                expResultPendingSave.ToString());
                            expResultPendingSave.Clear();
                            lastSaveTime = stopwatch.ElapsedMilliseconds;
                        }

                        step++;

                        if (cancelled) break;
                    }
                    if (cancelled) break;
                }
                if (cancelled) break;
            }

            EditorUtility.ClearProgressBar();

            File.AppendAllText(expResultsFile, expResultPendingSave.ToString());

            JsonUtility.FromJsonOverwrite(savedNs, nsInstance);

            if (savedSmCfg != null)
            {
                JsonUtility.FromJsonOverwrite(savedSmCfg, gmSmConfig);
            }

            JsonUtility.FromJsonOverwrite(savedGm, gmInstance);

            _genParamSet = currentGenParamSet;
            _navParamSet = currentNavParamSet;

            clearMeth.Invoke(gmInstance, null);

            Debug.Log(string.Format(
                "==== Experiment '{0}' finished after {1} ms, results saved to {2} ====",
                _experimentName,
                stopwatch.ElapsedMilliseconds,
                expResultsFile));
        }
    }
}