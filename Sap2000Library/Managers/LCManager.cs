using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using Sap2000Library.DataClasses;
using Sap2000Library.Other;
using Sap2000Library.SapObjects;
using SAP2000v1;

namespace Sap2000Library.Managers
{
    public class LCManager : SapManagerBase
    {
        internal LCManager(S2KModel model) : base(model) { }

        public List<LCNonLinear> GetNonLinearStaticLoadCaseList(bool inOnlyFailed = false, string inRegexFilter = null, bool inUpdateInterface = false)
        {
            int count = 0;
            string[] names = null;

            if (inUpdateInterface) BusyOverlayBindings.I.SetDeterminate("SAP2000: Getting Non-Linear Load Case List and Data.", "Load Case");

            int ret = SapApi.LoadCases.GetNameList(ref count, ref names, (eLoadCaseType)(int)LoadCaseTypeEnum.CASE_NONLINEAR_STATIC);

            if (ret != 0 || count == 0) return new List<LCNonLinear>();

            // Declares the return value
            List<LCNonLinear> toReturn = new List<LCNonLinear>();

            Regex regex = !string.IsNullOrWhiteSpace(inRegexFilter) ? new Regex(inRegexFilter) : null;

            for (int i = 0; i < count; i++)
            {
                LCNonLinear cName = new LCNonLinear(this) { Name = names[i] };
                if (inUpdateInterface) BusyOverlayBindings.I.UpdateProgress(i, count, cName.Name);

                // Filters the regex
                if (regex != null)
                {
                    if (!regex.IsMatch(cName.Name))
                        continue;
                }

                // Checks if the status is failed
                if (inOnlyFailed)
                {
                    if (!(cName.Status == LCStatus.CouldNotStart || cName.Status == LCStatus.NotFinished))
                        continue;
                }
                
                // Gets the details of the load cases
                cName.FillSolverControlData();

                toReturn.Add(cName);
            }

            return toReturn;
        }

        public LCNonLinear GetNonLinearStaticLoadCaseByName(string inName)
        {
            // First, gets the type to make an inference if it actually exists
            eLoadCaseType sap_lcType = eLoadCaseType.Buckling;
            int sap_subType = 0;
            eLoadPatternType designType = eLoadPatternType.ActiveEarthPressure;
            int designTypeOptions = 0;
            int auto = 0;

            int ret = SapApi.LoadCases.GetTypeOAPI_1(inName, ref sap_lcType, ref sap_subType, ref designType, ref designTypeOptions, ref auto);
            if (ret != 0) throw new S2KHelperException($"Load case named {inName} could not be obtained from the model. Does it exist?");

            // Transforms the type
            LoadCaseTypeEnum lcType = (LoadCaseTypeEnum)(int)(sap_lcType);
            if (lcType != LoadCaseTypeEnum.CASE_NONLINEAR_STATIC) throw new S2KHelperException($"The Load Case named {inName} is not of Static Non-Linear (Regular or Staged) type.");
            LCNonLinear_SubType lCNonLinear_Subtype = (LCNonLinear_SubType)sap_subType;

            LCNonLinear toReturn = null;
            switch (lCNonLinear_Subtype)
            {
                case LCNonLinear_SubType.Nonlinear:
                    toReturn = new LCNonLinear(this) { Name = inName, NLSubType = lCNonLinear_Subtype };
                    break;
                case LCNonLinear_SubType.StagedConstruction:
                    toReturn = new LCStagedNonLinear(this) { Name = inName, NLSubType = lCNonLinear_Subtype };
                    break;
            }

            return toReturn;
        }
        public LoadCase GetLoadCaseByName(string inName)
        {
            return new LoadCase(this) { Name = inName };
        }

        public List<string> GetAllNames(LoadCaseTypeEnum? filterType = null, bool inUpdateInterface = false)
        {
            if (inUpdateInterface) BusyOverlayBindings.I.SetIndeterminate("SAP2000: Getting Load Case Name List.");

            int count = 0;
            string[] names = null;

            int ret = 0;
            if (filterType.HasValue) SapApi.LoadCases.GetNameList_1(ref count, ref names, (eLoadCaseType)filterType);
            else SapApi.LoadCases.GetNameList_1(ref count, ref names);

            if (ret != 0)
            {
                throw new S2KHelperException($"Could not get the Load Case Name List.", this);
            }

            if (count > 0) return new List<string>(names);
            else return new List<string>();
        }

