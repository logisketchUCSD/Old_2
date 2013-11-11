using System;
using System.Collections.Generic;
using System.Text;

namespace FeatureSpace
{
    [Serializable]
    public class MinimumDistance : Feature
    {
        public MinimumDistance(double distance)
            : base("Minimum Distance", distance, Scope.Pair_Static)
        { }
    }

    [Serializable]
    public class MaximumDistance : Feature
    {
        public MaximumDistance(double distance)
            : base("Maximum Distance", distance, Scope.Pair_Static)
        { }
    }

    [Serializable]
    public class CentroidDistance : Feature
    {
        public CentroidDistance(double distance)
            : base("Centroid Distance", distance, Scope.Pair_Static)
        { }
    }

    [Serializable]
    public class XOverlap : Feature
    {
        public XOverlap(double distance)
            : base("X-Overlap", distance, Scope.Pair_Static)
        { }
    }

    [Serializable]
    public class YOverlap : Feature
    {
        public YOverlap(double distance)
            : base("Y-Overlap", distance, Scope.Pair_Static)
        { }
    }

    [Serializable]
    public class TimeGap : Feature
    {
        public TimeGap(double distance)
            : base("Time Gap", distance, Scope.Pair_Static)
        {
            m_Normalizer = 1.0;
        }
    }

    [Serializable]
    public class DistanceRatioA : Feature
    {
        public DistanceRatioA(double distanceRatio)
            : base("Distance Ratio A", distanceRatio, Scope.Pair_Dynamic)
        {
            m_Normalizer = 1.0;
        }

        public DistanceRatioA(double distance, double MinDistance)
            : base("Distance Ratio A", distance, Scope.Pair_Dynamic)
        {
            double denominator = (1.0 + distance / Compute.PairwiseDistanceFactor);
            double numerator = (1.0 + MinDistance / Compute.PairwiseDistanceFactor);
            m_Value = numerator / denominator;

            m_Normalizer = 1.0;
        }
    }

    [Serializable]
    public class DistanceRatioB : Feature
    {
        public DistanceRatioB(double distanceRatio)
            : base("Distance Ratio B", distanceRatio, Scope.Pair_Dynamic)
        {
            m_Normalizer = 1.0;
        }

        public DistanceRatioB(double distance, double MinDistance)
            : base("Distance Ratio B", distance, Scope.Pair_Dynamic)
        {
            double denominator = (1.0 + distance / Compute.PairwiseDistanceFactor);
            double numerator = (1.0 + MinDistance / Compute.PairwiseDistanceFactor);
            m_Value = numerator / denominator;

            m_Normalizer = 1.0;
        }
    }
}
