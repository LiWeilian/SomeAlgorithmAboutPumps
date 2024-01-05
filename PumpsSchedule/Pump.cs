using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PumpsSchedule
{
    internal class Pump
    {
        public string PumpNum { get; private set; }
        public PumpRatedParam RatedParam { get; private set; }
        /// <summary>
        /// 转速
        /// </summary>
        public double CurrentSpeed { get; set; }
        /// <summary>
        /// 当前电机功率，理论上与转速比例的三次方成正比，实际可能小一些
        /// </summary>
        public double CurrentEMPower
        {
            get
            {
                return RatedParam.EMPower * Math.Pow(CurrentSpeed / RatedParam.RatedSpeed, 3.00);
            }
        }
        /// <summary>
        /// 是否变频水泵
        /// </summary>
        public bool IsVarFrequency
        {
            get
            {
                return RatedParam.MaxSpeed >= RatedParam.RatedSpeed
                    && RatedParam.RatedSpeed >= RatedParam.MinSpeed
                    && RatedParam.MaxSpeed != RatedParam.MinSpeed;
            }
        }
        /// <summary>
        /// 启停状态
        /// </summary>
        public bool IsOpen { get; set; }
        /// <summary>
        /// 运行状态，是否正常运行
        /// </summary>
        public bool RunStatus { get; set; } = true;

        public Pump(string pump_num, PumpRatedParam rated_param, bool isopen)
        {
            PumpNum = pump_num;
            RatedParam = rated_param;
            CurrentSpeed = RatedParam.RatedSpeed;
            IsOpen = isopen;
        }

        public double GetCurrentHeadByFlow(double flow)
        {
            double speed_rate = CurrentSpeed / RatedParam.RatedSpeed;

            //流量工况点，与转速比例成正比
            double flow_rate = RatedParam.RatedFlow * speed_rate;
            //扬程工况点，与转速比例平方成正比
            double head_rate = RatedParam.RatedHead * Math.Pow(speed_rate, 2.0);

            List<CurvePoint> curvepoints = new List<CurvePoint>();
            curvepoints.Add(new CurvePoint() { X = flow_rate, Y = head_rate });

            PumpCurve pump_curve = new PumpCurve(curvepoints);

            return pump_curve.CalcHeadByFlow(flow);
        }

        public double GetCurrentFlowByHead(double head)
        {
            double speed_rate = CurrentSpeed / RatedParam.RatedSpeed;

            //流量工况点，与转速比例成正比
            double flow_rate = RatedParam.RatedFlow * speed_rate;
            //扬程工况点，与转速比例平方成正比
            double head_rate = RatedParam.RatedHead * Math.Pow(speed_rate, 2.0);

            List<CurvePoint> curvepoints = new List<CurvePoint>();
            curvepoints.Add(new CurvePoint() { X = flow_rate, Y = head_rate });

            PumpCurve pump_curve = new PumpCurve(curvepoints);

            return pump_curve.CalcFlowByHead(head);
        }
        public double GetCurrentEfficiencyByFlow(double flow)
        {
            return RatedParam.PumpEfficiencyCurve.GetEfficiencyByFlow(flow);
        }
    }

    /// <summary>
    /// 水泵额定参数
    /// </summary>
    internal class PumpRatedParam
    {
        /// <summary>
        /// 额定流量，单位：跟随系统设置
        /// </summary>
        public double RatedFlow { get; set; }
        /// <summary>
        /// 额定扬程，单位：米
        /// </summary>
        public double RatedHead { get; set; }
        /// <summary>
        /// 额定转速
        /// </summary>
        public double RatedSpeed { get; set; }
        /// <summary>
        /// 最小转速
        /// </summary>
        public double MinSpeed { get; set; }
        /// <summary>
        /// 最大转速
        /// </summary>
        public double MaxSpeed { get; set; }
        /// <summary>
        /// 最小功率
        /// </summary>
        public double MinPower { get; set; }
        /// <summary>
        /// 最大功率
        /// </summary>
        public double MaxPower { get; set; }
        /// <summary>
        /// 电机额定功率，单位：kW
        /// </summary>
        public double EMPower { get; set; }
        /// <summary>
        /// 电机额定效率，百分比
        /// </summary>
        public double EMEfficiency { get; set; } = 0.85;
        /// <summary>
        /// 水泵效率曲线
        /// </summary>
        public PumpEfficiencyCurve PumpEfficiencyCurve { get; set; }
        /// <summary>
        /// 变频器效率，定频泵默认为1
        /// </summary>
        public double VFDEfficency { get; set; } = 1.0;
    }

    internal class CopyPump
    {
        public static Pump Copy(Pump pump_in)
        {
            List<CurvePoint> curve_points_out = new List<CurvePoint>();
            foreach (CurvePoint cp in pump_in.RatedParam.PumpEfficiencyCurve.CurvePoints)
            {
                curve_points_out.Add(new CurvePoint() { X = cp.X, Y = cp.Y });
            }
            PumpEfficiencyCurve eff_curve_out = new PumpEfficiencyCurve(pump_in.RatedParam.PumpEfficiencyCurve.Efficiency_Global,
                curve_points_out);
            PumpRatedParam rated_param = new PumpRatedParam()
            {
                RatedFlow = pump_in.RatedParam.RatedFlow,
                RatedHead = pump_in.RatedParam.RatedHead,
                RatedSpeed = pump_in.RatedParam.RatedSpeed,
                MinSpeed = pump_in.RatedParam.MinSpeed,
                MaxSpeed = pump_in.RatedParam.MaxSpeed,
                MinPower = pump_in.RatedParam.MinPower,
                MaxPower = pump_in.RatedParam.MaxPower,
                EMPower = pump_in.RatedParam.EMPower,
                EMEfficiency = pump_in.RatedParam.EMEfficiency,
                PumpEfficiencyCurve = eff_curve_out,
                VFDEfficency = pump_in.RatedParam.VFDEfficency
            };
            return new Pump(pump_in.PumpNum, rated_param, pump_in.IsOpen)
            {
                CurrentSpeed = pump_in.CurrentSpeed,
                IsOpen = pump_in.IsOpen,
                RunStatus = pump_in.RunStatus
            };
        }
    }

    internal class CopyPumps
    {
        public static List<Pump> Copy(List<Pump> pumps_in)
        {
            List<Pump> pumps = new List<Pump>();
            foreach (Pump p in pumps_in)
            {
                pumps.Add(CopyPump.Copy(p));
            }
            return pumps;
        }
    }
}
