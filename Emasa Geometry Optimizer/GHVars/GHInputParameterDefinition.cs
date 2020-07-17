extern alias r3dm;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using r3dm::Rhino.Geometry;
using RhinoInterfaceLibrary;

namespace Emasa_Geometry_Optimizer.GHVars
{
    public class GHInputParameterDefinition :BindableBase, IEquatable<GHInputParameterDefinition>
    {
        public GHInputParameterDefinition(string inVarTypeStr, string inName)
        {
            VarTypeStr = inVarTypeStr;
            Name = inName;
        }

        public GHInputParameterDefinition(string inVarTypeStr, string inName, byte[] inMinBlob, byte[] inMaxBlob)
        {
            VarTypeStr = inVarTypeStr;
            Name = inName;
            _minBlob = inMinBlob;
            _maxBlob = inMaxBlob;
        }

        private string _varTypeStr;
        public string VarTypeStr
        {
            get => _varTypeStr;
            set
            {
                if (!Enum.TryParse(value, out InputType eType)) throw new Exception($"Could not get the GH Input ParamDef Type Designated by {value}.");
                SetProperty(ref _varTypeStr, value);
                SetProperty(ref _varType, eType);
            }
        }

        private InputType _varType;
        public InputType VarType
        {
            get => _varType;
            set
            {
                SetProperty(ref _varType, value);
                SetProperty(ref _varTypeStr, value.ToString());
            }
        }

        private byte[] _maxBlob;
        public byte[] MaxBlob
        {
            get => _maxBlob;
            set
            {
                SetProperty(ref _maxBlob, value);

                // Notify others
                RaisePropertyChanged(nameof(Max));
                RaisePropertyChanged(nameof(MaxDisplayStr));
            }
        }

        public object Max
        {
            get => DBHelpers.ByteArrayToObject(_maxBlob);
            set
            {
                SetProperty(ref _maxBlob, DBHelpers.ObjectToByteArray(value));

                // Notify others
                RaisePropertyChanged(nameof(Max));
                RaisePropertyChanged(nameof(MaxDisplayStr));
            }
        }

        public string MaxDisplayStr
        {
            get
            {
                if (Max == null) return "Null";

                switch (VarType)
                {
                    case InputType.Integer:
                        if (Max is int maxvali)
                        {
                            return maxvali.ToString();
                        }
                        else throw new Exception($"The stored value is {Max} but it is not an integer.");

                    case InputType.Double:
                        if (Max is double maxvald)
                        {
                            return maxvald.ToString();
                        }
                        else throw new Exception($"The stored value is {Max} but it is not a double.");

                    case InputType.Point:
                        if (Max is Point3d maxvalp)
                        {
                            return maxvalp.ToString();
                        }
                        else return $"The stored value is {Max} but it is not a Point3d.";

                    default:
                        throw new Exception($"Variable type {VarType} is not supported.");
                }
            }
            set
            {
                switch (VarType)
                {
                    case InputType.Integer:
                        if (int.TryParse(value, out int valAsInt))
                        {
                            Max = valAsInt;
                        }
                        else throw new Exception($"Max value {value} is not valid for the variable type.");
                        break;

                    case InputType.Double:
                        if (double.TryParse(value, out double valAsDbl))
                        {
                            Max = valAsDbl;
                        }
                        else throw new Exception($"Max value {value} is not valid for the variable type.");
                        break;

                    case InputType.Point:
                        if (RhinoStaticMethods.TryParsePoint3d(value, out Point3d valAsPnt))
                        {
                            Max = valAsPnt;
                        }
                        else throw new Exception($"Max value {value} is not valid for the variable type.");
                        break;

                    default:
                        throw new Exception($"Variable type {VarType} is not supported.");
                }
                Sql_SendUpdate();
            }
        }

