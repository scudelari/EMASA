using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace StepwiseSpheres
{
    /// <remarks>
    /// This inApplication's main class. The class must be Public.
    /// </remarks>
    public class StepwiseSphereExternalApp : IExternalApplication
    {
        // Both OnStartup and OnShutdown must be implemented as public method
        public Result OnStartup(UIControlledApplication inApplication)
        {
            // Add a new ribbon panel
            RibbonPanel ribbonPanel = inApplication.CreateRibbonPanel("Emasa");

            // Create a push button to trigger a command add it to the ribbon panel.
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            PushButtonData buttonData = new PushButtonData("cmdEmsStepwiseSphere", "Stepwise Spheres", thisAssemblyPath, "StepwiseSpheres.StepwiseSphereExternalCmd");

            PushButton pushButton = ribbonPanel.AddItem(buttonData) as PushButton;

            // Optionally, other properties may be assigned to the button
            // a) tool-tip
            pushButton.ToolTip = "Create stepwise spheres based on the data contained in the SqLite file.";

            // b) large bitmap
            BitmapImage largeImage = ConvertImage(Properties.Resources.Revit_E);
            pushButton.LargeImage = largeImage;

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication inApplication)
        {
            // nothing to clean up in this simple case
            return Result.Succeeded;
        }

        #region Icon Helpers

        public BitmapImage ConvertImage(Image img)
        {
            using (var memory = new MemoryStream())
            {
                img.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        #endregion
    }
    /// <remarks>
    /// The "HelloWorld" external command. The class must be Public.
    /// </remarks>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class StepwiseSphereExternalCmd : IExternalCommand
    {
        // The main Execute method (inherited from IExternalCommand) must be public
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
        {
            // TaskDialog.Show("Revit", "Hello World");
            StepwiseSpheresDialog dlg = new StepwiseSpheresDialog();
            dlg.Revit_ExternalCommandData = revit;
            dlg.Revit_ElementSet = elements;

            // Shows the plugin dialog
            dlg.ShowDialog();

            // Means there was an error
            if (!string.IsNullOrWhiteSpace(dlg.Revit_OutputMessage))
            {
                message = dlg.Revit_OutputMessage;
                return Autodesk.Revit.UI.Result.Failed;
            }

            return Autodesk.Revit.UI.Result.Succeeded;
        }
    }
}
