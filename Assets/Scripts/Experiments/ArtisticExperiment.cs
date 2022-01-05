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
    /// <summary>
    /// Definition of the artistic experiment shown in the paper
    /// "Procedural Generation of 3D Maps with Snappable Meshes".
    /// </summary>
    public class ArtisticExperiment : IExperiment
    {

        /// <summary>
        /// Generation parameter sets for the artistic experiments.
        /// </summary>
        public IDictionary<string, IDictionary<string, object>> GenParamSet =>
            new Dictionary<string, IDictionary<string, object>>()
            {
                ["Arena"] = new Dictionary<string, object>()
                {
                    ["_useSeed"] = true,
                    ["_seed"] = -552867222,
                    ["_pieceDistance"] = 0.0001f,
                    ["_maxFailures"] = (uint)10,
                    ["_checkOverlaps"] = true,
                    ["_matchingRules"] = SnapRules.Colours | SnapRules.Pins,
                    ["_pinCountTolerance"] = (uint)0,
                    ["_starterConTol"] = (uint)0,
                    ["_selectionMethod"] = typeof(ArenaSMConfig),
                    ["_selectionParams"] = new Dictionary<string, object>()
                    {
                        ["_maxPieces"] = (uint)18
                    },
                },
                ["Branch"] = new Dictionary<string, object>()
                {
                    ["_useSeed"] = true,
                    ["_seed"] = 867031608,
                    ["_pieceDistance"] = 0.0001f,
                    ["_maxFailures"] = (uint)10,
                    ["_checkOverlaps"] = true,
                    ["_matchingRules"] = SnapRules.Colours | SnapRules.Pins,
                    ["_pinCountTolerance"] = (uint)0,
                    ["_starterConTol"] = (uint)0,
                    ["_selectionMethod"] = typeof(BranchSMConfig),
                    ["_selectionParams"] = new Dictionary<string, object>()
                    {
                        ["_branchCount"] = (uint)3,
                        ["_branchLength"] = (uint)9,
                        ["_branchLengthVar"] = (uint)2
                    },
                },

                ["Corridor"] = new Dictionary<string, object>()
                {
                    ["_useSeed"] = true,
                    ["_seed"] = -1189877736,
                    ["_pieceDistance"] = 0.0001f,
                    ["_maxFailures"] = (uint)10,
                    ["_checkOverlaps"] = true,
                    ["_matchingRules"] = SnapRules.Colours | SnapRules.Pins,
                    ["_pinCountTolerance"] = (uint)0,
                    ["_starterConTol"] = (uint)0,
                    ["_selectionMethod"] = typeof(CorridorSMConfig),
                    ["_selectionParams"] = new Dictionary<string, object>()
                    {
                        ["_maxPieces"] = (uint)12
                    },
                },

                ["Star"] = new Dictionary<string, object>()
                {
                    ["_useSeed"] = true,
                    ["_seed"] = 209636004,
                    ["_pieceDistance"] = 0.0001f,
                    ["_maxFailures"] = (uint)10,
                    ["_checkOverlaps"] = true,
                    ["_matchingRules"] = SnapRules.Colours | SnapRules.Pins,
                    ["_pinCountTolerance"] = (uint)0,
                    ["_starterConTol"] = (uint)0,
                    ["_selectionMethod"] = typeof(StarSMConfig),
                    ["_selectionParams"] = new Dictionary<string, object>()
                    {
                        ["_armLength"] = (uint)5,
                        ["_armLengthVar"] = (uint)2
                    },
                },

            };

        /// <summary>
        /// Specify a nav scan with 100 points using the same RNG used by the
        /// selection method.
        /// </summary>
        public IDictionary<string, IDictionary<string, object>> NavParamSet =>
            new Dictionary<string, IDictionary<string, object>>()
            {
                ["100"] = new Dictionary<string, object>()
                {
                    ["_navPointCount"] = 100,
                    ["_reinitializeRNG"] = NavScanner.ReInitRNG.No,
                },
            };
    }
}