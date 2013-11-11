using System;
using System.Collections.Generic;
using System.Text;
using Sketch;

namespace Cluster
{
    [System.Diagnostics.DebuggerDisplay("#ClusterSets = {clusterSets.Count}, Current Cluster = {currentClusterSet}")]
    public class SketchCluster
    {
        private ClusterSet bestClusterSet;
        Dictionary<int, Sketch.Substroke> ind2Substroke;

        private double[,] distances;
        private ulong[,] times;
        private double[,] probabilities;

        private double[] clusterWeights;
        private double distanceThreshold;
        private ulong timeThreshold;
        private double clusteringThreshold;

        


        #region Unused
        /*
        private List<ClusterSet> clusterSets;
        private int currentClusterSet;
        public int bestCluster;
        private bool useSpatial;
        private double[] mergeDistances;
        private double[] temporalMergeDistances;
         * 
        public SketchCluster(ClusterSet cSet, bool spatial)
        {
            this.clusterSets = new List<ClusterSet>();
            this.clusterSets.Add(cSet);
            this.currentClusterSet = -1;
            this.useSpatial = spatial;
            this.distanceThreshold = 250.0;
            this.timeThreshold = 900;
            this.clusteringThreshold = 0.7;
            this.clusterWeights = new double[] { 0.5, 0.5 };
            this.mergeDistances = new double[cSet.Clusters.Count];
            this.temporalMergeDistances = new double[cSet.Clusters.Count];
            this.ind2Substroke = new Dictionary<int, Substroke>();
            this.bestCluster = 0;
        }
         * 
        
         * */
        #endregion




        public SketchCluster(Sketch.Sketch sketch, Dictionary<Guid, string> strokeClassifications, string type)
        {
            ClusterSet cSet = new ClusterSet(sketch, strokeClassifications, type);
            cSet.findMinDelta();
            cSet.findMinTimeDelta();
            cSet.applyClassifications(sketch);
            this.clusterSets = new List<ClusterSet>();
            this.clusterSets.Add(cSet);
            this.currentClusterSet = -1;
            this.mergeDistances = new double[cSet.Clusters.Count];
            this.temporalMergeDistances = new double[cSet.Clusters.Count];
            this.bestCluster = 0;
            this.distanceThreshold = 250.0;
            this.timeThreshold = 900;
            this.clusteringThreshold = 0.7;
            this.clusterWeights = new double[] { 0.5, 0.5 };
            this.ind2Substroke = new Dictionary<int, Substroke>();
            //findSketchCluster();
            findBestClusterFromProbs();
        }

        public void findSketchCluster()
        {
            List<double> spatialDistances = clusterSets[0].findAllDeltas();
            List<double> temporalDistances = clusterSets[0].findAllTimeDeltas();

            double averageDistances = computeAverage(spatialDistances);
            double averageTimes = computeAverage(temporalDistances);

            int current = 0;
            while (clusterSets[current].Clusters.Count > 2)
            {
                current = clusterSets.Count - 1;
                Guid[] closeClusters;
                if (this.useSpatial)
                    closeClusters = clusterSets[current].ClosestClusters;
                else
                    closeClusters = clusterSets[current].TemporalClosestClusters;
                ClusterSet newSet = new ClusterSet();
                bool clusterToMerge = false;
                bool clusterToMergeAdded = false;
                foreach (Cluster c in clusterSets[current].Clusters)
                {
                    clusterToMerge = false;
                    foreach (Guid id in closeClusters)
                    {
                        if (c.Id == id)
                            clusterToMerge = true;
                    }

                    if (!clusterToMerge)
                    {
                        Cluster cNew = new Cluster();
                        cNew.addStroke(c.Strokes);
                        cNew.ClassificationNum = c.ClassificationNum;
                        newSet.addCluster(cNew);
                    }
                    else
                    {
                        if (!clusterToMergeAdded)
                        {
                            bool okay1, okay2;
                            Cluster c1 = clusterSets[current].getCluster(closeClusters[0], out okay1);
                            Cluster c2 = clusterSets[current].getCluster(closeClusters[1], out okay2);
                            if (okay1 && okay2)
                            {
                                clusterToMergeAdded = true;
                                newSet.addCluster(Cluster.merge(c1, c2));
                            }
                        }
                    }
                }

                newSet.findMinDelta();
                newSet.findMinTimeDelta();
                clusterSets.Add(newSet);
            }


            findBestCluster();
        }

