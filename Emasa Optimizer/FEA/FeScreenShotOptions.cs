using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Emasa_Optimizer.Properties;
using Prism.Mvvm;

namespace Emasa_Optimizer.FEA
{
    public class FeScreenShotOptions : BindableBase
    {
        #region ScreenShot Output
        private ImageCaptureViewDirectionEnum _imageCapture_ViewDirections = (ImageCaptureViewDirectionEnum)Enum.Parse(typeof(ImageCaptureViewDirectionEnum), Settings.Default.Default_ImageCapture_ViewDirections);
        public ImageCaptureViewDirectionEnum ImageCapture_ViewDirections
        {
            get => _imageCapture_ViewDirections;
            set => SetProperty(ref _imageCapture_ViewDirections, value);
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

        public double ImageCapture_AnsysHelper_XDir
        {
            get
            {
                switch (ImageCapture_ViewDirections)
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
        }
        public double ImageCapture_AnsysHelper_YDir
        {
            get
            {
                switch (ImageCapture_ViewDirections)
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
        }
        public double ImageCapture_AnsysHelper_ZDir
        {
            get
            {
                switch (ImageCapture_ViewDirections)
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
        }
        public double ImageCapture_AnsysHelper_RotateToAlign => ImageCapture_ViewDirections == ImageCaptureViewDirectionEnum.Top_Towards_ZNeg ? -90d : Double.NaN;
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

}
