! Model generated using EMASA's Rhino Automator.
    
! Changes the Working Directory 
/CWD,'C:\Users\EngRafaelSMacedo\Desktop\RhinoTester\CatenaryArch.gh_data\FeModel\Ansys' 
    
/title, 'EMSRhinoAuto'  
/UIS,MSGPOP,3 ! Sets the pop-up messages to appear only if they are errors  
/PREP7  
    
    
! Adding the Element Type   
ET,1,BEAM189
beam_element_type = 1   
    
    
! Adding Material Definitions   
MAT,1 ! Material Name S355  
MP,EX,1,210000000000 ! Setting Youngs Modulus   
MP,PRXY,1,0.3 ! Setting Poissons Ratio  
MP,DENS,1,7850 ! Setting Density
    
    
! Adding the ORDERED Joints 
K,1,-30,0,0 ! CSharp Joint ID: 1
K,2,-27.648,0,1.846 ! CSharp Joint ID: 2
K,3,-25.21,0,3.576 ! CSharp Joint ID: 3 
K,4,-22.687,0,5.18 ! CSharp Joint ID: 4 
K,5,-20.079,0,6.641 ! CSharp Joint ID: 5
K,6,-17.389,0,7.948 ! CSharp Joint ID: 6
K,7,-14.624,0,9.084 ! CSharp Joint ID: 7
K,8,-11.79,0,10.037 ! CSharp Joint ID: 8
K,9,-8.898,0,10.794 ! CSharp Joint ID: 9
K,10,-5.959,0,11.343 ! CSharp Joint ID: 10  
K,11,-2.988,0,11.676 ! CSharp Joint ID: 11  
K,12,0,0,11.787 ! CSharp Joint ID: 12   
K,13,2.988,0,11.676 ! CSharp Joint ID: 13   
K,14,5.959,0,11.343 ! CSharp Joint ID: 14   
K,15,8.898,0,10.794 ! CSharp Joint ID: 15   
K,16,11.79,0,10.037 ! CSharp Joint ID: 16   
K,17,14.624,0,9.084 ! CSharp Joint ID: 17   
K,18,17.389,0,7.948 ! CSharp Joint ID: 18   
K,19,20.079,0,6.641 ! CSharp Joint ID: 19   
K,20,22.687,0,5.18 ! CSharp Joint ID: 20
K,21,25.21,0,3.576 ! CSharp Joint ID: 21
K,22,27.648,0,1.846 ! CSharp Joint ID: 22   
K,23,30,0,0 ! CSharp Joint ID: 23   
    
    
! Adding the ORDERED Lines  
L,1,2 ! CSharp Line ID: 1   
L,2,3 ! CSharp Line ID: 2   
L,3,4 ! CSharp Line ID: 3   
L,4,5 ! CSharp Line ID: 4   
L,5,6 ! CSharp Line ID: 5   
L,6,7 ! CSharp Line ID: 6   
L,7,8 ! CSharp Line ID: 7   
L,8,9 ! CSharp Line ID: 8   
L,9,10 ! CSharp Line ID: 9  
L,10,11 ! CSharp Line ID: 10
L,11,12 ! CSharp Line ID: 11
L,12,13 ! CSharp Line ID: 12
L,13,14 ! CSharp Line ID: 13
L,14,15 ! CSharp Line ID: 14
L,15,16 ! CSharp Line ID: 15
L,16,17 ! CSharp Line ID: 16
L,17,18 ! CSharp Line ID: 17
L,18,19 ! CSharp Line ID: 18
L,19,20 ! CSharp Line ID: 19
L,20,21 ! CSharp Line ID: 20
L,21,22 ! CSharp Line ID: 21
L,22,23 ! CSharp Line ID: 22
    
    
! Defining the sections 
! BEGIN Define Section 327:FeSectionPipe:[<OuterDiameter:0.0761><Thickness:0.0025>] 
SECTYPE,327,BEAM,CTUBE,P0x0,0 ! Type is Hollow Round Pipe   
SECDATA,0.035550,0.038050 ! Setting the definitions for the previous SECTYPE command, which are #1: Inner Diameter #2 Outer Diameter
LSEL,NONE ! Clearing line selection 
LSEL,A,LINE,,1  
LSEL,A,LINE,,2  
LSEL,A,LINE,,3  
LSEL,A,LINE,,4  
LSEL,A,LINE,,5  
LSEL,A,LINE,,6  
LSEL,A,LINE,,7  
LSEL,A,LINE,,8  
LSEL,A,LINE,,9  
LSEL,A,LINE,,10 
LSEL,A,LINE,,11 
LSEL,A,LINE,,12 
LSEL,A,LINE,,13 
LSEL,A,LINE,,14 
LSEL,A,LINE,,15 
LSEL,A,LINE,,16 
LSEL,A,LINE,,17 
LSEL,A,LINE,,18 
LSEL,A,LINE,,19 
LSEL,A,LINE,,20 
LSEL,A,LINE,,21 
LSEL,A,LINE,,22 
LATT,1, ,beam_element_type, , , ,327 ! Sets the #1 Material; #3 ElementType, #7 Section for all selected lines  
    
    
! Defining the Groups   
    
