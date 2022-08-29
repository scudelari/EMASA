using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using TSDatatype = Tekla.Structures.Datatype;
using TSModel = Tekla.Structures.Model;
using TSPlugins = Tekla.Structures.Plugins;
using TSGeom = Tekla.Structures.Geometry3d;
using System.Data;
using System.Collections;
using System.IO;
using Microsoft.Win32;
using Tekla.Structures.Model;

namespace TeklaPluginInOutWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        public MainWindow(TSModel.Model inModel) : this()
        {
            _model = inModel;
        }

        // Enable inserting of objects in a model
        private TSModel.Model _model;
        public TSModel.Model Model
        {
            get { return _model; }
        }

        private void OutputButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsEnabled = false;

                // Creates the Dataset
                DataSet ds = new DataSet("TeklaData");

                #region BEAMS
                // Creates the beam table
                DataTable beamTable = ds.Tables.Add("MyBeamTable");
                beamTable.Columns.Add("MyBeamId", typeof(int));
                beamTable.Columns.Add("Class", typeof(string));
                beamTable.Columns.Add("StartPoint", typeof(string));
                beamTable.Columns.Add("StartPointOffset", typeof(string));
                beamTable.Columns.Add("EndPoint", typeof(string));
                beamTable.Columns.Add("EndPointOffset", typeof(string));
                beamTable.Columns.Add("Finish", typeof(string));
                beamTable.Columns.Add("Name", typeof(string));
                beamTable.Columns.Add("Position", typeof(string));
                beamTable.Columns.Add("Profile", typeof(string));
                beamTable.Columns.Add("Material", typeof(string));
                beamTable.Columns.Add("FabricationCode", typeof(string));
                beamTable.Columns.Add("ErectionCode", typeof(string));
                beamTable.Columns.Add("ErectionComment", typeof(string));
                beamTable.Columns.Add("Guid", typeof(string));

                // Gets all beams and iterates
                int beamCounter = 0;
                TSModel.ModelObjectEnumerator allBeams = Model.GetModelObjectSelector().GetAllObjectsWithType(TSModel.ModelObject.ModelObjectEnum.BEAM);
                foreach (TSModel.Beam bm in allBeams)
                {
                    DataRow bmRow = beamTable.NewRow();

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
                    bmRow.SetField("Guid", bm.Identifier.GUID.ToString());

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

                    beamTable.Rows.Add(bmRow);

                    beamCounter++;
                    //if (counter > 20) break;
                }

                beamTable.AcceptChanges();
                #endregion

                #region PLATES
                // Creates the Plate table
                DataTable plateTable = ds.Tables.Add("MyPlateTable");
                plateTable.Columns.Add("MyPlateId", typeof(int));
                plateTable.Columns.Add("Class", typeof(string));
                plateTable.Columns.Add("Finish", typeof(string));
                plateTable.Columns.Add("Name", typeof(string));
                plateTable.Columns.Add("Position", typeof(string));
                plateTable.Columns.Add("Profile", typeof(string));
                plateTable.Columns.Add("Material", typeof(string));
                plateTable.Columns.Add("FabricationCode", typeof(string));
                plateTable.Columns.Add("ErectionCode", typeof(string));
                plateTable.Columns.Add("ErectionComment", typeof(string));
                plateTable.Columns.Add("ContourPoints", typeof(string));
                plateTable.Columns.Add("Guid", typeof(string));

                int plateCounter = 0;
                TSModel.ModelObjectEnumerator allPlates = Model.GetModelObjectSelector().GetAllObjectsWithType(TSModel.ModelObject.ModelObjectEnum.CONTOURPLATE);
                foreach (TSModel.ContourPlate pl in allPlates)
                {
                    // Gets the userProperties that should have some of the data we want
                    Hashtable plateUserProperties = new Hashtable();
                    pl.GetAllUserProperties(ref plateUserProperties);

                    DataRow plateRow = plateTable.NewRow();

                    plateRow.SetField<int>("MyPlateId", plateCounter);
                    plateRow.SetField("Name", pl.Name);
                    plateRow.SetField("Finish", pl.Finish);
                    plateRow.SetField("Class", pl.Class);
                    plateRow.SetField("Profile", pl.Profile.ProfileString);
                    plateRow.SetField("Material", pl.Material.MaterialString);
                    plateRow.SetField("Position", pl.Position.ToStringCustom());
                    plateRow.SetField("ContourPoints", pl.Contour.ToStringCustom());
                    plateRow.SetField("Guid", pl.Identifier.GUID.ToString());

                    string erectionCode = null;
                    if (pl.GetUserProperty("ERECTION_CODE", ref erectionCode))
                    {
                        plateRow.SetField("ErectionCode", erectionCode);
                    }
                    else
                    {
                        plateRow.SetField("ErectionCode", string.Empty);
                    }

                    string erectionComment = null;
                    if (pl.GetUserProperty("ERECTION_COMMENT", ref erectionComment))
                    {
                        plateRow.SetField("ErectionComment", erectionComment);
                    }
                    else
                    {
                        plateRow.SetField("ErectionComment", string.Empty);
                    }

                    string fabricationCode = null;
                    if (pl.GetUserProperty("FABRICATION_CODE", ref fabricationCode))
                    {
                        plateRow.SetField("FabricationCode", fabricationCode);
                    }
                    else
                    {
                        plateRow.SetField("FabricationCode", string.Empty);
                    }

                    plateTable.Rows.Add(plateRow);

                    plateCounter++;
                    //if (plateCounter > 20) break;
                }

                plateTable.AcceptChanges();
                #endregion

                // Saves the Table to an XML file
                string outPath = System.IO.Path.Combine(Model.GetInfo().ModelPath, Model.GetInfo().ModelName + "_ExtractedData.xml");

                // Deletes the file if it already exists
                using (TextWriter tw = new StreamWriter(outPath, false))
                {
                    ds.WriteXml(tw, XmlWriteMode.WriteSchema);
                }

                IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                IsEnabled = true;
            }
        }

        private void InputButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsEnabled = false;

                // Saves the Table to an XML file
                OpenFileDialog fd = new OpenFileDialog();
                fd.Multiselect = false;
                fd.InitialDirectory = Model.GetInfo().ModelPath;
                fd.Filter = "XML files (*.XML)|*.xml";
                fd.ShowDialog();

                if (string.IsNullOrEmpty(fd.FileName))
                {
                    MessageBox.Show("Select the file with the XML table data");
                    IsEnabled = true;
                }

                // Tries to read back the dataset
                DataSet ds = new DataSet();
                using (TextReader tr = new StreamReader(fd.FileName))
                {
                    ds.ReadXml(tr);
                }

                #region BEAMS
                // Gets the beam Table
                DataTable beamTable = ds.Tables["MyBeamTable"];

                // Iterates the table
                int beamErrorCounter = 0;
                foreach (DataRow row in beamTable.Rows)
                {
                    try
                    {
                        TSModel.Beam newBeam = new TSModel.Beam();

                        int myBeamId = row.Field<int>("MyBeamId");

                        newBeam.StartPoint = ExtensionMethods.PointFromStringCustom(row.Field<string>("StartPoint"));
                        newBeam.StartPointOffset = ExtensionMethods.OffsetFromStringCustom(row.Field<string>("StartPointOffset"));

                        newBeam.EndPoint = ExtensionMethods.PointFromStringCustom(row.Field<string>("EndPoint"));
                        newBeam.EndPointOffset = ExtensionMethods.OffsetFromStringCustom(row.Field<string>("EndPointOffset"));

                        newBeam.Class = row.Field<string>("Class");
                        newBeam.Finish = row.Field<string>("Finish");
                        newBeam.Name = row.Field<string>("Name");

                        newBeam.Position = ExtensionMethods.PositionFromStringCustom(row.Field<string>("Position"));

                        newBeam.Material.MaterialString = row.Field<string>("Material");
                        newBeam.Profile.ProfileString = row.Field<string>("Profile");

                        // Sets the user parameters
                        if (!string.IsNullOrEmpty(row.Field<string>("ErectionCode")))
                        {
                            newBeam.SetUserProperty("ERECTION_CODE", row.Field<string>("ErectionCode"));
                        }
                        if (!string.IsNullOrEmpty(row.Field<string>("ErectionComment")))
                        {
                            newBeam.SetUserProperty("ERECTION_COMMENT", row.Field<string>("ErectionComment"));
                        }
                        if (!string.IsNullOrEmpty(row.Field<string>("FabricationCode")))
                        {
                            newBeam.SetUserProperty("FABRICATION_CODE", row.Field<string>("FabricationCode"));
                        }

                        if (!newBeam.Insert())
                        {
                            throw new InvalidDataException("Could not insert the beam.");
                        }
                    }
                    catch (Exception ex_inner)
                    {
                        MessageBox.Show($"Beam Name: {row.Field<string>("Name")} | MyBeamId: {row.Field<int>("MyBeamId")} | Profile: {row.Field<string>("Profile")} | Class: {row.Field<string>("Class")}{Environment.NewLine}{ex_inner.Message}");
                        beamErrorCounter++;
                        if (beamErrorCounter > 5) break;
                    }
                }
                #endregion

                #region PLATES
                // Gets the beam Table
                DataTable plateTable = ds.Tables["MyPlateTable"];

                // Iterates the table
                int plateErrorCounter = 0;
                foreach (DataRow row in plateTable.Rows)
                {
                    try
                    {
                        TSModel.ContourPlate newCPL = new TSModel.ContourPlate();

                        int myPlateId = row.Field<int>("MyPlateId");

                        newCPL.Class = row.Field<string>("Class");
                        newCPL.Finish = row.Field<string>("Finish");
                        newCPL.Name = row.Field<string>("Name");

                        newCPL.Position = ExtensionMethods.PositionFromStringCustom(row.Field<string>("Position"));

                        newCPL.Material.MaterialString = row.Field<string>("Material");
                        newCPL.Profile.ProfileString = row.Field<string>("Profile");

                        newCPL.Contour = ExtensionMethods.ContourFromStringCustom(row.Field<string>("ContourPoints"));

                        // Sets the user parameters
                        if (!string.IsNullOrEmpty(row.Field<string>("ErectionCode")))
                        {
                            newCPL.SetUserProperty("ERECTION_CODE", row.Field<string>("ErectionCode"));
                        }
                        if (!string.IsNullOrEmpty(row.Field<string>("ErectionComment")))
                        {
                            newCPL.SetUserProperty("ERECTION_COMMENT", row.Field<string>("ErectionComment"));
                        }
                        if (!string.IsNullOrEmpty(row.Field<string>("FabricationCode")))
                        {
                            newCPL.SetUserProperty("FABRICATION_CODE", row.Field<string>("FabricationCode"));
                        }

                        if (!newCPL.Insert())
                        {
                            throw new InvalidDataException("Could not insert the contour plate.");
                        }
                    }
                    catch (Exception ex_inner)
                    {
                        MessageBox.Show($"Plate Name: {row.Field<string>("Name")} | MyPlateId: {row.Field<int>("MyPlateId")} | Profile: {row.Field<string>("Profile")}{Environment.NewLine}{ex_inner.Message}");
                        plateErrorCounter++;
                        if (plateErrorCounter > 5) break;
                    }
                }
                #endregion


                // If there were no errors in the beam creation
                if (beamErrorCounter == 0 && plateErrorCounter == 0) Model.CommitChanges();

                IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                IsEnabled = true;
            }
        }
    }
}
