using System;
using System.Collections.Generic;
using System.Text;
using Sketch;

namespace FeatureSpace
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class ClosedPath
    {
        List<Substroke> strokes;
        System.Drawing.Rectangle boundingBox;
        Guid id;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="substrokes"></param>
        public ClosedPath(List<Substroke> substrokes)
        {
            id = Guid.NewGuid();
            strokes = substrokes;
            boundingBox = GetBoundingBox(strokes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        public ClosedPath(Substroke s)
        {
            id = Guid.NewGuid();
            strokes = new List<Substroke>();
            strokes.Add(s);
            boundingBox = GetBoundingBox(strokes);
        }

        /// <summary>
        /// 
        /// </summary>
        public ClosedPath()
        {
            id = Guid.NewGuid();
            strokes = new List<Substroke>();
            boundingBox = new System.Drawing.Rectangle();
        }

        /// <summary>
        /// If strokes inside each closed path match
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool Equals(ClosedPath path)
        {
            bool foundAll = true;

            foreach (Substroke s1 in this.strokes)
            {
                bool foundStroke = false;
                foreach (Substroke s2 in path.strokes)
                {
                    if (s1 == s2)
                    {
                        foundStroke = true;
                        break;
                    }
                }

                if (!foundStroke)
                {
                    foundAll = false;
                    break;
                }
            }

            return foundAll;
        }

        private System.Drawing.Rectangle GetBoundingBox(List<Substroke> strokes)
        {
            float maxX = float.MinValue;
            float minX = float.MaxValue;
            float maxY = float.MinValue;
            float minY = float.MaxValue;

            foreach (Substroke stroke in strokes)
            {
                Point[] points = stroke.Points;
                for (int i = 1; i < points.Length; i++)
                {
                    maxX = Math.Max(points[i].X, maxX);
                    minX = Math.Min(points[i].X, minX);
                    maxY = Math.Max(points[i].Y, maxY);
                    minY = Math.Min(points[i].Y, minY);
                }
            }
            return new System.Drawing.Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        public void AddStroke(Substroke s)
        {
            strokes.Add(s);
            boundingBox = GetBoundingBox(strokes);
        }

        /// <summary>
        /// 
        /// </summary>
        public List<Substroke> Substrokes
        {
            get { return strokes; }
        }

        /// <summary>
        /// 
        /// </summary>
        public System.Drawing.Rectangle BoundingBox
        {
            get { return boundingBox; }
        }
    }
}
