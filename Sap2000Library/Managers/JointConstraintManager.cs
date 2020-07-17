using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseWPFLibrary.Bindings;
using Sap2000Library.DataClasses;
using Sap2000Library.Other;
using Sap2000Library.SapObjects;
using SAP2000v1;

namespace Sap2000Library.Managers
{
    public class JointConstraintManager : SapManagerBase
    {
        internal JointConstraintManager(S2KModel model) : base(model) { }

        public ConstraintTypeEnum GetConstraintType(string ContraintName)
        {
            eConstraintType sapCteType = eConstraintType.Beam;

            int ret = SapApi.ConstraintDef.GetConstraintType(ContraintName, ref sapCteType);
            if (ret != 0) throw new ArgumentException($"Could not obtain data for the {ContraintName} constraint", nameof(ContraintName));

            return (ConstraintTypeEnum)(int)sapCteType;
        }

        /// <summary>
        /// Adds a new Body Constraint to the Model.
        /// </summary>
        /// <param name="ConstraintName">The name of the Body Contraint</param>
        /// <param name="Values">Value is an array of six booleans that indicate which joint degrees of freedom are included in the constraint. In order, the degrees of freedom addressed in the array are UX, UY, UZ, RX, RY and RZ.</param>
        /// <param name="CSys">The name of the coordinate system in which the constraint is defined.</param>
        /// <returns>True if successfully added. False if failed.</returns>
        public bool SetBodyConstraint(string ConstraintName, bool[] Values, string CSys = null)
        {
            return SapApi.ConstraintDef.SetBody(ConstraintName, ref Values, CSys ?? "Global") == 0;
        }
        /// <summary>
        /// Adds a new Equal Constraint to the Model.
        /// </summary>
        /// <param name="ConstraintName">The name of the Equal Contraint</param>
        /// <param name="Values">Value is an array of six booleans that indicate which joint degrees of freedom are included in the constraint. In order, the degrees of freedom addressed in the array are UX, UY, UZ, RX, RY and RZ.</param>
        /// <param name="CSys">The name of the coordinate system in which the constraint is defined.</param>
        /// <returns>True if successfully added. False if failed.</returns>
        public bool SetEqualConstraint(string ConstraintName, bool[] Values, string CSys = null)
        {
            return SapApi.ConstraintDef.SetEqual(ConstraintName, ref Values, CSys ?? "Global") == 0;
        }
        /// <summary>
        /// Adds a new Local Constraint to the Model.
        /// </summary>
        /// <param name="ConstraintName">The name of the Local Contraint</param>
        /// <param name="Values">Value is an array of six booleans that indicate which joint degrees of freedom are included in the constraint. In order, the degrees of freedom addressed in the array are UX, UY, UZ, RX, RY and RZ.</param>
        /// <returns>True if successfully added. False if failed.</returns>
        public bool SetLocalConstraint(string ConstraintName, bool[] Values)
        {
            return SapApi.ConstraintDef.SetLocal(ConstraintName, ref Values) == 0;
        }

        public bool DeleteConstraint(string ConstraintName)
        {
            return SapApi.ConstraintDef.Delete(ConstraintName) == 0;
        }

        public List<string> GetConstraintList(bool inUpdateInterface = false)
        {
            if (inUpdateInterface) BusyOverlayBindings.I.SetIndeterminate("Getting Constraint Name List.");
            int count = 0;
            string[] names = null;

            int ret = SapApi.ConstraintDef.GetNameList(ref count, ref names);

            if (ret != 0) return null;
            if (ret == 0 && count == 0) return new List<string>();

            return new List<string>(names);
        }

        /// <summary>
        /// Gets the points that have the constraint. This is a *VERY* heavy as it loads the entire point list.
        /// </summary>
        /// <param name="ConstraintName">The name of the constraint.</param>
        /// <returns>The list of points that have the mentioned constraint.</returns>
        public List<SapPoint> GetPointsWithConstraint(string ConstraintName)
        {
            // Get all points
            List<SapPoint> points = s2KModel.PointMan.GetAll();

            List<SapPoint> toReturn = new List<SapPoint>();

            return (from a in points
                    where a.JointConstraintNames.Contains(ConstraintName)
                    select a).ToList();
        }

        internal List<SapPoint> _bufferPointConstraintList;
        public bool ResetPointConstraintBuffer(IProgress<ProgressData> ReportProgress = null)
        {
            try
            {
                if (ReportProgress != null) ReportProgress.Report(ProgressData.SetMessage("Getting All Points for the Point Constraint Buffer.", true));

                //List<SapPoint> allPoints = s2KModel.PointMan.GetAll(ReportProgress);
                List<SapPoint> allPoints = s2KModel.PointMan.GetAll();

                if (ReportProgress != null) ReportProgress.Report(ProgressData.SetMessage("Filling Constraint Information of the Points in the Constraint Buffer.", true));
                for (int i = 0; i < allPoints.Count; i++)
                {
                    SapPoint pnt = allPoints[i];
                    if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(i, allPoints.Count));
                    _ = pnt.JointConstraintNames == null;
                }

                _bufferPointConstraintList = allPoints;

                return true;
            }
            catch (Exception)
            {
                _bufferPointConstraintList = null;
                return false;
            }
        }
        public List<SapPoint> GetPointsWithConstraintFromPointBuffer(string constraintName)
        {
            if (_bufferPointConstraintList == null) throw new S2KHelperException("You must first initialize the buffer.");

            return (from a in _bufferPointConstraintList
                    where a.JointConstraintNames.Contains(constraintName)
                    select a).ToList();
        }

