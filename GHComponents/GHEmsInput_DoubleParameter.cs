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

namespace GHComponents
{
    public class GHEmsInput_DoubleParameter : GH_Param<GH_Number>, GHEMSParameterInterface
    {
        public override Guid ComponentGuid => new Guid("e292c5a9-9bf6-4382-82af-aa2899d539ef");
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => Properties.Resources.GH_Icon_InputDouble;
        public Image PublicIcon => Icon;

        public GHEmsInput_DoubleParameter() :
            base("Input Double", "", "An input of a double that can be changed programatically through COM.", "Emasa", "Input", GH_ParamAccess.item)
        {
            // Sets the VariableName to a Random value
            Random rand = new Random();
            VariableName = $"Input Double {rand.Next(0, 10000):d5}";

            // Sets the default value
            LowerBound = 0d;
            CurrentValue = 1d;
            UpperBound = 10d;
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

        private double _currentValue;
        public double CurrentValue
        {
            get => _currentValue;
            set => _currentValue = value;
        }
        public string CurrrentValueString => $"{CurrentValue:g3}";

        private double _lowerBound;
        public double LowerBound
        {
            get => _lowerBound;
            set => _lowerBound = value;
        }
        private double _upperBound;
        public double UpperBound
        {
            get => _upperBound;
            set => _upperBound = value;
        }
        public string LowerBoundString => $"{LowerBound:g3}";
        public string UpperBoundString => $"{UpperBound:g3}";

        // Serialization of Persistent Data
        public override bool Write(GH_IWriter writer)
        {
            writer.SetString("VariableName", VariableName);

            writer.SetDouble("CurrentValue", CurrentValue);
            writer.SetDouble("LowerBound", LowerBound);
            writer.SetDouble("UpperBound", UpperBound);

            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            VariableName = reader.GetString("VariableName");

            CurrentValue = reader.GetDouble("CurrentValue");
            LowerBound = reader.GetDouble("LowerBound");
            UpperBound = reader.GetDouble("UpperBound");
            
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
            double inputValue;
            double inputLowerBound;
            double inputUpperBound;

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
                if (double.TryParse(_currentValue_ToolStripTextBox.Text, out inputValue) &&
                    double.TryParse(_lowerBound_ToolStripTextBox.Text, out inputLowerBound) &&
                    double.TryParse(_upperBound_ToolStripTextBox.Text, out inputUpperBound))
                {
                    // Are they consistent?
                    if (inputLowerBound < inputUpperBound &&
                        inputValue <= inputUpperBound &&
                        inputValue >= inputLowerBound)
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
                GH_Number val = new GH_Number(CurrentValue);
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
                return doc.Objects.Count(a => a is GHEmsInput_DoubleParameter p && p.VariableName == this.VariableName) == 1;
            }
        }

        public override void CreateAttributes()
        {
            m_attributes = new GHFileInputDoubleAttributes(this);
        }
    }

    public class GHFileInputDoubleAttributes : GH_Attributes<GHEmsInput_DoubleParameter>
    {
        public GHFileInputDoubleAttributes(GHEmsInput_DoubleParameter owner) : base(owner) { }

        private float NominalWidth = 185;
        private float NominalHeight = 75;

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

                // Drawing the value
                RectangleF valueRect = Bounds;
                valueRect.Height = 20;
                valueRect.Y += 30;
                valueRect.Offset(23 + 5 + 5, 0);
                valueRect.Width += -(23 + 5 + 5 + 5);
                graphics.DrawString(Owner.CurrrentValueString, GH_FontServer.Console, Brushes.Black, valueRect, leftFormat);

                // Drawing the range
                RectangleF rangeRect = Bounds;
                rangeRect.Height = 15;
                rangeRect.Y = valueRect.Y + 20;

                rangeRect.Width = 20;
                graphics.DrawString("├─", GH_FontServer.ConsoleSmall, Brushes.DarkSlateGray, rangeRect, centerFormat);
                
                rangeRect.X += rangeRect.Width;
                rangeRect.Width = 60;
                graphics.DrawRectangle(underline, ToIntRectangle(rangeRect));
                graphics.FillRectangle(bg, ToIntRectangle(rangeRect));
                graphics.DrawString(Owner.LowerBoundString, GH_FontServer.ConsoleSmall, Brushes.Black, rangeRect, centerFormat);

                rangeRect.X += rangeRect.Width;
                rangeRect.Width = 25;
                graphics.DrawString("───", GH_FontServer.ConsoleSmall, Brushes.DarkSlateGray, rangeRect, centerFormat);

                rangeRect.X += rangeRect.Width;
                rangeRect.Width = 60;
                graphics.DrawRectangle(underline, ToIntRectangle(rangeRect));
                graphics.FillRectangle(bg, ToIntRectangle(rangeRect));
                graphics.DrawString(Owner.UpperBoundString, GH_FontServer.ConsoleSmall, Brushes.Black, rangeRect, centerFormat);

                rangeRect.X += rangeRect.Width;
                rangeRect.Width = 20;
                graphics.DrawString("─┤", GH_FontServer.ConsoleSmall, Brushes.DarkSlateGray, rangeRect, centerFormat);

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
