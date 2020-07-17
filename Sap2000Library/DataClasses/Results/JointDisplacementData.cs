using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Sap2000Library.Managers;
using Sap2000Library.SapObjects;

namespace Sap2000Library.DataClasses.Results
{
    public class JointDisplacementData
    {
        private ResultManager owner = null;

        internal JointDisplacementData(ResultManager owner) { this.owner = owner; }
        internal JointDisplacementData(string inObj, string inElem, string inLoadCase, string inStepType,
            double inStepNum, double inU1, double inU2, double inU3, double inR1, double inR2, double inR3, ResultManager owner)
        {
            Obj = inObj;
            Element = inElem;
            LoadCase = inLoadCase;
            StepType = inStepType;
            StepNum = inStepNum;
            U1 = inU1;
            U2 = inU2;
            U3 = inU3;
            R1 = inR1;
            R2 = inR2;
            R3 = inR3;

            this.owner = owner;
        }

        public string Obj { get; set; }
        public string Element { get; set; }
        public string LoadCase { get; set; }
        public string StepType { get; set; }
        public double StepNum { get; set; }
        public double U1 { get; set; }
        public double U2 { get; set; }
        public double U3 { get; set; }
        public double R1 { get; set; }
        public double R2 { get; set; }
        public double R3 { get; set; }

        private SapPoint linkedPoint = null;
        public void LinkToPoint(SapPoint inPoint)
        {
            linkedPoint = inPoint;
        }

        public Point3D OriginalCoordinates
        {
            get
            {
                if (linkedPoint == null) throw new S2KHelperException($"You can only get the original coordinates if the linked point has been linked using the LinkToPoint function");
                return linkedPoint.Point;
            }
        }
        public Vector3D GlobalDelta
        {
            get
            {
                if (!GlobalU1.HasValue || !GlobalU2.HasValue || !GlobalU3.HasValue)
                    throw new S2KHelperException("Before getting the GlobalDelta you must get the global coordinates through the function FillGlobalCoordinates");
                return new Vector3D(GlobalU1.Value, GlobalU2.Value, GlobalU3.Value);
            }
        }
        public Point3D FinalCoordinates
        {
            get
            {
                if (linkedPoint == null) throw new S2KHelperException($"You can only get the final coordinates if the linked point has been linked using the LinkToPoint function");
                if (!GlobalU1.HasValue || !GlobalU2.HasValue || !GlobalU3.HasValue)
                    throw new S2KHelperException("Before getting the final coordinates you must get the global coordinates through the function FillGlobalCoordinates");

                return OriginalCoordinates.AddVector(GlobalDelta);
            }
        }

        public bool FillGlobalCoordinates()
        {
            if (string.IsNullOrWhiteSpace(Obj)) return false;

            // Tries to get the SapPoint
            SapPoint point = null;
            if (linkedPoint != null) point = linkedPoint;
            else point = owner.s2KModel.PointMan.GetByName(Obj);

            if (point == null) return false;

            try
            {
                Vector<double> localDisp = Vector<double>.Build.Dense(3);
                localDisp[0] = U1;
                localDisp[1] = U2;
                localDisp[2] = U3;

                Vector<double> localRot = Vector<double>.Build.Dense(3);
                localRot[0] = R1;
                localRot[1] = R2;
                localRot[2] = R3;

                _transMatrix = point.ToGlobalTransformationMatrix;
                Vector<double> globalDisp = point.ToGlobalTransformationMatrix.Multiply(localDisp);
                Vector<double> globalRot = point.ToGlobalTransformationMatrix.Multiply(localRot);

                GlobalU1 = globalDisp[0];
                GlobalU2 = globalDisp[1];
                GlobalU3 = globalDisp[2];

                GlobalR1 = globalRot[0];
                GlobalR2 = globalRot[1];
                GlobalR3 = globalRot[2];

                return true;
            }
            catch { return false; }
        }

        private Matrix<double> _transMatrix;
        public double? GlobalU1 { get; set; }
        public double? GlobalU2 { get; set; }
        public double? GlobalU3 { get; set; }
        public double? GlobalR1 { get; set; }
        public double? GlobalR2 { get; set; }
        public double? GlobalR3 { get; set; }

        public JointDisplacementData DuplicateDataWithNewObj(string subName)
        {
            JointDisplacementData toRet = new JointDisplacementData(owner);
            return new JointDisplacementData(owner)
            {
                Obj = subName,
                Element = Element,
                LoadCase = LoadCase,
                StepNum = StepNum,
                StepType = StepType,
                U1 = U1,
                U2 = U2,
                U3 = U3,
                R1 = R1,
                R2 = R2,
                R3 = R3,

                GlobalU1 = GlobalU1,
                GlobalU2 = GlobalU2,
                GlobalU3 = GlobalU3,
                GlobalR1 = GlobalR1,
                GlobalR2 = GlobalR2,
                GlobalR3 = GlobalR3
            };
        }

        public override bool Equals(object obj)
        {
            if (obj is JointDisplacementData inObject)
            {
                if (Obj == inObject.Obj &&
                    Element == inObject.Element &&
                    LoadCase == inObject.LoadCase &&
                    StepType == inObject.StepType &&
                    StepNum == inObject.StepNum ) return true;
                else return false;
            }

            return false;
        }
        public override int GetHashCode()
        {
            return (Obj,Element,LoadCase,StepType,StepNum).GetHashCode();
        }

        public string SQLite_InsertStatement
        {
            get     
            {
                return
$@"INSERT INTO JointDisplacement (Obj , Element , LoadCase , StepType , StepNum , 
    U1 , U2 , U3 , R1 , R2 , R3 , 
    GlobalU1 , GlobalU2 , GlobalU3 , GlobalR1 , GlobalR2 , GlobalR3 ,
    TransformationMatrix )
VALUES(
'{Obj ?? "NULL"}','{Element}','{LoadCase}','{StepType}','{StepNum}',
{U1},{U2},{U3},{R1},{R2},{R3},
{GlobalU1 ?? U1},{GlobalU2 ?? U2},{GlobalU3 ?? U3},{GlobalR1 ?? R1},{GlobalR2 ?? R2},{GlobalR3 ?? R3},
'{(_transMatrix != null ? _transMatrix.ToString() : "NULL")}'
);";
            }
        }
        public static string SQLite_CreateTableStatement
        {
            get
            {
                return
@"CREATE TABLE JointDisplacement (
    Obj TEXT, Element TEXT, LoadCase TEXT, StepType TEXT, StepNum TEXT, 
    U1 DOUBLE, U2 DOUBLE, U3 DOUBLE, R1 DOUBLE, R2 DOUBLE, R3 DOUBLE, 
    GlobalU1 DOUBLE, GlobalU2 DOUBLE, GlobalU3 DOUBLE, GlobalR1 DOUBLE, GlobalR2 DOUBLE, GlobalR3 DOUBLE,
    TransformationMatrix TEXT );";
            }
        }
    }
}
