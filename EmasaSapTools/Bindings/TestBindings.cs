using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Castle.Components.DictionaryAdapter;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Events;
using BaseWPFLibrary.Forms;
using MathNet.Spatial.Euclidean;
using Prism.Commands;
using Sap2000Library;
using Sap2000Library.Other;

namespace EmasaSapTools.Bindings
{
    public class TestBindings : BindableSingleton<TestBindings>
    {
        private TestBindings()
        {
        }

        public override void SetOrReset()
        {
        }

        private DelegateCommand _automationTestButtonCommand;

        public DelegateCommand AutomationTestButtonCommand =>
            _automationTestButtonCommand ?? (_automationTestButtonCommand = new DelegateCommand(ExecuteAutomationTestButtonCommand));

        public async void ExecuteAutomationTestButtonCommand()
        {
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    // Set a Title to the Busy Overlay
                    BusyOverlayBindings.I.Title = "Testing Automation Interface";
                    BusyOverlayBindings.I.AutomationWarning_Visibility = Visibility.Visible;

                    EventAggregatorSingleton.I.GetEvent<BindGenericCommandEvent>().Publish(new BindCommandEventArgs(this, "ActivateWindow"));

                    Thread.Sleep(1000);

                    S2KModel.SM.InterAuto.FlaUI_Action_FocusSap2000Window();

                    Thread.Sleep(1000);

                    EventAggregatorSingleton.I.GetEvent<BindGenericCommandEvent>().Publish(new BindCommandEventArgs(this, "ActivateWindow"));

                    Thread.Sleep(1000);

                    //S2KModel.SM.InterAuto.FlaUI_Action_ExportTablesToS2K("Bla.s2k", new List<Sap2000ExportTable>()
                    //        {
                    //        Sap2000ExportTable.Base_Reactions,
                    //        Sap2000ExportTable.Connectivity_MINUS_Frame,
                    //        Sap2000ExportTable.Joint_Coordinates,
                    //        Sap2000ExportTable.Joint_Displacements,
                    //        Sap2000ExportTable.Element_Joint_Forces_MINUS_Frames
                    //        },
                    //    new Sap2000ExportOptions() { LoadCombos = Sap2000ExportOptions.Sap2000OutLoadCombos.Envelopes, MultiStepStaticResults = Sap2000ExportOptions.Sap2000OutResultsOptions.LastStep, NonLinearStaticResults = Sap2000ExportOptions.Sap2000OutResultsOptions.LastStep }
                    //    , inUpdateInterface: true);

                    //S2KModel.SM.InterAuto.FlaUI_Action_Test();

                    //// Focus on the main window

                    //BusyOverlayBindings.I.AutomationWarning_Visibility = Visibility.Collapsed;

                    //// Reads the file
                    //FileInfo fInfo = new FileInfo(Path.Combine(S2KModel.SM.ModelDir, "Bla.s2k"));
                    //if (!fInfo.Exists) throw new S2KHelperException($"File {fInfo.FullName} does not exist!");
                    //string[] allFile = File.ReadAllLines(fInfo.FullName);

                    //List<(Sap2000ExportTable Table, long TableHeaderLine, long TableEndLine)> val = S2KModel.SM.InterAuto.FindAllTablesInS2K(allFile, true);

                    //DataSet set = S2KModel.SM.InterAuto.GetDataSetFromS2K(allFile, true);

                    //S2KModel.SM.InterAuto.FlaUI_Action_ImportTablesFromS2K(Path.Combine(S2KModel.SM.ModelDir, "tempGhostFrame_bb64a46c10719cd8bcde94db4b6ccd14.s2k_temp"));
                }

                // Runs the job async
                Task task = new Task(lf_Work);
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex);
            }
            finally
            {
                OnEndCommand();
            }
        }
    }
}