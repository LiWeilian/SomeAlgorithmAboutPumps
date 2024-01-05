using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PumpsSchedule
{
    internal class PumpSchedulingOperationPlan
    {
        public string OperationNum { get; set; }

        public double TotalFlow { get; set; }

        public double TotalPower { get; set; }

        public double ElectricityFees { get; set; }

        public double Fitness { get; set; }

        public List<PumpSchedulingPumpPlan> Pumps { get; set; } = new List<PumpSchedulingPumpPlan>();

    }
}
