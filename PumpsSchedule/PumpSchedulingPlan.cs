using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PumpsSchedule
{
    internal class PumpSchedulingPlan
    {
        public string SchedulingName { get; set; }

        public List<PumpSchedulingOperationPlan> Operations { get; set; } = new List<PumpSchedulingOperationPlan>();

    }
}
