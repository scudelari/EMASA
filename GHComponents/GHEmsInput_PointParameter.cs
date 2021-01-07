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
using GH_IO.Types;
using GHComponents.Properties;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Point = Rhino.Geometry.Point;

namespace GHComponents
{
    public class GHEmsInput_PointParameter : GH_Param<GH_Point>, GHEMSParameterInterface
    {
        public override Guid ComponentGuid => new Guid("cecb9ba1-4ed6-4cfd-8aaa-a0c8f1192d0d");
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => Resources.GH_Icon_InputPoint;
        public Image PublicIcon => Icon;

        public GHEmsInput_PointParameter() :
            base("Input Point", "", "An input of a point that can be changed programatically through COM.", "Emasa", "Input", GH_ParamAccess.item)
        {
            // Sets the VariableName to a Random value
            Random rand = new Random();
            VariableName = $"Input Point {rand.Next(0, 10000):d5}";

            // Sets the default values
            LowerBound = Point3d.Origin;
            CurrentValue = new Point3d(5d, 5d, 5d);
            UpperBound = new Point3d(10d,10d,10d);
        }

        // Variables
        private string _variableName;
        public string VariableName
        {
            get => _variableName;
            set
            {
                _variableName = value;
                if (!string.IsNullOrWhiteSpace(_variableName)) NickName = _variableName;
            }
        }

        private Point3d _currentValue;
        public Point3d CurrentValue
        {
            get => _currentValue;
            set => _currentValue = value;
        }
        public string[] CurrentValueStrings
        {
            get
            {
                if (m_data.IsEmpty) return new[] { "No Data", "", "" };

                Point3d p = CurrentValue;
                return new[] { $"{p.X:g3}", $"{p.Y:g3}", $"{p.Z:g3}" };
            }
        }

        private Point3d _lowerBound;
        public Point3d LowerBound
        {
            get => _lowerBound;
            set => _lowerBound = value;
        }
        private Point3d _upperBound;
        public Point3d UpperBound
        {
            get => _upperBound;
            set => _upperBound = value;
        }
        public string[] LowerBoundStrings => new[] { $"{LowerBound.X:g3}", $"{LowerBound.Y:g3}", $"{LowerBound.Z:g3}" };
        public string[] UpperBoundStrings => new[] { $"{UpperBound.X:g3}", $"{UpperBound.Y:g3}", $"{UpperBound.Z:g3}" };

        // Serialization of Persistent Data
        public override bool Write(GH_IWriter writer)
        {
            writer.SetString("VariableName", VariableName);
            
            writer.SetDoubleArray("CurrentValue", new [] { CurrentValue.X, CurrentValue.Y, CurrentValue.Z });
            writer.SetDoubleArray("LowerBound", new[] { LowerBound.X, LowerBound.Y, LowerBound.Z });
            writer.SetDoubleArray("UpperBound", new[] { UpperBound.X, UpperBound.Y, UpperBound.Z });

            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            VariableName = reader.GetString("VariableName");
            
            double[] currentValueArray = reader.GetDoubleArray("CurrentValue");
            CurrentValue = new Point3d(currentValueArray[0], currentValueArray[1], currentValueArray[2]);

            double[] lowerBoundArray = reader.GetDoubleArray("LowerBound");
            LowerBound = new Point3d(lowerBoundArray[0], lowerBoundArray[1], lowerBoundArray[2]);

            double[] upperBoundArray = reader.GetDoubleArray("UpperBound");
            UpperBound = new Point3d(upperBoundArray[0], upperBoundArray[1], upperBoundArray[2]);

            return base.Read(reader);
        }

        private ToolStripTextBox _varName_ToolStripTextBox = null;
        private ToolStripTextBox _currentValue_ToolStripTextBox = null;
        private ToolStripTextBox _lowerBound_ToolStripTextBox = null;
        private ToolStripTextBox _upperBound_ToolStripTextBox = null;
        public override bool AppendMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, this.Name);
            Menu_AppendSeparator(menu);

            ToolStripMenuItem varNameTextItem = Menu_AppendItem(menu, "Variable Name:");
            varNameTextItem.ForeColor = GHEMSStaticMethods.VarNameColor;

            _varName_ToolStripTextBox = Menu_AppendTextItem(menu, $"{VariableName}", keydown: null, textchanged: null, lockOnFocus: true);
            // Removes the buttons that are automatically added...
            menu.Items.RemoveAt(menu.Items.Count - 1);
            menu.Items.RemoveAt(menu.Items.Count - 1);
            
            Menu_AppendSeparator(menu);

