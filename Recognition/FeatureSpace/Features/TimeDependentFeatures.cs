using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace FeatureSpace
{
    /// <summary>
    /// Feature for the average pen speed throughout the stroke.
    /// This feature is calculated using the total arc length of
    /// the stroke divided by the total time to draw the stroke
    /// rather than the average of the pen speeds between
    /// consecutive points in the stroke.
    /// </summary>
    [Serializable]
    public class AvgSpeed : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="points"></param>
        /// <param name="ArcLength"></param>
        public AvgSpeed(Sketch.Point[] points, double ArcLength)
            : base("Average Pen Speed", Scope.Single)
        {
            double time = (double)(points[points.Length - 1].Time - points[0].Time);
            if (time >= 0.0)
                m_Value = ArcLength / time;
            else
                m_Value = 0.0;
        }

        public AvgSpeed(double value)
            : base("Average Pen Speed", Scope.Single)
        {
            m_Value = value;
        }
    }

    /// <summary>
    /// Feature for the maximum pen speed during the stroke
    /// </summary>
    [Serializable]
    public class MaxSpeed : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Speeds">speed profile for stroke</param>
        public MaxSpeed(double[] Speeds)
            : base("Maximum Pen Speed", Scope.Single)
        {
            m_Value = Compute.Max(Speeds);
        }

        public MaxSpeed(double value)
            : base("Maximum Pen Speed", Scope.Single)
        {
            m_Value = value;
        }
    }

    /// <summary>
    /// Feature for the minimum pen speed during the stroke
    /// </summary>
    [Serializable]
    public class MinSpeed : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Speeds">Speed profile for stroke</param>
        public MinSpeed(double[] Speeds)
            : base("Minimum Pen Speed", Scope.Single)
        {
            int start = Math.Min(Compute.Pts_From_End, Speeds.Length);
            int length = Math.Max(0, Speeds.Length - 2 * Compute.Pts_From_End);
            double[] validSpeeds = new double[length];
            Array.Copy(Speeds, start, validSpeeds, 0, length);
            m_Value = Compute.Min(validSpeeds);
        }

        public MinSpeed(double value)
            : base("Minimum Pen Speed", Scope.Single)
        {
            m_Value = value;
        }
    }

    /// <summary>
    /// Finds the difference between the maximum and minimum
    /// pen speeds in the speed profile
    /// </summary>
    [Serializable]
    public class MaxMinDiffSpeed : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Speeds">Speed profile for stroke</param>
        public MaxMinDiffSpeed(double[] Speeds)
            : base("Difference Between Maximum and Minimum Pen Speed", Scope.Single)
        {
            double max = Compute.Max(Speeds);
            double min = Compute.Min(Speeds);
            m_Value = max - min;
        }
    }

    /// <summary>
    /// Feature which indicates the time gap between the 
    /// current stroke and previous stroke (temporally)
    /// </summary>
    [Serializable]
    public class TimeToPrevious : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sketch"></param>
        /// <param name="stroke"></param>
        public TimeToPrevious()
            : base("Time to Previous Stroke", Scope.Multiple_Static)
        {
            m_Normalizer = Compute.StrokeTimeNormalizer;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sketch"></param>
        /// <param name="stroke"></param>
        public TimeToPrevious(SortedList<ulong, Sketch.Substroke> order, Sketch.Substroke stroke)
            : base("Time to Previous Stroke", Scope.Multiple_Static)
        {
            m_Normalizer = Compute.StrokeTimeNormalizer;

            int index = order.IndexOfValue(stroke);
            if (index == 0)
                m_Value = 0.0;
            else
            {
                ulong current = 0;
                ulong endPrevious = 0;

                foreach (KeyValuePair<ulong, Sketch.Substroke> pair in order)
                {
                    if (pair.Value == stroke)
                        current = pair.Key;
                    else if (order.IndexOfKey(pair.Key) == index - 1)
                    {
                        Sketch.Point[] points = pair.Value.Points;
                        endPrevious = points[points.Length - 1].Time;
                    }
                }

                m_Value = (double)(current - endPrevious);
            }
        }
    }

    /// <summary>
    /// Feature which indicates the time gap between the 
    /// current stroke and next stroke (temporally)
    /// </summary>
    [Serializable]
    public class TimeToNext : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public TimeToNext()
            : base("Time to Next Stroke", Scope.Multiple_Static)
        {

            m_Normalizer = Compute.StrokeTimeNormalizer;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sketch"></param>
        /// <param name="stroke"></param>
        public TimeToNext(SortedList<ulong, Sketch.Substroke> order, Sketch.Substroke stroke)
            : base("Time to Next Stroke", Scope.Multiple_Static)
        {
            m_Normalizer = Compute.StrokeTimeNormalizer;

            int index = order.IndexOfValue(stroke);
            if (index == order.Count - 1)
                m_Value = 0.0;
            else
            {
                ulong endCurrent = 0;
                ulong startNext = 0;

                foreach (KeyValuePair<ulong, Sketch.Substroke> pair in order)
                {
                    if (pair.Value == stroke)
                    {
                        Sketch.Point[] points = pair.Value.Points;
                        endCurrent = points[points.Length - 1].Time;
                    }
                    else if (order.IndexOfKey(pair.Key) == index + 1)
                        startNext = pair.Key;
                }

                m_Value = (double)(startNext - endCurrent);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nextStrokesTimeToPrevious">Reuse value from 'next' stroke's feature</param>
        public TimeToNext(double nextStrokesTimeToPrevious)
            : base("Time to Next Stroke", Scope.Multiple_Static)
        {
            m_Normalizer = Compute.StrokeTimeNormalizer;

            m_Value = nextStrokesTimeToPrevious;
        }
    }

    /// <summary>
    /// Feature indicating the time taken to draw the stroke
    /// </summary>
    [Serializable]
    public class StrokeTime : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="stroke"></param>
        public StrokeTime(Sketch.Point[] points)
            : base("Time to Draw Stroke", Scope.Single)
        {
            m_Normalizer = Compute.StrokeTimeNormalizer;
            m_Value = (double)(points[points.Length - 1].Time - points[0].Time);
        }
    }
}
