using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccordHelper.FEA.Results
{
    public class FeResult_SectionNode
    {
        public int SectionNodeId { get; set; }

        /// <summary>
        /// Principal total strain - 1
        /// </summary>
        private List<double> _eptt1 = new List<double>();
        public double? EPTT1
        {
            get
            {
                if (_eptt1.Count == 0) return null;
                return _eptt1.Average();
            }
        }
        public void AddEPTT1(double inVal) { _eptt1.Add(inVal); }

        /// <summary>
        /// Principal total strain - 2
        /// </summary>
        private List<double> _eptt2 = new List<double>();
        public double? EPTT2
        {
            get
            {
                if (_eptt2.Count == 0) return null;
                return _eptt2.Average();
            }
        }
        public void AddEPTT2(double inVal) { _eptt2.Add(inVal); }

        /// <summary>
        /// Principal total strain - 3
        /// </summary>
        private List<double> _eptt3 = new List<double>();
        public double? EPTT3
        {
            get
            {
                if (_eptt3.Count == 0) return null;
                return _eptt3.Average();
            }
        }
        public void AddEPTT3(double inVal) { _eptt3.Add(inVal); }

        /// <summary>
        /// Principal total strain Intensity
        /// </summary>
        private List<double> _epttINT = new List<double>();
        public double? EPTTINT
        {
            get
            {
                if (_epttINT.Count == 0) return null;
                return _epttINT.Average();
            }
        }
        public void AddEPTTINT(double inVal) { _epttINT.Add(inVal); }

        /// <summary>
        /// Principal total strain Equivalent
        /// </summary>
        private List<double> _epttEQV = new List<double>();
        public double? EPTTEQV
        {
            get
            {
                if (_epttEQV.Count == 0) return null;
                return _epttEQV.Average();
            }
        }
        public void AddEPTTEQV(double inVal) { _epttEQV.Add(inVal); }

        /// <summary>
        /// Principal stresses - 1
        /// </summary>
        private List<double> _s1 = new List<double>();
        public double? S1
        {
            get
            {
                if (_s1.Count == 0) return null;
                return _s1.Average();
            }
        }
        public void AddS1(double inVal) { _s1.Add(inVal); }

        /// <summary>
        /// Principal stresses - 2
        /// </summary>
        private List<double> _s2 = new List<double>();
        public double? S2
        {
            get
            {
                if (_s2.Count == 0) return null;
                return _s2.Average();
            }
        }
        public void AddS2(double inVal) { _s2.Add(inVal); }

        /// <summary>
        /// Principal stresses - 3
        /// </summary>
        private List<double> _s3 = new List<double>();
        public double? S3
        {
            get
            {
                if (_s3.Count == 0) return null;
                return _s3.Average();
            }
        }
        public void AddS3(double inVal) { _s3.Add(inVal); }

        /// <summary>
        /// Principal stresses Intensity
        /// </summary>
        private List<double> _sINT = new List<double>();
        public double? SINT
        {
            get
            {
                if (_sINT.Count == 0) return null;
                return _sINT.Average();
            }
        }
        public void AddSINT(double inVal) { _sINT.Add(inVal); }

        /// <summary>
        /// Principal stresses Equivalent
        /// </summary>
        private List<double> _seqv = new List<double>();
        public double? SEQV
        {
            get
            {
                if (_seqv.Count == 0) return null;
                return _seqv.Average();
            }
        }
        public void AddSEQV(double inVal) { _seqv.Add(inVal); }
    }
}
