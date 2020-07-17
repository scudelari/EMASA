using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using RhinoInterfaceLibrary;

namespace Emasa_Geometry_Optimizer.GHVars
{
    public class GHOutputParameterDefinition : IEquatable<GHOutputParameterDefinition>
    {
        public GHOutputParameterDefinition(string inVarTypeStr, string inName)
        {
            VarTypeStr = inVarTypeStr;
            Name = inName;
        }

        private string _varTypeStr;
        public string VarTypeStr
        {
            get => _varTypeStr;
            set
            {
                if (!Enum.TryParse(value, out OutputType eType)) throw new Exception($"Could not get the GH Output ParamDef Type Designated by {value}.");
                _varTypeStr = value;
                _varType = eType;
            }
        }

        private OutputType _varType;
        public OutputType VarType
        {
            get => _varType;
            set
            {
                _varType = value;
                _varTypeStr = value.ToString();
            }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public SQLiteCommand Sql_InsertCommand(SQLiteConnection inConn)
        {
            const string basicText = "INSERT INTO [GHOutputParameterDefinition] (Name,Type) VALUES (@Name,@Type)";
            
            SQLiteCommand cmd = inConn.CreateCommand();
            cmd.CommandText = basicText;
            cmd.Parameters.Add("@Name", DbType.String).Value = Name;
            cmd.Parameters.Add("@Type", DbType.String).Value = VarTypeStr;

            return cmd;
        }

        public bool Equals(GHOutputParameterDefinition other)
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
            return Equals((GHOutputParameterDefinition) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) _varType * 397) ^ _name.GetHashCode();
            }
        }

        public static bool operator ==(GHOutputParameterDefinition left, GHOutputParameterDefinition right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(GHOutputParameterDefinition left, GHOutputParameterDefinition right)
        {
            return !Equals(left, right);
        }
    }
}
