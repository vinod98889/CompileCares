using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompileCares.Shared.Enums
{
    public enum VisitStatus
    {
        Scheduled = 1,
        CheckedIn = 2,
        InProgress = 3,
        Completed = 4,
        Cancelled = 5,
        NoShow = 6
    }
}
