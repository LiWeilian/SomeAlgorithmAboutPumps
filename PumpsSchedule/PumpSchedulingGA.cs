using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PumpsSchedule
{
    class PumpSchedulingPopulation
    {
        private Random random = new Random(unchecked((int)DateTime.Now.Ticks));
        public int PopSize { get; private set; }
        public int Generation { get; set; } = 0;
        public double CrossoverRatio { get; private set; }
        public List<PumpSchedulingChromosome> Chromosomes { get; private set; }
        public PumpSchedulingPopulation(string pop_sn,
            List<PumpSchedulingChromosome> chromosomes,
            int pop_size,
            double crossover_ratio)
        {
            PopSize = pop_size;
            CrossoverRatio = crossover_ratio;

            Chromosomes = new List<PumpSchedulingChromosome>();
            foreach (PumpSchedulingChromosome chrom in chromosomes)
            {
                //List<Pump> pumps = new List<Pump>();
                //foreach (Pump p in chrom.Pumps)
                //{
                //    pumps.Add(CopyPump.Copy(p));
                //}
                //Chromosomes.Add(new PumpSchedulingChromosome(pumps, chrom.OnOffMutationRatio, chrom.SpeedMutationRatio)
                //{
                //    Fitness = chrom.Fitness,
                //    SelectedRatio = chrom.SelectedRatio,
                //    TotalFlow = chrom.TotalFlow,
                //    TotalPower = chrom.TotalPower
                //});
                Chromosomes.Add(CopyChromosome.Copy(chrom));
            }

            ChromosomeSort();
            UpdateChromosomeSelectedRatio();
        }

        public void ChromosomeSort()
        {
            //按适应度倒序排序
            Chromosomes.Sort((x, y) => -x.Fitness.CompareTo(y.Fitness));
            //保留适应度较高的染色体
            int size = PopSize > 0 ? PopSize : 100;
            Chromosomes = Chromosomes.Take(size).ToList();
        }

        public void ChromosomeMutate()
        {
            foreach (PumpSchedulingChromosome chrom in Chromosomes)
            {
                chrom.ChromosomeMutate();
            }
        }

        public void PopulationCrossover()
        {
            int current_size = Chromosomes.Count();
            int one = -1;
            int first = 0;

            List<PumpSchedulingChromosome> chromsomes_new = new List<PumpSchedulingChromosome>();

            for (int mem = 0; mem < current_size; ++mem)
            {
                double select_ratio = random.Next(0, 10000);
                bool selected = select_ratio <= Chromosomes[mem].SelectedRatio * 10000;

                if (selected)
                {
                    double xover_rate = random.Next(0, 1000) / 1000f;
                    if (xover_rate <= CrossoverRatio)
                    {
                        ++first;
                        if (first % 2 == 0)
                        {
                            List<Pump> pump_list1 = new List<Pump>();
                            List<Pump> pump_list2 = new List<Pump>();
                            for (int i = 0; i < Chromosomes[one].Pumps.Count; i++)
                            {
                                int xc = random.Next(0, 10000);
                                if (xc < 5000)
                                {
                                    pump_list1.Add(CopyPump.Copy(Chromosomes[one].Pumps[i]));
                                    pump_list2.Add(CopyPump.Copy(Chromosomes[mem].Pumps[i]));
                                }
                                else
                                {
                                    pump_list1.Add(CopyPump.Copy(Chromosomes[mem].Pumps[i]));
                                    pump_list2.Add(CopyPump.Copy(Chromosomes[one].Pumps[i]));
                                }
                            }
                            chromsomes_new.Add(new PumpSchedulingChromosome(pump_list1,
                                Chromosomes[one].OnOffMutationRatio,
                                Chromosomes[one].SpeedMutationRatio)
                            {
                                SelectedRatio = Chromosomes[one].SelectedRatio
                            });
                            chromsomes_new.Add(new PumpSchedulingChromosome(pump_list2,
                                Chromosomes[mem].OnOffMutationRatio,
                                Chromosomes[mem].SpeedMutationRatio)
                            {
                                SelectedRatio = Chromosomes[mem].SelectedRatio
                            });
                        }
                        else
                        {
                            one = mem;
                        }
                    }
                }

            }

            Chromosomes.AddRange(chromsomes_new);
        }

        public void UpdateChromosomeSelectedRatio()
        {
            //按适应度设置选择率，1~10：100%，11~20：80%，21~40：60%，41~70：40%，71~90：20%，91~100：10%
            for (int i = 0; i < Chromosomes.Count; i++)
            {
                if (i >= 0 && i < 10)
                {
                    Chromosomes[i].SelectedRatio = 1.0f;
                }
                else
                if (i >= 10 && i < 20)
                {
                    Chromosomes[i].SelectedRatio = 0.8f;
                }
                else
                if (i >= 20 && i < 40)
                {
                    Chromosomes[i].SelectedRatio = 0.6f;
                }
                else
                if (i >= 40 && i < 70)
                {
                    Chromosomes[i].SelectedRatio = 0.4f;
                }
                else
                if (i >= 70 && i < 90)
                {
                    Chromosomes[i].SelectedRatio = 0.2f;
                }
                else
                if (i >= 90 && i < 100)
                {
                    Chromosomes[i].SelectedRatio = 0.1f;
                }
            }
        }
    }

    class PumpSchedulingChromosome
    {
        private Random random = new Random(unchecked((int)DateTime.Now.Ticks));
        public double SelectedRatio { get; set; } = 0.5;
        public double OnOffMutationRatio { get; set; } = 0.1;
        public double SpeedMutationRatio { get; set; } = 0.1;
        public double Fitness { get; set; }
        public double TotalFlow { get; set; }
        public double TotalPower { get; set; }
        public List<Pump> Pumps { get; private set; }

        public PumpSchedulingChromosome(List<Pump> pumps, double on_off_mutation_ratio, double speed_mutation_ratio)
        {
            Pumps = new List<Pump>();
            Pumps.AddRange(pumps);
            OnOffMutationRatio = on_off_mutation_ratio;
            SpeedMutationRatio = speed_mutation_ratio;
        }

        public void ChromosomeMutate()
        {
            foreach (Pump p in Pumps)
            {
                //所有水泵启停状态变异
                int on_off_r = random.Next(0, 10000);
                bool change_on_off_status = on_off_r <= OnOffMutationRatio * 10000;
                if (change_on_off_status)
                {
                    p.IsOpen = !p.IsOpen;
                }

                //变速泵转速变异
                if (p.IsVarFrequency)
                {
                    double speed = p.CurrentSpeed;
                    int r = random.Next(0, 100);
                    switch (r % 2)
                    {
                        case 0:
                            speed = speed + SpeedMutationRatio * (p.RatedParam.MaxSpeed - speed) * r / 100f;
                            if (speed > p.RatedParam.MaxSpeed)
                            {
                                speed = p.RatedParam.MaxSpeed;
                            }
                            break;
                        case 1:
                            speed = speed - SpeedMutationRatio * (speed - p.RatedParam.MinSpeed) * r / 100f;
                            if (speed < p.RatedParam.MinSpeed)
                            {
                                speed = p.RatedParam.MinSpeed;
                            }
                            break;
                    }
                    p.CurrentSpeed = speed;
                }
            }
        }
    }

    class CopyChromosome
    {
        public static PumpSchedulingChromosome Copy(PumpSchedulingChromosome chrom_in)
        {
            List<Pump> pumps = new List<Pump>();
            foreach (Pump p in chrom_in.Pumps)
            {
                pumps.Add(CopyPump.Copy(p));
            }
            return new PumpSchedulingChromosome(pumps, chrom_in.OnOffMutationRatio, chrom_in.SpeedMutationRatio)
            {
                Fitness = chrom_in.Fitness,
                SelectedRatio = chrom_in.SelectedRatio,
                TotalFlow = chrom_in.TotalFlow,
                TotalPower = chrom_in.TotalPower
            };
        }
    }
}
