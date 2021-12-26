using System.Collections.Generic;
using SnapMeshPCG.SelectionMethods;

namespace SnapMeshPCG.Experiments
{
    public class BenchmarkExperiment : IExperiment
    {
        public IDictionary<string, IDictionary<string, object>> Runs =>
            new Dictionary<string, IDictionary<string, object>>()
            {
                ["(a)"] = new Dictionary<string, object>()
                {
                    ["_useSeed"] = true,
                    ["_seed"] = -267402550,
                    ["_pieceDistance"] = 0.0001f,
                    ["_maxFailures"] = (uint)10,
                    ["_checkOverlaps"] = true,
                    ["_matchingRules"] = SnapRules.Colours | SnapRules.Pins,
                    ["_pinCountTolerance"] = (uint)0,
                    ["_starterConTol"] = (uint)0,
                    ["_selectionMethod"] = typeof(ArenaSMConfig),
                    ["_selectionParams"] = new Dictionary<string, object>()
                    {
                        ["_maxPieces"] = (uint)12
                    },
                },
                ["(b)"] = null,
                ["(c)"] = null,
                ["(d)"] = null,
                ["(e)"] = null,
                ["(f)"] = null,
                ["(g)"] = null,
                ["(h)"] = null,
            };

    }
}