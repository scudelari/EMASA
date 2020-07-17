using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AccordHelper.FEA.Items;

namespace AccordHelper.FEA
{
    public class AnsysModel : FeModelBase
    {
        public override string SubDir { get; } = "Ansys";
        private string ansysExeLocation = @"C:\Program Files\ANSYS Inc\v201\ansys\bin\winx64\ANSYS201.exe";

        private bool IsInitialized => _commandLineProcess != null;

        private Process _commandLineProcess = null;

        /// <summary>
        /// Defines an Ansys Model.
        /// </summary>
        /// <param name="inModelFolder">The target folder for the analysis</param>
        public AnsysModel(string inModelFolder) : base(inModelFolder, "ansys.dat")
        {
        }

        public override void InitializeModelAndSoftware()
        {
            _commandLineProcess = new Process();
            _commandLineProcess.StartInfo.FileName = "cmd.exe";
            _commandLineProcess.StartInfo.RedirectStandardInput = true;
            _commandLineProcess.StartInfo.RedirectStandardOutput = true;
            _commandLineProcess.StartInfo.RedirectStandardError = true;
            //_commandLineProcess.StartInfo.WorkingDirectory = ModelFolder;
            _commandLineProcess.StartInfo.CreateNoWindow = true;
            _commandLineProcess.StartInfo.UseShellExecute = false;
            _commandLineProcess.Start();

            _commandLineProcess.StandardInput.Write($"CD \"{ModelFolder}\"");
            _commandLineProcess.StandardInput.Flush();

            _commandLineProcess.StandardInput.WriteLine("SET ANSYS201_PRODUCT=ANSYS");
            _commandLineProcess.StandardInput.Flush();

            _commandLineProcess.StandardInput.WriteLine("SET ANS_CONSEC=YES");
            _commandLineProcess.StandardInput.Flush();

            // Consumes and clears all output
            string s = ReadToEnd_NoLock(_commandLineProcess.StandardOutput);
        }

        public override void CloseApplication()
        {
            _commandLineProcess.StandardInput.Close();
            _commandLineProcess.WaitForExit();

            _commandLineProcess = null;
        }

