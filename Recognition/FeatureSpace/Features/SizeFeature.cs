using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace FeatureSpace
{
    /// <summary>
    /// This feature is the total length the pen travels during a stroke
    /// </summary>
    [Serializable]
    public class InkLength : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="points">Points in the Stroke</param>
        public InkLength(Sketch.Point[] points)
            : base("Arc Length", Scope.Single)
        {
            double length = 0.0;

            for (int i = 1; i < points.Length; i++)
                length += Compute.EuclideanDistance(points[i - 1], points[i]);

            m_Value = length;
        }

        public InkLength(double value)
            : base("Arc Length", Scope.Single)
        {
            m_Value = value;
        }
    }


    /// <summary>
    /// This feature is the width of the stroke's axially aligned bounding box
    /// </summary>
    [Serializable]
    public class Width : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="box">Axially Aligned bounding box for the stroke</param>
        public Width(System.Drawing.RectangleF box)
            : base("Bounding Box Width", Scope.Single)
        {
            m_Value = (double)box.Width;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="box">Axially Aligned bounding box for the stroke</param>
        public Width(System.Drawing.Rectangle box)
            : base("Bounding Box Width", Scope.Single)
        {
            m_Value = (double)box.Width;
        }
    }


    /// <summary>
    /// This feature is the height of the stroke's axially aligned bounding box
    /// </summary>
    [Serializable]
    public class Height : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="box">Axially Aligned bounding box for the stroke</param>
        public Height(System.Drawing.RectangleF box)
            : base("Bounding Box Height", Scope.Single)
        {
            m_Value = (double)box.Height;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="box">Axially Aligned bounding box for the stroke</param>
        public Height(System.Drawing.Rectangle box)
            : base("Bounding Box Height", Scope.Single)
        {
            m_Value = (double)box.Height;
        }
    }


    /// <summary>
    /// This feature is the area of the stroke's axially aligned bounding box
    /// </summary>
    [Serializable]
    public class Area : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="box">Axially Aligned bounding box for the stroke</param>
        public Area(System.Drawing.RectangleF box)
            : base("Bounding Box Area", Scope.Single)
        {
            m_Value = (double)(box.Height * box.Width);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="box">Axially Aligned bounding box for the stroke</param>
        public Area(System.Drawing.Rectangle box)
            : base("Bounding Box Area", Scope.Single)
        {
            m_Value = (double)(box.Height * box.Width);
        }
    }


    /// <summary>
    /// This feature is the ratio of end point distances
    /// to the total length of the stroke.
    /// This gives an indication of the straightness of the stroke.
    /// </summary>
    [Serializable]
    public class EndPt2LengthRatio : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="points">Points in stroke</param>
        /// <param name="ArcLength">Total Arc Length of stroke</param>
        public EndPt2LengthRatio(Sketch.Point[] points, double ArcLength)
            : base("End Point to Arc Length Ratio", Scope.Single)
        {
            m_Normalizer = 1.0;

            if (ArcLength > 0.0)
                m_Value = Compute.EuclideanDistance(points[0], points[points.Length - 1]) / ArcLength;
            else
                m_Value = 0.0;            
        }
    }

    /// <summary>
    /// This feature is the ratio of end point distances
    /// to the total length of the stroke.
    /// This gives an indication of the straightness of the stroke.
    /// </summary>
    [Serializable]
    public class SelfEnclosing : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="points">Points in stroke</param>
        /// <param name="ArcLength">Total Arc Length of stroke</param>
        public SelfEnclosing(Sketch.Point[] points, double ArcLength)
            : base("Self Enclosing", Scope.Single)
        {
            m_Normalizer = 1.0;

            if (ArcLength > 0.0)
            {
                double d = Compute.EuclideanDistance(points[0], points[points.Length - 1]) / ArcLength;
                if (d < (double)Compute.THRESHOLD)
                    m_Value = 1.0;
                else
                    m_Value = 0.0;
            }
            else
                m_Value = 0.0;
        }
    }


    /// <summary>
    /// This feature gives the density of ink in the stroke
    /// and is calculated by taking the square of the arc
    /// length and dividing my the area of the bounding box.
    /// </summary>
    [Serializable]
    public class PathDensity : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ArcLength">Total Arc Length of the stroke</param>
        /// <param name="Area">Area of the stroke's bounding box</param>
        public PathDensity(double ArcLength, double Area)
            : base("Path Density", Scope.Single)
        {
            m_Normalizer = Compute.PathDensityNormalizer;

            if (Area > 0.0)
                m_Value = Math.Pow(ArcLength, 2.0) / Area;
            else
                m_Value = 0.0;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ArcLength">Total Arc Length of the stroke</param>
        /// <param name="Area">Area of the stroke's bounding box</param>
        public PathDensity(double ArcLength, double width, double height)
            : base("Path Density", Scope.Single)
        {
            m_Normalizer = Compute.PathDensityNormalizer;

            
            double length = Math.Max(width, height);
            double Area = length * length;

            if (Area > 0.0)
                m_Value = Math.Pow(ArcLength, 2.0) / Area;
            else
                m_Value = 0.0;
        }
    }
}