        private byte[] _minBlob;
        public byte[] MinBlob
        {
            get => _minBlob;
            set
            {
                SetProperty(ref _minBlob, value);

                // Notify others
                RaisePropertyChanged(nameof(Min));
                RaisePropertyChanged(nameof(MinDisplayStr));
            }
        }
        public object Min
        {
            get => DBHelpers.ByteArrayToObject(_minBlob);
            set
            {
                SetProperty(ref _minBlob, DBHelpers.ObjectToByteArray(value));

                // Notify others
                RaisePropertyChanged(nameof(Min));
                RaisePropertyChanged(nameof(MinDisplayStr));
            }
        }
        public string MinDisplayStr
        {
            get
            {
                if (Min == null) return "Null";
                switch (VarType)
                {
                    case InputType.Integer:
                        if (Min is int minvali)
                        {
                            return minvali.ToString();
                        }
                        else throw new Exception($"The stored value is {Min} but it is not an integer.");

                    case InputType.Double:
                        if (Min is double minvald)
                        {
                            return minvald.ToString();
                        }
                        else throw new Exception($"The stored value is {Min} but it is not a double.");

                    case InputType.Point:
                        if (Min is Point3d minvalp)
                        {
                            return minvalp.ToString();
                        }
                        else throw new Exception($"The stored value is {Min} but it is not a Point3d.");

                    default:
                        throw new Exception($"Variable type {VarType} is not supported.");
                }
            }
            set
            {
                //throw new ArgumentException($"Value {value} is not valid. Are you sure the change was validated?");
                switch (VarType)
                {
                    case InputType.Integer:
                        if (int.TryParse(value, out int valAsInt))
                        {
                            Min = valAsInt;
                        }
                        else throw new Exception($"Min value {value} is not valid for the variable type.");
                        break;

                    case InputType.Double:
                        if (double.TryParse(value, out double valAsDbl))
                        {
                            Min = valAsDbl;
                        }
                        else throw new Exception($"Min value {value} is not valid for the variable type.");
                        break;

                    case InputType.Point:
                        if (RhinoStaticMethods.TryParsePoint3d(value, out Point3d valAsPnt))
                        {
                            Min = valAsPnt;
                        }
                        else throw new Exception($"Min value {value} is not valid for the variable type.");
                        break;

                    default:
                        throw new Exception($"Variable type {VarType} is not supported.");
                }

                Sql_SendUpdate();
            }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public SQLiteCommand Sql_InsertCommand(SQLiteConnection inConn)
        {
            const string basicText = "INSERT INTO [GHInputParameterDefinition] (Name,Max,Min,Type) VALUES (@Name,@Max,@Min,@Type)";
            
            SQLiteCommand cmd = inConn.CreateCommand();
            cmd.CommandText = basicText;
            cmd.Parameters.Add("@Name", DbType.String).Value = Name;
            cmd.Parameters.Add("@Type", DbType.String).Value = VarTypeStr;

            object maxDefault = null;
            if (MaxBlob == null)
            {
                switch (VarType)
                {
                    case InputType.Integer:
                        int maxDefI = 100;
                        maxDefault = maxDefI;
                        break;

                    case InputType.Double:
                        double maxDefD = 100d;
                        maxDefault = maxDefD;
                        break;

                    case InputType.Point:
                        Point3d maxDefP = new Point3d(100d, 100d, 100d);
                        maxDefault = maxDefP;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(VarType));
                }

                Max = maxDefault;
            }

            object minDefault = null;
            if (MinBlob == null)
            {
                switch (VarType)
                {
                    case InputType.Integer:
                        int minDefI = 100;
                        minDefault = minDefI;
                        break;

                    case InputType.Double:
                        double minDefD = 100d;
                        minDefault = minDefD;
                        break;

                    case InputType.Point:
                        Point3d minDefP = new Point3d(100d, 100d, 100d);
                        minDefault = minDefP;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(VarType));
                }

                Min = minDefault;
            }

            cmd.Parameters.Add("@Max", DbType.Binary).Value = MaxBlob;
            cmd.Parameters.Add("@Min", DbType.Binary).Value = MinBlob;

            return cmd;
        }

        public void Sql_SendUpdate()
        {
            using (SQLiteConnection conn = new SQLiteConnection(DBHelpers.ConnString.ConnectionString))
            {
                conn.Open();

                SQLiteCommand cmd = new SQLiteCommand()
                    {
                    CommandText = "UPDATE GHInputParameterDefinition SET Max = @Max, Min = @Min WHERE Name = @Name;",
                    Connection = conn
                    };

                cmd.Parameters.Add("@Max", DbType.Binary).Value = MaxBlob;
                cmd.Parameters.Add("@Min", DbType.Binary).Value = MinBlob;
                cmd.Parameters.Add("@Name", DbType.String).Value = Name;
                
                if (cmd.ExecuteNonQuery() == -1) throw new Exception($"Could not update Grasshopper variable definition. Name: {Name}");

                conn.Close();
            }
        }

        public bool Equals(GHInputParameterDefinition other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _varType == other._varType && _name == other._name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GHInputParameterDefinition) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) _varType * 397) ^ _name.GetHashCode();
            }
        }

        public static bool operator ==(GHInputParameterDefinition left, GHInputParameterDefinition right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(GHInputParameterDefinition left, GHInputParameterDefinition right)
        {
            return !Equals(left, right);
        }

        public static double[] GetLowerBoundList(List<GHInputParameterDefinition> InputParams)
        {
            List<double> toRet = new List<double>();

            foreach (GHInputParameterDefinition inputDef in InputParams)
            {
                switch (inputDef.VarType)
                {
                    case InputType.Integer:
                        toRet.Add((double)(int)inputDef.Min);
                        break;

                    case InputType.Double:
                        toRet.Add((double)inputDef.Min);
                        break;

                    case InputType.Point:
                        Point3d pnt = (Point3d) inputDef.Min;
                        toRet.Add(pnt.X);
                        toRet.Add(pnt.Y);
                        toRet.Add(pnt.Z);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return toRet.ToArray();
        }
        public static double[] GetUpperBoundList(List<GHInputParameterDefinition> InputParams)
        {
            List<double> toRet = new List<double>();

            foreach (GHInputParameterDefinition inputDef in InputParams)
            {
                switch (inputDef.VarType)
                {
                    case InputType.Integer:
                        toRet.Add((double)(int)inputDef.Max);
                        break;

                    case InputType.Double:
                        toRet.Add((double)inputDef.Max);
                        break;

                    case InputType.Point:
                        Point3d pnt = (Point3d)inputDef.Max;
                        toRet.Add(pnt.X);
                        toRet.Add(pnt.Y);
                        toRet.Add(pnt.Z);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return toRet.ToArray();
        }
    }
}
