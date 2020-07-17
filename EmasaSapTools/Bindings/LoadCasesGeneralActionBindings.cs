using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Forms;
using Prism.Commands;
using Sap2000Library;
using Sap2000Library.Other;
using Sap2000Library.SapObjects;

namespace EmasaSapTools.Bindings
{
    public class LoadCasesGeneralActionBindings : BindableSingleton<LoadCasesGeneralActionBindings>
    {
        private LoadCasesGeneralActionBindings()
        {
        }

        public override void SetOrReset()
        {
            IntermediateNumberOfSteps = 10;
        }

        private int _intermediateNumberOfSteps;
        public int IntermediateNumberOfSteps { get => _intermediateNumberOfSteps; set => SetProperty(ref _intermediateNumberOfSteps, value); }

        private DelegateCommand _addIntermediateStepsToCasesInClipboardButtonCommand;
        public DelegateCommand AddIntermediateStepsToCasesInClipboardButtonCommand =>
            _addIntermediateStepsToCasesInClipboardButtonCommand ?? (_addIntermediateStepsToCasesInClipboardButtonCommand = new DelegateCommand(ExecuteAddIntermediateStepsToCasesInClipboardButtonCommand));
        public async void ExecuteAddIntermediateStepsToCasesInClipboardButtonCommand()
        {
            StringBuilder endMessages = new StringBuilder();

            try
            {
                // First, reads the clipboard
                string clipText = Clipboard.GetText();

                if (string.IsNullOrWhiteSpace(clipText))
                {
                    OnMessage("Check the Clipboard", "The clipboard does not contain any text.");
                    return;
                }

                List<string> clipVals = clipText.Split(new string[] { "\t", "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (clipVals.Count == 0)
                {
                    OnMessage("Check the Clipboard", "Could not get a list of names from the Clipboard's text. Are they in different lines?");
                    return;
                }

                OnBeginCommand();

                void lf_Work()
                {
                    BusyOverlayBindings.I.SetDeterminate("Changing Number of Intermediate Steps", "Load Case");

                    for (int i = 0; i < clipVals.Count; i++)
                    {
                        string currItem = clipVals[i];
                        BusyOverlayBindings.I.UpdateProgress(i, clipVals.Count, currItem);

                        // Finds the load case
                        LCNonLinear loadCase = null;
                        try
                        {
                            loadCase = S2KModel.SM.LCMan.GetNonLinearStaticLoadCaseByName(currItem);
                        }
                        catch
                        {
                            endMessages.AppendLine($"{currItem}\tCould not find non-linear case with this name.");
                            continue;
                        }

                        if (loadCase.NLSubType == LCNonLinear_SubType.StagedConstruction)
                        {
                            endMessages.AppendLine($"{currItem}\tStaged Construction steps are currently *not* supported.");
                            continue;
                        }

                        try
                        {
                            if (loadCase.NLSubType == LCNonLinear_SubType.Nonlinear)
                                S2KModel.SM.LCMan.UpdateNLResultsSavedNL(loadCase, new LCNonLinear_ResultsSavedNL()
                                {
                                    MinSavedStates = IntermediateNumberOfSteps,
                                    MaxSavedStates = IntermediateNumberOfSteps,
                                    PositiveOnly = true,
                                    SaveMultipleSteps = true
                                });
                        }
                        catch
                        {
                            endMessages.AppendLine($"{currItem}\tCould not set the intermediate steps for case.");
                            continue;
                        }
                    }
                }

                // Runs the job async
                Task task = new Task(lf_Work);
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                ExceptionViewer.Show(ex, "Could not set the intermediate steps.");
            }
            finally
            {
                OnEndCommand();
                // Messages to send?
                if (endMessages.Length != 0)
                    OnMessage("Could not set the intermediate steps", endMessages.ToString());
            }
        }

    }
}