using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace GHComponents
{
    public class GHEmsOutput_PointParameter : GH_Param<GH_Number>, GHEMSParameterInterface
    {
        public override Guid ComponentGuid => new Guid("0c0ba380-fb37-4ff1-86ef-cf82514dbaf2");
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override Bitmap Icon => Properties.Resources.GH_Icon_OutputPoints;
        public Image PublicIcon => Icon;

        public GHEmsOutput_PointParameter() :
            base("Output Points", "", "Receives a list of points and makes them available through COM.", "Emasa", "Output", GH_ParamAccess.tree)
        {
            // Sets the VariableName to a Random value
            Random rand = new Random();
            VariableName = $"Output Points {rand.Next(0, 10000):d5}";
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

        private List<Point3d> _points;
        public List<Point3d> Points
        {
            get => _points;
            set
            {
                _points = value;

                m_data.ClearData();
                if (_points == null || _points.Count == 0)
                {
                    ValCount = "No Data";
                    m_data.Append(new GH_Number(0d));
                }
                else
                {
                    ValCount = $"{_points.Count}";
                    m_data.Append(new GH_Number(_points.Count));
                }
            }
        }

        private string _valCount;
        public string ValCount
        {
            get => _valCount;
            set => _valCount = value;
        }


        // Serialization of Persistent Data
        public override bool Write(GH_IWriter writer)
        {
            writer.SetString("VariableName", VariableName);
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            VariableName = reader.GetString("VariableName");
            return base.Read(reader);
        }
        
        private ToolStripTextBox _varName_ToolStripTextBox = null;
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

            menu.Closed += Menu_Closed;

            return true;
        }
        private void Menu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            try
            {
                // Valid data in all text boxes?
                if (string.IsNullOrWhiteSpace(_varName_ToolStripTextBox.Text)) return;

                if (VariableName != _varName_ToolStripTextBox.Text)
                {
                    VariableName = _varName_ToolStripTextBox.Text;
                    this.CollectData();
                }
            }
            finally
            {
                _varName_ToolStripTextBox = null;
            }
        }

        protected override void CollectVolatileData_FromSources()
        {
            Points = null;
            ClearRuntimeMessages();

            // Is variable unique?
            if (!UniqueNameInDocument)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Variable name is not unique in the document.");
                return;
            }

            // No input
            if (SourceCount == 0 || Sources.Count == 0 || Sources[0].VolatileData.IsEmpty || Sources[0].VolatileData.DataCount == 0)
            {
                CollectVolatileData_Custom();
                return;
            }

            try
            {
                List<Point3d> values = new List<Point3d>();

                foreach (IGH_Param source in Sources)
                {
                    foreach (IGH_Goo gh_Goo in source.VolatileData.AllData(true))
                    {
                        if (gh_Goo is GH_Point gh_Point)
                        {
                            values.Add(gh_Point.Value);
                        }
                        else
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The input contains elements that are not points.");
                            return;
                        }
                    }
                }

                Points = values;
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"There was an error while getting the data from the input.{Environment.NewLine}{e.Message}");
                return;
            }
        }
        protected override void CollectVolatileData_Custom()
        {
            Points = null;
            ClearRuntimeMessages();

            // Is variable unique?
            if (!UniqueNameInDocument)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Variable name is not unique in the document.");
                return;
            }

            // Also sets the error level
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"There is no input data.");
        }

        // Helper function to see if this is a unique named variable
        private bool UniqueNameInDocument
        {
            get
            {
                GH_Document doc = OnPingDocument();
                return doc.Objects.Count(a => a is GHEmsOutput_PointParameter p && p.VariableName == this.VariableName) == 1;
            }
        }

        public override void CreateAttributes()
        {
            m_attributes = new GHFileOutputPointAttributes(this);
        }
    }

    public class GHFileOutputPointAttributes : GH_Attributes<GHEmsOutput_PointParameter>
    {
        public GHFileOutputPointAttributes(GHEmsOutput_PointParameter owner) : base(owner) { }

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
