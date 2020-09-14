using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace GHComponents
{
    public class GHFileOutputDoubleParameter : GH_Param<GH_Number>
    {
        public override Guid ComponentGuid => new Guid("3bf29763-c9fb-44a3-8970-c94971ef80f3");
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => Properties.Resources.GH_Icon_OutputDouble;
        public Image PublicIcon => Icon;

        private GhOptFileManager _fManager = null;
        private GhOptVariableType _varType = GhOptVariableType.DoubleList;
        private GhOptVariableDirection _varDir = GhOptVariableDirection.Output;

        public GHFileOutputDoubleParameter() :
            base("File Output Double", "", "Receives a List of Doubles and Writes its Value", "Emasa", "Output", GH_ParamAccess.tree)
        {
        }

        public override void AddedToDocument(GH_Document document)
        {
            // Denies the addition of new items to GHDocs that don't have a defined name.
            if (string.IsNullOrWhiteSpace(NickName) && !document.IsFilePathDefined)
            {
                MessageBox.Show($"{Name} may only be added to documents that have a defined file path.", "Save the GH document first", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                document.RemoveObject(this, true);
                return;
            }

            // Creates the file manager, making a new name in case this does not exist
            _fManager = new GhOptFileManager(this, NickName, _varType, _varDir);
            if (string.IsNullOrWhiteSpace(NickName)) NickName = _fManager.VarName;

            // Listens to the file change events
            document.FilePathChanged += _fManager.Document_FilePathChanged;

            base.AddedToDocument(document);
        }
        public override void RemovedFromDocument(GH_Document document)
        {
            // If the document has not been saved, ignore.
            if (!document.IsFilePathDefined) return;

            // If the document is open (enabled) and the user deleted this object. 
            // Otherwise, it means that the document is closing....
            if (document.Enabled)
            {
                try
                {
                    _fManager.DeleteFiles(document);
                }
                catch
                {
                }
                finally
                {
                    document.FilePathChanged -= _fManager.Document_FilePathChanged;
                }
            }

            base.RemovedFromDocument(document);
        }
        
        public override void CreateAttributes()
        {
            m_attributes = new GHFileOutputDoubleAttributes(this);
        }

        private ToolStripTextBox _varName_ToolStripTextBox = null;
        public override bool AppendMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, this.Name);
            Menu_AppendSeparator(menu);
            ToolStripMenuItem varNameTextItem = Menu_AppendItem(menu, "Variable Name:");
            varNameTextItem.ForeColor = Color.DarkOrange;
            _varName_ToolStripTextBox = Menu_AppendTextItem(menu, $"{NickName}",
                keydown: (inSender, inArgs) => { },
                textchanged: ToolStripMenuTextChanged_CheckValidFileName,
                true);
            // Removes the buttons that are automatically added...
            menu.Items.RemoveAt(menu.Items.Count - 1);
            menu.Items.RemoveAt(menu.Items.Count - 1);

            menu.Closed += Menu_Closed;

            return true;
        }
        private void Menu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            try
            {
                NickName = _varName_ToolStripTextBox.Text;
            }
            catch
            {
                MessageBox.Show($"Could not change the variable name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _varName_ToolStripTextBox = null;
            }

            CollectVolatileData_Custom();
            CollectData();
        }
        private void ToolStripMenuTextChanged_CheckValidFileName(GH_MenuTextBox inSender, string inNewtext)
        {
            // Changed to the current NickName
            if (inNewtext == NickName) return;

            if (inNewtext.Any(a => Path.GetInvalidFileNameChars().Contains(a)))
            {
                inSender.Text = inSender.OriginalText; // Cancels
            }

            // Checks if the altered filename exists
            try
            {
                if (File.Exists(_fManager.VarFilePath(inNewtext))) inSender.Text = inSender.OriginalText; // Cancels
            }
            catch (Exception e)
            {
                // Also cancels as a safeguard
                inSender.Text = inSender.OriginalText; // Cancels
            }
        }

        public string ValCount
        {
            get
            {
                if (SourceCount == 0 || Sources[0].VolatileData.IsEmpty || Sources[0].VolatileData.DataCount == 0) return "No Data";

                int countInputs = 0;
                foreach (IGH_Param source in Sources)
                {
                    countInputs += source.VolatileData.DataCount;
                }

                return $"{countInputs}";
            }
        }
        public override string NickName
        {
            get => base.NickName;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) return; // Ignores the change
                if (base.NickName == value) return; // Did not change

                if (value.Any(a => Path.GetInvalidFileNameChars().Contains(a))) return; // Ignores the change

                // Changes the name in the file manager
                _fManager.UpdateVarName(value);

                // Sets the value in the controller
                base.NickName = value;
                this.CollectData();
            }
        }

        protected override void CollectVolatileData_FromSources()
        {
            base.CollectVolatileData_FromSources();

            ClearRuntimeMessages();

            GH_Document doc = OnPingDocument();
            if (!doc.IsFilePathDefined)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The grasshopper document must be saved for the relative paths to work.");
                return;
            }

            if (SourceCount == 0)
            {
                try
                {
                    _fManager.WriteEmptyFile();
                }
                catch
                {
                }

                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Please add an input.");
                return;
            }

            if (Sources[0].VolatileData.IsEmpty || Sources[0].VolatileData.DataCount == 0)
            {
                try
                {
                    _fManager.WriteEmptyFile();
                }
                catch
                {
                }

                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The input data is empty.");
                return;
            }

            try
            {
                List<double> values = new List<double>();

                foreach (IGH_Param source in Sources)
                {
                    foreach (IGH_Goo gh_Goo in source.VolatileData.AllData(true))
                    {
                        if (gh_Goo is GH_Number gh_Number)
                        {
                            values.Add(gh_Number.Value);
                        }
                        else
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The input contains elements that are not doubles.");
                            return;
                        }
                    }
                }

                _fManager.WriteDoubleValues(values);
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"There was an error while saving the data.{Environment.NewLine}{e.Message}");
                return;
            }
        }
        protected override void CollectVolatileData_Custom()
        {
            ClearRuntimeMessages();

            GH_Document doc = OnPingDocument();
            if (!doc.IsFilePathDefined)
            {
                try
                {
                    _fManager.WriteEmptyFile();
                }
                catch
                {
                }

                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The grasshopper document must be saved for the relative paths to work.");
                return;
            }

            if (SourceCount == 0)
            {
                try
                {
                    _fManager.WriteEmptyFile();
                }
                catch
                {
                }

                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Please add an input.");
                return;
            }
        }
    }

    public class GHFileOutputDoubleAttributes : GH_Attributes<GHFileOutputDoubleParameter>
    {
        public GHFileOutputDoubleAttributes(GHFileOutputDoubleParameter owner) : base(owner) { }

        private float NominalWidth = 185;
        private float NominalHeight = 53;

        protected override void Layout()
        {
            Bounds = new RectangleF(Pivot, new SizeF(NominalWidth, NominalHeight));
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

                // Adds an input grip for the capsule
                capsule.AddInputGrip((Bounds.Bottom - Bounds.Top) / 2 + Bounds.Top);
                // Render the capsule using the current Selection, Locked and Hidden states.
                capsule.Render(graphics, Selected, Owner.Locked, true);

                // Always dispose of a GH_Capsule when you're done with it.
                capsule.Dispose();
                capsule = null;

                // Draws the icon at the middle =>
                // graphics.DrawImage(Owner.PublicIcon, Bounds.Left + Bounds.Width / 2 - Owner.PublicIcon.Width / 2f, Bounds.Top + 5);
                // Draws the icon closer to the left
                graphics.DrawImage(Owner.PublicIcon, Bounds.Left + 5, Bounds.Top + 5);

                // Setting up the font configs
                StringFormat leftFormat = new StringFormat();
                leftFormat.Alignment = StringAlignment.Near;
                leftFormat.LineAlignment = StringAlignment.Center;
                leftFormat.Trimming = StringTrimming.EllipsisCharacter;

                StringFormat centerFormat = new StringFormat();
                centerFormat.Alignment = StringAlignment.Center;
                centerFormat.LineAlignment = StringAlignment.Center;
                centerFormat.Trimming = StringTrimming.Character;
                centerFormat.FormatFlags = StringFormatFlags.NoWrap;

                // Setting up the Pen Configs
                Pen underline = new Pen(Brushes.DarkSlateGray, 1f);
                Brush bg = new SolidBrush(Color.White);


                // Drawing the NickName
                RectangleF nameRect = Bounds;
                nameRect.Height = 20; // LineList height is of 20
                nameRect.Offset(23 + 5 + 5, 5);
                nameRect.Width += -(23 + 5 + 5 + 5);
                graphics.DrawString(Owner.NickName, GH_FontServer.Large, Brushes.Black, nameRect, leftFormat);
                graphics.DrawLine(underline, new PointF(nameRect.X, nameRect.Y + 20), new PointF(nameRect.X + nameRect.Width, nameRect.Y + 20));


                // Drawing the value name
                RectangleF valueNameRect = Bounds;
                valueNameRect.Height = 20;
                valueNameRect.Y += 30;
                valueNameRect.Offset(23 + 5 + 5, 0);
                valueNameRect.Width += -(23 + 5 + 5 + 5);
                graphics.DrawString($"► {Owner.ValCount}", GH_FontServer.Console, Brushes.Black, valueNameRect, leftFormat);

                //// Drawing the value
                //RectangleF valueRect = Bounds;
                //valueRect.Height = 20;
                //valueRect.Y += 30;
                //valueRect.X += 23 + 5 + 5 + 50;
                //valueRect.Width += -(23 + 5 + 5 + 50 +5);
                //graphics.DrawString(Owner.ValCount, GH_FontServer.Console, Brushes.Black, valueRect, leftFormat);


                // Always dispose of any GDI+ object that implement IDisposable.
                centerFormat.Dispose();
                centerFormat = null;
                leftFormat.Dispose();
                leftFormat = null;
                underline.Dispose();
                underline = null;
                bg.Dispose();
                bg = null;
            }
        }

        private Rectangle ToIntRectangle(RectangleF inRectangleF)
        {
            return new Rectangle((int)inRectangleF.X, (int)inRectangleF.Y, (int)inRectangleF.Width, (int)inRectangleF.Height);
        }
    }
}
