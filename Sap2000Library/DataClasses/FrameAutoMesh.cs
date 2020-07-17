namespace Sap2000Library.DataClasses
{
    public class FrameAutoMesh
    {
        public bool AutoMesh = true;
        public bool AutoMeshAtIntermdiateJoints = true;
        public bool AutoMeshAtIntersections = false;
        public int MinimumSegments = 0;
        public double AutoMeshMaxLength = 0d;

    }
}