        /// <summary>
        /// Compute a 2-D array of the minimum distances between all sets of strokes in a classification
        /// </summary>
        private void findAllDistances()
        {
            List<Substroke> strokes = new List<Substroke>();
            foreach (Cluster c in this.clusterSets[0].Clusters)
                strokes.Add(c.Strokes[0]);

            distances = new double[strokes.Count, strokes.Count];
            for (int i = 0; i < strokes.Count; i++)
            {
                ind2Substroke.Add(i, strokes[i]);
                for (int j = i; j < strokes.Count; j++)
                {
                    if (i == j)
                        distances[i, j] = 0;
                    else
                    {
                        distances[i, j] = computeDistances(strokes[i], strokes[j]);
                        distances[j, i] = distances[i, j];
                    }
                }
            }
        }

        /// <summary>
        /// Compute the minimum Euclidean Distance between two strokes
        /// </summary>
        /// <param name="a">First Stroke</param>
        /// <param name="b">Second Stroke</param>
        /// <returns>Minimum Distance</returns>
        private double computeDistances(Substroke a, Substroke b)
        {
            double d = 1000000.0;

            foreach (Point pt1 in a.Points)
            {
                foreach (Point pt2 in b.Points)
                {
                    d = Math.Min(d, euclDist(pt1, pt2));
                }
            }

            return d;
        }

        /// <summary>
        /// Compute the Euclidean Distance between two points
        /// </summary>
        /// <param name="pt1">First Point</param>
        /// <param name="pt2">Second Point</param>
        /// <returns>Distance</returns>
        private double euclDist(Point pt1, Point pt2)
        {
            return Math.Sqrt(Math.Pow(pt1.X - pt2.X, 2.0) + Math.Pow(pt1.Y - pt2.Y, 2.0));
        }

        /// <summary>
        /// Compute a 2-D array of the time gaps between all sets of strokes in a classification
        /// </summary>
        private void findAllTimes()
        {
            List<Substroke> strokes = new List<Substroke>();
            foreach (Cluster c in this.clusterSets[0].Clusters)
                strokes.Add(c.Strokes[0]);

            times = new ulong[strokes.Count, strokes.Count];
            for (int i = 0; i < strokes.Count; i++)
            {
                for (int j = i; j < strokes.Count; j++)
                {
                    if (i == j)
                        times[i, j] = 0;
                    else
                    {
                        times[i, j] = computeTimes(strokes[i], strokes[j]);
                        times[j, i] = times[i, j];
                    }
                }
            }
        }

        /// <summary>
        /// Compute the time gap between two strokes
        /// </summary>
        /// <param name="a">First stroke</param>
        /// <param name="b">Second stroke</param>
        /// <returns>Time gap</returns>
        private ulong computeTimes(Substroke a, Substroke b)
        {
            ulong t = 1000000000000;

            if (a.Points[0].Time > b.Points[b.Points.Length - 1].Time)
                t = a.Points[0].Time - b.Points[b.Points.Length - 1].Time;
            else if (b.Points[0].Time > a.Points[a.Points.Length - 1].Time)
                t = b.Points[0].Time - a.Points[a.Points.Length - 1].Time;
            else
                t = 0;

            return t;
        }

        /// <summary>
        /// Compute a 2-D array of probabilities of clustering any two given strokes
        /// </summary>
        private void findProbabilitiesClustering()
        {
            int num = this.distances.GetLength(0);
            probabilities = new double[num, num];

            for (int i = 0; i < num; i++)
            {
                for (int j = i; j < num; j++)
                {
                    double Pd = thresholdingFcnDistance(this.distances[i, j]);
                    double Pt = thresholdingFcnTime(this.times[i, j]);


                    if (i == j)
                        probabilities[i, j] = 0;
                    else
                    {
                        probabilities[i, j] = Pd * this.clusterWeights[0] + Pt * this.clusterWeights[1];
                        probabilities[j, i] = probabilities[i, j];
                    }
                }
            }
        }

        /// <summary>
        /// Compute a probability that 2 strokes should be clustered together based on distance
        /// </summary>
        /// <param name="d">Distance between the strokes</param>
        /// <returns>Distance probability that strokes belong together</returns>
        private double thresholdingFcnDistance(double d)
        {
            if (d < distanceThreshold)
                return 1.0;
            else
                return 0.0;
        }

