using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PumpsSchedule
{
    internal class PumpSchedulingParams
    {
        public List<InPumpParam> Pumps { get; set; }
        public List<InPumpSchedulingOperation> Operations { get; set; }
    }

    internal class InPumpParam
    {
        public string PumpNum { get; set; }
        public InPumpRatedParam RatedParam { get; set; }
    }

    class InPumpRatedParam
    {
        public double RatedFlow { get; set; }

        public double RatedHead { get; set; }

        public double RatedSpeed { get; set; }

        public double MinSpeed { get; set; }

        public double MaxSpeed { get; set; }

        public double MinPower { get; set; }

        public double MaxPower { get; set; }

        public double EMPower { get; set; }

        public double EMEfficiency { get; set; } = 0.85;


        public List<InCurvePoint> PumpEfficiencyCurve { get; set; }

        public double VFDEfficency { get; set; } = 1.0;

    }

    internal class InCurvePoint
    {
        public double X { get; set; }

        public double Y { get; set; }
    }

    internal class InPumpSchedulingOperation
    {
        public string OperationNum { get; set; }

        public double InPressure { get; set; }

        public double OutPressure { get; set; }

        public double OutFlow { get; set; }

        public List<string> Pumps { get; set; }

        public double Electrovalence { get; set; }
    }
}
