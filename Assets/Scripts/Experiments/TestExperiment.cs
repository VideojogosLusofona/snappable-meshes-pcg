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
                        ["_maxPieces"] = (uint)25
                    },
                },

            };
        public IDictionary<string, IDictionary<string, object>> NavParamSet => null;

        public int RunsPerCombo => 1;
    }
}