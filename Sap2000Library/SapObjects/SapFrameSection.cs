using Sap2000Library.Managers;

namespace Sap2000Library.SapObjects
{
    public class SapFrameSection
    {
        private FrameSectionManager owner = null;
        internal SapFrameSection(string name, FramePropType inSectType , FrameSectionManager frameSectMan)
        {
            owner = frameSectMan;
            Name = name;
            SectionType = inSectType;
        }

        public string Name { get; set; }

        public FramePropType SectionType { get; set; }
    }
}
