using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using Sap2000Library;
using Sap2000Library.SapObjects;

namespace EmasaSapTools.Bindings
{
    public class SelectionInfoBindings : BindableSingleton<SelectionInfoBindings>
    {
        private SelectionInfoBindings(){}
        public override void SetOrReset()
        {
            SelectionMonitorEnabled = false;
            GetGroups = false;
            GetFrames = false;
            STGWWeld = "1";
            GroupIncludePoints = false;

            SelectMisc_FromClipboard_FrameIsChecked = true;
            SelectMisc_FromClipboard_JointIsChecked = false;
            SelectMisc_FromClipboard_AreaIsChecked = false;
            SelectMisc_FromClipboard_CableIsChecked = false;
            SelectMisc_FromClipboard_LinkIsChecked = false;

            // Sets the CollectionViewSource on the gridview
            SelMonitor_GroupsDataGrid.DataContext = SelMonitor_GroupsDataGrid_CVS;

            SelectMisc_FramesBasedOnLength_FilterFromSelection = true;
            SelectMisc_FramesBasedOnLength_MaxLength = 1000d;
            SelectMisc_FramesBasedOnLength_MinLength = 0d;

            
        }

        private bool _SelectionMonitorEnabled;public bool SelectionMonitorEnabled { get => _SelectionMonitorEnabled; set => SetProperty(ref _SelectionMonitorEnabled, value); }

        private bool _GetGroups;public bool GetGroups { get => _GetGroups; set => SetProperty(ref _GetGroups, value); }

        private bool _GetFrames;public bool GetFrames { get => _GetFrames; set => SetProperty(ref _GetFrames, value); }

        private string _STGWWeld;public string STGWWeld { get => _STGWWeld; set => SetProperty(ref _STGWWeld, value); }

        private bool _GroupIncludePoints;public bool GroupIncludePoints { get => _GroupIncludePoints; set => SetProperty(ref _GroupIncludePoints, value); }


        private string _Frame_Name;public string Frame_Name { get => _Frame_Name; set => SetProperty(ref _Frame_Name, value); }

        private string _Frame_Section;public string Frame_Section { get => _Frame_Section; set => SetProperty(ref _Frame_Section, value); }

        private string _Frame_PointText;public string Frame_PointText { get => _Frame_PointText; set => SetProperty(ref _Frame_PointText, value); }

        private string _Frame_Length;public string Frame_Length { get => _Frame_Length; set => SetProperty(ref _Frame_Length, value); }

        // Necessary to Manage the Data of the GridView
        private CollectionViewSource SelMonitor_GroupsDataGrid_CVS = new CollectionViewSource();

