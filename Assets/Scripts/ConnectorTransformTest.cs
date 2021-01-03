using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace trinityGen
{

    public class ConnectorTransformTest : MonoBehaviour
    {
        //This is the Transform of the second GameObject
        public Transform otherPiece;
        public Transform otherConnector;
        Quaternion m_MyQuaternion;
        float m_Speed = 1.0f;

        public GameObject myConnector;

        void Start()
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


        private void OnDrawGizmos() {
            Gizmos.DrawWireCube(transform.position, new Vector3(0.5f,0.5f,0.5f));
            Gizmos.DrawWireSphere(myConnector.transform.position, 0.2f);
            Gizmos.DrawLine(myConnector.transform.position, myConnector.transform.position + myConnector.transform.forward );

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(otherConnector.position, 0.2f);
            Gizmos.DrawLine(otherConnector.position, otherConnector.position + otherConnector.forward );

        }
    }
}

