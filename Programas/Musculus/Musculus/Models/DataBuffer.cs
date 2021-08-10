using System;
using System.Collections;
using System.Collections.Generic;

namespace Musculus.Models
{
    public class DataBuffer
    {
        public List<double> Data = new List<double>();
        public List<double> Time = new List<double>();
        public int Length;

        // Biquad filter Transposed Direct Form Type II
        // 2nd order High-pass Elliptic filter
        // Scipy design command:
        //      coef = sg.ellip(2, 5, 60, 5, 'high', output='sos', fs=400)
        //      a = coef[3:]
        //      b = coef[:3]
        private readonly double a1 = -1.93265474;
        private readonly double a2 = 0.94253372;
        private readonly double b0 = 0.54479712;
        private readonly double b1 = -1.08958437;
        private readonly double b2 = 0.54479712;

        private double x_old1 = 0.0;
        private double x_old2 = 0.0;
        private double y_old1 = 0.0;
        private double y_old2 = 0.0;

        public DataBuffer(int bufferLength)
        {
            Length = bufferLength;
        }

        private double HighPassFilter(double input)
        {
            double s2 = b2 * x_old2 - a2 * y_old2; // -2 delay
            double s1 = s2 + b1 * x_old1 - a1 * y_old1; // -1 delay
            double output = b0 * input + s1; // 0 delay

            x_old2 = x_old1;
            x_old1 = input;

            y_old2 = y_old1;
            y_old1 = output;

            return output;
        }

        public void Add(double newValue, double dt = 1.0, bool high_pass_filter = false, double rescale = 1.0, double offset = 0.0)
        {
            if (high_pass_filter)
            {
                newValue = HighPassFilter(newValue);
            }
            Data.Add(rescale * newValue + offset);

            if (Time.Count > 0)
            {
                Time.Add(Time[Time.Count-1] + dt);
            }
            else
            {
                Time.Add(0.0);
            }

            if (Data.Count > Length)
            {
                Data.RemoveAt(0);
                Time.RemoveAt(0);
            }
        }

        public double CalculateAggregateFeature(Func<double[], double[], double> FeaureCalculator)
        {
            return FeaureCalculator(Time.ToArray(), Data.ToArray());
        }

        public void Reset()
        {
            Time.Clear();
            Data.Clear();
        }

        public void ResetFilter()
        {
            x_old2 = 0.0;
            x_old1 = 0.0;
            y_old2 = 0.0;
            y_old1 = 0.0;
        }
    }
}
