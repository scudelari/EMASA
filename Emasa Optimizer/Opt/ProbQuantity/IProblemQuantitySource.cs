namespace Emasa_Optimizer.Opt.ProbQuantity
{
    public interface IProblemQuantitySource
    {
        bool IsGhGeometryDoubleListData { get; }
        bool IsFiniteElementData { get; }

        string Wpf_ProblemQuantityName { get; }
        string Wpf_ProblemQuantityGroup { get; }
        string Wpf_Explanation { get; }

        string ScreenShotFileName { get; }
        string DataTableName { get; }

        void AddProblemQuantity_FunctionObjective();
        void AddProblemQuantity_ConstraintObjective();
        void AddProblemQuantity_OutputOnly();

        // Must have to match live filtering
        bool IsSupportedByCurrentSolver { get; }
        bool OutputData_IsSelected { get; }

        string ConcernedResultColumnName { get; }
    }
}
