using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Emasa_Optimizer.Bindings;
using Emasa_Optimizer.FEA.Results;
using Emasa_Optimizer.Opt.ProbQuantity;
using Emasa_Optimizer.Properties;
using MintPlayer.ObservableCollection;
using Prism.Mvvm;

namespace Emasa_Optimizer.Opt
{
    public class ScreenShotOptions : BindableBase
    {
        public ScreenShotOptions()
        {
            // Adds the special screenshots
            SpecialRhinoDisplayScreenshotInstance = new SpecialScreenShot_RhinoDisplay();
            SpecialFeModelOverviewKeypointsScreenshotKeypointsInstance = new SpecialScreenShot_FeModelOverview_Keypoints();
            SpecialFeModelOverviewKeypointsScreenshotLinesInstance = new SpecialScreenShot_FeModelOverview_Lines();
            _screenShotList = new ObservableCollection<IProblemQuantitySource>()
                {
                SpecialRhinoDisplayScreenshotInstance,
                SpecialFeModelOverviewKeypointsScreenshotKeypointsInstance,
                SpecialFeModelOverviewKeypointsScreenshotLinesInstance,
                };

            // The list of FeResults has already been established in the FeOptions object - adds all their references here 
            _screenShotList.AddRange(AppSS.I.FeOpt.Wpf_AllResultsForInterface.OfType<IProblemQuantitySource>());
            
            // Creates the view concerning the Screenshots
            Wpf_ScreenshotList = (new CollectionViewSource() { Source = _screenShotList }).View;
            Wpf_ScreenshotList.Filter += inO =>
            {
                // Is it a true FeResult
                if ((inO is FeResultClassification item)) return item.IsSupportedByCurrentSolver && item.OutputData_IsSelected;

                // Special screenshots
                return true;
            };
            // Setting live shaping
            if (Wpf_ScreenshotList is ICollectionViewLiveShaping ls2)
            {
                ls2.LiveFilteringProperties.Add("IsSupportedByCurrentSolver");
                ls2.LiveFilteringProperties.Add("OutputData_IsSelected");
                ls2.IsLiveFiltering = true;
            }
            else throw new Exception($"List does not accept ICollectionViewLiveShaping.");

        }

        #region List of Special Screenshots that are not of FeResultClassification
        private readonly MintPlayer.ObservableCollection.ObservableCollection<IProblemQuantitySource> _screenShotList;
        public SpecialScreenShot_RhinoDisplay SpecialRhinoDisplayScreenshotInstance { get; private set; }
        public SpecialScreenShot_FeModelOverview_Keypoints SpecialFeModelOverviewKeypointsScreenshotKeypointsInstance { get; private set; }
        public SpecialScreenShot_FeModelOverview_Lines SpecialFeModelOverviewKeypointsScreenshotLinesInstance { get; private set; }
        public ICollectionView Wpf_ScreenshotList { get; } // *grouped* and *sorted* - Used in the selection for output interface
        #endregion

        #region ScreenShot Output
        private ImageCaptureViewDirectionEnum _imageCapture_ViewDirections = (ImageCaptureViewDirectionEnum)Enum.Parse(typeof(ImageCaptureViewDirectionEnum), Settings.Default.Default_ImageCapture_ViewDirections);
        public ImageCaptureViewDirectionEnum ImageCapture_ViewDirections
        {
            get => _imageCapture_ViewDirections;
            set
            {
                SetProperty(ref _imageCapture_ViewDirections, value);

                RaisePropertyChanged("ImageCapture_ViewDirectionsEnumerable");
                RaisePropertyChanged("WfpCaption_FeSolverTypeEnum");
            }
        }

        public IEnumerable<ImageCaptureViewDirectionEnum> ImageCapture_ViewDirectionsEnumerable => Enum.GetValues(typeof(ImageCaptureViewDirectionEnum)).OfType<ImageCaptureViewDirectionEnum>().Where(value => _imageCapture_ViewDirections.HasFlag(value));