        public override void WriteModelData()
        {
            if (Directory.Exists(ModelFolder)) Directory.Delete(ModelFolder, true);
            Directory.CreateDirectory(ModelFolder);

            if (Frames.Count == 0) throw new Exception("The model must have defined frames!");
            if (Groups.Count == 0 || Groups.Count(a => a.Restraint != null) == 0) throw new Exception("The model must have defined restraints!");

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("! Model generated using EMASA's Rhino Automator.");
            sb.AppendLine();
            sb.AppendLine("! Changes the Working Directory");
            sb.AppendLine($"/CWD,'{ModelFolder}'");
            sb.AppendLine();
            sb.AppendLine($"/title, '{ModelName}'");
            sb.AppendLine($"/UIS,MSGPOP,3 ! Sets the pop-up messages to appear only if they are errors");

            sb.AppendLine("/PREP7").AppendLine().AppendLine();
            sb.AppendLine("! Adding the Element Type");
            sb.AppendLine("ET,1,BEAM189");
            sb.AppendLine("beam_element_type = 1");
            sb.AppendLine("KEYOPT,beam_element_type,1,0 ! Warping DOF 0 Six degrees of freedom per node, unrestrained warping (default)");
            sb.AppendLine("KEYOPT,beam_element_type,2,0 ! XSection Scaling Cross-section is scaled as a function of axial stretch (default); applies only if NLGEOM,ON has been invoked");
            sb.AppendLine("KEYOPT,beam_element_type,4,1 ! Shear Stress Options 1 - Output only flexure-related transverse-shear stresses");
            sb.AppendLine("KEYOPT,beam_element_type,6,1 ! Active only when OUTPR,ESOL is active: Output section forces/moments and strains/curvatures at integration points along the length (default) plus current section area");
            sb.AppendLine("KEYOPT,beam_element_type,7,2 ! Active only when OUTPR,ESOL is active: Output control at section integration point Maximum and minimum stresses/strains plus stresses and strains at each section point");
            sb.AppendLine("KEYOPT,beam_element_type,9,3 ! Active only when OUTPR,ESOL is active: Output control for values extrapolated to the element and section nodes Maximum and minimum stresses/strains plus stresses and strains along the exterior boundary of the cross-section plus stresses and strains at all section nodes");
            sb.AppendLine("KEYOPT,beam_element_type,11,0 ! Set section properties Automatically determine if preintegrated section properties can be used (default)");
            sb.AppendLine("KEYOPT,beam_element_type,12,0 ! Tapered section treatment Linear tapered section analysis");
            sb.AppendLine("KEYOPT,beam_element_type,15,0 ! Results file format: Store averaged results at each section corner node (default)");

            sb.AppendLine().AppendLine();
            sb.AppendLine("! Adding Material Definitions");
            foreach (FeMaterial feMaterial in Materials)
            {
                sb.AppendLine($"MAT,{feMaterial.Id} ! Material Name {feMaterial.Name}");
                sb.AppendLine($"MP,EX,{feMaterial.Id},{feMaterial.YoungModulus} ! Setting Youngs Modulus");
                sb.AppendLine($"MP,PRXY,{feMaterial.Id},{feMaterial.Poisson} ! Setting Poissons Ratio");
                sb.AppendLine($"MP,DENS,{feMaterial.Id},{feMaterial.Density} ! Setting Density");
            }

            sb.AppendLine().AppendLine();

            sb.AppendLine("! Adding the ORDERED Joints");
            foreach (FeJoint feJoint in Joints.OrderBy(a => a.Id))
            {
                sb.AppendLine($"K,{feJoint.Id},{feJoint.Point.X},{feJoint.Point.Y},{feJoint.Point.Z} ! CSharp Joint ID: {feJoint.Id}");
            }

            sb.AppendLine().AppendLine();

            sb.AppendLine("! Adding the ORDERED Lines");
            foreach (FeFrame feFrame in Frames.OrderBy(a => a.Id))
            {
                sb.AppendLine($"L,{feFrame.IJoint.Id},{feFrame.JJoint.Id} ! CSharp Line ID: {feFrame.Id}");
            }

            sb.AppendLine().AppendLine();

            sb.AppendLine("! Defining the sections");
            foreach (FeSection feSection in Sections)
            {
                sb.AppendLine($"! BEGIN Define Section {feSection.ToString()}");
                sb.AppendLine(feSection.AnsysSecTypeLine);
                sb.AppendLine(feSection.AnsysSecDataLine);

                sb.AppendLine("LSEL,NONE ! Clearing line selection");
                foreach (FeFrame feFrame in Frames.Where(a => a.Section == feSection))
                {
                    sb.AppendLine($"LSEL,A,LINE,,{feFrame.Id}");
                }
                sb.AppendLine($"LATT,{feSection.Material.Id}, ,beam_element_type, , , ,{feSection.Id} ! Sets the #1 Material; #3 ElementType, #7 Section for all selected lines");
            }

            sb.AppendLine().AppendLine();

            sb.AppendLine("! Defining the Groups");
            foreach (FeGroup feGroup in Groups)
            {
                sb.AppendLine().AppendLine($"! GroupName: {feGroup.Name}");
                // Selects the joints
                sb.AppendLine("KSEL,NONE ! Clearing KP selection");
                foreach (FeJoint feJoint in feGroup.Joints)
                {
                    sb.AppendLine($"KSEL,A,KP,,{feJoint.Id}");
                }

                // Selects the frames
                sb.AppendLine("LSEL,NONE ! Clearing Line selection");
                foreach (FeFrame feFrame in feGroup.Frames)
                {
                    sb.AppendLine($"LSEL,A,LINE,,{feFrame.Id}");
                }

                // Adds them to the components
                sb.AppendLine($"CM,{feGroup.Name}_J,KP ! The component that has the Joints of Group {feGroup.Name}");
                sb.AppendLine($"CM,{feGroup.Name}_L,LINE ! The component that has the Lines of Group {feGroup.Name}");

                sb.AppendLine($"CMGRP,{feGroup.Name},{feGroup.Name}_J,{feGroup.Name}_L ! Putting the joint and line components into same assembly {feGroup.Name}");
                sb.AppendLine();
            }

            sb.AppendLine().AppendLine();

            sb.AppendLine("! Meshing the Frames");
            sb.AppendLine("LSEL,ALL");
            sb.AppendLine("LESIZE,ALL, , ,3, , , , ,1");
            sb.AppendLine("LMESH,ALL");

            sb.AppendLine().AppendLine();

            sb.AppendLine("! Going to Solution Context");
            sb.AppendLine("FINISH");
            sb.AppendLine("/SOLU");

            sb.AppendLine().AppendLine();

            sb.AppendLine("! Defining the Restraints");
            foreach (FeGroup feGroup in Groups.Where(a => a.Restraint != null))
            {
                sb.AppendLine("KSEL,NONE ! Clearing KP selection");

                sb.AppendLine($"CMSEL,A,{feGroup.Name},KP ! Selecting the Joints that are in the assembly.");

                if (feGroup.Restraint.IsAll)
                {
                    sb.AppendLine($"DK,ALL,ALL,0 ! Sets all locked for given component");
                }
                else
                {
                    if (feGroup.Restraint.DoF[0])
                    {
                        sb.AppendLine($"DK,ALL,UX,0 ! Sets UX for previously selected joints");
                    }
                    if (feGroup.Restraint.DoF[1])
                    {
                        sb.AppendLine($"DK,ALL,UY,0 ! Sets UY for previously selected joints");
                    }
                    if (feGroup.Restraint.DoF[2])
                    {
                        sb.AppendLine($"DK,ALL,UZ,0 ! Sets UZ for previously selected joints");
                    }
                    if (feGroup.Restraint.DoF[3])
                    {
                        sb.AppendLine($"DK,ALL,ROTX,0 ! Sets ROTX for previously selected joints");
                    }
                    if (feGroup.Restraint.DoF[4])
                    {
                        sb.AppendLine($"DK,ALL,ROTY,0 ! Sets ROTY for previously selected joints");
                    }
                    if (feGroup.Restraint.DoF[5])
                    {
                        sb.AppendLine($"DK,ALL,ROTZ,0 ! Sets ROTZ for previously selected joints");
                    }
                }

                sb.AppendLine();
            }

            sb.AppendLine().AppendLine();

            sb.AppendLine("! Defining the Loads");
            sb.AppendLine("ACEL,0,0,9.80665 ! Sets the Gravity");

            sb.AppendLine().AppendLine();

            sb.AppendLine("! SOLVING");
            sb.AppendLine("ALLSEL,ALL ! Somehow, and for any weird-ass reason, Ansys will only pass KeyPoints definitions onwards if they are selected.");
            sb.AppendLine("SOLVE ! Solves the problem");
            sb.AppendLine("FINISH");


            sb.AppendLine().AppendLine();

            sb.AppendLine("/POST1 ! Changes to PostProc");
            sb.AppendLine("SET,LAST ! Gets the result set of the last timestep");

            sb.AppendLine().AppendLine().AppendLine(PrintLinesWithElements);
            sb.AppendLine().AppendLine().AppendLine(PrintNodalBasicData);
            sb.AppendLine().AppendLine().AppendLine(PrintNodalResults);
            sb.AppendLine().AppendLine().AppendLine(PrintElementResults);

            sb.AppendLine().AppendLine().AppendLine(PrintFinishSignal);

            // Writes the file
            if (File.Exists(FullFileName)) File.Delete(FullFileName);
            File.WriteAllText(FullFileName, sb.ToString());
        }

