using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace TrinityGen
{


    public class ArenaPiece : MonoBehaviour, IComparable<ArenaPiece>
    {
        /// <summary>
        /// DO NOT UNDER ANY EXCUSE REMOVE THE [SERIALIZEFIELD] OR
        /// THE INITIALIZATION FROM THESE LINES IT WILL MAKE UNITY HANG AND
        /// EAT YOUR MEMORY.
        /// </summary>
        [HideInInspector] [SerializeField] private List<Connector> _connectors
        = new List<Connector>();

        private bool _useRigidBody;

        [HideInInspector] public int ConnectorsCount;

       /* [SerializeField] private List<IArenaInitializable> initList = new List<IArenaInitializable>();*/


        /// <summary>
        /// Detects the connectors, sorts them and activates the rigidbodies
        /// </summary>
        /// <param name="spawnRigid"></param>
        public void Setup(bool spawnRigid)
        {

            //Debug.Log("Using first bottom/top connectors found.");
            List<Connector> children = new List<Connector>();
            _useRigidBody = spawnRigid;
            //Detect connectors
            foreach(Connector c in GetComponentsInChildren<Connector>())
            {

                children.Add(c);

            }

           /* List<IArenaInitializable> initChildren =
                new List<IArenaInitializable>();

            foreach (IArenaInitializable init in
                GetComponentsInChildren<IArenaInitializable>())
            {
                initChildren.Add(init);
            }

            initList = initChildren.Distinct().ToList();*/

            _connectors = children.Distinct().ToList();
            _useRigidBody = spawnRigid;
            _connectors.Sort();
            ConnectorsCount = _connectors.Count;


            foreach (Connector g in _connectors)
            {
                g.isUsed = false;
            }


            Rigidbody rb = GetComponent<Rigidbody>();
            MeshCollider mc = GetComponent<MeshCollider>();

            if(rb == null)
                rb = gameObject.AddComponent<Rigidbody>();
            if (mc == null)
                mc = gameObject.AddComponent<MeshCollider>();


            if(_useRigidBody)
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
            foreach(Connector c in _connectors)
                if(!c.isUsed)
                    return false;
            return true;

        }


        public (bool valid, Transform positionRot) EvaluatePiece(ConnectorMatchingRules rules, ArenaPiece other, float pieceDistance = 0.00f, uint groupTolerance = 0,  bool[,] colorMatrix = null)
        {

            List<(Connector mine, Connector oth)> possibleCombos =
            new List<(Connector mine, Connector oth)>();
            //Check for intersecting geometry?
            //Spawn the piece and have it tell if the trigger collider reports back?
            // ...but what if the piece is not all in one mesh?

            foreach(Connector co in other._connectors)
            {
                foreach(Connector ct in this._connectors)
                {
                    bool pinMatch = false;
                    bool colorMatch = false;
                    bool fullMatch = false;
                    // Match criteria
                    if(!co.isUsed && !ct.isUsed)
                    {
                        if((Mathf.Abs(co.pins - ct.pins) <= groupTolerance) && colorMatrix != null)
                            pinMatch = true;

                        if(colorMatrix[(int)ct.color, (int)co.color])
                        {
                            colorMatch = true;
                        }

                        if(rules == ConnectorMatchingRules.PINS && pinMatch)
                            fullMatch = true;
                        else if(rules == ConnectorMatchingRules.COLOURS && colorMatch)
                            fullMatch = true;
                        else if(rules == ConnectorMatchingRules.PINS_AND_COLORS && pinMatch && colorMatch)
                            fullMatch = true;
                        else
                            fullMatch = false;

                        if(fullMatch)
                            possibleCombos.Add((ct, co));

                    }
                }

            }

            if(possibleCombos.Count > 0)
            {
                (Connector chosenMine, Connector chosenOther)
                choosenCombo = possibleCombos[
                    UnityEngine.Random.Range(0, possibleCombos.Count)];

                choosenCombo.chosenOther.isUsed = true;
                choosenCombo.chosenMine.isUsed = true;

                choosenCombo.chosenMine.myMatch = choosenCombo.chosenOther;
                choosenCombo.chosenOther.myMatch = choosenCombo.chosenMine;

                Transform trn = TransformPiece(choosenCombo.chosenMine,
                choosenCombo.chosenOther, other, pieceDistance);

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
        private Transform TransformPiece(Connector myConnectorGroup, Connector otherConnectorGroup, ArenaPiece otherPiece,
        float offset)
        {

            Transform newPieceTrn = otherConnectorGroup.transform;
            Quaternion connectorPointRotation = new Quaternion();

            // temprarily revert parenting so we can move the connector
            // group and have the geometry follow.

            otherConnectorGroup.transform.SetParent(null, true);
            otherPiece.transform.SetParent(otherConnectorGroup.transform, true);

            // Put the other piece on my connector
            newPieceTrn.position = myConnectorGroup.transform.position;

                // Have the other connector group look towards my connector group
                connectorPointRotation.SetLookRotation(
                    -myConnectorGroup.heading,
                    transform.up);

                // Apply the rotation acquired above
                newPieceTrn.rotation = connectorPointRotation;




            // move the pieces away from each other based on an offset
            newPieceTrn.position -= otherConnectorGroup.heading * offset;

            // get the parenting back to normal to safeguard future transformations.
            otherPiece.transform.SetParent(null, true);
            otherConnectorGroup.transform.SetParent(otherPiece.transform, true);


            return newPieceTrn;

        }

        /// <summary>
        /// Order the peices by how many connectors they have
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(ArenaPiece other)
        {
            if (this._connectors.Count >
                other._connectors.Count)
                return -1;
            else if (this._connectors.Count <
                other._connectors.Count)
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
    }
}
