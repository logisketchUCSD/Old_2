using System;
using System.Collections.Generic;
using System.Text;
using Sketch;
using Utilities;

namespace FeatureSpace
{
    [Serializable]
    public class FeatureStrokePair
    {
        #region Member Variables and Private Structs

        /// <summary>
        /// A list of the calculated features, linked to string values indicating
        /// what feature it is.
        /// </summary>
        private Dictionary<string, Feature> m_Features;

        /// <summary>
        /// Passed in with constructor, tells the object what features to use
        /// </summary>
        private Dictionary<string, bool> m_FeaturesToUse;

        /// <summary>
        /// First Substroke
        /// </summary>
        private Substroke m_StrokeA;

        /// <summary>
        /// Second Substroke
        /// </summary>
        private Substroke m_StrokeB;

        /// <summary>
        /// All relevant distance between the two strokes
        /// </summary>
        private SubstrokeDistance m_Distances;

        /// <summary>
        /// All relevant overlaps between the two strokes
        /// </summary>
        private SubstrokeOverlap m_Overlaps;

        private StrokePair m_StrokePair;

        #endregion

        #region Constructors

        public FeatureStrokePair(Substroke s1, Substroke s2, StrokePair pair, Dictionary<string, bool> featureList)
        {
            m_StrokeA = s1;
            m_StrokeB = s2;
            m_StrokePair = pair;

            m_Distances = new SubstrokeDistance(s1, s2);
            m_Overlaps = new SubstrokeOverlap(s1, s2);

            m_FeaturesToUse = featureList;
            m_Features = new Dictionary<string, Feature>();

            AssignFeatureValues();
        }

        #endregion

        #region Methods

        private void AssignFeatureValues()
        {
            foreach (KeyValuePair<string, bool> pair in m_FeaturesToUse)
            {
                if (pair.Value)
                {
                    string key = pair.Key;
                    switch (key)
                    {
                        case "Minimum Distance":
                            MinimumDistance value = new MinimumDistance(m_Distances.Min);
                            m_Features.Add(key, value);
                            break;
                        case "Maximum Distance":
                            m_Features.Add(key, new MaximumDistance(m_Distances.Max));
                            break;
                        case "Centroid Distance":
                            m_Features.Add(key, new CentroidDistance(m_Distances.Avg));
                            break;
                        case "Horizontal Overlap":
                            m_Features.Add(key, new XOverlap(m_Overlaps.xOverlap));
                            break;
                        case "Vertical Overlap":
                            m_Features.Add(key, new YOverlap(m_Overlaps.yOverlap));
                            break;
                        case "Time Gap":
                            m_Features.Add(key, new TimeGap(m_Distances.Time));
                            break;
                        case "Distance Ratio A":
                            //m_Features.Add(key, new DistanceRatioA());
                            break;
                        case "Distance Ratio B":
                            //m_Features.Add(key, new DistanceRatioB());
                            break;
                    }
                    if (key == "")
                        m_Features.Add(pair.Key, new Feature());
                }
            }
        }

        private int FeatureCount
        {
            get
            {
                int count = 0;
                foreach (KeyValuePair<string, bool> pair in m_FeaturesToUse)
                {
                    if (pair.Value)
                        count++;
                }

                return count;
            }
        }


        #endregion

        #region Getters

        public Dictionary<string, Feature> Features
        {
            get { return m_Features; }
            set { m_Features = value; }
        }

        public Substroke StrokeA
        {
            get { return m_StrokeA; }
        }

        public Substroke StrokeB
        {
            get { return m_StrokeB; }
        }

        public StrokePair StrokePair
        {
            get { return m_StrokePair; }
        }

        public SubstrokeDistance SubstrokeDistance
        {
            get { return m_Distances; }
        }

        public SubstrokeOverlap SubstrokeOverlap
        {
            get { return m_Overlaps; }
        }

        public double[] Values(double minDistanceA, double minDistanceB)
        {
            int count = FeatureCount;
            double[] values = new double[count];

            int n = 0;
            string key = "Minimum Distance";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                values[n] = m_Distances.Min / Compute.PairwiseDistanceFactor;
            n++;

            key = "Time Gap";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                values[n] = (double)m_Distances.Time / Compute.PairwiseTimeFactor;
            n++;

            key = "Horizontal Overlap";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                values[n] = m_Overlaps.xOverlap / Compute.PairwiseDistanceFactor;
            n++;

            key = "Vertical Overlap";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                values[n] = m_Overlaps.yOverlap / Compute.PairwiseDistanceFactor;
            n++;

            key = "Maximum Distance";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                values[n] = m_Distances.Max / Compute.PairwiseDistanceFactor;
            n++;

            key = "Centroid Distance";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                values[n] = m_Distances.Avg / Compute.PairwiseDistanceFactor;
            n++;

            key = "RatioA";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
            {
                double denominator = (1.0 + m_Distances.Min / Compute.PairwiseDistanceFactor);
                double numerator = (1.0 + minDistanceA / Compute.PairwiseDistanceFactor);
                double ratio = numerator / denominator;
                values[n] = ratio;
            }
            n++;

            key = "RatioB";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
            {
                double denominator = (1.0 + m_Distances.Min / Compute.PairwiseDistanceFactor);
                double numerator = (1.0 + minDistanceB/ Compute.PairwiseDistanceFactor);
                double ratio = numerator / denominator;
                values[n] = ratio;
            }

            return values;
        }

        #endregion
    }


}
