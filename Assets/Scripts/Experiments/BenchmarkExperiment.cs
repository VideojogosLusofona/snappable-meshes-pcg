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
                ["(b)"] =  new Dictionary<string, object>()
                {
                    ["_useSeed"] = true,
                    ["_seed"] = -2095385667,
                    ["_pieceDistance"] = 0.0001f,
                    ["_maxFailures"] = (uint)10,
                    ["_checkOverlaps"] = true,
                    ["_matchingRules"] = SnapRules.Colours | SnapRules.Pins,
                    ["_pinCountTolerance"] = (uint)0,
                    ["_starterConTol"] = (uint)0,
                    ["_selectionMethod"] = typeof(CorridorSMConfig),
                    ["_selectionParams"] = new Dictionary<string, object>()
                    {
                        ["_maxPieces"] = (uint)20
                    },
                },
                ["(c)"] =  new Dictionary<string, object>()
                {
                    ["_useSeed"] = true,
                    ["_seed"] = 277759099,
                    ["_pieceDistance"] = 0.0001f,
                    ["_maxFailures"] = (uint)10,
                    ["_checkOverlaps"] = true,
                    ["_matchingRules"] = SnapRules.Colours | SnapRules.Pins,
                    ["_pinCountTolerance"] = (uint)0,
                    ["_starterConTol"] = (uint)0,
                    ["_selectionMethod"] = typeof(StarSMConfig),
                    ["_selectionParams"] = new Dictionary<string, object>()
                    {
                        ["_armLength"] = (uint)8,
                        ["_armLengthVar"] = (uint)2
                    },
                },
                ["(d)"] =  new Dictionary<string, object>()
                {
                    ["_useSeed"] = true,
                    ["_seed"] = 1388449552,
                    ["_pieceDistance"] = 0.0001f,
                    ["_maxFailures"] = (uint)10,
                    ["_checkOverlaps"] = true,
                    ["_matchingRules"] = SnapRules.Colours | SnapRules.Pins,
                    ["_pinCountTolerance"] = (uint)0,
                    ["_starterConTol"] = (uint)0,
                    ["_selectionMethod"] = typeof(BranchSMConfig),
                    ["_selectionParams"] = new Dictionary<string, object>()
                    {
                        ["_branchCount"] = (uint)4,
                        ["_branchLength"] = (uint)12,
                        ["_branchLengthVar"] = (uint)4
                    },
                },
                ["(e)"] =  new Dictionary<string, object>()
                {
                    ["_useSeed"] = true,
                    ["_seed"] = 811974397,
                    ["_pieceDistance"] = 6f,
                    ["_maxFailures"] = (uint)10,
                    ["_checkOverlaps"] = true,
                    ["_matchingRules"] = SnapRules.Colours,
                    ["_pinCountTolerance"] = (uint)0,
                    ["_starterConTol"] = (uint)0,
                    ["_selectionMethod"] = typeof(ArenaSMConfig),
                    ["_selectionParams"] = new Dictionary<string, object>()
                    {
                        ["_maxPieces"] = (uint)20
                    },
                },
                ["(f)"] =  new Dictionary<string, object>()
                {
                    ["_useSeed"] = true,
                    ["_seed"] = -359152709,
                    ["_pieceDistance"] = 0.0001f,
                    ["_maxFailures"] = (uint)10,
                    ["_checkOverlaps"] = true,
                    ["_matchingRules"] = SnapRules.Pins,
                    ["_pinCountTolerance"] = (uint)0,
                    ["_starterConTol"] = (uint)0,
                    ["_selectionMethod"] = typeof(ArenaSMConfig),
                    ["_selectionParams"] = new Dictionary<string, object>()
                    {
                        ["_maxPieces"] = (uint)20
                    },
                },
                ["(g)"] =  new Dictionary<string, object>()
                {
                    ["_useSeed"] = true,
                    ["_seed"] = 1242840355,
                    ["_pieceDistance"] = 0.0001f,
                    ["_maxFailures"] = (uint)10,
                    ["_checkOverlaps"] = false,
                    ["_matchingRules"] = SnapRules.Colours | SnapRules.Pins,
                    ["_pinCountTolerance"] = (uint)0,
                    ["_starterConTol"] = (uint)0,
                    ["_selectionMethod"] = typeof(StarSMConfig),
                    ["_selectionParams"] = new Dictionary<string, object>()
                    {
                        ["_armLength"] = (uint)10,
                        ["_armLengthVar"] = (uint)4
                    },
                },
                ["(h)"] =  new Dictionary<string, object>()
                {
                    ["_useSeed"] = true,
                    ["_seed"] = -1444708658,
                    ["_pieceDistance"] = 0.0001f,
                    ["_maxFailures"] = (uint)10,
                    ["_checkOverlaps"] = true,
                    ["_matchingRules"] = SnapRules.Pins,
                    ["_pinCountTolerance"] = (uint)0,
                    ["_starterConTol"] = (uint)0,
                    ["_selectionMethod"] = typeof(CorridorSMConfig),
                    ["_selectionParams"] = new Dictionary<string, object>()
                    {
                        ["_maxPieces"] = (uint)120
                    },
                },
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