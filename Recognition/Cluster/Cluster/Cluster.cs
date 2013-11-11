using System;
using System.Collections.Generic;
using System.Text;
using Sketch;

namespace Cluster
{
    /// <summary>
    /// Class to package together a number of strokes which belong together
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("StartTime = {startTime}, EndTime = {endTime}, #Strokes = {strokes.Count}")]
    public class Cluster: ICloneable
    {
        private List<Substroke> strokes;
        private double score;
        private Guid id;
        private int classificationNum;
        private string type;
        private ulong startTime;
        private ulong endTime;
        private bool justMerged;

        private double[] _centroid;
        //private List<Substroke> nearestStrokes;
        //private List<Substroke> nearestWires;
        //private List<Substroke> nearestNonWires;

        #region Constructors
        /// <summary>
        /// Create an empty cluster to put strokes into
        /// </summary>
        public Cluster()
        {
            initializeComponent();
            this.strokes = new List<Substroke>();
            this.startTime = 0;
            this.endTime = 0;
        }

        /// <summary>
        /// Creates a cluster from a single substroke
        /// </summary>
        /// <param name="stroke">Substroke to begin cluster with</param>
        public Cluster(Substroke stroke)
        {
            initializeComponent();
            this.strokes = new List<Substroke>();
            this.strokes.Add(stroke);
            this.startTime = stroke.Points[0].Time;
            this.endTime = stroke.Points[stroke.Points.Length - 1].Time;
        }

        /// <summary>
        /// Creates a cluster of strokes from a list
        /// </summary>
        /// <param name="strokes">List of strokes to add to cluster</param>
        public Cluster(List<Substroke> strokes)
        {
            initializeComponent();
            this.strokes = strokes;
            strokesStartEndTimes(strokes, out this.startTime, out this.endTime);
        }

        /// <summary>
        /// Creates a cluster of strokes from an array
        /// </summary>
        /// <param name="strokes">Array of strokes to add to cluster</param>
        public Cluster(Substroke[] strokes)
        {
            initializeComponent();
            this.strokes = new List<Substroke>(strokes.Length);
            foreach (Substroke stroke in strokes)
                this.strokes.Add(stroke);
            strokesStartEndTimes(new List<Substroke>(strokes), out this.startTime, out this.endTime);
        }


        private void initializeComponent()
        {
            this.id = Guid.NewGuid();
            this.score = 0.0;
            this.classificationNum = -1;
            this.justMerged = false;
            this._centroid = new double[2] { 0.0, 0.0 };
            this.type = "unknown";
            //this.nearestStrokes = new List<Substroke>();
            //this.nearestWires = new List<Substroke>();
            //this.nearestNonWires = new List<Substroke>();
        }

        public object Clone()
        {
            Cluster temp = (Cluster)this.MemberwiseClone();
            foreach (Substroke s in this.strokes)
                temp.strokes.Add(s.Clone());

            return temp;
        }
        #endregion


        #region Getters & Setters
        /// <summary>
        /// Gets the number of strokes in the cluster
        /// </summary>
        public int Count
        {
            get { return this.strokes.Count; }
        }

        /// <summary>
        /// Gets the Guid of the cluster
        /// </summary>
        public Guid Id
        {
            get { return this.id; }
        }

        /// <summary>
        /// Gets the list of strokes in the cluster
        /// </summary>
        public List<Substroke> Strokes
        {
            get { return this.strokes; }
        }

        /// <summary>
        /// Range of 0.0 to 1.0: how likely these cluster belongs
        /// </summary>
        public double Score
        {
            get { return this.score; }
            set { this.score = value; }
        }

        /// <summary>
        /// Get the bounding box around the entire cluster of strokes
        /// </summary>
        public System.Drawing.Rectangle BoundingBox
        {
            get
            {
                int minX = 1000000;
                int minY = 1000000;
                int maxX = 0;
                int maxY = 0;

                foreach (Substroke stroke in this.strokes)
                {
                    if (stroke.XmlAttrs.X < minX)
                        minX = (int)stroke.XmlAttrs.X;
                    if (stroke.XmlAttrs.X + stroke.XmlAttrs.Width > maxX)
                        maxX = (int)stroke.XmlAttrs.X + (int)stroke.XmlAttrs.Width;
                    if (stroke.XmlAttrs.Y < minY)
                        minY = (int)stroke.XmlAttrs.Y;
                    if (stroke.XmlAttrs.Y + stroke.XmlAttrs.Height > maxY)
                        maxY = (int)stroke.XmlAttrs.Y + (int)stroke.XmlAttrs.Height;
                }
                return new System.Drawing.Rectangle(minX, minY, maxX - minX, maxY - minY);
            }
        }

        /*
        /// <summary>
        /// Gets the list of nearest strokes to the current cluster
        /// </summary>
        public List<Substroke> NearestStrokes
        {
            get { return this.nearestStrokes; }
        }

        /// <summary>
        /// Gets the list of nearest strokes to the current cluster 
        /// which are classified as 'Wires'
        /// </summary>
        public List<Substroke> NearestWires
        {
            get { return this.nearestWires; }
        }

        /// <summary>
        /// Gets the list of nearest strokes to the current cluster
        /// which are classified as 'Non-Wires'
        /// </summary>
        public List<Substroke> NearestNonWires
        {
            get { return this.nearestNonWires; }
        }
         * */

        /// <summary>
        /// Get or set the classification number of this cluster (for color-coding)
        /// </summary>
        public int ClassificationNum
        {
            get { return this.classificationNum; }
            set { this.classificationNum = value; }
        }

        public string Type
        {
            get { return this.type; }
            set { this.type = value; }
        }

        /// <summary>
        /// Get the starting time of the first stroke in this cluster
        /// </summary>
        public ulong StartTime
        {
            get
            {
                if (this.startTime == 0)
                {
                    this.startTime = strokes[0].Points[0].Time;
                    foreach (Substroke s in this.strokes)
                        this.startTime = Math.Min(this.startTime, s.Points[0].Time);
                }
                return this.startTime;
            }
        }

        /// <summary>
        /// Get the ending time of the last stroke in this cluster
        /// </summary>
        public ulong EndTime
        {
            get
            {
                if (this.endTime == 0)
                {
                    this.endTime = strokes[0].Points[strokes[0].Points.Length - 1].Time;
                    foreach (Substroke s in this.strokes)
                        this.endTime = Math.Max(this.endTime, s.Points[s.Points.Length - 1].Time);
                }
                return this.endTime;
            }
        }

        /// <summary>
        /// Determine whether a cluster has just been merged
        /// Used mainly for display purposes (thicken strokes)
        /// </summary>
        public bool JustMerged
        {
            get { return this.justMerged; }
        }

        public double[] Centroid
        {
            get
            {
                if (_centroid[0] == 0.0 && _centroid[1] == 0.0)
                    computeCentroid(this.strokes);

                return _centroid;
            }
        }
        #endregion


        #region Functions

        public void computeCentroid(List<Substroke> strokes)
        {
            double[] res = new double[] { 0d, 0d };
            int pointCount = 0;

            foreach (Substroke s in strokes)
            {
                foreach (Point p in s.PointsL)
                {
                    res[0] += p.X;
                    res[1] += p.Y;
                    pointCount++;
                }
            }

            res[0] /= pointCount;
            res[1] /= pointCount;

            _centroid = res;
        }

        /// <summary>
        /// Indicates whether two clusters share a stroke in common
        /// </summary>
        /// <param name="C">Cluster to compare 'this' to</param>
        /// <returns>bool indicator of strokes in common</returns>
        public bool strokesOverlap(Cluster C)
        {
            foreach (Substroke stroke in this.strokes)
            {
                foreach (Substroke cStroke in C.strokes)
                {
                    if (stroke.XmlAttrs.Id.Equals(cStroke.XmlAttrs.Id.Value))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Indicates whether two clusters have overlapping areas
        /// </summary>
        /// <param name="C">Cluster to compare 'this' to</param>
        /// <returns>bool indicator of area in common</returns>
        public bool boundingBoxOverlap(Cluster C)
        {
            if (overlap(this.BoundingBox, C.BoundingBox, 0))
                return true;
            return false;
        }

        private bool overlap(System.Drawing.Rectangle a, System.Drawing.Rectangle b, int fudgeFactor)
        {
            if ((a.X >= (b.X - fudgeFactor) && a.X <= (b.X + b.Width + fudgeFactor))
                || ((a.X + a.Width) >= (b.X - fudgeFactor) && (a.X + a.Width) <= (b.X + b.Width + fudgeFactor))
                || ((b.X >= (a.X - fudgeFactor)) && (b.X <= (a.X + a.Width + fudgeFactor))))  // overlap in x
            {
                if ((a.Y >= (b.Y - fudgeFactor) && a.Y <= (b.Y + b.Height + fudgeFactor))
                    || ((a.Y + a.Height) >= (b.Y - fudgeFactor) && (a.Y + a.Height) <= (b.Y + b.Height + fudgeFactor))
                    || (b.Y >= (a.Y - fudgeFactor) && b.Y <= (a.Y + a.Height + fudgeFactor)))  // overlap in y
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determine whether a Cluster contains a certain Substroke
        /// </summary>
        /// <param name="stroke">Substroke to check</param>
        /// <returns>Bool whether cluster contains substroke</returns>
        public bool contains(Substroke stroke)
        {
            foreach (Substroke s in this.strokes)
            {
                if (s.XmlAttrs.Id.Equals(stroke.XmlAttrs.Id.Value))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determine whether a Cluster contains a certain Substroke by Guid
        /// </summary>
        /// <param name="ident">Substroke Guid to check</param>
        /// <returns>Bool whether cluster contains Substroke with ID</returns>
        public bool contains(Guid ident)
        {
            foreach (Substroke stroke in this.strokes)
            {
                if (stroke.XmlAttrs.Id.Equals(ident))
                    return true;
            }
            return false;
        }

        private void strokesStartEndTimes(List<Substroke> strokes, out ulong startTime, out ulong endTime)
        {
            startTime = strokes[0].Points[0].Time * 2;
            endTime = 0;
            foreach (Substroke s in strokes)
            {
                startTime = Math.Min(startTime, s.Points[0].Time);
                endTime = Math.Max(endTime, s.Points[s.Points.Length - 1].Time);
            }
        }

        /*
        /// <summary>
        /// Determine whether a cluster's nearest strokes contains a given stroke by Guid
        /// </summary>
        /// <param name="ident">Guid of stroke to check</param>
        /// <returns>Bool whether nearest strokes contains a given stroke</returns>
        public bool nearestContains(Guid ident)
        {
            foreach (Substroke stroke in this.nearestStrokes)
            {
                if (stroke.XmlAttrs.Id.Equals(ident))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Add a stroke to the nearest strokes list by type
        /// </summary>
        /// <param name="stroke">Stroke to add to nearest strokes list</param>
        /// <param name="classifiedType">Type of stroke being added</param>
        public void addNearStroke(Substroke stroke, string classifiedType)
        {
            this.nearestStrokes.Add(stroke);
            if (classifiedType == "Wire")
                this.nearestWires.Add(stroke);
            else if (classifiedType == "Non-Wire")
                this.nearestNonWires.Add(stroke);
        }
         * */

        /// <summary>
        /// Add a stroke to the cluster
        /// </summary>
        /// <param name="stroke">Stroke to add</param>
        public void addStroke(Substroke stroke)
        {
            this.strokes.Add(stroke);
        }

        /// <summary>
        /// Add a list of strokes to the cluster
        /// </summary>
        /// <param name="strokes">List of strokes to add</param>
        public void addStroke(List<Substroke> strokes)
        {
            foreach (Substroke s in strokes)
                this.strokes.Add(s);
        }

        /// <summary>
        /// Merge two clusters into one
        /// </summary>
        /// <param name="c1">First cluster to merge</param>
        /// <param name="c2">Second cluster to merge</param>
        /// <returns>New cluster</returns>
        public static Cluster merge(Cluster c1, Cluster c2)
        {
            Cluster nCluster = new Cluster();

            nCluster.addStroke(c1.Strokes);

            if (c2.Strokes.Count > c1.Strokes.Count)
                nCluster.classificationNum = c2.classificationNum;
            else
                nCluster.classificationNum = c1.classificationNum;

            nCluster.addStroke(c2.Strokes);
            nCluster.justMerged = true;

            return nCluster;
        }
        #endregion
    }
}
