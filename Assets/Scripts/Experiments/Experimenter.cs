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
        [Dropdown(nameof(ExperimentNames))]
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
        [OnValueChanged(nameof(OnChangeNumberOfScenarios))]
        private int _runsPerGenNavCombo = 1;

        // How many scenarios to skip before starting? This can be useful if a
        // previous run was cancelled at some point for some reason
        [SerializeField]
        [BoxGroup(experimentParams)]
        [OnValueChanged(nameof(OnChangeNumberOfScenarios))]
        private int _skipFirstNScenarios = 0;

        // Total number of scenarios, automatically updated
        [SerializeField]
        [ReadOnly]
        [BoxGroup(experimentParams)]
        private int _numberOfScenarios = 0;

        // Effective number of scenarios to run, automatically updated
        [SerializeField]
        [ReadOnly]
        [BoxGroup(experimentParams)]
        private int _scenariosToRun = 0;

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

        // Returns the experiment names, obtaining them (and the experiences
        // themselves) in case they were not yet determined
        private ICollection<string> ExperimentNames
        {
            get
            {
                // Were the experiment names not yet determined?
                if (_experimentNames is null)
                {
                    // We'll remove this part from the experiment name
                    const string toRm = "Experiment";

                    // Get a reference to the experiments type
                    Type expIType = typeof(IExperiment);

                    // Get classes which implement IExperiment and are not abstract
                    _experiments = AppDomain.CurrentDomain
                        .GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .Where(t => !t.IsAbstract && expIType.IsAssignableFrom(t))
                        .ToDictionary(
                            e => e.Name.EndsWith(toRm)
                                ? e.Name.Substring(0, e.Name.Length - toRm.Length)
                                : e.Name,
                            e => e);

                    // Experiment names are the experiment dictionary keys
                    _experimentNames = _experiments.Keys.ToArray();

                    // Sort experiment names
                    Array.Sort(_experimentNames);
                }

                // Return experiment names
                return _experimentNames;
            }
        }

        // Return the names of the generation parameter sets, obtaining them in
        // case they were not yet determined for the current experiment
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

        // Return the names of the navigation parameter sets, obtaining them in
        // case they were not yet determined for the current experiment
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

        // Invoked when another experiment is selected in the editor or when the
        // editor reloads
        // Obtains the concrete experiment and determines the names of the
        // generation and navigation parameter sets, also updating the number of
        // scenarios
        private void OnChangeExperiment()
        {
            // Obtain and instantiate the concrete experiment according to the
            // selected experiment name
            _experiment = _experiments[_experimentName]
                .GetConstructor(Type.EmptyTypes)
                .Invoke(null)
                as IExperiment;

            // Update names of generation parameter sets
            _genParamSets = _experiment.GenParamSet.Keys.ToArray();
            Array.Sort(_genParamSets);
            if (!_genParamSets.Contains(_genParamSet))
                _genParamSet = _genParamSets[0];

            // Update names of navigation parameter sets
            _navParamSets = _experiment.NavParamSet.Keys.ToArray();
            Array.Sort(_navParamSets);
            if (!_navParamSets.Contains(_navParamSet))
                _navParamSet = _navParamSets[0];

            // Update number of scenarios
            OnChangeNumberOfScenarios();
        }

        // Called when the number of scenarios (total or to run) changes
        private void OnChangeNumberOfScenarios()
        {
            // Update editor read-only var showing the total number of scenarios
            _numberOfScenarios =
                _genParamSets.Length * _navParamSets.Length * _runsPerGenNavCombo;

            // Update editor read-only var showing the number of scenarios to run
            _scenariosToRun = _numberOfScenarios - _skipFirstNScenarios;
            if (_scenariosToRun < 0) _scenariosToRun = 0;
        }

        // Sets the currently selected generation parameters in the GenerationManager
        [Button("Set gen params in GenerationManager", enabledMode: EButtonEnableMode.Editor)]
        private void SetGenParams()
        {
            // Get reference to the GenerationManager
            GenerationManager gmInstance = FindObjectOfType<GenerationManager>();

            // Get GenerationManager's class type
            Type gmType = typeof(GenerationManager);

            // Class type of the selection method configuration
            Type smType = null;

            // Generation parameters specific for the selection method
            IDictionary<string, object> smConfig = null;

            // Assume no seed strategy for the generation manager's PRNG
            _genSeedStrategy = null;

            // Loop through all generation parameter sets in the current experiment
            foreach (KeyValuePair<string, object> settings in _experiment.GenParamSet[_genParamSet])
            {
                // Check what's the current parameter
                if (settings.Key.Equals("seedStrategy"))
                {
                    // If it's a seed strategy (not exactly a parameter), keep it
                    _genSeedStrategy = settings.Value as Func<int, int>;
                }
                else if (settings.Key.Equals("_selectionMethod"))
                {
                    // If it's the selection method type, keep it, we'll use it later
                    smType = settings.Value as Type;
                }
                else if (settings.Key.Equals("_selectionParams"))
                {
                    // If it's the selection method params, keep them, we'll use them later
                    smConfig = settings.Value as IDictionary<string, object>;
                }
                else
                {
                    // If we get here, it's a regular parameter, set it in the
                    // GenerationManager using reflection

                    FieldInfo gmField = gmType.GetField(
                        settings.Key,
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    if (gmField is null)
                    {
                        Debug.LogWarning(
                            $"Unknown {nameof(GenerationManager)} field: '{settings.Key}'");
                    }
                    else
                    {
                        gmField.SetValue(gmInstance, settings.Value);
                    }
                }
            }

            // If a selection method was specified, configure it
            if (smType != null)
            {
                // Get the existing instance of the current selection method type
                AbstractSMConfig smCfgInstance = AbstractSMConfig.GetInstance(smType);

                // Get reference to the GenerationManager's field specifying the
                // selection method
                FieldInfo gmFieldSmName = gmType.GetField(
                    "_selectionMethod",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                // Get reference to the GenerationManager's field specifying the
                // selection method parameters
                FieldInfo gmFieldSmCfg = gmType.GetField(
                    "_selectionParams",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                // Update the generation manager with the experiment-specified
                // selection method name
                gmFieldSmName.SetValue(
                    gmInstance,
                    SMManager.Instance.GetNameFromType(smType));

                // Update the generation manager with the experiment-specified
                // selection method configuration
                gmFieldSmCfg.SetValue(gmInstance, smCfgInstance);

                // Are any selection method-specific parameters specified in the
                // experiment?
                if (smConfig != null)
                {
                    // Update the selection method configuration with the
                    // experiment-specified selection method parameters
                    foreach (KeyValuePair<string, object> smSettings in smConfig)
                    {
                        FieldInfo smField = smType.GetField(
                            smSettings.Key,
                            BindingFlags.NonPublic | BindingFlags.Instance);

                        if (smField is null)
                        {
                            Debug.LogWarning(
                                $"Unknown {smCfgInstance.GetType().Name} field: '{smSettings.Key}'");
                        }
                        else
                        {
                            smField.SetValue(smCfgInstance, smSettings.Value);
                        }
                    }
                }
            }
        }

        // Sets the currently selected navigation parameters in the NavController
        [Button("Set nav params in NavController", enabledMode: EButtonEnableMode.Editor)]
        private void SetNavParams()
        {
            // Get reference to the NavScanner
            NavScanner nsInstance = FindObjectOfType<NavScanner>();

            // Get NavScanner's class type
            Type nsType = typeof(NavScanner);

            // Assume no seed strategy for the nav scanner's PRNG
            _navSeedStrategy = null;

            // Loop through all nav parameter sets in the current experiment
            foreach (KeyValuePair<string, object> settings in _experiment.NavParamSet[_navParamSet])
            {
                // Check what's the current parameter
                if (settings.Key.Equals("seedStrategy"))
                {
                    // If it's a seed strategy (not exactly a parameter), keep it
                    _navSeedStrategy = settings.Value as Func<int, int>;
                }
                else
                {
                    // If we get here, it's a regular parameter, set it in the
                    // NavScanner using reflection
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

        // Run the currently selected experiment
        [Button("Start experiment", enabledMode: EButtonEnableMode.Editor)]
        private void StartExperiment()
        {
            // Require at least 30 seconds before re-saving results
            const long saveIntervalMillis = 30000;

            // Time at which the results file was saved by the last time
            long lastSaveTime = 0;

            // Start a stopwatch to measure the experiment time (all scenarios)
            Stopwatch stopwatch = Stopwatch.StartNew();

            // This StringBuilder will contain the temporary results, before
            // being flushed to a file
            StringBuilder expResultPendingSave =
                new StringBuilder("run,genset,navset,tg,tv,c,ar,nclu,genseed,navseed\n");

            // Currently selected parameter sets, to be restored when the
            // experiment finishes
            string currentGenParamSet = _genParamSet;
            string currentNavParamSet = _navParamSet;

            // Obtain the existing instances of the generation manager and nav scanner
            GenerationManager gmInstance = FindObjectOfType<GenerationManager>();
            NavScanner nsInstance = FindObjectOfType<NavScanner>();

            // Determine class types of the generation manager and nav scanner
            Type gmType = typeof(GenerationManager);
            Type nsType = typeof(NavScanner);

            // Save the generation manager's current configuration, to be restored
            // after the experiment finishes
            string savedGm = JsonUtility.ToJson(gmInstance);

            // Get reference to the selection method configuration field in the
            // generation manager
            FieldInfo gmFieldSmCfg = gmType.GetField(
                "_selectionParams",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Save the current selection method configuration, to be restored
            // after the experiment finishes
            object gmSmConfig = gmFieldSmCfg.GetValue(gmInstance);
            string savedSmCfg = JsonUtility.ToJson(gmSmConfig);

            // Save the nav scanner's current configuration, to be restored
            // after the experiment finishes
            string savedNs = JsonUtility.ToJson(nsInstance);

            // Get references to the map generation and map clear methods in the
            // generation manager
            MethodInfo genMeth = gmType.GetMethod(
                "GenerateMap",
                BindingFlags.NonPublic | BindingFlags.Instance);

            MethodInfo clearMeth = gmType.GetMethod(
                "ClearMap",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Get references to the seed fields in the generation manager and
            // nav scanner
            FieldInfo gmSeed = gmType.GetField(
                "_seed",
                BindingFlags.NonPublic | BindingFlags.Instance);

            FieldInfo nsSeed = nsType.GetField(
                "_seed", BindingFlags.NonPublic | BindingFlags.Instance);

            // Determine folder where to place results, at the project's root
            string expResultsFolder = Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                "experiments");

            // Determine full path of file containing experiment results
            string expResultsFile = Path.Combine(
                expResultsFolder,
                $"{_experimentName}-{DateTime.Now.ToString("yyyyMMddHHmmss", DateTimeFormatInfo.InvariantInfo)}.csv");

            // Current step, total steps and experiment cancelled status
            int step = 0;
            float totalSteps = _genParamSets.Length * _navParamSets.Length * _runsPerGenNavCombo;
            bool cancelled = false;

            // Create the experiment results folder, if it's not already created
            Directory.CreateDirectory(expResultsFolder);

            // Let's start the experiment
            Debug.Log($"==== Starting experiment '{_experimentName}' ====");

            // Loop through all generation parameter sets
            foreach (string genParamSet in _genParamSets)
            {
                // Set the current generation parameters and get the respective seed
                _genParamSet = genParamSet;
                SetGenParams();
                int genInitSeed = (int)gmSeed.GetValue(gmInstance);

                // Loop through all navigation parameter set
                foreach (string navParamSet in _navParamSets)
                {
                    // Set the current navigation parameters and get the respective seed
                    _navParamSet = navParamSet;
                    SetNavParams();
                    int navInitSeed = (int)nsSeed.GetValue(nsInstance);

                    // Perform the specified number of runs per scenario
                    for (int i = 0; i < _runsPerGenNavCombo; i++)
                    {
                        // Gen and nav seeds, to save in results
                        int genSeed, navSeed;

                        // Increment step
                        step++;

                        // Notify user of current experiment progress using a
                        // progress bar
                        if (EditorUtility.DisplayCancelableProgressBar(
                            $"Performing experiment '{_experimentName}'",
                            $"Running scenario {step}/{(int)totalSteps}...",
                            step / totalSteps))
                        {
                            // If user cancelled the experiment, bail out
                            cancelled = true;
                            break;
                        }

                        // Should the current scenario/run be skipped?
                        if (_skipFirstNScenarios >= step)
                        {
                            continue;
                        }

                        if (_genSeedStrategy is null)
                        {
                            // If a generation seed strategy wasn't specified,
                            // get seed from the generation manager and keep it
                            genSeed = (int)gmSeed.GetValue(gmInstance);
                        }
                        else
                        {
                            // Otherwise use strategy to obtain a seed and set
                            // it in the generation manager
                            genSeed = _genSeedStrategy.Invoke(genInitSeed + i);
                            gmSeed.SetValue(gmInstance, genSeed);
                        }

                        if (_navSeedStrategy is null)
                        {
                            // If a navigation seed strategy wasn't specified,
                            // get seed from the nav scanner and keep it
                            navSeed = (int)nsSeed.GetValue(nsInstance);
                        }
                        else
                        {
                            // Otherwise use strategy to obtain a seed and set
                            // it in the nav scanner
                            navSeed = _navSeedStrategy.Invoke(navInitSeed + i);
                            nsSeed.SetValue(nsInstance, navSeed);
                        }

                        // Generate map
                        genMeth.Invoke(gmInstance, null);

                        // Take note of results for current scenario/run
                        expResultPendingSave.AppendFormat(
                            CultureInfo.InvariantCulture,
                            "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}\n",
                            i,
                            $"\"{_genParamSet}\"",
                            $"\"{_navParamSet}\"",
                            gmInstance.GenTimeMillis,
                            nsInstance.ValidationTimeMillis,
                            nsInstance.MeanValidConnections,
                            nsInstance.RelAreaLargestCluster,
                            nsInstance.Clusters.Count,
                            genSeed,
                            navSeed);

                        // Is it time to save unsaved results to the results file?
                        if (stopwatch.ElapsedMilliseconds > lastSaveTime + saveIntervalMillis)
                        {
                            // Append unsaved results to the results file
                            File.AppendAllText(
                                expResultsFile,
                                expResultPendingSave.ToString());

                            // Clear string builder of unsaved results
                            expResultPendingSave.Clear();

                            // Take note of time results were saved to file
                            lastSaveTime = stopwatch.ElapsedMilliseconds;
                        }

                    }
                    // Bail out if experiment was cancelled by user
                    if (cancelled) break;
                }
                // Bail out if experiment was cancelled by user
                if (cancelled) break;
            }

            // Clear progress bar
            EditorUtility.ClearProgressBar();

            // Save unsaved results
            File.AppendAllText(expResultsFile, expResultPendingSave.ToString());

            // Restore scene state to what it was before the experiment
            JsonUtility.FromJsonOverwrite(savedNs, nsInstance);

            if (savedSmCfg != null)
            {
                JsonUtility.FromJsonOverwrite(savedSmCfg, gmSmConfig);
            }

            JsonUtility.FromJsonOverwrite(savedGm, gmInstance);

            _genParamSet = currentGenParamSet;
            _navParamSet = currentNavParamSet;

            // Since we can't restore the previously existing map, we clear the
            // last map generated in the experiment
            clearMeth.Invoke(gmInstance, null);

            // Log experiment duration
            Debug.Log(string.Format(
                "==== Experiment '{0}' finished after {1} ms, results saved to {2} ====",
                _experimentName,
                stopwatch.ElapsedMilliseconds,
                expResultsFile));
        }
    }
}