        public override void RunAnalysis()
        {
            if (_commandLineProcess == null) throw new InvalidOperationException($"You must first initialize the analysis.");

            if (!File.Exists(FullFileName)) throw new InvalidOperationException($"The Ansys model {FullFileName} could not be found.");

            string cmdString = $"\"{ansysExeLocation}\" -s noread -b -i \"{FullFileName}\" -o \"{FullFileName + "_out"}\"";

            // Issues the Ansys command
            _commandLineProcess.StandardInput.WriteLine(cmdString);
            _commandLineProcess.StandardInput.Flush();

            // Locks until result comes back from the output
            while (true)
            {
                if (ReadToEnd_NoLock(_commandLineProcess.StandardOutput).Contains(FileName)) break;
            }

            // Writes a file - will be done after the solve
            string signalFilename = Path.Combine(ModelFolder, "finish.signal");

            _commandLineProcess.StandardInput.WriteLine($"fsdafsa 1> \"{signalFilename}\" 2>&1");
            _commandLineProcess.StandardInput.Flush();
            
            // Deletes the file
            File.Delete(signalFilename);

            // DEBUG
            // Copies the original file
            string newName = FullFileName + "NEW";
            File.Copy(FullFileName, newName);

            string newCmd = $"\"{ansysExeLocation}\" -s noread -b -i \"{newName}\" -o \"{newName + "_out"}\"";

            _commandLineProcess.StandardInput.WriteLine(cmdString);
            _commandLineProcess.StandardInput.Flush();

            _commandLineProcess.StandardInput.WriteLine($"fsdafsa 1> \"{signalFilename}\" 2>&1");
            _commandLineProcess.StandardInput.Flush();

            using (FileSystemWatcher fw = new FileSystemWatcher())
            {
                fw.Path = Path.Combine(ModelFolder, "finish.signal");
                fw.NotifyFilter = NotifyFilters.FileName;
                WaitForChangedResult result = fw.WaitForChanged(WatcherChangeTypes.All, 100000000);
            }

            // Deletes the file
            File.Delete(signalFilename);
        }

