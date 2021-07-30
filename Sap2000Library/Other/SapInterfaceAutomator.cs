using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Forms;
using BaseWPFLibrary.Others;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Exceptions;
using FlaUI.Core.Identifiers;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using FlaUI.UIA3.Patterns;
using MathNet.Spatial.Euclidean;
using Sap2000Library.Managers;
using TestStack.White.Configuration;
using TestStack.White.UIItems.Finders;
using TestStack.White.UIItems.TableItems;
using TestStack.White.UIItems.WindowStripControls;
using Debug = System.Diagnostics.Debug;
using TS = TestStack.White;
using TS_WAPI = TestStack.White.WindowsAPI;
using TS_UITEM_WI = TestStack.White.UIItems.WindowItems;
using Window = FlaUI.Core.AutomationElements.Window;
using BaseWPFLibrary.Annotations;
using FlaUI.Core.Patterns;
using Point = System.Windows.Point;
using FlaUI.Core.Input;

namespace Sap2000Library.Other
{
    public class SapInterfaceAutomator : SapManagerBase
    {
        TestStack.White.InputDevices.Keyboard kb
        {
            get => TestStack.White.InputDevices.Keyboard.Instance;
        }

        public SapInterfaceAutomator(S2KModel model) : base(model)
        {
            // Configures the timeout
            CoreAppXmlConfiguration.Instance.BusyTimeout = 60000;
        }

        private Process _process = null;

        public Process SapProcess
        {
            get
            {
                try
                {
                    if (_process == null)
                    {
                        int procCount = 0;
                        Process retProcess = null;
                        //string sapModelName = Path.GetFileNameWithoutExtension(this.FullFileName);
                        // Also gets the process running this instance
                        foreach (Process item in Process.GetProcesses())
                        {
                            if (item.ProcessName == "SAP2000")
                            {
                                procCount++;
                                retProcess = item;
                            }
                        }

                        if (procCount != 1) throw new Exception();

                        _process = retProcess;
                    }

                    return _process;
                }
                catch (Exception)
                {
                    throw new InvalidOperationException("Could not find the SAP2000 process for the current model. Remember: Only one instance with the same model name may be running at the same time.");
                }
            }
        }

        private TS::Application _sapApp = null;

        public TS::Application SapApp
        {
            get
            {
                try
                {
                    return _sapApp ?? (_sapApp = TS::Application.Attach(SapProcess));
                }
                catch (Exception ex)
                {
                    throw new S2KHelperException("Could not attach to the SAP2000 Process.", ex);
                }
            }
        }

        TS_UITEM_WI::Window _SapMainWindow = null;

        public TS_UITEM_WI::Window SapMainWindow
        {
            get
            {
                try
                {
                    return _SapMainWindow ?? (_SapMainWindow = SapWindowsList.First(a => a.Title.StartsWith("SAP2000")));
                }
                catch (Exception)
                {
                    throw new S2KHelperException("Could not get the main SAP2000 window.");
                }
            }
        }

        public TS_UITEM_WI::Window GetSapWindowByTitle(string inTitle)
        {
            try
            {
                TS_UITEM_WI::Window toRet = SapWindowsList.First(a => a.Title.StartsWith(inTitle));
                Debug.WriteLine($"Got window called {inTitle}");
                return toRet;
            }
            catch (Exception ex)
            {
                throw new S2KHelperException($"Could not find window named {inTitle} opened in SAP2000.", ex);
            }
        }

        public List<string> SapWindowNames
        {
            get { return SapWindowsList.Select(a => a.Title).ToList(); }
        }

        public List<TS_UITEM_WI::Window> SapWindowsList
        {
            get
            {
                try
                {
                    //List < TS_UITEM_WI::Window > temp = this.SapApp.GetWindows();
                    Debug.WriteLine("SAP2000 Auto: Begin Get Window List");
                    var bla = SapApp.GetWindows();
                    Debug.WriteLine("SAP2000 Auto: End Get Window List");
                    return bla;
                }
                catch (Exception ex)
                {
                    throw new S2KHelperException("Could not get SAP2000 window list.", ex);
                }
            }
        }

        public bool SAP2000HasOtherWindows
        {
            get { return SapWindowsList.Count > 1; }
        }


        [Obsolete]
        public void Action_CloseAllOtherWindows(CancellationToken cancelToken)
        {
            throw new InvalidOperationException("The Action_CloseAllOtherWindows is deprecated - it doesn't work.");
        }

        public void PInvoke_MakeSap2000ActiveWindow(bool inUpdateInterface = false)
        {
            try
            {
                if (inUpdateInterface) BusyOverlayBindings.I.SetIndeterminate("SAP2000 Automation: Activating SAP2000 Window.");

                Stopwatch watch = Stopwatch.StartNew();
                PInvokeWrappers.SetForegroundWindow(SapProcess.MainWindowHandle);
                watch.Stop();
                Debug.WriteLine($"Took {watch.ElapsedMilliseconds} ms to focus SAP2000's main window using PInvoke.");
            }
            catch (Exception ex)
            {
                throw new S2KHelperException("Could not make main SAP2000 window active.", ex);
            }
        }

        public void Action_ClearAllLoadCasesAndCombos()
        {
            try
            {
                SapMainWindow.Keyboard.HoldKey(TS_WAPI.KeyboardInput.SpecialKeys.CONTROL);
                SapMainWindow.Keyboard.Enter("e");
                kb.LeaveAllKeys();

                TS_UITEM_WI::Window tableSelectWindow = GetSapWindowByTitle("Choose Tables for Interactive Editing");
                tableSelectWindow.Focus();
                SapMainWindow.Focus();

                // Gets the treeview
                TS.UIItems.TreeItems.Tree tree = tableSelectWindow.Get<TS.UIItems.TreeItems.Tree>("TreeView1");

                // The "expose all input tables" must be selected to have the right tree size
                TS.UIItems.CheckBox chkAllInputTables = tableSelectWindow.Get<TS.UIItems.CheckBox>("chkEnable");
                if (!chkAllInputTables.Checked)
                {
                    chkAllInputTables.Checked = true;
                    tableSelectWindow.WaitWhileBusy();
                }

                // Effectivelly deselects all tables
                Point newPos = new Point(tree.Location.X + 25, tree.Location.Y + 10);
                TestStack.White.InputDevices.Mouse.Instance.Location = newPos;

                TestStack.White.InputDevices.Mouse.Instance.Click();
                tableSelectWindow.WaitWhileBusy();

                TS.UIItems.Button bntOk = tableSelectWindow.Get<TS.UIItems.Button>("cmdOK");
                if (bntOk.Enabled)
                {
                    TestStack.White.InputDevices.Mouse.Instance.Click(); // It means that the selection is for all and therefore it must clear it
                    tableSelectWindow.WaitWhileBusy();
                }

                // Opens the Load Case Items
                newPos = new Point(tree.Location.X + 26, tree.Location.Y + 90);
                TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
                TestStack.White.InputDevices.Mouse.Instance.Click();
                tableSelectWindow.WaitWhileBusy();

                // Selects the Load Case Definition Table
                newPos = new Point(tree.Location.X + 59, tree.Location.Y + 107);
                TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
                TestStack.White.InputDevices.Mouse.Instance.Click();
                tableSelectWindow.WaitWhileBusy();

                // Opens the "Load Pattern Definitions"
                newPos = new Point(tree.Location.X + 27, tree.Location.Y + 58);
                TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
                TestStack.White.InputDevices.Mouse.Instance.Click();
                tableSelectWindow.WaitWhileBusy();


                // Selects the "Response Combinations Items"
                newPos = new Point(tree.Location.X + 58, tree.Location.Y + 170);
                TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
                TestStack.White.InputDevices.Mouse.Instance.Click();
                tableSelectWindow.WaitWhileBusy();

                // Clicks the button!
                bntOk.Click();

                // Gets the new window
                TS_UITEM_WI::Window tableDataWindow = GetSapWindowByTitle("Interactive Database Editing");
                tableDataWindow.Focus();
                SapMainWindow.Focus();

                // Gets a reference to the toolbar and to the button
                ToolStrip strip = tableDataWindow.Get<ToolStrip>("ToolStrip1");
                TS.UIItems.Button delSelectedLines = strip.Get<TS.UIItems.Button>(SearchCriteria.ByText("Delete Row"));

                // Gets the grid
                Table grid = tableDataWindow.Get<Table>("datGrid");

                // Selects the "Combination Definitions" in the TS.UIItems.ListBoxItems.ComboBox

                TS.UIItems.ListBoxItems.ComboBox tableSelectComboBox = tableDataWindow.Get<TS.UIItems.ListBoxItems.ComboBox>("cboType");
                tableSelectComboBox.Select("Combination Definitions");
                tableSelectWindow.WaitWhileBusy();

                // Selects all data in the DataTable
                newPos = new Point(grid.Location.X + 5, grid.Location.Y + 5);
                TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
                TestStack.White.InputDevices.Mouse.Instance.Click();
                tableSelectWindow.WaitWhileBusy();

                // Clicks Delete
                delSelectedLines.Click();

                // Selects the "Load Case Definitions" in the TS.UIItems.ListBoxItems.ComboBox
                tableSelectComboBox.Select("Load Case Definitions");
                tableSelectWindow.WaitWhileBusy();

                // Selects all data in the DataTable
                newPos = new Point(grid.Location.X + 5, grid.Location.Y + 5);
                TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
                TestStack.White.InputDevices.Mouse.Instance.Click();
                tableSelectWindow.WaitWhileBusy();

                // Clicks Delete
                delSelectedLines.Click();

                TS.UIItems.Button bntDone = tableDataWindow.Get<TS.UIItems.Button>("cmdDone");
                bntDone.Click();

                // We have a confirmation dialog
                TS_UITEM_WI::Window sap2000Dialog = null;
                try
                {
                    sap2000Dialog = GetSapWindowByTitle("SAP2000");
                }
                catch (Exception)
                {
                }

                if (sap2000Dialog != null)
                {
                    // Gets the Yes button
                    TS.UIItems.Button bntYes = sap2000Dialog.Get<TS.UIItems.Button>("6");
                    bntYes.Click();
                }

                tableDataWindow.WaitWhileBusy();

                TS_UITEM_WI::Window sap2000DataBaseLogWindow = null;
                try
                {
                    sap2000DataBaseLogWindow = GetSapWindowByTitle("Interactive Database Import Log");
                }
                catch (Exception)
                {
                }

                if (sap2000DataBaseLogWindow != null)
                {
                    // Gets the Done button
                    TS.UIItems.Button bntDone1 = sap2000DataBaseLogWindow.Get<TS.UIItems.Button>("cmdDone");
                    bntDone1.Click();
                }

                // Waits until the main window is finished.
                SapMainWindow.Focus();
                SapMainWindow.WaitWhileBusy();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Action_SendDeleteKey()
        {
            SapMainWindow.Keyboard.PressSpecialKey(TS_WAPI.KeyboardInput.SpecialKeys.DELETE);
        }

        public void Action_ExportFrameDefinitionsToAccess(CancellationToken cancelToken, string accessFileName)
        {
            string accessFullPath = Path.Combine(s2KModel.ModelDir, accessFileName);

            if (File.Exists(accessFullPath)) File.Delete(accessFullPath);

            if (cancelToken.IsCancellationRequested) return;


            // Opens export to access window
            SapMainWindow.Keyboard.HoldKey(TS_WAPI.KeyboardInput.SpecialKeys.ALT);
            SapMainWindow.Keyboard.Enter("f");
            kb.LeaveAllKeys();
            SapMainWindow.Keyboard.Enter("e");
            SapMainWindow.Keyboard.Enter("a");

            if (cancelToken.IsCancellationRequested) return;

            TS_UITEM_WI::Window tableSelectWindow = GetSapWindowByTitle("Choose Tables for Export to Access");
            tableSelectWindow.Focus();
            SapMainWindow.Focus();

            if (cancelToken.IsCancellationRequested) return;

            // Gets the treeview
            TS.UIItems.TreeItems.Tree tree = tableSelectWindow.Get<TS.UIItems.TreeItems.Tree>("TreeView1");

            // The "expose all input tables" must be selected to have the right tree size
            TS.UIItems.CheckBox chkAllInputTables = tableSelectWindow.Get<TS.UIItems.CheckBox>("chkEnable");
            if (!chkAllInputTables.Checked)
            {
                chkAllInputTables.Checked = true;
                tableSelectWindow.WaitWhileBusy();
            }

            // Effectivelly deselects all tables
            Point newPos = new Point(tree.Location.X + 25, tree.Location.Y + 10);
            TestStack.White.InputDevices.Mouse.Instance.Location = newPos;

            TestStack.White.InputDevices.Mouse.Instance.Click();
            tableSelectWindow.WaitWhileBusy();

            TS.UIItems.Button bntOk = tableSelectWindow.Get<TS.UIItems.Button>("cmdOK");
            if (bntOk.Enabled)
            {
                TestStack.White.InputDevices.Mouse.Instance.Click(); // It means that the selection is for all and therefore it must clear it
                tableSelectWindow.WaitWhileBusy();
            }

            #region Selects the desired tables

            // Opens the Frame Assingments Group
            newPos = new Point(tree.Location.X + 29, tree.Location.Y + 156);
            TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
            TestStack.White.InputDevices.Mouse.Instance.Click();
            tableSelectWindow.WaitWhileBusy();

            // Opens the Frame Item Assigns
            newPos = new Point(tree.Location.X + 43, tree.Location.Y + 171);
            TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
            TestStack.White.InputDevices.Mouse.Instance.Click();
            tableSelectWindow.WaitWhileBusy();

            int[] tablesToSelect = new int[] { 42, 75, 92, 107, 125, 138, 157, 171, 203, 493, 539 };
            foreach (int num in tablesToSelect)
            {
                newPos = new Point(tree.Location.X + 75, tree.Location.Y + num);
                TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
                TestStack.White.InputDevices.Mouse.Instance.Click();
                tableSelectWindow.WaitWhileBusy();
            }

            // Closes the Frame Item Assigns 
            newPos = new Point(tree.Location.X + 43, tree.Location.Y + 12);
            TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
            TestStack.White.InputDevices.Mouse.Instance.Click();
            tableSelectWindow.WaitWhileBusy();

            // Opens the Connectivity Data
            newPos = new Point(tree.Location.X + 27, tree.Location.Y + 122);
            TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
            TestStack.White.InputDevices.Mouse.Instance.Click();
            tableSelectWindow.WaitWhileBusy();

            // Opens the Object Connectivity
            newPos = new Point(tree.Location.X + 43, tree.Location.Y + 156);
            TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
            TestStack.White.InputDevices.Mouse.Instance.Click();
            tableSelectWindow.WaitWhileBusy();

            // Selects the Connectivity - Frame table
            newPos = new Point(tree.Location.X + 73, tree.Location.Y + 169);
            TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
            TestStack.White.InputDevices.Mouse.Instance.Click();
            tableSelectWindow.WaitWhileBusy();

            #endregion

            if (cancelToken.IsCancellationRequested) return;

            // Clicks the button!
            bntOk.Click();

            if (cancelToken.IsCancellationRequested) return;

            // Gets the save as dialog
            TS_UITEM_WI::Window saveAsWindow = GetSapWindowByTitle("Save Access Database File As");
            saveAsWindow.Focus();
            SapMainWindow.Focus();

            SapMainWindow.Keyboard.Enter(accessFullPath.Replace("\\\\", "\\"));

            // Saves
            SapMainWindow.Keyboard.HoldKey(TS_WAPI.KeyboardInput.SpecialKeys.ALT);
            SapMainWindow.Keyboard.Enter("s");
            kb.LeaveAllKeys();

            // Waits for SAP2000 to finish
            TS.UIItems.Label statusLabel = SapMainWindow.Get<TS.UIItems.Label>("lblStatus");
            while (statusLabel.Text.Trim() != "Ready" && statusLabel.Text.Trim() != "3-D View")
            {
                Thread.Sleep(500);
            }
        }

        public void Action_ImportFrameDefinitionsFromAccess(CancellationToken cancelToken, string accessFileName)
        {
            string accessFullPath = Path.Combine(s2KModel.ModelDir, accessFileName);

            if (!File.Exists(accessFullPath)) throw new S2KHelperException("The Access Filename does not exists");

            if (cancelToken.IsCancellationRequested) return;

            if (SAP2000HasOtherWindows) throw new S2KHelperException("You must close all SAP2000 dialogs before proceeding.");

            // Opens export to access window
            SapMainWindow.Keyboard.HoldKey(TS_WAPI.KeyboardInput.SpecialKeys.ALT);
            SapMainWindow.Keyboard.Enter("f");
            kb.LeaveAllKeys();
            SapMainWindow.Keyboard.Enter("i");
            SapMainWindow.Keyboard.Enter("a");

            if (cancelToken.IsCancellationRequested) return;

            TS_UITEM_WI::Window importBaseWindow = GetSapWindowByTitle("Import Tabular Database");
            importBaseWindow.Focus();
            SapMainWindow.Focus();

            if (cancelToken.IsCancellationRequested) return;

            // Gets the radiobutton
            TS.UIItems.RadioButton addExisting = importBaseWindow.Get<TS.UIItems.RadioButton>("optType1");
            addExisting.Select();

            if (cancelToken.IsCancellationRequested) return;

            TS.UIItems.Button bntAdvanced = importBaseWindow.Get<TS.UIItems.Button>("cmdAdvanced");
            bntAdvanced.Click();

            if (cancelToken.IsCancellationRequested) return;

            TS_UITEM_WI::Window importAdvancedWindow = GetSapWindowByTitle("Tabular Database Import Options");
            importAdvancedWindow.Focus();
            SapMainWindow.Focus();

            if (cancelToken.IsCancellationRequested) return;

            TS.UIItems.Button bntResetAllDefs = importAdvancedWindow.Get<TS.UIItems.Button>("cmdDefaultsAll");
            bntResetAllDefs.Click();

            //TS.UIItems.CheckBox chkDisplayError = importAdvancedWindow.Get<TS.UIItems.CheckBox>("chkDisplay1");
            //TS.UIItems.CheckBox chkDisplayWarning = importAdvancedWindow.Get<TS.UIItems.CheckBox>("chkDisplay2");

            TS.UIItems.RadioButton neverAbortError = importAdvancedWindow.Get<TS.UIItems.RadioButton>("optError1");
            neverAbortError.Select();

            TS.UIItems.RadioButton neverAbortWarning = importAdvancedWindow.Get<TS.UIItems.RadioButton>("optWarning1");
            neverAbortWarning.Select();

            // Goes to the other tab
            TS.UIItems.TabItems.Tab dialogTab = importAdvancedWindow.Get<TS.UIItems.TabItems.Tab>("TabControl1");
            dialogTab.SelectTabPage(2);

            TS.UIItems.RadioButton allowDuplicatesFrames = importAdvancedWindow.Get<TS.UIItems.RadioButton>("optLocation1");
            allowDuplicatesFrames.Select();

            TS.UIItems.RadioButton allowDuplicatesLinks = importAdvancedWindow.Get<TS.UIItems.RadioButton>("optLocationLink1");
            allowDuplicatesLinks.Select();

            if (cancelToken.IsCancellationRequested) return;

            TS.UIItems.Button bntOKAdvanced = importAdvancedWindow.Get<TS.UIItems.Button>("cmdOK");
            bntOKAdvanced.Click();

            if (cancelToken.IsCancellationRequested) return;

            TS.UIItems.Button bntOKBase = importBaseWindow.Get<TS.UIItems.Button>("cmdOK");
            bntOKBase.Click();

            if (cancelToken.IsCancellationRequested) return;

            // Gets the file select window
            TS_UITEM_WI::Window saveAsWindow = GetSapWindowByTitle("Import Microsoft Access Database");
            saveAsWindow.Focus();
            SapMainWindow.Focus();

            SapMainWindow.Keyboard.Enter(accessFullPath.Replace("\\\\", "\\"));

            // Opens
            SapMainWindow.Keyboard.HoldKey(TS_WAPI.KeyboardInput.SpecialKeys.ALT);
            SapMainWindow.Keyboard.Enter("o");
            kb.LeaveAllKeys();

            // Pools until we get the result window

            // Waits for SAP2000 to finish
            TS.UIItems.Label statusLabel = SapMainWindow.Get<TS.UIItems.Label>("lblStatus");
            while (statusLabel.Text.Trim() != "Ready" && statusLabel.Text.Trim() != "3-D View")
            {
                Thread.Sleep(500);
            }
        }

        public void Action_ExportFrameDefinitionsToS2KTextFile(string textFileName, List<FrameAssignmentTable> tablesInFrameAssignment = null)
        {
            string accessFullPath = Path.Combine(s2KModel.ModelDir, textFileName);

            if (File.Exists(accessFullPath)) File.Delete(accessFullPath);

            if (SAP2000HasOtherWindows) throw new S2KHelperException("You must close all SAP2000 dialogs before proceeding.");

            // Opens export to access window
            SapMainWindow.Keyboard.HoldKey(TS_WAPI.KeyboardInput.SpecialKeys.ALT);
            SapMainWindow.Keyboard.Enter("f");
            kb.LeaveAllKeys();
            SapMainWindow.Keyboard.Enter("e");
            SapMainWindow.Keyboard.Enter("t");

            TS_UITEM_WI::Window tableSelectWindow = GetSapWindowByTitle("Choose Tables for Export to Text File");
            tableSelectWindow.Focus();
            SapMainWindow.Focus();

            // Gets the treeview
            TS.UIItems.TreeItems.Tree tree = tableSelectWindow.Get<TS.UIItems.TreeItems.Tree>("TreeView1");

            // The "expose all input tables" must be selected to have the right tree size
            TS.UIItems.CheckBox chkAllInputTables = tableSelectWindow.Get<TS.UIItems.CheckBox>("chkEnable");
            if (!chkAllInputTables.Checked)
            {
                chkAllInputTables.Checked = true;
                tableSelectWindow.WaitWhileBusy();
            }

            // Effectivelly deselects all tables
            Point newPos = new Point(tree.Location.X + 25, tree.Location.Y + 10);
            TestStack.White.InputDevices.Mouse.Instance.Location = newPos;

            TestStack.White.InputDevices.Mouse.Instance.Click();
            tableSelectWindow.WaitWhileBusy();

            TS.UIItems.Button bntOk = tableSelectWindow.Get<TS.UIItems.Button>("cmdOK");
            if (bntOk.Enabled)
            {
                TestStack.White.InputDevices.Mouse.Instance.Click(); // It means that the selection is for all and therefore it must clear it
                tableSelectWindow.WaitWhileBusy();
            }

            #region Selects the desired tables

            if (tablesInFrameAssignment != null && tablesInFrameAssignment.Count > 0)
            {
                // Opens the Frame Assingments Group
                newPos = new Point(tree.Location.X + 29, tree.Location.Y + 156);
                TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
                TestStack.White.InputDevices.Mouse.Instance.Click();
                tableSelectWindow.WaitWhileBusy();

                // Opens the Frame Item Assigns
                newPos = new Point(tree.Location.X + 43, tree.Location.Y + 171);
                TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
                TestStack.White.InputDevices.Mouse.Instance.Click();
                tableSelectWindow.WaitWhileBusy();

                foreach (FrameAssignmentTable table in tablesInFrameAssignment)
                {
                    newPos = new Point(tree.Location.X + 75, tree.Location.Y + (int)table);
                    TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
                    TestStack.White.InputDevices.Mouse.Instance.Click();
                    tableSelectWindow.WaitWhileBusy();
                }

                // Closes the Frame Item Assigns 
                newPos = new Point(tree.Location.X + 43, tree.Location.Y + 12);
                TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
                TestStack.White.InputDevices.Mouse.Instance.Click();
                tableSelectWindow.WaitWhileBusy();
            }

            // Opens the Connectivity Data
            newPos = new Point(tree.Location.X + 27, tree.Location.Y + 122);
            TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
            TestStack.White.InputDevices.Mouse.Instance.Click();
            tableSelectWindow.WaitWhileBusy();

            // Opens the Object Connectivity
            newPos = new Point(tree.Location.X + 43, tree.Location.Y + 156);
            TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
            TestStack.White.InputDevices.Mouse.Instance.Click();
            tableSelectWindow.WaitWhileBusy();

            // Selects the Connectivity - Frame table
            newPos = new Point(tree.Location.X + 73, tree.Location.Y + 169);
            TestStack.White.InputDevices.Mouse.Instance.Location = newPos;
            TestStack.White.InputDevices.Mouse.Instance.Click();
            tableSelectWindow.WaitWhileBusy();

            #endregion

            // Clicks the button!
            bntOk.Click();

            // Gets the save as dialog
            TS_UITEM_WI::Window saveAsWindow = GetSapWindowByTitle("Save Text File As");
            saveAsWindow.Focus();
            SapMainWindow.Focus();

            SapMainWindow.Keyboard.Enter(accessFullPath.Replace("\\\\", "\\"));

            // Saves
            SapMainWindow.Keyboard.HoldKey(TS_WAPI.KeyboardInput.SpecialKeys.ALT);
            SapMainWindow.Keyboard.Enter("s");
            kb.LeaveAllKeys();

            // Waits for SAP2000 to finish
            TS.UIItems.Label statusLabel = SapMainWindow.Get<TS.UIItems.Label>("lblStatus");
            while (statusLabel.Text.Trim() != "Ready" && statusLabel.Text.Trim() != "3-D View")
            {
                Thread.Sleep(500);
            }
        }

        public void Action_ImportFrameDefinitionsFromS2KTextFile(string textFileName)
        {
            string accessFullPath = Path.Combine(s2KModel.ModelDir, textFileName);

            if (!File.Exists(accessFullPath)) throw new S2KHelperException("The Textfile Filename does not exists");

            if (SAP2000HasOtherWindows) throw new S2KHelperException("You must close all SAP2000 dialogs before proceeding.");

            // Opens export to access window
            SapMainWindow.Keyboard.HoldKey(TS_WAPI.KeyboardInput.SpecialKeys.ALT);
            SapMainWindow.Keyboard.Enter("f");
            kb.LeaveAllKeys();
            SapMainWindow.Keyboard.Enter("i");
            SapMainWindow.Keyboard.Enter("t");

            TS_UITEM_WI::Window importBaseWindow = GetSapWindowByTitle("Import Tabular Database");
            importBaseWindow.Focus();
            SapMainWindow.Focus();

            // Gets the radiobutton
            TS.UIItems.RadioButton addExisting = importBaseWindow.Get<TS.UIItems.RadioButton>("optType1");
            addExisting.Select();

            TS.UIItems.Button bntAdvanced = importBaseWindow.Get<TS.UIItems.Button>("cmdAdvanced");
            bntAdvanced.Click();

            TS_UITEM_WI::Window importAdvancedWindow = GetSapWindowByTitle("Tabular Database Import Options");
            importAdvancedWindow.Focus();
            SapMainWindow.Focus();

            TS.UIItems.Button bntResetAllDefs = importAdvancedWindow.Get<TS.UIItems.Button>("cmdDefaultsAll");
            bntResetAllDefs.Click();

            //TS.UIItems.CheckBox chkDisplayError = importAdvancedWindow.Get<TS.UIItems.CheckBox>("chkDisplay1");
            //TS.UIItems.CheckBox chkDisplayWarning = importAdvancedWindow.Get<TS.UIItems.CheckBox>("chkDisplay2");

            TS.UIItems.RadioButton neverAbortError = importAdvancedWindow.Get<TS.UIItems.RadioButton>("optError1");
            neverAbortError.Select();

            TS.UIItems.RadioButton neverAbortWarning = importAdvancedWindow.Get<TS.UIItems.RadioButton>("optWarning1");
            neverAbortWarning.Select();

            // Goes to the other tab
            TS.UIItems.TabItems.Tab dialogTab = importAdvancedWindow.Get<TS.UIItems.TabItems.Tab>("TabControl1");
            dialogTab.SelectTabPage(2);

            TS.UIItems.RadioButton allowDuplicatesFrames = importAdvancedWindow.Get<TS.UIItems.RadioButton>("optLocation1");
            allowDuplicatesFrames.Select();

            TS.UIItems.RadioButton allowDuplicatesLinks = importAdvancedWindow.Get<TS.UIItems.RadioButton>("optLocationLink1");
            allowDuplicatesLinks.Select();

            TS.UIItems.Button bntOKAdvanced = importAdvancedWindow.Get<TS.UIItems.Button>("cmdOK");
            bntOKAdvanced.Click();

            TS.UIItems.Button bntOKBase = importBaseWindow.Get<TS.UIItems.Button>("cmdOK");
            bntOKBase.Click();

            // Gets the file select window
            TS_UITEM_WI::Window saveAsWindow = GetSapWindowByTitle("Import SAP2000 Text File");
            saveAsWindow.Focus();
            SapMainWindow.Focus();

            SapMainWindow.Keyboard.Enter(accessFullPath.Replace("\\\\", "\\"));

            // Opens
            SapMainWindow.Keyboard.HoldKey(TS_WAPI.KeyboardInput.SpecialKeys.ALT);
            SapMainWindow.Keyboard.Enter("o");
            kb.LeaveAllKeys();

            // Lock until we get the result window
            while (true)
            {
                try
                {
                    List<string> names = SapWindowNames;
                    string accessWindowReportName = names.FirstOrDefault(a => a.Contains("Access"));
                    if (!string.IsNullOrWhiteSpace(accessWindowReportName))
                    {
                        Debug.WriteLine("Access window report appeared on screen.");

                        TS_UITEM_WI::Window importLogWindow = GetSapWindowByTitle("Access Database Import Log");
                        importLogWindow.Focus();
                        importLogWindow.Keyboard.HoldKey(TS_WAPI.KeyboardInput.SpecialKeys.ALT);
                        importLogWindow.Keyboard.PressSpecialKey(TS_WAPI.KeyboardInput.SpecialKeys.F4);
                        kb.LeaveAllKeys();

                        Debug.WriteLine("Access window report closed.");

                        break;
                    }

                    Debug.WriteLine("SAP2000 Auto: Access Window did not appear.");
                    Thread.Sleep(500);
                }
                catch (Exception)
                {
                    break;
                }
            }

            // Hits the OK button in that window

            // Waits for SAP2000 to finish
            TS.UIItems.Label statusLabel = SapMainWindow.Get<TS.UIItems.Label>("lblStatus");
            while (statusLabel.Text.Trim() != "Ready" && statusLabel.Text.Trim() != "3-D View")
            {
                Thread.Sleep(500);
                statusLabel = SapMainWindow.Get<TS.UIItems.Label>("lblStatus");
            }
        }

        public void Action_ImportCoordinateUpdateFromS2KTextFile(string textFileName)
        {
            //string accessFullPath = Path.Combine(this.s2KModel.ModelDir, textFileName);

            if (!File.Exists(textFileName)) throw new S2KHelperException("The Textfile Filename does not exists");

            SapMainWindow.Focus();
            Debug.WriteLine("Sap window focused.");

            // Ensures that the Analysis Complete is not open
            try
            {
                List<string> names = SapWindowNames;
                string analysisCompleteName = names.FirstOrDefault(a => a.Contains("Analysis Complete"));
                if (!string.IsNullOrWhiteSpace(analysisCompleteName))
                {
                    TS_UITEM_WI::Window analysisComplete = GetSapWindowByTitle(analysisCompleteName);
                    analysisComplete.Focus();

                    analysisComplete.Keyboard.HoldKey(TS_WAPI.KeyboardInput.SpecialKeys.ALT);
                    analysisComplete.Keyboard.PressSpecialKey(TS_WAPI.KeyboardInput.SpecialKeys.F4);
                    kb.LeaveAllKeys();
                    Debug.WriteLine("Analysis Window Closed.");
                }
            }
            catch (Exception)
            {
            }

            if (SAP2000HasOtherWindows) throw new S2KHelperException("You must close all SAP2000 dialogs before proceeding.");

            SapMainWindow.Focus();
            // Opens export to access window
            SapMainWindow.Keyboard.HoldKey(TS_WAPI.KeyboardInput.SpecialKeys.ALT);
            SapMainWindow.Keyboard.Enter("f");
            kb.LeaveAllKeys();
            SapMainWindow.Keyboard.Enter("i");
            SapMainWindow.Keyboard.Enter("t");

            TS_UITEM_WI::Window importBaseWindow = GetSapWindowByTitle("Import Tabular Database");
            importBaseWindow.Focus();
            SapMainWindow.Focus();


            // Gets the radiobutton
            TS.UIItems.RadioButton addExisting = importBaseWindow.Get<TS.UIItems.RadioButton>("optType1");
            addExisting.Select();
            Debug.WriteLine("Selected add Existing.");

            TS.UIItems.Button bntAdvanced = importBaseWindow.Get<TS.UIItems.Button>("cmdAdvanced");
            bntAdvanced.Click();
            Debug.WriteLine("Clicked Advanced.");

            TS_UITEM_WI::Window importAdvancedWindow = GetSapWindowByTitle("Tabular Database Import Options");
            importAdvancedWindow.Focus();
            SapMainWindow.Focus();

            TS.UIItems.Button bntResetAllDefs = importAdvancedWindow.Get<TS.UIItems.Button>("cmdDefaultsAll");
            bntResetAllDefs.Click();
            Debug.WriteLine("Clicked All Defaults.");

            //TS.UIItems.CheckBox chkDisplayError = importAdvancedWindow.Get<TS.UIItems.CheckBox>("chkDisplay1");
            //TS.UIItems.CheckBox chkDisplayWarning = importAdvancedWindow.Get<TS.UIItems.CheckBox>("chkDisplay2");

            TS.UIItems.RadioButton neverAbortError = importAdvancedWindow.Get<TS.UIItems.RadioButton>("optError1");
            neverAbortError.Select();
            Debug.WriteLine("Never Abort Error: OK.");

            TS.UIItems.RadioButton neverAbortWarning = importAdvancedWindow.Get<TS.UIItems.RadioButton>("optWarning1");
            neverAbortWarning.Select();
            Debug.WriteLine("Never Abort Warning: OK.");

            // Goes to the other tab
            TS.UIItems.TabItems.Tab dialogTab = importAdvancedWindow.Get<TS.UIItems.TabItems.Tab>("TabControl1");
            dialogTab.SelectTabPage(1); // Items with Same Name
            Debug.WriteLine("Changed TAB.");

            TS.UIItems.RadioButton Joint_Frame_Area_ReplaceElementInModel = importAdvancedWindow.Get<TS.UIItems.RadioButton>("optNameTwo1");
            Joint_Frame_Area_ReplaceElementInModel.Select();
            Debug.WriteLine("Told to replace.");

            TS.UIItems.Button bntOKAdvanced = importAdvancedWindow.Get<TS.UIItems.Button>("cmdOK");
            bntOKAdvanced.Click();
            Debug.WriteLine("Closed advanced.");

            TS.UIItems.Button bntOKBase = importBaseWindow.Get<TS.UIItems.Button>("cmdOK");
            bntOKBase.Click();
            Debug.WriteLine("Closed Base.");

            // Gets the file select window
            TS_UITEM_WI::Window saveAsWindow = GetSapWindowByTitle("Import SAP2000 Text File");
            saveAsWindow.Focus();
            SapMainWindow.Focus();

            SapMainWindow.Keyboard.Enter(textFileName.Replace("\\\\", "\\"));

            // Opens
            SapMainWindow.Keyboard.HoldKey(TS_WAPI.KeyboardInput.SpecialKeys.ALT);
            SapMainWindow.Keyboard.Enter("o");
            kb.LeaveAllKeys();

            // Lock until we get the result window
            while (true)
            {
                try
                {
                    List<string> names = SapWindowNames;
                    string accessWindowReportName = names.FirstOrDefault(a => a.Contains("Access"));
                    if (!string.IsNullOrWhiteSpace(accessWindowReportName))
                    {
                        Debug.WriteLine("Access window report appeared on screen.");

                        TS_UITEM_WI::Window importLogWindow = GetSapWindowByTitle("Access Database Import Log");
                        importLogWindow.Focus();
                        importLogWindow.Keyboard.HoldKey(TS_WAPI.KeyboardInput.SpecialKeys.ALT);
                        importLogWindow.Keyboard.PressSpecialKey(TS_WAPI.KeyboardInput.SpecialKeys.F4);
                        kb.LeaveAllKeys();

                        Debug.WriteLine("Access window report closed.");

                        break;
                    }

                    Thread.Sleep(500);
                }
                catch (Exception)
                {
                    break;
                }
            }


            // Waits for SAP2000 to finish
            TS.UIItems.Label statusLabel = SapMainWindow.Get<TS.UIItems.Label>("lblStatus");
            while (statusLabel.Text.Trim() != "Ready" && statusLabel.Text.Trim() != "3-D View")
            {
                Thread.Sleep(500);
                statusLabel = SapMainWindow.Get<TS.UIItems.Label>("lblStatus");
            }

            Debug.WriteLine("Import Finished.");
        }

        //public bool SendHotKeyInstructionToSAP(SAP2000HotKeyInstruction inInstruction)
        //{
        //    // Makes SAP window the front window
        //    if (!this.ActivateSap()) return false;

        //    switch (inInstruction)
        //    {
        //        case SAP2000HotKeyInstruction.RemoveSelectionFromView:
        //            System.Windows.Forms.SendKeys.SendWait("%v");
        //            System.Windows.Forms.SendKeys.SendWait("c");
        //            break;
        //        case SAP2000HotKeyInstruction.ShowAll:
        //            System.Windows.Forms.SendKeys.SendWait("%v");
        //            System.Windows.Forms.SendKeys.SendWait("A");
        //            break;
        //        default:
        //            break;
        //    }

        //    // Makes SAP window the front window
        //    if (!this.ActivateCurrentApplication()) return false;
        //    return true;
        //}


        public void ExportTablesToS2K(string FileName, List<Sap2000ExportTable> Tables, Sap2000ExportOptions exportOptions = null, bool inUpdateInterface = false)
        {
            if (string.IsNullOrEmpty(FileName)) throw new S2KHelperException("The export filename must be given.");

            if (Tables == null || Tables.Count == 0) throw new S2KHelperException("You must select at least one table to export.");

            if (exportOptions == null) exportOptions = new Sap2000ExportOptions();

            try
            {
                FlaUI.Core.Application sapApp = FlaUI_SapApplication;

                using (var automation = new UIA3Automation())
                {
                    if (inUpdateInterface) BusyOverlayBindings.I.SetIndeterminate("SAP2000: Opening and Setting Export Form.");

                    var mainWindow = sapApp.GetMainWindow(automation);
                    FlaUI_CloseAllSecondaryWindows_RecursiveChildren(mainWindow);

                    mainWindow.Focus();
                    using (FlaUI.Core.Input.Keyboard.Pressing(VirtualKeyShort.ALT))
                    {
                        FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.KEY_F);
                        FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.KEY_E);
                        FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.KEY_T);
                    }

                    // Gets the export window
                    var exportWindowRetry = Retry.WhileNull(() => mainWindow.FindFirstChild(cf => cf.ByAutomationId("DBTableForm")),
                        timeout: TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(200), throwOnTimeout: false, ignoreException: false);
                    if (!exportWindowRetry.Success) throw new S2KHelperException("Could not get the Export Form.");
                    FlaUI.Core.AutomationElements.Window exportWindow = exportWindowRetry.Result.AsWindow();

                    // Makes a basic setup
                    var tabletree = exportWindow.FindFirstChild(cf => cf.ByAutomationId("TreeView1")).AsTree();

                    var modelDefinition = tabletree.FindChildAt(0).AsTreeItem();

                    // First, collapses all entries
                    var allDescendents = modelDefinition.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.TreeItem));
                    foreach (var descendent in allDescendents)
                    {
                        var descTreeItem = descendent.AsTreeItem();
                        descTreeItem.Collapse();
                    }

