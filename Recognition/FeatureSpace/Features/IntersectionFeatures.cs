using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace FeatureSpace
{
    [Serializable]
    public class NumLLIntersection : Feature
    {
        public NumLLIntersection(int value)
            : base("Number of 'LL' Intersections", Scope.Multiple_Dynamic)
        {
            m_Normalizer = Compute.IntersectionNormalizer;

            m_Value = (double)value;
        }
    }

    [Serializable]
    public class NumXXIntersection : Feature
    {
        public NumXXIntersection(int value)
            : base("Number of 'XX' Intersections", Scope.Multiple_Dynamic)
        {
            m_Normalizer = Compute.IntersectionNormalizer;

            m_Value = (double)value;
        }
    }

    [Serializable]
    public class NumLXIntersection : Feature
    {
        public NumLXIntersection(int value)
            : base("Number of 'LX' Intersections", Scope.Multiple_Dynamic)
        {
            m_Normalizer = Compute.IntersectionNormalizer;

            m_Value = (double)value;
        }
    }

    [Serializable]
    public class NumXLIntersection : Feature
    {
        public NumXLIntersection(int value)
            : base("Number of 'XL' Intersections", Scope.Multiple_Dynamic)
        {
            m_Normalizer = Compute.IntersectionNormalizer;

            m_Value = (double)value;
        }
    }

    [Serializable]
    public class NumSelfIntersection : Feature
    {
        public NumSelfIntersection(Sketch.Point[] points)
            : base("Number of Self Intersections", Scope.Single)
        {
            m_Normalizer = Compute.IntersectionNormalizer;

            int intersections = 0;

            for (int i = 1; i < points.Length; i++)
            {
                for (int j = 1; j < i; j++)
                {
                    if (intersects(points[j], points[j - 1], points[i], points[i - 1]))
                        intersections++;
                }
            }

            m_Value = intersections;
        }

        /// <summary>
        /// Checks whether there is an intersection between points a,b and c,d
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        private bool intersects(Sketch.Point a, Sketch.Point b, Sketch.Point c, Sketch.Point d)
        {
            double m1 = (double)(b.Y - a.Y) / (double)(b.X - a.X);
            double b1 = (double)a.Y - (m1 * (double)a.X);
            double m2 = (double)(d.Y - c.Y) / (double)(d.X - c.X);
            double b2 = (double)c.Y - (m2 * (double)c.X);

            double x = (b2 - b1) / (m1 - m2);
            double y = m1 * x + b1;

            if (((a.X < x && b.X > x) || (a.X > x && b.X < x))
                && ((c.X < x && d.X > x) || (c.X > x && d.X < x))
                && ((a.Y < y && b.Y > y) || (a.Y > y && b.Y < y))
                && ((c.Y < y && d.Y > y) || (c.Y > y && d.Y < y)))
                return true;
            else
                return false;
        }
    }

    
}
