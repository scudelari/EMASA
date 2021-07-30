using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using BaseWPFLibrary.Forms;
using EmasaSapTools.Resources;
using MathNet.Spatial.Euclidean;
using Microsoft.Win32;
using Sap2000Library;
using Sap2000Library.DataClasses;
using Sap2000Library.SapObjects;

namespace EmasaSapTools.Bindings
{
    public class AlignAreaBindings : BindableSingleton<AlignAreaBindings>
    {
        private AlignAreaBindings()
        {
        }

        public override void SetOrReset()
        {
            Plane31_IsChecked = true;
            FlipLastButton_IsEnabled = false;
        }

        private bool _plane31_IsChecked;

        public bool Plane31_IsChecked
        {
            get => _plane31_IsChecked;
            set => SetProperty(ref _plane31_IsChecked, value);
        }

        private bool _Plane32_IsChecked;

        public bool Plane32_IsChecked
        {
            get => _Plane32_IsChecked;
            set => SetProperty(ref _Plane32_IsChecked, value);
        }

        private bool _FlipLastButton_IsEnabled;

        public bool FlipLastButton_IsEnabled
        {
            get => _FlipLastButton_IsEnabled;
            set => SetProperty(ref _FlipLastButton_IsEnabled, value);
        }

        public AreaAdvancedAxes_Plane AreaAlignPlaneOption
        {
            get
            {
                if (Plane31_IsChecked) return AreaAdvancedAxes_Plane.Plane31;
                if (Plane32_IsChecked) return AreaAdvancedAxes_Plane.Plane32;
                return AreaAdvancedAxes_Plane.Plane31;
            }
        }






        #region Points Aling Z

        private Plane? _pointsAlignZ_PlaneToAlign = null;
        public Plane? PointsAlignZ_PlaneToAlign
        {
            get => _pointsAlignZ_PlaneToAlign;
            set
            {
                PointsAlignZ_AlignIsEnabled = value != null;
                _pointsAlignZ_PlaneToAlign = value;
            }
        }
        
        private bool _pointsAlignZ_AlignIsEnabled = false;
        public bool PointsAlignZ_AlignIsEnabled
        {
            get => _pointsAlignZ_AlignIsEnabled;
            set => SetProperty(ref _pointsAlignZ_AlignIsEnabled, value);
        }
        

        public async void PointsAlignZ_GetJointsForPlane()
        {
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    BusyOverlayBindings.I.Title = $"Getting target Plane";

                    BusyOverlayBindings.I.SetIndeterminate("Getting Selected Points from SAP2000");
                    List<SapPoint> points = S2KModel.SM.PointMan.GetSelected();

                    if (points.Count != 3)
                    {
                        PointsAlignZ_PlaneToAlign = null;
                        throw new Exception("You must select exactly 3 points in SAP2000.");
                    }

                    try
                    {
                        Plane p = Plane.FromPoints(points[0].Point, points[1].Point, points[2].Point);

                        PointsAlignZ_PlaneToAlign = p;
                    }
                    catch 
                    {
                        throw new Exception("Could not create a plane definition from these points. Are they colinear?.");
                    }
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

        public async void PointsAlignZ_Move()
        {
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    BusyOverlayBindings.I.Title = $"Moving selected points to plane by changing Z only.";

                    BusyOverlayBindings.I.SetIndeterminate("Getting Selected Points from SAP2000.");
                    List<SapPoint> points = S2KModel.SM.PointMan.GetSelected();

                    S2KModel.SM.WindowVisible = false;

                    BusyOverlayBindings.I.SetDeterminate("Moving Joints.", "Joint");
                    for (int index = 0; index < points.Count; index++)
                    {
                        SapPoint sapPoint = points[index];
                        BusyOverlayBindings.I.UpdateProgress(index, points.Count, sapPoint.Name);

                        Ray3D ray = new Ray3D(sapPoint.Point, UnitVector3D.ZAxis);

                        Point3D newP = PointsAlignZ_PlaneToAlign.Value.IntersectionWith(ray);

                        if (newP.DistanceTo(sapPoint.Point) > 0.001) sapPoint.MoveTo(newP, false);

                        
                    }


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
                S2KModel.SM.WindowVisible = true;
                OnEndCommand();
            }
        }

        public async void PointsAlignZ_NewCoordTableToClipboard()
        {
            StringBuilder sb = new StringBuilder();
            Clipboard.Clear();
            
            try
            {
                OnBeginCommand();

                void lf_Work()
                {
                    BusyOverlayBindings.I.Title = $"Moving selected points to plane by changing Z only - Data to Clipboard.";

                    BusyOverlayBindings.I.SetIndeterminate("Getting Selected Points from SAP2000.");
                    List<SapPoint> points = S2KModel.SM.PointMan.GetSelected();

                    S2KModel.SM.WindowVisible = false;

                    BusyOverlayBindings.I.SetDeterminate("Calculating new coordinates.", "Joint");
                    for (int index = 0; index < points.Count; index++)
                    {
                        SapPoint sapPoint = points[index];
                        BusyOverlayBindings.I.UpdateProgress(index, points.Count, sapPoint.Name);

                        Ray3D ray = new Ray3D(sapPoint.Point, UnitVector3D.ZAxis);

                        Point3D newP = PointsAlignZ_PlaneToAlign.Value.IntersectionWith(ray);

                        if (newP.DistanceTo(sapPoint.Point) > 0.001)
                        {
                            sb.AppendLine($"{sapPoint.Name},{newP.X},{newP.Y},{newP.Z}");
                        }
                        else
                        {
                            sb.AppendLine($"{sapPoint.Name},{sapPoint.X},{sapPoint.Y},{sapPoint.Z}");
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
                ExceptionViewer.Show(ex);

            }
            finally
            {
                S2KModel.SM.WindowVisible = true;
                OnEndCommand();

                Clipboard.SetText(sb.ToString());
            }
        }
        #endregion
    }
}