        private double _imageCapture_Extrude_Multiplier = Settings.Default.Default_ImageCapture_Extrude_Multiplier;
        public double ImageCapture_Extrude_Multiplier
        {
            get => _imageCapture_Extrude_Multiplier;
            set => SetProperty(ref _imageCapture_Extrude_Multiplier, value);
        }
        private bool _imageCapture_UndeformedShadow = Settings.Default.Default_ImageCapture_UndeformedShadow;
        public bool ImageCapture_UndeformedShadow
        {
            get => _imageCapture_UndeformedShadow;
            set => SetProperty(ref _imageCapture_UndeformedShadow, value);
        }
        private bool _imageCapture_DeformedShape = Settings.Default.Default_ImageCapture_DeformedShape;
        public bool ImageCapture_DeformedShape
        {
            get => _imageCapture_DeformedShape;
            set => SetProperty(ref _imageCapture_DeformedShape, value);
        }

        public double ImageCapture_AnsysHelper_XDir(ImageCaptureViewDirectionEnum inImageDir)
        {
            switch (inImageDir)
            {
                case ImageCaptureViewDirectionEnum.Top_Towards_ZNeg:
                    return 0d;
                    break;

                case ImageCaptureViewDirectionEnum.Front_Towards_YPos:
                    return 0d;
                    break;

                case ImageCaptureViewDirectionEnum.Back_Towards_YNeg:
                    return 0d;
                    break;

                case ImageCaptureViewDirectionEnum.Right_Towards_XNeg:
                    return 1d;
                    break;

                case ImageCaptureViewDirectionEnum.Left_Towards_XPos:
                    return -1d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Front_Edge:
                    return 0.2d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Back_Edge:
                    return 0.2d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Right_Edge:
                    return 1d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Left_Edge:
                    return -1d;
                    break;


                case ImageCaptureViewDirectionEnum.Perspective_TFR_Corner:
                    return 1d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_TFL_Corner:
                    return -1d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_TBR_Corner:
                    return 1d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_TBL_Corner:
                    return -1d;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        public double ImageCapture_AnsysHelper_YDir(ImageCaptureViewDirectionEnum inImageDir)
        {
            switch (inImageDir)
            {
                case ImageCaptureViewDirectionEnum.Top_Towards_ZNeg:
                    return 0d;
                    break;

                case ImageCaptureViewDirectionEnum.Front_Towards_YPos:
                    return -1d;
                    break;

                case ImageCaptureViewDirectionEnum.Back_Towards_YNeg:
                    return 1d;
                    break;

                case ImageCaptureViewDirectionEnum.Right_Towards_XNeg:
                    return 0d;
                    break;

                case ImageCaptureViewDirectionEnum.Left_Towards_XPos:
                    return 0d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Front_Edge:
                    return -1d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Back_Edge:
                    return 1d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Right_Edge:
                    return 0.2d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Left_Edge:
                    return 0.2d;
                    break;


                case ImageCaptureViewDirectionEnum.Perspective_TFR_Corner:
                    return -1d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_TFL_Corner:
                    return -1d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_TBR_Corner:
                    return 1d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_TBL_Corner:
                    return 1d;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        public double ImageCapture_AnsysHelper_ZDir(ImageCaptureViewDirectionEnum inImageDir)
        {
            switch (inImageDir)
            {
                case ImageCaptureViewDirectionEnum.Top_Towards_ZNeg:
                    return 1d;
                    break;

                case ImageCaptureViewDirectionEnum.Front_Towards_YPos:
                    return 0d;
                    break;

                case ImageCaptureViewDirectionEnum.Back_Towards_YNeg:
                    return 0d;
                    break;

                case ImageCaptureViewDirectionEnum.Right_Towards_XNeg:
                    return 0d;
                    break;

                case ImageCaptureViewDirectionEnum.Left_Towards_XPos:
                    return 0d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Front_Edge:
                    return 1d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Back_Edge:
                    return 1d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Right_Edge:
                    return 1d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Left_Edge:
                    return 1d;
                    break;


                case ImageCaptureViewDirectionEnum.Perspective_TFR_Corner:
                    return 1d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_TFL_Corner:
                    return 1d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_TBR_Corner:
                    return 1d;
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_TBL_Corner:
                    return 1d;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        public double ImageCapture_AnsysHelper_RotateToAlign(ImageCaptureViewDirectionEnum inImageDir) => inImageDir == ImageCaptureViewDirectionEnum.Top_Towards_ZNeg ? -90d : Double.NaN;
        #endregion
        
        #region Friendly String Helpers
        public static string GetFriendlyEnumName(ImageCaptureViewDirectionEnum inImageDirection)
        {
            switch (inImageDirection)
            {
                case ImageCaptureViewDirectionEnum.Top_Towards_ZNeg:
                    return "Top";
                    break;

                case ImageCaptureViewDirectionEnum.Front_Towards_YPos:
                    return "Front";
                    break;

                case ImageCaptureViewDirectionEnum.Back_Towards_YNeg:
                    return "Back";
                    break;

                case ImageCaptureViewDirectionEnum.Right_Towards_XNeg:
                    return "Right";
                    break;

                case ImageCaptureViewDirectionEnum.Left_Towards_XPos:
                    return "Left";
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Front_Edge:
                    return "Top-Front";
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Back_Edge:
                    return "Top-Back";
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Right_Edge:
                    return "Top-Right";
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_Top_Left_Edge:
                    return "Top-Left";
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_TFR_Corner:
                    return "Top-Front-Right";
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_TFL_Corner:
                    return "Top-Front-Left";
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_TBR_Corner:
                    return "Top-Back-Right";
                    break;

                case ImageCaptureViewDirectionEnum.Perspective_TBL_Corner:
                    return "Top-Back-Left";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(inImageDirection), inImageDirection, null);
            }
        }
        #endregion

        #region Wpf Properties
        private ImageCaptureViewDirectionEnum _selectedDisplayDirection;
        public ImageCaptureViewDirectionEnum SelectedDisplayDirection
        {
            get => _selectedDisplayDirection;
            set => SetProperty(ref _selectedDisplayDirection, value);
        }
        public List<KeyValuePair<ImageCaptureViewDirectionEnum, string>> WfpCaption_ImageCaptureViewDirectionEnum
        {
            get
            {
                List<KeyValuePair<ImageCaptureViewDirectionEnum, string>> tmpList = new List<KeyValuePair<ImageCaptureViewDirectionEnum, string>>();

                foreach (ImageCaptureViewDirectionEnum imageCaptureViewDirectionEnum in ImageCapture_ViewDirectionsEnumerable)
                {
                    switch (imageCaptureViewDirectionEnum)
                    {
                        case ImageCaptureViewDirectionEnum.Top_Towards_ZNeg:
                            tmpList.Add( new KeyValuePair<ImageCaptureViewDirectionEnum, string>(imageCaptureViewDirectionEnum, "Axial - Top"));
                            break;

                        case ImageCaptureViewDirectionEnum.Front_Towards_YPos:
                            tmpList.Add( new KeyValuePair<ImageCaptureViewDirectionEnum, string>(imageCaptureViewDirectionEnum, "Axial - Front"));
                            break;

                        case ImageCaptureViewDirectionEnum.Back_Towards_YNeg:
                            tmpList.Add( new KeyValuePair<ImageCaptureViewDirectionEnum, string>(imageCaptureViewDirectionEnum, "Axial - Back"));
                            break;

                        case ImageCaptureViewDirectionEnum.Right_Towards_XNeg:
                            tmpList.Add( new KeyValuePair<ImageCaptureViewDirectionEnum, string>(imageCaptureViewDirectionEnum, "Axial - Right"));
                            break;

                        case ImageCaptureViewDirectionEnum.Left_Towards_XPos:
                            tmpList.Add( new KeyValuePair<ImageCaptureViewDirectionEnum, string>(imageCaptureViewDirectionEnum, "Axial - Left"));
                            break;

                        case ImageCaptureViewDirectionEnum.Perspective_Top_Front_Edge:
                            tmpList.Add( new KeyValuePair<ImageCaptureViewDirectionEnum, string>(imageCaptureViewDirectionEnum, "Edges - Top-Front"));
                            break;

                        case ImageCaptureViewDirectionEnum.Perspective_Top_Back_Edge:
                            tmpList.Add( new KeyValuePair<ImageCaptureViewDirectionEnum, string>(imageCaptureViewDirectionEnum, "Edges - Top-Back"));
                            break;

                        case ImageCaptureViewDirectionEnum.Perspective_Top_Right_Edge:
                            tmpList.Add( new KeyValuePair<ImageCaptureViewDirectionEnum, string>(imageCaptureViewDirectionEnum, "Edges - Top-Right"));
                            break;

                        case ImageCaptureViewDirectionEnum.Perspective_Top_Left_Edge:
                            tmpList.Add( new KeyValuePair<ImageCaptureViewDirectionEnum, string>(imageCaptureViewDirectionEnum, "Edges - Top-Left"));
                            break;

                        case ImageCaptureViewDirectionEnum.Perspective_TFR_Corner:
                            tmpList.Add( new KeyValuePair<ImageCaptureViewDirectionEnum, string>(imageCaptureViewDirectionEnum, "Corners - Top-Front-Right"));
                            break;

                        case ImageCaptureViewDirectionEnum.Perspective_TFL_Corner:
                            tmpList.Add( new KeyValuePair<ImageCaptureViewDirectionEnum, string>(imageCaptureViewDirectionEnum, "Corners - Top-Front-Left"));
                            break;

                        case ImageCaptureViewDirectionEnum.Perspective_TBR_Corner:
                            tmpList.Add( new KeyValuePair<ImageCaptureViewDirectionEnum, string>(imageCaptureViewDirectionEnum, "Corners - Top-Back-Right"));
                            break;

                        case ImageCaptureViewDirectionEnum.Perspective_TBL_Corner:
                            tmpList.Add( new KeyValuePair<ImageCaptureViewDirectionEnum, string>(imageCaptureViewDirectionEnum, "Corners - Top-Back-Left"));
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                tmpList.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));

                SelectedDisplayDirection = ImageCapture_ViewDirectionsEnumerable.FirstOrDefault();

                return tmpList;
            }
        }

        private IProblemQuantitySource _selectedDisplayImageResultClassification;
        public IProblemQuantitySource SelectedDisplayImageResultClassification
        {
            get => _selectedDisplayImageResultClassification;
            set => SetProperty(ref _selectedDisplayImageResultClassification, value);
        }
        #endregion
    }

    [Flags]
    public enum ImageCaptureViewDirectionEnum
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

    public class SpecialScreenShot_RhinoDisplay : IProblemQuantitySource
    {
        public string ResultFamilyGroupName => "Screenshot";
        public string ResultTypeDescription => "";
        public string TargetShapeDescription => "Rhino";
        public string ResultTypeExplanation => "A ScreenShot of the Rhino Geometry.";

        public string Wpf_ProblemQuantityName => "Screenshot";
        public string Wpf_ProblemQuantityGroup => "Rhino";
        public string Wpf_Explanation => "A ScreenShot of the Rhino Geometry.";
        
        public string ScreenShotFileName => "RhinoScreenShot";
        

        public bool IsGhGeometryDoubleListData => throw new InvalidOperationException($"{this.GetType()} does not support this method.");
        public bool IsFiniteElementData => throw new InvalidOperationException($"{this.GetType()} does not support this method.");
        public void AddProblemQuantity_FunctionObjective()
        {
            throw new InvalidOperationException($"{this.GetType()} does not support this method.");
        }
        public void AddProblemQuantity_ConstraintObjective()
        {
            throw new InvalidOperationException($"{this.GetType()} does not support this method.");
        }
        public void AddProblemQuantity_OutputOnly()
        {
            throw new InvalidOperationException($"{this.GetType()} does not support this method.");
        }
        public string DataTableName => "Screenshot has no table!";

        // Must have to match live filtering
        public bool IsSupportedByCurrentSolver => true;
        public bool OutputData_IsSelected => true;
        public string ConcernedResultColumnName => null;
    }
    public class SpecialScreenShot_FeModelOverview_Keypoints : IProblemQuantitySource
    {
        public string ResultFamilyGroupName => "Screenshot";
        public string ResultTypeDescription => "Keypoints";
        public string TargetShapeDescription => "Fe Model Overview";
        public string ResultTypeExplanation => "General view of the Fe model's configuration.";

        public string Wpf_ProblemQuantityName => "Keypoints";
        public string Wpf_ProblemQuantityGroup => "Fe Model Overview";
        public string Wpf_Explanation => "General view of the Fe model's configuration.";

        public string ScreenShotFileName => $"ems_image_FeModelOverview_Keypoints";

        public bool IsGhGeometryDoubleListData => throw new InvalidOperationException($"{this.GetType()} does not support this method.");
        public bool IsFiniteElementData => throw new InvalidOperationException($"{this.GetType()} does not support this method.");
        public void AddProblemQuantity_FunctionObjective()
        {
            throw new InvalidOperationException($"{this.GetType()} does not support this method.");
        }
        public void AddProblemQuantity_ConstraintObjective()
        {
            throw new InvalidOperationException($"{this.GetType()} does not support this method.");
        }
        public void AddProblemQuantity_OutputOnly()
        {
            throw new InvalidOperationException($"{this.GetType()} does not support this method.");
        }
        public string DataTableName => "Screenshot has no table!";

        // Must have to match live filtering
        public bool IsSupportedByCurrentSolver => true;
        public bool OutputData_IsSelected => true;
        public string ConcernedResultColumnName => null;
    }
    public class SpecialScreenShot_FeModelOverview_Lines : IProblemQuantitySource
    {
        public string ResultFamilyGroupName => "Screenshot";
        public string ResultTypeDescription => "Lines";
        public string TargetShapeDescription => "Fe Model Overview";
        public string ResultTypeExplanation => "General view of the Fe model's configuration.";

        public string Wpf_ProblemQuantityName => "Lines";
        public string Wpf_ProblemQuantityGroup => "Fe Model Overview";
        public string Wpf_Explanation => "General view of the Fe model's configuration.";

        public string ScreenShotFileName => $"ems_image_FeModelOverview_Lines";

        public bool IsGhGeometryDoubleListData => throw new InvalidOperationException($"{this.GetType()} does not support this method.");
        public bool IsFiniteElementData => throw new InvalidOperationException($"{this.GetType()} does not support this method.");
        public void AddProblemQuantity_FunctionObjective()
        {
            throw new InvalidOperationException($"{this.GetType()} does not support this method.");
        }
        public void AddProblemQuantity_ConstraintObjective()
        {
            throw new InvalidOperationException($"{this.GetType()} does not support this method.");
        }
        public void AddProblemQuantity_OutputOnly()
        {
            throw new InvalidOperationException($"{this.GetType()} does not support this method.");
        }
        public string DataTableName => "Screenshot has no table!";

        // Must have to match live filtering
        public bool IsSupportedByCurrentSolver => true;
        public bool OutputData_IsSelected => true;
        public string ConcernedResultColumnName => null;
    }
}
