#if false
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace IntegratedStackedTankDecouplers
{
    public class NodeControl
    {

        AttachNode topNode;
        public Vector3 pos;
        const double RADIAN = 180 / Math.PI;

        const string NODE_PREFIX = "sep_";


        public void DelNodes(Part p)
        {
            Log.Info("Deleting Nodes");
#if true
            for (int idx = p.attachNodes.Count() - 1; idx >= 0; idx--)
            {
                if (p.attachNodes[idx].id.Length >= NODE_PREFIX.Length)
                    if (p.attachNodes[idx].id.Substring(0, NODE_PREFIX.Length) == NODE_PREFIX)
                        p.attachNodes.Remove(p.attachNodes[idx]);
            }

#else
            for (int idx = p.children.Count() - 1; idx > 0; idx--)
            {
                var part = p.children[idx];
                Log.Info("part.partInfo.name: " + part.partInfo.name);
                if (part.partInfo.name == "integratedSepMotor")
                {
                    Log.Info("Deleting part");
                   // p.removeChild(part);
                   // EditorLogic.fetch.ship.Parts.Remove(part);
                    EditorLogic.DeletePart(part);

                }
            }
#endif
        }

        public NodeControl(Part p, int nodeCnt)
        {
            AddNodes(p, nodeCnt);
        }

        public void AddNodes(Part p, int nodeCnt)
        {
            Log.Info("Adding nodes");
#if true
            if (GetTopNode(p) && nodeCnt > 0)
            {
                DelNodes(p);
                double radius = Math.Min(p.prefabSize.z / 2, p.prefabSize.x / 2);

                double angle = 0;
                for (int cnt = 0; cnt < Math.Max(2, nodeCnt); cnt++)
                {
                    angle = ((360 / nodeCnt) / RADIAN) * cnt;

                    string newNodeName = NODE_PREFIX + cnt.ToString();


                    double x = radius * Math.Cos(angle);
                    double z = radius * Math.Sin(angle);
                    //pos.z = pos.z + p.prefabSize.z / 2;

                    pos = topNode.position;

                    pos.x += (float)Math.Round(x, 4);
                    pos.z += (float)Math.Round(z, 4);

                    var an = new AttachNode
                    {
                        owner = p,
                        position = pos,
                        nodeType = AttachNode.NodeType.Stack,
                        size = 0,
                        id = newNodeName,
                        attachMethod = AttachNodeMethod.FIXED_JOINT,
                        nodeTransform = p.transform,
                        orientation = p.transform.up
                    };

                    p.attachNodes.Add(an);
                }
            }
#else
            AddParts(p, nodeCnt);
#endif
        }

        bool GetTopNode(Part p)
        {
            foreach (var attachNode in p.attachNodes.Where(an => an != null))
            {
                if (p.srfAttachNode != null && attachNode == p.srfAttachNode)
                    continue;
                if (attachNode.id == "")
                    continue;
                if (attachNode.id == "top")
                {
                    topNode = attachNode;
                    return true;

                }
            }
            return false;
        }

        public Part CreatePart(string partname)
        {
            AvailablePart avPart = PartLoader.getPartInfoByName(partname);

            UnityEngine.Object obj = UnityEngine.Object.Instantiate(avPart.partPrefab);
            if (!obj)
            {
                Log.Info("CreatePart(Crate) Failed to instantiate " + avPart.partPrefab.name);
                return null;
            }

            Part newPart = UnityEngine.Object.Instantiate<Part>(avPart.partPrefab);

            newPart.gameObject.SetActive(true);
            newPart.gameObject.name = avPart.name;
            newPart.partInfo = avPart;
            newPart.symMethod = SymmetryMethod.Radial;
    
            return newPart;
        }


        void AddParts(Part p, int nodeCnt)
        {
            double radius = Math.Min(p.prefabSize.z / 2, p.prefabSize.x / 2);
            double angle = 0;

            if (GetTopNode(p) && nodeCnt > 0)
            {
                for (int cnt = 0; cnt < Math.Max(2, nodeCnt); cnt++)
                {

                    Part newP = CreatePart("integratedSepMotor");
                    Log.Info("newP prefabSize.x: " + newP.prefabSize.x.ToString() + ", z: " + newP.prefabSize.z.ToString());
                    radius = Math.Min(p.prefabSize.z / 2, p.prefabSize.x / 2) - Math.Max(newP.prefabSize.x, newP.prefabSize.z);
                    if (newP != null)
                    {
                        //newP.partTransform = topNode.nodeTransform;
                        angle = ((360 / nodeCnt) / RADIAN) * cnt;
                        double x = radius * Math.Cos(angle);
                        double z = radius * Math.Sin(angle);
                        //pos.z = pos.z + p.prefabSize.z / 2;

                        pos = topNode.position;
                        pos.x += (float)Math.Round(x, 4);
                        pos.z += (float)Math.Round(z, 4);

                        Log.Info("cnt: " + cnt.ToString() + ",  angle: " + (angle * RADIAN).ToString() + ",   x: " + pos.x.ToString() + ", z: " + pos.z.ToString());

                        // multiply rotation by 180 on y axis
                       // newP.transform.localRotation *= Quaternion.Euler(0f, 0f, 180f);

                        Log.Info("Adding part");
                        EditorLogic.fetch.ship.Add(newP);
                        p.addChild(newP);
                        newP.setParent(p);
                        newP.transform.parent = p.transform;

                        newP.transform.localPosition = pos;

                        newP.attRotation0 = newP.transform.localRotation;
                        newP.attPos0 = pos + p.attPos;
                        newP.onAttach(p, true);

                    }
                }
                GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
            }

        }
    }
}


#endif