                    modelDefinition.Collapse();

                    Regex modelDefRegex = new Regex(@".*\((?<selCount>\d*) of (?<totalCount>\d*) tables selected\)");
                    Debug.WriteLine(modelDefinition.Name);
                    if (Int32.Parse(modelDefRegex.Match(modelDefinition.Name).Groups["selCount"].Value) == Int32.Parse(modelDefRegex.Match(modelDefinition.Name).Groups["totalCount"].Value))
                    {
                        // Only presses once to deselect
                        exportWindow.Focus();
                        FlaUI.Core.Input.Mouse.Click(new System.Drawing.Point(modelDefinition.BoundingRectangle.X - 10, modelDefinition.BoundingRectangle.Y + 8));
                    }

                    if (Int32.Parse(modelDefRegex.Match(modelDefinition.Name).Groups["selCount"].Value) != 0)
                    {
                        // Selects all
                        exportWindow.Focus();
                        FlaUI.Core.Input.Mouse.Click(new System.Drawing.Point(modelDefinition.BoundingRectangle.X - 10, modelDefinition.BoundingRectangle.Y + 8));

                        if (Int32.Parse(modelDefRegex.Match(modelDefinition.Name).Groups["selCount"].Value) != Int32.Parse(modelDefRegex.Match(modelDefinition.Name).Groups["totalCount"].Value))
                            throw new S2KHelperException("Could not select all items of group MODEL DEFINITION");

                        // Deselects all
                        exportWindow.Focus();
                        FlaUI.Core.Input.Mouse.Click(new System.Drawing.Point(modelDefinition.BoundingRectangle.X - 10, modelDefinition.BoundingRectangle.Y + 8));

                        if (Int32.Parse(modelDefRegex.Match(modelDefinition.Name).Groups["selCount"].Value) != 0)
                            throw new S2KHelperException("Could not deselect all items of group MODEL DEFINITION");
                    }

                    Debug.WriteLine(modelDefinition.Name);
                    var analysisResults = tabletree.FindChildAt(1).AsTreeItem();
                }