        private void FwOnCreated(object inSender, FileSystemEventArgs inE)
        {
            throw new NotImplementedException();
        }

        public override void SaveDataAs(string inFilePath)
        {
            throw new NotImplementedException();
        }

        public override T GetResult<T>(string inResultName)
        {
            throw new NotImplementedException();
        }

        private string ReadToEnd_NoLock(StreamReader inStreamReader)
        {
            // Consumes all output until end
            List<char> buffer = new List<char>();
            while (true)
            {
                int peekResult = _commandLineProcess.StandardOutput.Peek();
                if (peekResult == -1) break;

                int readVal = _commandLineProcess.StandardOutput.Read();
                buffer.Add((char)readVal);
            }
            return new string(buffer.ToArray());
        }

        private const string PrintNodalBasicData = @"! Printing - Nodal data
NSEL,ALL
/OUTPUT,'nodes_locations','ems'
NLIST

/OUTPUT,'nodes_reactions','ems'
PRRSOL";
        private const string PrintLinesWithElements = @"! #################################
! GETS LIST OF LINES WITH ELEMENTS AND NODES I,J,K

LSEL,ALL ! Selects all lines
*GET,total_line_count,LINE,0,COUNT ! Gets count of all lines

ESEL,ALL ! Selects all elements
*GET,total_element_count,ELEM,0,COUNT ! Gets count of all elements

*DIM,line_element_match,ARRAY,total_element_count,5 ! Defines the target array

! Makes a loop on the lines
currentline = 0 !  Declares a start number for the lines
elemindex = 1 ! The element index in the array

*DO,i,1,total_line_count ! loops on all lines
	
	LSEL,ALL ! Reselects all lines
	*GET,nextline,LINE,currentline,NXTH ! Gets the number of the next line in the selected set
	
	LSEL,S,LINE,,nextline,,,1 ! Selects the next line and its associated elements
	
	currentelement = 0 ! Declares a start number for the current element
	*GET,lecount,ELEM,0,COUNT ! Gets the number of selected elements
	
	*DO,j,1,lecount ! loops on the selected elements in this line
		*GET,nextelement,ELEM,currentelement,NXTH ! Gets the number of the next element in the selected set
		
		! Getting the nodes of each element
		*GET,e_i,ELEM,nextelement,NODE,1
		*GET,e_j,ELEM,nextelement,NODE,2
		*GET,e_k,ELEM,nextelement,NODE,3
		
		! Stores into the array
		line_element_match(elemindex,1) = nextline
		line_element_match(elemindex,2) = nextelement
		line_element_match(elemindex,3) = e_i
		line_element_match(elemindex,4) = e_j
		line_element_match(elemindex,5) = e_k
		
		currentelement = nextelement ! updates for the next iteration
		elemindex = elemindex + 1 ! Increments the element index counter
	*ENDDO
	