        public void UpdateNLSolControlParams(LCNonLinear inCase, LCNonLinear_SolverControlParams inParams = null)
        {
            int ret = 0;

            if (inParams == null) inParams = inCase.SolControlParams;

            // If the case has been finished, its data must be deleted
            if (inCase.Status == LCStatus.Finished)
            {
                int subret = SapApi.Analyze.DeleteResults(inCase.Name, false);
                if (subret == 0) inCase.Status = null;
                else throw new S2KHelperException($"Could not delete the results for case {inCase.Name}. Therefore, we cannot change the solution parameters.");
            }

            switch (inCase.NLSubType)
            {
                case LCNonLinear_SubType.Nonlinear:
                    ret = SapApi.LoadCases.StaticNonlinear.SetSolControlParameters(
                        inCase.Name,
                        inParams.MaxTotalSteps.Value,
                        inParams.MaxFailedSubSteps.Value,
                        inParams.MaxIterCS.Value,
                        inParams.MaxIterNR.Value,
                        inParams.TolConvD.Value,
                        inParams.UseEventStepping,
                        inParams.TolEventD.Value,
                        inParams.MaxLineSearchPerIter.Value,
                        inParams.TolLineSearch.Value,
                        inParams.LineSearchStepFact.Value);
                    break;
                case LCNonLinear_SubType.StagedConstruction:
                    ret = SapApi.LoadCases.StaticNonlinearStaged.SetSolControlParameters(
                        inCase.Name,
                        inParams.MaxTotalSteps.Value,
                        inParams.MaxFailedSubSteps.Value,
                        inParams.MaxIterCS.Value,
                        inParams.MaxIterNR.Value,
                        inParams.TolConvD.Value,
                        inParams.UseEventStepping,
                        inParams.TolEventD.Value,
                        inParams.MaxLineSearchPerIter.Value,
                        inParams.TolLineSearch.Value,
                        inParams.LineSearchStepFact.Value);
                    break;
                default:
                    break;
            }

            if (ret != 0) throw new S2KHelperException($"Could not update the solution parameters for load case {inCase.Name}.");
        }
        public void UpdateNLTargetForceParams(LCNonLinear inCase, LCNonLinear_TargetForceParams inParams = null)
        {
            int ret = 0;

            if (inParams == null) inParams = inCase.TargetForceParams;

            // If the case has been finished, its data must be deleted
            if (inCase.Status == LCStatus.Finished)
            {
                int subret = SapApi.Analyze.DeleteResults(inCase.Name, false);
                if (subret == 0) inCase.Status = null;
                else throw new S2KHelperException($"Could not delete the results for case {inCase.Name}. Therefore, we cannot change the target force parameters.");
            }

            switch (inCase.NLSubType)
            {
                case LCNonLinear_SubType.Nonlinear:
                    ret = SapApi.LoadCases.StaticNonlinear.SetTargetForceParameters(
                        inCase.Name,
                        inParams.TolConvF.Value,
                        inParams.MaxIter.Value,
                        inParams.AccelFact.Value,
                        inParams.NoStop);
                    break;
                case LCNonLinear_SubType.StagedConstruction:
                    ret = SapApi.LoadCases.StaticNonlinearStaged.SetTargetForceParameters(
                        inCase.Name,
                        inParams.TolConvF.Value,
                        inParams.MaxIter.Value,
                        inParams.AccelFact.Value,
                        inParams.NoStop);
                    break;
                default:
                    break;
            }

            if (ret != 0) throw new S2KHelperException($"Could not update the target force parameters for load case {inCase.Name}.");
        }
        public void UpdateNLResultsSavedNL(LCNonLinear inCase, LCNonLinear_ResultsSavedNL inParams = null)
        {
            int ret = 0;

            if (inParams == null) inParams = inCase.ResultsSavedNL;

            // If the case has been finished, its data must be deleted
            if (inCase.Status == LCStatus.Finished)
            {
                int subret = SapApi.Analyze.DeleteResults(inCase.Name, false);
                if (subret == 0) inCase.Status = null;
                else throw new S2KHelperException($"Could not delete the results for case {inCase.Name}. Therefore, we cannot change the options for saved results.");
            }

            switch (inCase.NLSubType)
            {
                case LCNonLinear_SubType.Nonlinear:
                    ret = SapApi.LoadCases.StaticNonlinear.SetResultsSaved(
                        inCase.Name,
                        !(inParams.MinSavedStates == 0 && inParams.MaxSavedStates == 0),
                        inParams.MinSavedStates.Value,
                        inParams.MaxSavedStates.Value,
                        inParams.PositiveOnly);
                    break;
                case LCNonLinear_SubType.StagedConstruction:
                    throw new S2KHelperException($"You cannot update the NL results saved for a NL staged construction case. Case Name: {inCase.Name}.");
                default:
                    break;
            }

            if (ret != 0) throw new S2KHelperException($"Could not update the save results options for load case {inCase.Name}.");
        }
        public void UpdateNLResultsSavedStaged(LCNonLinear inCase, LCNonLinear_ResultsSavedStaged inParams = null)
        {
            int ret = 0;

            if (inParams == null) inParams = inCase.ResultsSavedStaged;

            // If the case has been finished, its data must be deleted
            if (inCase.Status == LCStatus.Finished)
            {
                int subret = SapApi.Analyze.DeleteResults(inCase.Name, false);
                if (subret == 0) inCase.Status = null;
                else throw new S2KHelperException($"Could not delete the results for case {inCase.Name}. Therefore, we cannot change the options for saved results.");
            }

            switch (inCase.NLSubType)
            {
                case LCNonLinear_SubType.Nonlinear:
                    throw new S2KHelperException($"You cannot update the NL results saved for a NL staged construction case. Case Name: {inCase.Name}.");
                case LCNonLinear_SubType.StagedConstruction:
                    ret = SapApi.LoadCases.StaticNonlinearStaged.SetResultsSaved(
                        inCase.Name,
                        (int)inParams.StagedSaveOption,
                        inParams.StagedMinSteps.Value,
                        inParams.StagedMinStepsTD.Value);
                    break;
                    
                default:
                    break;
            }

            if (ret != 0) throw new S2KHelperException($"Could not update the save results options for load case {inCase.Name}.");
        }
        public void UpdateNLLoadApplication(LCNonLinear inCase, LCNonLinear_LoadApplicationOptions inParams = null)
        {
            if (inParams == null) inParams = inCase.LoadApplicationOptions;

            // If the case has been finished, its data must be deleted
            if (inCase.Status == LCStatus.Finished)
            {
                int subret = SapApi.Analyze.DeleteResults(inCase.Name, false);
                if (subret == 0) inCase.Status = null;
                else throw new S2KHelperException($"Could not delete the results for case {inCase.Name}. Therefore, we cannot change the options for the load application.");
            }

            switch (inCase.NLSubType)
            {
                case LCNonLinear_SubType.Nonlinear:
                    if (0 != SapApi.LoadCases.StaticNonlinear.SetLoadApplication(inCase.Name,
                        (int)inParams.LoadControl,
                        (int)inParams.DispType,
                        inParams.Displacement,
                        (int)inParams.Monitor,
                        (int)inParams.DOF,
                        inParams.PointName,
                        inParams.GeneralizedDisplacementName)) throw new S2KHelperException($"Could not update the NL load application options for case {inCase.Name}.");

                    inCase.LoadApplicationOptions = inParams;

                    break;

                case LCNonLinear_SubType.StagedConstruction:
                    throw new S2KHelperException($"You cannot update the NL load application options for a NL staged construction case. Case Name: {inCase.Name}.");


                default:
                    break;
            }
        }

