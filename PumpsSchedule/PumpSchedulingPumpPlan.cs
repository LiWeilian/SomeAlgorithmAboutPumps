using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PumpsSchedule
{
    internal class PumpSchedulingPumpPlan
    {
        public string PumpNum { get; set; }

        public bool IsOpen { get; set; }

        public double CurrentFlow { get; set; }

        public double CurrentHead { get; set; }

        public double CurrentSpeed { get; set; }

        public double CurrentPumpPower { get; set; }

        public double CurrentPumpEfficiency { get; set; }

        public double CurrentTotalPower { get; set; }
    }
}
