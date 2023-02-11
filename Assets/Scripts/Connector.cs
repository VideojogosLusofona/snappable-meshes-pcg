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
using UnityEngine;
using NaughtyAttributes;

namespace SnapMeshPCG
{
    /// <summary>
    /// Represents the behavior of a connector placed in a map piece.
    /// </summary>
    public class Connector : MonoBehaviour, IComparable<Connector>
    {
        // How to display connector gizmos in the scene view
        private enum ConnectorVisual { Wireframe, Opaque }

        // Show in inspector what other connector is this connector snapped with
        [SerializeField] [ReadOnly]
        private Connector match;

        // //////////////////////////////////////////////////// //
        // Code to globally define the look of connector gizmos //
        // //////////////////////////////////////////////////// //

        // Global connector visualization properties
        private static float _PinSpacing = 0.5f;
        private static float _GizmoTransparency = 1;
        private static ConnectorVisual _GizmoVisuals;

        // Editor variables for changing the connector visuals
        [OnValueChanged(nameof(OnSpacingChanged))]
        [SerializeField]
        private float _pinSpacing = 0.5f;

        [OnValueChanged(nameof(OnTransparencyChanged))]
        [Range(0, 1)]
        [SerializeField]
        private float _gizmoTransparency = 1;

        [OnValueChanged(nameof(OnLooksChanged))]
        [SerializeField]
        private ConnectorVisual _gizmoLooks = ConnectorVisual.Wireframe;

        // Callbacks which change the global visualization properties after
        // the user changes them in the editor for a single connector
        private void OnSpacingChanged() => _PinSpacing = _pinSpacing;
        private void OnTransparencyChanged() => _GizmoTransparency = _gizmoTransparency;
        private void OnLooksChanged() => _GizmoVisuals = _gizmoLooks;

        // ////////////////////////////////////////////////////////////// //
        // Connector parameters, which define how this connector can snap //
        // with other connectors.                                         //
        // ////////////////////////////////////////////////////////////// //

        // Connector color, which can influence what other connectors this
        // connector can snap with
        [SerializeField]
        private ConnectorColor connColor = ConnectorColor.White;

        // Number of pins in this connector, which can influence what other
        // connectors this connector can snap with
        [SerializeField]
        private int pins = 0;

        // /////////////////////////////////////////////////// //
        // Public properties for accessing the connector state //
        // /////////////////////////////////////////////////// //

        /// <summary>
        /// Number of pins in this connector.
        /// </summary>
        public int Pins
        {
            set { pins = value; }
            get { return pins; }
        }

        /// <summary>
        /// The color of this connector.
        /// </summary>
        public ConnectorColor ConnColor => connColor;

        /// <summary>
        /// Is this connector currently being used?
        /// </summary>
        [ShowNativeProperty]
        public bool IsUsed => !(match is null);

        /// <summary>
        /// Connector heading.
        /// </summary>
        public Vector3 Heading => transform.forward;

        /// <summary>
        /// Snap this connector with another connector.
        /// </summary>
        /// <param name="other">Connector to snap this one with.</param>
        public void SnapWith(Connector other)
        {
            match = other;
            other.match = this;
        }

        /// <summary>
        /// Used for sorting connectors. Connectors are sorted by number of
        /// pins in descending order.
        /// </summary>
        /// <param name="other">
        /// The other connector to compare this one with.
        /// </param>
        /// <returns>
        /// A negative number if this connector has more pins than the other,
        /// a positive number if it has less, or zero if both connectors have
        /// the same amount of pins.
        /// </returns>
        public int CompareTo(Connector other)
        {
            if (pins > other.pins)
                return -1;
            else if (pins < other.pins)
                return 1;
            else
                return 0;
        }

        /// <summary>
        /// Draw connector gizmos.
        /// </summary>
        private void OnDrawGizmos()
        {
            Color col;
            switch (connColor)
            {
                case ConnectorColor.White:
                    col = Color.white;
                    break;
                case ConnectorColor.Red:
                    col = Color.red;
                    break;
                case ConnectorColor.Green:
                    col = new Color(0.25f, 0.9f, 0.25f);
                    break;
                case ConnectorColor.Blue:
                    col = new Color(0.25f, 0.5f, 0.9f);
                    break;
                case ConnectorColor.Cyan:
                    col = new Color(0.25f, 0.9f, 1.0f);
                    break;
                case ConnectorColor.Orange:
                    col = new Color(1.0f, 0.6f, 0);
                    break;
                case ConnectorColor.Yellow:
                    col = Color.yellow;
                    break;
                case ConnectorColor.Pink:
                    col = new Color(0.9f, 0.6f, 0.9f);
                    break;
                case ConnectorColor.Purple:
                    col = Color.magenta;
                    break;
                case ConnectorColor.Brown:
                    col = new Color(0.6f, 0.2f, 0.2f);
                    break;
                case ConnectorColor.Black:
                    col = Color.black;
                    break;
                case ConnectorColor.Grey:
                    col = Color.grey;
                    break;
                default:
                    col = default;
                    break;
            }

            col.a = _GizmoTransparency;
            Gizmos.color = col;
            Gizmos.DrawLine(transform.position,
                transform.position + Heading * 2);
            Gizmos.DrawSphere(transform.position, 0.1f);
            Gizmos.DrawLine(transform.position,
                transform.position + transform.right * (pins + 1) / 2 * _PinSpacing);
            Gizmos.DrawLine(transform.position,
                transform.position - transform.right * (pins + 1) / 2 * _PinSpacing);

            for (float i = 0 - pins / 2; i <= pins / 2; i++)
            {
                Vector3 pos;

                if (pins % 2 == 0 && i == 0)
                {
                    continue;
                }

                //pos.x = transform.position.x + (i * connectorSpacing);
                pos = transform.position + transform.right * i * _PinSpacing;
                //pos.z = transform.position.z * transform.right.z  + (i * connectorSpacing);

                Gizmos.matrix = Matrix4x4.TRS(pos, transform.rotation, Vector3.one);

                if (_GizmoVisuals == ConnectorVisual.Opaque)
                {
                    Gizmos.DrawCube(Vector3.zero, new Vector3(
                    _PinSpacing * 0.9f,
                    _PinSpacing * 0.9f,
                    _PinSpacing * 0.9f));
                }
                else
                {
                    Gizmos.DrawWireCube(Vector3.zero, new Vector3(
                        _PinSpacing * 0.9f,
                        _PinSpacing * 0.9f,
                        _PinSpacing * 0.9f));
                }
            }
        }
    }
}