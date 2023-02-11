﻿/*
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
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace SnapMeshPCG
{
    /// <summary>
    /// Represents the behavior of a map piece.
    /// </summary>
    public class MapPiece : MonoBehaviour, IComparable<MapPiece>
    {
        // Never access this variable directly
        // Do so via the _Connectors property
        private ICollection<Connector> _connectors;

        // Connectors in this piece
        private ICollection<Connector> _Connectors
        {
            get
            {
                // Make sure connector collection is initialized
                if (_connectors is null) InitConnectors();

                // Return collector collection
                return _connectors;
            }
        }

        /// <summary>
        /// Number of connectors in this piece.
        /// </summary>
        public int ConnectorCount => _Connectors.Count;

        /// <summary>
        /// Number of free connectors in this piece.
        /// </summary>
        public int FreeConnectorCount
        {
            get
            {
                int n = 0;
                foreach (Connector c in _Connectors)
                    if (!c.IsUsed) n++;
                return n;
            }
        }

        /// <summary>
        /// Are all the connectors in this piece used?
        /// </summary>
        public bool Full
        {
            get
            {
                foreach (Connector c in _Connectors)
                    if (!c.IsUsed) return false;
                return true;
            }
        }

        /// <summary>
        /// Create a copy of this piece.
        /// </summary>
        /// <returns>A copy of this piece.</returns>
        public GameObject ClonePiece()
        {
            MapPiece clonedPiece = Instantiate(this);
            clonedPiece.Setup();
            return clonedPiece.gameObject;
        }

#if DEBUG_OVERLAPS
        private static int interError = 0;
#endif

        /// <summary>
        /// Try and snap a piece with the current piece.
        /// </summary>
        /// <param name="rules">Current matching rules.</param>
        /// <param name="other">
        /// The piece to check for possible connection with.
        /// </param>
        /// <param name="checkOverlaps">Check for overlaps?</param>
        /// <param name="collidersLayer">
        /// Overlap layer to use if <paramref name="checkOverlaps"/> is
        /// true.
        /// </param>
        /// <param name="pieceDistance">
        /// Distance between two connected connectors.
        /// </param>
        /// <param name="pinTolerance">
        /// Maximum allowed difference between pin counts in two connectors to
        /// allow them to be paired up.
        /// </param>
        /// <param name="colorMatrix">
        /// Valid color combinations for matching connectors.
        /// </param>
        /// <param name="allPieces">
        /// All pieces already generated, to be able to check overlaps.
        /// </param>
        /// <param name="stepId">
        /// A step ID, used for debugging purposes
        /// </param>
        /// <returns>True if snap was successful, false otherwise.</returns>
        public bool TrySnapWith(
            SnapRules rules, MapPiece other,
            bool checkOverlaps, LayerMask collidersLayer,
            float pieceDistance = 0.00f, uint pinTolerance = 0,
            bool[,] colorMatrix = null,
            List<MapPiece> allPieces = null,
            int stepId = -1)
        {
            // List of valid connector combinations
            List<(Connector curr, Connector other)> validCombos =
                new List<(Connector curr, Connector other)>();

            // Create a list of valid connections between this piece and the
            // other piece
            foreach (Connector co in other._Connectors)
            {
                foreach (Connector cc in _Connectors)
                {
                    // Only check for matching if both connectors are unused
                    if (!co.IsUsed && !cc.IsUsed)
                    {
                        // By default we assume there's a pin match and a color
                        // match
                        bool pinMatch = true;
                        bool colorMatch = true;

                        // If we're using pins count as criteria, check pins
                        if (rules.UsePins())
                        {
                            // Pin match is verified if the pin difference
                            // between connectors is below the pin tolerance
                            pinMatch = Mathf.Abs(co.Pins - cc.Pins) <= pinTolerance;
                        }

                        // If we're using colour as criteria, check colour matrix
                        if (rules.UseColours())
                        {
                            // Get the validity of the connectors color
                            // combination from the color matrix
                            colorMatch =
                                colorMatrix?[(int)cc.ConnColor, (int)co.ConnColor]
                                ?? true;
                        }

                        // If we have a match, then this is a valid connection
                        if (pinMatch && colorMatch) validCombos.Add((cc, co));
                    }
                }
            }

            // If there are valid connections, try to enable one of them
            // This might fail if we're checking for overlaps
            while (validCombos.Count > 0)
            {
                // Get a random connection from the valid connections list
                int chosenIndex = UnityEngine.Random.Range(0, validCombos.Count);

                // Get connectors from the randomly selected connection
                (Connector curr, Connector other) chosenCombo =
                    validCombos[chosenIndex];

                // Remove this combo from the pool (so it can't be selected again:
                // if it has failed before, it will fail again - we risk an 
                // infinite loop otherwise
                validCombos.Remove(chosenCombo);

                // Get current PRS components from the other piece, since it
                // may be necessary to undo the transformation if we're checking
                // for overlaps
                Vector3 prevPos = other.transform.position;
                Quaternion prevRot = other.transform.rotation;
                Vector3 prevScale = other.transform.localScale;

                // Set the correct position and rotation of the other piece so
                // its connector matches this piece's connector
                SetOtherPiecePosRot(
                    chosenCombo.curr, chosenCombo.other, other, pieceDistance);

                // Apply Transform changes to the physics engine
                Physics.SyncTransforms();

                // If checking for overlaps, verify if there are any with the
                // existing geometry, using this transform
                if (checkOverlaps)
                {
                    // Assume there are no overlaps
                    bool                overlaps = false;

                    // Get colliders (voxel and box colliders) of new piece
                    VoxelCollider       voxelCollider = GetVoxelCollider(other, collidersLayer);
                    List<BoxCollider>   boxColliders = GetBoxColliders(other, collidersLayer);
                    // Create OBBs from BoxColliders
                    List<OBB>           OBBs = new List<OBB>();
                    foreach (var box in boxColliders)
                    {
                        OBB obb = new OBB(box.center, box.size);
                        obb.Transform(other.transform.localToWorldMatrix);
                        OBBs.Add(obb);
                    }

                    // For all pieces
                    foreach (var piece in allPieces)
                    {
                        if (voxelCollider)
                        {
                            // If the current piece has a voxel collider
                            // Get a voxel collider on the other pieces
                            VoxelCollider otherVoxelCollider = GetVoxelCollider(piece, collidersLayer);
                            if (otherVoxelCollider)
                            {
                                if (voxelCollider.Intersect(otherVoxelCollider, 0.95f))
                                {
#if DEBUG_OVERLAPS
                                        Debug.Log($"Can't connect {name} with {other.name}");
                                        Debug.Log($"Connector {chosenCombo.chosenMine.name} / {chosenCombo.chosenOther.name}");
                                        Debug.Log($"Overlap detected with {piece.name}");

                                        // Create a copy for later debug
                                        MapPiece newObject = Instantiate(other);
                                        newObject.name = $"OverlapError {interError++}";
                                        newObject.transform.position += Vector3.up * 20;
                                        newObject.transform.SetParent(null);
                                        newObject.gameObject.SetActive(false);
                                        Debug.Log($"New object = {newObject.name}");
#endif

                                    overlaps = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // If the current piece only has box colliders
                            // Fetch all box colliders in the other pieces
                            List<BoxCollider>    existingBoxColliders = GetBoxColliders(piece, collidersLayer);

                            // Check all the colliders against the other colliders
                            foreach (var obb in OBBs)
                            {
                                DebugGizmo.AddWireOBB($"Type=OBBTest;Subtype=Tentative;step={stepId}", obb, Color.cyan);
                                foreach (var otherBoxCollider in existingBoxColliders)
                                {
                                    OBB otherObb = new OBB(otherBoxCollider.center, otherBoxCollider.size);
                                    otherObb.Transform(otherBoxCollider.transform.localToWorldMatrix);

                                    DebugGizmo.AddWireOBB($"Type=OBBTest;Subtype=Existing;step={stepId}", otherObb, Color.magenta);

                                    if (obb.Intersect(otherObb))
                                    {
#if DEBUG_OVERLAPS
                                        Debug.Log($"Can't connect {name} with {other.name}");
                                        Debug.Log($"Connector {chosenCombo.chosenMine.name} / {chosenCombo.chosenOther.name}");
                                        Debug.Log($"Overlap detected with {piece.name}");

                                        // Create a copy for later debug
                                        MapPiece newObject = Instantiate(other);
                                        newObject.name = $"OverlapError {interError++}";
                                        newObject.transform.position += Vector3.up * 20;
                                        newObject.transform.SetParent(null);
                                        newObject.gameObject.SetActive(false);
                                        Debug.Log($"New object = {newObject.name}");
#endif

                                        overlaps = true;
                                        break;
                                    }
                                }

                                if (overlaps) break;
                            }
                        }
                        if (overlaps) break;
                    }

                    // Get box colliders in layer and loop through them
/*                    foreach (BoxCollider boxCollider in GetBoxColliders(other, collidersLayer))
                    {
                        // Get the center and extents of the current box collider
                        Vector3 center = boxCollider.transform.rotation * boxCollider.center + boxCollider.transform.position;
                        Vector3 extents = new Vector3(0.5f * boxCollider.size.x * boxCollider.transform.lossyScale.x,
                                                      0.5f * boxCollider.size.y * boxCollider.transform.lossyScale.y,
                                                      0.5f * boxCollider.size.z * boxCollider.transform.lossyScale.z);

                        // Get colliders that overlap with the current box
                        Collider[] hits = Physics.OverlapBox(
                            center, extents, boxCollider.transform.rotation, collidersLayer);

                        // Loop through the colliders that overlap with the
                        // current box
                        foreach (Collider hit in hits)
                        {
                            // Check for self overlap, or overlaps with the connected piece
                            // Ignore the collision in those cases
                            MapPiece parentMapPiece = hit.GetComponentInParent<MapPiece>();
                            if ((parentMapPiece == other) || (parentMapPiece == this))
                            {
                                continue;
                            }

#if DEBUG_OVERLAPS
                            Debug.Log($"Can't connect {name} with {other.name}");
                            Debug.Log($"Connector {chosenCombo.chosenMine.name} / {chosenCombo.chosenOther.name}");
                            Debug.Log($"Overlap detected with {parentMapPiece.name}");

                            // Create a copy for later debug
                            MapPiece newObject = Instantiate(other);
                            newObject.name = $"OverlapError {interError++}";
                            newObject.transform.position += Vector3.up * 20;
                            newObject.transform.SetParent(null);
                            newObject.gameObject.SetActive(false);
                            Debug.Log($"New object = {newObject.name}");
#endif

                            // It auto-overlaps, so remove this possibility and retry
                            validCombos.Remove(chosenCombo);

                            overlaps = true;
                            break; // hit found, not need to check more hits

                        } // for each hit/collision

                        if (overlaps) break; // hit found, not need to check more box colliders

                    } // for each box collider*/

                    if (overlaps)
                    {
                        // Undo transform, so we can do it again...
                        other.transform.position = prevPos;
                        other.transform.rotation = prevRot;
                        other.transform.localScale = prevScale;
                        continue; // to next valid combo, if any
                    }

                } // if (checkOverlaps)

                // If we get here, then the connector match is definitively
                // validated, and we can carry on with it
                chosenCombo.curr.SnapWith(chosenCombo.other);

                // Return true, indicating that a match was found and piece
                // snapping was successful
                return true;

            } // while (validCombos.Count > 0)

            // Return false, indicating the piece snapping was not successful
            return false;
        }

        /// <summary>
        /// Set the correct position and rotation of the other piece so
        /// its connector matches this piece's connector.
        /// </summary>
        /// <param name="currConn">This piece's connector.</param>
        /// <param name="otherConn">The other piece's connector.</param>
        /// <param name="otherPiece">The other piece.</param>
        /// <param name="pieceDistance">
        /// Distance between two connected connectors.
        /// </param>
        private void SetOtherPiecePosRot(Connector currConn,
            Connector otherConn, MapPiece otherPiece, float pieceDistance)
        {
            // Get the transform of the other piece's connector
            Transform otherConnTrnsf = otherConn.transform;

            // Temporarily revert parenting so we can move the connector
            // and have the geometry follow
            otherConn.transform.SetParent(null, true);
            otherPiece.transform.SetParent(otherConn.transform, true);

            // Put the other piece on the current piece's connector
            otherConnTrnsf.position = currConn.transform.position;

            // Have the other connector look towards the current piece's connector
            otherConnTrnsf.rotation =
                Quaternion.LookRotation(-currConn.Heading, transform.up);

            // Move the pieces away from each other based on the specified
            // piece distance
            otherConnTrnsf.position -= otherConn.Heading * pieceDistance;

            // Get the parenting back to normal to safeguard future
            // transformations
            otherPiece.transform.SetParent(null, true);
            otherConn.transform.SetParent(otherPiece.transform, true);
        }

        /// <summary>
        /// Initializes the connectors and activates the rigidbodies.
        /// </summary>
        private void Setup()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            MeshCollider mc = GetComponent<MeshCollider>();

            if (rb == null)
                rb = GetComponentInChildren<Rigidbody>();
            if (mc == null)
                mc = GetComponentInChildren<MeshCollider>();

            // Make sure connector collection is initialized
            if (_connectors is null) InitConnectors();

            if (rb == null && mc != null)
                rb = mc.gameObject.AddComponent<Rigidbody>();
            if (mc == null && rb != null)
                mc = rb.gameObject.AddComponent<MeshCollider>();

            if (rb != null)
                rb.isKinematic = true;
        }

        /// <summary>
        /// Retrieves the box colliders of the appropriate layer for the given piece. It will only go into 
        /// the immediate children of the piece.
        /// </summary>
        /// <param name="go">The map piece.</param>
        /// <param name="mask">The layer containing the colliders.</param>
        /// <returns>
        /// The box colliders of the appropriate layer for the given piece.
        /// </returns>
        private static List<BoxCollider> GetBoxColliders(MapPiece go, LayerMask mask)
        {
            List <BoxCollider> ret = new List<BoxCollider>();

            foreach (Transform child in go.transform)
            {
                if ((mask.value & (1 << child.gameObject.layer)) != 0)
                {
                    BoxCollider[] colliders = child.GetComponents<BoxCollider>();
                    foreach (var collider in colliders)
                    {
                        ret.Add(collider);
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// Retrieves the first voxel collider of the appropriate layer for the given piece. It will only go into 
        /// the immediate children of the piece.
        /// </summary>
        /// <param name="go">The map piece.</param>
        /// <param name="mask">The layer containing the colliders.</param>
        /// <returns>
        /// The voxel collider of the appropriate layer for the given piece.
        /// </returns>
        private static VoxelCollider GetVoxelCollider(MapPiece go, LayerMask mask)
        {
            foreach (Transform child in go.transform)
            {
                VoxelCollider collider = child.GetComponent<VoxelCollider>();
                if (collider == null) continue;
                if ((mask.value & (1 << child.gameObject.layer)) != 0)
                {
                    return collider;
                }
            }

            return null;
        }

        /// <summary>
        /// Initializes the connectors.
        /// </summary>
        private void InitConnectors()
        {
            // Calling this method if _connectors is not null is a bug
            Assert.IsNull(_connectors);

            // Get connectors
            IEnumerable<Connector> children =
                GetComponentsInChildren<Connector>();

            // Sort connectors using their default ordering
            _connectors = children
                .Distinct()
                .OrderBy(n => n)
                .ToArray();
        }

        /// <summary>
        /// Order the pieces by how many connectors they have
        /// </summary>
        /// <param name="other">Piece to compare with this one.</param>
        /// <returns>
        /// Returns -1 if this piece has more connectors than the other, 0 if
        /// they have the same amount of connectors or 1 if this piece has less
        /// connectors than the other.
        /// </returns>
        public int CompareTo(MapPiece other)
        {
            if (ConnectorCount > other.ConnectorCount)
                return -1;
            else if (ConnectorCount < other.ConnectorCount)
                return 1;
            else
                return 0;
        }
    }
}
