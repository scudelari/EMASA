using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Markup.Localizer;
using Sap2000Library;

namespace Sap2000Library.DataClasses
{
    public class JointConstraintDef
    {
        public JointConstraintDef(string inName)
        {
            _name = inName;
        }

        private ConstraintAxisEnum? _axis;
        private string _cSys;
        private bool[] _doF;
        private double? _tolerance;
        private readonly string _name;
        private ConstraintTypeEnum? _constraintType;

        public ConstraintTypeEnum ConstraintType
        {
            get
            {
                if (_constraintType == null)
                {
                    _constraintType = S2KModel.SM.JointConstraintMan.GetConstraintType((Name));
                    if (_constraintType == null) throw new S2KHelperException($"Could not get the type of the joint constraint named {Name}");
                }
                
                return _constraintType.Value;
            }
            set => _constraintType = value;
        }

        public string Name => _name;

        public string CSys
        {
            get
            {
                if (!_constraintType.HasValue) throw new S2KHelperException($"The ConstraintType property must be set first.");

                switch (ConstraintType)
                {
                    case ConstraintTypeEnum.Local:
                        throw new S2KHelperException($"Constraints of type {ConstraintType} do not have {MethodBase.GetCurrentMethod()}");

                    case ConstraintTypeEnum.Equal:
                    case ConstraintTypeEnum.Body:
                    case ConstraintTypeEnum.Line:
                    case ConstraintTypeEnum.Weld:
                        if (string.IsNullOrEmpty(_cSys))
                        {
                            S2KModel.SM.JointConstraintMan.FillDoFs(this);
                        }
                        return _cSys;

                    case ConstraintTypeEnum.Beam:
                    case ConstraintTypeEnum.Diaphragm:
                    case ConstraintTypeEnum.Plate:
                    case ConstraintTypeEnum.Rod:
                        if (string.IsNullOrEmpty(_cSys))
                        {
                            S2KModel.SM.JointConstraintMan.FillAxis(this);
                        }
                        return _cSys;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                switch (ConstraintType)
                {
                    case ConstraintTypeEnum.Beam:
                    case ConstraintTypeEnum.Equal:
                    case ConstraintTypeEnum.Body:
                    case ConstraintTypeEnum.Line:
                    case ConstraintTypeEnum.Diaphragm:
                    case ConstraintTypeEnum.Plate:
                    case ConstraintTypeEnum.Rod:
                    case ConstraintTypeEnum.Weld:
                        _cSys = value;
                        break;

                    case ConstraintTypeEnum.Local:
                        throw new S2KHelperException($"Constraints of type {ConstraintType} do not have {MethodBase.GetCurrentMethod()}");

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool[] DoF
        {
            get
            {
                if (!_constraintType.HasValue) throw new S2KHelperException($"The ConstraintType property must be set first.");

                switch (ConstraintType)
                {
                    case ConstraintTypeEnum.Equal:
                    case ConstraintTypeEnum.Body:
                    case ConstraintTypeEnum.Line:
                    case ConstraintTypeEnum.Local:
                    case ConstraintTypeEnum.Weld:
                        if (_doF == null)
                        {
                            S2KModel.SM.JointConstraintMan.FillDoFs(this);
                        }
                        return _doF;

                    case ConstraintTypeEnum.Beam:
                    case ConstraintTypeEnum.Diaphragm:
                    case ConstraintTypeEnum.Plate:
                    case ConstraintTypeEnum.Rod:
                        throw new S2KHelperException($"Constraints of type {ConstraintType} do not have {MethodBase.GetCurrentMethod()}");

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                switch (ConstraintType)
                {
                    case ConstraintTypeEnum.Equal:
                    case ConstraintTypeEnum.Body:
                    case ConstraintTypeEnum.Line:
                    case ConstraintTypeEnum.Local:
                    case ConstraintTypeEnum.Weld:
                        _doF = value;
                        break;

                    case ConstraintTypeEnum.Beam:
                    case ConstraintTypeEnum.Diaphragm:
                    case ConstraintTypeEnum.Plate:
                    case ConstraintTypeEnum.Rod:
                        throw new S2KHelperException($"Constraints of type {ConstraintType} do not have {MethodBase.GetCurrentMethod()}");

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool U1
        {
            get => DoF[0];
            set => DoF[0] = value;
        }
        public bool U2
        {
            get => DoF[1];
            set => DoF[1] = value;
        }
        public bool U3
        {
            get => DoF[2];
            set => DoF[2] = value;
        }
        public bool R1
        {
            get => DoF[3];
            set => DoF[3] = value;
        }
        public bool R2
        {
            get => DoF[4];
            set => DoF[4] = value;
        }
        public bool R3
        {
            get => DoF[5];
            set => DoF[5] = value;
        }

        public ConstraintAxisEnum Axis
        {
            get
            {
                if (!_constraintType.HasValue) throw new S2KHelperException($"The ConstraintType property must be set first.");

                switch (ConstraintType)
                {
                    case ConstraintTypeEnum.Equal:
                    case ConstraintTypeEnum.Body:
                    case ConstraintTypeEnum.Line:
                    case ConstraintTypeEnum.Local:
                    case ConstraintTypeEnum.Weld:
                        throw new S2KHelperException($"Constraints of type {ConstraintType} do not have {MethodBase.GetCurrentMethod()}");

                    case ConstraintTypeEnum.Beam:
                    case ConstraintTypeEnum.Diaphragm:
                    case ConstraintTypeEnum.Plate:
                    case ConstraintTypeEnum.Rod:
                        if (!_axis.HasValue)
                        {
                            S2KModel.SM.JointConstraintMan.FillAxis(this);
                        }
                    
                        if (_axis != null) return _axis.Value;
                        else throw new S2KHelperException($"Could not get the axis definition of the constraint named {this.Name}.");
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                switch (ConstraintType)
                {
                    case ConstraintTypeEnum.Equal:
                    case ConstraintTypeEnum.Body:
                    case ConstraintTypeEnum.Line:
                    case ConstraintTypeEnum.Local:
                    case ConstraintTypeEnum.Weld:
                        throw new S2KHelperException($"Constraints of type {ConstraintType} do not have {MethodBase.GetCurrentMethod()}");

                    case ConstraintTypeEnum.Beam:
                    case ConstraintTypeEnum.Diaphragm:
                    case ConstraintTypeEnum.Plate:
                    case ConstraintTypeEnum.Rod:
                        _axis = value;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public double Tolerance
        {
            get
            {
                switch (ConstraintType)
                {
                    case ConstraintTypeEnum.Weld:
                        if (!_tolerance.HasValue) S2KModel.SM.JointConstraintMan.FillDoFs(this);
                        if (_tolerance != null) return _tolerance.Value;
                        else throw new S2KHelperException($"Could not get the tolerance of the constraint named {this.Name}.");

                    case ConstraintTypeEnum.Equal:
                    case ConstraintTypeEnum.Body:
                    case ConstraintTypeEnum.Line:
                    case ConstraintTypeEnum.Local:
                    case ConstraintTypeEnum.Beam:
                    case ConstraintTypeEnum.Diaphragm:
                    case ConstraintTypeEnum.Plate:
                    case ConstraintTypeEnum.Rod:
                        throw new S2KHelperException($"Constraints of type {ConstraintType} do not have {MethodBase.GetCurrentMethod()}");

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                switch (ConstraintType)
                {
                    case ConstraintTypeEnum.Weld:
                        _tolerance = value;
                        break;

                    case ConstraintTypeEnum.Equal:
                    case ConstraintTypeEnum.Body:
                    case ConstraintTypeEnum.Line:
                    case ConstraintTypeEnum.Local:
                    case ConstraintTypeEnum.Beam:
                    case ConstraintTypeEnum.Diaphragm:
                    case ConstraintTypeEnum.Plate:
                    case ConstraintTypeEnum.Rod:
                        throw new S2KHelperException($"Constraints of type {ConstraintType} do not have {MethodBase.GetCurrentMethod()}");

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        protected bool Equals(JointConstraintDef other)
        {
            return Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JointConstraintDef) obj);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }
    }
}