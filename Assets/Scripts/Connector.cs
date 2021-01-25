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
using UnityEngine;
using NaughtyAttributes;

namespace TrinityGen
{
    public class Connector : MonoBehaviour, IComparable<Connector>
    {
        private enum ConnectorVisual { WIREFRAME, OPAQUE }

        private static float ConnectorSize = 0.5f;
        private static float GizmoTransparency = 1;
        private static ConnectorVisual GizmoVisuals;

        [SerializeField] [ReadOnly]
        private Connector match;

        [SerializeField] [HideInInspector]
        private bool isUsed;

        [SerializeField]
        public ConnectorColor color;

        // Number of pins in this connector
        [SerializeField]
        private int pins = 0;

        [OnValueChanged("OnSpacingChanged")]
        [SerializeField] private float _pinSpacing = 0.5f;

        [OnValueChanged("OnTransparencyChanged")] [Range(0, 1)] [SerializeField]
        private float _gizmoTransparency = 1;

        [OnValueChanged("OnLooksChanged")] [SerializeField]
        private ConnectorVisual _gizmoLooks;

        /// <summary>
        /// Number of pins in this connector.
        /// </summary>
        public int Pins => pins;

        /// <summary>
        /// Is this connector currently being used?
        /// </summary>
        public bool IsUsed => isUsed;

        public Vector3 Heading => transform.forward;

        private void OnSpacingChanged() =>
            ConnectorSize = _pinSpacing;
        private void OnTransparencyChanged() =>
            GizmoTransparency = _gizmoTransparency;
        private void OnLooksChanged() =>
            GizmoVisuals = _gizmoLooks;

        //[SerializeField] private Color _gizmoColor;

        private void Awake()
        {
            //GetUnionPoint();
            /*foreach (Connector c in _connectors)
                c.groupColor = gizmoColor;
            */
        }

        /// <summary>
        /// Match two connectors.
        /// </summary>
        /// <param name="conn1">First connector to match.</param>
        /// <param name="conn2">Second connector to match.</param>
        public static void Match(Connector conn1, Connector conn2)
        {
            conn1.match = conn2;
            conn1.isUsed = true;
            conn2.match = conn1;
            conn2.isUsed = true;
        }

        public int CompareTo(Connector other)
        {
            // I want the large ones at the start of the lists
            if (pins > other.pins)
                return -1;
            else if (pins < other.pins)
                return 1;
            else
                return 0;
        }

        private void OnDrawGizmos()
        {
            Color col;
            switch (color)
            {
                case ConnectorColor.WHITE:
                    col = Color.white;
                    break;
                case ConnectorColor.RED:
                    col = Color.red;
                    break;
                case ConnectorColor.GREEN:
                    col = new Color(0.25f, 0.9f, 0.25f);
                    break;
                case ConnectorColor.BLUE:
                    col = new Color(0.25f, 0.5f, 0.9f);
                    break;
                case ConnectorColor.CYAN:
                    col = new Color(0.25f, 0.9f, 1.0f);
                    break;
                case ConnectorColor.ORANGE:
                    col = new Color(1.0f, 0.6f, 0);
                    break;
                case ConnectorColor.YELLOW:
                    col = Color.yellow;
                    break;
                case ConnectorColor.PINK:
                    col = new Color(0.9f, 0.6f, 0.9f);
                    break;
                case ConnectorColor.PURPLE:
                    col = Color.magenta;
                    break;
                case ConnectorColor.BROWN:
                    col = new Color(0.6f, 0.2f, 0.2f);
                    break;
                case ConnectorColor.BLACK:
                    col = Color.black;
                    break;
                case ConnectorColor.GREY:
                    col = Color.grey;
                    break;
                default:
                    col = default;
                    break;
            }

            col.a = GizmoTransparency;
            Gizmos.color = col;
            Gizmos.DrawLine(transform.position,
                transform.position + Heading * 2);
            Gizmos.DrawSphere(transform.position, 0.1f);
            Gizmos.DrawLine(transform.position,
                transform.position + transform.right * (pins + 1) / 2 * ConnectorSize);
            Gizmos.DrawLine(transform.position,
                transform.position - transform.right * (pins + 1) / 2 * ConnectorSize);

            for (float i = 0 - pins / 2; i <= pins / 2; i++)
            {
                Vector3 pos;

                if (pins % 2 == 0 && i == 0)
                {
                    continue;
                }

                //pos.x = transform.position.x + (i * connectorSpacing);
                pos = transform.position + transform.right * i * ConnectorSize;
                //pos.z = transform.position.z * transform.right.z  + (i * connectorSpacing);

                Gizmos.matrix = Matrix4x4.TRS(pos, transform.rotation, Vector3.one);

                if (GizmoVisuals == ConnectorVisual.OPAQUE)
                {
                    Gizmos.DrawCube(Vector3.zero, new Vector3(
                    ConnectorSize * 0.9f,
                    ConnectorSize * 0.9f,
                    ConnectorSize * 0.9f));
                }
                else
                {
                    Gizmos.DrawWireCube(Vector3.zero, new Vector3(
                        ConnectorSize * 0.9f,
                        ConnectorSize * 0.9f,
                        ConnectorSize * 0.9f));
                }
            }
        }
    }
}