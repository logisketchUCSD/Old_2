using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Ink;
using Sketch;

namespace FeatureSpace
{
    
    [Serializable]
    public class Intersection
    {
        #region INTERNALS

        /// <summary>
        /// Unique identification number for the intersection
        /// </summary>
        private Guid m_Id;

        /// <summary>
        /// Reference to the first substroke in the intersection
        /// </summary>
        private Substroke m_SubstrokeA;

        /// <summary>
        /// Reference to the second substroke in the intersection
        /// </summary>
        private Substroke m_SubstrokeB;

        /// <summary>
        /// Value indicating where on the first stroke the intersection occurs. 
        /// 0.0f = start, 1.0f = end
        /// </summary>
        private float m_aIntPt;

        /// <summary>
        /// Value indicating where on the second stroke the intersection occurs. 
        /// 0.0f = start, 1.0f = end
        /// </summary>
        private float m_bIntPt;

        private bool m_IsEndLineA;
        private bool m_IsEndLineB;

        #endregion


        #region CONSTRUCTORS

        /// <summary>
        /// Constructor for a specific intersection
        /// </summary>
        /// <param name="SSa">First Substroke</param>
        /// <param name="SSb">Second Substroke</param>
        /// <param name="aIntPt">Point along first stroke that the intersection occurs</param>
        /// <param name="bIntPt">Point along second stroke that the intersection occurs</param>
        public Intersection(Substroke SSa, Substroke SSb, float aIntPt, float bIntPt, bool aIsEndLine, bool bIsEndLine)
        {
            m_Id = Guid.NewGuid();
            m_SubstrokeA = SSa;
            m_SubstrokeB = SSb;
            m_aIntPt = aIntPt;
            m_bIntPt = bIntPt;
            m_IsEndLineA = aIsEndLine;
            m_IsEndLineB = bIsEndLine;
        }

        #endregion


        #region GETTERS

        /// <summary>
        /// Gets the type of intersection
        /// </summary>
        public string Type
        {
            get { return "None"; }
        }

        public float GetIntersectionPoint(Substroke stroke)
        {
            if (m_SubstrokeA == stroke)
                return m_aIntPt;
            else if (m_SubstrokeB == stroke)
                return m_bIntPt;
            else
                return -1.0f;
        }

        public float GetOtherStrokesIntersectionPoint(Substroke stroke)
        {
            if (m_SubstrokeA == stroke)
                return m_bIntPt;
            else if (m_SubstrokeB == stroke)
                return m_aIntPt;
            else
                return -1.0f;
        }

        /// <summary>
        /// Gets the point along first stroke that the intersection occurs
        /// </summary>
        public float IntersectionPointA
        {
            get { return m_aIntPt; }
        }

        /// <summary>
        /// Gets the point along first stroke that the intersection occurs
        /// </summary>
        public float IntersectionPointB
        {
            get { return m_bIntPt; }
        }

        /// <summary>
        /// Gets the points along both strokes that the intersection occurs
        /// </summary>
        public float[] IntersectionPoints
        {
            get { return new float[2] { m_aIntPt, m_bIntPt }; }
        }

        /// <summary>
        /// Gets the id# of the intersection
        /// </summary>
        public Guid Id
        {
            get { return m_Id; }
        }

        /// <summary>
        /// First Substroke
        /// </summary>
        public Substroke SubStrokeA
        {
            get { return m_SubstrokeA; }
        }

        /// <summary>
        /// Second Substroke
        /// </summary>
        public Substroke SubStrokeB
        {
            get { return m_SubstrokeB; }
        }

        #endregion
    }
}
