using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PumpsSchedule
{
    internal class PumpScheduling
    {
        public static List<PumpSchedulingPlan> Run(PumpSchedulingParams inputPrams)
        {
            List<PumpSchedulingPlan> plans = new List<PumpSchedulingPlan>();

            List<Pump> pumps = new List<Pump>();
            foreach (InPumpParam pump_param in inputPrams.Pumps)
            {
                List<CurvePoint> pump_eff_curvepoints = new List<CurvePoint>();
                foreach (InCurvePoint curvepoint in pump_param.RatedParam.PumpEfficiencyCurve)
                {
                    pump_eff_curvepoints.Add(new CurvePoint()
                    {
                        X = curvepoint.X,
                        Y = curvepoint.Y
                    });
                }
                PumpEfficiencyCurve pump_eff_curve = new PumpEfficiencyCurve(0.65, pump_eff_curvepoints);

                PumpRatedParam rated_param = new PumpRatedParam()
                {
                    EMEfficiency = pump_param.RatedParam.EMEfficiency,
                    EMPower = pump_param.RatedParam.EMPower,
                    MaxPower = pump_param.RatedParam.MaxPower,
                    MinPower = pump_param.RatedParam.MinPower,
                    MaxSpeed = pump_param.RatedParam.MaxSpeed,
                    MinSpeed = pump_param.RatedParam.MinSpeed,
                    PumpEfficiencyCurve = pump_eff_curve,
                    RatedFlow = pump_param.RatedParam.RatedFlow,
                    RatedHead = pump_param.RatedParam.RatedHead,
                    RatedSpeed = pump_param.RatedParam.RatedSpeed,
                    VFDEfficency = pump_param.RatedParam.VFDEfficency
                };

                pumps.Add(new Pump(pump_param.PumpNum, rated_param, true));
            }

            List<PumpGroupSchedulingOperation> operations = new List<PumpGroupSchedulingOperation>();

            foreach (InPumpSchedulingOperation operation_param in inputPrams.Operations)
            {
                operations.Add(new PumpGroupSchedulingOperation()
                {
                    OperationNum = operation_param.OperationNum,
                    InPressure = operation_param.InPressure,
                    OutPressure = operation_param.OutPressure,
                    OutFlow = operation_param.OutFlow,
                    Pumps = CopyPumps.Copy(pumps)
                });
            }

            PumpGroupSchedulingManager sch_mgr = new PumpGroupSchedulingManager(operations);
            List<PumpSchedulingOperationPlan> op_results_match_flow = sch_mgr.MatchFlowPlan();
            List<PumpSchedulingOperationPlan> op_results_min_power = sch_mgr.MinPowerPlan();

            PumpSchedulingPlan sch_result_match_flow = new PumpSchedulingPlan();
            sch_result_match_flow.Operations.AddRange(op_results_match_flow);
            sch_result_match_flow.SchedulingName = "最匹配流量方案";

            plans.Add(sch_result_match_flow);

            PumpSchedulingPlan sch_result_min_power = new PumpSchedulingPlan();
            sch_result_min_power.Operations.AddRange(op_results_min_power);
            sch_result_min_power.SchedulingName = "最低能耗方案";

            plans.Add(sch_result_min_power);

            return plans;
        }
    }
}
