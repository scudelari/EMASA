using System;
using Sap2000Library.Managers;

namespace Sap2000Library.SapObjects
{
    public class SapCableSection
    {
        private CableSectionManager owner = null;
        internal SapCableSection(string name, CableSectionManager cableSectMan)
        {
            owner = cableSectMan;
            Name = name;
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private double _diameter;
        public double Diameter
        {
            get { return _diameter; }
            set { _diameter = value; }
        }
        public double Area
        {
            get { return Math.PI * (_diameter / 2d) * (_diameter / 2d);  }
            set { _diameter = Math.Sqrt(4 * value / Math.PI); }
        }

        private string _material;
        public string Material
        {
            get { return _material; }
            set { _material = value; }
        }

    }
}