        public bool AddNew_LCStagedNonLinear(string inCaseName, List<LoadCaseNLStagedStageData> inStages, bool inUseStepping, string inPreviousCase = null)
        {
            int ret = SapApi.LoadCases.StaticNonlinearStaged.SetCase(inCaseName);
            if (ret != 0) throw new S2KHelperException($"Could not add Staged Non-Linear case named {inCaseName}", this);

            ret = SapApi.LoadCases.StaticNonlinearStaged.SetGeometricNonlinearity(inCaseName, (int)LCNonLinear_NLGeomType.PDelta);
            if (ret != 0) throw new S2KHelperException($"Could set the Geometric Non-Linearity to P-Delta to Staged Non-Linear case named {inCaseName}", this);

            // The LCs will have only *one* stage
            double[] duration = new double[] { 10d };
            bool[] output = new bool[] { true };
            string[] outputName = new string[] { $"{inCaseName}_OL" };
            string[] comment = new string[] { $"{inCaseName}_OL - Automatically Generated" };

            ret = SapApi.LoadCases.StaticNonlinearStaged.SetStageDefinitions_2(inCaseName, 1, ref duration, ref output, ref outputName, ref comment);
            if (ret != 0) throw new S2KHelperException($"Could not add Stage Definitions (SetStageDefinitions_2) to Staged Non-Linear case named {inCaseName}", this);

            // Now, the actions to this one stage
            if (inStages != null && inStages.Count > 0)
            {
                int[] operation = (from a in inStages select ((int)a.Operation) / 100 != 0 ? ((int)a.Operation) / 100 : (int)a.Operation).ToArray();
                string[] objectType = (from a in inStages select a.ObjectType).ToArray();
                string[] objectName = (from a in inStages select a.ObjectName).ToArray();
                double[] age = (from a in inStages select a.AgeWhenAdded ?? 0).ToArray();
                string[] myType = (from a in inStages select a.MyType).ToArray();
                string[] myName = (from a in inStages select a.MyName).ToArray();
                double[] scaleFactor = (from a in inStages select a.ScaleFactor ?? 0).ToArray();

                ret = SapApi.LoadCases.StaticNonlinearStaged.SetStageData_2(inCaseName, 1, inStages.Count,
                    ref operation, ref objectType, ref objectName, ref age, ref myType, ref myName, ref scaleFactor);

                if (ret != 0) throw new S2KHelperException($"Could not add Stage Data [actions] (SetStageData_2) to Staged Non-Linear case named {inCaseName}", this);
            }

            // The default created by SAP is to use stepping
            if (!inUseStepping)
            {
                // Gets the instance that was created
                LCNonLinear newlyCreated = GetNonLinearStaticLoadCaseByName(inCaseName);

                // Updates the local designation
                LCNonLinear_SolverControlParams solverParams = newlyCreated.SolControlParams;
                solverParams.UseEventStepping = false;

                // Submits this change to SAP
                UpdateNLSolControlParams(newlyCreated);
            }

            if (!string.IsNullOrWhiteSpace(inPreviousCase))
            {
                ret = SapApi.LoadCases.StaticNonlinearStaged.SetInitialCase(inCaseName, inPreviousCase);
                if (ret != 0) throw new S2KHelperException($"Could not set previous case for Staged Non-Linear case named {inCaseName}", this);
            }

            return true;
        }
        public bool AddNew_LCNonLinear(string inCaseName, List<LoadCaseNLLoadData> inLoads, bool inUseStepping, string inPreviousCase = null)
        {
            int ret = SapApi.LoadCases.StaticNonlinear.SetCase(inCaseName);
            if (ret != 0) throw new S2KHelperException($"Could not add Non-Linear case named {inCaseName}", this);

            ret = SapApi.LoadCases.StaticNonlinear.SetGeometricNonlinearity(inCaseName, (int)LCNonLinear_NLGeomType.PDelta);
            if (ret != 0) throw new S2KHelperException($"Could set the Geometric Non-Linearity to P-Delta to Non-Linear case named {inCaseName}", this);

            // Adds the loads
            if (inLoads != null && inLoads.Count > 0)
            {
                string[] loadType = (from a in inLoads select a.LoadType).ToArray();
                string[] loadName = (from a in inLoads select a.LoadName).ToArray();
                double[] scaleFactor = (from a in inLoads select a.ScaleFactor).ToArray();

                ret = SapApi.LoadCases.StaticNonlinear.SetLoads(inCaseName, inLoads.Count,
                    ref loadType, ref loadName, ref scaleFactor);

                if (ret != 0) throw new S2KHelperException($"Could not add Loads (SetLoads) to Non-Linear case named {inCaseName}", this);
            }

            // The default created by SAP is to use stepping
            if (!inUseStepping)
            {
                // Gets the instance that was created
                LCNonLinear newlyCreated = GetNonLinearStaticLoadCaseByName(inCaseName);

                // Updates the local designation
                LCNonLinear_SolverControlParams solverParams = newlyCreated.SolControlParams;
                solverParams.UseEventStepping = false;

                // Submits this change to SAP
                UpdateNLSolControlParams(newlyCreated);
            }

            if (!string.IsNullOrWhiteSpace(inPreviousCase))
            {
                ret = SapApi.LoadCases.StaticNonlinear.SetInitialCase(inCaseName, inPreviousCase);
                if (ret != 0) throw new S2KHelperException($"Could not set previous case for Non-Linear case named {inCaseName}", this);
            }

            return true;
        }
        public bool AddNew_Modal(string inCaseName, string inPreviousCase = null)
        {
            int ret = SapApi.LoadCases.ModalEigen.SetCase(inCaseName);
            if (ret != 0) throw new S2KHelperException($"Could not add Modal case named {inCaseName}", this);

            if (!string.IsNullOrWhiteSpace(inPreviousCase))
            {
                ret = SapApi.LoadCases.ModalEigen.SetInitialCase(inCaseName, inPreviousCase);
                if (ret != 0) throw new S2KHelperException($"Could not set previous case for Modal case named {inCaseName}", this);
            }

            ret = SapApi.LoadCases.ModalEigen.SetNumberModes(inCaseName, 3 , 1);
            if (ret != 0) throw new S2KHelperException($"Could not add Modal case named {inCaseName}", this);

            return true;
        }
        public bool AddNew_Linear(string inCaseName, List<LoadCaseNLLoadData> inLoads)
        {
            int ret = SapApi.LoadCases.StaticLinear.SetCase(inCaseName);
            if (ret != 0) throw new S2KHelperException($"Could not add linear case named {inCaseName}", this);

            // Adds the loads
            if (inLoads != null && inLoads.Count > 0)
            {
                string[] loadType = (from a in inLoads select a.LoadType).ToArray();
                string[] loadName = (from a in inLoads select a.LoadName).ToArray();
                double[] scaleFactor = (from a in inLoads select a.ScaleFactor).ToArray();

                ret = SapApi.LoadCases.StaticLinear.SetLoads(inCaseName, inLoads.Count,
                    ref loadType, ref loadName, ref scaleFactor);

                if (ret != 0) throw new S2KHelperException($"Could not add Loads (SetLoads) to linear case named {inCaseName}", this);
            }

            return true;
        }

