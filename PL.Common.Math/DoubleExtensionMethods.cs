using System;

namespace PL.Common.Math
{
    public static class DoubleExtensionMethods
    {
        public static bool IsAboutEqualTo(this double? aX, double? aY)
        {
            if (aX.HasValue && aY.HasValue)
                return aX.Value.IsAboutEqualTo(aY.Value, 0.00001d);

            return !aX.HasValue && !aY.HasValue;
        }

        // Based on https://floating-point-gui.de/errors/comparison/
        internal static bool IsAboutEqualTo(this double aX, double aY, double aEpsilon)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator, as we are trained professionals in this method.

            // Const found in Java code (http://www.docjar.com/html/api/java/lang/Double.java.html)
            const double minNormal = 2.2250738585072014E-308d;
            var absX = System.Math.Abs(aX);
            var absY = System.Math.Abs(aY);
            var diff = System.Math.Abs(aX - aY);

            if (aX == aY) // shortcut, handles infinities
            {
                return true;
            }

            if (aX == 0 || aY == 0 || absX + absY < minNormal)
            {
                // aX or aY is zero or both are extremely close to it
                // relative error is less meaningful here
                return diff < aEpsilon * minNormal;
            }

            // use relative error
            return diff / System.Math.Max(absX, absY) < aEpsilon;

            // ReSharper restore CompareOfFloatsByEqualityOperator
        }

        // General, strict, equality comparison.
        public static bool IsAboutEqualTo(this double aX, double aY)
        {
            // A double has a precision of about 16 significant figures (52 bits).
            // For equality we use an epsilon of 1e14 so 2 significant figures are allowed
            // to be lost due to rounding.
            return IsAboutEqualTo(aX, aY, 1e-14);
        }
    }
}
