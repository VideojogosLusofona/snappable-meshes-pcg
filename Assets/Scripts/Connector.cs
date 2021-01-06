using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace trinityGen
{


    public class Connector : MonoBehaviour, IComparable<Connector>
    {

        [SerializeField] public ConnectorColor color;
        public Vector3 heading => transform.forward;


        [HideInInspector] public bool isUsed = false;

        public int pins = 0;

        [SerializeField]private  float _pinSpacing = 0.5f;


        //[SerializeField] private Color _gizmoColor;

        private void Awake()
        {
            //GetUnionPoint();
            /*foreach (Connector c in _connectors)
                c.groupColor = gizmoColor;
            */  
            
        }

        public int CompareTo(Connector other)
        {
            // I want the large ones at the start of the lists
            if (this.pins > other.pins)
                return -1;
            else if (this.pins < other.pins)
                return 1;
            else
                return 0;
        }

        private void OnDrawGizmos()
        {

            switch(color)
            {
                case(ConnectorColor.WHITE):
                    Gizmos.color = Color.white;
                    break;
                case(ConnectorColor.RED):
                    Gizmos.color = Color.red;
                    break;
                case(ConnectorColor.GREEN):
                    Gizmos.color = new Color(0.25f, 0.9f, 0.25f);
                    break;
                case(ConnectorColor.BLUE):
                    Gizmos.color = new Color(0.25f, 0.5f, 0.9f);
                    break;
                case(ConnectorColor.CYAN):
                    Gizmos.color = new Color(0.25f, 0.9f, 1.0f);
                    break;
                case(ConnectorColor.ORANGE):
                    Gizmos.color = new Color(1.0f, 0.6f, 0);
                    break;
                case(ConnectorColor.YELLOW):
                    Gizmos.color = Color.yellow;
                    break;
                case(ConnectorColor.PINK):
                    Gizmos.color = new Color(0.9f,0.6f,0.9f);
                    break;
                case(ConnectorColor.PURPLE):
                    Gizmos.color = Color.magenta;
                    break;
                case(ConnectorColor.BROWN):
                    Gizmos.color = new Color(0.6f,0.2f,0.2f);
                    break;
                case(ConnectorColor.BLACK):
                    Gizmos.color = Color.black;
                    break;
                case(ConnectorColor.GREY):
                    Gizmos.color = Color.grey;
                    break;
                


            }
    
            Gizmos.DrawLine(transform.position, transform.position + heading * 2);
            Gizmos.DrawSphere(transform.position, 0.1f);
            Gizmos.DrawLine(transform.position, transform.position + transform.right * (pins+1) /2 * _pinSpacing);
            Gizmos.DrawLine(transform.position, transform.position - transform.right * (pins+1) /2 * _pinSpacing);

            Vector3 pos;

            for(float i = 0 - pins / 2; i <=  pins / 2; i++)
            {
                if(pins % 2 == 0 && i == 0)
                {

                    continue;

                }
                //pos.x = transform.position.x + (i * connectorSpacing);
                pos = transform.position + transform.right * i * _pinSpacing;
                //pos.z = transform.position.z * transform.right.z  + (i * connectorSpacing);

                Gizmos.matrix = Matrix4x4.TRS(pos, transform.rotation, Vector3.one);
                Gizmos.DrawCube(Vector3.zero, new Vector3(
                    _pinSpacing * 0.9f,
                    _pinSpacing * 0.9f,
                    _pinSpacing * 0.9f) );
             
            }
        

            
        }
    }
}