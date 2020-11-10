using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Emasa_Optimizer.Opt;
using Emasa_Optimizer.Opt.ParamDefinitions;
using Emasa_Optimizer.Opt.ProbQuantity;

namespace Emasa_Optimizer.WpfResources
{
    public class Results_ConfigSummary_ListBox_ItemTemplate_ConstraintItemSummary_ItemTemplate_ConstraintTemplate : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element && item != null && item is NlOpt_Point_ConstraintData param)
            {
                if (!param.ProbQuantity.IsConstraint) throw new Exception("The Problem Quantity is not of a Constraint type");
                switch (param.ProbQuantity.ConstraintObjective)
                {
                    case Quantity_ConstraintObjectiveEnum.EqualTo:
                        return element.FindResource("Results_ConfigSummary_ListBox_ItemTemplate_ConfigElementvalues_GhLineList_ItemTemplate") as DataTemplate;
                    case Quantity_ConstraintObjectiveEnum.HigherThanOrEqual:
                        return element.FindResource("Results_ConfigSummary_ListBox_ItemTemplate_ConfigElementvalues_GhLineList_ItemTemplate") as DataTemplate;
                    case Quantity_ConstraintObjectiveEnum.LowerThanOrEqual:
                        return element.FindResource("Results_ConfigSummary_ListBox_ItemTemplate_ConfigElementvalues_GhLineList_ItemTemplate") as DataTemplate;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return null;
        }
    }

    public class Results_ConfigSummary_ListBox_ItemTemplate_ConfigElementvalues_TemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element && item != null && item is ProblemConfig_ElementCombinationValue param)
            {
                switch (param)
                {
                    case ProblemConfig_GhIntegerInputConfigValues problemConfig_GhIntegerInputConfigValues:
                        return element.FindResource("Results_ConfigSummary_ListBox_ItemTemplate_ConfigElementvalues_GhIntegerInput_ItemTemplate") as DataTemplate;

                    case ProblemConfig_GhLineListConfigValues problemConfig_GhLineListConfigValues:
                        return element.FindResource("Results_ConfigSummary_ListBox_ItemTemplate_ConfigElementvalues_GhLineList_ItemTemplate") as DataTemplate;
                }
            }

            return null;
        }
    }
    
    public class GhOutputGeometry_Setup_ListBox_TemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element && item != null && item is GhGeom_ParamDefBase param)
            {
                switch (param)
                {
                    case LineList_GhGeom_ParamDef _:
                        return element.FindResource("GhOutputGeometry_Setup_ListBox_ItemTemplate_LineList_GhGeom_ParamDef") as DataTemplate;

                    case PointList_GhGeom_ParamDef _:
                        return element.FindResource("GhOutputGeometry_Setup_ListBox_ItemTemplate_PointList_GhGeom_ParamDef") as DataTemplate;
                }
            }

            return null;
        }
    }

    public class GhNonLinearParameter_Setup_ListBox_TemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element && item != null && item is Input_ParamDefBase param)
            {
                switch (param)
                {
                    case Double_Input_ParamDef _:
                        return element.FindResource("GhNonLinearParameter_Setup_ListBox_ItemTemplate_Double_Input_ParamDef") as DataTemplate;

                    case Point_Input_ParamDef _:
                        return element.FindResource("GhNonLinearParameter_Setup_ListBox_ItemTemplate_Point_Input_ParamDef") as DataTemplate;
                }
            }

            return null;
        }
    }

    public class Wpf_ResultList_Input_DataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element && item != null && item is KeyValuePair<Input_ParamDefBase, object> kvp)
            {
                switch (kvp.Key)
                {
                    case Double_Input_ParamDef _:
                        return element.FindResource("Results_FunctionIterationSummary_DataTemplate_InputParams_DoubleInputParam") as DataTemplate;

                    case Point_Input_ParamDef _:
                        return element.FindResource("Results_FunctionIterationSummary_DataTemplate_InputParams_PointInputParam") as DataTemplate;
                }
            }

            return null;
        }
    }

    public class Results_FunctionIterationSummary_ProblemQuantity_DataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;

            if (element != null && item != null && item is KeyValuePair<ProblemQuantity, NlOpt_Point_ProblemQuantity_Output> kvp)
            {
                if (kvp.Key.IsObjectiveFunctionMinimize)
                    return element.FindResource("Results_FunctionIterationSummary_ProblemQuantity_ObjectiveFunction_DataTemplate") as DataTemplate;
                if (kvp.Key.IsOutputOnly)
                    return element.FindResource("Results_FunctionIterationSummary_ProblemQuantity_OutputOnly_DataTemplate") as DataTemplate;
                if (kvp.Key.IsConstraint)
                    return element.FindResource("Results_FunctionIterationSummary_ProblemQuantity_Constraint_DataTemplate") as DataTemplate;
            }

            return null;
        }
    }

    public class NlOpt_Point_GhInputNL_InputType_TemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element && item != null && item is KeyValuePair<Input_ParamDefBase, object> kvp)
            {
                switch (kvp.Key)
                {
                    case Double_Input_ParamDef _:
                        return element.FindResource("NlOpt_Point_GhInputNL_InputTypeDouble") as DataTemplate;

                    case Point_Input_ParamDef _:
                        return element.FindResource("NlOpt_Point_GhInputNL_InputTypePoint") as DataTemplate;
                }
            }

            return null;
        }
    }

}