        /// <summary>
        /// Compute a probability that 2 strokes should be clustered together based on distance
        /// </summary>
        /// <param name="t">Time gap between the strokes</param>
        /// <returns>Time probability that strokes belong together</returns>
        private double thresholdingFcnTime(ulong t)
        {
            if (t < timeThreshold)
                return 1.0;
            else
                return 0.0;
        }

        /// <summary>
        /// Determine the best clustering based on the stroke-stroke probability matrix
        /// </summary>
        public void findBestClusterFromProbs()
        {
            findAllDistances();
            findAllTimes();
            findProbabilitiesClustering();
            List<int[]> pairs = findPairs();
            List<int> singles = findSingles(pairs);

            List<List<int>> finalClusters = findFinalClusters(pairs, singles);

            List<Cluster> bestClusters = new List<Cluster>();
            foreach (List<int> cluster in finalClusters)
            {
                List<Substroke> strokes = new List<Substroke>(cluster.Count);
                foreach (int i in cluster)
                    strokes.Add(ind2Substroke[i]);

                bestClusters.Add(new Cluster(strokes));
            }

            bestClusterSet = new ClusterSet(bestClusters);
        }

        /// <summary>
        /// Find all single stroke clusters (ones that didn't get grouped)
        /// </summary>
        /// <param name="pairs">list of pairs of substroke indices</param>
        /// <returns>Indices of Single Stroke Clusters</returns>
        private List<int> findSingles(List<int[]> pairs)
        {
            int num = this.distances.GetLength(0);
            List<int> pairValues = new List<int>(pairs.Count * 2);
            int a1, a2;
            bool found1, found2;

            foreach (int[] pair in pairs)
            {
                a1 = pair[0]; a2 = pair[1];
                found1 = false; found2 = false;

                foreach (int b in pairValues)
                {
                    if (a1 == b)
                        found1 = true;
                    if (a2 == b)
                        found2 = true;
                }

                if (!found1)
                    pairValues.Add(a1);
                if (!found2)
                    pairValues.Add(a2);
            }
            pairValues.Sort();

            List<int> singles = new List<int>(num - pairValues.Count);
            for (int i = 0; i < num; i++)
            {
                if (!pairValues.Contains(i))
                    singles.Add(i);
            }

            return singles;
        }

        /// <summary>
        /// Find stroke pairs based on probability matrix
        /// </summary>
        /// <returns>List of pairs</returns>
        private List<int[]> findPairs()
        {
            int num = this.distances.GetLength(0);
            List<int[]> pairs = new List<int[]>();

            for (int i = 0; i < num; i++)
            {
                for (int j = i; j < num; j++)
                {
                    if (this.probabilities[i, j] > clusteringThreshold)
                    {
                        pairs.Add(new int[] { i, j });
                    }
                }
            }

            return pairs;
        }

        /// <summary>
        /// Combines pair lists of strokes to find best clusterings
        /// </summary>
        /// <param name="pairs">List of stroke pairs</param>
        /// <returns>List of indices to make final clusters from</returns>
        private List<List<int>> findFinalClusters(List<int[]> pairs, List<int> singles)
        {
            List<List<int>> finalClusters = new List<List<int>>();
            bool found, found1, found2;
            int a1, a2;

            foreach (int[] pair in pairs)
            {
                found = found1 = found2 = false;
                a1 = pair[0];
                a2 = pair[1];

                foreach (List<int> cluster in finalClusters)
                {
                    foreach (int b in cluster)
                    {
                        if (a1 == b)
                        {
                            found = true;
                            found1 = true;
                        }
                        if (a2 == b)
                        {
                            found = true;
                            found2 = true;
                        }
                    }

                    if (found1 && !found2)
                        cluster.Add(a2);
                    else if (found2 && !found1)
                        cluster.Add(a1);
                }

                if (!found)
                    finalClusters.Add(new List<int>(pair));
            }

            foreach (int a in singles)
            {
                List<int> l = new List<int>(1);
                l.Add(a);
                finalClusters.Add(new List<int>(l));
            }

            return finalClusters;
        }

        private double computeAverage(List<double> values)
        {
            double avg = 0.0;
            for (int i = 0; i < values.Count; i++)
                avg += values[i];

            avg /= (values.Count);
            return avg;
        }

        /// <summary>
        /// Get the "best" Cluster Set which is found by inter-stroke distances and times.
        /// </summary>
        public ClusterSet BestClusterSet
        {
            get { return this.bestClusterSet; }
        }

        public List<ClusterSet> ClusterSets
        {
            get { return this.clusterSets; }
        }

