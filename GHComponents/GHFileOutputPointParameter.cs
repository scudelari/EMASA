using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GHComponents
{
    public class GHFileOutputPointParameter : GH_Param<GH_Point>
    {
        public GHFileOutputPointParameter() :
            base("File Output Point", "", "Receives a List of Points and Writes its Value", "Emasa", "Output", GH_ParamAccess.tree)
        {
        }

        public string SubDir => this.SubCategory;
        public override Guid ComponentGuid => new Guid("0c0ba380-fb37-4ff1-86ef-cf82514dbaf2");
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override System.Drawing.Bitmap Icon => Properties.Resources.GH_Icon_OutputPoints;

        // Helper functions
        public string VarFilePath(GH_Document inDocument = null, string inVarName = null)
        {
            if (inDocument == null) inDocument = OnPingDocument();
            if (inVarName == null) inVarName = NickName;

            if (!inDocument.IsFilePathDefined) return null;
            if (string.IsNullOrWhiteSpace(inDocument.FilePath)) return null;

            try
            {
                // Gets the document
                string projectFolder = Path.GetDirectoryName(inDocument.FilePath);
                string ghFilename = Path.GetFileName(inDocument.FilePath);

                string targetDir = Path.Combine(projectFolder, ghFilename + "_data", SubDir);

                DirectoryInfo dirInfo = new DirectoryInfo(targetDir);
                if (!dirInfo.Exists) dirInfo.Create();

                return Path.Combine(targetDir, $"{inVarName}.PointList");
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Image PublicIcon => Icon;

        public override void CreateAttributes()
        {
            m_attributes = new GHFileOutputPointAttributes(this);
        }

        public override bool AppendMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, this.Name);
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Variable Name:");
            Menu_AppendObjectNameEx(menu);

            return true;
        }

        public string DisplayString
        {
            get
            {
                if (SourceCount == 0 || Sources[0].VolatileData.IsEmpty || Sources[0].VolatileData.DataCount == 0) return "No Data";

                int countInputs = 0;
                foreach (IGH_Param source in Sources)
                {
                    countInputs += source.VolatileData.DataCount;
                }

                return countInputs.ToString();
            }
        }

        public override string NickName
        {
            get => base.NickName;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                if (base.NickName == value) return;

                try
                {
                    string oldFile = VarFilePath(inVarName: NickName);
                    string newFile = VarFilePath(inVarName: value);

                    // If the old file exists, we must move it
                    if (File.Exists(oldFile))
                    {
                        // The new file already exists; the change of name is aborted
                        if (File.Exists(newFile)) return;

                        File.Copy(oldFile, newFile);
                        File.Delete(oldFile);
                    }
                }
                catch
                {
                }

                // Sets the value
                base.NickName = value;

                this.CollectData();
            }
        }

        public override void AddedToDocument(GH_Document document)
        {
            if (base.NickName == "")
            {
                Random random = new Random();
                int randVal = random.Next(1, 100);
                base.NickName = "PVar_" + randVal;

                // Makes sure there is no duplicate on the default value
                if (document.IsFilePathDefined)
                {
                    while (File.Exists(VarFilePath()))
                    {
                        randVal = random.Next(1, 100);
                        base.NickName = "PVar_" + randVal;
                    }
                }
            }

            document.FilePathChanged += Document_FilePathChanged;

            base.AddedToDocument(document);
        }
        public override void RemovedFromDocument(GH_Document document)
        {
            // If the document is open (enabled) and the user deleted this object. 
            // Otherwise, it means that the document is closing....
            if (document.Enabled)
            {
                try
                {
                    if (File.Exists(VarFilePath(document))) File.Delete(VarFilePath(document));
                }
                catch
                {
                }
                finally
                {
                    document.FilePathChanged -= Document_FilePathChanged;
                }
            }

            base.RemovedFromDocument(document);
        }

        private void Document_FilePathChanged(object sender, GH_DocFilePathEventArgs e)
        {
            try
            {
                DirectoryInfo oldDir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(e.OldFilePath), Path.GetFileNameWithoutExtension(e.OldFilePath)));
                DirectoryInfo newDir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(e.NewFilePath), Path.GetFileNameWithoutExtension(e.NewFilePath)));

                if (oldDir.Exists) oldDir.MoveTo(newDir.FullName);
            }
            catch
            {
            }
            finally
            {
                this.ClearData();
                this.CollectData();
            }
        }

        protected override void CollectVolatileData_FromSources()
        {
            ClearRuntimeMessages();

            GH_Document doc = OnPingDocument();
            if (!doc.IsFilePathDefined)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The grasshopper document must be saved for the relative paths to work.");
                return;
            }

            if (SourceCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Please add an input.");
                return;
            }

            if (Sources[0].VolatileData.IsEmpty || Sources[0].VolatileData.DataCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The input data is empty.");
                return;
            }


            List<string> fileLines = new List<string>();

            foreach (IGH_Param source in Sources)
            {
                foreach (IGH_Goo gh_Goo in source.VolatileData.AllData(true))
                {
                    if (gh_Goo is GH_Point point)
                    {
                        fileLines.Add($"{point.Value.ToString()}");
                    }
                    else
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The input contains elements that are not points.");
                        return;
                    }
                }
            }

            // Writes the file
            if (File.Exists(VarFilePath())) File.Delete(VarFilePath());
            File.WriteAllLines(VarFilePath(), fileLines);

            base.CollectVolatileData_FromSources();
        }

        protected override void CollectVolatileData_Custom()
        {
            ClearRuntimeMessages();

            GH_Document doc = OnPingDocument();
            if (!doc.IsFilePathDefined)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The grasshopper document must be saved for the relative paths to work.");
                return;
            }

            if (SourceCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Please add an input.");
                return;
            }
        }
    }

    public class GHFileOutputPointAttributes : GH_Attributes<GHFileOutputPointParameter>
    {
        public GHFileOutputPointAttributes(GHFileOutputPointParameter owner) : base(owner) { }

        protected override void Layout()
        {
            Bounds = new RectangleF(Pivot, new SizeF(100, 70));
        }

        public override bool HasInputGrip => true;
        public override bool HasOutputGrip => false;

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            // Render all the wires that connect the Owner to all its Sources.
            if (channel == GH_CanvasChannel.Wires)
            {
                RenderIncomingWires(canvas.Painter, Owner.Sources, Owner.WireDisplay);
                return;
            }

            // Render the parameter capsule and any additional text on top of it.
            if (channel == GH_CanvasChannel.Objects)
            {
                // Define the default palette.
                GH_Palette palette = GH_Palette.Normal;

                // Adjust palette based on the Owner's worst case messaging level.
                switch (Owner.RuntimeMessageLevel)
                {
                    case GH_RuntimeMessageLevel.Warning:
                        palette = GH_Palette.Warning;
                        break;

                    case GH_RuntimeMessageLevel.Error:
                        palette = GH_Palette.Error;
                        break;
                }

                // Create a new Capsule without text or icon.
                GH_Capsule capsule = GH_Capsule.CreateCapsule(Bounds, palette);

                // Render the capsule using the current Selection, Locked and Hidden states.
                // Integer parameters are always hidden since they cannot be drawn in the viewport.
                capsule.AddInputGrip((Bounds.Bottom - Bounds.Top) / 2 + Bounds.Top);
                capsule.Render(graphics, Selected, Owner.Locked, true);

                // Always dispose of a GH_Capsule when you're done with it.
                capsule.Dispose();
                capsule = null;

                graphics.DrawImage(Owner.PublicIcon, Bounds.Left + Bounds.Width / 2 - Owner.PublicIcon.Width / 2f, Bounds.Top + 5);

                // Now it's time to draw the text on top of the capsule.
                // First we'll draw the Owner NickName using a standard font and a black brush.
                // We'll also align the NickName in the center of the Bounds.
                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;

                // Our entire capsule is 60 pixels high, and we'll draw 
                // three lines of text, each 20 pixels high.
                RectangleF textRectangle = Bounds;
                textRectangle.Height = 20;
                textRectangle.Y += 24 + 5;

                // Draw the NickName in a Standard Grasshopper font.
                graphics.DrawString(Owner.NickName, GH_FontServer.Standard, Brushes.Black, textRectangle, format);

                // Now we need to draw the median and mean information.
                // Adjust the formatting and the layout rectangle.
                format.Alignment = StringAlignment.Near;
                textRectangle.Inflate(-5, 0);

                textRectangle.Y += 20;
                graphics.DrawString($"Count: {Owner.DisplayString}", GH_FontServer.StandardItalic, Brushes.Black, textRectangle, format);

                // Always dispose of any GDI+ object that implement IDisposable.
                format.Dispose();
            }
        }
    }
}
