using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmasaSapTools.Monitors
{
    public interface IMonitorInterfaceItems
    {
        string Name { get; set; }
        string ShortName { get; set; }
        bool Constraint_IsEnabled { get; set; }
    }
}