        public int CurrentClusterSet
        {
            get { return this.currentClusterSet; }
        }

        public Dictionary<Guid, int> getClusterClassifications(ClusterSet cSet, Sketch.Sketch sketch)
        {
            foreach (ClusterSet cs in this.clusterSets)
            {
                if (cs.Id == cSet.Id)
                    return cs.getClusterClassifications(sketch);
            }
            if (cSet.Id == bestClusterSet.Id)
                return bestClusterSet.getClusterClassifications(sketch);

            return new Dictionary<Guid, int>();
        }

        public void nextClusterSet()
        {
            this.currentClusterSet++;
            if (this.currentClusterSet >= this.clusterSets.Count)
                this.currentClusterSet = 0;
        }

        public void previousClusterSet()
        {
            this.currentClusterSet--;
            if (this.currentClusterSet < 0)
                this.currentClusterSet = this.clusterSets.Count - 1;
        }

        public double[] MergeDistances
        {
            get
            {
                for (int i = 0; i < this.clusterSets.Count; i++)
                {
                    this.mergeDistances[i] = this.clusterSets[i].Delta;
                }
                return this.mergeDistances;
            }
        }

        public double[] TemporalMergeDistances
        {
            get
            {
                for (int i = 0; i < this.clusterSets.Count; i++)
                {
                    this.temporalMergeDistances[i] = this.clusterSets[i].TimeDelta;
                }
                return this.temporalMergeDistances;
            }
        }

        public void printDistances()
        {
            System.IO.StreamWriter writer = new System.IO.StreamWriter("C:\\distances.txt");

            for (int i = 0; i < this.TemporalMergeDistances.Length; i++)
            {
                writer.WriteLine("{0}, {1}, {2}", i, this.MergeDistances[i], this.TemporalMergeDistances[i]);
            }

            writer.Close();
        }

        public static void printDistances(double[] distances, double[] times)
        {
            System.IO.StreamWriter writer = new System.IO.StreamWriter("C:\\distances.txt");

            if (distances.Length == times.Length)
            {
                for (int i = 0; i < distances.Length; i++)
                {
                    writer.WriteLine("{0}, {1}, {2}", i, distances[i], times[i]);
                }
            }

            writer.Close();
        }

        public string currentDistances()
        {
            string a, b, c;

            if (currentClusterSet > 1)
                a = "     i-1= " + mergeDistances[currentClusterSet - 2].ToString("#0.00") + ",";
            else
                a = "     i-1= 0,";

            if (currentClusterSet > 0)
                b = "     i= " + mergeDistances[currentClusterSet - 1].ToString("#0.00") + ",";
            else
                b = "     i= 0,";

            if (currentClusterSet < clusterSets.Count)
                c = "     i+1= " + mergeDistances[currentClusterSet].ToString("#0.00");
            else
                c = "     i+1= 0";

            return a + b + c;
        }

        public string currentTimes()
        {
            string a, b, c;

            if (currentClusterSet > 1)
                a = "     i-1= " + TemporalMergeDistances[currentClusterSet - 2].ToString("#0.00") + ",";
            else
                a = "     i-1= 0,";

            if (currentClusterSet > 0)
                b = "     i= " + TemporalMergeDistances[currentClusterSet - 1].ToString("#0.00") + ",";
            else
                b = "     i= 0,";

            if (currentClusterSet < clusterSets.Count)
                c = "     i+1= " + TemporalMergeDistances[currentClusterSet].ToString("#0.00");
            else
                c = "     i+1= 0";

            return a + b + c;
        }

        private void findBestCluster()
        {
            double[] distances;
            if (useSpatial)
                distances = this.mergeDistances;
            else
                distances = this.temporalMergeDistances;
            this.bestCluster = 1;
            double numer, denom;
            if (distances.Length > 3)
            {
                double bestJump, Jump;
                numer = Math.Pow(distances[2] - distances[1], 2.0);
                denom = distances[1] - distances[0];
                if (denom < 0.1) denom = 0.1;
                bestJump = numer / denom;
                for (int i = 2; i < distances.Length - 2; i++)
                {
                    numer = Math.Pow(distances[i + 1] - distances[i], 2.0);
                    denom = distances[i] - distances[i - 1];
                    if (denom < 0.1) denom = 0.1;
                    Jump = numer / denom;

                    if (Jump > bestJump)
                    {
                        bestJump = Jump;
                        bestCluster = i;
                    }
                }
            }
        }

    }
}
