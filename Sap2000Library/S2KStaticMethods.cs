using MathNet.Spatial.Euclidean;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Security.Cryptography;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Diagnostics;
using SAP2000v1;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;
using Sap2000Library.Other;
using Sap2000Library.SapObjects;
using Color = System.Drawing.Color;

namespace Sap2000Library
{
    public static class S2KStaticMethods
    {
        /// <summary>
        /// A helper to give an integer percent of a progress - to be used in a ProgressBar
        /// </summary>
        /// <param name="Current">The current value.</param>
        /// <param name="Maximum">The maximum value.</param>
        /// <returns></returns>
        public static int ProgressPercent(int Current, int Maximum)
        {
            return (int)Math.Round(((double)Current) * 100 / ((double)(Maximum)));
        }
        public static int ProgressPercent(long Current, long Maximum)
        {
            return (int)Math.Round(((double)Current) * 100 / ((double)(Maximum)));
        }

        /// <summary>
        /// Gets a unique name that is an MD5 of the timestep and of a guid.
        /// </summary>
        /// <param name="length">Maximum length of the string to return. Give a negative value to avoid truncating.</param>
        /// <returns>A unique(?) name.</returns>
        public static string UniqueName(int length = -1)
        {
            byte[] data1 = Encoding.UTF8.GetBytes($"{DateTime.Now:fffffssmmHHddMM}");
            byte[] data2 = Guid.NewGuid().ToByteArray();

            byte[] finalData = new byte[data1.Length + data2.Length];
            Array.Copy(data1, finalData, data1.Length);
            Array.Copy(data2, 0, finalData, data1.Length, data2.Length);

            MD5 md5Hash = MD5.Create();
            byte[] md5Data = md5Hash.ComputeHash(finalData);

            // Loop through each byte of the hashed data and inFormat each one as a hexadecimal string.
            StringBuilder sBuilder = new StringBuilder();
            foreach (byte _t in md5Data)
            {
                sBuilder.Append(_t.ToString("x2"));

                if (length > 0)
                {
                    if (sBuilder.Length == length) break;
                }
            }

            return sBuilder.ToString();
        }
        public static string CurrentTimeStamp()
        {
            return $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
        }

        private static readonly Random rand = new Random();
        /// <summary>
        /// Gets a new random integer *positive* that uses the global seed that has been initialized with the program.
        /// </summary>
        /// <param name="max">The maximum value of the integer.</param>
        /// <returns>The random value.</returns>
        public static int RandomInteger(int max = 200000)
        {
            return rand.Next(max);
        }

        public static string AppendExceptionMessage(this Exception exception)
        {
            return $"{Environment.NewLine}{exception.Message}{Environment.NewLine}{exception.StackTrace}";
        }

        public static MessageBoxResult ShowErrorMessageBox(string message, Exception exception, string title = "Error!",  MessageBoxImage image = MessageBoxImage.Error, MessageBoxButton button = MessageBoxButton.OK)
        {
            return MessageBox.Show(message + exception.AppendExceptionMessage(), title, button, image);
        }
        public static MessageBoxResult ShowWarningMessageBox(string message, string title = "Warning!", MessageBoxImage inImage = MessageBoxImage.Warning, MessageBoxButton inButton = MessageBoxButton.OK)
        {
            return MessageBox.Show(message, title, inButton, inImage);
        }

        /// <summary>
        /// Checks if an integer is even.
        /// </summary>
        /// <param name="number">The number to check.</param>
        /// <returns>True if even. False if odd.</returns>
        public static bool IsEven(this Int32 number)
        {
            return number % 2 == 0;
        }

        public static Point3D AddVector(this Point3D point, Vector3D vector)
        {
            return new Point3D(point.X + vector.X,
                point.Y + vector.Y,
                point.Z + vector.Z);
        }
        public static bool IsVectorVertical(this Vector3D vector)
        {
            double sine = Math.Sin(vector.AngleTo(new Vector3D(0d, 0d, 1d)).Radians);
            return sine < 10e-3;
        }
        public static bool IsVectorVertical(this UnitVector3D vector)
        {
            double sine = Math.Sin(vector.AngleTo(new Vector3D(0d, 0d, 1d)).Radians);
            return sine < 10e-3;
        }
        public static double[] ToDoubleArray(this Point3D inPnt)
        {
            if (inPnt == null) return null;
            return new double[] { inPnt.X, inPnt.Y, inPnt.Z };
        }
        public static Point3D SubtractCoordinates(this Point3D pointFromWhichSubstract, Point3D pointToSubtract)
        {
            if (pointToSubtract == null) throw new S2KHelperException("pointToSubtract cannot be null.");
            return new Point3D(pointFromWhichSubstract.X - pointToSubtract.X,
                pointFromWhichSubstract.Y - pointToSubtract.Y,
                pointFromWhichSubstract.Z - pointToSubtract.Z);
        }

        public static void FillAllJointConstraints(this List<SapPoint> points, bool inUpdateInterface = false)
        {
            if (inUpdateInterface) BusyOverlayBindings.I.SetDeterminate("SAP2000: Getting Joint Constraint Information.", "Joint");
            
            for (int i = 0; i < points.Count; i++)
            {
                SapPoint item = points[i];
                if (inUpdateInterface) BusyOverlayBindings.I.UpdateProgress(i, points.Count, item.Name);

                // Forces the refresh
                _ = item.JointConstraintNames != null;
            }
        }

