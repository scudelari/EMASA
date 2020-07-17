using System.Windows;
using BaseWPFLibrary;
using BaseWPFLibrary.Bindings;

namespace EmasaSapTools.Bindings
{
    public class RhinoOperationsBindings : BindableSingleton<RhinoOperationsBindings>
    {
        private RhinoOperationsBindings(){}
        public override void SetOrReset()
        {
            MarkJointsInPurple = true;
            TargetRhinoGroupForSelectedJoints = "";

            
        }

        private bool _MarkJointsInPurple;
        public bool MarkJointsInPurple { get => _MarkJointsInPurple; set => SetProperty(ref _MarkJointsInPurple, value); }

        private string _TargetRhinoGroupForSelectedJoints;
        public string TargetRhinoGroupForSelectedJoints { get => _TargetRhinoGroupForSelectedJoints; set => SetProperty(ref _TargetRhinoGroupForSelectedJoints, value); }
    }
}