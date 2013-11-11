using System;
using System.Collections.Generic;
using System.Text;
using Sketch;

namespace Cluster
{
    [System.Diagnostics.DebuggerDisplay("TimeD = {timeDelta}, SpatialD = {spatialDelta}, #Clusters = {clusters.Count}")]
    public class ClusterSet
    {
        private List<Cluster> clusters;
        private double spatialDelta;
        private double timeDelta;
        private Guid[] closestClusters;
        private Guid[] temporalClosestClusters;
        private Guid id;
        private Dictionary<Guid, int> clusterClassifications;

        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        public ClusterSet()
        {
            initializeClass();
            this.clusters = new List<Cluster>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cluster">First cluster to place in the ClusterSet</param>
        public ClusterSet(Cluster cluster)
        {
            initializeClass();
            this.clusters = new List<Cluster>();
            this.clusters.Add(cluster);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="clusters">Clusters to put into the ClusterSet</param>
        public ClusterSet(List<Cluster> clusters)
        {
            initializeClass();
            this.clusters = clusters;
        }

        /// <summary>
        /// Create a clusterset from classifications
        /// </summary>
        /// <param name="sketch">Sketch with substrokes to populate cluster with</param>
        /// <param name="classifications">Classifications from NN</param>
        /// <param name="type">Type of classification to create clusters from</param>
        public ClusterSet(Sketch.Sketch sketch, Dictionary<Guid, string> classifications, string type)
        {
            initializeClass();
            this.clusters = new List<Cluster>();

            foreach (Substroke s in sketch.Substrokes)
            {
                if (classifications[s.Id] == type)
                    this.clusters.Add(new Cluster(s));
            }
        }

        public void initializeClass()
        {
            this.id = Guid.NewGuid();
            this.spatialDelta = 1000000.0;
            this.timeDelta = 1000000.0;
            this.closestClusters = new Guid[2];
            this.temporalClosestClusters = new Guid[2];
            this.clusterClassifications = new Dictionary<Guid, int>();
        }
        #endregion


        #region Getters & Setters
        /// <summary>
        /// Get the smallest Merge Distance in this ClusterSet
        /// </summary>
        public double Delta
        {
            get { return this.spatialDelta; }
        }

        /// <summary>
        /// Get the Guid of the ClusterSet
        /// </summary>
        public Guid Id
        {
            get { return this.id; }
        }

        /// <summary>
        /// Get the List of clusters
        /// </summary>
        public List<Cluster> Clusters
        {
            get { return this.clusters; }
        }

        /// <summary>
        /// Get a specific cluster based on Guid
        /// </summary>
        /// <param name="id">Guid of cluster</param>
        /// <param name="foundCluster">Tells whether a cluster was successfully found</param>
        /// <returns>Cluster associated with ID</returns>
        public Cluster getCluster(Guid id, out bool foundCluster)
        {
            foreach (Cluster c in this.clusters)
            {
                if (c.Id == id)
                {
                    foundCluster = true;
                    return c;
                }
            }
            foundCluster = false;
            return new Cluster();
        }

        private Cluster getCluster(Guid strokeID)
        {
            foreach (Cluster c in this.clusters)
            {
                foreach (Substroke stroke in c.Strokes)
                {
                    if (stroke.Id == strokeID)
                        return c;
                }
            }

            return new Cluster();
        }

        /// <summary>
        /// Get the Guids of the closest 2 clusters
        /// </summary>
        public Guid[] ClosestClusters
        {
            get { return this.closestClusters; }
        }

        /// <summary>
        /// Get the cluster classifications for this clusterset
        /// </summary>
        public Dictionary<Guid, int> getClusterClassifications(Sketch.Sketch sketch)
        {
            if (this.clusterClassifications.Count == 0)
                applyClassifications(sketch);

            return this.clusterClassifications;
        }

        /// <summary>
        /// Get the shortest time between two clusters in this clusterSet
        /// </summary>
        public double TimeDelta
        {
            get { return this.timeDelta; }
        }

        /// <summary>
        /// Get the Guids of the closest clusters temporally
        /// </summary>
        public Guid[] TemporalClosestClusters
        {
            get { return this.temporalClosestClusters; }
        }
        #endregion


        public void applyClassifications(Sketch.Sketch sketch)
        {
            int classNum = 0;
            foreach (Cluster c in this.clusters)
            {
                if (c.ClassificationNum == -1)
                {
                    classNum++;
                    c.ClassificationNum = classNum;
                }
            }

            foreach (Substroke stroke in sketch.Substrokes)
            {
                bool found = false;
                foreach (Cluster c in this.clusters)
                {
                    foreach (Substroke s in c.Strokes)
                    {
                        if (s.Id == stroke.Id)
                            found = true;
                    }
                }

                if (found)
                {
                    if (!clusterClassifications.ContainsKey(stroke.Id))
                        clusterClassifications.Add(stroke.Id, getCluster(stroke.Id).ClassificationNum);
                }
                else
                {
                    if (!clusterClassifications.ContainsKey(stroke.Id))
                        clusterClassifications.Add(stroke.Id, 0);
                }
            }
        }

        private int getClusterClassNum(Guid SubstrokeID, Guid ClusterID)
        {
            foreach (Cluster c in this.clusters)
            {
                if (c.Id == ClusterID)
                {
                    foreach (Substroke stroke in c.Strokes)
                    {
                        if (stroke.Id == SubstrokeID)
                            return c.ClassificationNum;
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Add another cluster to the ClusterSet
        /// </summary>
        /// <param name="cluster">Cluster to add</param>
        public void addCluster(Cluster cluster)
        {
            this.clusters.Add(cluster);
        }

        /// <summary>
        /// Search through all clusters for a minimum distance between any 2 clusters
        /// </summary>
        public void findMinDelta()
        {
            Cluster c1, c2;
            for (int i = 0; i < clusters.Count; i++)
            {
                c1 = clusters[i];
                for (int j = i + 1; j < clusters.Count; j++)
                {
                    c2 = clusters[j];
                    if (c1.Id != c2.Id)
                    {
                        double distance = computeDistance(c1.BoundingBox, c2.BoundingBox);
                        if (distance < this.spatialDelta)
                        {
                            distance = computeDistance(c1, c2);
                            if (distance < this.spatialDelta)
                            {
                                this.spatialDelta = distance;
                                this.closestClusters[0] = c1.Id;
                                this.closestClusters[1] = c2.Id;
                            }
                        }
                    }
                }
            }
        }

        private double computeDistance(System.Drawing.Rectangle rect1, System.Drawing.Rectangle rect2)
        {
            bool overlapHorizontal, overlapVertical;

            // Find if overlap in X
            if ((rect1.X >= rect2.X && rect1.X <= rect2.X + rect2.Width)
                || (rect1.X + rect1.Width >= rect2.X && rect1.X + rect1.Width <= rect2.X + rect2.Width)
                || (rect2.X >= rect1.X && rect2.X <= rect1.X + rect1.Width))
                overlapHorizontal = true;
            else
                overlapHorizontal = false;

            // Find if overlap in Y
            if ((rect1.Y >= rect2.Y && rect1.Y <= rect2.Y + rect2.Height)
                || (rect1.Y + rect1.Height >= rect2.Y && rect1.Y + rect1.Height <= rect2.Y + rect2.Height)
                || (rect2.Y >= rect1.Y && rect2.Y <= rect1.Y + rect1.Height))
                overlapVertical = true;
            else
                overlapVertical = false;

            if (overlapHorizontal && overlapVertical)
                return 0.0;
            else if (overlapHorizontal)
                return Math.Min(rect1.Y - (rect2.Y + rect2.Height), rect2.Y - (rect1.Y + rect1.Height));
            else if (overlapVertical)
                return Math.Min(rect1.X - (rect2.X + rect2.Width), rect2.X - (rect1.X + rect1.Width));
            else
            {
                double distance = 1000000.0;

                distance = Math.Min(distance, euclideanDistance(rect1, 1, rect2, 1));
                distance = Math.Min(distance, euclideanDistance(rect1, 1, rect2, 2));
                distance = Math.Min(distance, euclideanDistance(rect1, 1, rect2, 3));
                distance = Math.Min(distance, euclideanDistance(rect1, 1, rect2, 4));
                distance = Math.Min(distance, euclideanDistance(rect1, 2, rect2, 1));
                distance = Math.Min(distance, euclideanDistance(rect1, 2, rect2, 2));
                distance = Math.Min(distance, euclideanDistance(rect1, 2, rect2, 3));
                distance = Math.Min(distance, euclideanDistance(rect1, 2, rect2, 4));
                distance = Math.Min(distance, euclideanDistance(rect1, 3, rect2, 1));
                distance = Math.Min(distance, euclideanDistance(rect1, 3, rect2, 2));
                distance = Math.Min(distance, euclideanDistance(rect1, 3, rect2, 3));
                distance = Math.Min(distance, euclideanDistance(rect1, 3, rect2, 4));
                distance = Math.Min(distance, euclideanDistance(rect1, 4, rect2, 1));
                distance = Math.Min(distance, euclideanDistance(rect1, 4, rect2, 2));
                distance = Math.Min(distance, euclideanDistance(rect1, 4, rect2, 3));
                distance = Math.Min(distance, euclideanDistance(rect1, 4, rect2, 4));

                return distance;
            }
        }

        private double computeDistance(Cluster c1, Cluster c2)
        {
            double distance = 1000000.0;
            foreach (Substroke stroke1 in c1.Strokes)
            {
                foreach (Substroke stroke2 in c2.Strokes)
                {
                    foreach (Point pt1 in stroke1.Points)
                    {
                        foreach (Point pt2 in stroke2.Points)
                            distance = Math.Min(distance, euclDist(pt1, pt2));
                    }
                }
            }

            return distance;
        }

        private double euclDist(Point pt1, Point pt2)
        {
            return Math.Sqrt(Math.Pow(pt1.X - pt2.X, 2.0) + Math.Pow(pt1.Y - pt2.Y, 2.0));
        }

        private double euclideanDistance(System.Drawing.Rectangle rect1, int corner1, System.Drawing.Rectangle rect2, int corner2)
        {
            System.Drawing.PointF pt1, pt2;
            switch (corner1)
            {
                case 1:
                    pt1 = new System.Drawing.PointF((float)(rect1.X), (float)(rect1.Y));
                    break;
                case 2:
                    pt1 = new System.Drawing.PointF((float)(rect1.X + rect1.Width), (float)(rect1.Y));
                    break;
                case 3:
                    pt1 = new System.Drawing.PointF((float)(rect1.X), (float)(rect1.Y + rect1.Height));
                    break;
                case 4:
                    pt1 = new System.Drawing.PointF((float)(rect1.X + rect1.Width), (float)(rect1.Y + rect1.Height));
                    break;
                default:
                    pt1 = new System.Drawing.PointF();
                    break;
            }

            switch (corner2)
            {
                case 1:
                    pt2 = new System.Drawing.PointF((float)(rect2.X), (float)(rect2.Y));
                    break;
                case 2:
                    pt2 = new System.Drawing.PointF((float)(rect2.X + rect2.Width), (float)(rect2.Y));
                    break;
                case 3:
                    pt2 = new System.Drawing.PointF((float)(rect2.X), (float)(rect2.Y + rect2.Height));
                    break;
                case 4:
                    pt2 = new System.Drawing.PointF((float)(rect2.X + rect2.Width), (float)(rect2.Y + rect2.Height));
                    break;
                default:
                    pt2 = new System.Drawing.PointF();
                    break;
            }

            return Math.Sqrt(Math.Pow(pt1.X - pt2.X, 2.0) + Math.Pow(pt1.Y - pt2.Y, 2.0));
        }

        public void findMinTimeDelta()
        {
            Cluster c1, c2;
            for (int i = 0; i < clusters.Count; i++)
            {
                c1 = clusters[i];
                for (int j = i + 1; j < clusters.Count; j++)
                {
                    c2 = clusters[j];
                    if (c1.Id != c2.Id)
                    {
                        ulong timeGap = computeTimeGap(c1, c2);
                        if (timeGap < this.timeDelta)
                        {
                            this.timeDelta = timeGap;
                            this.temporalClosestClusters[0] = c1.Id;
                            this.temporalClosestClusters[1] = c2.Id;
                        }
                    }
                }
            }
        }

        public List<double> findAllDeltas()
        {
            List<double> values = new List<double>(clusters.Count * clusters.Count / 2);
            Cluster c1, c2;
            for (int i = 0; i < clusters.Count; i++)
            {
                c1 = clusters[i];
                for (int j = i + 1; j < clusters.Count; j++)
                {
                    c2 = clusters[j];
                    if (c1.Id != c2.Id)
                    {
                        values.Add(computeDistance(c1, c2));
                    }
                }
            }

            return values;
        }

        public List<double> findAllTimeDeltas()
        {
            List<double> values = new List<double>(clusters.Count * clusters.Count / 2);
            Cluster c1, c2;
            for (int i = 0; i < clusters.Count; i++)
            {
                c1 = clusters[i];
                for (int j = i + 1; j < clusters.Count; j++)
                {
                    c2 = clusters[j];
                    if (c1.Id != c2.Id)
                    {
                        values.Add(computeTimeGap(c1, c2));
                    }
                }
            }

            return values;
        }

        private ulong computeTimeGap(Cluster c1, Cluster c2)
        {
            if (c1.StartTime <= c2.StartTime)
            {
                if (c1.EndTime <= c2.StartTime)
                    return c2.StartTime - c1.EndTime;
                else
                    return 0;
            }
            else
            {
                if (c2.EndTime <= c1.StartTime)
                    return c1.StartTime - c2.EndTime;
                else
                    return 0;
            }
        }
    }
}