        //If you get 'dllimport unknown'-, then add 'using System.Runtime.InteropServices;'
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject([In] IntPtr hObject);
        internal static ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        public static ImageSource GetImageSource(this Bitmap inBitmap)
        {
            return ImageSourceFromBitmap(inBitmap);
        }
        
        const int ERROR_SHARING_VIOLATION = 32;
        const int ERROR_LOCK_VIOLATION = 33;
        private static bool IsFileLocked(Exception exception)
        {
            int errorCode = Marshal.GetHRForException(exception) & ((1 << 16) - 1);
            return errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION;
        }
        public static bool CanReadFile(string filePath)
        {
            //Try-Catch so we dont crash the program and can check the exception
            try
            {
                //The "using" is important because FileStream implements IDisposable and
                //"using" will avoid a heap exhaustion situation when too many handles  
                //are left undisposed.
                using (FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    if (fileStream != null) fileStream.Close();  //This line is me being overly cautious, fileStream will never be null unless an exception occurs... and I know the "using" does it but its helpful to be explicit - especially when we encounter errors - at least for me anyway!
                }
            }
            catch (IOException ex)
            {
                //THE FUNKY MAGIC - TO SEE IF THIS FILE REALLY IS LOCKED!!!
                if (IsFileLocked(ex))
                {
                    // do something, eg File.Copy or present the user with a MsgBox - I do not recommend Killing the process that is locking the file
                    return false;
                }
            }
            finally
            { }
            return true;
        }

        public static void DebugMessage(string message, 
            [CallerMemberName] string callMember = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            //Debug.WriteLine($"{message} | {callMember} Line:{lineNumber} | Thread Name: {Thread.CurrentThread.Name} | StackTrace: {Environment.StackTrace}");
            Debug.WriteLine($"{message} | {callMember} Line:{lineNumber} | Thread Name: {Thread.CurrentThread.Name}");
        }

        public static object[,] ConvertToObjectArray2(this DataTable dt, bool inIncludeColumnHeaders = true)
        {
            if (inIncludeColumnHeaders)
            {
                DataRowCollection rows = dt.Rows;
                int rowCount = rows.Count + 1;
                int colCount = dt.Columns.Count;

                object[,] result = new object[rowCount, colCount];

                for (int j = 0; j < colCount; j++)
                {
                    result[0, j] = dt.Columns[j].ColumnName;
                }

                for (int i = 0; i < rowCount - 1; i++)
                {
                    DataRow row = rows[i];
                    for (int j = 0; j < colCount; j++)
                    {
                        result[i + 1, j] = row[j];
                    }
                }

                return result;
            }
            else
            {
                DataRowCollection rows = dt.Rows;
                int rowCount = rows.Count;
                int colCount = dt.Columns.Count;

                object[,] result = new object[rowCount, colCount];

                for (int i = 0; i < rowCount; i++)
                {
                    DataRow row = rows[i];
                    for (int j = 0; j < colCount; j++)
                    {
                        result[i, j] = row[j];
                    }
                }

                return result;
            }

        }


        public enum TransformColorFormat
        {
            RGB, RGBA, ARGB, SAP2000
        }
        public static int ColorToDecimal(System.Drawing.Color inColor, TransformColorFormat inFormat = TransformColorFormat.SAP2000)
        {
            switch (inFormat)
            {
                default:
                case TransformColorFormat.RGB:
                    return inColor.R << 16 | inColor.G << 8 | inColor.B;
                case TransformColorFormat.RGBA:
                    return inColor.R << 24 | inColor.G << 16 | inColor.B << 8 | inColor.A;
                case TransformColorFormat.ARGB:
                    return inColor.A << 24 | inColor.R << 16 | inColor.G << 8 | inColor.B;
                case TransformColorFormat.SAP2000:
                    return inColor.R | inColor.G << 8 | inColor.B << 16;
            }
        }
        public static System.Drawing.Color DecimalToColor(int inVal, TransformColorFormat inFormat = TransformColorFormat.SAP2000)
        {
            switch (inFormat)
            {
                default:
                case TransformColorFormat.RGB:
                    return System.Drawing.Color.FromArgb((inVal >> 16) & 0xFF, (inVal >> 8) & 0xFF, inVal & 0xFF);
                case TransformColorFormat.RGBA:
                    return System.Drawing.Color.FromArgb(inVal & 0xFF, (inVal >> 24) & 0xFF, (inVal >> 16) & 0xFF, (inVal >> 8) & 0xFF);
                case TransformColorFormat.ARGB:
                    return System.Drawing.Color.FromArgb((inVal >> 24) & 0xFF, (inVal >> 16) & 0xFF, (inVal >> 8) & 0xFF, inVal & 0xFF);
                case TransformColorFormat.SAP2000:
                    int red = (inVal) & 0xFF;
                    int green = (inVal >> 8) & 0xFF;
                    int blue = (inVal >> 16) & 0xFF;
                    return System.Drawing.Color.FromArgb(255, red, green, blue);
            }
        }
    }
}
