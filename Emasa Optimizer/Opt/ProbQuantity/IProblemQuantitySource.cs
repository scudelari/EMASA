namespace Emasa_Optimizer.Opt.ProbQuantity
{
    public interface IProblemQuantitySource
    {
        bool IsGhGeometryDoubleListData { get; }
        bool IsFiniteElementData { get; }

        string ResultFamilyGroupName { get; }
        string ResultTypeDescription { get; }
        string TargetShapeDescription { get; }

        void AddProblemQuantity_FunctionObjective(object inSolveMan);
        void AddProblemQuantity_ConstraintObjective(object inSolveMan);
        void AddProblemQuantity_OutputOnly(object inSolveMan);
    }
}
