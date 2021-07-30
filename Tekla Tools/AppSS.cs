using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BaseWPFLibrary.Bindings;
using EmasaTeklaTools;
using Sap2000Library;
using TSModel = Tekla.Structures.Model;
using TSGeom = Tekla.Structures.Geometry3d;

namespace Tekla_Tools
{
    /// <summary>
    /// Application-Wide Singleton Static Class
    /// </summary>
    public class AppSS : BindableSingleton<AppSS>
    {
        private AppSS() { }

        #region Tekla
        // Tekla TeklaModel
        private readonly TSModel.Model _teklaModel = new TSModel.Model();
        public TSModel.Model TeklaModel => _teklaModel;

        private string _teklaModelName;
        public string TeklaModelName
        {
            get => _teklaModelName;
            set => SetProperty(ref _teklaModelName, value);
        }

        private string _teklaModelPath;
        public string TeklaModelPath
        {
            get => _teklaModelPath;
            set => SetProperty(ref _teklaModelPath, value);
        }

        private DataTable _teklaFile_DefinitionTable;
        public DataTable TeklaFile_DefinitionTable
        {
            get => _teklaFile_DefinitionTable;
            set => SetProperty(ref _teklaFile_DefinitionTable, value);
        }
        #endregion

        #region Sap2000
        private string _sapModelName;
        public string SapModelName
        {
            get => _sapModelName;
            set => SetProperty(ref _sapModelName, value);
        }
        private string _sapModelPath;
        public string SapModelPath
        {
            get => _sapModelPath;
            set => SetProperty(ref _sapModelPath, value);
        }
        private int _sapBasedOnTeklaVersion;
        public int SapBasedOnTeklaVersion
        {
            get => _sapBasedOnTeklaVersion;
            set => SetProperty(ref _sapBasedOnTeklaVersion, value);
        }
        #endregion

        private bool _canSelectTeklaVersionForCompare;
        public bool CanSelectTeklaVersionForCompare
        {
            get => _canSelectTeklaVersionForCompare;
            set => SetProperty(ref _canSelectTeklaVersionForCompare, value);
        }

        private string _sqLiteFullFileName;
        public string SqLiteFullFileName
        {
            get => _sqLiteFullFileName;
            set => SetProperty(ref _sqLiteFullFileName, value);
        }


        /// <summary>
        /// This is the Entry Point Constructor
        /// </summary>
        public override void SetOrReset()
        {
            // Attempts to connect to an instance of Tekla
            if (TeklaModel.GetConnectionStatus() == false) throw new Exception($"Tekla version 2020 must be running to open this application.");

            TSModel.ModelInfo info = TeklaModel.GetInfo();
            TeklaModelName = info.ModelName;
            TeklaModelPath = info.ModelPath;

            // Reads definition of the current file Table
            ReadCurrentTeklaDefinitionTable();

            // Attempts to connect to an instance of SAP2000
            try
            {
                // SAP2000 is available
                SapModelName = S2KModel.SM.FileNameWithoutExtension;
                SapModelPath = S2KModel.SM.ModelDir;

                // Gets the Tekla Version that based this Sap2000 file
                Regex teklaVersionRegex = new Regex(@"###TV_(?<tv>\d*)###");
                Match m = teklaVersionRegex.Match(SapModelName);
                if (!m.Success) throw new InvalidOperationException("Could not find the Tekla version that is related to this model.");
                SapBasedOnTeklaVersion = int.Parse(m.Groups["tv"].Value);

                // Points to the existing SQLite file
                SqLiteFullFileName = Path.Combine(SapModelPath, SapModelName + ".sqlite");
                if (!File.Exists(SqLiteFullFileName)) throw new InvalidOperationException($"Could not find the SqLite file. {SqLiteFullFileName}");

                CanSelectTeklaVersionForCompare = false;
            }
            catch (S2KHelperException)
            {
                // SAP2000 is *not* available
                CanSelectTeklaVersionForCompare = true;
            }
        }

        public void ReadCurrentTeklaDefinitionTable()
        {
            // Creates the beam table
            DataTable teklaFrames = new DataTable("TeklaFrames");
            teklaFrames.Columns.Add("MyBeamId", typeof(int));
            teklaFrames.Columns.Add("Class", typeof(string));
            teklaFrames.Columns.Add("StartPoint", typeof(string));
            teklaFrames.Columns.Add("StartPointOffset", typeof(string));
            teklaFrames.Columns.Add("EndPoint", typeof(string));
            teklaFrames.Columns.Add("EndPointOffset", typeof(string));
            teklaFrames.Columns.Add("Finish", typeof(string));
            teklaFrames.Columns.Add("Name", typeof(string));
            teklaFrames.Columns.Add("Position", typeof(string));
            teklaFrames.Columns.Add("Profile", typeof(string));
            teklaFrames.Columns.Add("Material", typeof(string));
            teklaFrames.Columns.Add("FabricationCode", typeof(string));
            teklaFrames.Columns.Add("ErectionCode", typeof(string));
            teklaFrames.Columns.Add("ErectionComment", typeof(string));

            // Gets all beams and iterates
            int beamCounter = 0;
            TSModel.ModelObjectEnumerator allBeams = TeklaModel.GetModelObjectSelector().GetAllObjectsWithType(TSModel.ModelObject.ModelObjectEnum.BEAM);
            foreach (TSModel.Beam bm in allBeams)
            {
                DataRow bmRow = teklaFrames.NewRow();

                bmRow.SetField<int>("MyBeamId", beamCounter);
                bmRow.SetField("Class", bm.Class);
                bmRow.SetField("StartPoint", bm.StartPoint.ToString());
                bmRow.SetField("StartPointOffset", bm.StartPointOffset.ToStringCustom());
                bmRow.SetField("EndPoint", bm.EndPoint.ToString());
                bmRow.SetField("EndPointOffset", bm.EndPointOffset.ToStringCustom());
                bmRow.SetField("Finish", bm.Finish);
                bmRow.SetField("Name", bm.Name);
                bmRow.SetField("Position", bm.Position.ToStringCustom());
                bmRow.SetField("Profile", bm.Profile.ProfileString);
                bmRow.SetField("Material", bm.Material.MaterialString);

                // From the user properties...
                string erectionCode = null;
                if (bm.GetUserProperty("ERECTION_CODE", ref erectionCode))
                {
                    bmRow.SetField("ErectionCode", erectionCode);
                }
                else
                {
                    bmRow.SetField("ErectionCode", string.Empty);
                }

                string erectionComment = null;
                if (bm.GetUserProperty("ERECTION_COMMENT", ref erectionComment))
                {
                    bmRow.SetField("ErectionComment", erectionComment);
                }
                else
                {
                    bmRow.SetField("ErectionComment", string.Empty);
                }

                string fabricationCode = null;
                if (bm.GetUserProperty("FABRICATION_CODE", ref fabricationCode))
                {
                    bmRow.SetField("FabricationCode", fabricationCode);
                }
                else
                {
                    bmRow.SetField("FabricationCode", string.Empty);
                }

                teklaFrames.Rows.Add(bmRow);

                beamCounter++;
                //if (counter > 20) break;
            }

            teklaFrames.AcceptChanges();

            TeklaFile_DefinitionTable = teklaFrames;
        }

        public void BrowseSqLiteForComparison()
        {

        }

        public void MakeNewSap2000()
        {

        }
    }
}
