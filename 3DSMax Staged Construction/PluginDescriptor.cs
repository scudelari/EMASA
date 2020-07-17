using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Max;
using Autodesk.Max.Plugins;

namespace _3DSMax_Staged_Construction
{
    public class PluginDescriptor : ClassDesc2
    {
        IGlobal global;
        internal static IClass_ID classId;

        IGlobal _maxGlobal;
        public IGlobal MaxGlobal { get => _maxGlobal; }
        IInterface _maxInterface;
        public IInterface MaxInterface { get => _maxInterface; }
        public PluginDescriptor(IGlobal inGlobal, IInterface inInterface) : base()
        {
            this._maxGlobal = inGlobal;
            this._maxInterface = inInterface;

            classId = this.MaxGlobal.Class_ID.Create(0x5c57086d, 0x16ed7753);
        }

        public override bool IsPublic { get { return true; } }
        public override bool UseOnlyInternalNameForMAXScriptExposure { get { return true; } }
        public override string InternalName { get { return "EMSStagedConstruction"; } }
        public override string ClassName { get { return "Staged Construction Handler"; } }
        public override string Category { get { return "EMASA Plugins"; } }
        
        public override IClass_ID ClassID
        {
            get
            {
                return classId;
            }
        }
        public override SClass_ID SuperClassID { get { return SClass_ID.Utility; } }

        public override object Create(bool loading = false)
        {
            return new StagedConstruction(this);
        }
    }
}