        private DataGrid SelMonitor_GroupsDataGrid
        {
            get
            {
                try
                {
                    return (DataGrid) BoundTo.FindName("SelMonitor_GroupsDataGrid");
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public List<SelectionInfo_GroupInfoItem> SelMonitor_GroupsDataGridItems
        {
            get
            {
                if (SelMonitor_GroupsDataGrid_CVS.Source is List<SelectionInfo_GroupInfoItem>)
                    return (List<SelectionInfo_GroupInfoItem>) SelMonitor_GroupsDataGrid_CVS.Source;
                else
                    return null;
            }
            set => SelMonitor_GroupsDataGrid_CVS.Source = value;
        }

        private List<SapObject> _SelectedObjects;public List<SapObject> SelectedObjects { get => _SelectedObjects; 
            set
            {
                // Saves the value
                SetProperty(ref _SelectedObjects, value);

                if (GetGroups)
                {
                    var groupNames = new HashSet<string>();
                    var groupTable = new List<SelectionInfo_GroupInfoItem>();
                    if (value != null && value.Count > 0)
                        foreach (SapObject sapObject in value)
                        {
                            if (!GroupIncludePoints && sapObject.SapType == SapObjectType.Point)
                                continue;
                            foreach (string objGrp in sapObject.Groups)
                            {
                                if (groupNames.Add(objGrp)) groupTable.Add(new SelectionInfo_GroupInfoItem(objGrp));
                                (from a in groupTable where a.GroupName == objGrp select a).First().ItemCount++;
                            }
                        }

                    SelMonitor_GroupsDataGridItems = groupTable;
                }
                else if (SelMonitor_GroupsDataGridItems != null)
                {
                    SelMonitor_GroupsDataGridItems = null;
                }

                if (GetFrames)
                {
                    int selFrameCount = value.Count(a => a.SapType == SapObjectType.Frame);
                    if (selFrameCount == 0)
                    {
                        Frame_Name = "No Frame Selected";
                        Frame_Section = string.Empty;
                        Frame_PointText = string.Empty;
                        Frame_Length = string.Empty;
                    }
                    else if (selFrameCount > 1)
                    {
                        Frame_Name = $"More than 1 frame selected [{selFrameCount} frames]";
                        Frame_Section = string.Empty;
                        Frame_PointText = string.Empty;
                        Frame_Length = string.Empty;
                    }
                    else
                    {
                        SapFrame frame = value.First(a => a.SapType == SapObjectType.Frame) as SapFrame;
                        if (frame == null)
                        {
                            Frame_Name = $"Bug in the SapObjectType of the Frame (Could not cast to SapFrame)";
                            Frame_Section = string.Empty;
                            Frame_PointText = string.Empty;
                            Frame_Length = string.Empty;
                        }
                        else
                        {
                            Frame_Name = frame.Name;
                            Frame_Section = frame.Section.Name;
                            Frame_Length = $"{frame.Length:F3}";

                            // Is there a point selected?
                            int selPointCount = value.Count(a => a.SapType == SapObjectType.Point);
                            if (selPointCount == 0)
                            {
                                Frame_PointText = "No Joint Selected";
                            }
                            else if (selPointCount > 1)
                            {
                                Frame_PointText = $"More than 1 joint selected [{selPointCount} joint]";
                            }
                            else
                            {
                                SapPoint pnt = value.First(a => a.SapType == SapObjectType.Point) as SapPoint;

                                if (pnt == null)
                                {
                                    Frame_PointText =
                                        $"Bug in the SapObjectType of the Joint (Could not cast to SapPoint)";
                                }
                                else
                                {
                                    string text = $"{pnt.Name} - ";
                                    if (frame.iEndPoint == pnt) text += "I Joint";
                                    else if (frame.jEndPoint == pnt) text += "J Joint";
                                    else text += "Not Frame Joint";

                                    Frame_PointText = text;
                                }
                            }
                        }
                    }
                }
                else
                {
                    Frame_Name = string.Empty;
                    Frame_Section = string.Empty;
                    Frame_PointText = string.Empty;
                    Frame_Length = string.Empty;
                }
            }
        }

        private bool _SelectMisc_FromClipboard_FrameIsChecked;public bool SelectMisc_FromClipboard_FrameIsChecked { get => _SelectMisc_FromClipboard_FrameIsChecked; set => SetProperty(ref _SelectMisc_FromClipboard_FrameIsChecked, value); }

        private bool _SelectMisc_FromClipboard_JointIsChecked;public bool SelectMisc_FromClipboard_JointIsChecked { get => _SelectMisc_FromClipboard_JointIsChecked; set => SetProperty(ref _SelectMisc_FromClipboard_JointIsChecked, value); }

        private bool _SelectMisc_FromClipboard_AreaIsChecked;public bool SelectMisc_FromClipboard_AreaIsChecked { get => _SelectMisc_FromClipboard_AreaIsChecked; set => SetProperty(ref _SelectMisc_FromClipboard_AreaIsChecked, value); }

        private bool _SelectMisc_FromClipboard_CableIsChecked;public bool SelectMisc_FromClipboard_CableIsChecked { get => _SelectMisc_FromClipboard_CableIsChecked; set => SetProperty(ref _SelectMisc_FromClipboard_CableIsChecked, value); }

        private bool _SelectMisc_FromClipboard_LinkIsChecked;public bool SelectMisc_FromClipboard_LinkIsChecked { get => _SelectMisc_FromClipboard_LinkIsChecked; set => SetProperty(ref _SelectMisc_FromClipboard_LinkIsChecked, value); }

        private double _SelectMisc_FramesBasedOnLength_MaxLength;public double SelectMisc_FramesBasedOnLength_MaxLength { get => _SelectMisc_FramesBasedOnLength_MaxLength; set => SetProperty(ref _SelectMisc_FramesBasedOnLength_MaxLength, value); }

        private double _SelectMisc_FramesBasedOnLength_MinLength;public double SelectMisc_FramesBasedOnLength_MinLength { get => _SelectMisc_FramesBasedOnLength_MinLength; set => SetProperty(ref _SelectMisc_FramesBasedOnLength_MinLength, value); }

        private bool _SelectMisc_FramesBasedOnLength_FilterFromSelection;public bool SelectMisc_FramesBasedOnLength_FilterFromSelection { get => _SelectMisc_FramesBasedOnLength_FilterFromSelection; set => SetProperty(ref _SelectMisc_FramesBasedOnLength_FilterFromSelection, value); }
    }

    public class SelectionInfo_GroupInfoItem
    {
        public SelectionInfo_GroupInfoItem(string grpName)
        {
            GroupName = grpName;
            ItemCount = 0;
        }

        public string GroupName { get; set; }
        public int ItemCount { get; set; }
    }
}