! GroupName: Arch   
KSEL,NONE ! Clearing KP selection   
LSEL,NONE ! Clearing Line selection 
LSEL,A,LINE,,1  
LSEL,A,LINE,,2  
LSEL,A,LINE,,3  
LSEL,A,LINE,,4  
LSEL,A,LINE,,5  
LSEL,A,LINE,,6  
LSEL,A,LINE,,7  
LSEL,A,LINE,,8  
LSEL,A,LINE,,9  
LSEL,A,LINE,,10 
LSEL,A,LINE,,11 
LSEL,A,LINE,,12 
LSEL,A,LINE,,13 
LSEL,A,LINE,,14 
LSEL,A,LINE,,15 
LSEL,A,LINE,,16 
LSEL,A,LINE,,17 
LSEL,A,LINE,,18 
LSEL,A,LINE,,19 
LSEL,A,LINE,,20 
LSEL,A,LINE,,21 
LSEL,A,LINE,,22 
CM,Arch_J,KP ! The component that has the Joints of Group Arch  
CM,Arch_L,LINE ! The component that has the Lines of Group Arch 
CMGRP,Arch,Arch_J,Arch_L ! Putting the joint and line components into same assembly Arch
    
    
! GroupName: pin
KSEL,NONE ! Clearing KP selection   
KSEL,A,KP,,1
LSEL,NONE ! Clearing Line selection 
CM,pin_J,KP ! The component that has the Joints of Group pin
CM,pin_L,LINE ! The component that has the Lines of Group pin   
CMGRP,pin,pin_J,pin_L ! Putting the joint and line components into same assembly pin
    
    
! GroupName: slide  
KSEL,NONE ! Clearing KP selection   
KSEL,A,KP,,23   
LSEL,NONE ! Clearing Line selection 
CM,slide_J,KP ! The component that has the Joints of Group slide
CM,slide_L,LINE ! The component that has the Lines of Group slide   
CMGRP,slide,slide_J,slide_L ! Putting the joint and line components into same assembly slide
    
    
! GroupName: apnts  
KSEL,NONE ! Clearing KP selection   
KSEL,A,KP,,1
KSEL,A,KP,,2
KSEL,A,KP,,3
KSEL,A,KP,,4
KSEL,A,KP,,5
KSEL,A,KP,,6
KSEL,A,KP,,7
KSEL,A,KP,,8
KSEL,A,KP,,9
KSEL,A,KP,,10   
KSEL,A,KP,,11   
KSEL,A,KP,,12   
KSEL,A,KP,,13   
KSEL,A,KP,,14   
KSEL,A,KP,,15   
KSEL,A,KP,,16   
KSEL,A,KP,,17   
KSEL,A,KP,,18   
KSEL,A,KP,,19   
KSEL,A,KP,,20   
KSEL,A,KP,,21   
KSEL,A,KP,,22   
KSEL,A,KP,,23   
LSEL,NONE ! Clearing Line selection 
CM,apnts_J,KP ! The component that has the Joints of Group apnts
CM,apnts_L,LINE ! The component that has the Lines of Group apnts   
CMGRP,apnts,apnts_J,apnts_L ! Putting the joint and line components into same assembly apnts
    
    
    
! Meshing the Frames
LSEL,ALL
LESIZE,ALL, , ,3, , , , ,1  
LMESH,ALL   
    
    
! Going to Solution Context 
FINISH  
/SOLU   
    
    
! Defining the Restraints   
KSEL,NONE ! Clearing KP selection   
CMSEL,A,pin,KP ! Selecting the Joints that are in the assembly. 
DK,ALL,UX,0 ! Sets UX for previously selected joints
DK,ALL,UY,0 ! Sets UY for previously selected joints
DK,ALL,UZ,0 ! Sets UZ for previously selected joints
    
KSEL,NONE ! Clearing KP selection   
CMSEL,A,slide,KP ! Selecting the Joints that are in the assembly.   
DK,ALL,UX,0 ! Sets UX for previously selected joints
DK,ALL,UY,0 ! Sets UY for previously selected joints
DK,ALL,UZ,0 ! Sets UZ for previously selected joints
    
KSEL,NONE ! Clearing KP selection   
CMSEL,A,apnts,KP ! Selecting the Joints that are in the assembly.   
DK,ALL,UY,0 ! Sets UY for previously selected joints
    
    
    
! Defining the Loads
ACEL,0,0,9.80665 ! Sets the Gravity 
    
    
! SOLVING   
ALLSEL,ALL ! Somehow, and for any weird-ass reason, Ansys will only pass KeyPoints definitions onwards if they are selected.
SOLVE ! Solves the problem  
FINISH  
    
    
/POST1 ! Changes to PostProc
PRRSOL,F ! List Reaction Forces 
PLDISP,2 ! Plot Deformed shape  
PLNSOL, U, SUM,0,1 ! Contour Plot of deflection 
ETABLE,SAXL,LS, 1 ! Axial Stress
PRETAB,SAXL ! List Element Table
PLETAB, SAXL, NOAV !Plot Axial Stress   
