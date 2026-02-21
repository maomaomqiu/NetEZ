using System;
using System.Collections.Generic;
using System.Text;

namespace NetEZ.Utility.Algorithm
{
    public class Regression
    {
        static double ArrayAverage(double[] input)
        {
            if (input == null || input.Length < 1)
                return 0;

            double sum = 0;
            foreach (double val in input)
            {
                sum += val;
            }

            return sum / input.Length;
        }

        /// <summary>
        /// 计算线性回归趋势线的a、b常数
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool LinearCalc(double[] x, double[] y, out double a, out double b)
        {
            if (x == null || x.Length < 2 || y == null || x.Length != y.Length)
            {
                a = b = 0;
                return false;
            }

            double avgX = ArrayAverage(x);
            double avgY = ArrayAverage(y);

            double numerator = 0, denominator = 0;
            for (int i = 0; i < x.Length; i++)
            {
                numerator += x[i] * y[i];
                denominator += x[i] * x[i];
            }

            numerator -= x.Length * avgX * avgY;
            denominator -= x.Length * avgX * avgX;

            b = numerator / denominator;
            a = avgY - b * avgX;

            return true;
        }
    }
}
