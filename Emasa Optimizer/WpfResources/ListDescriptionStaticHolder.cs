using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emasa_Optimizer.FEA;
using Emasa_Optimizer.FEA.Items;
using Emasa_Optimizer.Opt;
using NLoptNet;

namespace Emasa_Optimizer.WpfResources
{
    public sealed class ListDescriptionStaticHolder
    {
        #region Singleton Management
        private static ListDescriptionStaticHolder _mainInstance;
        private static readonly object _lockThis = new object();

        private ListDescriptionStaticHolder()
        {
            FeSectionListEnumDescriptions = new List<FeSection>();
            FeSectionListEnumDescriptions.AddRange(FeSectionPipe.GetAllSections());
        }

        public static ListDescriptionStaticHolder ListDescSingleton
        {
            get
            {
                lock (_lockThis)
                {
                    // If singleton hasn't been initialized
                    if (_mainInstance == null) _mainInstance = new ListDescriptionStaticHolder();
                }

                return _mainInstance;
            }
        }
        #endregion

        public Dictionary<StartPositionTypeEnum, string> StartPositionTypeEnumStaticDescriptions { get; private set; } = new Dictionary<StartPositionTypeEnum, string>()
            {
                {StartPositionTypeEnum.Given, "Given Value"},
                {StartPositionTypeEnum.CenterOfRange, "Center of Range"},
                {StartPositionTypeEnum.Random, "Random Within Range"},
                {StartPositionTypeEnum.PercentRandomFromCenter, "% Rand Shift from Center"},
                {StartPositionTypeEnum.PercentRandomFromGiven, "% Rand Shift from Given"}
            };
        public Dictionary<MainAxisDirectionEnum, string> MainAxisDirectionEnumStaticDescriptions = new Dictionary<MainAxisDirectionEnum, string>()
            {
                {MainAxisDirectionEnum.xPos, "+X"},
                {MainAxisDirectionEnum.xNeg, "-X"},
                {MainAxisDirectionEnum.yPos, "+Y"},
                {MainAxisDirectionEnum.yNeg, "-Y"},
                {MainAxisDirectionEnum.zPos, "+Z"},
                {MainAxisDirectionEnum.zNeg, "-Z"},
            };
        public Dictionary<FeSolverTypeEnum, string> FeSolverTypeEnumStaticDescriptions = new Dictionary<FeSolverTypeEnum, string>()
            {
                {FeSolverTypeEnum.Ansys, "Ansys"},
            };
        public Dictionary<NLoptAlgorithm, string> NlOptAlgorithmEnumDescriptions = new Dictionary<NLoptAlgorithm, string>()
            {
                {NLoptAlgorithm.LN_COBYLA, "Cobyla [LN]"},
                {NLoptAlgorithm.LN_BOBYQA, "Bobyqa [LN]"},
                {NLoptAlgorithm.GN_DIRECT, "Dividing Rectangles [GN]"},
                {NLoptAlgorithm.GN_DIRECT_L, "Dividing Rectangles - Locally Biased [GN]"},
                {NLoptAlgorithm.GN_DIRECT_L_RAND, "Dividing Rectangles - Locally Biased With Some Randomization [GN]"},
                {NLoptAlgorithm.GN_CRS2_LM, "Controlled Random Search With Local Mutation [GN]"},
                {NLoptAlgorithm.GD_STOGO, "StoGo [GD]"},
                {NLoptAlgorithm.GD_STOGO_RAND, "StoGo - Randomized [GD]"},
                {NLoptAlgorithm.GN_ISRES, "Improved Stochastic Ranking Evolution Strategy [GN]"},
                {NLoptAlgorithm.GN_ESCH, "ESCH (evolutionary algorithm) [GN]"},
            };


        // Initialized in the constructor
        public List<FeSection> FeSectionListEnumDescriptions { get; private set; } 
    }
}
