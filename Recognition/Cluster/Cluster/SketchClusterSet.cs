using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Sketch;
using NeuralNetAForge;

namespace Cluster
{
    public class SketchClusterSet
    {
        #region Parameters / Thresholds
        private double distanceThreshold = 250.0;
        private ulong timeThreshold = 900;
        private double clusteringThreshold = 0.5;

        private const double DISTANCE_FACTOR = 10000.0;
        private const double TIME_FACTOR = 100000.0;
        #endregion


        #region Member Variables
        private Guid _id;

        private List<Substroke> _strokes;

        private Dictionary<int, Substroke> _index2substroke;

        private Dictionary<Guid, double> _minDistanceFromStroke;

        private List<Cluster> _clusters;

        private double[,] _minDistances;

        private ulong[,] _times;

        private int[][,] _overlaps;

        private double[,] _centroidDistances;

        private double[,] _maxDistances;

        private double[] _clusterWeights;

        private double[][,] _ratios;

        #endregion


        #region Constructors
        /// <summary>
        /// Constructor which finds the best clustering of strokes of a given type
        /// </summary>
        /// <param name="sketch">Sketch containing substrokes to cluster</param>
        /// <param name="classificationType">Type of substrokes to cluster together</param>
        public SketchClusterSet(Sketch.Sketch sketch, Dictionary<Guid, string> strokeClassifications,
            string classificationType)
        {
            _id = Guid.NewGuid();
            _clusterWeights = new double[] { 0.5, 0.5 };
            _clusters = new List<Cluster>();
            _strokes = new List<Substroke>();
            _index2substroke = new Dictionary<int, Substroke>();
            _minDistanceFromStroke = new Dictionary<Guid, double>();

            foreach (Substroke s in sketch.Substrokes)
            {
                if (strokeClassifications[s.Id] == classificationType)
                    _strokes.Add(s);
            }

            double[][,] distances = computeDistances(_strokes);
            _minDistances = distances[0];
            _maxDistances = distances[1];
            _centroidDistances = distances[2];
            _times = computeTimes(_strokes);
            _overlaps = computeOverlap(_strokes);

            findMinDistancesFromStrokes();

            _ratios = computeRatios();

            //FindBestClustering();
        }

        /// <summary>
        /// Put ALL substrokes from the sketch into the SketchClusterSet
        /// </summary>
        /// <param name="sketch">Input Sketch</param>
        public SketchClusterSet(Sketch.Sketch sketch)
        {
            _id = Guid.NewGuid();
            _clusterWeights = new double[] { 0.5, 0.5 };
            _clusters = new List<Cluster>();
            _strokes = sketch.SubstrokesL;
            _index2substroke = new Dictionary<int, Substroke>();
            _minDistanceFromStroke = new Dictionary<Guid, double>();

            double[][,] distances = computeDistances(_strokes);
            _minDistances = distances[0];
            _maxDistances = distances[1];
            _centroidDistances = distances[2];
            _times = computeTimes(_strokes);
            _overlaps = computeOverlap(_strokes);

            findMinDistancesFromStrokes();

            _ratios = computeRatios();
        }
        #endregion


        #region Computations

        private double[][,] computeRatios()
        {
            double[,] ratio1 = new double[_strokes.Count, _strokes.Count];
            double[,] ratio2 = new double[_strokes.Count, _strokes.Count];

            for (int i = 0; i < _strokes.Count; i++)
            {
                for (int j = 0; j < _strokes.Count; j++)
                {
                    if (i == j)
                    {
                        ratio1[i, j] = 0.0;
                        ratio2[i, j] = 0.0;
                    }
                    else
                    {
                        ratio1[i, j] = (1.0 + _minDistanceFromStroke[_index2substroke[i].Id] / DISTANCE_FACTOR) / (1.0 + _minDistances[i, j] / DISTANCE_FACTOR);
                        ratio2[i, j] = (1.0 + _minDistanceFromStroke[_index2substroke[j].Id] / DISTANCE_FACTOR) / (1.0 + _minDistances[i, j] / DISTANCE_FACTOR);
                    }
                }
            }

            double[][,] ratios = new double[2][,] { ratio1, ratio2 };

            return ratios;
        }