                sapApp = FlaUI_SapApplication;
                using (var automation = new UIA3Automation())
                {
                    var mainWindow = sapApp.GetMainWindow(automation);
                }
            }
            catch (Exception ex)
            {
                throw new S2KHelperException("Could not automate SAP2000 to export the tables to s2k format", ex);
            }
        }

        public void ExportTablesToSQLite()
        {
            throw new NotImplementedException();
            FlaUI.Core.Application sapApp = FlaUI_SapApplication;
            using (var automation = new UIA3Automation())
            {
                var mainWindow = sapApp.GetMainWindow(automation);

                FlaUI_CloseAllSecondaryWindows_RecursiveChildren(mainWindow);

                //mainWindow.
            }
        }


        #region FlaUI

        private UIA3Automation _flaUiAutomation = null;

        private UIA3Automation FlaUI_Automation
        {
            get
            {
                if (_flaUiAutomation == null) throw new S2KHelperException("FlaUI: Attempt to get the automation object without initializing. Did you add the Dispose and nullifying try-finally block?");
                return _flaUiAutomation;
            }
            set
            {
                if (_flaUiAutomation != null && value != null) throw new S2KHelperException("FlaUI: Attempt to get set a new automation object while the previous one wasn't disposed. Did you add the Dispose and nullifying try-finally block?");
                _flaUiAutomation = value;
            }
        }

        private FlaUI.Core.Application FlaUI_SapApplication
        {
            get
            {
                // Finds the path
                string programFiles = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
                DirectoryInfo baseDir = new DirectoryInfo(Path.Combine(programFiles, "Computers and Structures"));
                if (!baseDir.Exists) throw new S2KHelperException($"SAP2000 must be installed and available in the default install location! Could not find the directory <{baseDir.FullName}>.");

                Regex SapDirRegex = new Regex(@"SAP2000\s*(?<version>\s\d+)");

                // Gets the largest dir
                (string DirName, double Version) largestVersion;
                try
                {
                    var dirWithVersion = from a in baseDir.GetDirectories() where SapDirRegex.IsMatch(a.Name) select (a.Name, Double.Parse(SapDirRegex.Match(a.Name).Groups["version"].Value));
                    largestVersion = dirWithVersion.OrderByDescending(a => a.Item2).First();
                }
                catch (Exception ex)
                {
                    throw new S2KHelperException("FlaUI: SAP2000 must be installed and available in the default install location! Could not find a subdir with the format SAP2000 <version>, or could not find the maximum version.", ex);
                }

                string fullPath = Path.Combine(baseDir.FullName, largestVersion.DirName, "SAP2000.exe");

                try
                {
                    return FlaUI.Core.Application.Attach(fullPath);
                }
                catch (Exception ex)
                {
                    throw new S2KHelperException($"FlaUI: Could not attach to running SAP2000 for UI Automation. Tried to attach to program at {fullPath}.", ex);
                }
            }
        }
        private Window FlaUI_SapMainWindow
        {
            get
            {
                try
                {
                    FlaUI.Core.Application sapApp = FlaUI_SapApplication;
                    return sapApp.GetMainWindow(FlaUI_Automation);
                }
                catch (Exception ex)
                {
                    throw new S2KHelperException("FlaUI: Could not get the main SAP2000 window.", ex);
                }
            }
        }

        private List<Window> FlaUI_AllChildWindows(Window inWindow)
        {
            try
            {
                RetryResult<List<Window>> result = Retry.While(
                    () =>
                    {
                        AutomationElement[] tmpWindows = inWindow.FindAllChildren(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Window));
                        return (from a in tmpWindows select a.AsWindow()).ToList();
                    },
                    inList => inList == null, timeout: TimeSpan.FromSeconds(10), interval: TimeSpan.FromSeconds(1), throwOnTimeout: true);

                return result.Result;
            }
            catch (Exception ex)
            {
                throw new S2KHelperException("FlaUI: Could not get the SAP2000 Window List.", ex);
            }
        }

        private bool FlaUI_SAP2000HasOtherWindows => FlaUI_AllChildWindows(FlaUI_SapMainWindow).Count > 1;

        private void FlaUI_CloseAllSecondaryWindows_RecursiveChildren(Window inWindow = null)
        {
            Window wnd = inWindow ?? FlaUI_SapMainWindow;
            while (true)
            {
                var secWindows = FlaUI_AllChildWindows(wnd);
                if (secWindows.Count == 0) return;

                foreach (var item in secWindows)
                {
                    // Closes the child windows
                    FlaUI_CloseAllSecondaryWindows_RecursiveChildren(item);
                    item.Close();
                }
            }
        }

        private void FlaUI_ClickOnWindow(System.Drawing.Point inPoint, Window inWindow)
        {
            //inWindow.Focus();
            //inWindow.SetForeground();
            FlaUI.Core.Input.Mouse.Click(inPoint);
        }

        /// <summary>
        /// DO NOT SEND FROM ANOTHER FlaUI_Function! This is due to the try/finally
        /// </summary>
        public void FlaUI_Action_SendDeleteKey()
        {
            try
            {
                FlaUI_Automation = new UIA3Automation();

                FlaUI_SapMainWindow.Focus();
                FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.DELETE);
            }
            finally
            {
                FlaUI_Automation.Dispose();
                FlaUI_Automation = null;
            }

        }
        public void FlaUI_Action_SendDeleteMenuAction()
        {
            try
            {
                FlaUI_Automation = new UIA3Automation();

                FlaUI_SapMainWindow.Focus();
                FlaUI_CloseAllSecondaryWindows_RecursiveChildren(FlaUI_SapMainWindow);

                // Opens the Import from S2K Window
                using (FlaUI.Core.Input.Keyboard.Pressing(VirtualKeyShort.ALT))
                {
                    FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.KEY_E);
                    FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.KEY_D);
                }
                FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.ESC);
            }
            finally
            {
                FlaUI_Automation.Dispose();
                FlaUI_Automation = null;
            }
        }

        public void FlaUI_Action_ExportTablesToS2K(string inFullFileName, List<Sap2000ExportTable> inSelectedTables, Sap2000ExportOptions inExportOptions = null, int inFinishTimeoutMins = 300, bool inUpdateInterface = false)
        {
            // Transforms the list into a list of table data
            List<Sap2000AutomatorTableExportData> selTables = SapAutoExtensions.FromEnumList(inSelectedTables);

            if (string.IsNullOrEmpty(inFullFileName)) throw new S2KHelperException("The export to S2K filename must be given.");

            if (selTables == null || selTables.Count == 0) throw new S2KHelperException("You must select at least one table to export.");

            // Handling the output filename
            string fileName = inFullFileName;
            FileInfo fInfo = new FileInfo(fileName);
            if (fInfo.Exists)
            {
                try
                {
                    fInfo.Delete();
                }
                catch (Exception)
                {
                    throw new S2KHelperException("The target filename already exists and it could not be deleted."); ;
                }
            }

            if (inExportOptions == null) inExportOptions = new Sap2000ExportOptions();

            try
            {
                if (inUpdateInterface) BusyOverlayBindings.I.SetIndeterminate("SAP2000: Opening and Setting Export Forms.");

                FlaUI_Automation = new UIA3Automation();
                FlaUI_SapMainWindow.Focus();

                FlaUI_CloseAllSecondaryWindows_RecursiveChildren(FlaUI_SapMainWindow);

                // Opens the Export to S2K Window
                FlaUI_SapMainWindow.Focus();
                FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.ESC);
                using (FlaUI.Core.Input.Keyboard.Pressing(VirtualKeyShort.ALT))
                {
                    FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.KEY_F);
                    FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.KEY_E);
                    FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.KEY_T);
                }

                // Gets the export window
                RetryResult<AutomationElement> exportWindowRetry = Retry.WhileNull(() => FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByAutomationId("DBTableForm").And(cf.ByControlType(ControlType.Window))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Export Form.");
                Window exportWindow = exportWindowRetry.Result.AsWindow();

                #region Expose All Input Tables
                // First, marks the Expose all Tables
                RetryResult<AutomationElement> exposeAllRetry = Retry.WhileNull(() => exportWindow.FindFirstDescendant(cf => cf.ByAutomationId("chkEnable").And(cf.ByControlType(ControlType.CheckBox))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Expose All Tables CheckBox.");
                CheckBox exposeAllCheckBox = exposeAllRetry.Result.AsCheckBox();

                if (exposeAllCheckBox.IsChecked.HasValue && exposeAllCheckBox.IsChecked.Value == false)
                {
                    exposeAllCheckBox.IsChecked = true;
                } 
                #endregion

                // Getting the Table Tree
                RetryResult<AutomationElement> tableTreeRetry = Retry.WhileNull(() => exportWindow.FindFirstChild(cf => cf.ByAutomationId("TreeView1").And(cf.ByControlType(ControlType.Tree))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Table Tree.");
                Tree tableTree = tableTreeRetry.Result.AsTree();

                #region Model Definition - Reset

                // Gets the first element - Model Definition
                RetryResult<AutomationElement> modelDefTreeItemRetry = Retry.WhileNull(() => tableTree.FindFirstChild(cf => cf.ByControlType(ControlType.TreeItem)),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Model Definition Tree Item.");
                TreeItem modelDefTreeItem = modelDefTreeItemRetry.Result.AsTreeItem();
                if (!modelDefTreeItem.Patterns.ExpandCollapse.IsSupported) throw new FlaUIException($"TreeItem {modelDefTreeItem.Name} does not support the Expand-Collapse Pattern.");

                // Checks the name to see if the selections are clear
                Regex modelDefRegex = new Regex(@"MODEL\s*DEFINITION\s*\((?<sel>\d*)\s*of\s*(?<total>\d*)");

                while (true)
                {
                    // Gets again to ensure refresh
                    modelDefTreeItemRetry = Retry.WhileNull(() => tableTree.FindFirstChild(cf => cf.ByControlType(ControlType.TreeItem)),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Model Definition Tree Item.");
                    modelDefTreeItem = modelDefTreeItemRetry.Result.AsTreeItem();
                    if (!modelDefTreeItem.Patterns.ExpandCollapse.IsSupported) throw new FlaUIException($"TreeItem {modelDefTreeItem.Name} does not support the Expand-Collapse Pattern.");


                    Match modelDefMatch1 = modelDefRegex.Match(modelDefTreeItem.Name);
                    if (!modelDefMatch1.Success) throw new S2KHelperException("Could not match the Model Definition Tree Item Name to the Regex.");
                    if (!int.TryParse(modelDefMatch1.Groups["sel"].Value, out int modelDefMatch1_SelCount)) throw new S2KHelperException("Could not match the Model Definition Tree Item Name to the Regex.");

                    if (modelDefMatch1_SelCount == 0) break;

                    // Clicks on the selection
                    FlaUI_ClickOnWindow(modelDefTreeItem.SapTreeCheckBox(), exportWindow);
                }
                modelDefTreeItem.Patterns.ExpandCollapse.Pattern.Collapse();
                #endregion

                #region Analysis Results - Reset
                // Tries to Get the Second item - Analysis Result
                RetryResult<AutomationElement> analysisResultTreeItemRetry = Retry.WhileNull(() => tableTree.FindChildAt(1, cf => cf.ByControlType(ControlType.TreeItem)),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: false, ignoreException: true);
                TreeItem analysisResultTreeItem = null;
                if (analysisResultTreeItemRetry.Success)
                {

                    // Checks the name to see if the selections are clear
                    Regex analysisResultRegex = new Regex(@"ANALYSIS\s*RESULTS\s*\((?<sel>\d*)\s*of\s*(?<total>\d*)");

                    while (true)
                    {
                        analysisResultTreeItemRetry = Retry.WhileNull(() => tableTree.FindChildAt(1, cf => cf.ByControlType(ControlType.TreeItem)),
                            timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: false);
                        analysisResultTreeItem = analysisResultTreeItemRetry.Result.AsTreeItem();
                        if (!analysisResultTreeItem.Patterns.ExpandCollapse.IsSupported) throw new FlaUIException($"TreeItem {analysisResultTreeItem.Name} does not support the Expand-Collapse Pattern.");


                        Match analysisResultMatch1 = analysisResultRegex.Match(analysisResultTreeItem.Name);
                        if (!analysisResultMatch1.Success) throw new S2KHelperException("Could not match the Analysis Results Tree Item Name to the Regex.");
                        if (!int.TryParse(analysisResultMatch1.Groups["sel"].Value, out int analysisResultMatch1_SelCount)) throw new S2KHelperException("Could not match the Analysis Results Tree Item Name to the Regex.");

                        if (analysisResultMatch1_SelCount == 0) break;

                        // Clicks on the selection
                        FlaUI_ClickOnWindow(analysisResultTreeItem.SapTreeCheckBox(), exportWindow);
                    }

                    analysisResultTreeItem.Patterns.ExpandCollapse.Pattern.Collapse();
                }
                else // The analysis results tree item could not be found. This means that the model is not run.
                {
                    if (selTables.Any(a => a.BaseTreeItemEnumValue == Sap2000ExportTableBaseTreeItem.AnalysisResults))
                        throw new S2KHelperException("In order to export analysis results tables the analysis must be run!");
                }

                #endregion

                #region Selection Only

                RetryResult<AutomationElement> selectionOnlyCheckBoxRetry = Retry.WhileNull(() => exportWindow.FindFirstDescendant(cf => cf.ByAutomationId("chkSelect").And(cf.ByControlType(ControlType.CheckBox))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Selection Only CheckBox.");
                CheckBox selectionOnlyCheckBox = selectionOnlyCheckBoxRetry.Result.AsCheckBox();
                if (selectionOnlyCheckBox.IsEnabled)
                {
                    if (selectionOnlyCheckBox.IsChecked != inExportOptions.SelectionOnly)
                        selectionOnlyCheckBox.IsChecked = inExportOptions.SelectionOnly;
                }

                #endregion

                if (inUpdateInterface) BusyOverlayBindings.I.SetIndeterminate("SAP2000: Selecting Related Load Patterns (for load inputs) and Load Cases/Combos (for output).");

                #region Load Patterns

                RetryResult<AutomationElement> loadPatternButtonRetry = Retry.WhileNull(() => exportWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByAutomationId("cmdLoad"))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Select Load Pattern Button.");
                Button loadPatternButton = loadPatternButtonRetry.Result.AsButton();
                loadPatternButton.Click();

                // Gets the window
                RetryResult<AutomationElement> selectLoadPatternWindowRetry = Retry.WhileNull(() => exportWindow.FindFirstChild(cf => cf.ByAutomationId("SelectForm").And(cf.ByControlType(ControlType.Window))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Select Load Pattern Window.");
                Window selectLoadPatternWindow = selectLoadPatternWindowRetry.Result.AsWindow();

                // Gets the clear all button
                RetryResult<AutomationElement> selectLoadPatternWindowClearAllButtonRetry = Retry.WhileNull(() => selectLoadPatternWindow.FindFirstDescendant(cf => cf.ByAutomationId("cmdClear").And(cf.ByControlType(ControlType.Button))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Select Load Pattern Window Clear All Button.");
                Button selectLoadPatternWindowClearAllButton = selectLoadPatternWindowClearAllButtonRetry.Result.AsButton();
                selectLoadPatternWindowClearAllButton.Click();

                // Gets the list
                RetryResult<AutomationElement> selectLoadPatternWindowListRetry = Retry.WhileNull(() => selectLoadPatternWindow.FindFirstDescendant(cf => cf.ByAutomationId("_lstName_0").And(cf.ByControlType(ControlType.List))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Select Load Pattern Window List.");
                ListBox selectLoadPatternWindowList = selectLoadPatternWindowListRetry.Result.AsListBox();

                // Selects all
                RetryResult<AutomationElement[]> selectLoadPatternWindowListItemsRetry = Retry.WhileNull(() => selectLoadPatternWindowList.FindAllChildren(cf => cf.ByControlType(ControlType.ListItem)),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Select Load Pattern Window List Items.");
                List<ListBoxItem> selectLoadPatternWindowListItems = (from a in selectLoadPatternWindowListItemsRetry.Result
                    select a.AsListBoxItem()).ToList();

                // Selects the requested items
                if (inExportOptions.LoadPatterns == null)
                {
                    foreach (ListBoxItem item in selectLoadPatternWindowListItems)
                    {
                        item.AddToSelection();
                    }
                }
                else
                {
                    foreach (string loadPattern in inExportOptions.LoadPatterns)
                    {
                        ListBoxItem item = selectLoadPatternWindowListItems.FirstOrDefault(a => a.Name == loadPattern);
                        if (item == null) throw new S2KHelperException($"FlaUI: Requested load pattern {loadPattern} is not available.");
                        else item.AddToSelection();
                    }
                }

                // Gets the ok button
                RetryResult<AutomationElement> selectLoadPatternWindowOKButtonRetry = Retry.WhileNull(() => selectLoadPatternWindow.FindFirstDescendant(cf => cf.ByAutomationId("cmdOK").And(cf.ByControlType(ControlType.Button))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Select Load Pattern Window OK Button.");
                Button selectLoadPatternWindowOKButton = selectLoadPatternWindowOKButtonRetry.Result.AsButton();
                selectLoadPatternWindowOKButton.Click();

                #endregion

                #region Load Cases Selection - Only if the analysis result exists

                if (analysisResultTreeItem != null)
                {
                    RetryResult<AutomationElement> loadCaseButtonRetry = Retry.WhileNull(() => exportWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByAutomationId("cmdCase"))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Select Load Case Button.");
                    Button loadCaseButton = loadCaseButtonRetry.Result.AsButton();
                    loadCaseButton.Click();

                    // Gets the window
                    RetryResult<AutomationElement> selectLoadCaseWindowRetry = Retry.WhileNull(() => exportWindow.FindFirstChild(cf => cf.ByAutomationId("SelectForm").And(cf.ByControlType(ControlType.Window))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Select Load Case Window.");
                    Window selectLoadCaseWindow = selectLoadCaseWindowRetry.Result.AsWindow();

                    // Gets the clear all button
                    RetryResult<AutomationElement> selectLoadCaseWindowClearAllButtonRetry = Retry.WhileNull(() => selectLoadCaseWindow.FindFirstDescendant(cf => cf.ByAutomationId("cmdClear").And(cf.ByControlType(ControlType.Button))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Select Load Case Window Clear All Button.");
                    Button selectLoadCaseWindowClearAllButton = selectLoadCaseWindowClearAllButtonRetry.Result.AsButton();
                    selectLoadCaseWindowClearAllButton.Click();

                    // Gets the list
                    RetryResult<AutomationElement> selectLoadCaseWindowListRetry = Retry.WhileNull(() => selectLoadCaseWindow.FindFirstDescendant(cf => cf.ByAutomationId("_lstName_0").And(cf.ByControlType(ControlType.List))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Select Load Case Window List.");
                    ListBox selectLoadCaseWindowList = selectLoadCaseWindowListRetry.Result.AsListBox();

                    // Selects all
                    RetryResult<AutomationElement[]> selectLoadCaseWindowListItemsRetry = Retry.WhileNull(() => selectLoadCaseWindowList.FindAllChildren(cf => cf.ByControlType(ControlType.ListItem)),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Select Load Case Window List Items.");
                    List<ListBoxItem> selectLoadCaseWindowListItems = (from a in selectLoadCaseWindowListItemsRetry.Result
                        select a.AsListBoxItem()).ToList();

                    // Selects the requested items
                    if (inExportOptions.LoadCasesAndCombos == null)
                    {
                        foreach (ListBoxItem item in selectLoadCaseWindowListItems)
                        {
                            item.AddToSelection();
                        }
                    }
                    else
                    {
                        foreach (string loadCase in inExportOptions.LoadCasesAndCombos)
                        {
                            ListBoxItem item = selectLoadCaseWindowListItems.FirstOrDefault(a => a.Name == loadCase);
                            if (item == null) throw new S2KHelperException($"FlaUI: Requested load case {loadCase} is not available.");
                            else item.AddToSelection();
                        }
                    }

                    // Gets the ok button
                    RetryResult<AutomationElement> selectLoadCaseWindowOKButtonRetry = Retry.WhileNull(() => selectLoadCaseWindow.FindFirstDescendant(cf => cf.ByAutomationId("cmdOK").And(cf.ByControlType(ControlType.Button))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Select Load Case Window OK Button.");
                    Button selectLoadCaseWindowOKButton = selectLoadCaseWindowOKButtonRetry.Result.AsButton();
                    selectLoadCaseWindowOKButton.Click();
                }

                #endregion

                #region Load Cases Options - Only if the analysis result exists

                if (analysisResultTreeItem != null)
                {
                    RetryResult<AutomationElement> loadCaseOptionsButtonRetry = Retry.WhileNull(() => exportWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByAutomationId("cmdOptions"))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Load Case Options Button.");
                    Button loadCaseOptionsButton = loadCaseOptionsButtonRetry.Result.AsButton();
                    loadCaseOptionsButton.Click();

                    // Gets the window
                    RetryResult<AutomationElement> loadCaseOptionsWindowRetry = Retry.WhileNull(() => exportWindow.FindFirstChild(cf => cf.ByAutomationId("fDBOutputOptions").And(cf.ByControlType(ControlType.Window))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Load Case Output Options Window.");
                    Window loadCaseOptionsWindow = loadCaseOptionsWindowRetry.Result.AsWindow();

                    // ** Base Reactions
                    RetryResult<AutomationElement> reactionsGroupRetry = Retry.WhileNull(() => loadCaseOptionsWindow.FindFirstDescendant(cf => cf.ByAutomationId("frmReac").And(cf.ByControlType(ControlType.Group))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Base Reactions Location Group.");
                    AutomationElement reactionsGroup = reactionsGroupRetry.Result;

                    if (reactionsGroup.IsEnabled)
                    {
                        RetryResult<AutomationElement> baseReactionsXRetry = Retry.WhileNull(() => reactionsGroup.FindFirstChild(cf => cf.ByAutomationId("TextBox1")),
                            timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Base Reactions X TextBox.");
                        TextBox baseReactionsX = baseReactionsXRetry.Result.AsTextBox();
                        baseReactionsX.Text = inExportOptions.BaseReactionsLocation.X.ToString();

                        RetryResult<AutomationElement> baseReactionsYRetry = Retry.WhileNull(() => reactionsGroup.FindFirstChild(cf => cf.ByAutomationId("TextBox2")),
                            timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Base Reactions Y TextBox.");
                        TextBox baseReactionsY = baseReactionsYRetry.Result.AsTextBox();
                        baseReactionsY.Text = inExportOptions.BaseReactionsLocation.Y.ToString();

                        RetryResult<AutomationElement> baseReactionsZRetry = Retry.WhileNull(() => reactionsGroup.FindFirstChild(cf => cf.ByAutomationId("TextBox3")),
                            timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Base Reactions Z TextBox.");
                        TextBox baseReactionsZ = baseReactionsZRetry.Result.AsTextBox();
                        baseReactionsZ.Text = inExportOptions.BaseReactionsLocation.Z.ToString();
                    }

                    // ** NonLinearStaticResults frmNLStatic
                    RetryResult<AutomationElement> nlStaticResultsGroupRetry = Retry.WhileNull(() => loadCaseOptionsWindow.FindFirstDescendant(cf => cf.ByAutomationId("frmNLStatic").And(cf.ByControlType(ControlType.Group))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Non-Linear Static Results Group.");
                    AutomationElement nlStaticResultsGroup = nlStaticResultsGroupRetry.Result;

                    if (nlStaticResultsGroup.IsEnabled)
                    {
                        string nlStaticResultsAutomationID = String.Empty;
                        switch (inExportOptions.NonLinearStaticResults)
                        {
                            case Sap2000ExportOptions.Sap2000OutResultsOptions.Envelopes:
                                nlStaticResultsAutomationID = "optNLStatic1";
                                break;

                            case Sap2000ExportOptions.Sap2000OutResultsOptions.StepByStep:
                                nlStaticResultsAutomationID = "optNLStatic2";
                                break;

                            case Sap2000ExportOptions.Sap2000OutResultsOptions.LastStep:
                                nlStaticResultsAutomationID = "optNLStatic3";
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        RetryResult<AutomationElement> nlStaticResultsGroupOptionRetry = Retry.WhileNull(() => nlStaticResultsGroup.FindFirstChild(cf => cf.ByAutomationId(nlStaticResultsAutomationID).And(cf.ByControlType(ControlType.RadioButton))),
                            timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the NonLinear Statics Results Option.");
                        RadioButton nlStaticResultsGroupOption = nlStaticResultsGroupOptionRetry.Result.AsRadioButton();
                        nlStaticResultsGroupOption.IsChecked = true;
                    }

                    // ** MultiStep Options frmMultiStep
                    RetryResult<AutomationElement> multiStepResultsGroupRetry = Retry.WhileNull(() => loadCaseOptionsWindow.FindFirstDescendant(cf => cf.ByAutomationId("frmMultiStep").And(cf.ByControlType(ControlType.Group))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Multi-Step Static Results Group.");
                    AutomationElement multiStepResultsGroup = multiStepResultsGroupRetry.Result;

                    if (multiStepResultsGroup.IsEnabled)
                    {
                        string multiStepResultsAutomationID = String.Empty;
                        switch (inExportOptions.MultiStepStaticResults)
                        {
                            case Sap2000ExportOptions.Sap2000OutResultsOptions.Envelopes:
                                multiStepResultsAutomationID = "optMultiStep1";
                                break;

                            case Sap2000ExportOptions.Sap2000OutResultsOptions.StepByStep:
                                multiStepResultsAutomationID = "optMultiStep2";
                                break;

                            case Sap2000ExportOptions.Sap2000OutResultsOptions.LastStep:
                                multiStepResultsAutomationID = "optMultiStep3";
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        RetryResult<AutomationElement> multiStepStaticResultsGroupOptionRetry = Retry.WhileNull(() => multiStepResultsGroup.FindFirstChild(cf => cf.ByAutomationId(multiStepResultsAutomationID).And(cf.ByControlType(ControlType.RadioButton))),
                            timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Multi-Step Statics Results Option.");
                        RadioButton multiStepStaticResultsGroupOption = multiStepStaticResultsGroupOptionRetry.Result.AsRadioButton();
                        multiStepStaticResultsGroupOption.IsChecked = true;
                    }

                    // ** LoadCombos Options frmCombo
                    RetryResult<AutomationElement> loadCombosGroupRetry = Retry.WhileNull(() => loadCaseOptionsWindow.FindFirstDescendant(cf => cf.ByAutomationId("frmCombo").And(cf.ByControlType(ControlType.Group))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Load Combos Group.");
                    AutomationElement loadCombosGroup = loadCombosGroupRetry.Result;

                    if (loadCombosGroup.IsEnabled)
                    {
                        string loadCombosAutomationID = String.Empty;
                        switch (inExportOptions.LoadCombos)
                        {
                            case Sap2000ExportOptions.Sap2000OutLoadCombos.Envelopes:
                                loadCombosAutomationID = "optCombo1";
                                break;

                            case Sap2000ExportOptions.Sap2000OutLoadCombos.Correspondance:
                                loadCombosAutomationID = "optCombo3";
                                break;

                            case Sap2000ExportOptions.Sap2000OutLoadCombos.MultipleValuesIfPossible:
                                loadCombosAutomationID = "optCombo2";
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        RetryResult<AutomationElement> loadCombosGroupOptionRetry = Retry.WhileNull(() => loadCombosGroup.FindFirstChild(cf => cf.ByAutomationId(loadCombosAutomationID).And(cf.ByControlType(ControlType.RadioButton))),
                            timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Load Combos Option.");
                        RadioButton loadCombosGroupOption = loadCombosGroupOptionRetry.Result.AsRadioButton();
                        loadCombosGroupOption.IsChecked = true;
                    }

                    // Gets the OK Button
                    RetryResult<Button> outputOptionsOkButtonRetry = Retry.WhileNull(() => loadCaseOptionsWindow.FindFirstDescendant(cf => cf.ByAutomationId("cmdOK").And(cf.ByControlType(ControlType.Button))).AsButton(),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the OK Button of the Output Options Window.");
                    Button outputOptionsOkButton = outputOptionsOkButtonRetry.Result;
                    outputOptionsOkButton.Click();
                }

                #endregion

                if (inUpdateInterface) BusyOverlayBindings.I.SetIndeterminate("SAP2000: Selecting Tables to Export.");

                #region Model Definition - Table Selections

                // Selecting the tables from the list
                modelDefTreeItem.Patterns.ExpandCollapse.Pattern.Expand();
                HashSet<string> modelDefSelectedTableNames = new HashSet<string>();
                foreach (Sap2000AutomatorTableExportData table in selTables.Where(a=> a.BaseTreeItemEnumValue == Sap2000ExportTableBaseTreeItem.ModelDefinition))
                {
                    // Skips if already selected
                    if (modelDefSelectedTableNames.Contains(table.ExportTable)) continue;

                    // Opens the main tree item
                    RetryResult<AutomationElement> mainTreeItemRetry = Retry.WhileNull(() => modelDefTreeItem.FindFirstChild(cf => cf.ByControlType(ControlType.TreeItem).And(cf.ByName(table.MainTreeItem))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: $"FlaUI: Could not get the {table.MainTreeItem} Main Tree Item. {table.BaseTreeItem} - {table.MainTreeItem} - {table.SecondaryTreeItem} - {table.ExportTable}.");
                    TreeItem mainTreeItem = mainTreeItemRetry.Result.AsTreeItem();
                    if (!mainTreeItem.Patterns.ExpandCollapse.IsSupported) throw new FlaUIException($"TreeItem {mainTreeItem.Name} does not support the Expand-Collapse Pattern.");
                    mainTreeItem.Patterns.ExpandCollapse.Pattern.Expand();

                    // Opens the secondary tree item
                    RetryResult<AutomationElement> secondaryTreeItemRetry = Retry.WhileNull(() => mainTreeItem.FindFirstChild(cf => cf.ByControlType(ControlType.TreeItem).And(cf.ByName(table.SecondaryTreeItem))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: $"FlaUI: Could not get the {table.SecondaryTreeItem} Secondary Tree Item. {table.BaseTreeItem} - {table.MainTreeItem} - {table.SecondaryTreeItem} - {table.ExportTable}.");
                    TreeItem secondaryTreeItem = secondaryTreeItemRetry.Result.AsTreeItem();
                    if (!secondaryTreeItem.Patterns.ExpandCollapse.IsSupported) throw new FlaUIException($"TreeItem {secondaryTreeItem.Name} does not support the Expand-Collapse Pattern.");
                    secondaryTreeItem.Patterns.ExpandCollapse.Pattern.Expand();

                    // Selects all of the tables in the same location
                    foreach (Sap2000AutomatorTableExportData tableOnSameSecTreeItem in selTables.Where(a => a.SecondaryTreeItem == table.SecondaryTreeItem))
                    {
                        RetryResult<AutomationElement> tableTreeItemRetry = Retry.WhileNull(() => secondaryTreeItem.FindFirstChild(cf => cf.ByControlType(ControlType.TreeItem).And(cf.ByName(tableOnSameSecTreeItem.ExportTable))),
                            timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: $"FlaUI: Could not get the {tableOnSameSecTreeItem.ExportTable} Export Table Tree Item. {tableOnSameSecTreeItem.BaseTreeItem} - {tableOnSameSecTreeItem.MainTreeItem} - {tableOnSameSecTreeItem.SecondaryTreeItem} - {tableOnSameSecTreeItem.ExportTable}.");
                        TreeItem tableTreeItem = tableTreeItemRetry.Result.AsTreeItem();
                        if (!tableTreeItem.Patterns.SelectionItem.IsSupported) throw new FlaUIException($"TreeItem {tableTreeItem.Name} does not support the SelectionItem Pattern.");
                        tableTreeItem.Patterns.SelectionItem.Pattern.Select();
                        FlaUI_ClickOnWindow(tableTreeItem.SapTreeCheckBox(), exportWindow);

                        // Adds to the list of selected tables
                        modelDefSelectedTableNames.Add(tableOnSameSecTreeItem.ExportTable);
                    }

                    // Collapses the items
                    secondaryTreeItem.Patterns.ExpandCollapse.Pattern.Collapse();
                    mainTreeItem.Patterns.ExpandCollapse.Pattern.Collapse();
                }

                #endregion

                #region Analysis Results - Table Selections

                // Selecting the tables from the list
                if (analysisResultTreeItem != null) analysisResultTreeItem.Patterns.ExpandCollapse.Pattern.Expand();
                HashSet<string> analysisResultsSelectedTableNames = new HashSet<string>();
                foreach (Sap2000AutomatorTableExportData table in selTables.Where(a => a.BaseTreeItemEnumValue == Sap2000ExportTableBaseTreeItem.AnalysisResults))
                {
                    // Skips if already selected
                    if (analysisResultsSelectedTableNames.Contains(table.ExportTable)) continue;

                    // Opens the main tree item
                    RetryResult<AutomationElement> mainTreeItemRetry = Retry.WhileNull(() => analysisResultTreeItem.FindFirstChild(cf => cf.ByControlType(ControlType.TreeItem).And(cf.ByName(table.MainTreeItem))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: $"FlaUI: Could not get the {table.MainTreeItem} Main Tree Item. {table.BaseTreeItem} - {table.MainTreeItem} - {table.SecondaryTreeItem} - {table.ExportTable}.");
                    TreeItem mainTreeItem = mainTreeItemRetry.Result.AsTreeItem();
                    if (!mainTreeItem.Patterns.ExpandCollapse.IsSupported) throw new FlaUIException($"TreeItem {mainTreeItem.Name} does not support the Expand-Collapse Pattern.");
                    mainTreeItem.Patterns.ExpandCollapse.Pattern.Expand();

                    // Opens the secondary tree item
                    RetryResult<AutomationElement> secondaryTreeItemRetry = Retry.WhileNull(() => mainTreeItem.FindFirstChild(cf => cf.ByControlType(ControlType.TreeItem).And(cf.ByName(table.SecondaryTreeItem))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: $"FlaUI: Could not get the {table.SecondaryTreeItem} Secondary Tree Item. {table.BaseTreeItem} - {table.MainTreeItem} - {table.SecondaryTreeItem} - {table.ExportTable}.");
                    TreeItem secondaryTreeItem = secondaryTreeItemRetry.Result.AsTreeItem();
                    if (!secondaryTreeItem.Patterns.ExpandCollapse.IsSupported) throw new FlaUIException($"TreeItem {secondaryTreeItem.Name} does not support the Expand-Collapse Pattern.");
                    secondaryTreeItem.Patterns.ExpandCollapse.Pattern.Expand();

                    // Selects all of the tables in the same location
                    foreach (Sap2000AutomatorTableExportData tableOnSameSecTreeItem in selTables.Where(a => a.SecondaryTreeItem == table.SecondaryTreeItem))
                    {
                        RetryResult<AutomationElement> tableTreeItemRetry = Retry.WhileNull(() => secondaryTreeItem.FindFirstChild(cf => cf.ByControlType(ControlType.TreeItem).And(cf.ByName(tableOnSameSecTreeItem.ExportTable))),
                            timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: $"FlaUI: Could not get the {tableOnSameSecTreeItem.ExportTable} Export Table Tree Item. {tableOnSameSecTreeItem.BaseTreeItem} - {tableOnSameSecTreeItem.MainTreeItem} - {tableOnSameSecTreeItem.SecondaryTreeItem} - {tableOnSameSecTreeItem.ExportTable}.");
                        TreeItem tableTreeItem = tableTreeItemRetry.Result.AsTreeItem();
                        if (!tableTreeItem.Patterns.SelectionItem.IsSupported) throw new FlaUIException($"TreeItem {tableTreeItem.Name} does not support the SelectionItem Pattern.");
                        tableTreeItem.Patterns.SelectionItem.Pattern.Select();
                        FlaUI_ClickOnWindow(tableTreeItem.SapTreeCheckBox(), exportWindow);

                        // Adds to the list of selected tables
                        analysisResultsSelectedTableNames.Add(tableOnSameSecTreeItem.ExportTable);
                    }

                    // Collapses the items
                    secondaryTreeItem.Patterns.ExpandCollapse.Pattern.Collapse();
                    mainTreeItem.Patterns.ExpandCollapse.Pattern.Collapse();
                }

                #endregion

                if (inUpdateInterface) BusyOverlayBindings.I.SetIndeterminate("SAP2000: Setting the save file dialog.");

                #region Export
                // Gets the OK button of the export window
                RetryResult<AutomationElement> exportOkButtonRetry = Retry.WhileNull(() => exportWindow.FindFirstChild(cf => cf.ByAutomationId("cmdOK").And(cf.ByControlType(ControlType.Button))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the OK Button of the Export Form.");
                Button exportOkButton = exportOkButtonRetry.Result.AsButton();
                exportOkButton.Click();

                // Gets the save window
                RetryResult<AutomationElement> saveWindowRetry = Retry.WhileNull(() => FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByClassName("#32770")),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Save Text File As Form.");
                Window saveWindow = saveWindowRetry.Result.AsWindow();

                // Manipulates the textbox
                RetryResult<AutomationElement> saveWindowFileNameTextBoxRetry = Retry.WhileNull(() => saveWindow.FindFirstDescendant(cf => cf.ByAutomationId("1001")),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the filename TextBox from the save form.");
                AutomationElement saveWindowFileNameTextBox = saveWindowFileNameTextBoxRetry.Result;
                if (!saveWindowFileNameTextBox.Patterns.Value.IsSupported) throw  new FlaUIException("FlaUI: The filename TextBox from the save for does not implement the Value pattern");
                saveWindowFileNameTextBox.Patterns.Value.Pattern.SetValue(fileName);

                // Save OK button
                RetryResult<AutomationElement> saveButtonRetry = Retry.WhileNull(() => saveWindow.FindFirstChild(cf => cf.ByAutomationId("1").And(cf.ByControlType(ControlType.Button))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the save button from the save form.");
                Button saveButton = saveButtonRetry.Result.AsButton();
                saveButton.Click();
                #endregion

                // Waits until finished
                if (inUpdateInterface) BusyOverlayBindings.I.SetIndeterminate("SAP2000: Waiting for SAP2000 to populate the s2k file.");
                Thread.Sleep(200);
                RetryResult<bool> waitRetry = Retry.WhileFalse(() =>
                {
                    AutomationElement statusPane = FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByAutomationId("panStatus"));
                    AutomationElement statusText = statusPane.FindFirstChild();
                    return statusText.Name.Trim() == "Ready";
                }, timeout: TimeSpan.FromMinutes(inFinishTimeoutMins), interval: TimeSpan.FromMilliseconds(500), throwOnTimeout: true, ignoreException: true, timeoutMessage: "FlaUI: Sap2000 took too long to finish the export operation.");

                if (!waitRetry.Success) throw new S2KHelperException("Could not export the tables to s2k format text file.");
            }
            finally
            {
                FlaUI_Automation.Dispose();
                FlaUI_Automation = null;
            }
        }

        public void FlaUI_Action_ImportTablesFromS2K(string inFullFileName, Sap2000ImportOptions inImportOptions = null, int inFinishTimeoutMins = 300, bool inUpdateInterface = false)
        {
            if (string.IsNullOrEmpty(inFullFileName)) throw new S2KHelperException("The export to S2K filename must be given.");

            // Handling the input filename
            string fileName = inFullFileName;
            FileInfo fInfo = new FileInfo(fileName);
            if (!fInfo.Exists) throw new S2KHelperException($"The input filename {inFullFileName} could not be found."); ;

            if (inImportOptions == null) inImportOptions = new Sap2000ImportOptions();

            try
            {
                if (inUpdateInterface) BusyOverlayBindings.I.SetIndeterminate("SAP2000: Opening and Setting Import Forms.");

                FlaUI_Automation = new UIA3Automation();
                FlaUI_SapMainWindow.Focus();

                FlaUI_CloseAllSecondaryWindows_RecursiveChildren(FlaUI_SapMainWindow);

                // Opens the Import from S2K Window
                FlaUI_SapMainWindow.Focus();
                FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.ESC);
                using (FlaUI.Core.Input.Keyboard.Pressing(VirtualKeyShort.ALT))
                {
                    FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.KEY_F);
                    FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.KEY_I);
                    FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.KEY_T);
                }

                // Gets the import window
                RetryResult<AutomationElement> importWindowRetry = Retry.WhileNull(() => FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByAutomationId("DBImportForm").And(cf.ByControlType(ControlType.Window))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Import Form.");
                Window importWindow = importWindowRetry.Result.AsWindow();

                // To existing or to new model?
                string newOrExistingAutomationId = inImportOptions.AddToExitingModel ? "optType1" : "optType0";
                RetryResult<AutomationElement> newOrExistingRadioButtonRetry = Retry.WhileNull(() => importWindow.FindFirstDescendant(cf => cf.ByAutomationId(newOrExistingAutomationId).And(cf.ByControlType(ControlType.RadioButton))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the New or Existing Radio Button from the Import Form.");
                RadioButton newOrExistingRadioButton = newOrExistingRadioButtonRetry.Result.AsRadioButton();
                newOrExistingRadioButton.IsChecked = true;

                // Advanced Options
                RetryResult<AutomationElement> advancedButtonRetry = Retry.WhileNull(() => importWindow.FindFirstDescendant(cf => cf.ByAutomationId("cmdAdvanced").And(cf.ByControlType(ControlType.Button))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Advanced Options Button from the Import Form.");
                Button advancedButton = advancedButtonRetry.Result.AsButton();
                advancedButton.Click();

                // Gets the export window
                RetryResult<AutomationElement> advancedImportOptionsWindowRetry = Retry.WhileNull(() => importWindow.FindFirstChild(cf => cf.ByAutomationId("fDBImportOptions").And(cf.ByControlType(ControlType.Window))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Advanced Import Options Form.");
                Window advancedImportOptionsWindow = advancedImportOptionsWindowRetry.Result.AsWindow();

                // Gets the reset all button
                RetryResult<AutomationElement> advancedImportResetAllButtonRetry = Retry.WhileNull(() => advancedImportOptionsWindow.FindFirstChild(cf => cf.ByAutomationId("cmdDefaultsAll").And(cf.ByControlType(ControlType.Button))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Reset All Button of the Advanced Import Options Form.");
                Button advancedImportResetAllButton = advancedImportResetAllButtonRetry.Result.AsButton();
                advancedImportResetAllButton.Click();

                // Gets the tabcontrol
                RetryResult<AutomationElement> advancedImportTabControlRetry = Retry.WhileNull(() => advancedImportOptionsWindow.FindFirstChild(cf => cf.ByAutomationId("TabControl1").And(cf.ByControlType(ControlType.Tab))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Tab Control of the Advanced Import Options Form.");
                Tab advancedImportTabControl = advancedImportTabControlRetry.Result.AsTab();
                TabItem messageControlTabItem = advancedImportTabControl.TabItems[0];
                TabItem itemsWithSameNameTabItem = advancedImportTabControl.TabItems[1];
                TabItem itemsInSameLocationTabItem = advancedImportTabControl.TabItems[2];
                TabItem coordTransformTabItem = advancedImportTabControl.TabItems[3];


                #region Message Control Tab
                messageControlTabItem.IsSelected = true;

                // refreshes the tab control
                advancedImportTabControlRetry = Retry.WhileNull(() => advancedImportOptionsWindow.FindFirstChild(cf => cf.ByAutomationId("TabControl1").And(cf.ByControlType(ControlType.Tab))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Tab Control of the Advanced Import Options Form.");
                advancedImportTabControl = advancedImportTabControlRetry.Result.AsTab();

                // Gets the TabPage
                RetryResult<AutomationElement> messageControlTabPageRetry = Retry.WhileNull(() => advancedImportTabControl.FindFirstChild(cf => cf.ByAutomationId("TabPage1").And(cf.ByControlType(ControlType.Pane))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Message Control Tab Page of the Advanced Import Options Form.");
                AutomationElement messageControlTabPage = messageControlTabPageRetry.Result;

                RetryResult<AutomationElement> errorLimitTextBoxRetry = Retry.WhileNull(() => messageControlTabPage.FindFirstDescendant(cf => cf.ByAutomationId("TextBox5").And(cf.ByControlType(ControlType.Edit))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get Error Limit TextBox of the Message Control Tab Page of the Advanced Import Options Form.");
                TextBox errorLimitTextBox = errorLimitTextBoxRetry.Result.AsTextBox();
                errorLimitTextBox.Text = "1";

                RetryResult<AutomationElement> neverAbortOnWarningsRetry = Retry.WhileNull(() => messageControlTabPage.FindFirstDescendant(cf => cf.ByAutomationId("optWarning1").And(cf.ByControlType(ControlType.RadioButton))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Never Abort on Warnings RadioButton of the Message Control Tab Page of the Advanced Import Options Form.");
                RadioButton neverAbortOnWarnings = neverAbortOnWarningsRetry.Result.AsRadioButton();
                neverAbortOnWarnings.IsChecked = true;

                #endregion

                #region Items with same name tab

                itemsWithSameNameTabItem.IsSelected = true;

                // refreshes the tab control
                advancedImportTabControlRetry = Retry.WhileNull(() => advancedImportOptionsWindow.FindFirstChild(cf => cf.ByAutomationId("TabControl1").And(cf.ByControlType(ControlType.Tab))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Tab Control of the Advanced Import Options Form.");
                advancedImportTabControl = advancedImportTabControlRetry.Result.AsTab();

                RetryResult<AutomationElement> itemsWithSameNameTabPageRetry = Retry.WhileNull(() => advancedImportTabControl.FindFirstChild(cf => cf.ByAutomationId("TabPage2").And(cf.ByControlType(ControlType.Pane))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Items With Same Name Tab Page of the Advanced Import Options Form.");
                AutomationElement itemsWithSameNameTabPage = itemsWithSameNameTabPageRetry.Result;

                // OtherItems - Replace in Model
                RetryResult<AutomationElement> otherItemsRadioButtonRetry = Retry.WhileNull(() => itemsWithSameNameTabPage.FindFirstDescendant(cf => cf.ByAutomationId("optNameThree1").And(cf.ByControlType(ControlType.RadioButton))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Other Items - Replace in Model RadioButton of the Items With Same Name Tab Page of the Advanced Import Options Form.");
                RadioButton otherItemsRadioButton = otherItemsRadioButtonRetry.Result.AsRadioButton();
                otherItemsRadioButton.IsChecked = true;

                // Joint Frame Area... - Replace in Model
                RetryResult<AutomationElement> jointFrameAreaRadioButtonRetry = Retry.WhileNull(() => itemsWithSameNameTabPage.FindFirstDescendant(cf => cf.ByAutomationId("optNameTwo1").And(cf.ByControlType(ControlType.RadioButton))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Joint,Frame,Area... - Replace in Model RadioButton of the Items With Same Name Tab Page of the Advanced Import Options Form.");
                RadioButton jointFrameAreaRadioButton = jointFrameAreaRadioButtonRetry.Result.AsRadioButton();
                jointFrameAreaRadioButton.IsChecked = true;

                #endregion

                #region Items in same location tab

                itemsInSameLocationTabItem.IsSelected = true;

                // refreshes the tab control
                advancedImportTabControlRetry = Retry.WhileNull(() => advancedImportOptionsWindow.FindFirstChild(cf => cf.ByAutomationId("TabControl1").And(cf.ByControlType(ControlType.Tab))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Tab Control of the Advanced Import Options Form.");
                advancedImportTabControl = advancedImportTabControlRetry.Result.AsTab();

                RetryResult<AutomationElement> itemsInSameLocationTabPageRetry = Retry.WhileNull(() => advancedImportTabControl.FindFirstChild(cf => cf.ByAutomationId("TabPage3").And(cf.ByControlType(ControlType.Pane))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Items in Same Location Tab Page of the Advanced Import Options Form.");
                AutomationElement itemsInSameLocationTabPage = itemsInSameLocationTabPageRetry.Result;

                // Joints
                RetryResult<AutomationElement> jointsDuplicatesRadioButtonRetry = Retry.WhileNull(() => itemsInSameLocationTabPage.FindFirstDescendant(cf => cf.ByAutomationId("optLocationJoint1").And(cf.ByControlType(ControlType.RadioButton))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Joints - Allow Duplicates RadioButton of the Items in Same Location Tab Page of the Advanced Import Options Form.");
                RadioButton jointsDuplicatesRadioButton = jointsDuplicatesRadioButtonRetry.Result.AsRadioButton();
                jointsDuplicatesRadioButton.IsChecked = true;

                // Frame Area Solid
                RetryResult<AutomationElement> frameAreaSolidDuplicatesRadioButtonRetry = Retry.WhileNull(() => itemsInSameLocationTabPage.FindFirstDescendant(cf => cf.ByAutomationId("optLocation1").And(cf.ByControlType(ControlType.RadioButton))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Frame, Area, Solid - Allow Duplicates RadioButton of the Items in Same Location Tab Page of the Advanced Import Options Form.");
                RadioButton frameAreaSolidDuplicatesRadioButton = frameAreaSolidDuplicatesRadioButtonRetry.Result.AsRadioButton();
                frameAreaSolidDuplicatesRadioButton.IsChecked = true;

                // Links
                RetryResult<AutomationElement> linksDuplicatesRadioButtonRetry = Retry.WhileNull(() => itemsInSameLocationTabPage.FindFirstDescendant(cf => cf.ByAutomationId("optLocationLink1").And(cf.ByControlType(ControlType.RadioButton))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Links - Allow Duplicates RadioButton of the Items in Same Location Tab Page of the Advanced Import Options Form.");
                RadioButton linksDuplicatesRadioButton = linksDuplicatesRadioButtonRetry.Result.AsRadioButton();
                linksDuplicatesRadioButton.IsChecked = true;

                #endregion

                // Gets the OK Advanced button
                RetryResult<AutomationElement> advancedOKButtonRetry = Retry.WhileNull(() => advancedImportOptionsWindow.FindFirstChild(cf => cf.ByAutomationId("cmdOK").And(cf.ByControlType(ControlType.Button))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the OK Button of the Advanced Import Options Form.");
                Button advancedOKButton = advancedOKButtonRetry.Result.AsButton();
                advancedOKButton.Click();

                // Gets the OK Import button
                RetryResult<AutomationElement> importOKButtonRetry = Retry.WhileNull(() => importWindow.FindFirstDescendant(cf => cf.ByAutomationId("cmdOK").And(cf.ByControlType(ControlType.Button))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the OK Button from the Import Form.");
                Button importOKButton = importOKButtonRetry.Result.AsButton();
                importOKButton.Click();

                #region File Dialog

                // Gets the file window
                RetryResult<AutomationElement> fileWindowRetry = Retry.WhileNull(() => FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByClassName("#32770")),
                    timeout: TimeSpan.FromSeconds(10), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Import Select File Form.");
                Window fileWindow = fileWindowRetry.Result.AsWindow();

                // Manipulates the textbox
                RetryResult<AutomationElement> fileWindowFileNameTextBoxRetry = Retry.WhileNull(() => fileWindow.FindFirstDescendant(cf => cf.ByAutomationId("1148").And(cf.ByControlType(ControlType.Edit))),
                    timeout: TimeSpan.FromSeconds(10), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the filename TextBox from the file form.");
                AutomationElement fileWindowFileNameTextBox = fileWindowFileNameTextBoxRetry.Result;
                if (!fileWindowFileNameTextBox.Patterns.Value.IsSupported) throw new FlaUIException("FlaUI: The filename TextBox from the file form does not implement the Value pattern");
                fileWindowFileNameTextBox.Patterns.Value.Pattern.SetValue(fileName);

                // File Open button
                RetryResult<AutomationElement> openButtonRetry = Retry.WhileNull(() => fileWindow.FindFirstChild(cf => cf.ByAutomationId("1")),
                    timeout: TimeSpan.FromSeconds(10), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the open button from the file form.");
                Button openButton = openButtonRetry.Result.AsButton();
                openButton.Invoke(); // Invoke because it is a SplitButton - not a button.

                #endregion

                // Waits until the dialog is shown
                if (inUpdateInterface) BusyOverlayBindings.I.SetIndeterminate("SAP2000: Waiting for SAP2000 to import the s2k file.");
                Thread.Sleep(200);
                RetryResult<AutomationElement> waitLogFormRetry = Retry.WhileNull(() =>
                {
                    AutomationElement logForm = FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByAutomationId("DBLogForm").And(cf.ByControlType(ControlType.Window)));
                    if (logForm != null && logForm.Patterns.Window.IsSupported && logForm.Patterns.Window.Pattern.WindowInteractionState.Value == WindowInteractionState.ReadyForUserInteraction) return logForm;
                    return null;
                }, timeout: TimeSpan.FromMinutes(inFinishTimeoutMins), interval: TimeSpan.FromMilliseconds(500), throwOnTimeout: true, ignoreException: true, timeoutMessage: "FlaUI: Sap2000 took too long to finish the import operation.");

                if (!waitLogFormRetry.Success) throw new S2KHelperException("Could not import the tables from the s2k format text file.");
                Window logFormOut = waitLogFormRetry.Result.AsWindow();

                // Closes the log dialog
                RetryResult<AutomationElement> logDoneButtonRetry = Retry.WhileNull(() => logFormOut.FindFirstChild(cf => cf.ByAutomationId("cmdDone").And(cf.ByControlType(ControlType.Button))),
                    timeout: TimeSpan.FromSeconds(10), interval: TimeSpan.FromMilliseconds(200), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the done button from the import log form.");
                Button logDoneButton = logDoneButtonRetry.Result.AsButton();
                logDoneButton.Click();


                // Waits until SAP2000 is ready again
                if (inUpdateInterface) BusyOverlayBindings.I.SetIndeterminate("SAP2000: Waiting for SAP2000 to become ready once again.");
                Thread.Sleep(200);
                RetryResult<bool> waitRetry = Retry.WhileFalse(() =>
                {
                    AutomationElement statusPane = FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByAutomationId("panStatus"));
                    AutomationElement statusText = statusPane.FindFirstChild();
                    return statusText.Name.Trim() == "Ready";
                }, timeout: TimeSpan.FromMinutes(inFinishTimeoutMins), interval: TimeSpan.FromMilliseconds(500), throwOnTimeout: true, ignoreException: true, timeoutMessage: "FlaUI: Sap2000 took too long to get ready after the import operation.");

                if (!waitRetry.Success) throw new S2KHelperException("Could not import the tables from the s2k format text file.");
            }
            finally
            {
                FlaUI_Automation.Dispose();
                FlaUI_Automation = null;
            }
        }

        public void FlaUI_Action_CloseAllOtherSAP2000Windows(bool inUpdateInterface = false)
        {
            try
            {
                if (inUpdateInterface) BusyOverlayBindings.I.SetIndeterminate("SAP2000: Closing all SAP2000 windows.");

                FlaUI_Automation = new UIA3Automation();
                FlaUI_SapMainWindow.Focus();

                FlaUI_CloseAllSecondaryWindows_RecursiveChildren(FlaUI_SapMainWindow);
            }
            finally
            {
                FlaUI_Automation.Dispose();
                FlaUI_Automation = null;
            }
        }

        public void FlaUI_Action_Test()
        {
            try
            {
                FlaUI_Automation = new UIA3Automation();
                //FlaUI_SapMainWindow.Focus();

                FlaUI_CloseAllSecondaryWindows_RecursiveChildren(FlaUI_SapMainWindow);

                // Finds the main menu
                RetryResult<AutomationElement> mainMenuRetry = Retry.WhileNull(() => FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByControlType(ControlType.MenuBar)),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Main Menu.");
                Menu mainMenu = mainMenuRetry.Result.AsMenu();
                mainMenu.Items.Find(a => a.Name == "File").Items.Find(b => b.Name == "Export").Items.Find(c => c.Name == "SAP2000 .s2k Text File...").Invoke();


            }
            finally
            {
                FlaUI_Automation.Dispose();
                FlaUI_Automation = null;
            }
        }

        public void FlaUI_Action_FocusSap2000Window()
        {
            bool alreadyStarted = _flaUiAutomation != null;
            try
            {
                if (!alreadyStarted) FlaUI_Automation = new UIA3Automation();

                FlaUI_SapMainWindow.Focus();
            }
            finally
            {
                if (!alreadyStarted)
                {
                    FlaUI_Automation.Dispose();
                    FlaUI_Automation = null;
                }
            }
        }

        public List<(Sap2000ExportTable Table, long TableHeaderLine, long TableEndLine)> FindAllTablesInS2K(string[] inFileLines, bool inUpdateInterface = false)
        {
            if (inUpdateInterface) BusyOverlayBindings.I.SetDeterminate("Finding table positions in S2K file.");

            List<(Sap2000ExportTable Table, long TableHeaderLine, long TableEndLine)> toRet = new List<(Sap2000ExportTable Table, long TableHeaderLine, long TableEndLine)>();

            string currentTableName = null;
            long currentTableStart = -1;

            for (long i = 0; i < inFileLines.LongLength; i++)
            {
                if (inFileLines[i].Length <= 1) // Empty Line
                {
                    if (currentTableName == null) continue;

                    toRet.Add((SapAutoExtensions.TableNameToEnum(currentTableName), currentTableStart, i - 1));

                    if (inUpdateInterface) BusyOverlayBindings.I.UpdateProgress(i, inFileLines.LongLength);

                    currentTableName = null;
                    currentTableStart = -1;

                    continue;
                }

                if (inFileLines[i].StartsWith("T"))
                {
                    int firstQuotationMarks = inFileLines[i].IndexOf('"');
                    int lastQuotationMarks = inFileLines[i].LastIndexOf('"');

                    currentTableName = inFileLines[i].Substring(firstQuotationMarks + 1, lastQuotationMarks - firstQuotationMarks - 1);
                    currentTableStart = i;

                    continue;
                }
            }

            return toRet;
        }

        public DataSet GetDataSetFromS2K(string[] inFileLines, bool inUpdateInterface = false)
        {
            List<(Sap2000ExportTable Table, long TableHeaderLine, long TableEndLine)> tablePositions = FindAllTablesInS2K(inFileLines, inUpdateInterface);

            DataSet ds = new DataSet("SAP2000 Exported Tables");

            foreach ((Sap2000ExportTable Table, long TableHeaderLine, long TableEndLine) tablePosition in tablePositions)
            {
                DataTable dt = GetDataTableFromS2K(inFileLines, tablePosition.Table, tablePosition.TableHeaderLine, tablePosition.TableEndLine, inUpdateInterface);

                ds.Tables.Add(dt);

            }

            ds.AcceptChanges();

            return ds;
        }

        public DataTable GetDataTableFromS2K(string[] inFileLines, Sap2000ExportTable inTable, long inTableHeaderLine, long inTableEndLine, bool inUpdateInterface = false)
        {
            if (inUpdateInterface) BusyOverlayBindings.I.SetDeterminate($"Reading Table {SapAutoExtensions.EnumToString(inTable)} into an in-memory .NET DataTable.");

            DataTable dt = inTable.GetTableFormat();

            Regex lineRegex = new Regex(@"\s*?(?<f>\S+)=(\""(?<v>.*?)\""|(?<v>\S+))\s*?");

            long updateInterfaceEveryXLines = 0, tableLineLength = inTableEndLine - inTableHeaderLine;
            if (tableLineLength <= 300) updateInterfaceEveryXLines = 1;
            else updateInterfaceEveryXLines = tableLineLength / 100;

            string fullLine = "";
            for (long i = inTableHeaderLine + 1; i < inTableEndLine + 1; i++)
            {
                fullLine += inFileLines[i];
                if (fullLine.EndsWith("_"))
                {
                    fullLine = fullLine.TrimEnd(new char[] {'_'});
                    continue;
                }

                DataRow row = dt.NewRow();
                MatchCollection allPairs = lineRegex.Matches(fullLine);
                foreach (Match pair in allPairs)
                {
                    try
                    {
                        row[pair.Groups["f"].Value] = Convert.ChangeType(pair.Groups["v"].Value, dt.Columns[pair.Groups["f"].Value].DataType);
                    }
                    catch (InvalidCastException e)
                    {
                        throw new S2KHelperException($"Could not convert the S2K Data from {pair.Groups["v"].Value} to {dt.Columns[pair.Groups["f"].Value].DataType}. The error occured in line {i} and in column {pair.Groups["f"].Value}.", e);
                    }
                    catch (Exception e)
                    {
                        throw new S2KHelperException($"Could not convert the S2K data to in-memory Data Table.", e);
                    }
                }

                if (inUpdateInterface)
                {
                    if (i % updateInterfaceEveryXLines == 0) BusyOverlayBindings.I.UpdateProgress(i -(inTableHeaderLine + 1), tableLineLength);
                }

                dt.Rows.Add(row);
                fullLine = "";
            }
            dt.AcceptChanges();

            return dt;
        }

        public void WriteDataSetToS2K(string inFullFileName, DataSet inDataSet, bool inUpdateInterface = false)
        {
            if (inUpdateInterface) BusyOverlayBindings.I.SetIndeterminate($"Writing the contents of the DataSet into the S2K File: Preparing...");

            if (string.IsNullOrEmpty(inFullFileName)) throw new S2KHelperException("The output S2K filename must be given.");

            if (inDataSet == null || inDataSet.Tables.Count == 0) throw new S2KHelperException("The DataSet must be given and must contain at least one Table.");

            // Handling the output filename
            string fileName = inFullFileName;
            FileInfo fInfo = new FileInfo(fileName);
            if (fInfo.Exists)
            {
                try
                {
                    fInfo.Delete();
                }
                catch (Exception)
                {
                    throw new S2KHelperException("The target filename already exists and it could not be deleted."); ;
                }
            }

            // Checks if the program control table exists
            if (!inDataSet.Tables.Contains(SapAutoExtensions.EnumToString(Sap2000ExportTable.Program_Control)))
            {
                DataTable progControlTable = Sap2000ExportTable.Program_Control.GetTableFormat();

                DataRow nRow = progControlTable.NewRow();
                nRow["ProgramName"] = "SAP2000";
                nRow["Version"] = S2KModel.SM.VersionText;
                nRow["CurrUnits"] = S2KModel.SM.PresentationUnitsStringFormat;
                progControlTable.Rows.Add(nRow);

                inDataSet.Tables.Add(progControlTable);
            }

            inDataSet.AcceptChanges();

            if (inUpdateInterface) BusyOverlayBindings.I.SetDeterminate($"Writing the contents of the DataSet into the S2K File.", "Table");
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                for (int index = 0; index < inDataSet.Tables.Count; index++)
                {
                    DataTable table = inDataSet.Tables[index];
                    if (inUpdateInterface) BusyOverlayBindings.I.UpdateProgress(index, inDataSet.Tables.Count, table.TableName);

                    foreach (string tableLine in table.DumpToS2KStream())
                    {
                        sw.WriteLine(tableLine);
                    }
                }

                sw.WriteLine("END TABLE DATA");
            }

        }

        public bool FlaUI_Action_OpenFileAndCloseDialog(string inSapFileName)
        {
            try
            {
                FlaUI_Automation = new UIA3Automation();
                FlaUI_SapMainWindow.Focus();
                FlaUI_CloseAllSecondaryWindows_RecursiveChildren(FlaUI_SapMainWindow);

                if (Path.GetExtension(inSapFileName) != ".sdb" && Path.GetExtension(inSapFileName) != ".s2k") return false;

                // Flag that tells to abandon the search for the dialog.
                bool release = false;
                Task windowAutoCloserTask = new Task(() =>
                {
                    Thread.Sleep(200); // Waits to ensure that the file is loading in SAP2000

                    // Waits until the dialog is shown
                    RetryResult<bool> waitLogFormRetry = Retry.WhileFalse(() =>
                    {
                        if (release) return true;

                        // Tries to the get the log form
                        AutomationElement logForm = FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByAutomationId("DBLogForm").And(cf.ByControlType(ControlType.Window)));
                        
                        // Could not get the form - returns false
                        if (logForm == null) return false;

                        if (logForm.Patterns.Window.IsSupported && logForm.Patterns.Window.Pattern.WindowInteractionState.Value == WindowInteractionState.ReadyForUserInteraction)
                        {
                            // Closes the forms
                            RetryResult<AutomationElement> logDoneButtonRetry = Retry.WhileNull(() => logForm.FindFirstChild(cf => cf.ByAutomationId("cmdDone").And(cf.ByControlType(ControlType.Button))),
                                timeout: TimeSpan.FromSeconds(1), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the done button from the import log form.");
                            Button logDoneButton = logDoneButtonRetry.Result.AsButton();
                            logDoneButton.Invoke();

                            return true;
                        }
                        
                        return false;
                    }, timeout: TimeSpan.FromMinutes(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, ignoreException: false, timeoutMessage: "FlaUI: Sap2000 took too long to finish the import operation.");

                });
                windowAutoCloserTask.Start();

                // Tells SAP2000 to open the file - this has the potential of locking if the model summary dialog appears
                S2KModel.SM.OpenFile(inSapFileName);
                release = true;

                // Waits for the closure of the form
                windowAutoCloserTask.Wait();

                return true;
            }
            finally
            {
                FlaUI_Automation.Dispose();
                FlaUI_Automation = null;
            }
        }
        public void FlaUI_Action_AsyncCloseImportSummaryDialog()
        {
            try
            {
                FlaUI_Automation = new UIA3Automation();

                // Waits until the dialog is shown
                RetryResult<AutomationElement> waitLogFormRetry = Retry.WhileNull(() =>
                {
                    AutomationElement logForm = FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByAutomationId("DBLogForm").And(cf.ByControlType(ControlType.Window)));
                    if (logForm != null && logForm.Patterns.Window.IsSupported && logForm.Patterns.Window.Pattern.WindowInteractionState.Value == WindowInteractionState.ReadyForUserInteraction) return logForm;
                    return null;
                }, timeout: TimeSpan.FromMinutes(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, ignoreException: true, timeoutMessage: "FlaUI: Sap2000 took too long to finish the import operation.");

                if (!waitLogFormRetry.Success) throw new S2KHelperException("Could not import the tables from the s2k format text file.");
                Window logFormOut = waitLogFormRetry.Result.AsWindow();


            }
            finally
            {
                FlaUI_Automation.Dispose();
                FlaUI_Automation = null;
            }
        }



        public void FlaUI_Action_OutputTableNamedSetToS2K([NotNull] string inNamedTableSet, [NotNull] string inFullFileName, int inFinishTimeoutMins = 300)
        {
            if (string.IsNullOrEmpty(inNamedTableSet)) throw new ArgumentNullException(nameof(inNamedTableSet));
            if (string.IsNullOrEmpty(inFullFileName)) throw new ArgumentNullException(nameof(inFullFileName));

            // Handling the output filename
            FileInfo fInfo = new FileInfo(inFullFileName);
            if (fInfo.Exists)
            {
                try
                {
                    fInfo.Delete();
                }
                catch (Exception)
                {
                    throw new S2KHelperException("The target filename already exists and it could not be deleted."); ;
                }
            }

            try
            {
                FlaUI_Automation = new UIA3Automation();
                FlaUI_SapMainWindow.Focus();
                FlaUI_CloseAllSecondaryWindows_RecursiveChildren(FlaUI_SapMainWindow);

                // Opens the Export to S2K Window
                FlaUI_SapMainWindow.Focus();

                // Sets the desired output to no results
                FlaUI_SapMainWindow.Focus();

                // Roundtrip is necessary because SAP2000 was ignoring my the first key.
                FlaUI.Core.Input.Keyboard.Type(VirtualKeyShort.ESC);
                using (FlaUI.Core.Input.Keyboard.Pressing(VirtualKeyShort.ALT))
                {
                    FlaUI.Core.Input.Keyboard.Type(VirtualKeyShort.KEY_F);
                }
                FlaUI.Core.Input.Keyboard.Type(VirtualKeyShort.ESC);
                using (FlaUI.Core.Input.Keyboard.Pressing(VirtualKeyShort.ALT))
                {
                    FlaUI.Core.Input.Keyboard.Type(VirtualKeyShort.KEY_F);
                    FlaUI.Core.Input.Keyboard.Type(VirtualKeyShort.KEY_E);
                    FlaUI.Core.Input.Keyboard.Type(VirtualKeyShort.KEY_T);
                }

                //FlaUI_ClickOnSap2000MainMenuItem(new[] {"File", "Export", "SAP2000 .s2k Text File..." });

                // Gets the export window
                RetryResult<AutomationElement> exportWindowRetry = Retry.WhileNull(() => FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByAutomationId("DBTableForm").And(cf.ByControlType(ControlType.Window))),
                    timeout: TimeSpan.FromSeconds(10), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Export Form.");
                Window exportWindow = exportWindowRetry.Result.AsWindow();

                // Finds the Show Named Sets Button _cmdNamedSet_1
                RetryResult<AutomationElement> showNamedSetsButtonRetry = Retry.WhileNull(() => exportWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByAutomationId("_cmdNamedSet_1"))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Show Named Sets Button.");
                Button showNamedSetsButton = showNamedSetsButtonRetry.Result.AsButton();
                showNamedSetsButton.Click();

                // Gets the Select Named Sets Window
                RetryResult<AutomationElement> selectNamedSetWindowRetry = Retry.WhileNull(() => exportWindow.FindFirstChild(cf => cf.ByAutomationId("SelectForm").And(cf.ByControlType(ControlType.Window))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Select Named Set Window.");
                Window selectNamedSetWindow = selectNamedSetWindowRetry.Result.AsWindow();

                // Gets the List of Named Sets from the selection window _lstName_1
                RetryResult<AutomationElement> selectNamedSetListRetry = Retry.WhileNull(() => selectNamedSetWindow.FindFirstDescendant(cf => cf.ByAutomationId("_lstName_1")),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Select Named Set List.");
                ListBox selectNamedSetList = selectNamedSetListRetry.Result.AsListBox();

                // Gets the list item that was requested
                RetryResult<AutomationElement[]> namedTablesListItemRetry = Retry.WhileNull(() => selectNamedSetList.FindAllChildren(cf => cf.ByControlType(ControlType.ListItem)),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Select Named Set List Items.");
                List<ListBoxItem> namedTablesListItems = (from a in namedTablesListItemRetry.Result select a.AsListBoxItem()).ToList();

                // Selects the requested item
                foreach (ListBoxItem namedTablesListItem in namedTablesListItems)
                {
                    if (namedTablesListItem.Name == inNamedTableSet.ToUpper())
                    {
                        namedTablesListItem.Select();
                        break;
                    }
                }

                // Gets the ok button and clicks
                RetryResult<AutomationElement> selectNamedSetWindowOKButtonRetry = Retry.WhileNull(() => selectNamedSetWindow.FindFirstDescendant(cf => cf.ByAutomationId("cmdOK").And(cf.ByControlType(ControlType.Button))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Select Named Set Window OK Button.");
                Button selectNamedSetWindowOKButton = selectNamedSetWindowOKButtonRetry.Result.AsButton();
                selectNamedSetWindowOKButton.Click();

                #region Export
                // Gets the OK button of the export window
                RetryResult<AutomationElement> exportOkButtonRetry = Retry.WhileNull(() => exportWindow.FindFirstChild(cf => cf.ByAutomationId("cmdOK").And(cf.ByControlType(ControlType.Button))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the OK Button of the Export Form.");
                Button exportOkButton = exportOkButtonRetry.Result.AsButton();
                exportOkButton.Click();

                Debug.WriteLine($"{DateTime.Now}: Export window closed.");

                Window saveWindow;
                try
                {
                    // Gets the save window
                    RetryResult<AutomationElement> saveWindowRetry = Retry.WhileNull(() => FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByClassName("#32770")),
                        timeout: TimeSpan.FromSeconds(10), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Save Text File As Form.");
                    saveWindow = saveWindowRetry.Result.AsWindow();
                }
                catch (Exception e)
                {
                    throw e;
                }

                // Manipulates the textbox
                RetryResult<AutomationElement> saveWindowFileNameTextBoxRetry = Retry.WhileNull(() => saveWindow.FindFirstDescendant(cf => cf.ByAutomationId("1001")),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the filename TextBox from the save form.");
                AutomationElement saveWindowFileNameTextBox = saveWindowFileNameTextBoxRetry.Result;
                if (!saveWindowFileNameTextBox.Patterns.Value.IsSupported) throw new FlaUIException("FlaUI: The filename TextBox from the save for does not implement the Value pattern");
                saveWindowFileNameTextBox.Patterns.Value.Pattern.SetValue(inFullFileName);

                // Save OK button
                RetryResult<AutomationElement> saveButtonRetry = Retry.WhileNull(() => saveWindow.FindFirstChild(cf => cf.ByAutomationId("1").And(cf.ByControlType(ControlType.Button))),
                    timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the save button from the save form.");
                Button saveButton = saveButtonRetry.Result.AsButton();
                saveButton.Click();

                Debug.WriteLine($"{DateTime.Now}: Save window closed.");
                #endregion

                // Waits until finished
                RetryResult<bool> waitRetry = Retry.WhileFalse(() =>
                {
                    AutomationElement statusPane = FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByAutomationId("panStatus"));
                    AutomationElement statusText = statusPane.FindFirstChild();
                    return statusText.Name.Trim() == "Ready";
                }, timeout: TimeSpan.FromMinutes(3), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, ignoreException: true, timeoutMessage: "FlaUI: Sap2000 took too long to finish the export operation.");

                if (!waitRetry.Success) throw new S2KHelperException("Could not export the tables to s2k format text file.");
            }
            finally
            {
                FlaUI_Automation.Dispose();
                FlaUI_Automation = null;
            }
        }
        public void FlaUI_Action_ModifyUndeformedGeometry(double inScaleFactor, string inCase, int inMode)
        {
            try
            {
                FlaUI_Automation = new UIA3Automation();
                FlaUI_SapMainWindow.Focus();

                FlaUI_CloseAllSecondaryWindows_RecursiveChildren(FlaUI_SapMainWindow);

                // Sets the desired output to no results
                FlaUI_SapMainWindow.Focus();
                FlaUI.Core.Input.Keyboard.Type(VirtualKeyShort.ESC);
                using (FlaUI.Core.Input.Keyboard.Pressing(VirtualKeyShort.ALT))
                {
                    FlaUI.Core.Input.Keyboard.Type(VirtualKeyShort.KEY_N);
                    FlaUI.Core.Input.Keyboard.Type(VirtualKeyShort.KEY_G);
                }

                // Gets the form
                RetryResult<AutomationElement> modifyFormRetry = Retry.WhileNull(() => FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByAutomationId("fModifyUndeformedGeometry").And(cf.ByControlType(ControlType.Window))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Modify Undeformed Geometry Form.");
                Window modifyWindow = modifyFormRetry.Result.AsWindow();

                RetryResult<AutomationElement> scaleModeShapeRadioButtonRetry = Retry.WhileNull(() => modifyWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.RadioButton).And(cf.ByAutomationId("optGeom2"))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Scale Mode Shape RadioButton.");
                RadioButton scaleModeShapeRadioButton = scaleModeShapeRadioButtonRetry.Result.AsRadioButton();
                scaleModeShapeRadioButton.IsChecked = true;

                RetryResult<AutomationElement> loadCaseComboBoxRetry = Retry.WhileNull(() => modifyWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.ComboBox).And(cf.ByAutomationId("cboCase2"))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Load Case ComboBox.");
                ComboBox loadCaseComboBox = loadCaseComboBoxRetry.Result.AsComboBox();
                loadCaseComboBox.Select(inCase);

                RetryResult<AutomationElement> modeComboBoxRetry = Retry.WhileNull(() => modifyWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.ComboBox).And(cf.ByAutomationId("cboMode"))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Mode ComboBox.");
                ComboBox modeComboBox = modeComboBoxRetry.Result.AsComboBox();
                modeComboBox.Select($"{inMode}");

                RetryResult<AutomationElement> maximumDisplacementTextBoxRetry = Retry.WhileNull(() => modifyWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Edit).And(cf.ByAutomationId("TextBox2"))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Maximum Displacement TextBox.");
                TextBox maximumDisplacementTextBox = maximumDisplacementTextBoxRetry.Result.AsTextBox();
                maximumDisplacementTextBox.Text = $"{inScaleFactor}";

                // Clicks the OK Button
                RetryResult<AutomationElement> okButtonRetry = Retry.WhileNull(() => modifyWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByAutomationId("cmdOK"))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the OK Button of the Modify Undeformed Geometry Form.");
                Button okButton = okButtonRetry.Result.AsButton();
                okButton.Click();
            }
            finally
            {
                FlaUI_Automation.Dispose();
                FlaUI_Automation = null;
            }
        }

        #endregion


        #region FlaUI - Screenshots
        public void FlaUI_Prepare_For_Screenshots(double inScreenshotWidth, double inScreenshotHeight)
        {
            try
            {
                FlaUI_Automation = new UIA3Automation();
                FlaUI_SapMainWindow.Focus();

                FlaUI_CloseAllSecondaryWindows_RecursiveChildren(FlaUI_SapMainWindow);

                // Ensures correct menu display
                FlaUI_SapMainWindow.Focus();
                FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.ESC);
                using (FlaUI.Core.Input.Keyboard.Pressing(VirtualKeyShort.ALT))
                {
                    FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.KEY_O);
                    FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.KEY_R);
                }

                // Closing all other views
                Rectangle r = FlaUI_SapMainWindow.BoundingRectangle;
                System.Drawing.Point viewCloseLocation = new System.Drawing.Point(r.Right - 20, r.Top + 95);
                FlaUI_SapMainWindow.Focus();
                Retry.WhileFalse(() =>
                {
                    IWindowPattern mwPat = FlaUI_SapMainWindow.Patterns.Window.PatternOrDefault;
                    return mwPat?.WindowInteractionState.ValueOrDefault == WindowInteractionState.ReadyForUserInteraction;
                }, TimeSpan.FromSeconds(20), TimeSpan.FromMilliseconds(20), true, true, "The Main Window of SAP2000 did not get back to the ReadyForUserInteraction state");
                Mouse.Click(viewCloseLocation, MouseButton.Left);
                FlaUI_SapMainWindow.Click();


                IWindowPattern mainWindowPattern = FlaUI_SapMainWindow.Patterns.Window.PatternOrDefault;
                mainWindowPattern?.SetWindowVisualState(WindowVisualState.Normal);
                
                ITransformPattern mainWindowTransformPattern = FlaUI_SapMainWindow.Patterns.Transform.PatternOrDefault;
                mainWindowTransformPattern?.Resize(inScreenshotWidth + 45, inScreenshotHeight + 137);

                FlaUI_SapMainWindow.Focus();
                Retry.WhileFalse(() =>
                {
                    IWindowPattern mwPat = FlaUI_SapMainWindow.Patterns.Window.PatternOrDefault;
                    return mwPat?.WindowInteractionState.ValueOrDefault == WindowInteractionState.ReadyForUserInteraction;
                }, TimeSpan.FromSeconds(20), TimeSpan.FromMilliseconds(20), true, true, "The Main Window of SAP2000 did not get back to the ReadyForUserInteraction state");
            }
            finally
            {
                FlaUI_Automation.Dispose();
                FlaUI_Automation = null;
            }
        }

        // The crop rectangle to get only the viewport in SAP2000 (removing the interface).
        private readonly Rectangle _screenShotRectangle = new Rectangle(36, 107, 810, 610);

        public Dictionary<Sap2000ViewDirection, Image> FlaUI_GetSapScreenShot_JointNames(IEnumerable<Sap2000ViewDirection> inDirections)
        {
            try
            {
                FlaUI_Automation = new UIA3Automation();
                FlaUI_SapMainWindow.Focus();

                FlaUI_CloseAllSecondaryWindows_RecursiveChildren(FlaUI_SapMainWindow);

                // Sets the desired output to no results
                FlaUI_SapMainWindow.Focus();
                FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.ESC);
                FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.F4);

                // Configures the view
                FlaUI_SetViewBasic(inJointLabels: true, inViewType: "Extrude", inShowAnalysisModelIfAvailable: true, inJointRestraints: true);

                // Gets the screenshots and return
                return FlaUI_GetScreenshots(inDirections);
            }
            finally
            {
                FlaUI_Automation.Dispose();
                FlaUI_Automation = null;
            }
        }
        public Dictionary<Sap2000ViewDirection, Image> FlaUI_GetSapScreenShot_FrameNames(IEnumerable<Sap2000ViewDirection> inDirections)
        {
            try
            {
                FlaUI_Automation = new UIA3Automation();
                FlaUI_SapMainWindow.Focus();

                FlaUI_CloseAllSecondaryWindows_RecursiveChildren(FlaUI_SapMainWindow);

                // Sets the desired output to no results
                FlaUI_SapMainWindow.Focus();
                FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.ESC);
                FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.F4);

                // Configures the view
                FlaUI_SetViewBasic(inFramesLabels: true, inViewType: "Extrude", inShowAnalysisModelIfAvailable: true, inJointRestraints: true);

                // Gets the screenshots and return
                return FlaUI_GetScreenshots(inDirections);

            }
            finally
            {
                FlaUI_Automation.Dispose();
                FlaUI_Automation = null;
            }
        }
        public Dictionary<Sap2000ViewDirection, Image> FlaUI_GetSapScreenShot_JointReaction(IEnumerable<Sap2000ViewDirection> inDirections, string inCase)
        {
            try
            {
                FlaUI_Automation = new UIA3Automation();
                FlaUI_SapMainWindow.Focus();

                FlaUI_CloseAllSecondaryWindows_RecursiveChildren(FlaUI_SapMainWindow);

                // Sets the desired output to no results
                FlaUI_SapMainWindow.Focus();

                // Configures the basic view
                FlaUI_SetViewBasic(inJointLabels: true, inViewType: "Extrude", inShowAnalysisModelIfAvailable: true, inJointRestraints: true);

                FlaUI_SapMainWindow.Focus();
                FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.ESC);
                FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.F7);

                // Gets the display form view
                RetryResult<AutomationElement> jointReactionsOptionsRetry = Retry.WhileNull(() => FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByName("Display Joint Reactions").And(cf.ByControlType(ControlType.Window))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the joint reactions Form.");
                Window jointReactionsWindow = jointReactionsOptionsRetry.Result.AsWindow();

                RetryResult<AutomationElement> caseComboGroupRetry = Retry.WhileNull(() => jointReactionsWindow.FindFirstChild(cf => cf.ByName("Case/Combo").And(cf.ByControlType(ControlType.Group))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Case/Combo Group.");
                AutomationElement caseComboGroup = caseComboGroupRetry.Result;

                RetryResult<AutomationElement> caseComboComboBoxRetry = Retry.WhileNull(() => caseComboGroup.FindFirstChild(cf => cf.ByControlType(ControlType.ComboBox)),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Case/Combo ComboBox.");
                ComboBox caseComboComboBox = caseComboComboBoxRetry.Result.AsComboBox();
                caseComboComboBox.Select(inCase);

                RetryResult<AutomationElement> arrowsDisplayTypeRadioButtonRetry = Retry.WhileNull(() => jointReactionsWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.RadioButton).And(cf.ByName("Arrows"))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Arrows Display Type RadioButton.");
                RadioButton arrowsDisplayTypeRadioButton = arrowsDisplayTypeRadioButtonRetry.Result.AsRadioButton();
                arrowsDisplayTypeRadioButton.IsChecked = true;

                // Clicks the OK Button
                RetryResult<AutomationElement> okButtonRetry = Retry.WhileNull(() => jointReactionsWindow.FindFirstChild(cf => cf.ByName("OK").And(cf.ByControlType(ControlType.Button))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the OK Button of the joint reactions Form.");
                Button okButton = okButtonRetry.Result.AsButton();
                okButton.Invoke();

                // Gets the screenshots and return
                return FlaUI_GetScreenshots(inDirections);
            }
            finally
            {
                FlaUI_Automation.Dispose();
                FlaUI_Automation = null;
            }

        }
        public Dictionary<Sap2000ViewDirection, Image> FlaUI_GetSapScreenShot_DeformedShape(IEnumerable<Sap2000ViewDirection> inDirections, string inCase, double? inScale = null, string inContour = null, bool inShowUndeformedShadow = false, int? inStepOrMode = null)
        {
            try
            {
                FlaUI_Automation = new UIA3Automation();
                FlaUI_SapMainWindow.Focus();

                FlaUI_CloseAllSecondaryWindows_RecursiveChildren(FlaUI_SapMainWindow);

                // Sets the desired output to no results
                FlaUI_SapMainWindow.Focus();

                // Configures the basic view
                FlaUI_SetViewBasic(inJointLabels: true, inViewType: "Extrude", inShowAnalysisModelIfAvailable: true, inJointRestraints: true);

                FlaUI_SapMainWindow.Focus();
                FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.ESC);
                FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.F6);

                // Gets the display form view
                RetryResult<AutomationElement> deformedShapeOptionsRetry = Retry.WhileNull(() => FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByName("Display Deformed Shape").And(cf.ByControlType(ControlType.Window))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Display Deformed Shape Form.");
                Window deformedShapeOptionsWindow = deformedShapeOptionsRetry.Result.AsWindow();

                // Case
                RetryResult<AutomationElement> caseComboGroupRetry = Retry.WhileNull(() => deformedShapeOptionsWindow.FindFirstChild(cf => cf.ByName("Case/Combo").And(cf.ByControlType(ControlType.Group))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Case/Combo Group.");
                AutomationElement caseComboGroup = caseComboGroupRetry.Result;

                RetryResult<AutomationElement> caseComboComboBoxRetry = Retry.WhileNull(() => caseComboGroup.FindFirstChild(cf => cf.ByControlType(ControlType.ComboBox)),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Case/Combo ComboBox.");
                ComboBox caseComboComboBox = caseComboComboBoxRetry.Result.AsComboBox();
                caseComboComboBox.Select(inCase);

                // Scaling
                RetryResult<AutomationElement> scalingGroupRetry = Retry.WhileNull(() => deformedShapeOptionsWindow.FindFirstChild(cf => cf.ByName("Scaling").And(cf.ByControlType(ControlType.Group))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Scaling Group.");
                AutomationElement scalingGroup = scalingGroupRetry.Result;
                if (inScale.HasValue)
                {
                    RetryResult<AutomationElement> userDefinedRadioButtonRetry = Retry.WhileNull(() => scalingGroup.FindFirstChild(cf => cf.ByControlType(ControlType.RadioButton).And(cf.ByName("User Defined"))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the User Defined RadioButton.");
                    RadioButton userDefinedRadioButton = userDefinedRadioButtonRetry.Result.AsRadioButton();
                    userDefinedRadioButton.IsChecked = true;

                    RetryResult<AutomationElement> scaleTextBoxRetry = Retry.WhileNull(() => scalingGroup.FindFirstChild(cf => cf.ByControlType(ControlType.Edit)),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the scale TextBox.");
                    TextBox scaleTextBox = scaleTextBoxRetry.Result.AsTextBox();
                    scaleTextBox.Text = $"{inScale.Value}";
                }
                else
                {
                    RetryResult<AutomationElement> automaticRadioButtonRetry = Retry.WhileNull(() => scalingGroup.FindFirstChild(cf => cf.ByControlType(ControlType.RadioButton).And(cf.ByName("Automatic"))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Automatic RadioButton.");
                    RadioButton automaticRadioButton = automaticRadioButtonRetry.Result.AsRadioButton();
                    automaticRadioButton.IsChecked = true;
                }

                // step or mode?
                if (inStepOrMode.HasValue)
                {
                    RetryResult<AutomationElement> multiValuedOptionsRetry = Retry.WhileNull(() => deformedShapeOptionsWindow.FindFirstChild(cf => cf.ByName("Multivalued Options").And(cf.ByControlType(ControlType.Group))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Multivalued Options Group.");
                    AutomationElement multiValuedOptions = multiValuedOptionsRetry.Result;

                    RetryResult<AutomationElement[]> multiValueTextBoxesRetry = Retry.WhileNull(() => multiValuedOptions.FindAllChildren(cf => cf.ByControlType(ControlType.Edit)),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Multivalued Options' TextBoxes.");
                    TextBox[] multiValueTextBoxes = multiValueTextBoxesRetry.Result.Select(a => a.AsTextBox()).ToArray();

                    foreach (TextBox tb in multiValueTextBoxes)
                    {
                        if (tb.IsEnabled && !tb.IsOffscreen) tb.Text = $"{inStepOrMode.Value}";
                    }
                }

                RetryResult<AutomationElement> contourGroupRetry = Retry.WhileNull(() => deformedShapeOptionsWindow.FindFirstChild(cf => cf.ByName("Contour Options").And(cf.ByControlType(ControlType.Group))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Contour Options Group.");
                AutomationElement contourGroup = contourGroupRetry.Result;

                RetryResult<AutomationElement> drawContourRetry = Retry.WhileNull(() => contourGroup.FindFirstChild(cf => cf.ByName("Draw Contours on Objects").And(cf.ByControlType(ControlType.CheckBox))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Draw Contours on Objects CheckBox.");
                CheckBox drawContourCheckBox = drawContourRetry.Result.AsCheckBox();

                if (string.IsNullOrWhiteSpace(inContour))
                {
                    drawContourCheckBox.IsChecked = false;
                }
                else
                {
                    drawContourCheckBox.IsChecked = true;
                    RetryResult<AutomationElement> contourRetry = Retry.WhileNull(() => contourGroup.FindFirstChild(cf => cf.ByControlType(ControlType.ComboBox)),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the contour ComboBox.");
                    ComboBox contourComboBox = contourRetry.Result.AsComboBox();
                    contourComboBox.Select(inContour);
                }


                RetryResult<AutomationElement> wireShadowRetry = Retry.WhileNull(() => deformedShapeOptionsWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox).And(cf.ByName("Wire Shadow"))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Wire Shadow CheckBox.");
                CheckBox wireShadowCheckBox = wireShadowRetry.Result.AsCheckBox();
                wireShadowCheckBox.IsChecked = inShowUndeformedShadow;

                // Clicks the OK Button
                RetryResult<AutomationElement> okButtonRetry = Retry.WhileNull(() => deformedShapeOptionsWindow.FindFirstChild(cf => cf.ByName("OK").And(cf.ByControlType(ControlType.Button))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the OK Button of the Display Deformed Shape Form.");
                Button okButton = okButtonRetry.Result.AsButton();
                okButton.Invoke();

                // Gets the screenshots and return
                return FlaUI_GetScreenshots(inDirections);
            }
            finally
            {
                FlaUI_Automation.Dispose();
                FlaUI_Automation = null;
            }

        }
        public Dictionary<Sap2000ViewDirection, Image> FlaUI_GetSapScreenShot_ForceStress(IEnumerable<Sap2000ViewDirection> inDirections, string inCase, string inForceStressName)
        {
            try
            {
                FlaUI_Automation = new UIA3Automation();
                FlaUI_SapMainWindow.Focus();

                FlaUI_CloseAllSecondaryWindows_RecursiveChildren(FlaUI_SapMainWindow);

                // Sets the desired output to no results
                FlaUI_SapMainWindow.Focus();

                // Configures the basic view
                FlaUI_SetViewBasic(inJointLabels: true, inViewType: "Extrude", inShowAnalysisModelIfAvailable: true, inJointRestraints: true);

                FlaUI_SapMainWindow.Focus();
                FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.ESC);
                FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.F8);

                // Gets the display form view
                RetryResult<AutomationElement> forcesStressesRetry = Retry.WhileNull(() => FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByName("Display Frame Forces/Stresses").And(cf.ByControlType(ControlType.Window))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Display Frame Forces/Stresses Form.");
                Window forcesStressesWindow = forcesStressesRetry.Result.AsWindow();

                // Case
                RetryResult<AutomationElement> caseComboGroupRetry = Retry.WhileNull(() => forcesStressesWindow.FindFirstChild(cf => cf.ByName("Case/Combo").And(cf.ByControlType(ControlType.Group))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Case/Combo Group.");
                AutomationElement caseComboGroup = caseComboGroupRetry.Result;

                RetryResult<AutomationElement> caseComboComboBoxRetry = Retry.WhileNull(() => caseComboGroup.FindFirstChild(cf => cf.ByControlType(ControlType.ComboBox)),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Case/Combo ComboBox.");
                ComboBox caseComboComboBox = caseComboComboBoxRetry.Result.AsComboBox();
                caseComboComboBox.Select(inCase);

                // Display Type Group
                RetryResult<AutomationElement> displayTypeGroupRetry = Retry.WhileNull(() => forcesStressesWindow.FindFirstChild(cf => cf.ByName("Display Type").And(cf.ByControlType(ControlType.Group))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Display Type Group.");
                AutomationElement displayTypeGroup = displayTypeGroupRetry.Result;

                // Components groups
                RetryResult<AutomationElement[]> componentsGroupRetry = Retry.WhileNull(() => forcesStressesWindow.FindAllChildren(cf => cf.ByName("Component").And(cf.ByControlType(ControlType.Group))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Component Groups.");
                AutomationElement[] componentsGroups = componentsGroupRetry.Result;

                // Forces
                if (inForceStressName == "Axial Force" ||
                    inForceStressName == "Torsion" ||
                    inForceStressName == "Shear 2-2" ||
                    inForceStressName == "Moment 2-2" ||
                    inForceStressName == "Shear 3-3" ||
                    inForceStressName == "Moment 3-3")
                {
                    // Selects the force
                    RetryResult<AutomationElement> forceRadioButtonRetry = Retry.WhileNull(() => displayTypeGroup.FindFirstChild(cf => cf.ByControlType(ControlType.RadioButton).And(cf.ByName("Force"))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Force RadioButton.");
                    RadioButton forceRadioButton = forceRadioButtonRetry.Result.AsRadioButton();
                    forceRadioButton.IsChecked = true;

                    // Selects the force component
                    RetryResult<AutomationElement> desiredComponentRadioButtonRetry = Retry.WhileNull(() => componentsGroups[0].FindFirstChild(cf => cf.ByControlType(ControlType.RadioButton).And(cf.ByName(inForceStressName))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: $"FlaUI: Could not get the component {inForceStressName} RadioButton.");
                    RadioButton desiredComponentRadioButton = desiredComponentRadioButtonRetry.Result.AsRadioButton();
                    desiredComponentRadioButton.IsChecked = true;
                }
                else if (inForceStressName == "S11" ||
                         inForceStressName == "S12" ||
                         inForceStressName == "S13" ||
                         inForceStressName == "SVM")
                {
                    // Selects the force
                    RetryResult<AutomationElement> stressRadioButtonRetry = Retry.WhileNull(() => displayTypeGroup.FindFirstChild(cf => cf.ByControlType(ControlType.RadioButton).And(cf.ByName("Stress"))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Stress RadioButton.");
                    RadioButton stressRadioButton = stressRadioButtonRetry.Result.AsRadioButton();
                    stressRadioButton.IsChecked = true;

                    // Selects the force component
                    RetryResult<AutomationElement> desiredComponentRadioButtonRetry = Retry.WhileNull(() => componentsGroups[1].FindFirstChild(cf => cf.ByControlType(ControlType.RadioButton).And(cf.ByName(inForceStressName))),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: $"FlaUI: Could not get the component {inForceStressName} RadioButton.");
                    RadioButton desiredComponentRadioButton = desiredComponentRadioButtonRetry.Result.AsRadioButton();
                    desiredComponentRadioButton.IsChecked = true;

                    // Selects the correct option for output
                    RetryResult<AutomationElement> stressPointComboBoxRetry = Retry.WhileNull(() => componentsGroups[1].FindFirstChild(cf => cf.ByControlType(ControlType.ComboBox)),
                        timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: $"FlaUI: Could not get the component Stress Point combobox.");
                    ComboBox stressPointComboBox = stressPointComboBoxRetry.Result.AsComboBox();
                    stressPointComboBox.Select("Stress Max/Min");
                }
                else throw new S2KHelperException($"{inForceStressName} is not a valid component for the Force / Stress plots");


                // Clicks the OK Button
                RetryResult<AutomationElement> okButtonRetry = Retry.WhileNull(() => forcesStressesWindow.FindFirstChild(cf => cf.ByName("OK").And(cf.ByControlType(ControlType.Button))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the OK Button of the Display Frame Forces/Stresses Form.");
                Button okButton = okButtonRetry.Result.AsButton();
                okButton.Invoke();

                // Gets the screenshots and return
                return FlaUI_GetScreenshots(inDirections);
            }
            finally
            {
                FlaUI_Automation.Dispose();
                FlaUI_Automation = null;
            }

        }
        public Dictionary<Sap2000ViewDirection, Image> FlaUI_GetSapScreenShot_CodeCheck(IEnumerable<Sap2000ViewDirection> inDirections)
        {
            try
            {
                FlaUI_Automation = new UIA3Automation();
                FlaUI_SapMainWindow.Focus();

                FlaUI_CloseAllSecondaryWindows_RecursiveChildren(FlaUI_SapMainWindow);

                // Sets the desired output to no results
                FlaUI_SapMainWindow.Focus();

                // Configures the basic view
                FlaUI_SetViewBasic(inJointLabels: true, inViewType: "Extrude", inShowAnalysisModelIfAvailable: true, inJointRestraints: true);

                FlaUI_SapMainWindow.Focus();
                FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.ESC);
                using (FlaUI.Core.Input.Keyboard.Pressing(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT))
                {
                    FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.F5);
                }

                // Gets the display form view
                RetryResult<AutomationElement> displaySteelFormRetry = Retry.WhileNull(() => FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByAutomationId("DisplaySteelForm").And(cf.ByControlType(ControlType.Window))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Display Steel Form.");
                Window displaySteelForm = displaySteelFormRetry.Result.AsWindow();

                RetryResult<AutomationElement> designOutputRadioButtonRetry = Retry.WhileNull(() => displaySteelForm.FindFirstDescendant(cf => cf.ByControlType(ControlType.RadioButton).And(cf.ByAutomationId("_optItem_0"))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Design Output RadioButton.");
                RadioButton designOutputRadioButton = designOutputRadioButtonRetry.Result.AsRadioButton();
                designOutputRadioButton.IsChecked = true;

                RetryResult<AutomationElement> stressComboBoxRetry = Retry.WhileNull(() => displaySteelForm.FindFirstDescendant(cf => cf.ByControlType(ControlType.ComboBox)),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the stress ComboBox.");
                ComboBox stressComboBox = stressComboBoxRetry.Result.AsComboBox();
                stressComboBox.Value = "P-M Ratio Colors & Values";

                // Clicks the OK Button
                RetryResult<AutomationElement> okButtonRetry = Retry.WhileNull(() => displaySteelForm.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByAutomationId("cmdOK"))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the OK Button of the Display Steel Form.");
                Button okButton = okButtonRetry.Result.AsButton();
                okButton.Invoke();

                // Gets the screenshots and return
                return FlaUI_GetScreenshots(inDirections);
            }
            finally
            {
                FlaUI_Automation.Dispose();
                FlaUI_Automation = null;
            }

        }

        // Private because it doesn't have a try to handle the FlaUI - needs to be called from within a function that has it.
        private void FlaUI_SetViewDirection(Sap2000ViewDirection inDirection)
        {
            // Opens the view form
            FlaUI_SapMainWindow.Focus();
            FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.ESC);
            using (FlaUI.Core.Input.Keyboard.Pressing(VirtualKeyShort.SHIFT, VirtualKeyShort.CONTROL))
            {
                FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.F3);
            }

            // Gets the view form
            RetryResult<AutomationElement> viewWindowRetry = Retry.WhileNull(() => FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByName("Set 3D View").And(cf.ByControlType(ControlType.Window))),
                timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Set 3D View Form.");
            Window viewWindow = viewWindowRetry.Result.AsWindow();

            // Gets the groupbox
            RetryResult<AutomationElement> viewDirectionAngleRetry = Retry.WhileNull(() => viewWindow.FindFirstChild(cf => cf.ByName("View Direction Angle").And(cf.ByControlType(ControlType.Group))),
                timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the View Direction Angle group of the Set 3D View Form.");
            AutomationElement viewDirectionAngle = viewDirectionAngleRetry.Result;

            // Gets the Textboxes inside this groupbox
            RetryResult<AutomationElement[]> textBoxesRetry = Retry.WhileNull(() => viewDirectionAngle.FindAllChildren(cf => cf.ByControlType(ControlType.Edit)),
                timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the TextBoxes of the Set 3D View Form.");
            TextBox[] textBoxes = textBoxesRetry.Result.Select(a => a.AsTextBox()).ToArray();

            // The first is the Plan
            // The second is the Elevation
            // The third is the Aperture

            switch (inDirection)
            {
                case Sap2000ViewDirection.Top_Towards_ZNeg:
                    textBoxes[0].Text = "270";
                    textBoxes[1].Text = "90";
                    textBoxes[2].Text = "0";
                    break;

                case Sap2000ViewDirection.Front_Towards_YPos:
                    textBoxes[0].Text = "270";
                    textBoxes[1].Text = "0";
                    textBoxes[2].Text = "0";
                    break;

                case Sap2000ViewDirection.Back_Towards_YNeg:
                    textBoxes[0].Text = "90";
                    textBoxes[1].Text = "0";
                    textBoxes[2].Text = "0";
                    break;

                case Sap2000ViewDirection.Right_Towards_XNeg:
                    textBoxes[0].Text = "0";
                    textBoxes[1].Text = "0";
                    textBoxes[2].Text = "0";
                    break;

                case Sap2000ViewDirection.Left_Towards_XPos:
                    textBoxes[0].Text = "180";
                    textBoxes[1].Text = "0";
                    textBoxes[2].Text = "0";
                    break;

                case Sap2000ViewDirection.Perspective_Top_Front_Edge:
                    textBoxes[0].Text = "270";
                    textBoxes[1].Text = "45";
                    textBoxes[2].Text = "0";
                    break;

                case Sap2000ViewDirection.Perspective_Top_Back_Edge:
                    textBoxes[0].Text = "90";
                    textBoxes[1].Text = "45";
                    textBoxes[2].Text = "0";
                    break;

                case Sap2000ViewDirection.Perspective_Top_Right_Edge:
                    textBoxes[0].Text = "0";
                    textBoxes[1].Text = "45";
                    textBoxes[2].Text = "0";
                    break;

                case Sap2000ViewDirection.Perspective_Top_Left_Edge:
                    textBoxes[0].Text = "180";
                    textBoxes[1].Text = "45";
                    textBoxes[2].Text = "0";
                    break;

                case Sap2000ViewDirection.Perspective_TFR_Corner:
                    textBoxes[0].Text = "315";
                    textBoxes[1].Text = "45";
                    textBoxes[2].Text = "0";
                    break;

                case Sap2000ViewDirection.Perspective_TFL_Corner:
                    textBoxes[0].Text = "225";
                    textBoxes[1].Text = "45";
                    textBoxes[2].Text = "0";
                    break;

                case Sap2000ViewDirection.Perspective_TBR_Corner:
                    textBoxes[0].Text = "45";
                    textBoxes[1].Text = "45";
                    textBoxes[2].Text = "0";
                    break;

                case Sap2000ViewDirection.Perspective_TBL_Corner:
                    textBoxes[0].Text = "135";
                    textBoxes[1].Text = "45";
                    textBoxes[2].Text = "0";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(inDirection), inDirection, null);
            }

            // Gets the OK button
            RetryResult<AutomationElement> okButtonRetry = Retry.WhileNull(() => viewWindow.FindFirstChild(cf => cf.ByName("OK").And(cf.ByControlType(ControlType.Button))),
                timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the OK Button of the Set 3D View Form.");
            Button okButton = okButtonRetry.Result.AsButton();
            okButton.Invoke();

            // Locks until the form can't be found anymore
            Retry.WhileNotNull(() => FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByName("Set 3D View").And(cf.ByControlType(ControlType.Window))),
                timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Took too long to close the view direction form.");

            // Locks until the SAP2000's window is ready
            Retry.WhileFalse(() => FlaUI_SapMainWindow.Patterns.Window.Pattern.WindowInteractionState.Value == WindowInteractionState.ReadyForUserInteraction,
                timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Took too long to close the view direction form.");

            Thread.Sleep(150);
        }
        private Dictionary<Sap2000ViewDirection, Image> FlaUI_GetScreenshots(IEnumerable<Sap2000ViewDirection> inDirections)
        {
            Dictionary<Sap2000ViewDirection, Image> toRet = new Dictionary<Sap2000ViewDirection, Image>();

            foreach (Sap2000ViewDirection sap2000ViewDirection in inDirections)
            {
                // Sets the view direction
                FlaUI_SetViewDirection(sap2000ViewDirection);

                Bitmap src = FlaUI_SapMainWindow.Capture();
                Bitmap target = new Bitmap(_screenShotRectangle.Width, _screenShotRectangle.Height);

                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(src, new Rectangle(0, 0, target.Width, target.Height), _screenShotRectangle, GraphicsUnit.Pixel);
                }

                toRet.Add(sap2000ViewDirection, target);
            }

            return toRet;
        }

        // Private because it doesn't have a try to handle the FlaUI - needs to be called from within a function that has it.
        private void FlaUI_SetViewBasic(
            string inViewType = "Standard",
            string inColorBy = "Sections",
            bool inShowAnalysisModelIfAvailable = false,
            bool inJointLabels = false,
            bool inJointRestraints = false,
            bool inJointSprings = false,
            bool inJointLocalAxes = false,
            bool inJointInvisible = false,
            bool inJointNotInView = false,
            bool inFramesLabels = false,
            bool inFramesSections = false,
            bool inFramesReleases = false,
            bool inFramesLocalAxes = false,
            bool inFramesNotInView = false)
        {
            // Configs the output window
            FlaUI_SapMainWindow.Focus();
            FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.ESC);
            using (FlaUI.Core.Input.Keyboard.Pressing(VirtualKeyShort.CONTROL))
            {
                FlaUI.Core.Input.Keyboard.Press(VirtualKeyShort.KEY_W);
            }
            // Gets the setup view
            RetryResult<AutomationElement> displayOptionsRetry = Retry.WhileNull(() => FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByName("Display Options").And(cf.ByControlType(ControlType.Window))),
                timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Display Options Form.");
            Window displayOptionsWindow = displayOptionsRetry.Result.AsWindow();

            #region Handling the Object Options Tab

            RetryResult<AutomationElement> objectOptionsTabRetry = Retry.WhileNull(() => FlaUI_SapMainWindow.FindFirstDescendant(cf => cf.ByName("Object Options").And(cf.ByControlType(ControlType.TabItem))),
                timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Object Options TabItem.");
            TabItem objectOptionsTab = objectOptionsTabRetry.Result.AsTabItem();
            objectOptionsTab.Select();

            // Gets the Joints group
            RetryResult<AutomationElement> jointsGroupRetry = Retry.WhileNull(() => objectOptionsTab.FindFirstChild(cf => cf.ByName("Joints").And(cf.ByControlType(ControlType.Group))),
                timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Joints Group.");
            AutomationElement jointsGroup = jointsGroupRetry.Result;

            // Gets the joint options
            RetryResult<AutomationElement[]> allJointOptionsRetry = Retry.WhileNull(() => jointsGroup.FindAllChildren(cf => cf.ByControlType(ControlType.CheckBox)),
                timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Joints' options.");
            CheckBox[] allJointOptions = allJointOptionsRetry.Result.Select(a => a.AsCheckBox()).ToArray();

            allJointOptions[0].IsChecked = inJointLabels;
            allJointOptions[1].IsChecked = inJointRestraints;
            allJointOptions[2].IsChecked = inJointSprings;
            allJointOptions[3].IsChecked = inJointLocalAxes;
            allJointOptions[4].IsChecked = inJointInvisible;
            allJointOptions[5].IsChecked = inJointNotInView;
            
            // Gets the Frames group
            RetryResult<AutomationElement> framesGroupRetry = Retry.WhileNull(() => objectOptionsTab.FindFirstChild(cf => cf.ByName("Frames").And(cf.ByControlType(ControlType.Group))),
                timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Frames Group.");
            AutomationElement framesGroup = framesGroupRetry.Result;

            // Gets the frames options
            RetryResult<AutomationElement[]> allFrameOptionsRetry = Retry.WhileNull(() => framesGroup.FindAllChildren(cf => cf.ByControlType(ControlType.CheckBox)),
                timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Frames' options.");
            CheckBox[] allFrameOptions = allFrameOptionsRetry.Result.Select(a => a.AsCheckBox()).ToArray();

            allFrameOptions[0].IsChecked = inFramesLabels;
            allFrameOptions[1].IsChecked = inFramesSections;
            allFrameOptions[2].IsChecked = inFramesReleases;
            allFrameOptions[3].IsChecked = inFramesLocalAxes;
            allFrameOptions[4].IsChecked = inFramesNotInView;
            #endregion


            #region Handles the General Options Tab
            RetryResult<AutomationElement> generalOptionsTabRetry = Retry.WhileNull(() => FlaUI_SapMainWindow.FindFirstDescendant(cf => cf.ByName("General Options").And(cf.ByControlType(ControlType.TabItem))),
                timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the General Options TabItem.");
            TabItem generalOptionsTab = generalOptionsTabRetry.Result.AsTabItem();
            generalOptionsTab.Select();
            
            // Selects colouring
            RetryResult<AutomationElement> colorByRetry = Retry.WhileNull(() => generalOptionsTab.FindFirstDescendant(cf => cf.ByName(inColorBy).And(cf.ByControlType(ControlType.RadioButton))),
                timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: $"FlaUI: Could not get the Color by RadioButton given by {inColorBy}.");
            RadioButton colorBy = colorByRetry.Result.AsRadioButton();
            colorBy.IsChecked = true;

            // Selects View Type
            RetryResult<AutomationElement> viewTypeRetry = Retry.WhileNull(() => generalOptionsTab.FindFirstDescendant(cf => cf.ByName(inViewType).And(cf.ByControlType(ControlType.RadioButton))),
                timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: $"FlaUI: Could not get the view type RadioButton given by {inColorBy}.");
            RadioButton viewType = viewTypeRetry.Result.AsRadioButton();
            viewType.IsChecked = true;

            // Marks the show available model
            RetryResult<AutomationElement> showAnalysisModelRetry = Retry.WhileNull(() => generalOptionsTab.FindFirstDescendant(cf => cf.ByName("Show Analysis Model (If Available)").And(cf.ByControlType(ControlType.CheckBox))),
                timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the Show Analysis Model (If Available) CheckBox.");
            CheckBox showAnalysisModel = showAnalysisModelRetry.Result.AsCheckBox();
            showAnalysisModel.IsChecked = inShowAnalysisModelIfAvailable;

            #endregion

            // Clicks the OK Button
            RetryResult<AutomationElement> okButtonRetry = Retry.WhileNull(() => displayOptionsWindow.FindFirstChild(cf => cf.ByName("OK").And(cf.ByControlType(ControlType.Button))),
                timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get the OK Button of the Display Options Form.");
            Button okButton = okButtonRetry.Result.AsButton();
            okButton.Invoke();
        }

        private void FlaUI_ClickOnSap2000MainMenuItem(string[] inMenuNames)
        {
            // Gets the toolstrip menu
            RetryResult<AutomationElement> mainMenuRetry = Retry.WhileNull(() => FlaUI_SapMainWindow.FindFirstChild(cf => cf.ByControlType(ControlType.MenuBar)),
                timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: "FlaUI: Could not get Sap2000's Main Menu.");
            AutomationElement currentBase = mainMenuRetry.Result;

            for (int index = 0; index < inMenuNames.Length; index++)
            {
                string menuName = inMenuNames[index];

                RetryResult<AutomationElement> itemRetry = Retry.WhileNull(() => currentBase.FindFirstChild(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName(menuName))),
                    timeout: TimeSpan.FromSeconds(2), interval: TimeSpan.FromMilliseconds(50), throwOnTimeout: true, timeoutMessage: $"FlaUI: Could not get Sap2000's Main Menu {menuName} item.");

                currentBase = itemRetry.Result;

                // Goes cascading-clicking to open
                MenuItem item = currentBase.AsMenuItem();

                if (index == inMenuNames.Length - 1) // Last item
                {
                    item.Focus();
                    Keyboard.Type(VirtualKeyShort.ENTER);
                }
                else
                {
                    item.Invoke();
                }
            }
   
        }
        #endregion
    }

    public enum Sap2000ViewDirection
    {
        Top_Towards_ZNeg = 1,
        Front_Towards_YPos = 2,
        Back_Towards_YNeg = 4,
        Right_Towards_XNeg = 8,
        Left_Towards_XPos = 16,

        Perspective_Top_Front_Edge = 32,
        Perspective_Top_Back_Edge = 64,
        Perspective_Top_Right_Edge = 128,
        Perspective_Top_Left_Edge = 256,

        Perspective_TFR_Corner = 512,
        Perspective_TFL_Corner = 1024,
        Perspective_TBR_Corner = 2048,
        Perspective_TBL_Corner = 4096,
    }

    public class Sap2000ExportOptions
    {
        public List<string> LoadCasesAndCombos { get; set; } = null;
        public List<string> LoadPatterns { get; set; } = null;
        public bool SelectionOnly { get; set; } = false;
        public Point3D BaseReactionsLocation { get; set; } = new Point3D(0.0, 0.0, 0.0);
        public Sap2000OutResultsOptions NonLinearStaticResults { get; set; } = Sap2000OutResultsOptions.LastStep;
        public Sap2000OutResultsOptions MultiStepStaticResults { get; set; } = Sap2000OutResultsOptions.LastStep;
        public Sap2000OutLoadCombos LoadCombos { get; set; } = Sap2000OutLoadCombos.Envelopes;

        public enum Sap2000OutResultsOptions
        {
            Envelopes,
            StepByStep,
            LastStep
        }

        public enum Sap2000OutLoadCombos
        {
            Envelopes,
            Correspondance,
            MultipleValuesIfPossible
        }
    }

    public class Sap2000ImportOptions
    {
        public bool AddToExitingModel { get; set; } = true;

    }

    public class Sap2000AutomatorTableExportData
    {
        public Sap2000AutomatorTableExportData(Sap2000ExportTable inExportTableGroup)
        {
            _exportTable = inExportTableGroup;

            switch (inExportTableGroup)
            {
                case Sap2000ExportTable.Program_Control:
                    _baseTreeItem = Sap2000ExportTableBaseTreeItem.ModelDefinition;
                    _mainTreeItem = Sap2000ExportTableMainTreeItem.System_Data;
                    _secondaryTreeItem = Sap2000ExportTableSecondaryTreeItem.Control_Data;
                    break;

                case Sap2000ExportTable.Groups_1_MINUS_Definitions:
                case Sap2000ExportTable.Groups_2_MINUS_Assignments:
                case Sap2000ExportTable.Groups_3_MINUS_Masses_and_Weights:
                    _baseTreeItem = Sap2000ExportTableBaseTreeItem.ModelDefinition;
                    _mainTreeItem = Sap2000ExportTableMainTreeItem.Other_Definitions;
                    _secondaryTreeItem = Sap2000ExportTableSecondaryTreeItem.Group_Data;
                    break;

                case Sap2000ExportTable.Frame_Section_Assignments:
                case Sap2000ExportTable.Frame_Property_Modifiers:
                case Sap2000ExportTable.Frame_Release_Assignments_1_MINUS_General:
                case Sap2000ExportTable.Frame_Release_Assignments_2_MINUS_Partial_Fixity:
                case Sap2000ExportTable.Frame_Local_Axes_Assignments_1_MINUS_Typical:
                case Sap2000ExportTable.Frame_Local_Axes_Assignments_2_MINUS_Advanced:
                case Sap2000ExportTable.Frame_Insertion_Point_Assignments:
                case Sap2000ExportTable.Frame_Bridge_Object_Flags:
                case Sap2000ExportTable.Frame_Tension_And_Compression_Limits:
                    _baseTreeItem = Sap2000ExportTableBaseTreeItem.ModelDefinition;
                    _mainTreeItem = Sap2000ExportTableMainTreeItem.Frame_Assignments;
                    _secondaryTreeItem = Sap2000ExportTableSecondaryTreeItem.Frame_Item_Assignments;
                    break;

                case Sap2000ExportTable.Joint_Coordinates:
                    _baseTreeItem = Sap2000ExportTableBaseTreeItem.ModelDefinition;
                    _mainTreeItem = Sap2000ExportTableMainTreeItem.Connectivity_Data;
                    _secondaryTreeItem = Sap2000ExportTableSecondaryTreeItem.Joint_Coordinates;
                    break;

                case Sap2000ExportTable.Connectivity_MINUS_Frame:
                case Sap2000ExportTable.Connectivity_MINUS_Cable:
                    _baseTreeItem = Sap2000ExportTableBaseTreeItem.ModelDefinition;
                    _mainTreeItem = Sap2000ExportTableMainTreeItem.Connectivity_Data;
                    _secondaryTreeItem = Sap2000ExportTableSecondaryTreeItem.Object_Connectivity;
                    break;


                case Sap2000ExportTable.Joint_Displacements:
                    _baseTreeItem = Sap2000ExportTableBaseTreeItem.AnalysisResults;
                    _mainTreeItem = Sap2000ExportTableMainTreeItem.Joint_Output;
                    _secondaryTreeItem = Sap2000ExportTableSecondaryTreeItem.Displacements;
                    break;

                case Sap2000ExportTable.Joint_Reactions:
                    _baseTreeItem = Sap2000ExportTableBaseTreeItem.AnalysisResults;
                    _mainTreeItem = Sap2000ExportTableMainTreeItem.Joint_Output;
                    _secondaryTreeItem = Sap2000ExportTableSecondaryTreeItem.Reactions;
                    break;

                case Sap2000ExportTable.Assembled_Joint_Masses:
                    _baseTreeItem = Sap2000ExportTableBaseTreeItem.AnalysisResults;
                    _mainTreeItem = Sap2000ExportTableMainTreeItem.Joint_Output;
                    _secondaryTreeItem = Sap2000ExportTableSecondaryTreeItem.Joint_Masses;
                    break;

                case Sap2000ExportTable.Element_Forces_MINUS_Frames:
                case Sap2000ExportTable.Element_Stresses_MINUS_Frames:
                case Sap2000ExportTable.Element_Joint_Forces_MINUS_Frames:
                case Sap2000ExportTable.Frame_Hinge_States:
                    _baseTreeItem = Sap2000ExportTableBaseTreeItem.AnalysisResults;
                    _mainTreeItem = Sap2000ExportTableMainTreeItem.Element_Output;
                    _secondaryTreeItem = Sap2000ExportTableSecondaryTreeItem.Frame_Output;
                    break;

                case Sap2000ExportTable.Objects_And_Elements_MINUS_Joints:
                case Sap2000ExportTable.Objects_And_Elements_MINUS_Frames:
                    _baseTreeItem = Sap2000ExportTableBaseTreeItem.AnalysisResults;
                    _mainTreeItem = Sap2000ExportTableMainTreeItem.Element_Output;
                    _secondaryTreeItem = Sap2000ExportTableSecondaryTreeItem.Objects_and_Elements;
                    break;

                case Sap2000ExportTable.Base_Reactions:
                    _baseTreeItem = Sap2000ExportTableBaseTreeItem.AnalysisResults;
                    _mainTreeItem = Sap2000ExportTableMainTreeItem.Structure_Output;
                    _secondaryTreeItem = Sap2000ExportTableSecondaryTreeItem.Base_Reactions;
                    break;

                case Sap2000ExportTable.Modal_Periods_And_Frequencies:
                case Sap2000ExportTable.Modal_Load_Participation_Ratios:
                case Sap2000ExportTable.Modal_Participating_Mass_Ratios:
                case Sap2000ExportTable.Modal_Participation_Factors:
                    _baseTreeItem = Sap2000ExportTableBaseTreeItem.AnalysisResults;
                    _mainTreeItem = Sap2000ExportTableMainTreeItem.Structure_Output;
                    _secondaryTreeItem = Sap2000ExportTableSecondaryTreeItem.Modal_Information;
                    break;

                case Sap2000ExportTable.Material_Properties_01_MINUS_General:
                case Sap2000ExportTable.Material_Properties_02_MINUS_Basic_Mechanical_Properties:
                case Sap2000ExportTable.Material_Properties_03a_MINUS_Steel_Data:
                    _baseTreeItem = Sap2000ExportTableBaseTreeItem.ModelDefinition;
                    _mainTreeItem = Sap2000ExportTableMainTreeItem.Property_Definitions;
                    _secondaryTreeItem = Sap2000ExportTableSecondaryTreeItem.Material_Properties;
                    break;

                case Sap2000ExportTable.Frame_Section_Properties_01_MINUS_General:
                    _baseTreeItem = Sap2000ExportTableBaseTreeItem.ModelDefinition;
                    _mainTreeItem = Sap2000ExportTableMainTreeItem.Property_Definitions;
                    _secondaryTreeItem = Sap2000ExportTableSecondaryTreeItem.Frame_Section_Properties;
                    break;

                case Sap2000ExportTable.Cable_Section_Definitions:
                    _baseTreeItem = Sap2000ExportTableBaseTreeItem.ModelDefinition;
                    _mainTreeItem = Sap2000ExportTableMainTreeItem.Property_Definitions;
                    _secondaryTreeItem = Sap2000ExportTableSecondaryTreeItem.Cable_Properties;
                    break;

                case Sap2000ExportTable.Cable_Section_Assignments:
                    _baseTreeItem = Sap2000ExportTableBaseTreeItem.ModelDefinition;
                    _mainTreeItem = Sap2000ExportTableMainTreeItem.Cable_Assignments;
                    _secondaryTreeItem = Sap2000ExportTableSecondaryTreeItem.Cable_Item_Assignments;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(inExportTableGroup), inExportTableGroup, null);
            }
        }

        private Sap2000ExportTable _exportTable;
        public string ExportTable
        {
            get => SapAutoExtensions.EnumToString(_exportTable);
        }

        private Sap2000ExportTableBaseTreeItem _baseTreeItem;
        public string BaseTreeItem
        {
            get => SapAutoExtensions.EnumToString(_baseTreeItem);
        }
        public Sap2000ExportTableBaseTreeItem BaseTreeItemEnumValue
        {
            get => _baseTreeItem;
        }

        private Sap2000ExportTableMainTreeItem _mainTreeItem;
        public string MainTreeItem
        {
            get => SapAutoExtensions.EnumToString(_mainTreeItem);
        }

        private Sap2000ExportTableSecondaryTreeItem _secondaryTreeItem;
        public string SecondaryTreeItem
        {
            get => SapAutoExtensions.EnumToString(_secondaryTreeItem);
        }
    }

    public enum Sap2000ExportTableBaseTreeItem
    {
        ModelDefinition,
        AnalysisResults
    }

    public enum Sap2000ExportTableMainTreeItem
    {
        // Model Definition
        System_Data,
        Property_Definitions,
        Load_Pattern_Definitions,
        Other_Definitions,
        Load_Case_Definitions,
        Bridge_Data,
        Connectivity_Data,
        Joint_Assignments,
        Frame_Assignments,
        Cable_Assignments,
        Tendon_Assignments,
        Area_Assignments,
        Solid_Assignments,
        Link_Assignments,
        OptionsSLASHPreferences_Data,
        Miscellaneous_Data,

        // Analysis Results
        Run_Information,
        Joint_Output,
        Element_Output,
        Structure_Output
    }

    public enum Sap2000ExportTableSecondaryTreeItem
    {

        #region Model Definition

        // System Data
        Control_Data,
        Coordinate_Systems_and_Grids,

        // Property Definitions
        Material_Properties,
        Frame_Section_Properties,
        Cable_Properties,
        Hinge_Properties,
        Area_Section_Properties,
        Solid_Properties,
        Link_Properties,
        Section_Designer_Properties,
        Rebar_Sizes,
        Named_Property_Sets,

        // Load Pattern Definitions

        // Other Definitions
        Constraint_Definitions,
        Group_Data,

        // Load Case Definitions
        Load_Case_Definitions,
        Static_Case_Data,
        Multistep_Static_Case_Data,
        Modal_Case_Data,
        Response_Spectrum_Case_Data,
        Modal_Distory_Case_Data,

        // Connectivity Data
        Joint_Coordinates,
        Object_Connectivity,

        // Joint Assignments
        Joint_Item_Assignments,
        Joint_Load_Assignments,
        Joint_Design_Assignments,

        // Frame Assignments
        Frame_Item_Assignments,
        Frame_Load_Assignments,
        Frame_Design_Assignments,
        Frame_Rating_Assignments,


        // Cable Assignments
        Cable_Item_Assignments,
        Cable_Load_Assignments,

        // Tendon Assignments

        // Area Assignments
        Area_Item_Assignments,
        Area_Load_Assignments,

        // Solid Assignments

        // Link Assignments
        Link_Item_Assignments,
        Link_Load_Assignments,

        #endregion

        #region Analysis Results

        // Joint Output
        Displacements,
        Reactions,
        Joint_Masses,

        // Element Output
        Frame_Output,
        Objects_and_Elements,

        // Structure Output
        Base_Reactions,
        Modal_Information,
        Other_Output_Items

        #endregion
    }

    public enum Sap2000ExportTable
    {
        Program_Control,

        // Property Definitions - Material Properties
        Material_Properties_01_MINUS_General,
        Material_Properties_02_MINUS_Basic_Mechanical_Properties,
        Material_Properties_03a_MINUS_Steel_Data,

        // Property Definitions - Frame Section Properties
        Frame_Section_Properties_01_MINUS_General,

        // Property Definitions - Cable Properties
        Cable_Section_Definitions,

        // Other Definitions
        Groups_1_MINUS_Definitions,
        Groups_2_MINUS_Assignments,
        Groups_3_MINUS_Masses_and_Weights,

        // Connectivity Data
        Joint_Coordinates,
        Connectivity_MINUS_Frame,
        Connectivity_MINUS_Cable,

        // Frame Item Assignments
        Frame_Section_Assignments,
        Frame_Property_Modifiers,
        Frame_Release_Assignments_1_MINUS_General,
        Frame_Release_Assignments_2_MINUS_Partial_Fixity,
        Frame_Local_Axes_Assignments_1_MINUS_Typical,
        Frame_Local_Axes_Assignments_2_MINUS_Advanced,
        Frame_Insertion_Point_Assignments,
        Frame_Bridge_Object_Flags,
        Frame_Tension_And_Compression_Limits,

        // Cable Item Assignments
        Cable_Section_Assignments,

        // RESULTS
        Joint_Displacements,
        Joint_Reactions,
        Assembled_Joint_Masses,
        Element_Forces_MINUS_Frames,
        Element_Stresses_MINUS_Frames,
        Element_Joint_Forces_MINUS_Frames,
        Frame_Hinge_States,
        Objects_And_Elements_MINUS_Joints,
        Objects_And_Elements_MINUS_Frames,
        Base_Reactions,
        Modal_Periods_And_Frequencies,
        Modal_Load_Participation_Ratios,
        Modal_Participating_Mass_Ratios,
        Modal_Participation_Factors
    }
    
    public enum FrameAssignmentTable
    {
        FrameCurveData = 26,
        FrameSectionAssignments = 42,
        FramePropertyModifiers = 58,
        FrameReleaseAssignments1General = 74,
        FrameReleaseAssignments2PartialFixity = 90,
        FrameLocalAxesAssignments1Typical = 106,
        FrameLocalAxesAssignments2Advanced = 122,
        FrameInsertionPointAssignments = 138,
        FrameOffsetAlongLengthAssignments = 154,
        FrameEndSkewAngleAssignments = 170,
        FrameOutputStationAssignments = 186,
        FrameSpringAssignments = 202,
        FrameAddedMassAssignments = 218,
        FrameHingeAssigns01Overview = 234,
        FrameHingeAssigns02UserDefinedProperties = 250,
        FrameHingeAssigns03AutoCaltransFlexuralHinge = 266,
        FrameHingeAssigns04 = 282,
        FrameHingeAssigns05 = 298,
        FrameHingeAssigns06 = 314,
        FrameHingeAssigns07 = 330,
        FrameHingeAssigns08 = 346,
        FrameHingeAssigns09HingeOverwrites = 362,
        FrameHingeAssigns10 = 378,
        FrameHingeAssigns11 = 394,
        FrameHingeAssigns12 = 410,
        FrameHingeAssigns13 = 426,
        FrameHingeAssigns14 = 442,
        FrameHingeAssigns15 = 458,
        FrameHingeAssigns16 = 474,
        FrameTensionAndCompressionLimits = 490,
        FramePDeltaForceAssignments = 506,
        FrameMaterialTemperatures = 522,
        FrameAutoMeshAssignments = 538
    }

    public static class SapAutoExtensions
    {
        public static readonly int ExpandShiftX = -25;
        public static readonly int ExpandShiftY = 8;

        public static readonly int SelectShiftX = -10;
        public static readonly int SelectShiftY = 7;

        public static string EnumToString(Enum inEnum)
        {
            string str = inEnum.ToString();
            str = str.Replace("SLASH", "/");
            str = str.Replace("MINUS", "-");
            str = str.Replace('_', ' ');

            if (inEnum is Sap2000ExportTable)
            {
                str = "Table:  " + str;
            }

            return str;
        }
        public static Sap2000ExportTable TableNameToEnum(string inString)
        {
            TextInfo ti = new CultureInfo("en-US", false).TextInfo;
            string str = ti.ToLower(inString);
            str = ti.ToTitleCase(str);
            str = str.Replace(' ', '_').Replace("-", "MINUS").Replace("/", "SLASH");

            if (Enum.TryParse(str, out Sap2000ExportTable eVal))
            {
                return eVal;
            }
            else
            {
                // Perhaps the text has issues with the lowercase;uppercase of words such as of, and, API, etc.
            }
            
            throw new S2KHelperException($"Could not convert {inString} to a {typeof(Sap2000ExportTable)}. String attempted: {str}.");
        }

        public static DataTable GetTableFormat(this Sap2000ExportTable inSap2000ExportTable)
        {
            /* GET IT FROM ACCESS -Requires Reference: Microsoft Office Access Database Engine...
            Private Sub ShowTableFields()

                Dim db As Database
                Dim tdf As TableDef
                Dim x As Integer

                Set db = CurrentDb

                For Each tdf In db.TableDefs
                    If Left(tdf.Name, 4) <> "MSys" Then ' Don't enumerate the system tables
                        Debug.Print "// TABLE NAME: " & tdf.Name

                        For x = 0 To tdf.Fields.Count - 1
                            Dim colType As String
                            If tdf.Fields(x).Type = 7 Then
                                colType = "double"
                            ElseIf tdf.Fields(x).Type = 10 Then
                                colType = "string"
                            End If
                            'dt.Columns.Add("ColName", typeof(string));
                            Debug.Print "dt.Columns.Add(""" & tdf.Fields(x).Name & """,typeof(" & colType & "));"
                        Next x

                        Debug.Print " "
                    End If
                Next tdf
            End Sub
             */
            DataTable dt = new DataTable(EnumToString(inSap2000ExportTable));

            switch (inSap2000ExportTable)
            {
                case Sap2000ExportTable.Joint_Coordinates:
                    // TABLE NAME: Joint Coordinates
                    dt.Columns.Add("Joint", typeof(string));
                    dt.Columns.Add("CoordSys", typeof(string));
                    dt.Columns.Add("CoordType", typeof(string));
                    dt.Columns.Add("XorR", typeof(double));
                    dt.Columns.Add("Y", typeof(double));
                    dt.Columns.Add("T", typeof(double));
                    dt.Columns.Add("Z", typeof(double));
                    dt.Columns.Add("SpecialJt", typeof(string));
                    dt.Columns.Add("GlobalX", typeof(double));
                    dt.Columns.Add("GlobalY", typeof(double));
                    dt.Columns.Add("GlobalZ", typeof(double));
                    dt.Columns.Add("TargetGX", typeof(double));
                    dt.Columns.Add("TargetGY", typeof(double));
                    dt.Columns.Add("TargetGZ", typeof(double));
                    dt.Columns.Add("TargetCase", typeof(string));
                    dt.Columns.Add("TargetStage", typeof(string));
                    dt.Columns.Add("TargetSF", typeof(double));
                    dt.Columns.Add("OriginalGX", typeof(double));
                    dt.Columns.Add("OriginalGY", typeof(double));
                    dt.Columns.Add("OriginalGZ", typeof(double));
                    dt.Columns.Add("SModeCase", typeof(string));
                    dt.Columns.Add("SModeNumber", typeof(string));
                    dt.Columns.Add("SModeDispl", typeof(double));
                    dt.Columns.Add("ModCSys", typeof(string));
                    dt.Columns.Add("ModDirX", typeof(string));
                    dt.Columns.Add("ModDirY", typeof(string));
                    dt.Columns.Add("ModDirZ", typeof(string));
                    dt.Columns.Add("GUID", typeof(string));
                    break;

                case Sap2000ExportTable.Connectivity_MINUS_Frame:
                    // TABLE NAME: Connectivity - Frame
                    dt.Columns.Add("Frame", typeof(string));
                    dt.Columns.Add("JointI", typeof(string));
                    dt.Columns.Add("JointJ", typeof(string));
                    dt.Columns.Add("IsCurved", typeof(string));
                    dt.Columns.Add("Length", typeof(double));
                    dt.Columns.Add("CentroidX", typeof(double));
                    dt.Columns.Add("CentroidY", typeof(double));
                    dt.Columns.Add("CentroidZ", typeof(double));
                    dt.Columns.Add("GUID", typeof(string));
                    break;

                case Sap2000ExportTable.Connectivity_MINUS_Cable:
                    // TABLE NAME: Connectivity - Cable
                    dt.Columns.Add("Cable", typeof(string));
                    dt.Columns.Add("JointI", typeof(string));
                    dt.Columns.Add("JointJ", typeof(string));
                    dt.Columns.Add("Length", typeof(double));
                    dt.Columns.Add("GUID", typeof(string));
                    break;

                case Sap2000ExportTable.Frame_Section_Assignments:
                    // TABLE NAME: Frame Section Assignments
                    dt.Columns.Add("Frame", typeof(string));
                    dt.Columns.Add("SectionType", typeof(string));
                    dt.Columns.Add("AutoSelect", typeof(string));
                    dt.Columns.Add("AnalSect", typeof(string));
                    dt.Columns.Add("DesignSect", typeof(string));
                    dt.Columns.Add("MatProp", typeof(string));
                    //dt.Columns.Add("NPSectType", typeof(string));
                    //dt.Columns.Add("NPSectLen", typeof(double));
                    //dt.Columns.Add("NPSectRD", typeof(double));
                    break;

                case Sap2000ExportTable.Frame_Property_Modifiers:
                    break;

                case Sap2000ExportTable.Frame_Release_Assignments_1_MINUS_General:
                    // TABLE NAME: Frame Release Assignments 1 - General
                    dt.Columns.Add("Frame", typeof(string));
                    dt.Columns.Add("PI", typeof(string));
                    dt.Columns.Add("V2I", typeof(string));
                    dt.Columns.Add("V3I", typeof(string));
                    dt.Columns.Add("TI", typeof(string));
                    dt.Columns.Add("M2I", typeof(string));
                    dt.Columns.Add("M3I", typeof(string));
                    dt.Columns.Add("PJ", typeof(string));
                    dt.Columns.Add("V2J", typeof(string));
                    dt.Columns.Add("V3J", typeof(string));
                    dt.Columns.Add("TJ", typeof(string));
                    dt.Columns.Add("M2J", typeof(string));
                    dt.Columns.Add("M3J", typeof(string));
                    dt.Columns.Add("PartialFix", typeof(string));
                    break;

                case Sap2000ExportTable.Frame_Release_Assignments_2_MINUS_Partial_Fixity:
                    break;

                case Sap2000ExportTable.Frame_Local_Axes_Assignments_1_MINUS_Typical:
                    // TABLE NAME: Frame Local Axes Assignments 1 - Typical
                    dt.Columns.Add("Frame", typeof(string));
                    dt.Columns.Add("Angle", typeof(double));
                    dt.Columns.Add("AdvanceAxes", typeof(string));
                    break;

                case Sap2000ExportTable.Frame_Local_Axes_Assignments_2_MINUS_Advanced:
                    // TABLE NAME: Frame Local Axes Assignments 2 - Advanced
                    dt.Columns.Add("Frame", typeof(string));
                    dt.Columns.Add("LocalPlane", typeof(string));
                    dt.Columns.Add("PlOption1", typeof(string));
                    dt.Columns.Add("PlCoordSys", typeof(string));
                    dt.Columns.Add("CoordDir1", typeof(string));
                    dt.Columns.Add("CoordDir2", typeof(string));
                    dt.Columns.Add("PlVecJt1", typeof(string));
                    dt.Columns.Add("PlVecJt2", typeof(string));
                    dt.Columns.Add("PlVecX", typeof(double));
                    dt.Columns.Add("PlVecY", typeof(double));
                    dt.Columns.Add("PlVecZ", typeof(double));
                    break;

                case Sap2000ExportTable.Frame_Insertion_Point_Assignments:
                    break;

                case Sap2000ExportTable.Program_Control:
                    // TABLE NAME: Program Control
                    dt.Columns.Add("ProgramName", typeof(string));
                    dt.Columns.Add("Version", typeof(string));
                    dt.Columns.Add("ProgLevel", typeof(string));
                    dt.Columns.Add("LicenseNum", typeof(string));
                    dt.Columns.Add("LicenseOS", typeof(string));
                    dt.Columns.Add("LicenseSC", typeof(string));
                    dt.Columns.Add("LicenseHT", typeof(string));
                    dt.Columns.Add("CurrUnits", typeof(string));
                    dt.Columns.Add("SteelCode", typeof(string));
                    dt.Columns.Add("ConcCode", typeof(string));
                    dt.Columns.Add("AlumCode", typeof(string));
                    dt.Columns.Add("ColdCode", typeof(string));
                    dt.Columns.Add("RegenHinge", typeof(string));
                    break;

                case Sap2000ExportTable.Frame_Bridge_Object_Flags:
                    break;

                case Sap2000ExportTable.Joint_Displacements:
                    // TABLE NAME: Joint Displacements
                    dt.Columns.Add("Joint", typeof(string));
                    dt.Columns.Add("OutputCase", typeof(string));
                    dt.Columns.Add("CaseType", typeof(string));
                    dt.Columns.Add("StepType", typeof(string));
                    dt.Columns.Add("StepNum", typeof(double));
                    dt.Columns.Add("U1", typeof(double));
                    dt.Columns.Add("U2", typeof(double));
                    dt.Columns.Add("U3", typeof(double));
                    dt.Columns.Add("R1", typeof(double));
                    dt.Columns.Add("R2", typeof(double));
                    dt.Columns.Add("R3", typeof(double));
                    break;

                case Sap2000ExportTable.Joint_Reactions:
                    break;

                case Sap2000ExportTable.Assembled_Joint_Masses:
                    break;

                case Sap2000ExportTable.Element_Forces_MINUS_Frames:
                    break;

                case Sap2000ExportTable.Element_Stresses_MINUS_Frames:
                    break;

                case Sap2000ExportTable.Element_Joint_Forces_MINUS_Frames:
                    // TABLE NAME: Element Joint Forces - Frames
                    dt.Columns.Add("Frame", typeof(string));
                    dt.Columns.Add("Joint", typeof(string));
                    dt.Columns.Add("OutputCase", typeof(string));
                    dt.Columns.Add("CaseType", typeof(string));
                    dt.Columns.Add("StepType", typeof(string));
                    dt.Columns.Add("StepNum", typeof(double));
                    dt.Columns.Add("StepLabel", typeof(string));
                    dt.Columns.Add("F1", typeof(double));
                    dt.Columns.Add("F2", typeof(double));
                    dt.Columns.Add("F3", typeof(double));
                    dt.Columns.Add("M1", typeof(double));
                    dt.Columns.Add("M2", typeof(double));
                    dt.Columns.Add("M3", typeof(double));
                    dt.Columns.Add("FrameElem", typeof(string));
                    break;

                case Sap2000ExportTable.Frame_Hinge_States:
                    break;

                case Sap2000ExportTable.Objects_And_Elements_MINUS_Joints:
                    break;

                case Sap2000ExportTable.Objects_And_Elements_MINUS_Frames:
                    break;

                case Sap2000ExportTable.Base_Reactions:
                    // TABLE NAME: Base Reactions
                    dt.Columns.Add("OutputCase", typeof(string));
                    dt.Columns.Add("CaseType", typeof(string));
                    dt.Columns.Add("StepType", typeof(string));
                    dt.Columns.Add("StepNum", typeof(double));
                    dt.Columns.Add("StepLabel", typeof(string));
                    dt.Columns.Add("GlobalFX", typeof(double));
                    dt.Columns.Add("GlobalFY", typeof(double));
                    dt.Columns.Add("GlobalFZ", typeof(double));
                    dt.Columns.Add("GlobalMX", typeof(double));
                    dt.Columns.Add("GlobalMY", typeof(double));
                    dt.Columns.Add("GlobalMZ", typeof(double));
                    dt.Columns.Add("GlobalX", typeof(double));
                    dt.Columns.Add("GlobalY", typeof(double));
                    dt.Columns.Add("GlobalZ", typeof(double));
                    dt.Columns.Add("XCentroidFX", typeof(double));
                    dt.Columns.Add("YCentroidFX", typeof(double));
                    dt.Columns.Add("ZCentroidFX", typeof(double));
                    dt.Columns.Add("XCentroidFY", typeof(double));
                    dt.Columns.Add("YCentroidFY", typeof(double));
                    dt.Columns.Add("ZCentroidFY", typeof(double));
                    dt.Columns.Add("XCentroidFZ", typeof(double));
                    dt.Columns.Add("YCentroidFZ", typeof(double));
                    dt.Columns.Add("ZCentroidFZ", typeof(double));
                    break;

                case Sap2000ExportTable.Modal_Periods_And_Frequencies:
                    break;

                case Sap2000ExportTable.Modal_Load_Participation_Ratios:
                    break;

                case Sap2000ExportTable.Modal_Participating_Mass_Ratios:
                    break;

                case Sap2000ExportTable.Modal_Participation_Factors:
                    break;

                case Sap2000ExportTable.Groups_1_MINUS_Definitions:
                    // TABLE NAME: Groups 1 - Definitions
                    dt.Columns.Add("GroupName", typeof(string));
                    dt.Columns.Add("Selection", typeof(string));
                    dt.Columns.Add("SectionCut", typeof(string));
                    dt.Columns.Add("Steel", typeof(string));
                    dt.Columns.Add("Concrete", typeof(string));
                    dt.Columns.Add("Aluminum", typeof(string));
                    dt.Columns.Add("ColdFormed", typeof(string));
                    dt.Columns.Add("Stage", typeof(string));
                    dt.Columns.Add("Bridge", typeof(string));
                    dt.Columns.Add("AutoSeismic", typeof(string));
                    dt.Columns.Add("AutoWind", typeof(string));
                    dt.Columns.Add("SelDesSteel", typeof(string));
                    dt.Columns.Add("SelDesAlum", typeof(string));
                    dt.Columns.Add("SelDesCold", typeof(string));
                    dt.Columns.Add("MassWeight", typeof(string));
                    dt.Columns.Add("Color", typeof(string));
                    break;

                case Sap2000ExportTable.Groups_2_MINUS_Assignments:
                    // TABLE NAME: Groups 2 - Assignments
                    dt.Columns.Add("GroupName", typeof(string));
                    dt.Columns.Add("ObjectType", typeof(string));
                    dt.Columns.Add("ObjectLabel", typeof(string));
                    break;

                case Sap2000ExportTable.Groups_3_MINUS_Masses_and_Weights:
                    // TABLE NAME: Groups 3 - Masses and Weights
                    dt.Columns.Add("GroupName", typeof(string));
                    dt.Columns.Add("SelfMass", typeof(double));
                    dt.Columns.Add("SelfWeight", typeof(double));
                    dt.Columns.Add("TotalMassX", typeof(double));
                    dt.Columns.Add("TotalMassY", typeof(double));
                    dt.Columns.Add("TotalMassZ", typeof(double));
                    break;

                case Sap2000ExportTable.Material_Properties_01_MINUS_General:
                    // TABLE NAME: Material Properties 01 - General
                    dt.Columns.Add("Material", typeof(string));
                    dt.Columns.Add("Type", typeof(string));
                    dt.Columns.Add("Grade", typeof(string));
                    dt.Columns.Add("SymType", typeof(string));
                    dt.Columns.Add("TempDepend", typeof(string));
                    dt.Columns.Add("Color", typeof(string));
                    dt.Columns.Add("GUID", typeof(string));
                    dt.Columns.Add("Notes", typeof(string));
                    break;

                case Sap2000ExportTable.Material_Properties_02_MINUS_Basic_Mechanical_Properties:
                    // TABLE NAME: Material Properties 02 - Basic Mechanical Properties
                    dt.Columns.Add("Material", typeof(string));
                    dt.Columns.Add("Temp", typeof(double));
                    dt.Columns.Add("UnitWeight", typeof(double));
                    dt.Columns.Add("UnitMass", typeof(double));
                    dt.Columns.Add("E1", typeof(double));
                    dt.Columns.Add("E2", typeof(double));
                    dt.Columns.Add("E3", typeof(double));
                    dt.Columns.Add("G12", typeof(double));
                    dt.Columns.Add("G13", typeof(double));
                    dt.Columns.Add("G23", typeof(double));
                    dt.Columns.Add("U12", typeof(double));
                    dt.Columns.Add("U13", typeof(double));
                    dt.Columns.Add("U23", typeof(double));
                    dt.Columns.Add("U14", typeof(double));
                    dt.Columns.Add("U24", typeof(double));
                    dt.Columns.Add("U34", typeof(double));
                    dt.Columns.Add("U15", typeof(double));
                    dt.Columns.Add("U25", typeof(double));
                    dt.Columns.Add("U35", typeof(double));
                    dt.Columns.Add("U45", typeof(double));
                    dt.Columns.Add("U16", typeof(double));
                    dt.Columns.Add("U26", typeof(double));
                    dt.Columns.Add("U36", typeof(double));
                    dt.Columns.Add("U46", typeof(double));
                    dt.Columns.Add("U56", typeof(double));
                    dt.Columns.Add("A1", typeof(double));
                    dt.Columns.Add("A2", typeof(double));
                    dt.Columns.Add("A3", typeof(double));
                    dt.Columns.Add("A12", typeof(double));
                    dt.Columns.Add("A13", typeof(double));
                    dt.Columns.Add("A23", typeof(double));
                    break;

                case Sap2000ExportTable.Material_Properties_03a_MINUS_Steel_Data:
                    // TABLE NAME: Material Properties 03a - Steel Data
                    dt.Columns.Add("Material", typeof(string));
                    dt.Columns.Add("Temp", typeof(double));
                    dt.Columns.Add("Fy", typeof(double));
                    dt.Columns.Add("Fu", typeof(double));
                    dt.Columns.Add("EffFy", typeof(double));
                    dt.Columns.Add("EffFu", typeof(double));
                    dt.Columns.Add("SSCurveOpt", typeof(string));
                    dt.Columns.Add("SSHysType", typeof(string));
                    dt.Columns.Add("SHard", typeof(double));
                    dt.Columns.Add("SMax", typeof(double));
                    dt.Columns.Add("SRup", typeof(double));
                    dt.Columns.Add("FinalSlope", typeof(double));
                    break;

                case Sap2000ExportTable.Frame_Section_Properties_01_MINUS_General:
                    // TABLE NAME: Frame Section Properties 01 - General
                    dt.Columns.Add("SectionName", typeof(string));
                    dt.Columns.Add("Material", typeof(string));
                    dt.Columns.Add("Shape", typeof(string));
                    dt.Columns.Add("AutoType", typeof(string));
                    dt.Columns.Add("t3", typeof(double));
                    dt.Columns.Add("t2", typeof(double));
                    dt.Columns.Add("tf", typeof(double));
                    dt.Columns.Add("tw", typeof(double));
                    dt.Columns.Add("t2b", typeof(double));
                    dt.Columns.Add("tfb", typeof(double));
                    dt.Columns.Add("dis", typeof(double));
                    dt.Columns.Add("Radius", typeof(double));
                    dt.Columns.Add("LipDepth", typeof(double));
                    dt.Columns.Add("LipAngle", typeof(double));
                    dt.Columns.Add("Area", typeof(double));
                    dt.Columns.Add("TorsConst", typeof(double));
                    dt.Columns.Add("I33", typeof(double));
                    dt.Columns.Add("I22", typeof(double));
                    dt.Columns.Add("I23", typeof(double));
                    dt.Columns.Add("AS2", typeof(double));
                    dt.Columns.Add("AS3", typeof(double));
                    dt.Columns.Add("S33", typeof(double));
                    dt.Columns.Add("S22", typeof(double));
                    dt.Columns.Add("Z33", typeof(double));
                    dt.Columns.Add("Z22", typeof(double));
                    dt.Columns.Add("R33", typeof(double));
                    dt.Columns.Add("R22", typeof(double));
                    dt.Columns.Add("EccV2", typeof(double));
                    dt.Columns.Add("ConcCol", typeof(string));
                    dt.Columns.Add("ConcBeam", typeof(string));
                    dt.Columns.Add("Color", typeof(string));
                    dt.Columns.Add("TotalWt", typeof(double));
                    dt.Columns.Add("TotalMass", typeof(double));
                    dt.Columns.Add("FromFile", typeof(string));
                    dt.Columns.Add("AMod", typeof(double));
                    dt.Columns.Add("A2Mod", typeof(double));
                    dt.Columns.Add("A3Mod", typeof(double));
                    dt.Columns.Add("JMod", typeof(double));
                    dt.Columns.Add("I2Mod", typeof(double));
                    dt.Columns.Add("I3Mod", typeof(double));
                    dt.Columns.Add("MMod", typeof(double));
                    dt.Columns.Add("WMod", typeof(double));
                    dt.Columns.Add("SectInFile", typeof(string));
                    dt.Columns.Add("FileName", typeof(string));
                    dt.Columns.Add("GUID", typeof(string));
                    dt.Columns.Add("Notes", typeof(string));
                    break;

                case Sap2000ExportTable.Cable_Section_Definitions:
                    // TABLE NAME: Cable Section Definitions
                    dt.Columns.Add("CableSect", typeof(string));
                    dt.Columns.Add("Material", typeof(string));
                    dt.Columns.Add("Specify", typeof(string));
                    dt.Columns.Add("Diameter", typeof(double));
                    dt.Columns.Add("Area", typeof(double));
                    dt.Columns.Add("TorsConst", typeof(double));
                    dt.Columns.Add("I", typeof(double));
                    dt.Columns.Add("AS", typeof(double));
                    dt.Columns.Add("Color", typeof(string));
                    dt.Columns.Add("TotalWt", typeof(double));
                    dt.Columns.Add("TotalMass", typeof(double));
                    dt.Columns.Add("AMod", typeof(double));
                    dt.Columns.Add("A2Mod", typeof(double));
                    dt.Columns.Add("A3Mod", typeof(double));
                    dt.Columns.Add("JMod", typeof(double));
                    dt.Columns.Add("I2Mod", typeof(double));
                    dt.Columns.Add("I3Mod", typeof(double));
                    dt.Columns.Add("MMod", typeof(double));
                    dt.Columns.Add("WMod", typeof(double));
                    dt.Columns.Add("GUID", typeof(string));
                    dt.Columns.Add("Notes", typeof(string));
                    break;

                case Sap2000ExportTable.Cable_Section_Assignments:
                    // TABLE NAME: Cable Section Assignments
                    dt.Columns.Add("Cable", typeof(string));
                    dt.Columns.Add("CableSect", typeof(string));
                    dt.Columns.Add("MatProp", typeof(string));
                    break;

                case Sap2000ExportTable.Frame_Tension_And_Compression_Limits:
                    // TABLE NAME: Frame Tension And Compression Limits
                    dt.Columns.Add("Frame", typeof(string));
                    dt.Columns.Add("TensLimit", typeof(string));
                    dt.Columns.Add("CompLimit", typeof(string));
                    dt.Columns.Add("Tension", typeof(double));
                    dt.Columns.Add("Compression", typeof(double));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(inSap2000ExportTable), inSap2000ExportTable, null);
            }

            if (dt.Columns.Count == 0) throw new S2KHelperException($"Definition for table called {SapAutoExtensions.EnumToString(inSap2000ExportTable)} was not set. Please set it in the code.");

            return dt;
        }

        public static IEnumerable<string> DumpToS2KStream(this DataTable inTable)
        {
            Regex nameRegex = new Regex(@"Table:  (?<tName>.*)");
            Match nameMatch = nameRegex.Match(inTable.TableName);
            if (!nameMatch.Success) throw new S2KHelperException($"Table named {inTable.TableName} is not in the right format.");
            string s2KHeader = "TABLE:  \"" + nameMatch.Groups["tName"].Value.ToUpper() + "\"";

            yield return s2KHeader;

            StringBuilder sb = new StringBuilder();
            foreach (DataRow tableRow in inTable.Rows)
            {
                sb.Append("   ");

                for (int index = 0; index < inTable.Columns.Count - 1; index++)
                {
                    DataColumn tableColumn = inTable.Columns[index];
                    if (tableRow[tableColumn] == null || tableRow[tableColumn] == DBNull.Value) continue;

                    sb.Append(tableColumn.ColumnName);
                    if (tableColumn.DataType == typeof(string)) sb.Append("=\""); else sb.Append('=');
                    sb.Append(tableRow[tableColumn]);
                    if (tableColumn.DataType == typeof(string)) sb.Append("\"   "); else sb.Append("   ");
                }

                DataColumn lastColumn = inTable.Columns[inTable.Columns.Count - 1];
                if (!(tableRow[lastColumn] == null || tableRow[lastColumn] == DBNull.Value))
                {
                    sb.Append(lastColumn.ColumnName);
                    if (lastColumn.DataType == typeof(string)) sb.Append("=\"");
                    else sb.Append('=');
                    sb.Append(tableRow[lastColumn]);
                    if (lastColumn.DataType == typeof(string)) sb.Append('"');
                }

                yield return sb.ToString();

                sb.Clear();
            }
            yield return " ";
        }

        public static List<Sap2000AutomatorTableExportData> FromEnumList(List<Sap2000ExportTable> inEnumList)
        {
            List<Sap2000AutomatorTableExportData> toRet = new List<Sap2000AutomatorTableExportData>();

            toRet.AddRange(from a in inEnumList select new Sap2000AutomatorTableExportData(a));

            return toRet;
        }

        public static System.Drawing.Point SapExpandPoint(this TreeItem inTreeItem)
        {
            return new System.Drawing.Point(inTreeItem.BoundingRectangle.X + SapAutoExtensions.ExpandShiftX, inTreeItem.BoundingRectangle.Y + SapAutoExtensions.ExpandShiftY);
        }

        public static System.Drawing.Point SapTreeCheckBox(this TreeItem inTreeItem)
        {
            return new System.Drawing.Point(inTreeItem.BoundingRectangle.X + SapAutoExtensions.SelectShiftX, inTreeItem.BoundingRectangle.Y + SapAutoExtensions.SelectShiftY);
        }
    }
}