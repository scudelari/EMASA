using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Collections;
using Rhino.Geometry;

namespace GHComponents
{
    internal class GhOptFileManager
    {
        public GhOptFileManager(GH_DocumentObject inGH_Owner, string inVarName, GhOptVariableType inVarType, GhOptVariableDirection inVarDirection)
        {
            _gh_owner = inGH_Owner ?? throw new ArgumentNullException(nameof(inGH_Owner));
            _varType = inVarType;
            _varDirection = inVarDirection;

            if (string.IsNullOrWhiteSpace(inVarName))
            {
                string tryName;
                while (true)
                {
                    Random random = new Random();
                    int randVal = random.Next(1, 100);
                    tryName = "DVar_" + randVal;

                    // Makes sure there is no duplicate on the default value
                    if (!File.Exists(VarFilePath(tryName))) break;
                }

                VarName = tryName;
            }
            else VarName = inVarName;
        }

        private readonly GH_DocumentObject _gh_owner;
        private GH_Document GH_Doc
        {
            get => _gh_owner.OnPingDocument();
        }

        public string VarName { get; private set; }

        private readonly GhOptVariableType _varType;
        public GhOptVariableType VarType
        {
            get => _varType;
        }

        private readonly GhOptVariableDirection _varDirection;
        public GhOptVariableDirection VarDirection
        {
            get => _varDirection;
        }

        public string VarFilePath(string inVarName = null, GH_Document inDoc = null)
        {
            if (inVarName == null) inVarName = VarName;

            // Must be able to overwrite the document that is being targeted
            GH_Document docToTarget = inDoc ?? GH_Doc;

            if (!docToTarget.IsFilePathDefined) return null;
            if (string.IsNullOrWhiteSpace(docToTarget.FilePath)) return null;

            try
            {
                // Gets the document
                string projectFolder = Path.GetDirectoryName(docToTarget.FilePath);
                string ghFilename = Path.GetFileName(docToTarget.FilePath);

                string targetDir = Path.Combine(projectFolder, ghFilename + "_data", VarDirection.ToString());

                DirectoryInfo dirInfo = new DirectoryInfo(targetDir);
                if (!dirInfo.Exists) dirInfo.Create();

                return Path.Combine(targetDir, $"{inVarName}.{VarType}");
            }
            catch (Exception)
            {
                return null;
            }
        }
        public string RangeFilePath(string inVarName = null, GH_Document inDoc = null)
        {
            string varPath = VarFilePath(inVarName, inDoc);
            return varPath == null ? null : varPath + "Range";
        }

        public void UpdateVarName(string inNewName)
        {
            try
            {
                string oldVarFile = VarFilePath(VarName);
                string newVarFile = VarFilePath(inNewName);

                string oldRangeFile = RangeFilePath(VarName);
                string newRangeFile = RangeFilePath(inNewName);

                // If the old file oldVarFile, we must move it
                if (File.Exists(oldVarFile))
                {
                    // The new file already exists; the change of name is aborted
                    if (File.Exists(newVarFile)) return;

                    File.Copy(oldVarFile, newVarFile);
                    File.Delete(oldVarFile);
                }

                // If the old file oldRangeFile, we must move it
                if (File.Exists(oldRangeFile))
                {
                    // The new file already exists; the change of name is aborted
                    if (File.Exists(newRangeFile)) return;

                    File.Copy(oldRangeFile, newRangeFile);
                    File.Delete(oldRangeFile);
                }

                // Stores the value
                VarName = inNewName;
            }
            catch
            {
            }
        }
        