        public ConstraintTypeEnum? GetType(string ConstraintName)
        {
            eConstraintType cType = eConstraintType.Beam;

            if (SapApi.ConstraintDef.GetConstraintType(ConstraintName, ref cType) != 0) return null;

            return (ConstraintTypeEnum)(int)cType;
        }

        internal void FillDoFs(JointConstraintDef inConstraintDef)
        {
            if (inConstraintDef == null) throw new ArgumentNullException(nameof(inConstraintDef));

            int ret;
            string cSys = null;
            bool[] vals = new bool[6];
            double tol = 0d;

            switch (inConstraintDef.ConstraintType)
            {
                case ConstraintTypeEnum.Equal:
                    ret = S2KModel.SM.SapApi.ConstraintDef.GetEqual(inConstraintDef.Name, ref vals, ref cSys);
                    break;
                case ConstraintTypeEnum.Body:
                    ret = S2KModel.SM.SapApi.ConstraintDef.GetBody(inConstraintDef.Name, ref vals, ref cSys);
                    break;
                case ConstraintTypeEnum.Line:
                    ret = S2KModel.SM.SapApi.ConstraintDef.GetLine(inConstraintDef.Name, ref vals, ref cSys);
                    break;
                case ConstraintTypeEnum.Local:
                    ret = S2KModel.SM.SapApi.ConstraintDef.GetLocal(inConstraintDef.Name, ref vals);
                    break;
                case ConstraintTypeEnum.Weld:
                    ret = S2KModel.SM.SapApi.ConstraintDef.GetWeld(inConstraintDef.Name, ref vals, ref tol, ref cSys);
                    break;

                case ConstraintTypeEnum.Beam:
                case ConstraintTypeEnum.Diaphragm:
                case ConstraintTypeEnum.Plate:
                case ConstraintTypeEnum.Rod:
                    throw new S2KHelperException($"Constraints of type {inConstraintDef.ConstraintType} do not have {MethodBase.GetCurrentMethod()}");

                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (ret != 0) throw new S2KHelperException($"Could not get the DoFs of joint constraint named {inConstraintDef.Name}.");

            inConstraintDef.DoF = vals;
            if (inConstraintDef.ConstraintType != ConstraintTypeEnum.Local) inConstraintDef.CSys = cSys;
            if (inConstraintDef.ConstraintType == ConstraintTypeEnum.Weld) inConstraintDef.Tolerance = tol;
        }

        internal void FillAxis(JointConstraintDef inConstraintDef)
        {
            if (inConstraintDef == null) throw new ArgumentNullException(nameof(inConstraintDef));

            int ret;
            eConstraintAxis axis = eConstraintAxis.AutoAxis;
            string cSys = null;

            switch (inConstraintDef.ConstraintType)
            {
                case ConstraintTypeEnum.Equal:
                case ConstraintTypeEnum.Body:
                case ConstraintTypeEnum.Line:
                case ConstraintTypeEnum.Local:
                case ConstraintTypeEnum.Weld:
                    throw new S2KHelperException($"Constraints of type {inConstraintDef.ConstraintType} do not have {MethodBase.GetCurrentMethod()}");

                case ConstraintTypeEnum.Beam:
                    ret = S2KModel.SM.SapApi.ConstraintDef.GetBeam(inConstraintDef.Name, ref axis, ref cSys);
                    break;
                case ConstraintTypeEnum.Diaphragm:
                    ret = S2KModel.SM.SapApi.ConstraintDef.GetDiaphragm(inConstraintDef.Name, ref axis, ref cSys);
                    break;
                case ConstraintTypeEnum.Plate:
                    ret = S2KModel.SM.SapApi.ConstraintDef.GetPlate(inConstraintDef.Name, ref axis, ref cSys);
                    break;
                case ConstraintTypeEnum.Rod:
                    ret = S2KModel.SM.SapApi.ConstraintDef.GetRod(inConstraintDef.Name, ref axis, ref cSys);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (ret != 0) throw new S2KHelperException($"Could not get the Axis of joint constraint named {inConstraintDef.Name}.");

            inConstraintDef.Axis = (ConstraintAxisEnum)(int)axis;
            inConstraintDef.CSys = cSys;
        }

        public JointConstraintDef GetJointConstraintDef(string Name, bool GetAllNow = false)
        {
            JointConstraintDef temp = new JointConstraintDef(Name);

            if (GetAllNow)
            {
                switch (temp.ConstraintType)
                {
                    case ConstraintTypeEnum.Equal:
                    case ConstraintTypeEnum.Body:
                    case ConstraintTypeEnum.Line:
                    case ConstraintTypeEnum.Local:
                    case ConstraintTypeEnum.Weld:
                        FillDoFs(temp);
                        break;

                    case ConstraintTypeEnum.Beam:
                    case ConstraintTypeEnum.Diaphragm:
                    case ConstraintTypeEnum.Plate:
                    case ConstraintTypeEnum.Rod:
                        FillAxis(temp);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
            }

            return temp;
        }
    }
}