        /// <summary>
        /// Find the minimum distance from a stroke to any other stroke
        /// </summary>
        private void findMinDistancesFromStrokes()
        {
            for (int i = 0; i < _minDistances.GetLength(0); i++)
            {
                double minD = 1000000.0;
                for (int j = 0; j < _minDistances.GetLength(1); j++)
                {
                    if (i != j)
                        minD = Math.Min(minD, _minDistances[i, j]);
                }
                _minDistanceFromStroke.Add(_index2substroke[i].Id, minD);
            }
        }

        /// <summary>
        /// Compute a 2-D array of the minimum distances between all sets of strokes in a classification
        /// </summary>
        /// <param name="strokes">Strokes to consider</param>
        /// <returns>Inter-stroke minimum distances</returns>
        private double[][,] computeDistances(List<Substroke> strokes)
        {
            double[,] minDistances = new double[strokes.Count, strokes.Count];
            double[,] maxDistances = new double[strokes.Count, strokes.Count];
            double[,] centroidDistances = new double[strokes.Count, strokes.Count];
            for (int i = 0; i < strokes.Count; i++)
            {
                _index2substroke.Add(i, strokes[i]);
                for (int j = i; j < strokes.Count; j++)
                {
                    if (i == j)
                    {
                        minDistances[i, j] = 0;
                        maxDistances[i, j] = 0;
                        centroidDistances[i, j] = 0;
                    }
                    else
                    {
                        double[] d = computeDistances(strokes[i], strokes[j]);
                        minDistances[i, j] = d[0];
                        minDistances[j, i] = minDistances[i, j];
                        maxDistances[i, j] = d[1];
                        maxDistances[j, i] = maxDistances[i, j];
                        centroidDistances[i, j] = d[2];
                        centroidDistances[j, i] = centroidDistances[i, j];
                    }
                }
            }

            double[][,] distances = new double[3][,];
            distances[0] = minDistances;
            distances[1] = maxDistances;
            distances[2] = centroidDistances;
            return distances;
        }

