using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GH_IO.Serialization;
using GHComponents.Properties;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Point = Rhino.Geometry.Point;

namespace GHComponents
{
    public class GHFileInputPointParameter : GH_Param<GH_Point>
    {
        public override Guid ComponentGuid => new Guid("cecb9ba1-4ed6-4cfd-8aaa-a0c8f1192d0d");
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => Resources.GH_Icon_InputPoint;
        public Image PublicIcon => Icon;

        private GhOptFileManager _fManager = null;
        private GhOptVariableType _varType = GhOptVariableType.Point;
        private GhOptVariableDirection _varDir = GhOptVariableDirection.Input;

        public GHFileInputPointParameter() :
            base("File Input Point", "", "Reads a Point from a Text File and Outputs its value.", "Emasa", "Input", GH_ParamAccess.item)
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
            m_attributes = new GHFileInputPointAttributes(this);
        }

        private ToolStripTextBox _varName_ToolStripTextBox = null;
        private ToolStripTextBox _minRange_ToolStripTextBox = null;
        private ToolStripTextBox _maxRange_ToolStripTextBox = null;
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

            Menu_AppendSeparator(menu);

            Menu_AppendItem(menu, "Range Minimum:");
            _minRange_ToolStripTextBox = Menu_AppendTextItem(menu, $"{RangeMin}",
                keydown: (inSender, inArgs) => { },
                textchanged: ToolStripMenuTextChanged_CheckValidPoint,
                true);
            // Removes the buttons that are automatically added...
            menu.Items.RemoveAt(menu.Items.Count - 1);
            menu.Items.RemoveAt(menu.Items.Count - 1);

