using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Sketch;

namespace FeatureSpace
{
    [Serializable]
    public class IntersectionPair
    {
        #region Member Variables

        /// <summary>
        /// First Substroke
        /// </summary>
        private Substroke m_SubstrokeA;

        private List<Line> m_LinesA;

        private RectangleF m_BoxA;

        /// <summary>
        /// Second Substroke
        /// </summary>
        private Substroke m_SubstrokeB;

        private List<Line> m_LinesB;

        private RectangleF m_BoxB;

        /// <summary>
        /// List of intersections for two strokes
        /// </summary>
        private List<Intersection> m_Intersections;

        /// <summary>
        /// Unique Identification number for the intersection pair
        /// </summary>
        private Guid m_Id;

        #endregion


        #region Constructors

        /// <summary>
        /// Constructor which creates an intersection pair between 2 strokes
        /// and then populates the list of intersections between the strokes
        /// </summary>
        /// <param name="ssA">First Substroke</param>
        /// <param name="ssB">Second Substroke</param>
        public IntersectionPair(Substroke ssA, Substroke ssB, RectangleF boxA, RectangleF boxB, List<Line> linesA, List<Line> linesB)
        {
            m_Id = Guid.NewGuid();
            m_SubstrokeA = ssA;
            m_BoxA = boxA;
            m_LinesA = linesA;
            m_SubstrokeB = ssB;
            m_BoxB = boxB;
            m_LinesB = linesB;
            m_Intersections = Compute.Intersect(m_SubstrokeA, m_SubstrokeB, m_LinesA, m_LinesB, m_BoxA, m_BoxB, 0.0f);
        }

        #endregion

        #region Methods

        

        #endregion


        #region GETTERS

        public List<Intersection> Intersections
        {
            get { return m_Intersections; }
        }

        public Sketch.Substroke StrokeA
        {
            get { return m_SubstrokeA; }
        }

        public Sketch.Substroke StrokeB
        {
            get { return m_SubstrokeB; }
        }

        public Guid Id
        {
            get { return m_Id; }
        }

        public bool IsEmpty
        {
            get
            {
                if (m_Intersections.Count == 0)
                    return true;
                else
                    return false;
            }
        }

        public bool Contains(Guid substrokeID)
        {
            if (m_SubstrokeA.Id == substrokeID || m_SubstrokeB.Id == substrokeID)
                return true;
            else
                return false;
        }

        public List<Intersection> EndPtIntersections
        {
            get
            {
                List<Intersection> list = new List<Intersection>();

                foreach (Intersection intersection in m_Intersections)
                {
                    float[] ptsIntersection = intersection.IntersectionPoints;
                    if (IsEndPtIntersection(ptsIntersection))
                        list.Add(intersection);
                }

                return list;
            }
        }

        private bool IsEndPtIntersection(float[] intersectionPts)
        {
            if (IsEndPt(intersectionPts[0]) && IsEndPt(intersectionPts[1]))
                return true;
            else
                return false;
        }

        private bool IsEndPt(float intersectionPt)
        {
            float threshold = Compute.THRESHOLD;
            if (intersectionPt > 0.0f - threshold && intersectionPt < 0.0f + threshold ||
                intersectionPt > 1.0f - threshold && intersectionPt < 1.0f + threshold)
                return true;
            else
                return false;
        }

        #endregion
    }
}