        /// <summary>
        /// Compute the minimum Euclidean Distance between two strokes
        /// </summary>
        /// <param name="a">First Stroke</param>
        /// <param name="b">Second Stroke</param>
        /// <returns>Minimum Distance</returns>
        private double[] computeDistances(Substroke a, Substroke b)
        {
            double[] d = new double[3];
            d[0] = 1000000.0; //minDistance
            d[1] = 0.0; // maxDistance
            d[2] = euclDist(new Point((float)a.Centroid[0], (float)a.Centroid[1]), 
                new Point((float)b.Centroid[0], (float)b.Centroid[1]));
            
            foreach (Point pt1 in a.Points)
            {
                foreach (Point pt2 in b.Points)
                {
                    d[0] = Math.Min(d[0], euclDist(pt1, pt2));
                    d[1] = Math.Max(d[1], euclDist(pt1, pt2));
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
        /// <param name="strokes">Strokes to consider</param>
        /// <returns>Time gapes</returns>
        private ulong[,] computeTimes(List<Substroke> strokes)
        {
            ulong[,] times = new ulong[strokes.Count, strokes.Count];
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

            return times;
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
        /// Computes the horizontal and vertical overlaps of a set of strokes
        /// </summary>
        /// <param name="strokes">Strokes to find overlaps with</param>
        /// <returns>overlaps</returns>
        private int[][,] computeOverlap(List<Substroke> strokes)
        {
            int[,] horizontal = new int[strokes.Count, strokes.Count];
            int[,] vertical = new int[strokes.Count, strokes.Count];
            for (int i = 0; i < strokes.Count; i++)
            {
                for (int j = i; j < strokes.Count; j++)
                {
                    if (i == j)
                    {
                        horizontal[i, j] = 0;
                        vertical[i, j] = 0;
                    }
                    else
                    {
                        int[] overlaps = computeOverlap(strokes[i], strokes[j]);
                        horizontal[i, j] = overlaps[0];
                        horizontal[j, i] = horizontal[i, j];
                        vertical[i, j] = overlaps[1];
                        vertical[j, i] = vertical[i, j];
                    }
                }
            }

            int[][,] Alloverlaps = new int[2][,];
            Alloverlaps[0] = horizontal;
            Alloverlaps[1] = vertical;
            return Alloverlaps;
        }

        /// <summary>
        /// Compute the horizontal and vertical overlaps of the substrokes' bounding boxes
        /// </summary>
        /// <param name="a">First Stroke</param>
        /// <param name="b">Second Stroke</param>
        /// <returns>overlaps</returns>
        private int[] computeOverlap(Substroke a, Substroke b)
        {
            int[] d = new int[2];

            System.Drawing.Rectangle boxA = computeBoundingBox(a.Points);
            System.Drawing.Rectangle boxB = computeBoundingBox(b.Points);
            bool aLeft, aRight, aTop, aBottom;
            aLeft = aRight = aTop = aBottom = false;

            if (boxA.X <= boxB.X)
                aLeft = true;
            if (boxA.X + boxA.Width >= boxB.X + boxB.Width)
                aRight = true;
            if (boxA.Y <= boxB.Y)
                aBottom = true;
            if (boxA.Y + boxA.Height >= boxB.Y + boxB.Height)
                aTop = true;

            if (aLeft && aRight) // 'b' enclosed in 'a' (horizontal)
                d[0] = boxB.Width;
            else if (!aLeft && !aRight) // 'a' enclosed in 'b' (horizontal)
                d[0] = boxA.Width;
            else if (aLeft) // 'a' left of 'b'
                d[0] = boxA.X + boxA.Width - boxB.X;
            else // 'b' left of 'a'
                d[0] = boxB.X + boxB.Width - boxA.X;

            if (aTop && aBottom)
                d[1] = boxB.Height;
            else if (!aTop && !aBottom)
                d[1] = boxA.Height;
            else if (aTop)
                d[1] = boxA.Y + boxA.Height - boxB.Y;
            else
                d[1] = boxB.Y + boxB.Height - boxA.Y;

            return d;
        }

        /// <summary>
        /// Find the bounding box for an array of points
        /// </summary>
        /// <param name="pts">Array of points</param>
        /// <returns>Rectangular boundingbox</returns>
        private System.Drawing.Rectangle computeBoundingBox(Sketch.Point[] pts)
        {
            System.Drawing.Rectangle box = new System.Drawing.Rectangle((int)pts[0].X, (int)pts[0].Y, 0, 0);
            int maxX = (int)pts[0].X;
            int maxY = (int)pts[0].Y;
            for (int i = 1; i < pts.Length; i++)
            {
                box.X = Math.Min(box.X, (int)pts[i].X);
                maxX = Math.Max(maxX, (int)pts[i].X);
                box.Y = Math.Min(box.Y, (int)pts[i].Y);
                maxY = Math.Max(maxY, (int)pts[i].Y);
            }
            box.Width = maxX - box.X;
            box.Height = maxY - box.Y;

            return box;
        }

        #endregion


        #region Getters

        /// <summary>
        /// Get the probabilites of clustering two strokes based on distances and times
        /// </summary>
        private double[,] ClusteringProbabilites
        {
            get
            {
                int num = _minDistances.GetLength(0);
                double[,] probabilities = new double[num, num];

                for (int i = 0; i < num; i++)
                {
                    for (int j = i; j < num; j++)
                    {
                        double Pd = thresholdingFcnDistance(_minDistances[i, j]);
                        double Pt = thresholdingFcnTime(_times[i, j]);


                        if (i == j)
                            probabilities[i, j] = 0;
                        else
                        {
                            probabilities[i, j] = Pd * _clusterWeights[0] + Pt * _clusterWeights[1];
                            probabilities[j, i] = probabilities[i, j];
                        }
                    }
                }

                return probabilities;
            }
        }

        /// <summary>
        /// Gets the list of clusters found to be best
        /// </summary>
        public List<Cluster> Clusters
        {
            get { return _clusters; }
        }

        public Dictionary<Guid, int> ClusterClassifications
        {
            get
            {
                Dictionary<Guid, int> cc = new Dictionary<Guid, int>(_clusters.Count);

                foreach (Cluster c in _clusters)
                    cc.Add(c.Id, c.ClassificationNum);

                return cc;
            }
        }

        public double[][,] InterStrokeFeatures
        {
            get
            {
                // Get all the features, normalized so that their values will usually
                // be in the 0.0 - 1.0 range.
                // Features to use:
                //   0: minDistance
                //   1: timeGap
                //   2: horizontalOverlap
                //   3: verticalOverlap
                //   4: maxDistance
                //   5: centroidDistance
                //   6: Distance 1 ratio
                //   7: Distance 2 ratio
                double[][,] interStrokeFeatures = new double[8][,];

                double[,] minD = new double[_minDistances.GetLength(0), _minDistances.GetLength(1)];
                double[,] maxD = new double[_minDistances.GetLength(0), _minDistances.GetLength(1)];
                double[,] centroidD = new double[_minDistances.GetLength(0), _minDistances.GetLength(1)];
                double[,] overlap0 = new double[_minDistances.GetLength(0), _minDistances.GetLength(1)];
                double[,] overlap1 = new double[_minDistances.GetLength(0), _minDistances.GetLength(1)];
                double[,] times = new double[_minDistances.GetLength(0), _minDistances.GetLength(1)];

                for (int i = 0; i < _minDistances.GetLength(0); i++)
                {
                    for (int j = 0; j < _minDistances.GetLength(1); j++)
                    {
                        minD[i, j] = _minDistances[i, j] / DISTANCE_FACTOR;
                        maxD[i, j] = _maxDistances[i, j] / DISTANCE_FACTOR;
                        centroidD[i, j] = _centroidDistances[i, j] / DISTANCE_FACTOR;
                        overlap0[i, j] = _overlaps[0][i, j] / DISTANCE_FACTOR;
                        overlap1[i, j] = _overlaps[1][i, j] / DISTANCE_FACTOR;
                        times[i, j] = (double)_times[i, j] / TIME_FACTOR;
                    }
                }

                interStrokeFeatures[0] = minD;
                interStrokeFeatures[1] = times;
                interStrokeFeatures[2] = overlap0;
                interStrokeFeatures[3] = overlap1;
                interStrokeFeatures[4] = maxD;
                interStrokeFeatures[5] = centroidD;
                interStrokeFeatures[6] = _ratios[0];
                interStrokeFeatures[7] = _ratios[1];


                return interStrokeFeatures;
            }
        }

        public Dictionary<int, Substroke> Index2Substroke
        {
            get { return _index2substroke; }
        }

        #endregion


        #region Clustering

        public Dictionary<Guid, int> getBestClustering(NeuralNet NNet, bool[] useFeatures)
        {
            int num = (_strokes.Count * _strokes.Count - _strokes.Count) / 2;
            Dictionary<int, int[]> single2pair = new Dictionary<int, int[]>(num);
            double[][] testSet = new double[num][];
            int n = 0;

            // Adjust numFeatures based on which bools are turned on
            int numFeatures = 0;
            foreach (bool feature in useFeatures)
            {
                if (feature)
                    numFeatures++;
            }

            for (int i = 0; i < _minDistances.GetLength(0); i++)
            {
                for (int j = i + 1; j < _minDistances.GetLength(0); j++)
                {
                    
                    double[] set = new double[numFeatures];
                    int k = 0;
                    int f = 0;
                    if (useFeatures[f])
                    {
                        set[k] = _minDistances[i, j] / 10000.0; 
                        k++;
                    }
                    f++;
                    if (useFeatures[f])
                    {
                        set[k] = _times[i, j] / 100000.0; 
                        k++;
                    }
                    f++;
                    if (useFeatures[f])
                    {
                        set[k] = _overlaps[0][i, j] / 10000.0; 
                        k++;
                    }
                    f++;
                    if (useFeatures[f])
                    {
                        set[k] = _overlaps[1][i, j] / 10000.0; 
                        k++;
                    }
                    f++;
                    if (useFeatures[f])
                    {
                        set[k] = _maxDistances[i, j] / 10000.0; 
                        k++;
                    }
                    f++;
                    if (useFeatures[f])
                    {
                        set[k] = _centroidDistances[i, j] / 10000.0; 
                        k++;
                    }
                    f++;
                    if (useFeatures[f])
                    {
                        set[k] = (1.0 + _minDistanceFromStroke[_index2substroke[i].Id] / 10000.0) / (1.0 + _minDistances[i, j] / 10000.0); 
                        k++;
                    }
                    f++;
                    if (useFeatures[f])
                    {
                        set[k] = (1.0 + _minDistanceFromStroke[_index2substroke[j].Id] / 10000.0) / (1.0 + _minDistances[i, j] / 10000.0); 
                        k++;
                    }
                    f++;
                    testSet[n] = set;
                    int[] pair = new int[] { i, j };
                    single2pair.Add(n, pair);
                    n++;
                }
            }

            double[] probs = NNet.TestNN(testSet);
            double[,] probabilities = new double[_minDistances.GetLength(0), _minDistances.GetLength(0)];
            for (int i = 0; i < probs.Length; i++)
            {
                int[] indices = single2pair[i];
                probabilities[indices[0], indices[1]] = probs[i];
                probabilities[indices[1], indices[0]] = probabilities[indices[0], indices[1]];
            }

            FindBestClustering(probabilities);

            return ClusterClassifications;
        }

        /// <summary>
        /// Find the best set of clusters based on inter-stroke distances and times
        /// using probabilities found from the NN.
        /// </summary>
        /// <param name="probabilities"></param>
        private void FindBestClustering(double[,] probabilities)
        {
            List<int[]> pairs = findPairs(probabilities);
            List<int> singles = findSingles(pairs, probabilities);

            List<List<int>> finalClusters = findFinalClusters(pairs, singles);

            List<Cluster> bestClusters = new List<Cluster>();
            foreach (List<int> cluster in finalClusters)
            {
                List<Substroke> strokes = new List<Substroke>(cluster.Count);
                foreach (int i in cluster)
                    strokes.Add(_index2substroke[i]);

                bestClusters.Add(new Cluster(strokes));
            }

            for (int i = 0; i < bestClusters.Count; i++)
                bestClusters[i].ClassificationNum = i;

            _clusters.Clear();
            _clusters = bestClusters;
        }

        /// <summary>
        /// Find the best set of clusters based on inter-stroke distances and times
        /// using some thresholds.
        /// </summary>
        private void FindBestClustering()
        {
            double[,] probabilities = ClusteringProbabilites;
            List<int[]> pairs = findPairs(probabilities);
            List<int> singles = findSingles(pairs, probabilities);

            List<List<int>> finalClusters = findFinalClusters(pairs, singles);

            List<Cluster> bestClusters = new List<Cluster>();
            foreach (List<int> cluster in finalClusters)
            {
                List<Substroke> strokes = new List<Substroke>(cluster.Count);
                foreach (int i in cluster)
                    strokes.Add(_index2substroke[i]);

                bestClusters.Add(new Cluster(strokes));
            }

            for (int i = 0; i < bestClusters.Count; i++)
                bestClusters[i].ClassificationNum = i;

            _clusters = bestClusters;
        }

        /// <summary>
        /// Finds the best clustering based on given parameters
        /// </summary>
        /// <param name="distanceThresh">Distance Threshold to use for clustering</param>
        /// <param name="timeThresh">Time Gap Threshold to use for clustering</param>
        public void FindBestClustering(double distanceThresh, ulong timeThresh)
        {
            this.distanceThreshold = distanceThresh;
            this.timeThreshold = timeThresh;
            FindBestClustering();
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
            
            /*
            for (int i = 0; i < finalClusters.Count; i++)
            {
                for (int j = 0; j < finalClusters.Count; j++)
                {
                    bool extra = false;
                    foreach (int a in finalClusters[j])
                    {
                        if (finalClusters[i].Contains(a))
                        {
                            extra = true;
                            foreach (int b in finalClusters[j])
                            {
                                if (!finalClusters[i].Contains(b))
                                    finalClusters[i].Add(b);
                            }
                        }
                    }
                    if (extra)
                        finalClusters.Remove(finalClusters[j]);
                }
            }
            */
            /*
            List<List<int>> mergedClusters = new List<List<int>>();
            foreach (int[] pair in pairs)
            {
                // See if any of the clusters have the pair's strokes
                foreach (List<int> cluster in mergedClusters)
                {
                    if (cluster.Contains(pair[0]))
                    {
                        if (!cluster.Contains(pair[1]))
                            cluster.Add(pair[1]);
                    }
                    if (cluster.Contains(pair[1]))
                    {
                        if (!cluster.Contains(pair[0]))
                            cluster.Add(pair[0]);
                    }
                }
            }*/

            int max = 0;
            foreach (List<int> cluster in finalClusters)
            {
                foreach (int num in cluster)
                    max = Math.Max(max, num);
            }

            bool noChanges = false;
            while (!noChanges)
            {
                Dictionary<int, int> numOccurances = new Dictionary<int, int>();
                for (int i = 0; i < max; i++)
                {
                    int count = 0;
                    foreach (List<int> cluster in finalClusters)
                    {
                        foreach (int num in cluster)
                        {
                            if (num == i)
                                count++;
                        }
                    }

                    numOccurances.Add(i, count);
                }

                List<int> matchingIndices = new List<int>();
                bool match = false;
                foreach (KeyValuePair<int, int> pair in numOccurances)
                {
                    if (pair.Value > 1)
                    {
                        match = true;
                        matchingIndices.Add(pair.Key);
                    }
                }

                if (!match)
                    noChanges = true;

                foreach (int index in matchingIndices)
                {
                    List<int> newCluster = new List<int>();
                    List<List<int>> clustersToRemove = new List<List<int>>();
                    foreach (List<int> cluster in finalClusters)
                    {
                        if (cluster.Contains(index))
                        {
                            foreach (int num in cluster)
                            {
                                if (!newCluster.Contains(num))
                                    newCluster.Add(num);
                            }
                            clustersToRemove.Add(cluster);
                        }
                    }

                    foreach (List<int> cluster in clustersToRemove)
                        finalClusters.Remove(cluster);

                    finalClusters.Add(newCluster);
                }
            }

            foreach (int a in singles)
            {
                List<int> l = new List<int>(1);
                l.Add(a);
                finalClusters.Add(new List<int>(l));
            }

            return finalClusters;
        }

        /// <summary>
        /// Find all single stroke clusters (ones that didn't get grouped)
        /// </summary>
        /// <param name="pairs">list of pairs of substroke indices</param>
        /// <returns>Indices of Single Stroke Clusters</returns>
        private List<int> findSingles(List<int[]> pairs, double[,] probabilities)
        {
            int num = probabilities.GetLength(0);
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
        /// <param name="probabilites">Clustering probabilities matrix</param>
        /// <returns>List of pairs</returns>
        private List<int[]> findPairs(double[,] probabilities)
        {
            int num = probabilities.GetLength(0);
            List<int[]> pairs = new List<int[]>();

            for (int i = 0; i < num; i++)
            {
                for (int j = i; j < num; j++)
                {
                    if (probabilities[i, j] > clusteringThreshold)
                    {
                        pairs.Add(new int[] { i, j });
                    }
                }
            }

            return pairs;
        }

        #endregion
    }
}