        public void Document_FilePathChanged(object sender, GH_DocFilePathEventArgs e)
        {
            // The file is being loaded, or saved for the first time
            if (e.OldFilePath == null) return;

            try
            {
                DirectoryInfo oldDataDir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(e.OldFilePath), Path.GetFileName(e.OldFilePath) + "_data"));
                DirectoryInfo newDataDir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(e.NewFilePath), Path.GetFileName(e.NewFilePath) + "_data"));
                
                // Has to consider that this function will be called by each of the components' instances in the document

                // The old dir still exists - it must be moved
                if (oldDataDir.Exists)
                {
                    // The new data exists, it means that the save operation is overwriting another file
                    if (newDataDir.Exists) newDataDir.Delete(true);

                    // Executes the move
                    oldDataDir.MoveTo(newDataDir.FullName);
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                // Invokes using reflection because of the Generic typeof the GHParam
                _gh_owner.GetType().GetMethod("ClearData").Invoke(_gh_owner, null);
                _gh_owner.GetType().GetMethod("CollectData").Invoke(_gh_owner, null);
            }
        }

        public void DeleteFiles(GH_Document inDoc)
        {
            string varFile = VarFilePath(inDoc: inDoc);
            if (File.Exists(varFile)) File.Delete(varFile);

            string rangeFile = RangeFilePath(inDoc: inDoc);
            if (File.Exists(rangeFile)) File.Delete(rangeFile);
        }

        // Default values
        private readonly double _defaultDouble = 50d;
        private readonly double[] _defaultDoubleRange = new[] { 0d, 100d };
        public void WriteDefaultDoubleValue()
        {
            File.WriteAllText(VarFilePath(), $"{_defaultDouble}");
        }
        public void WriteDefaultDoubleRange()
        {
            File.WriteAllLines(RangeFilePath(), _defaultDoubleRange.Select(a => $"{a}"));
        }
        
        private readonly Point3d _defaultPoint = new Point3d(50d, 50d, 50d);
        private readonly Point3d[] _defaultPointRange = new[] {new Point3d(0d,0d,0d), new Point3d(100d,100d,100d) };
        public void WriteDefaultPointValue()
        {
            File.WriteAllText(VarFilePath(), $"{_defaultPoint}");
        }
        public void WriteDefaultPointRange()
        {
            File.WriteAllLines(RangeFilePath(), _defaultPointRange.Select(a => $"{a}"));
        }

        public double ReadDoubleValue()
        {
            // Tries to read the file. If anything wrong happens, recreates the file with default values
            try
            {
                return Convert.ToDouble(File.ReadAllText(VarFilePath()));
            }
            catch (Exception e)
            {
                DeleteFiles(GH_Doc);
                WriteDefaultDoubleValue();
                WriteDefaultDoubleRange();

                return _defaultDouble;
            }
        }
        public double[] ReadDoubleRange()
        {
            // Tries to read the file. If anything wrong happens, recreates the file with default values
            try
            {
                string[] lines = File.ReadAllLines(RangeFilePath());
                return lines.Select(Convert.ToDouble).ToArray();
            }
            catch (Exception e)
            {
                DeleteFiles(GH_Doc);
                WriteDefaultDoubleValue();
                WriteDefaultDoubleRange();

                return _defaultDoubleRange;
            }
        }

        public Point3d ReadPointValue()
        {
            // Tries to read the file. If anything wrong happens, recreates the file with default values
            try
            {
                if (Point3d.TryParse(File.ReadAllText(VarFilePath()), out Point3d p)) return p;
                throw new Exception();
            }
            catch (Exception e)
            {
                DeleteFiles(GH_Doc);
                WriteDefaultPointValue();
                WriteDefaultPointRange();

                return _defaultPoint;
            }
        }
        public Point3d[] ReadPointRange()
        {
            // Tries to read the file. If anything wrong happens, recreates the file with default values
            try
            {
                string[] lines = File.ReadAllLines(RangeFilePath());
                return lines.Select(l =>
                {
                    if (Point3d.TryParse(l, out Point3d p)) return p;
                    throw new Exception();
                }).ToArray();
            }
            catch (Exception e)
            {
                DeleteFiles(GH_Doc);
                WriteDefaultPointValue();
                WriteDefaultPointRange();

                return _defaultPointRange;
            }
        }

        public void WriteEmptyFile()
        {
            File.WriteAllText(VarFilePath(), "");
        }
        public void WriteDoubleValue(double inValue)
        {
            File.WriteAllText(VarFilePath(), $"{inValue}");
        }
        public void WriteDoubleValues(IEnumerable<double> inValues)
        {
            File.WriteAllLines(VarFilePath(), inValues.Select(a => $"{a}"));
        }
        public void WritePoint3dValues(IEnumerable<Point3d> inValues)
        {
            File.WriteAllLines(VarFilePath(), inValues.Select(a => $"{a}"));
        }
        public void WriteLine3dValues(IEnumerable<Line> inValues)
        {
            File.WriteAllLines(VarFilePath(), inValues.Select(a => $"{a.From}\t{a.To}"));
        }
        public double[] WriteDoubleRange(double inMin = double.NaN, double inMax = double.NaN)
        {
            if (double.IsNaN(inMin)  && double.IsNaN(inMax)) throw new ArgumentException("You must specify either the min or the max, or both.");

            double[] toWrite = new double[2] {inMin, inMax};

            // If one is not given, recovers from the current values
            if (double.IsNaN(inMin) || double.IsNaN(inMax))
            {
                double[] currentRange = ReadDoubleRange();
                if (double.IsNaN(toWrite[0])) toWrite[0] = currentRange[0];
                if (double.IsNaN(toWrite[1])) toWrite[1] = currentRange[1];
            }
            
            // Ensures we have a correct order
            if (toWrite[0] == toWrite[1]) throw new ArgumentException("The values of the range must be different.");

            // If in the wrong order, swap
            if (toWrite[0] > toWrite[1])
            {
                double tmp = toWrite[1];
                toWrite[1] = toWrite[0];
                toWrite[0] = tmp;
            }

            // Finally, writes the values to the disk
            File.WriteAllLines(RangeFilePath(), toWrite.Select(a => $"{a}"));

            return toWrite;
        }

        public void WritePointValue(Point3d inValue)
        {
            File.WriteAllText(VarFilePath(), $"{inValue}");
        }
        public Point3d[] WritePointRange(Point3d? inMin = null, Point3d? inMax = null)
        {
            if (!inMin.HasValue && !inMax.HasValue) throw new ArgumentException("You must specify either the min or the max, or both.");

            // Both have values
            Point3d[] toWrite = new Point3d[2];
            if (inMin.HasValue && inMax.HasValue)
            {
                toWrite[0] = inMin.Value; 
                toWrite[1] = inMax.Value;
            }
            else // One of them does not have values, recovers from the current values
            {
                Point3d[] currentRange = ReadPointRange();
                toWrite[0] = inMin ?? currentRange[0];
                toWrite[1] = inMax ?? currentRange[1];
            }

            // Ensures we have a correct order; == for Point3d is coordinate-wise
            if (toWrite[0].X == toWrite[1].X || 
                toWrite[0].Y == toWrite[1].Y || 
                toWrite[0].Z == toWrite[1].Z) throw new ArgumentException("The values of each coordinate of the range must be different.");

            // Fixes the range, which is coordinate wise
            Point3d fixMin = new Point3d(
                Math.Min(toWrite[0].X, toWrite[1].X),
                Math.Min(toWrite[0].Y, toWrite[1].Y),
                Math.Min(toWrite[0].Z, toWrite[1].Z)
                );
            Point3d fixMax = new Point3d(
                Math.Max(toWrite[0].X, toWrite[1].X),
                Math.Max(toWrite[0].Y, toWrite[1].Y),
                Math.Max(toWrite[0].Z, toWrite[1].Z)
            );
            toWrite[0] = fixMin;
            toWrite[1] = fixMax;

            // Finally, writes the values to the disk
            File.WriteAllLines(RangeFilePath(), toWrite.Select(a => $"{a}"));

            return toWrite;
        }
    }
}
