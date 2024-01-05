using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PumpsSchedule
{
    internal class PumpCurve
    {
        private double m_a, m_b, m_c = 0.0;
        public List<CurvePoint> CurvePoints { get; }

        public PumpCurve(List<CurvePoint> curvePoints)
        {
            CurvePoints = new List<CurvePoint>();
            CurvePoints.AddRange(curvePoints);

            CalcFactor();
        }

        private void CalcFactor()
        {
            double tiny = 1e-6;

            double h0, h1, h2, h4, h5;
            double q0, q1, q2;
            double a, a1, b, c;

            if (CurvePoints.Count == 0)
            {
                throw new Exception("未定义水泵曲线");
            }
            else
            if (CurvePoints.Count == 1)
            {
                q0 = 0.0;
                q1 = CurvePoints[0].X;
                h1 = CurvePoints[0].Y;
                h0 = 1.33334 * h1;
                q2 = 2.0 * q1;
                h2 = 0.0;
            }
            else
            {
                q0 = CurvePoints[0].X;
                h0 = CurvePoints[0].Y;
                q1 = CurvePoints[1].X;
                h1 = CurvePoints[1].Y;
                q2 = CurvePoints[2].X;
                h2 = CurvePoints[2].Y;
            }

            a = h0;
            b = 0.0;
            c = 1.0;

            if (h0 < tiny
                || h0 - h1 < tiny
                || h1 - h2 < tiny
                || q1 - q0 < tiny
                || q2 - q1 < tiny)
            {
                throw new Exception("水泵曲线定义无效");
            }
            else
            {
                a = h0;

                //迭代计算，设置为50次提高精度，epanet2中为5次。
                for (int i = 0; i < 50; i++)
                {
                    h4 = a - h1;
                    h5 = a - h2;
                    c = Math.Log(h5 / h4) / Math.Log(q2 / q1);
                    if (c <= 0.0 || c > 20.0)
                    {
                        break;
                    }
                    b = -h4 / Math.Pow(q1, c);
                    if (b > 0.0)
                    {
                        break;
                    }
                    a1 = h0 - b * Math.Pow(q0, c);
                    if (Math.Abs(a1 - a) < 0.01)
                    {
                        break;
                    }
                    a = a1;
                }
            }

            m_a = a;
            m_b = b;
            m_c = c;
        }

        public double CalcHeadByFlow(double flow)
        {
            return m_a + m_b * Math.Pow(flow, m_c);
        }

        public double CalcFlowByHead(double head)
        {
            return Math.Pow((head - m_a) / m_b, 1.0 / m_c);
        }
    }

    internal class CurvePoint : IComparer<CurvePoint>
    {
        public double X { get; set; }
        public double Y { get; set; }

        public int Compare(CurvePoint x, CurvePoint y)
        {
            if (x.X > y.X)
            {
                return 1;
            }
            else
            if (x.X < y.X)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }

    internal class PumpEfficiencyCurve
    {
        public double Efficiency_Global { get; private set; } = 0.85;
        /// <summary>
        /// 效率曲线，表示流量-效率关系
        /// </summary>
        public List<CurvePoint> CurvePoints { get; private set; }
        public PumpEfficiencyCurve(double efficiency_global, List<CurvePoint> curvePoints)
        {
            Efficiency_Global = efficiency_global;
            CurvePoints = new List<CurvePoint>();
            if (curvePoints != null)
            {
                CurvePoints.AddRange(curvePoints);
                //排序
                CurvePoints.Sort(new CurvePoint());
            }
        }

        public double GetEfficiencyByFlow(double flow)
        {
            if (CurvePoints.Count == 0)
            {
                return Efficiency_Global;
            }
            else
            {
                for (int i = 0; i < CurvePoints.Count; i++)
                {
                    if (flow <= CurvePoints[i].X)
                    {
                        if (i == 0)
                        {
                            return CurvePoints[i].Y;
                        }
                        else
                        {
                            if (CurvePoints[i - 1].Y == CurvePoints[i].Y)
                            {
                                return CurvePoints[i].Y;
                            }
                            else
                            {
                                return (CurvePoints[i].Y - CurvePoints[i - 1].Y)
                                    * (flow - CurvePoints[i - 1].X)
                                    / (CurvePoints[i].X - CurvePoints[i - 1].X)
                                    + CurvePoints[i - 1].Y;
                            }
                        }
                    }
                    else
                    {
                        if (i == CurvePoints.Count - 1)
                        {
                            return CurvePoints[i].Y;
                        }
                    }
                }
            }

            return Efficiency_Global;
        }
    }
}