        public bool Delete(string inName)
        {
            if (SapApi.LoadCases.Delete(inName) != 0)
                throw new S2KHelperException($"Could not delete load case called {inName}.", this);

            return true;
        }
        public bool DeleteAll()
        {
            return DeleteAll(null, null);
        }
        public bool DeleteAll(IProgress<ProgressData> ReportProgres)
        {
            return DeleteAll(ReportProgres, null);
        }
        public bool DeleteAll(LoadCaseTypeEnum? type)
        {
            return DeleteAll(null, type);
        }
        public bool DeleteAll(IProgress<ProgressData> ReportProgress, LoadCaseTypeEnum? type)
        {
            int numberCases = 0;
            string[] caseNames = null;

            int ret;
            if (type.HasValue)
                ret = SapApi.LoadCases.GetNameList(ref numberCases, ref caseNames, (eLoadCaseType)(int)type.Value);
            else
                ret = SapApi.LoadCases.GetNameList(ref numberCases, ref caseNames);

            if (ret != 0) throw new S2KHelperException($"Could not get load case list.");

            for (int i = 0; i < caseNames.Length; i++)
            {
                string item = caseNames[i];

                if (ReportProgress != null) ReportProgress.Report(ProgressData.UpdateProgress(i,caseNames.Length));

                Delete(item);
            }

            return true;
        }

        public List<LoadCase> GetAll(LoadCaseTypeEnum? filterType = null)
        {
            List<string> lcNames = GetAllNames(filterType);

            return lcNames.Select(a => new LoadCase(this) { Name = a }).ToList();
        }
    }
}
