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

using UnityEngine;

namespace SnapMeshPCG
{
    /// <summary>
    /// A class for debugging connector transforms. Does not do anything in the
    /// generation process.
    /// </summary>
    public class ConnectorTransformTest : MonoBehaviour
    {
        //This is the Transform of the second GameObject
        private Transform otherPiece = null;

        private Transform otherConnector = null;

        private Quaternion _myQuaternion;

        private GameObject myConnector = null;

        private void Start()
        {
            Transform newPieceTrn = otherConnector;
            Quaternion connectorPointRotation = new Quaternion();

            otherConnector.SetParent(null, true);
            otherPiece.SetParent(otherConnector, true);

            print(connectorPointRotation);
            newPieceTrn.position = myConnector.transform.position;

            connectorPointRotation.SetLookRotation(
                -myConnector.transform.forward, otherConnector.up);

            print(connectorPointRotation);
            newPieceTrn.rotation = connectorPointRotation;
            newPieceTrn.position -= otherConnector.transform.forward * 0.5f;

            otherConnector.SetParent(null, true);
            otherPiece.SetParent(otherConnector, true);
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position,
                new Vector3(0.5f, 0.5f, 0.5f));
            Gizmos.DrawWireSphere(myConnector.transform.position, 0.2f);
            Gizmos.DrawLine(myConnector.transform.position,
                myConnector.transform.position + myConnector.transform.forward);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(otherConnector.position, 0.2f);
            Gizmos.DrawLine(otherConnector.position,
                otherConnector.position + otherConnector.forward);
        }
    }
}