            ToolStripMenuItem currentValueTextItem = Menu_AppendItem(menu, "Current Value:");
            _currentValue_ToolStripTextBox = Menu_AppendTextItem(menu, $"{CurrentValue}", keydown: null, textchanged: null, lockOnFocus: true);
            // Removes the buttons that are automatically added...
            menu.Items.RemoveAt(menu.Items.Count - 1);
            menu.Items.RemoveAt(menu.Items.Count - 1);

            Menu_AppendSeparator(menu);

            ToolStripMenuItem lowerBoundTextItem = Menu_AppendItem(menu, "Lower Bounds:");
            _lowerBound_ToolStripTextBox = Menu_AppendTextItem(menu, $"{LowerBound}", keydown: null, textchanged: null, lockOnFocus: true);
            // Removes the buttons that are automatically added...
            menu.Items.RemoveAt(menu.Items.Count - 1);
            menu.Items.RemoveAt(menu.Items.Count - 1);

            ToolStripMenuItem upperBoundTextItem = Menu_AppendItem(menu, "Upper Bounds:");
            _upperBound_ToolStripTextBox = Menu_AppendTextItem(menu, $"{UpperBound}", keydown: null, textchanged: null, lockOnFocus: true);
            // Removes the buttons that are automatically added...
            menu.Items.RemoveAt(menu.Items.Count - 1);
            menu.Items.RemoveAt(menu.Items.Count - 1);

            // Commits changes to the files
            menu.Closed += Menu_Closed;

            return true;
        }

        private void Menu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            bool mustUpdate = false;
            Point3d inputValue;
            Point3d inputLowerBound;
            Point3d inputUpperBound;

            try
            {
                // Valid data in the name textbox?
                if (!string.IsNullOrWhiteSpace(_varName_ToolStripTextBox.Text))
                {
                    if (VariableName != _varName_ToolStripTextBox.Text)
                    {
                        VariableName = _varName_ToolStripTextBox.Text;
                        mustUpdate = true;
                    }
                }

                // Valid data in the value textboxes?
                if (Point3d.TryParse(_currentValue_ToolStripTextBox.Text, out inputValue) &&
                    Point3d.TryParse(_lowerBound_ToolStripTextBox.Text, out inputLowerBound) &&
                    Point3d.TryParse(_upperBound_ToolStripTextBox.Text, out inputUpperBound))
                {
                    // Are they consistent?
                    if (inputLowerBound.X < inputUpperBound.X &&
                        inputLowerBound.Y < inputUpperBound.Y &&
                        inputLowerBound.Z < inputUpperBound.Z &&
                        inputValue.X <= inputUpperBound.X &&
                        inputValue.Y <= inputUpperBound.Y &&
                        inputValue.Z <= inputUpperBound.Z &&
                        inputValue.X >= inputLowerBound.X &&
                        inputValue.Y >= inputLowerBound.Y &&
                        inputValue.Z >= inputLowerBound.Z)
                    {
                        CurrentValue = inputValue;
                        LowerBound = inputLowerBound;
                        UpperBound = inputUpperBound;
                        mustUpdate = true;
                    }
                }
            }
            finally
            {
                // Clears the menu variables
                _varName_ToolStripTextBox = null;
                _currentValue_ToolStripTextBox = null;
                _lowerBound_ToolStripTextBox = null;
                _upperBound_ToolStripTextBox = null;

                // Updates if required
                if (mustUpdate) this.CollectData();
            }
        }

        protected override void CollectVolatileData_Custom()
        {
            ClearRuntimeMessages();

            // Is variable unique?
            if (!UniqueNameInDocument)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Variable name is not unique in the document.");
                return;
            }

            try
            {
                // Reads the point value and transforms to GH data
                GH_Point val = new GH_Point(CurrentValue);
                m_data.Append(val);
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"There was an error while acquiring the data.{Environment.NewLine}{e.Message}");
                return;
            }
        }
        
        // Helper function to see if this is a unique named variable
        private bool UniqueNameInDocument
        {
            get
            {
                GH_Document doc = OnPingDocument();
                return doc.Objects.Count(a => a is GHEmsInput_PointParameter p && p.VariableName == this.VariableName) == 1;
            }
        }

        public override void CreateAttributes()
        {
            m_attributes = new GHFileInputPointAttributes(this);
        }
    }

    public class GHFileInputPointAttributes : GH_Attributes<GHEmsInput_PointParameter>
    {
        public GHFileInputPointAttributes(GHEmsInput_PointParameter owner) : base(owner) { }

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

                string[] valStrings = Owner.CurrentValueStrings;

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

                string[] rangeMinStrings = Owner.LowerBoundStrings;
                string[] rangeMaxStrings = Owner.UpperBoundStrings;

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
