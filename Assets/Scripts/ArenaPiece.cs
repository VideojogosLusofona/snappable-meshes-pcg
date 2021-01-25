/*
 * Copyright 2021 TrinityGenerator_Standalone contributors
 * (https://github.com/RafaelCS-Aula/TrinityGenerator_Standalone)
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

namespace TrinityGen
{
    public class ArenaPiece : MonoBehaviour, IComparable<ArenaPiece>
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
        public int ConnectorsCount => _Connectors.Count;

        /// <summary>
        /// Initializes the connectors and activates the rigidbodies.
        /// </summary>
        /// <param name="spawnRigid">Use clipping correction?</param>
        public void Setup(bool spawnRigid)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            MeshCollider mc = GetComponent<MeshCollider>();

            _useRigidBody = spawnRigid;

            // Make sure connector collection is initialized
            if (_connectors is null) InitConnectors();

            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();
            if (mc == null)
                mc = gameObject.AddComponent<MeshCollider>();

            if (_useRigidBody)
            {
                rb.isKinematic = false;
                mc.convex = true;
                rb = GetComponent<Rigidbody>();
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
        /// Checks all connectors to see if they're already connected to another
        /// </summary>
        /// <returns> Are all the connectors in this piece used?</returns>
        public bool IsFull()
        {
            foreach (Connector c in _Connectors)
                if (!c.IsUsed) return false;
            return true;
        }

        public (bool valid, Transform positionRot) EvaluatePiece(
            ConnectorMatchingRules rules, ArenaPiece other,
            float pieceDistance = 0.00f, uint groupTolerance = 0,
            bool[,] colorMatrix = null)
        {

            List<(Connector mine, Connector oth)> possibleCombos =
                new List<(Connector mine, Connector oth)>();

            // Check for intersecting geometry?
            // Spawn the piece and have it tell if the trigger collider
            // reports back? ...but what if the piece is not all in one mesh?

            foreach (Connector co in other._Connectors)
            {
                foreach (Connector ct in this._Connectors)
                {
                    // Match criteria
                    if (!co.IsUsed && !ct.IsUsed)
                    {
                        bool pinMatch = false;
                        bool colorMatch = false;
                        bool fullMatch;

                        if ((Mathf.Abs(co.Pins - ct.Pins) <= groupTolerance)
                                && colorMatrix != null)
                        {
                            pinMatch = true;
                        }

                        if (colorMatrix[(int)ct.ConnColor, (int)co.ConnColor])
                        {
                            colorMatch = true;
                        }

                        if (rules == ConnectorMatchingRules.PINS && pinMatch)
                        {
                            fullMatch = true;
                        }
                        else if (rules == ConnectorMatchingRules.COLOURS
                            && colorMatch)
                        {
                            fullMatch = true;
                        }
                        else if (rules == ConnectorMatchingRules.PINS_AND_COLORS
                            && pinMatch && colorMatch)
                        {
                            fullMatch = true;
                        }
                        else
                        {
                            fullMatch = false;
                        }

                        if (fullMatch)
                            possibleCombos.Add((ct, co));
                    }
                }
            }

            if (possibleCombos.Count > 0)
            {
                (Connector chosenMine, Connector chosenOther) chosenCombo =
                    possibleCombos[UnityEngine.Random.Range(0, possibleCombos.Count)];

                Connector.Match(chosenCombo.chosenMine, chosenCombo.chosenOther);

                Transform trn = TransformPiece(
                    chosenCombo.chosenMine, chosenCombo.chosenOther,
                    other, pieceDistance);

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
            Connector otherConnectorGroup, ArenaPiece otherPiece, float offset)
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
        /// Order the pieces by how many connectors they have
        /// </summary>
        /// <param name="other">Piece to compare with this one.</param>
        /// <returns>
        /// Returns -1 if this piece has more connectors than the other, 0 if
        /// they have the same amount of connectors or 1 if this piece has less
        /// connectors than the other.
        /// </returns>
        public int CompareTo(ArenaPiece other)
        {
            if (ConnectorsCount > other.ConnectorsCount)
                return -1;
            else if (ConnectorsCount < other.ConnectorsCount)
                return 1;
            else
                return 0;
        }

        /*public void Initialize()
        {
            foreach (IArenaInitializable init in initList)
            {
                init.Initialize();
            }
        }*/

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
    }
}
