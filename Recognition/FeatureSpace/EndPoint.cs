using System;
using System.Collections.Generic;
using System.Text;
using Sketch;

namespace FeatureSpace
{
    [Serializable]
    public class EndPoint
    {
        private Substroke m_Stroke;
        private int m_End;
        private Point m_Pt;
        private List<EndPoint> m_AttachedEndpoints;

        public EndPoint(Substroke stroke, int end)
        {
            m_Stroke = stroke;
            m_End = end;
            if (end == 1)
                m_Pt = m_Stroke.Points[0];
            else if (end == 2)
                m_Pt = m_Stroke.Points[m_Stroke.Points.Length - 1];
            else
                m_Pt = new Point();
            m_AttachedEndpoints = new List<EndPoint>();
        }

        public void AddAttachment(EndPoint endPoint)
        {
            m_AttachedEndpoints.Add(endPoint);
        }

        public Substroke Stroke
        {
            get { return m_Stroke; }
        }

        public Point Point
        {
            get { return m_Pt; }
        }

        public List<EndPoint> Attachments
        {
            get { return m_AttachedEndpoints; }
        }

        public int End
        {
            get { return m_End; }
        }
    }
}
