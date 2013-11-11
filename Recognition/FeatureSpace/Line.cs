using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;

namespace FeatureSpace
{
    [DebuggerDisplay("X1 = {endpoint1.X}, Y1 = {endpoint1.Y}, X2 = {endpoint2.X}, Y2 = {endpoint2.Y}, m = {slope}, b = {intercept}")]
    [Serializable]
    public class Line
    {
        #region Member Variables
        private float slope;
        private float intercept;
        private PointF endpoint1;
        private PointF endpoint2;
        private bool isEndLine;
        private Guid Id;
        #endregion


        #region Constructors
        /// <summary>
        /// Main Constructor, takes two System.Drawing.PointF points.
        /// </summary>
        /// <param name="p1">First point</param>
        /// <param name="p2">Second point</param>
        public Line(PointF p1, PointF p2)
        {
            this.Id = Guid.NewGuid();
            this.endpoint1 = p1;
            this.endpoint2 = p2;
            float[] temp = computeLine(p1, p2);
            this.slope = temp[0];
            this.intercept = temp[1];
            this.isEndLine = false;
        }

        /// <summary>
        /// Main Constructor, takes two System.Drawing.PointF points.
        /// </summary>
        /// <param name="p1">First point</param>
        /// <param name="p2">Second point</param>
        public Line(PointF p1, PointF p2, bool isEndLine)
        {
            this.Id = Guid.NewGuid();
            this.endpoint1 = p1;
            this.endpoint2 = p2;
            float[] temp = computeLine(p1, p2);
            this.slope = temp[0];
            this.intercept = temp[1];
            this.isEndLine = isEndLine;
        }
        #endregion


        #region Public Functions
        /// <summary>
        /// Extend the line by a given length (from endpoint1 to endpoint2)
        /// </summary>
        /// <param name="length">Amount to extend the line</param>
        public void extend(double length)
        {
            float dx = this.endpoint2.X - this.endpoint1.X;
            float dy = this.endpoint2.Y - this.endpoint1.Y;
            double theta = Math.Atan2((double)dy, (double)dx);
            //PointF temp = this.endpoint2;
            PointF p1 = new PointF();
            //double theta = Math.Atan((double)this.slope);

            p1.X = this.endpoint2.X + (float)(length * Math.Cos(theta));
            p1.Y = this.endpoint2.Y + (float)(length * Math.Sin(theta));

            this.endpoint2 = p1;
            //this.endpoint1 = temp;
        }

        public bool sameAs(Line a)
        {
            bool same = false;

            if (this.endpoint1 == a.endpoint1)
            {
                if (this.endpoint2 == a.endpoint2)
                {
                    same = true;
                }
            }

            return same;
        }
        #endregion


        #region Getters & Setters
        public float Slope
        {
            get { return this.slope; }
        }

        public float Intercept
        {
            get { return this.intercept; }
        }

        public PointF EndPoint1
        {
            get { return this.endpoint1; }
        }

        public PointF EndPoint2
        {
            get { return this.endpoint2; }
        }

        public Guid ID
        {
            get { return this.Id; }
        }

        public bool IsEndLine
        {
            get { return this.isEndLine; }
        }
        #endregion


        #region Private Functions
        private float[] computeLine(PointF p1, PointF p2)
        {
            float[] att = new float[2];

            // Compute slope of the line
            if (p2.X != p1.X)
                att[0] = (p2.Y - p1.Y) / (p2.X - p1.X);
            else
                att[0] = (p2.Y - p1.Y) / 0.01f;
            // Compute y-intersection of the line
            att[1] = p1.Y - att[0] * p1.X;

            return att;
        }
        #endregion


        #region Static Functions
        /// <summary>
        /// Determine whether two line segments intersect
        /// </summary>
        /// <param name="a">First Line Segment</param>
        /// <param name="b">Second Line Segment</param>
        /// <returns>Bool variable indicating whether the two line segments intersect</returns>
        public static bool intersects(Line a, Line b)
        {
            PointF p1 = findIntersection(a, b);
            bool intersects = false;

            if ((p1.X >= a.endpoint1.X && p1.X <= a.endpoint2.X)
                || (p1.X >= a.endpoint2.X && p1.X <= a.endpoint1.X))
            {
                if ((p1.Y >= a.endpoint1.Y && p1.Y <= a.endpoint2.Y)
                    || (p1.Y >= a.endpoint2.Y && p1.Y <= a.endpoint1.Y))
                {
                    if ((p1.X >= b.endpoint1.X && p1.X <= b.endpoint2.X)
                        || (p1.X >= b.endpoint2.X && p1.X <= b.endpoint1.X))
                    {
                        if ((p1.Y >= b.endpoint1.Y && p1.Y <= b.endpoint2.Y)
                            || (p1.Y >= b.endpoint2.Y && p1.Y <= b.endpoint1.Y))
                        {

                            intersects = true;
                        }
                    }
                }
            }

            return intersects;
        }

        /// <summary>
        /// Finds the point of intersection of the two lines
        /// </summary>
        /// <param name="a">First Line</param>
        /// <param name="b">Second Line</param>
        /// <returns>System.Drawing.PointF at the intersection point</returns>
        public static PointF findIntersection(Line a, Line b)
        {
            // Taken from http://mathworld.wolfram.com/Line-LineIntersection.html
            float determinant = (a.endpoint1.X - a.endpoint2.X) * (b.endpoint1.Y - b.endpoint2.Y)
                - (b.endpoint1.X - b.endpoint2.X) * (a.endpoint1.Y - a.endpoint2.Y);

            float detA = (a.endpoint1.X * a.endpoint2.Y) - (a.endpoint2.X * a.endpoint1.Y);
            float detB = (b.endpoint1.X * b.endpoint2.Y) - (b.endpoint2.X * b.endpoint1.Y);

            float detX = (detA * (b.endpoint1.X - b.endpoint2.X)) - (detB * (a.endpoint1.X - a.endpoint2.X));
            float detY = (detA * (b.endpoint1.Y - b.endpoint2.Y)) - (detB * (a.endpoint1.Y - a.endpoint2.Y));

            if (determinant != 0)
                return new PointF((detX / determinant), (detY / determinant));
            else
                return new PointF(0.0f, 0.0f);
        }

        public static float findIntersectionDistanceAlongLineA(Line a, Line b, bool DistanceToEndPoint1)
        {
            // Taken from http://mathworld.wolfram.com/Line-LineIntersection.html
            float determinant = (a.endpoint1.X - a.endpoint2.X) * (b.endpoint1.Y - b.endpoint2.Y)
                - (b.endpoint1.X - b.endpoint2.X) * (a.endpoint1.Y - a.endpoint2.Y);

            float detA = (a.endpoint1.X * a.endpoint2.Y) - (a.endpoint2.X * a.endpoint1.Y);
            float detB = (b.endpoint1.X * b.endpoint2.Y) - (b.endpoint2.X * b.endpoint1.Y);

            float detX = (detA * (b.endpoint1.X - b.endpoint2.X)) - (detB * (a.endpoint1.X - a.endpoint2.X));
            float detY = (detA * (b.endpoint1.Y - b.endpoint2.Y)) - (detB * (a.endpoint1.Y - a.endpoint2.Y));

            if (determinant != 0)
            {
                PointF p = new PointF((detX / determinant), (detY / determinant));
                float length = Compute.EuclideanDistance(a.EndPoint1, a.EndPoint2);
                float f = Compute.EuclideanDistance(a.EndPoint1, p);
                if (DistanceToEndPoint1)
                    return f / length;
                else
                    return 1f - (f / length);
            }
            else
                return -1f;
        }

        #endregion
    }
}
