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
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SnapMeshPCG
{
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

        // Use clipping correction?
        private bool _useRigidBody;

        /// <summary>
        /// Number of connectors in this piece.
        /// </summary>
        public int ConnectorCount => _Connectors.Count;

        /// <summary>
        /// Checks all connectors to see if they're already connected to
        /// another.
        /// </summary>
        /// <returns> Are all the connectors in this piece used?</returns>
        public bool IsFull()
        {
            foreach (Connector c in _Connectors)
                if (!c.IsUsed) return false;
            return true;
        }

        /// <summary>
        /// Create a copy of this piece.
        /// </summary>
        /// <param name="useRigidBody">Use clipping correction?</param>
        /// <returns>A copy of this piece.</returns>
        public GameObject ClonePiece(bool useRigidBody)
        {
            MapPiece clonedPiece = Instantiate(this);
            clonedPiece.Setup(useRigidBody);
            return clonedPiece.gameObject;
        }

#if DEBUG_INTERSECTION
        private static int interError = 0;
#endif

        /// <summary>
        /// Evaluate piece.
        /// </summary>
        /// <param name="rules"></param>
        /// <param name="other"></param>
        /// <param name="pieceDistance"></param>
        /// <param name="groupTolerance"></param>
        /// <param name="colorMatrix"></param>
        /// <returns></returns>
        public (bool valid, Transform positionRot) EvaluatePiece(
            SnapRules rules, MapPiece other,
            bool intersectionTest, LayerMask intersectionLayer,
            float pieceDistance = 0.00f, uint groupTolerance = 0,
            bool[,] colorMatrix = null)
        {

            List<(Connector mine, Connector oth)> possibleCombos =
                new List<(Connector mine, Connector oth)>();

            foreach (Connector co in other._Connectors)
            {
                foreach (Connector ct in _Connectors)
                {
                    // Match criteria
                    if (!co.IsUsed && !ct.IsUsed)
                    {
                        bool pinMatch = true;
                        bool colorMatch = true;

                        // If we're using pins count as criteria, check pins
                        if (rules.UsePins())
                        {
                            pinMatch = Mathf.Abs(co.Pins - ct.Pins) <= groupTolerance;
                        }

                        // If we're using colour as criteria, check colour matrix
                        if (rules.UseColours())
                        {
                            colorMatch =
                                colorMatrix?[(int)ct.ConnColor, (int)co.ConnColor]
                                ?? true;
                        }

                        // If we have a match, then this is a possible connection
                        if (pinMatch && colorMatch)
                            possibleCombos.Add((ct, co));
                    }
                }
            }

            while (possibleCombos.Count > 0)
            {
                int chosenIndex = UnityEngine.Random.Range(0, possibleCombos.Count);

                (Connector chosenMine, Connector chosenOther) chosenCombo =
                    possibleCombos[chosenIndex];

                var prevPos = other.transform.position;
                var prevRot = other.transform.rotation;
                var prevScale = other.transform.localScale;

                Transform trn = TransformPiece(
                    chosenCombo.chosenMine, chosenCombo.chosenOther,
                    other, pieceDistance);

                Physics.SyncTransforms();

                if (intersectionTest)
                {
                    // Check if there is an intersection with the existing geometry,
                    // using this transform
                    bool    intersection = false;
                    var     candidateBoxColliders = GetBoxColliders(other, intersectionLayer);

                    foreach (var boxCollider in candidateBoxColliders)
                    {
                        Vector3 center = boxCollider.center + boxCollider.transform.position;
                        Vector3 extents = new Vector3(0.5f * boxCollider.size.x * boxCollider.transform.lossyScale.x,
                                                      0.5f * boxCollider.size.y * boxCollider.transform.lossyScale.y,
                                                      0.5f * boxCollider.size.z * boxCollider.transform.lossyScale.z);

                        var hits = Physics.OverlapBox(center, extents, boxCollider.transform.rotation, intersectionLayer);

                        foreach (var hit in hits)
                        {
                            // Check for self intersection, or intersection with the connected piece
                            // Ignore the collision in those cases
                            MapPiece parentMapPiece = hit.GetComponentInParent<MapPiece>();
                            if ((parentMapPiece == other) ||
                                (parentMapPiece == this)) continue;

#if DEBUG_INTERSECTION
                            Debug.Log("Can't connect " + name + " with " + other.name);
                            Debug.Log("Connector " + chosenCombo.chosenMine.name + " / " + chosenCombo.chosenOther.name);
                            Debug.Log("Intersection detected with " + parentMapPiece.name);

                            // Create a copy for later debug
                            MapPiece newObject = Instantiate(other);
                            newObject.name = "IntersectionError " + (interError++);
                            newObject.transform.position += Vector3.up * 20;
                            newObject.transform.SetParent(null);
                            newObject.gameObject.SetActive(false);
                            Debug.Log("New object = " + newObject.name);
#endif

                            // It auto-intersects, so remove this possibility and retry
                            possibleCombos.Remove(chosenCombo);

                            intersection = true;
                            break;
                        }

                        if (intersection) break;
                    }

                    if (intersection)
                    {
                        // Undo transform, so we can do it again...
                        other.transform.position = prevPos;
                        other.transform.rotation = prevRot;
                        other.transform.localScale = prevScale;
                        continue;
                    }
                }

                Connector.Match(chosenCombo.chosenMine, chosenCombo.chosenOther);

                return (true, trn);
            }
            return (false, null);
        }

        /// <summary>
        /// Gets the correct position and rotation of the other piece so its
        /// connector group matches this piece's.
        /// </summary>
        /// <param name="myConnectorGroup"></param>
        /// <param name="otherConnectorGroup"></param>
        /// <param name="otherPiece"></param>
        /// <returns></returns>
        private Transform TransformPiece(Connector myConnectorGroup,
            Connector otherConnectorGroup, MapPiece otherPiece, float offset)
        {

            Transform newPieceTrn = otherConnectorGroup.transform;
            Quaternion connectorPointRotation = new Quaternion();

            // temporarily revert parenting so we can move the connector
            // group and have the geometry follow.

            otherConnectorGroup.transform.SetParent(null, true);
            otherPiece.transform.SetParent(otherConnectorGroup.transform, true);

            // Put the other piece on my connector
            newPieceTrn.position = myConnectorGroup.transform.position;

            // Have the other connector group look towards my connector group
            connectorPointRotation.SetLookRotation(
                -myConnectorGroup.Heading,
                transform.up);

            // Apply the rotation acquired above
            newPieceTrn.rotation = connectorPointRotation;

            // Move the pieces away from each other based on an offset
            newPieceTrn.position -= otherConnectorGroup.Heading * offset;

            // Get the parenting back to normal to safeguard future
            // transformations
            otherPiece.transform.SetParent(null, true);
            otherConnectorGroup.transform.SetParent(otherPiece.transform, true);

            return newPieceTrn;
        }

        /// <summary>
        /// Initializes the connectors and activates the rigidbodies.
        /// </summary>
        /// <param name="spawnRigid">Use clipping correction?</param>
        private void Setup(bool spawnRigid)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            MeshCollider mc = GetComponent<MeshCollider>();

            if(rb == null)
                rb = GetComponentInChildren<Rigidbody>();
            if(mc == null)
                mc = GetComponentInChildren<MeshCollider>();

            _useRigidBody = spawnRigid;

            // Make sure connector collection is initialized
            if (_connectors is null) InitConnectors();

            if (rb == null && mc != null)
                rb = mc.gameObject.AddComponent<Rigidbody>();
            if (mc == null && rb != null)
                mc = rb.gameObject.AddComponent<MeshCollider>();

            if (_useRigidBody)
            {
                rb.isKinematic = false;
                mc.convex = true;
                rb.useGravity = false;
                rb.mass = 0;
                rb.drag = 900;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }
            else
            {
                rb.isKinematic = true;
            }
        }

        /// <summary>
        /// Retrieves the box colliders of the appropriate layer
        /// for the given piece
        /// </summary>
        static List<BoxCollider> GetBoxColliders(MapPiece go, LayerMask mask)
        {
            List<BoxCollider> ret = new List<BoxCollider>();

            BoxCollider[] colliders = go.GetComponentsInChildren<BoxCollider>();

            foreach (var collider in colliders)
            {
                if ((mask.value & (1 << collider.gameObject.layer)) != 0)
                {
                    ret.Add(collider);
                }
            }

            return ret;
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
