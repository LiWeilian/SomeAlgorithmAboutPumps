using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PumpsSchedule
{
    internal class PumpGroupSchedulingManager
    {
        public enum OptimizeType
        {
            MinPower,
            MatchFlow
        }

        private int evolove_gens = 100;

        private double xross_over_ratio = 0.5;
        private double on_off_mutation_ratio = 0.05;
        private double speed_mutation_ratio = 0.3;
        public string PlanNum { get; set; }
        public string PlanName { get; set; }
        public List<PumpGroupSchedulingOperation> SchedulingOperations { get; private set; }

        public PumpGroupSchedulingManager(List<PumpGroupSchedulingOperation> scheduling_operations)
        {
            SchedulingOperations = new List<PumpGroupSchedulingOperation>();
            SchedulingOperations.AddRange(scheduling_operations);
        }
        public double GetTotalCost(List<CostParams> cost_params)
        {
            foreach (PumpGroupSchedulingOperation step in SchedulingOperations)
            {
                OptimizeSchedulingOperationPlan(step, OptimizeType.MatchFlow, null);
            }
            return 0.0;
        }

        /// <summary>
        /// 最低功耗方案
        /// </summary>
        /// <returns></returns>
        public List<PumpSchedulingOperationPlan> MinPowerPlan()
        {
            List<PumpSchedulingOperationPlan> op_plans = new List<PumpSchedulingOperationPlan>();
            PumpSchedulingOperationPlan op_plan = null;
            foreach (PumpGroupSchedulingOperation operation in SchedulingOperations)
            {
                op_plan = OptimizeSchedulingOperationPlan(operation, OptimizeType.MinPower, op_plan);
                op_plans.Add(op_plan);
            }
            return op_plans;
        }

        /// <summary>
        /// 最匹配流量方案
        /// </summary>
        /// <returns></returns>
        public List<PumpSchedulingOperationPlan> MatchFlowPlan()
        {
            List<PumpSchedulingOperationPlan> op_plans = new List<PumpSchedulingOperationPlan>();
            PumpSchedulingOperationPlan op_plan = null;
            foreach (PumpGroupSchedulingOperation operation in SchedulingOperations)
            {
                op_plan = OptimizeSchedulingOperationPlan(operation, OptimizeType.MatchFlow, op_plan);
                op_plans.Add(op_plan);
            }
            return op_plans;
        }

        public PumpSchedulingOperationPlan OptimizeSchedulingOperationPlan(PumpGroupSchedulingOperation sch_op,
            OptimizeType optimize_type, PumpSchedulingOperationPlan last_op_plan)
        {
            //扬程
            double head_total = sch_op.OutPressure - sch_op.InPressure;
            //总流量
            double flow_total = sch_op.OutFlow;


            List<PumpSchedulingChromosome> init_chromosome_list = new List<PumpSchedulingChromosome>();
            List<PumpSchedulingChromosome> history_chromosome_list = new List<PumpSchedulingChromosome>();


            int pop_size = 10;
            for (int i = 0; i < pop_size; i++)
            {
                List<Pump> pumps = new List<Pump>();
                foreach (Pump p in sch_op.Pumps)
                {
                    pumps.Add(CopyPump.Copy(p));
                }

                PumpSchedulingChromosome chrom = new PumpSchedulingChromosome(pumps, on_off_mutation_ratio, speed_mutation_ratio);
                //先变异一次
                chrom.ChromosomeMutate();

                init_chromosome_list.Add(chrom);
            }

            PumpSchedulingPopulation pop = new PumpSchedulingPopulation(DateTime.Now.ToString("yyyyMMddHHmmss"),
                init_chromosome_list,
                pop_size,
                xross_over_ratio);

            for (int i = 0; i < evolove_gens; i++)
            {
                pop.Generation++;
                if (i > 0)
                {
                    //交叉
                    pop.PopulationCrossover();
                    //变异
                    pop.ChromosomeMutate();
                }

                //评估适应度
                foreach (PumpSchedulingChromosome chrom in pop.Chromosomes)
                {
                    EvaluteFitness(head_total, flow_total, chrom, optimize_type, last_op_plan);
                }

                //排序
                pop.ChromosomeSort();
                //更新选择率
                pop.UpdateChromosomeSelectedRatio();

                foreach (PumpSchedulingChromosome chrom in pop.Chromosomes)
                {
                    //List<Pump> pumps = new List<Pump>();
                    //foreach (Pump p in chrom.Pumps)
                    //{
                    //    pumps.Add(CopyPump.Copy(p));
                    //}

                    //history_chromosome_list.Add(new PumpSchedulingChromosome(pumps, 
                    //    chrom.OnOffMutationRatio, 
                    //    chrom.SpeedMutationRatio)
                    //    {
                    //        Fitness = chrom.Fitness,
                    //        SelectedRatio = chrom.SelectedRatio,
                    //        TotalFlow = chrom.TotalFlow,
                    //        TotalPower = chrom.TotalPower
                    //    });
                    history_chromosome_list.Add(CopyChromosome.Copy(chrom));
                }

                //输出结果
                OutputPopulation(pop, head_total, flow_total);
            }

            PumpSchedulingPopulation pop_history = new PumpSchedulingPopulation(DateTime.Now.ToString("History_yyyyMMddHHmmss"),
                history_chromosome_list,
                pop_size,
                xross_over_ratio);
            pop_history.Generation = evolove_gens + 1;

            //输出最佳历史结果
            OutputPopulation(pop_history, head_total, flow_total);

            return GetSchedulingOperation(sch_op.OperationNum, pop_history, head_total, flow_total);
        }

        private PumpSchedulingOperationPlan GetSchedulingOperation(string operation_num, PumpSchedulingPopulation pop, double head, double total_flow)
        {
            if (pop.Chromosomes.Count == 0)
            {
                return null;
            }

            PumpSchedulingOperationPlan op_result = new PumpSchedulingOperationPlan();
            op_result.OperationNum = operation_num;
            op_result.Pumps = new List<PumpSchedulingPumpPlan>();

            foreach (Pump pump in pop.Chromosomes[0].Pumps)
            {
                double flow = pump.GetCurrentFlowByHead(head);
                if (double.IsNaN(flow))
                {
                    flow = 0.0d;
                }
                double eff = pump.GetCurrentEfficiencyByFlow(flow);
                if (double.IsNaN(eff))
                {
                    eff = 0.0d;
                }
                double pump_power = 0.0d;
                if (eff > 0)
                {
                    pump_power = flow * 3.6 * head * 9.8 / 3600 / eff;
                }
                double total_power = pump_power / pump.RatedParam.EMEfficiency
                    / pump.RatedParam.VFDEfficency;

                op_result.Pumps.Add(new PumpSchedulingPumpPlan()
                {
                    PumpNum = pump.PumpNum,
                    IsOpen = pump.IsOpen,
                    CurrentFlow = flow,
                    CurrentHead = head,
                    CurrentPumpEfficiency = eff,
                    CurrentPumpPower = pump_power,
                    CurrentSpeed = pump.CurrentSpeed,
                    CurrentTotalPower = total_power
                });
            }

            op_result.TotalFlow = pop.Chromosomes[0].TotalFlow;
            op_result.TotalPower = pop.Chromosomes[0].TotalPower;
            op_result.Fitness = pop.Chromosomes[0].Fitness;

            return op_result;
        }

        private void EvaluteFitness(double head_total, double flow_total,
            PumpSchedulingChromosome chromosome, OptimizeType optimize_type,
            PumpSchedulingOperationPlan last_op_plan)
        {
            bool overload = false;

            //泵组适应度因子
            double isopen_fitness_factor = 1.0;

            double pump_eff_avg = 1.0;
            double em_eff_avg = 1.0;
            double vfd_eff_avg = 1.0;

            double current_total_power = 0.0;
            double current_total_flow = 0.0;
            foreach (Pump pump in chromosome.Pumps)
            {
                PumpSchedulingPumpPlan last_op_pump_result = last_op_plan?.Pumps.Find(p => p.PumpNum == pump.PumpNum);
                if (last_op_pump_result != null && last_op_pump_result.IsOpen != pump.IsOpen)
                {
                    //需要进行水泵启停操作时，降低适应度
                    isopen_fitness_factor *= 0.95;
                }
                if (!pump.IsOpen)
                {
                    continue;
                }

                double flow = pump.GetCurrentFlowByHead(head_total);
                if (double.IsNaN(flow))
                {
                    flow = 0.0d;
                }
                double pump_eff = pump.GetCurrentEfficiencyByFlow(flow);
                if (double.IsNaN(pump_eff))
                {
                    pump_eff = 0.0d;
                }
                double pump_power = 0.0d;
                if (pump_eff > 0)
                {
                    pump_power = flow * 3.6 * head_total * 9.8 / 3600 / pump_eff
                    / pump.RatedParam.EMEfficiency / pump.RatedParam.VFDEfficency;
                }
                //double em_power = pump_power / pump.RatedParam.EMEfficiency;

                //if (pump_power > pump.RatedParam.EMPower 
                //    && (pump_power - pump.RatedParam.EMPower) / pump.RatedParam.EMPower > 0.3)
                //{
                //    overload = true;
                //}

                //if (pump.RatedParam.MaxPower > 0 && pump_power > pump.RatedParam.MaxPower)
                //{
                //    overload = true;
                //}

                current_total_power += pump_power;
                current_total_flow += flow;

                pump_eff_avg = (pump_eff_avg + pump_eff) / 2.0;
                em_eff_avg = (em_eff_avg + pump.RatedParam.EMEfficiency) / 2.0;
                vfd_eff_avg = (vfd_eff_avg + pump.RatedParam.VFDEfficency) / 2.0;
            }

            chromosome.TotalFlow = current_total_flow;
            chromosome.TotalPower = current_total_power;


            switch (optimize_type)
            {
                case OptimizeType.MinPower:
                    double flow_ratio2 = (flow_total - Math.Abs(flow_total - current_total_flow)) / flow_total;
                    double power_temp = flow_total * 3.6 * head_total * 9.8 / 3600 / pump_eff_avg
                        / em_eff_avg / vfd_eff_avg;

                    double power_ratio = 0.0;
                    if (flow_ratio2 > 0.9)
                    {
                        power_ratio = (power_temp - Math.Abs(power_temp - current_total_power)) / power_temp;
                    }

                    chromosome.Fitness = power_ratio * isopen_fitness_factor;
                    break;
                case OptimizeType.MatchFlow:
                    double flow_ratio = (flow_total - Math.Abs(flow_total - current_total_flow)) / flow_total;

                    chromosome.Fitness = flow_ratio * isopen_fitness_factor;

                    break;
            }

            if (overload)
            {
                chromosome.Fitness = 0.0;
            }
        }

        private void OutputPopulation(PumpSchedulingPopulation pop, double head, double total_flow)
        {
            string chrom_info = "";
            foreach (PumpSchedulingChromosome chrom in pop.Chromosomes)
            {
                string pump_info = "";
                foreach (Pump pump in chrom.Pumps)
                {
                    double flow = pump.GetCurrentFlowByHead(head);
                    if (double.IsNaN(flow))
                    {
                        flow = 0.0d;
                    }
                    double eff = pump.GetCurrentEfficiencyByFlow(flow);
                    if (double.IsNaN(eff))
                    {
                        eff = 0.0d;
                    }
                    double pump_power = 0.0d;
                    if (eff > 0)
                    {
                        pump_power = flow * 3.6 * head * 9.8 / 3600 / eff;
                    }

                    double total_power = pump_power / pump.RatedParam.EMEfficiency / pump.RatedParam.VFDEfficency;

                    pump_info = string.Format("{0}水泵编号：{1}\r\n运行状态：{2}\r\n开关状态：{3}\r\n是否变频；{4}\r\n当前流量：{5:0.000}L/s\r\n当前水泵功率：{6:0.000}kW\r\n当前转速：{7:0.000}\r\n当前水泵效率：{8:0.000}\r\n当前电机效率：{9:0.000}\r\n当前变速器效率：{10:0.000}\r\n当前总功率：{11:0.000}kW\r\n",
                        pump_info,
                        pump.PumpNum,
                        pump.RunStatus,
                        pump.IsOpen,
                        pump.IsVarFrequency,
                        flow,
                        pump_power,
                        pump.CurrentSpeed,
                        eff,
                        pump.RatedParam.EMEfficiency,
                        pump.RatedParam.VFDEfficency,
                        total_power);
                }
                chrom_info = string.Format("{0}适应度：{1}，目标流量：{2}，当前流量：{3}，当前功率：{4}\r\n水泵信息：\r\n{5}\r\n",
                    chrom_info,
                    chrom.Fitness,
                    total_flow,
                    chrom.TotalFlow,
                    chrom.TotalPower,
                    pump_info);
            }

            string pop_info = string.Format("第{0}/{1}代，样本数：{2}，交叉率：{3}，启停变异率：{4}，转速变异率：{5}",
                pop.Generation,
                evolove_gens,
                pop.Chromosomes.Count,
                xross_over_ratio,
                on_off_mutation_ratio,
                speed_mutation_ratio);

            string info = string.Format("{0}\r\n{1}\r\n", pop_info, chrom_info);

            LogRunMessage(info);

        }

        public static void LogRunMessage(string msg)
        {
            try
            {
                string logFileName = string.Empty;
                string dir = string.Format("{0}\\log\\{1}", AppDomain.CurrentDomain.BaseDirectory,
                    DateTime.Now.ToString("yyyy-MM-dd"));
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (Directory.Exists(dir))
                {
                    logFileName = string.Format("{0}\\PUMP_SCHEDULING_{1}.log",
                                dir,
                                DateTime.Now.ToString("yyyyMMdd"));
                }

                FileStream fs;
                if (File.Exists(logFileName))
                {
                    fs = new FileStream(logFileName, FileMode.Append, FileAccess.Write);
                }
                else
                {
                    fs = new FileStream(logFileName, FileMode.Create, FileAccess.Write);
                }

                StreamWriter sw = new StreamWriter(fs);

                sw.WriteLine(string.Format("{0}\r\n{1}\r\n",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    msg));
                sw.Flush();
                sw.Close();
                fs.Close();
            }
            catch (Exception)
            {

            }

        }
    }

    /// <summary>
    /// 泵组在某一时段的调度操作，包括入水压力、出水压力、出水流量、参与调度的水泵等。
    /// </summary>
    internal class PumpGroupSchedulingOperation
    {
        public string OperationNum { get; set; }
        public double InPressure { get; set; }
        public double OutPressure { get; set; }
        public double OutFlow { get; set; }
        public List<Pump> Pumps { get; set; }
    }

    internal class CostParams
    {
        public double ElectricityUnitPrice { get; set; }
    }
}
