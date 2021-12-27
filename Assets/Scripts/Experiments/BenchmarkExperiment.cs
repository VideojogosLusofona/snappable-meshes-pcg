using System;
using System.Collections.Generic;
using UnityEngine;
using SnapMeshPCG.Navigation;
using SnapMeshPCG.SelectionMethods;

namespace SnapMeshPCG.Experiments
{
    public class BenchmarkExperiment : IExperiment
    {
        private readonly Func<int, int> _seeder = i => int.Parse(
            Hash128.Compute(i).ToString().Substring(0, 8),
            System.Globalization.NumberStyles.HexNumber);

        public IDictionary<string, IDictionary<string, object>> GenParamSet =>
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

        public IDictionary<string, IDictionary<string, object>> NavParamSet =>
            new Dictionary<string, IDictionary<string, object>>()
            {
                ["50"] = new Dictionary<string, object>()
                {
                    ["_navPointCount"] = 50,
                    ["_reinitializeRNG"] = NavScanner.ReInitRNG.Seed,
                    ["seedStrategy"] = _seeder
                },
                ["500"] = new Dictionary<string, object>()
                {
                    ["_navPointCount"] = 500,
                    ["_reinitializeRNG"] = NavScanner.ReInitRNG.Seed,
                    ["seedStrategy"] = _seeder
                },
                // ["5000"] = new Dictionary<string, object>()
                // {
                //     ["_navPointCount"] = 5000,
                //     ["_reinitializeRNG"] = NavScanner.ReInitRNG.Seed,
                //     ["seedStrategy"] = _seeder
                // }
            };
    }
}