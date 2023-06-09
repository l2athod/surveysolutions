using System;

namespace WB.Core.SharedKernels.DataCollection.V2.CustomFunctions
{
    internal class LMS
    {
        public double L;
        public double M;
        public double S;

        internal static double ScoreLmsModel(double x, LMS p)
        {
            // there are no L==0.0 models for any of the indicators, but 
            // will keep this branch in accordance with the formal definition.
            if (p.L == 0.0)
                return Math.Log(x / p.M) / p.S;

            return (Math.Pow(x / p.M, p.L) - 1) / (p.L * p.S);
        }

        internal LMS(double l, double m, double s)
        {
            L = l;
            M = m;
            S = s;
        }

        public static double GetScore(long? ageMonths, bool? isBoy, double? measurement, LMS[] boys, LMS[] girls)
        {
            if (ageMonths.HasValue == false)
                throw new ArgumentNullException("ageMonths", "Error. Age may not be null.");

            if (isBoy.HasValue == false)
                throw new ArgumentNullException("isBoy", "Error. Gender attribute may not be null.");

            if (ageMonths < 0 || ageMonths >= boys.Length)
            {
                // lengths of boys and girls arrays must always be the same!
                throw new ArgumentOutOfRangeException("ageMonths", "Error. Age is out of range");
            }

            if ((!measurement.HasValue) || (measurement <= 0))
                throw new ArgumentException("Error. Measurement must be positive.", "measurement");

            var model = isBoy.Value ? boys[ageMonths.Value] : girls[ageMonths.Value];
            return ScoreLmsModel(measurement.Value, model);
        }
    }
}