            Menu_AppendItem(menu, "Range Maximum:");
            _maxRange_ToolStripTextBox = Menu_AppendTextItem(menu, $"{RangeMax}",
                keydown: (inSender, inArgs) => { },
                textchanged: ToolStripMenuTextChanged_CheckValidPoint,
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

            try
            {
                Point3d minVal = Point3d.Unset;
                if (!Point3d.TryParse(_minRange_ToolStripTextBox.Text, out minVal)) throw new Exception();

                Point3d maxVal = Point3d.Unset;
                if (!Point3d.TryParse(_maxRange_ToolStripTextBox.Text, out maxVal)) throw new Exception();

                // saves to the file
                _fManager.WritePointRange(minVal, maxVal);
            }
            catch
            {
                MessageBox.Show($"Could not change the range.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _minRange_ToolStripTextBox = null;
                _maxRange_ToolStripTextBox = null;
            }
            CollectVolatileData_Custom();
            CollectData();
        }
        private void ToolStripMenuTextChanged_CheckValidPoint(GH_MenuTextBox inSender, string inNewtext)
        {
            if (!Point3d.TryParse(inNewtext, out Point3d _)) inSender.Text = inSender.OriginalText;
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
        
        public Point3d RangeMin
        {
            get;
            set;
        }
        public Point3d RangeMax
        {
            get;
            set;
        }
        public string[] RangeMinStrings => new[] { $"{RangeMin.X:g3}", $"{RangeMin.Y:g3}", $"{RangeMin.Z:g3}" };
        public string[] RangeMaxStrings => new[] { $"{RangeMax.X:g3}", $"{RangeMax.Y:g3}", $"{RangeMax.Z:g3}" };

        public string[] ValueStrings
        {
            get
            {
                if (m_data.IsEmpty) return new[] {"No Data","No Data", "No Data"};

                Point3d p = m_data.get_FirstItem(true).Value;
                return new[] { $"{p.X:g3}", $"{p.Y:g3}", $"{p.Z:g3}" };
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

        protected override void CollectVolatileData_Custom()
        {
            ClearRuntimeMessages();

            GH_Document doc = OnPingDocument();
            if (!doc.IsFilePathDefined)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The grasshopper document must be saved for the relative paths to work.");
                return;
            }

            try
            {
                // Clears the current data
                m_data.Clear();

                // Reads the double value and transforms to GH data
                GH_Point val = new GH_Point(_fManager.ReadPointValue());
                m_data.Append(val);

                // Reads the Range
                Point3d[] range = _fManager.ReadPointRange();
                RangeMin = range[0];
                RangeMax = range[1];
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"There was an error while acquiring the data.{Environment.NewLine}{e.Message}");
                return;
            }
        }
    }

    public class GHFileInputPointAttributes : GH_Attributes<GHFileInputPointParameter>
    {
        public GHFileInputPointAttributes(GHFileInputPointParameter owner) : base(owner) { }

        private float NominalWidth = 185;
        private float NominalHeight = 154;

        protected override void Layout()
        {
            Bounds = new RectangleF(Pivot, new SizeF(NominalWidth, NominalHeight));
        }

        public override bool HasInputGrip => false;
        public override bool HasOutputGrip => true;

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

                // Adds an output grip fo the capsule
                capsule.AddOutputGrip((Bounds.Bottom - Bounds.Top) / 2 + Bounds.Top);
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

                string[] valStrings = Owner.ValueStrings;

                // Drawing the value coordinates
                RectangleF valueCoordRect = Bounds;
                valueCoordRect.Y += 30;
                valueCoordRect.Height = 20;
                valueCoordRect.Offset(23 + 5 + 5, 0);
                valueCoordRect.Width = 25;
                graphics.DrawString("X:", GH_FontServer.Console, Brushes.Black, valueCoordRect, leftFormat);
                valueCoordRect.Y += 20;
                graphics.DrawString("Y:", GH_FontServer.Console, Brushes.Black, valueCoordRect, leftFormat);
                valueCoordRect.Y += 20;
                graphics.DrawString("Z:", GH_FontServer.Console, Brushes.Black, valueCoordRect, leftFormat);

                // Drawing the value values
                RectangleF valueRect = Bounds;
                valueRect.Y += 30;
                valueRect.Height = 20;
                valueRect.Offset(23 + 5 + 5 + 25, 0);
                valueRect.Width += -(23 + 5 + 5 + 25);
                graphics.DrawString(valStrings[0], GH_FontServer.Console, Brushes.Black, valueRect, leftFormat);
                valueRect.Y += 20;
                graphics.DrawString(valStrings[1], GH_FontServer.Console, Brushes.Black, valueRect, leftFormat);
                valueRect.Y += 20;
                graphics.DrawString(valStrings[2], GH_FontServer.Console, Brushes.Black, valueRect, leftFormat);

                string[] rangeMinStrings = Owner.RangeMinStrings;
                string[] rangeMaxStrings = Owner.RangeMaxStrings;

                // Drawing the range - X
                RectangleF rangeRectX = Bounds;
                rangeRectX.Height = 15;
                rangeRectX.Y = valueCoordRect.Y + 25;

                rangeRectX.Width = 20;
                //graphics.DrawString("├─", GH_FontServer.ConsoleSmall, Brushes.DarkSlateGray, rangeRectX, centerFormat);

                rangeRectX.X += rangeRectX.Width;
                rangeRectX.Width = 60;
                graphics.DrawRectangle(underline, ToIntRectangle(rangeRectX));
                graphics.FillRectangle(bg, ToIntRectangle(rangeRectX));
                graphics.DrawString(rangeMinStrings[0], GH_FontServer.ConsoleSmall, Brushes.Black, rangeRectX, centerFormat);

                rangeRectX.X += rangeRectX.Width;
                rangeRectX.Width = 25;
                //graphics.DrawString("───", GH_FontServer.ConsoleSmall, Brushes.DarkSlateGray, rangeRectX, centerFormat);

                rangeRectX.X += rangeRectX.Width;
                rangeRectX.Width = 60;
                graphics.DrawRectangle(underline, ToIntRectangle(rangeRectX));
                graphics.FillRectangle(bg, ToIntRectangle(rangeRectX));
                graphics.DrawString(rangeMaxStrings[0], GH_FontServer.ConsoleSmall, Brushes.Black, rangeRectX, centerFormat);

                rangeRectX.X += rangeRectX.Width;
                rangeRectX.Width = 20;
                //graphics.DrawString("─┤", GH_FontServer.ConsoleSmall, Brushes.DarkSlateGray, rangeRectX, centerFormat);


                // Drawing the range - Y
                RectangleF rangeRectY = Bounds;
                rangeRectY.Height = 15;
                rangeRectY.Y = rangeRectX.Y + 17;

                rangeRectY.Width = 20;
                graphics.DrawString("├─", GH_FontServer.ConsoleSmall, Brushes.DarkSlateGray, rangeRectY, centerFormat);

                rangeRectY.X += rangeRectY.Width;
                rangeRectY.Width = 60;
                graphics.DrawRectangle(underline, ToIntRectangle(rangeRectY));
                graphics.FillRectangle(bg, ToIntRectangle(rangeRectY));
                graphics.DrawString(rangeMinStrings[1], GH_FontServer.ConsoleSmall, Brushes.Black, rangeRectY, centerFormat);

                rangeRectY.X += rangeRectY.Width;
                rangeRectY.Width = 25;
                graphics.DrawString("───", GH_FontServer.ConsoleSmall, Brushes.DarkSlateGray, rangeRectY, centerFormat);

                rangeRectY.X += rangeRectY.Width;
                rangeRectY.Width = 60;
                graphics.DrawRectangle(underline, ToIntRectangle(rangeRectY));
                graphics.FillRectangle(bg, ToIntRectangle(rangeRectY));
                graphics.DrawString(rangeMaxStrings[1], GH_FontServer.ConsoleSmall, Brushes.Black, rangeRectY, centerFormat);

                rangeRectY.X += rangeRectY.Width;
                rangeRectY.Width = 20;
                graphics.DrawString("─┤", GH_FontServer.ConsoleSmall, Brushes.DarkSlateGray, rangeRectY, centerFormat);


                // Drawing the range - Z
                RectangleF rangeRectZ = Bounds;
                rangeRectZ.Height = 15;
                rangeRectZ.Y = rangeRectY.Y + 17;

                rangeRectZ.Width = 20;
                //graphics.DrawString("├─", GH_FontServer.ConsoleSmall, Brushes.DarkSlateGray, rangeRectZ, centerFormat);

                rangeRectZ.X += rangeRectZ.Width;
                rangeRectZ.Width = 60;
                graphics.DrawRectangle(underline, ToIntRectangle(rangeRectZ));
                graphics.FillRectangle(bg, ToIntRectangle(rangeRectZ));
                graphics.DrawString(rangeMinStrings[2], GH_FontServer.ConsoleSmall, Brushes.Black, rangeRectZ, centerFormat);

                rangeRectZ.X += rangeRectZ.Width;
                rangeRectZ.Width = 25;
                //graphics.DrawString("───", GH_FontServer.ConsoleSmall, Brushes.DarkSlateGray, rangeRectZ, centerFormat);

                rangeRectZ.X += rangeRectZ.Width;
                rangeRectZ.Width = 60;
                graphics.DrawRectangle(underline, ToIntRectangle(rangeRectZ));
                graphics.FillRectangle(bg, ToIntRectangle(rangeRectZ));
                graphics.DrawString(rangeMaxStrings[2], GH_FontServer.ConsoleSmall, Brushes.Black, rangeRectZ, centerFormat);

                rangeRectZ.X += rangeRectZ.Width;
                rangeRectZ.Width = 20;
                //graphics.DrawString("─┤", GH_FontServer.ConsoleSmall, Brushes.DarkSlateGray, rangeRectZ, centerFormat);


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
