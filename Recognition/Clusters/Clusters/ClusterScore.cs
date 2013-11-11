using System;
using System.Collections.Generic;
using System.Text;
using ImageRecognizer;
using Utilities;

namespace Clusters
{
    [Serializable]
    public class ClusterScore
    {
        #region Member Variables

        /// <summary>
        /// Score which takes into account the types of strokes used,
        /// values from the image-score, completeness of the cluster, 
        /// etc.
        /// </summary>
        double m_OverallScore;

        /// <summary>
        /// 
        /// </summary>
        //ImageScore m_ImageScore;
        SortedList<int, ImageScore> m_AllResults;

        #endregion

        #region Constructors

        public ClusterScore(Dictionary<string, List<SymbolRank>> clusterResults)
        {
            m_AllResults = new SortedList<int, ImageScore>();
            List<SymbolRank> rank;
            bool found = clusterResults.TryGetValue("Fusio", out rank);
            if (found)
            {
                for (int i = 0; i < rank.Count; i++)
                    m_AllResults.Add(i, new ImageScore(rank[i]));

                ImageScore top;
                bool foundTop = m_AllResults.TryGetValue(0, out top);
                if (foundTop)
                    m_OverallScore = top.FusionScore;
            }
        }

        public ClusterScore(List<ImageScore> imageScores)
        {
            m_AllResults = new SortedList<int, ImageScore>();
            for (int i = 0; i < imageScores.Count; i++)
                m_AllResults.Add(i, imageScores[i]);

            if (imageScores.Count > 0)
                m_OverallScore = imageScores[0].FusionScore;
        }

        #endregion

        public double Score
        {
            get { return m_OverallScore; }
        }

        public SortedList<int, ImageScore> AllImageResults
        {
            get { return m_AllResults; }
        }

        public ImageScore GetImageResult(int rank)
        {
            if (m_AllResults.ContainsKey(rank))
                return m_AllResults[rank];
            else
                return null;
        }

        public ImageScore TopMatch
        {
            get
            {
                List<ImageScore> scores = new List<ImageScore>(m_AllResults.Values);
                if (scores.Count > 0)
                    return scores[0];
                else
                    return null;
            }
        }
    }
}