	currentline = nextline ! updates for the next iteration
*ENDDO

! Writes the data to file
*CFOPEN,'line_element_data','ems' ! Opens the file

! Writes the header
*VWRITE,'LINE', ',' ,'ELEM', ',' , 'INODE' , ',' , 'JNODE', ',' , 'KNODE'
(A4,A1,A4,A1,A5,A1,A5,A1,A5)

! Writes the data
*VWRITE,line_element_match(1,1), ',' ,line_element_match(1,2), ',' , line_element_match(1,3) , ',' , line_element_match(1,4), ',' , line_element_match(1,5)
%I%C%I%C%I%C%I%C%I

*CFCLOSE";
        private const string PrintElementResults = @"! Selects all elements
ESEL,ALL

! First, acquires the tables
ETABLE, i_Fx, SMISC, 1
ETABLE, i_My, SMISC, 2
ETABLE, i_Mz, SMISC, 3
ETABLE, i_Tq, SMISC, 4  ! Torsional Moment
ETABLE, i_SFz, SMISC, 5  ! Shear Force Z
ETABLE, i_SFy, SMISC, 6  ! Shear Force Z

ETABLE, i_Ex, SMISC, 7  ! Axial Strain
ETABLE, i_Ky, SMISC, 8  ! Curvature Y
ETABLE, i_Kz, SMISC, 9  ! Curvature Z
ETABLE, i_SEz, SMISC, 11  ! Strain Z
ETABLE, i_SEy, SMISC, 12  ! Strain Y

ETABLE, i_SDIR, SMISC, 31 ! Axial Direct Stress
ETABLE, i_SByT, SMISC, 32 ! Bending stress on the element +Y side of the beam
ETABLE, i_SByB, SMISC, 33 ! Bending stress on the element -Y side of the beam
ETABLE, i_SBzT, SMISC, 34 ! Bending stress on the element +Z side of the beam
ETABLE, i_SBzB, SMISC, 35 ! Bending stress on the element -Z side of the beam

ETABLE, i_EPELDIR, SMISC, 41 ! Axial strain at the end
ETABLE, i_EPELByT, SMISC, 42 ! Bending strain on the element +Y side of the beam.
ETABLE, i_EPELByB, SMISC, 43 ! Bending strain on the element -Y side of the beam.
ETABLE, i_EPELBzT, SMISC, 44 ! Bending strain on the element +Z side of the beam.
ETABLE, i_EPELBzB, SMISC, 45 ! Bending strain on the element -Z side of the beam.

ETABLE, j_Fx, SMISC, 14
ETABLE, j_My, SMISC, 15
ETABLE, j_Mz, SMISC, 16
ETABLE, j_Tq, SMISC, 17 ! Torsional Moment
ETABLE, j_SFz, SMISC, 18  ! Shear Force Z
ETABLE, j_SFy, SMISC, 19  ! Shear Force Z
ETABLE, j_Ex, SMISC, 20  ! Axial Strain
ETABLE, j_Ky, SMISC, 21  ! Curvature Y
ETABLE, j_Kz, SMISC, 22  ! Curvature Z
ETABLE, j_SEz, SMISC, 24  ! Strain Z
ETABLE, j_SEy, SMISC, 25  ! Strain Y

ETABLE, j_SDIR, SMISC, 36 ! Axial Direct Stress
ETABLE, j_SByT, SMISC, 37 ! Bending stress on the element +Y side of the beam
ETABLE, j_SByB, SMISC, 38 ! Bending stress on the element -Y side of the beam
ETABLE, j_SBzT, SMISC, 39 ! Bending stress on the element +Z side of the beam
ETABLE, j_SBzB, SMISC, 40 ! Bending stress on the element -Z side of the beam

ETABLE, j_EPELDIR, SMISC, 46 ! Axial strain at the end
ETABLE, j_EPELByT, SMISC, 47 ! Bending strain on the element +Y side of the beam.
ETABLE, j_EPELByB, SMISC, 48 ! Bending strain on the element -Y side of the beam.
ETABLE, j_EPELBzT, SMISC, 49 ! Bending strain on the element +Z side of the beam.
ETABLE, j_EPELBzB, SMISC, 50 ! Bending strain on the element -Z side of the beam.

! Gets the ELEMENTAL data
ETABLE, e_StrEn, SENE




! WRITE: I NODE BASIC FORCE DATA
*DIM,ibasicf,ARRAY,total_element_count,6
*VGET,ibasicf(1,1),ELEM,1,ETAB,i_Fx
*VGET,ibasicf(1,2),ELEM,1,ETAB,i_My
*VGET,ibasicf(1,3),ELEM,1,ETAB,i_Mz
*VGET,ibasicf(1,4),ELEM,1,ETAB,i_Tq
*VGET,ibasicf(1,5),ELEM,1,ETAB,i_SFz
*VGET,ibasicf(1,6),ELEM,1,ETAB,i_SFy

! Writes the data to file
*CFOPEN,'result_element_inode_basic_forces','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'i_Fx', ',' , 'i_My' , ',' , 'i_Mz', ',' , 'i_Tq', ',' , 'i_SFz', ',' , 'i_SFy'
(A8,A1,A8,A1,A8,A1,A8,A1,A8,A1,A8,A1,A8)

! Writes the data
*VWRITE,SEQU, ',' ,ibasicf(1,1), ',' , ibasicf(1,2) , ',' , ibasicf(1,3), ',' , ibasicf(1,4), ',' , ibasicf(1,5), ',' , ibasicf(1,6)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE


! WRITE: I NODE BASIC STRAIN DATA
*DIM,ibasics,ARRAY,total_element_count,5
*VGET,ibasics(1,1),ELEM,1,ETAB,i_Ex
*VGET,ibasics(1,2),ELEM,1,ETAB,i_Ky
*VGET,ibasics(1,3),ELEM,1,ETAB,i_Kz
*VGET,ibasics(1,4),ELEM,1,ETAB,i_SEz
*VGET,ibasics(1,5),ELEM,1,ETAB,i_SEy

! Writes the data to file
*CFOPEN,'result_element_inode_basic_strains','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'i_Ex', ',' , 'i_Ky' , ',' , 'i_Kz', ',' , 'i_SEz', ',' , 'i_SEy'
(A8,A1,A8,A1,A8,A1,A8,A1,A8,A1,A8)

! Writes the data
*VWRITE,SEQU, ',' ,ibasics(1,1), ',' , ibasics(1,2) , ',' , ibasics(1,3), ',' , ibasics(1,4), ',' , ibasics(1,5)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE


! WRITE: I NODE BASIC DIRECTIONAL STRESS
*DIM,ibasic_dirstress,ARRAY,total_element_count,5
*VGET,ibasic_dirstress(1,1),ELEM,1,ETAB,i_SDIR
*VGET,ibasic_dirstress(1,2),ELEM,1,ETAB,i_SByT
*VGET,ibasic_dirstress(1,3),ELEM,1,ETAB,i_SByB
*VGET,ibasic_dirstress(1,4),ELEM,1,ETAB,i_SBzT
*VGET,ibasic_dirstress(1,5),ELEM,1,ETAB,i_SBzB

! Writes the data to file
*CFOPEN,'result_element_inode_basic_dirstress','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'i_SDIR', ',' , 'i_SByT' , ',' , 'i_SByB', ',' , 'i_SBzT', ',' , 'i_SBzB'
(A8,A1,A8,A1,A8,A1,A8,A1,A8,A1,A8)

! Writes the data
*VWRITE,SEQU, ',' ,ibasic_dirstress(1,1), ',' , ibasic_dirstress(1,2) , ',' , ibasic_dirstress(1,3), ',' , ibasic_dirstress(1,4), ',' , ibasic_dirstress(1,5)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE


! WRITE: I NODE BASIC DIRECTIONAL STRAIN
*DIM,ibasic_dirstrain,ARRAY,total_element_count,5
*VGET,ibasic_dirstrain(1,1),ELEM,1,ETAB,i_EPELDIR
*VGET,ibasic_dirstrain(1,2),ELEM,1,ETAB,i_EPELByT
*VGET,ibasic_dirstrain(1,3),ELEM,1,ETAB,i_EPELByB
*VGET,ibasic_dirstrain(1,4),ELEM,1,ETAB,i_EPELBzT
*VGET,ibasic_dirstrain(1,5),ELEM,1,ETAB,i_EPELBzB

! Writes the data to file
*CFOPEN,'result_element_inode_basic_dirstrain','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'i_EPELDIR', ',' , 'i_EPELByT' , ',' , 'i_EPELByB', ',' , 'i_EPELBzT', ',' , 'i_EPELBzB'
(A8,A1,A8,A1,A8,A1,A8,A1,A8,A1,A8)

! Writes the data
*VWRITE,SEQU, ',' ,ibasic_dirstrain(1,1), ',' , ibasic_dirstrain(1,2) , ',' , ibasic_dirstrain(1,3), ',' , ibasic_dirstrain(1,4), ',' , ibasic_dirstrain(1,5)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE






! WRITE: J NODE BASIC FORCE DATA
*DIM,jbasicf,ARRAY,total_element_count,6
*VGET,jbasicf(1,1),ELEM,1,ETAB,j_Fx
*VGET,jbasicf(1,2),ELEM,1,ETAB,j_My
*VGET,jbasicf(1,3),ELEM,1,ETAB,j_Mz
*VGET,jbasicf(1,4),ELEM,1,ETAB,j_Tq
*VGET,jbasicf(1,5),ELEM,1,ETAB,j_SFz
*VGET,jbasicf(1,6),ELEM,1,ETAB,j_SFy

! Writes the data to file
*CFOPEN,'result_element_jnode_basic_forces','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'j_Fx', ',' , 'j_My' , ',' , 'j_Mz', ',' , 'j_Tq', ',' , 'j_SFz', ',' , 'j_SFy'
(A8,A1,A8,A1,A8,A1,A8,A1,A8,A1,A8,A1,A8)

! Writes the data
*VWRITE,SEQU, ',' ,jbasicf(1,1), ',' , jbasicf(1,2) , ',' , jbasicf(1,3), ',' , jbasicf(1,4), ',' , jbasicf(1,5), ',' , jbasicf(1,6)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE


! WRITE: J NODE BASIC STRAIN DATA
*DIM,jbasics,ARRAY,total_element_count,5
*VGET,jbasics(1,1),ELEM,1,ETAB,j_Ex
*VGET,jbasics(1,2),ELEM,1,ETAB,j_Ky
*VGET,jbasics(1,3),ELEM,1,ETAB,j_Kz
*VGET,jbasics(1,4),ELEM,1,ETAB,j_SEz
*VGET,jbasics(1,5),ELEM,1,ETAB,j_SEy

! Writes the data to file
*CFOPEN,'result_element_jnode_basic_strains','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'j_Ex', ',' , 'j_Ky' , ',' , 'j_Kz', ',' , 'j_SEz', ',' , 'j_SEy'
(A8,A1,A8,A1,A8,A1,A8,A1,A8,A1,A8)

! Writes the data
*VWRITE,SEQU, ',' ,jbasics(1,1), ',' , jbasics(1,2) , ',' , jbasics(1,3), ',' , jbasics(1,4), ',' , jbasics(1,5)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE


! WRITE: J NODE BASIC DIRECTIONAL STRESS
*DIM,jbasic_dirstress,ARRAY,total_element_count,5
*VGET,jbasic_dirstress(1,1),ELEM,1,ETAB,j_SDIR
*VGET,jbasic_dirstress(1,2),ELEM,1,ETAB,j_SByT
*VGET,jbasic_dirstress(1,3),ELEM,1,ETAB,j_SByB
*VGET,jbasic_dirstress(1,4),ELEM,1,ETAB,j_SBzT
*VGET,jbasic_dirstress(1,5),ELEM,1,ETAB,j_SBzB

! Writes the data to file
*CFOPEN,'result_element_jnode_basic_dirstress','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'j_SDIR', ',' , 'j_SByT' , ',' , 'j_SByB', ',' , 'j_SBzT', ',' , 'j_SBzB'
(A8,A1,A8,A1,A8,A1,A8,A1,A8,A1,A8)

! Writes the data
*VWRITE,SEQU, ',' ,jbasic_dirstress(1,1), ',' , jbasic_dirstress(1,2) , ',' , jbasic_dirstress(1,3), ',' , jbasic_dirstress(1,4), ',' , jbasic_dirstress(1,5)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE


! WRITE: J NODE BASIC DIRECTIONAL STRAIN
*DIM,jbasic_dirstrain,ARRAY,total_element_count,5
*VGET,jbasic_dirstrain(1,1),ELEM,1,ETAB,j_EPELDIR
*VGET,jbasic_dirstrain(1,2),ELEM,1,ETAB,j_EPELByT
*VGET,jbasic_dirstrain(1,3),ELEM,1,ETAB,j_EPELByB
*VGET,jbasic_dirstrain(1,4),ELEM,1,ETAB,j_EPELBzT
*VGET,jbasic_dirstrain(1,5),ELEM,1,ETAB,j_EPELBzB

! Writes the data to file
*CFOPEN,'result_element_jnode_basic_dirstrain','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'j_EPELDIR', ',' , 'j_EPELByT' , ',' , 'j_EPELByB', ',' , 'j_EPELBzT', ',' , 'j_EPELBzB'
(A8,A1,A8,A1,A8,A1,A8,A1,A8,A1,A8)

! Writes the data
*VWRITE,SEQU, ',' ,jbasic_dirstrain(1,1), ',' , jbasic_dirstrain(1,2) , ',' , jbasic_dirstrain(1,3), ',' , jbasic_dirstrain(1,4), ',' , jbasic_dirstrain(1,5)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE



! WRITE: STRAIN ENERGY
*DIM,elem_strain_enery,ARRAY,total_element_count,1
*VGET,elem_strain_enery(1,1),ELEM,1,ETAB,e_StrEn

! Writes the data to file
*CFOPEN,'result_element_senergy','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'e_StrEn'
(A8,A1,A8)

! Writes the data
*VWRITE,SEQU, ',' ,elem_strain_enery(1,1)
%I%C%30.6G

*CFCLOSE";
        private const string PrintNodalResults = @"! SELECTS ALL NODES
NSEL,ALL

! GETS THE NODAL RESULTS
*VGET,n_stress(1,1),NODE,1,S,1  ! Stress Principal 1
*VGET,n_stress(1,2),NODE,1,S,2  ! Stress Principal 1
*VGET,n_stress(1,3),NODE,1,S,3  ! Stress Principal 1
*VGET,n_stress(1,4),NODE,1,S,INT  ! Stress Intensity
*VGET,n_stress(1,5),NODE,1,S,EQV  ! Stress Von Mises

*VGET,n_totalstrain(1,1),NODE,1,EPTO,1  ! TOTAL Strain Principal 1
*VGET,n_totalstrain(1,2),NODE,1,EPTO,2  ! TOTAL Strain Principal 1
*VGET,n_totalstrain(1,3),NODE,1,EPTO,3  ! TOTAL Strain Principal 1
*VGET,n_totalstrain(1,4),NODE,1,EPTO,INT  ! TOTAL Strain Intensity
*VGET,n_totalstrain(1,5),NODE,1,EPTO,EQV  ! TOTAL Strain Von Mises

*VGET,n_dof(1,1),NODE,1,U,X  ! Displacement X
*VGET,n_dof(1,2),NODE,1,U,Y  ! Displacement Y
*VGET,n_dof(1,3),NODE,1,U,Z  ! Displacement Z
*VGET,n_dof(1,4),NODE,1,ROT,X  ! Rotation X
*VGET,n_dof(1,5),NODE,1,ROT,Y  ! Rotation Y
*VGET,n_dof(1,6),NODE,1,ROT,Z  ! Rotation Z


! Writes the data to file
*CFOPEN,'result_nodal_displacements','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'UX', ',' , 'UY' , ',' , 'UZ', ',' , 'RX', ',' , 'RY', ',' , 'RZ'
(A8,A1,A8,A1,A8,A1,A8,A1,A8,A1,A8,A1,A8)

! Writes the data
*VWRITE,SEQU, ',' ,n_dof(1,1), ',' , n_dof(1,2) , ',' , n_dof(1,3), ',' , n_dof(1,4), ',' , n_dof(1,5), ',' , n_dof(1,6)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE


! Writes the data to file
*CFOPEN,'result_nodal_stress','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'Stress1', ',' , 'Stress2' , ',' , 'Stress3', ',' , 'StressINT', ',' , 'StressEQV'
(A8,A1,A8,A1,A8,A1,A8,A1,A8,A1,A8)

! Writes the data
*VWRITE,SEQU, ',' ,n_stress(1,1), ',' , n_stress(1,2) , ',' , n_stress(1,3), ',' , n_stress(1,4), ',' , n_stress(1,5)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE


! Writes the data to file
*CFOPEN,'result_nodal_strain','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'Strain1', ',' , 'Strain2' , ',' , 'Strain3', ',' , 'StrainINT', ',' , 'StrainEQV'
(A8,A1,A8,A1,A8,A1,A8,A1,A8,A1,A8)

! Writes the data
*VWRITE,SEQU, ',' ,n_totalstrain(1,1), ',' , n_totalstrain(1,2) , ',' , n_totalstrain(1,3), ',' , n_totalstrain(1,4), ',' , n_totalstrain(1,5)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE";

        private const string PrintFinishSignal = @"/OUTPUT,'FINISH','ems'
/OUTPUT";

    }
}
