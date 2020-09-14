using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GH_IO.Serialization;
using GHComponents.Properties;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace GHComponents
{
    //public class GHFileInputIntegerParameter : GH_Param<GH_Integer>
    //{
    //    public GHFileInputIntegerParameter() :
    //        base("File Input Integer", "", "Reads an Integer from a Text File and Outputs its value.", "Emasa", "Input", GH_ParamAccess.item)
    //    {
    //    }

    //    public string SubDir => this.SubCategory;
    //    public override Guid ComponentGuid => new Guid("426ae71e-82f2-4821-8829-723b10a2ead1");
    //    public override GH_Exposure Exposure => GH_Exposure.primary;
    //    protected override System.Drawing.Bitmap Icon => Properties.Resources.GH_Icon_InputInteger;

    //    // Helper functions
    //    public string VarFilePath(GH_Document inDocument = null, string inVarName = null)
    //    {
    //        if (inDocument == null) inDocument = OnPingDocument();
    //        if (inVarName == null) inVarName = NickName;

    //        if (!inDocument.IsFilePathDefined) return null;
    //        if (string.IsNullOrWhiteSpace(inDocument.FilePath)) return null;

    //        try
    //        {
    //            // Gets the document
    //            string projectFolder = Path.GetDirectoryName(inDocument.FilePath);
    //            string ghFilename = Path.GetFileName(inDocument.FilePath);

    //            string targetDir = Path.Combine(projectFolder, ghFilename + "_data", SubDir);

    //            DirectoryInfo dirInfo = new DirectoryInfo(targetDir);
    //            if (!dirInfo.Exists) dirInfo.Create();

    //            return Path.Combine(targetDir, $"{inVarName}.Integer");
    //        }
    //        catch (Exception)
    //        {
    //            return null;
    //        }
    //    }

    //    public Image PublicIcon => Icon;

    //    public override void CreateAttributes()
    //    {
    //        m_attributes = new GHFileInputIntegerAttributes(this);
    //    }

    //    public override bool AppendMenuItems(ToolStripDropDown menu)
    //    {
    //        Menu_AppendItem(menu, this.Name);
    //        Menu_AppendSeparator(menu);
    //        Menu_AppendItem(menu, "Variable Name:");
    //        Menu_AppendObjectNameEx(menu);
            
    //        return true;
    //    }

    //    public string ValCount
    //    {
    //        get
    //        {
    //            if (m_data.IsEmpty) return "No Data";
    //            else return m_data.get_FirstItem(true).ToString();
    //        }
    //    }

    //    public override string NickName
    //    {
    //        get => base.NickName;
    //        set
    //        {
    //            if (string.IsNullOrWhiteSpace(value)) return;
    //            if (base.NickName == value) return;

    //            try
    //            {
    //                string oldFile = VarFilePath(inVarName: NickName);
    //                string newFile = VarFilePath(inVarName: value);

    //                // If the old file exists, we must move it
    //                if (File.Exists(oldFile))
    //                {
    //                    // The new file already exists; the change of name is aborted
    //                    if (File.Exists(newFile)) return;

    //                    File.Copy(oldFile, newFile);
    //                    File.Delete(oldFile);
    //                }
    //            }
    //            catch
    //            {
    //            }

    //            // Sets the value
    //            base.NickName = value;

    //            this.CollectData();
    //        }
    //    }

    //    public override void AddedToDocument(GH_Document document)
    //    {
    //        if (base.NickName == "") 
    //        {
    //            Random random = new Random();
    //            int randVal = random.Next(1, 100);
    //            base.NickName = "IVar_" + randVal;

    //            // Makes sure there is no duplicate on the default value
    //            if (document.IsFilePathDefined)
    //            {
    //                while (File.Exists(VarFilePath()))
    //                {
    //                    randVal = random.Next(1, 100);
    //                    base.NickName = "IVar_" + randVal;
    //                }
    //            }
    //        }

    //        document.FilePathChanged += Document_FilePathChanged;

    //        base.AddedToDocument(document);
    //    }
    //    public override void RemovedFromDocument(GH_Document document)
    //    {
    //        // If the document is open (enabled) and the user deleted this object. 
    //        // Otherwise, it means that the document is closing....
    //        if (document.Enabled)
    //        {
    //            try
    //            {
    //                if (File.Exists(VarFilePath(document))) File.Delete(VarFilePath(document));
    //            }
    //            catch
    //            {
    //            }
    //            finally
    //            {
    //                document.FilePathChanged -= Document_FilePathChanged;
    //            }
    //        }

    //        base.RemovedFromDocument(document);
    //    }

    //    private void Document_FilePathChanged(object sender, GH_DocFilePathEventArgs e)
    //    {
    //        try
    //        {
    //            DirectoryInfo oldDir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(e.OldFilePath), Path.GetFileNameWithoutExtension(e.OldFilePath)));
    //            DirectoryInfo newDir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(e.NewFilePath), Path.GetFileNameWithoutExtension(e.NewFilePath)));

    //            if (oldDir.Exists) oldDir.MoveTo(newDir.FullName);
    //        }
    //        catch
    //        {
    //        }
    //        finally
    //        {
    //            this.ClearData();
    //            this.CollectData();
    //        }
    //    }

    //    protected override void CollectVolatileData_Custom()
    //    {
    //        ClearRuntimeMessages();

    //        GH_Document doc = OnPingDocument();
    //        if (!doc.IsFilePathDefined)
    //        {
    //            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The grasshopper document must be saved for the relative paths to work.");
    //            return;
    //        }

    //        try
    //        {
    //            if (!File.Exists(VarFilePath()))
    //            {
    //                using (StreamWriter sw = new StreamWriter( File.Create(VarFilePath())))
    //                {
    //                    sw.WriteLine(0);
    //                }
    //            }
    //        }
    //        catch (Exception)
    //        {
    //            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not get the Grasshopper file name. Please save the file.");
    //            return;
    //        }

    //        // Reads the double from the file
    //        string fileContents = string.Empty;
    //        try
    //        {
    //            fileContents = File.ReadAllText(VarFilePath());
    //            GH_Integer val = new GH_Integer(Convert.ToInt32(fileContents));
    //            m_data.Clear();
    //            m_data.Append(val);
    //        }
    //        catch (Exception ex)
    //        {
    //            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Could not convert the contents {fileContents} of the file to GH_Integer. {ex.Message} | {ex.StackTrace}");
    //            return;
    //        }
    //    }
    //}

    //public class GHFileInputIntegerAttributes : GH_Attributes<GHFileInputIntegerParameter>
    //{
    //    public GHFileInputIntegerAttributes(GHFileInputIntegerParameter owner) : base(owner) { }

    //    protected override void Layout()
    //    {
    //        Bounds = new RectangleF(Pivot, new SizeF(100, 70));
    //    }

    //    public override bool HasInputGrip => false;
    //    public override bool HasOutputGrip => true;

    //    protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
    //    {
    //        // Render all the wires that connect the Owner to all its Sources.
    //        if (channel == GH_CanvasChannel.Wires)
    //        {
    //            RenderIncomingWires(canvas.Painter, Owner.Sources, Owner.WireDisplay);
    //            return;
    //        }

    //        // Render the parameter capsule and any additional text on top of it.
    //        if (channel == GH_CanvasChannel.Objects)
    //        {
    //            // Define the default palette.
    //            GH_Palette palette = GH_Palette.Normal;

    //            // Adjust palette based on the Owner's worst case messaging level.
    //            switch (Owner.RuntimeMessageLevel)
    //            {
    //                case GH_RuntimeMessageLevel.Warning:
    //                    palette = GH_Palette.Warning;
    //                    break;

    //                case GH_RuntimeMessageLevel.Error:
    //                    palette = GH_Palette.Error;
    //                    break;
    //            }

    //            // Create a new Capsule without text or icon.
    //            GH_Capsule capsule = GH_Capsule.CreateCapsule(Bounds, palette);

    //            // Render the capsule using the current Selection, Locked and Hidden states.
    //            // Integer parameters are always hidden since they cannot be drawn in the viewport.
    //            capsule.AddOutputGrip((Bounds.Bottom - Bounds.Top) / 2 + Bounds.Top);
    //            capsule.Render(graphics, Selected, Owner.Locked, true);
                
    //            // Always dispose of a GH_Capsule when you're done with it.
    //            capsule.Dispose();
    //            capsule = null;

    //            graphics.DrawImage(Owner.PublicIcon, Bounds.Left + Bounds.Width / 2 - Owner.PublicIcon.Width / 2f, Bounds.Top + 5);

    //            // Now it's time to draw the text on top of the capsule.
    //            // First we'll draw the Owner NickName using a standard font and a black brush.
    //            // We'll also align the NickName in the center of the Bounds.
    //            StringFormat format = new StringFormat();
    //            format.Alignment = StringAlignment.Center;
    //            format.LineAlignment = StringAlignment.Center;
    //            format.Trimming = StringTrimming.EllipsisCharacter;

    //            // Our entire capsule is 60 pixels high, and we'll draw 
    //            // three lines of text, each 20 pixels high.
    //            RectangleF textRectangle = Bounds;
    //            textRectangle.Height = 20;
    //            textRectangle.Y += 24 + 5;

    //            // Draw the NickName in a Standard Grasshopper font.
    //            graphics.DrawString(Owner.NickName, GH_FontServer.Standard, Brushes.Black, textRectangle, format);

    //            // Now we need to draw the median and mean information.
    //            // Adjust the formatting and the layout rectangle.
    //            format.Alignment = StringAlignment.Near;
    //            textRectangle.Inflate(-5, 0);

    //            textRectangle.Y += 20;
    //            graphics.DrawString($"{Owner.ValCount}", GH_FontServer.StandardItalic, Brushes.Black, textRectangle, format);

    //            // Always dispose of any GDI+ object that implement IDisposable.
    //            format.Dispose();
    //        }
    //    }
    //}
}
