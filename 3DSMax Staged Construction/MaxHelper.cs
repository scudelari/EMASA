using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Max;
using MathNet.Spatial.Euclidean;

namespace _3DSMax_Staged_Construction
{
    public static class MaxHelper
    {
        public static IGlobal MaxGlobal;
        public static IInterface MaxInterface;

        public static List<IINode> GetAllNodesOfTheScene(this IInterface iface)
        {
            List<IINode> toRet = new List<IINode>();
            IINode root = iface.RootNode;
            toRet.Add(root);
            toRet.AddRange(MaxHelper.GetAllChildNodesRecursive(iface.RootNode));
            return toRet;
        }
        private static IEnumerable<IINode> GetAllChildNodesRecursive(IINode iNode)
        {
            int childCount = iNode.NumberOfChildren;
            for (int i = 0; i < childCount; i++)
            {
                // Yields the current child node
                IINode child = iNode.GetChildNode(i);
                yield return child;

                // Yields the children of the current child node
                foreach (var item in GetAllChildNodesRecursive(child))
                {
                    yield return item;
                }
            }
        }
        public static IEnumerable<Point3D> Points(this IObject obj)
        {
            for (int i = 0; i < obj.NumPoints; i++)
            {
                IPoint3 pnt = obj.GetPoint(i);

                yield return new Point3D(pnt.X, pnt.Y, pnt.Z);
            }
        }

        public static (double volume, IDPoint3 center)? GetVolumeAndMassCenter(this ITriObject triObject)
        {
            try
            {
                double Volume = 0;
                IDPoint3 Center = MaxGlobal.DPoint3.Create(0, 0, 0);

                for (int i = 0; i < triObject.Mesh.NumFaces; i++)
                {
                    IFace face = triObject.Mesh.Faces[i];

                    IDPoint3 vert2 = MaxGlobal.DPoint3.Create(triObject.Mesh.GetVert((int)face.GetVert(2)));
                    IDPoint3 vert1 = MaxGlobal.DPoint3.Create(triObject.Mesh.GetVert((int)face.GetVert(1)));
                    IDPoint3 vert0 = MaxGlobal.DPoint3.Create(triObject.Mesh.GetVert((int)face.GetVert(0)));

                    vert1.Subtract(vert0);
                    vert2.Subtract(vert0);

                    double dV = MaxGlobal.DotProd(
                        MaxGlobal.CrossProd(
                            vert1.Subtract(vert0),
                            vert2.Subtract(vert0))
                        , vert0);

                    Volume += dV;

                    Center = Center.Add((vert0.Add(vert1).Add(vert2)).MultiplyBy(dV));
                }

                Volume = Volume / 6;
                Center = Center.DivideBy(24);
                Center = Center.DivideBy(Volume);

                return (Volume, Center);
            }
            catch 
            {
                return null;
            }
        }
        public static (double volume, Point3D center)? GetVolumeAndMassCenter(this IINode node)
        {
            try
            {
                bool toDelete = false;
                ITriObject triObject = node.GetTriObjectFromNodeInCurrentTime(out toDelete);

                (double volume, IDPoint3 center)? ret = triObject.GetVolumeAndMassCenter();

                // Delete the triObject
                //triObject.DeleteAllRefsToMe();

                // Gets the WORLD position
                IDMatrix3 tm = node.GetObjectTM(MaxInterface.Time, null).ToDMatrix3();
                IDPoint3 worldCenter = MaxGlobal.Multiply(tm, ret.Value.center);

                return (ret.Value.volume, worldCenter.ToPoint3D());
            }
            catch
            {
                return null;
            }
        }

        public static ITriObject GetTriObjectFromNode(this IINode node, int time, out bool deleteIt)
        {
            deleteIt = false;
            IObject obj = node.EvalWorldState(time, true).Obj;
            
            IClass_ID triObjId = MaxGlobal.Class_ID.Create((int)BuiltInClassIDA.TRIOBJ_CLASS_ID, 0);

            if (obj.CanConvertToType(triObjId) != 0)
            {
                ITriObject tri = (ITriObject)obj.ConvertToType(time, triObjId);

                // Note that the TriObject should only be deleted
                // if the pointer to it is not equal to the object
                // pointer that called ConvertToType()
                if (obj != tri) deleteIt = true;
                return tri;
            }
            else
            {
                return null;
            }
        }
        public static ITriObject GetTriObjectFromNodeInCurrentTime(this IINode node, out bool deleteIt)
        {
            return node.GetTriObjectFromNode(MaxInterface.Time, out deleteIt);
        }

        public static Point3D ToPoint3D(this IPoint3 pnt)
        {
            return new Point3D(pnt.X, pnt.Y, pnt.Z);
        }
        public static Point3D ToPoint3D(this IDPoint3 pnt)
        {
            return new Point3D(pnt.X, pnt.Y, pnt.Z);
        }
        public static IDMatrix3 ToDMatrix3(this IMatrix3 matrix3)
        {
            return MaxGlobal.DMatrix3.Create(
                    MaxGlobal.DPoint3.Create(matrix3.GetRow(0)),
                    MaxGlobal.DPoint3.Create(matrix3.GetRow(1)),
                    MaxGlobal.DPoint3.Create(matrix3.GetRow(2)),
                    MaxGlobal.DPoint3.Create(matrix3.GetRow(3))
                );
        }
    }
}
