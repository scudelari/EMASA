using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhinoInterfaceLibrary
{
    [Flags]
    public enum RhinoObjectType : uint
    {
        None = 0, // Nothing.
        Point = 1, // A point.
        PointSet = 2, // A point set or cloud.
        Curve = 4, // A curve.
        Surface = 8, // A surface.
        Brep = 16, // A brep.
        Mesh = 32, // A mesh.
        Light = 256, // A rendering light.
        Annotation = 512, // An annotation.
        InstanceDefinition = 2048, // A block definition.
        InstanceReference = 4096, // A block reference.
        TextDot = 8192, // A text dot.
        Grip = 16384, // Selection filter value - not a real object type.
        Detail = 32768, // A detail.
        Hatch = 65536, // A hatch.
        MorphControl = 131072, // A morph control.
        BrepLoop = 524288, // A brep loop.
        PolysrfFilter = 2097152, // Selection filter value - not a real object type.
        EdgeFilter = 4194304, // Selection filter value - not a real object type.
        PolyedgeFilter = 8388608, // Selection filter value - not a real object type.
        MeshVertex = 16777216, // A mesh vertex.
        MeshEdge = 33554432, // A mesh edge.
        MeshFace = 67108864, // A mesh face.
        Cage = 134217728, // A cage.
        Phantom = 268435456, // A phantom object.
        ClipPlane = 536870912, // A clipping plane.
        Extrusion = 1073741824, // An extrusion.
        AnyObject = 4294967295, // All bits set.
        SubD = 262144 // A SubD object.
    }
}
