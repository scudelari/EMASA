using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Accord.Genetic;
using AccordHelper.FEA.Items;
using AccordHelper.FEA.Results;
using CsvHelper;
using CsvHelper.Configuration;

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
        public AnsysModel(string inModelFolder) : base(inModelFolder, "input_file.dat")
        {
        }

        public override void InitializeSoftware()
        {
            // Cleans-up the model folder
            if (Directory.Exists(ModelFolder)) Directory.Delete(ModelFolder, true);
            Directory.CreateDirectory(ModelFolder);
            
            string jobEaterString = $@"! JOB EATER
! -------------------------------------
! Sets the directory
/CWD,'{ModelFolder}'

:BEGIN

/CLEAR   ! Clears the current problem

! Waits for the start signal
start_exists = 0
end_exists = 0

/WAIT,0.050 ! Waits on the iteration

/INQUIRE,start_exists,EXIST,'start_signal','control'       ! Gets the file exist info
/INQUIRE,end_exists,EXIST,'end_signal','control'           ! Gets the file exist info

*IF,start_exists,EQ,1,THEN ! Start signal received
	/DELETE,'start_signal','control'                       ! Deletes the signal file
	/INPUT,'input_file','dat',,,1                               ! Reads the new iteration file
	
    SAVE,ALL               ! Saves the database
    FINISH                 ! Closes the data

	! Writes a file to signal that the iteration has finished
	/OUTPUT,'iteration_finish','control'
	/OUTPUT
*ENDIF

*IF,end_exists,EQ,0,:BEGIN                                 ! If the end signal file does not exist, jumps to the beginning
/DELETE,'end_signal','control'                             ! Deletes the signal file";
            string jobEaterFileName = Path.Combine(ModelFolder, "Job_Eater.dat");

            File.WriteAllText(jobEaterFileName, jobEaterString);

            _commandLineProcess = new Process();
            _commandLineProcess.StartInfo.FileName = "cmd.exe";
            _commandLineProcess.StartInfo.RedirectStandardInput = true;
            _commandLineProcess.StartInfo.RedirectStandardOutput = true;
            _commandLineProcess.StartInfo.RedirectStandardError = true;
            _commandLineProcess.StartInfo.WorkingDirectory = ModelFolder;
            _commandLineProcess.StartInfo.CreateNoWindow = true;
            _commandLineProcess.StartInfo.UseShellExecute = false;
            _commandLineProcess.Start();

            _commandLineProcess.StandardInput.Write($"CD \"{ModelFolder}\"");
            _commandLineProcess.StandardInput.Flush();

            _commandLineProcess.StandardInput.WriteLine("SET ANSYS201_PRODUCT=ANSYS");
            _commandLineProcess.StandardInput.Flush();

            _commandLineProcess.StandardInput.WriteLine("SET ANS_CONSEC=YES");
            _commandLineProcess.StandardInput.Flush();

            // Launches the Ansys process that will keep consuming the iterations
            string cmdString = $"\"{ansysExeLocation}\" -s noread -b -i \"{jobEaterFileName}\" -o \"{jobEaterFileName + "_out"}\"";

            // Issues the Ansys command
            _commandLineProcess.StandardInput.WriteLine(cmdString);
            _commandLineProcess.StandardInput.Flush();
        }

        public override void ResetSoftwareData()
        {
        }

        public override void CloseApplication()
        {
            // Writes the termination signal file
            string endSignalPath = Path.Combine(ModelFolder, "end_signal.control");
            if (File.Exists(endSignalPath)) File.Delete(endSignalPath);
            File.WriteAllText(endSignalPath, " ");

            _commandLineProcess.StandardInput.Close();
            _commandLineProcess.WaitForExit();

            _commandLineProcess = null;
        }

        public override void WriteModelToSoftware()
        {
            if (Frames.Count == 0) throw new Exception("The model must have defined frames!");
            if (Groups.Count == 0 || Groups.Count(a => a.Value.Restraint != null) == 0) throw new Exception("The model must have defined restraints!");

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("! Model generated using EMASA's Rhino Automator.");
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
            foreach (var feJoint in Joints.OrderBy(a => a.Key))
            {
                sb.AppendLine($"K,{feJoint.Value.Id},{feJoint.Value.Point.X},{feJoint.Value.Point.Y},{feJoint.Value.Point.Z} ! CSharp Joint ID: {feJoint.Key}");
            }

            sb.AppendLine().AppendLine();

            sb.AppendLine("! Adding the ORDERED Lines");
            foreach (var feFrame in Frames.OrderBy(a => a.Key))
            {
                sb.AppendLine($"L,{feFrame.Value.IJoint.Id},{feFrame.Value.JJoint.Id} ! CSharp Line ID: {feFrame.Value.Id}");
            }

            sb.AppendLine().AppendLine();

            sb.AppendLine("! Defining the sections");
            foreach (FeSection feSection in Sections)
            {
                sb.AppendLine($"! BEGIN Define Section {feSection.ToString()}");
                sb.AppendLine(feSection.AnsysSecTypeLine);
                sb.AppendLine(feSection.AnsysSecDataLine);

                sb.AppendLine("LSEL,NONE ! Clearing line selection");
                foreach (var feFrame in Frames.Where(a => a.Value.Section == feSection))
                {
                    sb.AppendLine($"LSEL,A,LINE,,{feFrame.Value.Id}");
                }
                sb.AppendLine($"LATT,{feSection.Material.Id}, ,beam_element_type, , , ,{feSection.Id} ! Sets the #1 Material; #3 ElementType, #7 Section for all selected lines");
            }

            sb.AppendLine().AppendLine();

            sb.AppendLine("! Defining the Groups");
            foreach (var feGroup in Groups)
            {
                sb.AppendLine().AppendLine($"! GroupName: {feGroup.Value.Name}");
                // Selects the joints
                sb.AppendLine("KSEL,NONE ! Clearing KP selection");
                foreach (FeJoint feJoint in feGroup.Value.Joints)
                {
                    sb.AppendLine($"KSEL,A,KP,,{feJoint.Id}");
                }

                // Selects the frames
                sb.AppendLine("LSEL,NONE ! Clearing Line selection");
                foreach (FeFrame feFrame in feGroup.Value.Frames)
                {
                    sb.AppendLine($"LSEL,A,LINE,,{feFrame.Id}");
                }

                // Adds them to the components
                sb.AppendLine($"CM,{feGroup.Value.Name}_J,KP ! The component that has the Joints of Group {feGroup.Value.Name}");
                sb.AppendLine($"CM,{feGroup.Value.Name}_L,LINE ! The component that has the Lines of Group {feGroup.Value.Name}");

                sb.AppendLine($"CMGRP,{feGroup.Value.Name},{feGroup.Value.Name}_J,{feGroup.Value.Name}_L ! Putting the joint and line components into same assembly {feGroup.Value.Name}");
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
            foreach (var feGroup in Groups.Where(a => a.Value.Restraint != null))
            {
                sb.AppendLine("KSEL,NONE ! Clearing KP selection");

                sb.AppendLine($"CMSEL,A,{feGroup.Value.Name},KP ! Selecting the Joints that are in the assembly.");

                if (feGroup.Value.Restraint.IsAll)
                {
                    sb.AppendLine($"DK,ALL,ALL,0 ! Sets all locked for given component");
                }
                else
                {
                    if (feGroup.Value.Restraint.DoF[0])
                    {
                        sb.AppendLine($"DK,ALL,UX,0 ! Sets UX for previously selected joints");
                    }
                    if (feGroup.Value.Restraint.DoF[1])
                    {
                        sb.AppendLine($"DK,ALL,UY,0 ! Sets UY for previously selected joints");
                    }
                    if (feGroup.Value.Restraint.DoF[2])
                    {
                        sb.AppendLine($"DK,ALL,UZ,0 ! Sets UZ for previously selected joints");
                    }
                    if (feGroup.Value.Restraint.DoF[3])
                    {
                        sb.AppendLine($"DK,ALL,ROTX,0 ! Sets ROTX for previously selected joints");
                    }
                    if (feGroup.Value.Restraint.DoF[4])
                    {
                        sb.AppendLine($"DK,ALL,ROTY,0 ! Sets ROTY for previously selected joints");
                    }
                    if (feGroup.Value.Restraint.DoF[5])
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

            // Writes the file
            if (File.Exists(FullFileName)) File.Delete(FullFileName);
            File.WriteAllText(FullFileName, sb.ToString());
        }
        public override void RunAnalysis()
        {
            if (_commandLineProcess == null) throw new InvalidOperationException($"You must first initialize the analysis.");

            if (!File.Exists(FullFileName)) throw new InvalidOperationException($"The Ansys model {FullFileName} could not be found.");

            // Sends a signal to start the analysis
            string startSignalPath = Path.Combine(ModelFolder, "start_signal.control");
            if (File.Exists(startSignalPath)) File.Delete(startSignalPath);
            File.WriteAllText(startSignalPath, " ");

            // Pools for the existence of the finish file
            string finishFile = Path.Combine(ModelFolder, "iteration_finish.control");
            while (true)
            {
                if (File.Exists(finishFile)) break;
                Thread.Sleep(50);
            }
            File.Delete(finishFile);

            #region Fills the Basic Mesh Data from the model
            // Reading the mesh joint coordinates
            {
                string line = null;
                Regex lineRegex = new Regex(@"^(?<NODE>\s+[\-\.\dE]+)(?<X>\s+[\-\.\dE]+)(?<Y>\s+[\-\.\dE]+)(?<Z>\s+[\-\.\dE]+)(?<THXY>\s+[\-\.\dE]+)(?<THYZ>\s+[\-\.\dE]+)(?<THZX>\s+[\-\.\dE]+)\s*$");
                
                if (!File.Exists(Path.Combine(ModelFolder, "nodes_locations.ems"))) throw new IOException($"The Nodal Location file was not created by Ansys!");
                
                using (StreamReader reader = new StreamReader(Path.Combine(ModelFolder, "nodes_locations.ems")))
                {
                    try
                    {
                        while ((line = reader.ReadLine()) != null)
                        {
                            Match m = lineRegex.Match(line);
                            if (!m.Success) continue;

                            int nodeId = int.Parse(m.Groups["NODE"].Value);

                            MeshNodes.Add(nodeId,
                                new FeMeshNode(nodeId,
                                    double.Parse(m.Groups["X"].Value),
                                    double.Parse(m.Groups["Y"].Value),
                                    double.Parse(m.Groups["Z"].Value)));
                        }
                    }
                    catch (Exception)
                    {
                        throw new Exception($"Could not parse the line {line} into a FeMeshNode while reading the results.");
                    } 
                }

            }


            using (StreamReader reader = new StreamReader(Path.Combine(ModelFolder, "line_element_data.ems")))
            {
                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                    var line_element_data_def = new
                    {
                        LINE = default(int),
                        ELEM = default(int),
                        INODE = default(int),
                        JNODE = default(int),
                        KNODE = default(int),
                    };
                    var records = csv.GetRecords(line_element_data_def);

                    // Saves all the elements in the element list for quick queries down the line
                    HashSet<int> addedList = new HashSet<int>();
                    foreach (var r in records)
                    {
                        if (addedList.Add(r.ELEM)) MeshBeamElements.Add(r.ELEM, new FeMeshBeamElement(r.ELEM, MeshNodes[r.INODE], MeshNodes[r.JNODE], MeshNodes[r.KNODE]));
                    }

                    // Links them to the frames
                    foreach (var r in records)
                    {
                        FeFrame frame = Frames[r.LINE];
                        frame.MeshElements.Add(MeshBeamElements[r.ELEM]);
                    }
                }
            } 
            #endregion

        }

        public override void SaveDataAs(string inFilePath)
        {
            throw new NotImplementedException();
        }

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
ETABLE, iFx, SMISC, 1
ETABLE, iMy, SMISC, 2
ETABLE, iMz, SMISC, 3
ETABLE, iTq, SMISC, 4  ! Torsional Moment
ETABLE, iSFz, SMISC, 5  ! Shear Force Z
ETABLE, iSFy, SMISC, 6  ! Shear Force Z

ETABLE, iEx, SMISC, 7  ! Axial Strain
ETABLE, iKy, SMISC, 8  ! Curvature Y
ETABLE, iKz, SMISC, 9  ! Curvature Z
ETABLE, iSEz, SMISC, 11  ! Strain Z
ETABLE, iSEy, SMISC, 12  ! Strain Y

ETABLE, iSDIR, SMISC, 31 ! Axial Direct Stress
ETABLE, iSByT, SMISC, 32 ! Bending stress on the element +Y side of the beam
ETABLE, iSByB, SMISC, 33 ! Bending stress on the element -Y side of the beam
ETABLE, iSBzT, SMISC, 34 ! Bending stress on the element +Z side of the beam
ETABLE, iSBzB, SMISC, 35 ! Bending stress on the element -Z side of the beam

ETABLE, iEPELDIR, SMISC, 41 ! Axial strain at the end
ETABLE, iEPELByT, SMISC, 42 ! Bending strain on the element +Y side of the beam.
ETABLE, iEPELByB, SMISC, 43 ! Bending strain on the element -Y side of the beam.
ETABLE, iEPELBzT, SMISC, 44 ! Bending strain on the element +Z side of the beam.
ETABLE, iEPELBzB, SMISC, 45 ! Bending strain on the element -Z side of the beam.

ETABLE, jFx, SMISC, 14
ETABLE, jMy, SMISC, 15
ETABLE, jMz, SMISC, 16
ETABLE, jTq, SMISC, 17 ! Torsional Moment
ETABLE, jSFz, SMISC, 18  ! Shear Force Z
ETABLE, jSFy, SMISC, 19  ! Shear Force Z
ETABLE, jEx, SMISC, 20  ! Axial Strain
ETABLE, jKy, SMISC, 21  ! Curvature Y
ETABLE, jKz, SMISC, 22  ! Curvature Z
ETABLE, jSEz, SMISC, 24  ! Strain Z
ETABLE, jSEy, SMISC, 25  ! Strain Y

ETABLE, jSDIR, SMISC, 36 ! Axial Direct Stress
ETABLE, jSByT, SMISC, 37 ! Bending stress on the element +Y side of the beam
ETABLE, jSByB, SMISC, 38 ! Bending stress on the element -Y side of the beam
ETABLE, jSBzT, SMISC, 39 ! Bending stress on the element +Z side of the beam
ETABLE, jSBzB, SMISC, 40 ! Bending stress on the element -Z side of the beam

ETABLE, jEPELDIR, SMISC, 46 ! Axial strain at the end
ETABLE, jEPELByT, SMISC, 47 ! Bending strain on the element +Y side of the beam.
ETABLE, jEPELByB, SMISC, 48 ! Bending strain on the element -Y side of the beam.
ETABLE, jEPELBzT, SMISC, 49 ! Bending strain on the element +Z side of the beam.
ETABLE, jEPELBzB, SMISC, 50 ! Bending strain on the element -Z side of the beam.

! Gets the ELEMENTAL data
ETABLE, e_StrEn, SENE

! WRITE: I NODE BASIC FORCE DATA
*DIM,ibasicf,ARRAY,total_element_count,6
*VGET,ibasicf(1,1),ELEM,1,ETAB,iFx
*VGET,ibasicf(1,2),ELEM,1,ETAB,iMy
*VGET,ibasicf(1,3),ELEM,1,ETAB,iMz
*VGET,ibasicf(1,4),ELEM,1,ETAB,iTq
*VGET,ibasicf(1,5),ELEM,1,ETAB,iSFz
*VGET,ibasicf(1,6),ELEM,1,ETAB,iSFy

! Writes the data to file
*CFOPEN,'result_element_inode_basic_forces','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'iFx', ',' , 'iMy' , ',' , 'iMz', ',' , 'iTq', ',' , 'iSFz', ',' , 'iSFy'
(A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,ibasicf(1,1), ',' , ibasicf(1,2) , ',' , ibasicf(1,3), ',' , ibasicf(1,4), ',' , ibasicf(1,5), ',' , ibasicf(1,6)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE


! WRITE: I NODE BASIC STRAIN DATA
*DIM,ibasics,ARRAY,total_element_count,5
*VGET,ibasics(1,1),ELEM,1,ETAB,iEx
*VGET,ibasics(1,2),ELEM,1,ETAB,iKy
*VGET,ibasics(1,3),ELEM,1,ETAB,iKz
*VGET,ibasics(1,4),ELEM,1,ETAB,iSEz
*VGET,ibasics(1,5),ELEM,1,ETAB,iSEy

! Writes the data to file
*CFOPEN,'result_element_inode_basic_strains','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'iEx', ',' , 'iKy' , ',' , 'iKz', ',' , 'iSEz', ',' , 'iSEy'
(A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,ibasics(1,1), ',' , ibasics(1,2) , ',' , ibasics(1,3), ',' , ibasics(1,4), ',' , ibasics(1,5)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE


! WRITE: I NODE BASIC DIRECTIONAL STRESS
*DIM,ibasic_dirstress,ARRAY,total_element_count,5
*VGET,ibasic_dirstress(1,1),ELEM,1,ETAB,iSDIR
*VGET,ibasic_dirstress(1,2),ELEM,1,ETAB,iSByT
*VGET,ibasic_dirstress(1,3),ELEM,1,ETAB,iSByB
*VGET,ibasic_dirstress(1,4),ELEM,1,ETAB,iSBzT
*VGET,ibasic_dirstress(1,5),ELEM,1,ETAB,iSBzB

! Writes the data to file
*CFOPEN,'result_element_inode_basic_dirstress','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'iSDIR', ',' , 'iSByT' , ',' , 'iSByB', ',' , 'iSBzT', ',' , 'iSBzB'
(A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,ibasic_dirstress(1,1), ',' , ibasic_dirstress(1,2) , ',' , ibasic_dirstress(1,3), ',' , ibasic_dirstress(1,4), ',' , ibasic_dirstress(1,5)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE


! WRITE: I NODE BASIC DIRECTIONAL STRAIN
*DIM,ibasic_dirstrain,ARRAY,total_element_count,5
*VGET,ibasic_dirstrain(1,1),ELEM,1,ETAB,iEPELDIR
*VGET,ibasic_dirstrain(1,2),ELEM,1,ETAB,iEPELByT
*VGET,ibasic_dirstrain(1,3),ELEM,1,ETAB,iEPELByB
*VGET,ibasic_dirstrain(1,4),ELEM,1,ETAB,iEPELBzT
*VGET,ibasic_dirstrain(1,5),ELEM,1,ETAB,iEPELBzB

! Writes the data to file
*CFOPEN,'result_element_inode_basic_dirstrain','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'iEPELDIR', ',' , 'iEPELByT' , ',' , 'iEPELByB', ',' , 'iEPELBzT', ',' , 'iEPELBzB'
(A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,ibasic_dirstrain(1,1), ',' , ibasic_dirstrain(1,2) , ',' , ibasic_dirstrain(1,3), ',' , ibasic_dirstrain(1,4), ',' , ibasic_dirstrain(1,5)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE






! WRITE: J NODE BASIC FORCE DATA
*DIM,jbasicf,ARRAY,total_element_count,6
*VGET,jbasicf(1,1),ELEM,1,ETAB,jFx
*VGET,jbasicf(1,2),ELEM,1,ETAB,jMy
*VGET,jbasicf(1,3),ELEM,1,ETAB,jMz
*VGET,jbasicf(1,4),ELEM,1,ETAB,jTq
*VGET,jbasicf(1,5),ELEM,1,ETAB,jSFz
*VGET,jbasicf(1,6),ELEM,1,ETAB,jSFy

! Writes the data to file
*CFOPEN,'result_element_jnode_basic_forces','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'jFx', ',' , 'jMy' , ',' , 'jMz', ',' , 'jTq', ',' , 'jSFz', ',' , 'jSFy'
(A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,jbasicf(1,1), ',' , jbasicf(1,2) , ',' , jbasicf(1,3), ',' , jbasicf(1,4), ',' , jbasicf(1,5), ',' , jbasicf(1,6)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE


! WRITE: J NODE BASIC STRAIN DATA
*DIM,jbasics,ARRAY,total_element_count,5
*VGET,jbasics(1,1),ELEM,1,ETAB,jEx
*VGET,jbasics(1,2),ELEM,1,ETAB,jKy
*VGET,jbasics(1,3),ELEM,1,ETAB,jKz
*VGET,jbasics(1,4),ELEM,1,ETAB,jSEz
*VGET,jbasics(1,5),ELEM,1,ETAB,jSEy

! Writes the data to file
*CFOPEN,'result_element_jnode_basic_strains','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'jEx', ',' , 'jKy' , ',' , 'jKz', ',' , 'jSEz', ',' , 'jSEy'
(A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,jbasics(1,1), ',' , jbasics(1,2) , ',' , jbasics(1,3), ',' , jbasics(1,4), ',' , jbasics(1,5)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE


! WRITE: J NODE BASIC DIRECTIONAL STRESS
*DIM,jbasic_dirstress,ARRAY,total_element_count,5
*VGET,jbasic_dirstress(1,1),ELEM,1,ETAB,jSDIR
*VGET,jbasic_dirstress(1,2),ELEM,1,ETAB,jSByT
*VGET,jbasic_dirstress(1,3),ELEM,1,ETAB,jSByB
*VGET,jbasic_dirstress(1,4),ELEM,1,ETAB,jSBzT
*VGET,jbasic_dirstress(1,5),ELEM,1,ETAB,jSBzB

! Writes the data to file
*CFOPEN,'result_element_jnode_basic_dirstress','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'jSDIR', ',' , 'jSByT' , ',' , 'jSByB', ',' , 'jSBzT', ',' , 'jSBzB'
(A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,jbasic_dirstress(1,1), ',' , jbasic_dirstress(1,2) , ',' , jbasic_dirstress(1,3), ',' , jbasic_dirstress(1,4), ',' , jbasic_dirstress(1,5)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE


! WRITE: J NODE BASIC DIRECTIONAL STRAIN
*DIM,jbasic_dirstrain,ARRAY,total_element_count,5
*VGET,jbasic_dirstrain(1,1),ELEM,1,ETAB,jEPELDIR
*VGET,jbasic_dirstrain(1,2),ELEM,1,ETAB,jEPELByT
*VGET,jbasic_dirstrain(1,3),ELEM,1,ETAB,jEPELByB
*VGET,jbasic_dirstrain(1,4),ELEM,1,ETAB,jEPELBzT
*VGET,jbasic_dirstrain(1,5),ELEM,1,ETAB,jEPELBzB

! Writes the data to file
*CFOPEN,'result_element_jnode_basic_dirstrain','ems' ! Opens the file

! Writes the header
*VWRITE,'ELEMENT', ',' ,'jEPELDIR', ',' , 'jEPELByT' , ',' , 'jEPELByB', ',' , 'jEPELBzT', ',' , 'jEPELBzB'
(A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12)

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
(A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,elem_strain_enery(1,1)
%I%C%30.6G

*CFCLOSE";
        private const string PrintNodalResults = @"! SELECTS ALL NODES
NSEL,ALL
*GET,total_node_count,NODE,0,COUNT ! Gets count of all elements

*DIM,n_dof,ARRAY,total_node_count,6
*VGET,n_dof(1,1),NODE,1,U,X  ! Displacement X
*VGET,n_dof(1,2),NODE,1,U,Y  ! Displacement Y
*VGET,n_dof(1,3),NODE,1,U,Z  ! Displacement Z
*VGET,n_dof(1,4),NODE,1,ROT,X  ! Rotation X
*VGET,n_dof(1,5),NODE,1,ROT,Y  ! Rotation Y
*VGET,n_dof(1,6),NODE,1,ROT,Z  ! Rotation Z


! Writes the data to file
*CFOPEN,'result_nodal_displacements','ems' ! Opens the file

! Writes the header
*VWRITE,'NODE', ',' ,'UX', ',' , 'UY' , ',' , 'UZ', ',' , 'RX', ',' , 'RY', ',' , 'RZ'
(A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12,A1,A12)

! Writes the data
*VWRITE,SEQU, ',' ,n_dof(1,1), ',' , n_dof(1,2) , ',' , n_dof(1,3), ',' , n_dof(1,4), ',' , n_dof(1,5), ',' , n_dof(1,6)
%I%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G%C%30.6G

*CFCLOSE

! Fixing the formats
/HEADER,OFF,OFF,OFF,OFF,ON,OFF
/PAGE,,,-100000000,240,0
/FORMAT,10,G,20,6,,,

! Printing - Nodal Stresses
ALLSEL,ALL
/OUTPUT,'nodes_stresses','ems'
PRESOL,S,PRIN
/OUTPUT

! Printing - Nodal Strains
ALLSEL,ALL
/OUTPUT,'nodes_strains','ems'
PRESOL,EPTT,PRIN
/OUTPUT";
        private const string PrintNodalBasicData = @"
! Fixing the formats
/HEADER,OFF,OFF,OFF,OFF,ON,OFF
/PAGE,,,-100000000,240,0
/FORMAT,10,G,20,6,,,


! Printing - Nodal data
NSEL,ALL
/OUTPUT,'nodes_locations','ems'
NLIST
/OUTPUT

/OUTPUT,'nodes_reactions','ems'
PRRSOL
/OUTPUT";

        public override void GetResult_FillElementStrainEnergy()
        {
            string resultFile = Path.Combine(ModelFolder, "result_element_senergy.ems");
            if (!File.Exists(resultFile)) throw new Exception($"Could not find the file {resultFile} that contains the elemental strain energy data.");

            using (StreamReader reader = new StreamReader(resultFile))
            {
                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                    var anonymousTypeDef = new
                    {
                        ELEMENT = default(int),
                        e_StrEn = default(double),
                    };
                    var records = csv.GetRecords(anonymousTypeDef);

                    foreach (var r in records)
                    {
                        FeMeshBeamElement element = MeshBeamElements[r.ELEMENT];
                        element.ElementStrainEnergy = new FeResult_ElementStrainEnergy(r.e_StrEn);
                    }
                }
            }

        }

        public override void GetResult_FillNodalReactions()
        {
            string nodalResultFilename = Path.Combine(ModelFolder, "nodes_reactions.ems");
            if (!File.Exists(nodalResultFilename)) throw new Exception($"Could not find the file {nodalResultFilename} that contains the nodal reaction data.");

            using (StreamReader reader = new StreamReader(nodalResultFilename))
            {
                try
                {
                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        int? nodeId = null;
                        FeResult_NodalReactions react = new FeResult_NodalReactions();
                        try
                        {
                            int start = 1;
                            nodeId = int.Parse(line.Substring(start, 10));
                            start += 10;

                            // Fx
                            string dataChunk = line.Substring(start, 20);
                            if (!string.IsNullOrWhiteSpace(dataChunk)) react.FX = double.Parse(dataChunk);
                            start += 20;

                            // Fy
                            dataChunk = line.Substring(start, 20);
                            if (!string.IsNullOrWhiteSpace(dataChunk)) react.FY = double.Parse(dataChunk);
                            start += 20;

                            // Fz
                            dataChunk = line.Substring(start, 20);
                            if (!string.IsNullOrWhiteSpace(dataChunk)) react.FZ = double.Parse(dataChunk);
                            start += 20;


                            dataChunk = line.Substring(start, 20);
                            if (!string.IsNullOrWhiteSpace(dataChunk)) react.MX = double.Parse(dataChunk);
                            start += 20;


                            dataChunk = line.Substring(start, 20);
                            if (!string.IsNullOrWhiteSpace(dataChunk)) react.MY = double.Parse(dataChunk);
                            start += 20;

                            dataChunk = line.Substring(start, 20);
                            if (!string.IsNullOrWhiteSpace(dataChunk)) react.MZ = double.Parse(dataChunk);

                        }
                        catch
                        {
                            if (nodeId.HasValue && react.ContainsAnyValue)
                            {
                                MeshNodes[nodeId.Value].Result_NodalReactions = react;
                            }
                            continue;
                        }
                    }
                }
                catch (Exception)
                {
                    throw new Exception($"Could not parse file {nodalResultFilename} to read the Nodal Results.");
                }
            }

        }

        public override void GetResult_FillElementNodalBendingStrain()
        {
            string iNodeResultFileName = Path.Combine(ModelFolder, "result_element_inode_basic_dirstrain.ems");
            if (!File.Exists(iNodeResultFileName)) throw new Exception($"Could not find the file {iNodeResultFileName} that contains the I nodal bending strain data.");

            string jNodeResultFileName = Path.Combine(ModelFolder, "result_element_jnode_basic_dirstrain.ems");
            if (!File.Exists(jNodeResultFileName)) throw new Exception($"Could not find the file {jNodeResultFileName} that contains the J nodal bending strain data.");

            using (StreamReader reader = new StreamReader(iNodeResultFileName))
            {
                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                    var anonymousTypeDef = new
                        {
                        ELEMENT = default(int),
                        iEPELDIR = default(double),
                        iEPELByT = default(double),
                        iEPELByB = default(double),
                        iEPELBzT = default(double),
                        iEPELBzB = default(double)
                        };
                    var records = csv.GetRecords(anonymousTypeDef);

                    foreach (var r in records)
                    {
                        FeMeshBeamElement element = MeshBeamElements[r.ELEMENT];
                        element.INode.Result_ElementNodalBendingStrains = new FeResult_ElementNodalBendingStrain()
                            {
                            EPELDIR = r.iEPELDIR,
                            EPELByT = r.iEPELByT,
                            EPELByB = r.iEPELByB,
                            EPELBzT = r.iEPELBzT,
                            EPELBzB = r.iEPELBzB
                            };
                    }
                }
            }

            using (StreamReader reader = new StreamReader(jNodeResultFileName))
            {
                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                    var anonymousTypeDef = new
                        {
                        ELEMENT = default(int),
                        jEPELDIR = default(double),
                        jEPELByT = default(double),
                        jEPELByB = default(double),
                        jEPELBzT = default(double),
                        jEPELBzB = default(double)
                        };
                    var records = csv.GetRecords(anonymousTypeDef);

                    foreach (var r in records)
                    {
                        FeMeshBeamElement element = MeshBeamElements[r.ELEMENT];
                        element.JNode.Result_ElementNodalBendingStrains = new FeResult_ElementNodalBendingStrain()
                            {
                            EPELDIR = r.jEPELDIR,
                            EPELByT = r.jEPELByT,
                            EPELByB = r.jEPELByB,
                            EPELBzT = r.jEPELBzT,
                            EPELBzB = r.jEPELBzB
                            };
                    }
                }
            }
        }
        public override void GetResult_FillElementNodalStress()
        {
            string iNodeResultFileName = Path.Combine(ModelFolder, "result_element_inode_basic_dirstress.ems");
            if (!File.Exists(iNodeResultFileName)) throw new Exception($"Could not find the file {iNodeResultFileName} that contains the I nodal stress data.");

            string jNodeResultFileName = Path.Combine(ModelFolder, "result_element_jnode_basic_dirstress.ems");
            if (!File.Exists(jNodeResultFileName)) throw new Exception($"Could not find the file {jNodeResultFileName} that contains the J nodal stress data.");

            using (StreamReader reader = new StreamReader(iNodeResultFileName))
            {
                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                    var anonymousTypeDef = new
                    {
                        ELEMENT = default(int),
                        iSDIR = default(double),
                        iSByT = default(double),
                        iSByB = default(double),
                        iSBzT = default(double),
                        iSBzB = default(double)
                    };
                    var records = csv.GetRecords(anonymousTypeDef);

                    foreach (var r in records)
                    {
                        FeMeshBeamElement element = MeshBeamElements[r.ELEMENT];
                        element.INode.Result_ElementNodalStress = new FeResult_ElementNodalStress()
                        {
                            SDIR = r.iSDIR,
                            SByB = r.iSByB,
                            SByT = r.iSByT,
                            SBzB = r.iSBzB,
                            SBzT = r.iSBzT
                        };
                    }
                }
            }

            using (StreamReader reader = new StreamReader(jNodeResultFileName))
            {
                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                    var anonymousTypeDef = new
                        {
                        ELEMENT = default(int),
                        jSDIR = default(double),
                        jSByT = default(double),
                        jSByB = default(double),
                        jSBzT = default(double),
                        jSBzB = default(double)
                        };
                    var records = csv.GetRecords(anonymousTypeDef);

                    foreach (var r in records)
                    {
                        FeMeshBeamElement element = MeshBeamElements[r.ELEMENT];
                        element.JNode.Result_ElementNodalStress = new FeResult_ElementNodalStress()
                            {
                            SDIR = r.jSDIR,
                            SByB = r.jSByB,
                            SByT = r.jSByT,
                            SBzB = r.jSBzB,
                            SBzT = r.jSBzT
                            };
                    }
                }
            }
        }
        public override void GetResult_FillElementNodalForces()
        {
            string iNodeResultFileName = Path.Combine(ModelFolder, "result_element_inode_basic_forces.ems");
            if (!File.Exists(iNodeResultFileName)) throw new Exception($"Could not find the file {iNodeResultFileName} that contains the I nodal force data.");

            string jNodeResultFileName = Path.Combine(ModelFolder, "result_element_jnode_basic_forces.ems");
            if (!File.Exists(jNodeResultFileName)) throw new Exception($"Could not find the file {jNodeResultFileName} that contains the J nodal force data.");

            using (StreamReader reader = new StreamReader(iNodeResultFileName))
            {
                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                    var anonymousTypeDef = new
                    {
                        ELEMENT = default(int),
                        iFx = default(double),
                        iMy = default(double),
                        iMz = default(double),
                        iTq = default(double),
                        iSFz = default(double),
                        iSFy = default(double)
                    };
                    var records = csv.GetRecords(anonymousTypeDef);

                    foreach (var r in records)
                    {
                        FeMeshBeamElement element = MeshBeamElements[r.ELEMENT];
                        element.INode.Result_ElementNodalForces = new FeResult_ElementNodalForces()
                        {
                            Fx = r.iFx,
                            My = r.iMy,
                            Mz = r.iMz,
                            Tq = r.iTq,
                            SFy = r.iSFy,
                            SFz = r.iSFz
                        };
                    }
                }
            }

            using (StreamReader reader = new StreamReader(jNodeResultFileName))
            {
                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                    var anonymousTypeDef = new
                    {
                        ELEMENT = default(int),
                        jFx = default(double),
                        jMy = default(double),
                        jMz = default(double),
                        jTq = default(double),
                        jSFz = default(double),
                        jSFy = default(double)
                    };
                    var records = csv.GetRecords(anonymousTypeDef);

                    foreach (var r in records)
                    {
                        FeMeshBeamElement element = MeshBeamElements[r.ELEMENT];
                        element.JNode.Result_ElementNodalForces = new FeResult_ElementNodalForces()
                        {
                            Fx = r.jFx,
                            My = r.jMy,
                            Mz = r.jMz,
                            Tq = r.jTq,
                            SFy = r.jSFy,
                            SFz = r.jSFz
                        };
                    }
                }
            }

        }
        public override void GetResult_FillElementNodalStrains()
        {
            string iNodeResultFileName = Path.Combine(ModelFolder, "result_element_inode_basic_strains.ems");
            if (!File.Exists(iNodeResultFileName)) throw new Exception($"Could not find the file {iNodeResultFileName} that contains the I nodal strain data.");

            string jNodeResultFileName = Path.Combine(ModelFolder, "result_element_jnode_basic_strains.ems");
            if (!File.Exists(jNodeResultFileName)) throw new Exception($"Could not find the file {jNodeResultFileName} that contains the J nodal strain data.");

            using (StreamReader reader = new StreamReader(iNodeResultFileName))
            {
                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                    var anonymousTypeDef = new
                    {
                        ELEMENT = default(int),
                        iEx = default(double),
                        iKy = default(double),
                        iKz = default(double),
                        iSEz = default(double),
                        iSEy = default(double),
                    };
                    var records = csv.GetRecords(anonymousTypeDef);

                    foreach (var r in records)
                    {
                        FeMeshBeamElement element = MeshBeamElements[r.ELEMENT];
                        element.INode.Result_ElementNodalStrains = new FeResult_ElementNodalStrain()
                        {
                            Ex = r.iEx,
                            Ky = r.iKy,
                            Kz = r.iKz,
                            SEy = r.iSEy,
                            SEz = r.iSEz,
                        };
                    }
                }
            }

            using (StreamReader reader = new StreamReader(jNodeResultFileName))
            {
                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                    var anonymousTypeDef = new
                        {
                        ELEMENT = default(int),
                        jEx = default(double),
                        jKy = default(double),
                        jKz = default(double),
                        jSEz = default(double),
                        jSEy = default(double),
                        };
                    var records = csv.GetRecords(anonymousTypeDef);

                    foreach (var r in records)
                    {
                        FeMeshBeamElement element = MeshBeamElements[r.ELEMENT];
                        element.JNode.Result_ElementNodalStrains = new FeResult_ElementNodalStrain()
                            {
                            Ex = r.jEx,
                            Ky = r.jKy,
                            Kz = r.jKz,
                            SEy = r.jSEy,
                            SEz = r.jSEz,
                            };
                    }
                }
            }
        }

        public override void GetResult_FillNodalDisplacements()
        {
            string nodeResultFileName = Path.Combine(ModelFolder, "result_nodal_displacements.ems");
            if (!File.Exists(nodeResultFileName)) throw new Exception($"Could not find the file {nodeResultFileName} that contains the nodal displacement data.");

            using (StreamReader reader = new StreamReader(nodeResultFileName))
            {
                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Configuration.TrimOptions = TrimOptions.Trim;

                    var anonymousTypeDef = new
                    {
                        NODE = default(int),
                        UX = default(double),
                        UY = default(double),
                        UZ = default(double),
                        RX = default(double),
                        RY = default(double),
                        RZ = default(double)
                    };
                    var records = csv.GetRecords(anonymousTypeDef);

                    foreach (var r in records)
                    {
                        FeMeshNode node = MeshNodes[r.NODE];
                        node.Result_NodalDisplacements = new FeResult_NodalDisplacements()
                        {
                            UX = r.UX,
                            UY = r.UY,
                            UZ = r.UZ,
                            RX = r.RX,
                            RY = r.RY,
                            RZ = r.RZ
                        };
                    }
                }
            }
        }
        public override void GetResult_FillNodalSectionPointStressStrain()
        {
            string stressResultFile = Path.Combine(ModelFolder, "nodes_stresses.ems");
            if (!File.Exists(stressResultFile)) throw new Exception($"Could not find the file {stressResultFile} that contains the nodal stresses per section point data.");

            string strainResultFile = Path.Combine(ModelFolder, "nodes_strains.ems");
            if (!File.Exists(strainResultFile)) throw new Exception($"Could not find the file {strainResultFile} that contains the nodal strains per section point data.");

            Regex dataRegex = new Regex(@"^(?<SECNODE>\s+[\d]+)(?<D1>\s+[\-\.\dE]+)(?<D2>\s+[\-\.\dE]+)(?<D3>\s+[\-\.\dE]+)(?<D4>\s+[\-\.\dE]+)(?<D5>\s+[\-\.\dE]+)");
            Regex elementIdRegex = new Regex(@"^\s*ELEMENT\s*=\s*(?<ID>\d*)\s*SECTION");
            Regex nodeIdRegex = new Regex(@"^\s*ELEMENT NODE =\s*(?<ID>\d*)");

            using (StreamReader reader = new StreamReader(stressResultFile))
            {
                try
                {
                    string line = null;
                    int currentElementId = -1;
                    int currentNodeId = -1;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Match elementIdMatch = elementIdRegex.Match(line);
                        if (elementIdMatch.Success)
                        {
                            currentElementId = int.Parse(elementIdMatch.Groups["ID"].Value);
                            continue;
                        }

                        Match nodeIdMatch = nodeIdRegex.Match(line);
                        if (nodeIdMatch.Success)
                        {
                            currentNodeId = int.Parse(nodeIdMatch.Groups["ID"].Value);
                            continue;
                        }

                        Match dataMatch = dataRegex.Match(line);
                        if (dataMatch.Success)
                        {
                            int secNodeId = int.Parse(dataMatch.Groups["SECNODE"].Value);
                            double d1 = double.Parse(dataMatch.Groups["D1"].Value);
                            double d2 = double.Parse(dataMatch.Groups["D2"].Value);
                            double d3 = double.Parse(dataMatch.Groups["D3"].Value);
                            double d4 = double.Parse(dataMatch.Groups["D4"].Value);
                            double d5 = double.Parse(dataMatch.Groups["D5"].Value);

                            FeMeshNode targetNode = MeshBeamElements[currentElementId].GetNodeById(currentNodeId);

                            // The section node data was already created
                            if (targetNode.Result_SectionNodes.ContainsKey(secNodeId))
                            {
                                FeResult_SectionNode secNode = targetNode.Result_SectionNodes[secNodeId];

                                // Saving the values
                                if (!secNode.S1.HasValue) secNode.S1 = d1;
                                else if (secNode.S1.Value != d1) throw new Exception($"The section point stress <Principal 1> values do not match! NodeId: {targetNode.Id}, Section Point: {secNode.SectionNodeId}, {d1} != {secNode.S1}");

                                if (!secNode.S2.HasValue) secNode.S2 = d2;
                                else if (secNode.S2.Value != d2) throw new Exception($"The section point stress <Principal 2> values do not match! NodeId: {targetNode.Id}, Section Point: {secNode.SectionNodeId}, {d2} != {secNode.S2}");

                                if (!secNode.S3.HasValue) secNode.S3 = d3;
                                else if (secNode.S3.Value != d3) throw new Exception($"The section point stress <Principal 3> values do not match! NodeId: {targetNode.Id}, Section Point: {secNode.SectionNodeId}, {d3} != {secNode.S3}");

                                if (!secNode.SINT.HasValue) secNode.SINT = d4;
                                else if (secNode.SINT.Value != d4) throw new Exception($"The section point stress <Intensity> values do not match! NodeId: {targetNode.Id}, Section Point: {secNode.SectionNodeId}, {d4} != {secNode.SINT}");

                                if (!secNode.SEQV.HasValue) secNode.SEQV = d5;
                                else if (secNode.SEQV.Value != d5) throw new Exception($"The section point stress <Equivalent> values do not match! NodeId: {targetNode.Id}, Section Point: {secNode.SectionNodeId}, {d5} != {secNode.SEQV}");
                            }
                            else
                            {
                                targetNode.Result_SectionNodes.Add(secNodeId, new FeResult_SectionNode()
                                    {
                                    SectionNodeId = secNodeId,
                                    S1 = d1,
                                    S2 = d2,
                                    S3 = d3,
                                    SINT = d4,
                                    SEQV = d5
                                });
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    throw new Exception($"Could not parse file {stressResultFile} to read the Nodal Stress Results by Section Point.");
                }
            }

            using (StreamReader reader = new StreamReader(strainResultFile))
            {
                try
                {
                    string line = null;
                    int currentElementId = -1;
                    int currentNodeId = -1;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Match elementIdMatch = elementIdRegex.Match(line);
                        if (elementIdMatch.Success)
                        {
                            currentElementId = int.Parse(elementIdMatch.Groups["ID"].Value);
                            continue;
                        }

                        Match nodeIdMatch = nodeIdRegex.Match(line);
                        if (nodeIdMatch.Success)
                        {
                            currentNodeId = int.Parse(nodeIdMatch.Groups["ID"].Value);
                            continue;
                        }

                        Match dataMatch = dataRegex.Match(line);
                        if (dataMatch.Success)
                        {
                            int secNodeId = int.Parse(dataMatch.Groups["SECNODE"].Value);
                            double d1 = double.Parse(dataMatch.Groups["D1"].Value);
                            double d2 = double.Parse(dataMatch.Groups["D2"].Value);
                            double d3 = double.Parse(dataMatch.Groups["D3"].Value);
                            double d4 = double.Parse(dataMatch.Groups["D4"].Value);
                            double d5 = double.Parse(dataMatch.Groups["D5"].Value);

                            FeMeshNode targetNode = MeshBeamElements[currentElementId].GetNodeById(currentNodeId);

                            // The section node data was already created
                            if (targetNode.Result_SectionNodes.ContainsKey(secNodeId))
                            {
                                FeResult_SectionNode secNode = targetNode.Result_SectionNodes[secNodeId];

                                // Saving the values
                                if (!secNode.EPTT1.HasValue) secNode.EPTT1 = d1;
                                else if (secNode.EPTT1.Value != d1) throw new Exception($"The section point strain <Principal 1> values do not match! NodeId: {targetNode.Id}, Section Point: {secNode.SectionNodeId}, {d1} != {secNode.EPTT1}");

                                if (!secNode.EPTT2.HasValue) secNode.EPTT2 = d2;
                                else if (secNode.EPTT2.Value != d2) throw new Exception($"The section point strain <Principal 2> values do not match! NodeId: {targetNode.Id}, Section Point: {secNode.SectionNodeId}, {d2} != {secNode.EPTT2}");

                                if (!secNode.EPTT3.HasValue) secNode.EPTT3 = d3;
                                else if (secNode.EPTT3.Value != d3) throw new Exception($"The section point strain <Principal 3> values do not match! NodeId: {targetNode.Id}, Section Point: {secNode.SectionNodeId}, {d3} != {secNode.EPTT3}");

                                if (!secNode.EPTTINT.HasValue) secNode.EPTTINT = d4;
                                else if (secNode.EPTTINT.Value != d4) throw new Exception($"The section point strain <Intensity> values do not match! NodeId: {targetNode.Id}, Section Point: {secNode.SectionNodeId}, {d4} != {secNode.EPTTINT}");

                                if (!secNode.EPTTEQV.HasValue) secNode.EPTTEQV = d5;
                                else if (secNode.EPTTEQV.Value != d5) throw new Exception($"The section point strain <Equivalent> values do not match! NodeId: {targetNode.Id}, Section Point: {secNode.SectionNodeId}, {d5} != {secNode.EPTTEQV}");
                            }
                            else
                            {
                                targetNode.Result_SectionNodes.Add(secNodeId, new FeResult_SectionNode()
                                {
                                    SectionNodeId = secNodeId,
                                    S1 = d1,
                                    S2 = d2,
                                    S3 = d3,
                                    SINT = d4,
                                    SEQV = d5
                                });
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    throw new Exception($"Could not parse file {stressResultFile} to read the Nodal Strain Results by Section Point.");
                }
            }
        }
    }
}
