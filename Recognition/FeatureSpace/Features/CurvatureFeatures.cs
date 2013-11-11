using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace FeatureSpace
{
    /// <summary>
    /// Feature indicating the total angle traversed by the stroke
    /// by taking the straight sum of the theta values
    /// </summary>
    [Serializable]
    public class SumTheta : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="thetas"></param>
        public SumTheta(double[] thetas)
            : base("Sum of Thetas", Scope.Single)
        {
            m_Normalizer = Compute.CurvatureNormalizer;

            m_Value = Compute.Sum(thetas);
        }
    }


    /// <summary>
    /// Feature indicating the total angle traversed by the stroke
    /// by taking the sume of the absolute values of the thetas
    /// </summary>
    [Serializable]
    public class SumAbsTheta : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="thetas"></param>
        public SumAbsTheta(double[] thetas)
            : base("Sum of Abs Value of Thetas", Scope.Single)
        {
            m_Normalizer = Compute.CurvatureNormalizer;

            m_Value = Compute.SumAbs(thetas);
        }
    }


    /// <summary>
    /// Feature indicating the total angle traversed by the stroke
    /// by taking the sum of the squared values of the thetas
    /// </summary>
    [Serializable]
    public class SumSquaredTheta : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="thetas"></param>
        public SumSquaredTheta(double[] thetas)
            : base("Sum of Squared Thetas", Scope.Single)
        {
            m_Normalizer = Compute.CurvatureNormalizer;

            m_Value = Compute.SumSquared(thetas);
        }
    }


    /// <summary>
    /// Feature indicating the total angle traversed by the stroke
    /// by taking the sum of the square-roots of the theta values
    /// </summary>
    [Serializable]
    public class SumSqrtTheta : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="thetas"></param>
        public SumSqrtTheta(double[] thetas)
            : base("Sum of Sqrt of Thetas", Scope.Single)
        {
            m_Normalizer = Compute.CurvatureNormalizer;

            m_Value = Compute.SumSqrt(thetas);
        }
    }
}
