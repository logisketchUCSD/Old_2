using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.Ink;
using Sketch;

namespace FeatureSpace
{
    public delegate void FeatureSketchCompletedEventHandler(object sender, EventArgs e);

    public enum ValuePreparationStage { ActualValue, Normalized, PreparedForNN };

    [Serializable]
    public class FeatureSketch : ISerializable
    {
        #region Member Variables

        /// <summary>
        /// Unique ID number for the FeatureSketch.
        /// </summary>
        private Guid m_Id;

        /// <summary>
        /// XML Sketch for which features are computed
        /// </summary>
        private Sketch.Sketch m_Sketch;

        /// <summary>
        /// Features to use for single-stroke classification
        /// </summary>
        private Dictionary<string, bool> m_FeatureListSingle;

        /// <summary>
        /// Features to use for pair-wise comparisons (grouping)
        /// </summary>
        private Dictionary<string, bool> m_FeatureListPair;

        /// <summary>
        /// Features for each stroke
        /// </summary>
        private Dictionary<Substroke, FeatureSingleStroke> m_StrokeFeatures;

        /// <summary>
        /// All intersections for the sketch
        /// </summary>
        private IntersectionSketch m_IntersectionSketch;

        /// <summary>
        /// All pairwise feature values for the sketch
        /// </summary>
        private PairwiseFeatureSketch m_PairwiseFeatureSketch;

        /// <summary>
        /// Ensures that the correct stroke order is used for time gaps between strokes
        /// </summary>
        private SortedList<ulong, Substroke> m_OrderOfStrokes;

        /// <summary>
        /// Total sum of arc length, used to compute average arc length
        /// </summary>
        private double m_ArcLengthSum = 0.0;

        /// <summary>
        /// Total sum of the widths of stroke bounding boxes
        /// </summary>
        private double m_BBoxWidthSum = 0.0;

        /// <summary>
        /// Total sum of the heights of stroke bounding boxes
        /// </summary>
        private double m_BBoxHeightSum = 0.0;

        /// <summary>
        /// Total sum of the areas of stroke bounding boxes
        /// </summary>
        private double m_BBoxAreaSum = 0.0;

        /// <summary>
        /// Total sum of the speeds of each stroke
        /// </summary>
        private double m_StrokeSpeedSum = 0.0;

        /// <summary>
        /// Bounding box for the entire sketch
        /// </summary>
        private System.Drawing.RectangleF m_SketchBox;

        private Dictionary<string, double[]> m_AvgsAndStdDevs;

        /// <summary>
        /// Is this needed? These are the lengths to extend each stroke's end
        /// </summary>
        private Dictionary<Substroke, double> m_ExtensionLengthsActual;

        private Dictionary<Substroke, double> m_ExtensionLengthsExtreme;

        /// <summary>
        /// This background worker performs a bunch of the computations while 
        /// still remaining responsive.
        /// </summary>
        [NonSerialized]
        private BackgroundWorker m_BackgroundWorker;

        /// <summary>
        /// Flag defining whether the background worker is finished computing.
        /// If it's not finished, the background computation is performed again 
        /// once it finishes the current job. This could be done away with if the 
        /// background worker's cancellation function worked as I expected.
        /// </summary>
        [NonSerialized]
        private bool m_IsBWfinished = true;

        /// <summary>
        /// Flag to indicate whether the intersection sketch has been successfully 
        /// computed. Both the intersection sketch and the pairwise sketch need 
        /// to be completed for the FeatureSketch's background worker to commence 
        /// its computations.
        /// </summary>
        [NonSerialized]
        private bool m_IsIntersectionFinished = false;

        /// <summary>
        /// Flag to indicate whether the pairwise sketch has been successfully 
        /// computed. Both the intersection sketch and the pairwise sketch need 
        /// to be completed for the FeatureSketch's background worker to commence 
        /// its computations.
        /// </summary>
        [NonSerialized]
        private bool m_IsPairwiseFinished = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new, empty FeatureSketch. If possible, use one of the other constructors.
        /// </summary>
        public FeatureSketch()
        {
            m_Id = Guid.NewGuid();
            m_Sketch = new Sketch.Sketch();
            m_SketchBox = new System.Drawing.RectangleF(0f, 0f, 0f, 0f);
            m_OrderOfStrokes = new SortedList<ulong, Substroke>();
            m_ExtensionLengthsActual = new Dictionary<Substroke, double>();
            m_ExtensionLengthsExtreme = new Dictionary<Substroke, double>();
            m_FeatureListSingle = new Dictionary<string, bool>();
            m_FeatureListPair = new Dictionary<string, bool>();
            m_StrokeFeatures = new Dictionary<Substroke, FeatureSingleStroke>();
            m_IntersectionSketch = new IntersectionSketch(m_Sketch, m_ExtensionLengthsActual, m_ExtensionLengthsExtreme);
            m_IntersectionSketch.CompletedIntersection += new IntersectionSketchCompletedEventHandler(m_IntersectionSketch_CompletedIntersection);
            m_PairwiseFeatureSketch = new PairwiseFeatureSketch(m_Sketch, m_FeatureListPair);
            m_PairwiseFeatureSketch.CompletedPairwiseFeatures += new PairwiseFeatureSketchCompletedEventHandler(m_PairwiseFeatureSketch_CompletedPairwiseFeatures);
            m_AvgsAndStdDevs = new Dictionary<string, double[]>();

            m_BackgroundWorker = new BackgroundWorker();
            m_BackgroundWorker.WorkerSupportsCancellation = true;
            m_BackgroundWorker.WorkerReportsProgress = true;
            m_BackgroundWorker.DoWork += new DoWorkEventHandler(m_BackgroundWorker_DoWork);
            m_BackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_BackgroundWorker_RunWorkerCompleted);
        }

        /// <summary>
        /// Constructor for FeatureSketch which takes in a single list of features.
        /// This list can be used for either the single-stroke classification part, 
        /// or the pairwise grouping part; the flag is used to determine which.
        /// </summary>
        /// <param name="featureList">List of features to use.</param>
        /// <param name="FeatureListIsForSingle">Is it for single-stroke classification? Or Pairwise (grouping) classification?</param>
        public FeatureSketch(Dictionary<string, bool> featureList, bool FeatureListIsForSingle, Dictionary<string, double[]> AvgsAndStdDevs)
        {
            m_Id = Guid.NewGuid();
            m_Sketch = new Sketch.Sketch();
            m_SketchBox = new System.Drawing.RectangleF(0f, 0f, 0f, 0f);
            if (FeatureListIsForSingle)
            {
                m_FeatureListSingle = featureList;
                m_FeatureListPair = new Dictionary<string, bool>();
            }
            else
            {
                m_FeatureListSingle = new Dictionary<string, bool>();
                m_FeatureListPair = featureList;
            }
            m_OrderOfStrokes = new SortedList<ulong, Substroke>();
            m_ExtensionLengthsActual = new Dictionary<Substroke, double>();
            m_ExtensionLengthsExtreme = new Dictionary<Substroke, double>();
            m_StrokeFeatures = new Dictionary<Substroke, FeatureSingleStroke>(m_Sketch.Substrokes.Length);
            m_IntersectionSketch = new IntersectionSketch(m_Sketch, m_ExtensionLengthsActual, m_ExtensionLengthsExtreme);
            m_IntersectionSketch.CompletedIntersection += new IntersectionSketchCompletedEventHandler(m_IntersectionSketch_CompletedIntersection);
            m_PairwiseFeatureSketch = new PairwiseFeatureSketch(m_Sketch, m_FeatureListPair);
            m_PairwiseFeatureSketch.CompletedPairwiseFeatures += new PairwiseFeatureSketchCompletedEventHandler(m_PairwiseFeatureSketch_CompletedPairwiseFeatures);
            m_AvgsAndStdDevs = AvgsAndStdDevs;

            m_BackgroundWorker = new BackgroundWorker();
            m_BackgroundWorker.WorkerSupportsCancellation = true;
            m_BackgroundWorker.WorkerReportsProgress = true;
            m_BackgroundWorker.DoWork += new DoWorkEventHandler(m_BackgroundWorker_DoWork);
            m_BackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_BackgroundWorker_RunWorkerCompleted);
        }

        /// <summary>
        /// Constructor for FeatureSketch which takes in lists of features to use 
        /// for both single-stroke classification as well as pairwise (grouping) 
        /// classification.
        /// </summary>
        /// <param name="featureListSingle">List of Features to use for single-stroke classification.</param>
        /// <param name="featureListPair">List of Features to use for pairwise (grouping) classification.</param>
        public FeatureSketch(Dictionary<string, bool> featureListSingle, Dictionary<string, bool> featureListPair, Dictionary<string, double[]> AvgsAndStdDevs)
        {
            m_Id = Guid.NewGuid();
            m_Sketch = new Sketch.Sketch();
            m_SketchBox = new System.Drawing.RectangleF(0f, 0f, 0f, 0f);
            m_FeatureListSingle = featureListSingle;
            m_FeatureListPair = featureListPair;
            m_OrderOfStrokes = new SortedList<ulong, Substroke>();
            m_ExtensionLengthsActual = new Dictionary<Substroke, double>();
            m_ExtensionLengthsExtreme = new Dictionary<Substroke, double>();
            m_StrokeFeatures = new Dictionary<Substroke, FeatureSingleStroke>(m_Sketch.Substrokes.Length);
            m_IntersectionSketch = new IntersectionSketch(m_Sketch, m_ExtensionLengthsActual, m_ExtensionLengthsExtreme);
            m_IntersectionSketch.CompletedIntersection += new IntersectionSketchCompletedEventHandler(m_IntersectionSketch_CompletedIntersection);
            m_PairwiseFeatureSketch = new PairwiseFeatureSketch(m_Sketch, m_FeatureListPair);
            m_PairwiseFeatureSketch.CompletedPairwiseFeatures += new PairwiseFeatureSketchCompletedEventHandler(m_PairwiseFeatureSketch_CompletedPairwiseFeatures);
            m_AvgsAndStdDevs = AvgsAndStdDevs;

            m_BackgroundWorker = new BackgroundWorker();
            m_BackgroundWorker.WorkerSupportsCancellation = true;
            m_BackgroundWorker.WorkerReportsProgress = true;
            m_BackgroundWorker.DoWork += new DoWorkEventHandler(m_BackgroundWorker_DoWork);
            m_BackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_BackgroundWorker_RunWorkerCompleted);
        }

        /// <summary>
        /// Constructor for FeatureSketch which includes an existing XML sketch. The lists 
        /// of features to use for both single-stroke and pairwise (grouping) classifications 
        /// are also required.
        /// </summary>
        /// <param name="sketch">XML sketch to featurize</param>
        /// <param name="featureListSingle">List of Features to use for single-stroke classification.</param>
        /// <param name="featureListPair">List of Features to use for pairwise (grouping) classification.</param>
        public FeatureSketch(Sketch.Sketch sketch, Dictionary<string, bool> featureListSingle, Dictionary<string, bool> featureListPair, Dictionary<string, double[]> AvgsAndStdDevs)
        {
            m_Id = Guid.NewGuid();
            
            m_Sketch = sketch;
            m_SketchBox = Compute.BoundingBox(sketch.Points);
            
            m_FeatureListSingle = featureListSingle;
            m_FeatureListPair = featureListPair;
            m_AvgsAndStdDevs = AvgsAndStdDevs;

            m_OrderOfStrokes = GetOrderOfStrokesFromSketch(m_Sketch);
            
            m_ExtensionLengthsActual = new Dictionary<Substroke, double>();
            m_ExtensionLengthsExtreme = new Dictionary<Substroke, double>();
            

            m_StrokeFeatures = new Dictionary<Substroke, FeatureSingleStroke>(m_Sketch.Substrokes.Length);
            foreach (Substroke stroke in m_Sketch.Substrokes)
            {
                FeatureSingleStroke feature = GetFeatureStrokeAndUpdateSums(stroke);
            }

            double avgArcLength = AvgArcLength;
            foreach (Substroke stroke in m_Sketch.Substrokes)
                FindActualExtensionLength(stroke, avgArcLength);

            m_IntersectionSketch = new IntersectionSketch(m_Sketch, m_ExtensionLengthsActual, m_ExtensionLengthsExtreme);
            m_IntersectionSketch.CompletedIntersection += new IntersectionSketchCompletedEventHandler(m_IntersectionSketch_CompletedIntersection);

            m_PairwiseFeatureSketch = new PairwiseFeatureSketch(m_Sketch, m_FeatureListPair);
            m_PairwiseFeatureSketch.CompletedPairwiseFeatures += new PairwiseFeatureSketchCompletedEventHandler(m_PairwiseFeatureSketch_CompletedPairwiseFeatures);

            m_BackgroundWorker = new BackgroundWorker();
            m_BackgroundWorker.WorkerSupportsCancellation = true;
            m_BackgroundWorker.WorkerReportsProgress = true;
            m_BackgroundWorker.DoWork += new DoWorkEventHandler(m_BackgroundWorker_DoWork);
            m_BackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_BackgroundWorker_RunWorkerCompleted);

            m_IsIntersectionFinished = true;
            m_IsPairwiseFinished = true;

            DoWork();
        }

        
        

        #endregion

        #region Events

        /// <summary>
        /// Event which is thrown when the FeatureSketch completes its background computations.
        /// </summary>
        public event FeatureSketchCompletedEventHandler CompletedFeatureSketch;

        /// <summary>
        /// Handler for the FeatureSketch being completed.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnCompletedFeatureSketch(EventArgs e)
        {
            if (CompletedFeatureSketch != null)
                CompletedFeatureSketch(this, e);
        }

        /// <summary>
        /// What to do when the intersection sketch is completed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void m_IntersectionSketch_CompletedIntersection(object sender, EventArgs e)
        {
            try
            {
                //Console.WriteLine("Completed IntersectionSketch event at {0}", ((ulong)DateTime.Now.ToFileTime() - 116444736000000000) / 10000);
                m_IsIntersectionFinished = true;
                if (m_IsPairwiseFinished)
                {
                    if (m_BackgroundWorker.IsBusy)
                        m_IsBWfinished = false;
                    else
                        m_BackgroundWorker_DoWork(sender, new DoWorkEventArgs(m_StrokeFeatures));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FeatureSketch IntersectionDone: " + ex.Message);
                //throw ex;
            }
        }

        /// <summary>
        /// What to do when the pairwise sketch is completed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void m_PairwiseFeatureSketch_CompletedPairwiseFeatures(object sender, EventArgs e)
        {
            try
            {
                //Console.WriteLine("Completed PairwiseFeatureSketch event at {0}", ((ulong)DateTime.Now.ToFileTime() - 116444736000000000) / 10000);
                m_IsPairwiseFinished = true;
                if (m_IsIntersectionFinished)
                {
                    if (m_BackgroundWorker.IsBusy)
                        m_IsBWfinished = false;
                    else
                        m_BackgroundWorker_DoWork(sender, new DoWorkEventArgs(m_StrokeFeatures));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FeatureSketch PairwiseDone: " + ex.Message);
                //throw ex;
            }
        }

        /// <summary>
        /// FIX: it seems that this function is never reached. HACK to get around this: at the
        /// end of the background computation, the OnCompletedFeatureSketch function is called.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void m_BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                if (m_IsBWfinished)
                    OnCompletedFeatureSketch(new EventArgs());
                else
                {
                    m_IsBWfinished = true;
                    m_BackgroundWorker_DoWork(sender, new DoWorkEventArgs(m_StrokeFeatures));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FeatureSketch BWdone: " + ex.Message);
                //throw ex;
            }
        }

        #endregion

        #region Background jobs

        /// <summary>
        /// What the background worker executes. This function finds all the closed paths, and 
        /// updates the intersections if the extension lengths have changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void m_BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            DoWork();
        }

        private void DoWork()
        {
            bool UseIntersections = false;
            try
            {
                lock (m_StrokeFeatures)
                {
                    foreach (KeyValuePair<Substroke, FeatureSingleStroke> pair in m_StrokeFeatures)
                    {
                        if (pair.Value.UseIntersections)
                        {
                            UseIntersections = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex1)
            {
                Console.WriteLine("FeatureSketch DoWork1: " + ex1.Message);
                //throw ex1;
            }

            bool UseClosedPath;
            List<ClosedPath> closedPaths = FindClosedPaths(out UseClosedPath);

            try
            {
                foreach (KeyValuePair<Substroke, FeatureSingleStroke> pair in m_StrokeFeatures)
                {
                    FeatureSingleStroke featureStroke = pair.Value;
                    Substroke stroke = pair.Key;
                    lock (featureStroke)
                    {
                        featureStroke.CalculateMultiStrokeFeatures(m_OrderOfStrokes, m_SketchBox);

                        if (UseIntersections)
                            featureStroke.SetIntersectionFeatures(m_IntersectionSketch.GetIntersectionCounts(stroke));

                        if (UseClosedPath)
                        {
                            foreach (ClosedPath path in closedPaths)
                            {
                                if (!path.Substrokes.Contains(stroke))
                                {
                                    if (Compute.StrokeInsideBoundingBox(stroke, path.BoundingBox))
                                    {
                                        if (featureStroke.Features.ContainsKey("Inside a Closed Path"))
                                            featureStroke.Features["Inside a Closed Path"] = new InsideClosedPath(true);
                                        else
                                            featureStroke.Features.Add("Inside a Closed Path", new InsideClosedPath(true));

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex2)
            {
                Console.WriteLine("FeatureSketch DoWork2: " + ex2.Message);
                //throw ex2;
            }

            OnCompletedFeatureSketch(new EventArgs());
        }

        /// <summary>
        /// Finds all of the closed paths in the sketch.
        /// </summary>
        /// <returns></returns>
        private List<ClosedPath> FindClosedPaths(out bool UseClosedPath)
        {
            UseClosedPath = false;
            try
            {
                lock (m_StrokeFeatures)
                {
                    foreach (KeyValuePair<Substroke, FeatureSingleStroke> pair in m_StrokeFeatures)
                    {
                        if (pair.Value.UseClosedPath)
                        {
                            UseClosedPath = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("FeatureSketch FindClosedPaths: " + e.Message);
                //throw e;
            }

            Dictionary<Substroke, EndPoint[]> endPointConnections = FindEndPointConnections();

            return FindClosedPaths(m_StrokeFeatures, endPointConnections);
        }

        /// <summary>
        /// Finds all the Endpoint-Endpoint connections in the sketch. 
        /// This is used to find the closed paths.
        /// </summary>
        /// <returns></returns>
        private Dictionary<Substroke, EndPoint[]> FindEndPointConnections()
        {
            Dictionary<Substroke, EndPoint[]> stroke2endpoints = new Dictionary<Substroke, EndPoint[]>(m_Sketch.Substrokes.Length);
            try
            {
                List<EndPoint> endPointConnections = new List<EndPoint>(m_Sketch.Substrokes.Length * 2);
                foreach (Substroke s in m_Sketch.Substrokes)
                {
                    EndPoint pt1 = new EndPoint(s, 1);
                    EndPoint pt2 = new EndPoint(s, 2);
                    endPointConnections.Add(pt1);
                    endPointConnections.Add(pt2);
                    stroke2endpoints.Add(s, new EndPoint[2] { pt1, pt2 });
                }

                for (int i = 0; i < endPointConnections.Count; i++)
                {
                    for (int j = i + 1; j < endPointConnections.Count; j++)
                    {
                        Substroke s1 = endPointConnections[i].Stroke;
                        Substroke s2 = endPointConnections[j].Stroke;
                        if (s1 != s2)
                        {
                            double d = 0.0;
                            if (m_ExtensionLengthsActual.ContainsKey(s1) && m_ExtensionLengthsActual.ContainsKey(s2))
                                d = Math.Max(m_ExtensionLengthsActual[s1], m_ExtensionLengthsActual[s2]);
                            if (Compute.EuclideanDistance(endPointConnections[i].Point, endPointConnections[j].Point) < d)
                            {
                                endPointConnections[i].AddAttachment(endPointConnections[j]);
                                endPointConnections[j].AddAttachment(endPointConnections[i]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FeatureSketch FindEndPointConnections: " + ex.Message);
                //throw ex;
            }

            return stroke2endpoints;
        }

        /// <summary>
        /// Search function which starts the process which goes through all of 
        /// the endpoint-endpoint connections to find the closed path.
        /// </summary>
        /// <param name="features">Used to search for single-stroke closed paths.</param>
        /// <param name="endPtConnections">Endpoint-Endpoint connections in the sketch.</param>
        /// <returns></returns>
        private List<ClosedPath> FindClosedPaths(Dictionary<Substroke, FeatureSingleStroke> features, Dictionary<Substroke, EndPoint[]> endPtConnections)
        {
            List<ClosedPath> paths = new List<ClosedPath>();

            try
            {
                foreach (KeyValuePair<Substroke, FeatureSingleStroke> pair in features)
                {
                    Substroke stroke = pair.Key;
                    FeatureSingleStroke feature = pair.Value;

                    // If the stroke is self enclosing, automatically make a closed path of it
                    if (feature.Features.ContainsKey("Self Enclosing"))
                    {
                        if (feature.Features["Self Enclosing"].Value == 1.0)
                        {
                            ClosedPath path = new ClosedPath(stroke);
                            paths.Add(path);
                        }
                    }

                    EndPoint endPt1 = endPtConnections[stroke][0];
                    EndPoint endPt2 = endPtConnections[stroke][1];
                    if (endPt1.Attachments.Count > 0 && endPt2.Attachments.Count > 0)
                    {
                        // Search for closed paths starting end 1
                        foreach (EndPoint endPoint in endPt1.Attachments)
                        {
                            //KeyValuePair<Guid, int> pathStartPt = new KeyValuePair<Guid, int>(feature.id, 1);
                            List<Substroke> strokesSoFar = new List<Substroke>();
                            strokesSoFar.Add(stroke);

                            if (ClosedPathSearch(endPt1, endPoint, endPtConnections, ref strokesSoFar))
                            {
                                ClosedPath path = new ClosedPath();
                                foreach (Substroke s in strokesSoFar)
                                    path.AddStroke(s);

                                paths.Add(path);
                            }
                        }

                        // Search for closed paths starting at end 2
                        foreach (EndPoint endPoint in endPt2.Attachments)
                        {
                            //KeyValuePair<Guid, int> pathStartPt = new KeyValuePair<Guid, int>(feature.id, 1);
                            List<Substroke> strokesSoFar = new List<Substroke>();
                            strokesSoFar.Add(stroke);

                            if (ClosedPathSearch(endPt2, endPoint, endPtConnections, ref strokesSoFar))
                            {
                                ClosedPath path = new ClosedPath();
                                foreach (Substroke s in strokesSoFar)
                                    path.AddStroke(s);

                                paths.Add(path);
                            }
                        }
                    }
                }

                // Remove duplicate paths
                List<ClosedPath> paths2remove = new List<ClosedPath>();
                for (int i = 0; i < paths.Count; i++)
                {
                    for (int j = i + 1; j < paths.Count; j++)
                    {
                        if (paths[j].Equals(paths[i]))
                            paths2remove.Add(paths[j]);
                    }
                }
                foreach (ClosedPath path2remove in paths2remove)
                    paths.Remove(path2remove);

                // Set closedShapeConnection = true for each stroke involved
                foreach (ClosedPath path in paths)
                {
                    foreach (Substroke s in path.Substrokes)
                    {
                        if (m_StrokeFeatures.ContainsKey(s))
                        {
                            if (m_StrokeFeatures[s].Features.ContainsKey("Part of a Closed Path"))
                                m_StrokeFeatures[s].Features["Part of a Closed Path"] = new PartOfClosedPath(true);
                            else
                                m_StrokeFeatures[s].Features.Add("Part of a Closed Path", new PartOfClosedPath(true));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FeatureSketch FindClosedPaths: " + ex.Message);
                //throw ex;
            }

            return paths;
        }

        /// <summary>
        /// Recursive function which searches the endpoint-endpoint connection space to find closed paths.
        /// </summary>
        /// <param name="pathStartPt">Where did the path start?</param>
        /// <param name="nextStroke">What endpoint are we going to now?</param>
        /// <param name="endPtConnections">All Endpoint-Endpoint connections.</param>
        /// <param name="strokesSoFar">Path so far, so that the same stroke is not 
        /// traversed more than once in a single path search.</param>
        /// <returns>Whether a closed path has been found.</returns>
        private bool ClosedPathSearch(EndPoint pathStartPt, EndPoint nextStroke, 
            Dictionary<Substroke, EndPoint[]> endPtConnections, ref List<Substroke> strokesSoFar)
        {
            // Base case - have found a closed path
            if (nextStroke.Stroke == pathStartPt.Stroke && nextStroke != pathStartPt)
                return true;

            Substroke stroke = nextStroke.Stroke;
            if (!strokesSoFar.Contains(stroke))
            {
                strokesSoFar.Add(stroke);
                if (nextStroke.End == 2)
                {
                    EndPoint endPt = endPtConnections[stroke][0];
                    // Search for closed paths starting end 1
                    foreach (EndPoint endPoint in endPt.Attachments)
                        return ClosedPathSearch(pathStartPt, endPoint, endPtConnections, ref strokesSoFar);
                }
                else if (nextStroke.End == 1)
                {
                    EndPoint endPt = endPtConnections[stroke][1];
                    // Search for closed paths starting end 2
                    foreach (EndPoint endPoint in endPt.Attachments)
                        return ClosedPathSearch(pathStartPt, endPoint, endPtConnections, ref strokesSoFar);
                }
            }

            return false;
        }

        #endregion

        #region Interface Methods

        /// <summary>
        /// Adds a stroke to the FeatureSketch, which creates a Feature Stroke 
        /// for this Sketch Stroke, and passes it along to the other 
        /// sketches (intersection and pairwise).
        /// </summary>
        /// <param name="stroke">Sketch stroke to add.</param>
        public void AddStroke(Substroke stroke)
        {
            if (m_SketchBox.Width == 0f && m_SketchBox.Height == 0f)
                m_SketchBox = Compute.BoundingBox(stroke.Points);

            try
            {
                Substroke[] strokeArray = new Substroke[1] { stroke };
                Sketch.Stroke s = new Sketch.Stroke(strokeArray, stroke.XmlAttrs);
                m_Sketch.AddStroke(s);

                if (!Compute.StrokeInsideBoundingBox(stroke, m_SketchBox))
                    m_SketchBox = Compute.BoundingBox(m_Sketch.Points);

                FeatureSingleStroke feature = GetFeatureStrokeAndUpdateSums(stroke);

                FindActualExtensionLength(stroke, AvgArcLength);

                if (!m_OrderOfStrokes.ContainsValue(stroke) && !m_OrderOfStrokes.ContainsKey(feature.Stroke.XmlAttrs.Time.Value))
                    m_OrderOfStrokes.Add(feature.Stroke.XmlAttrs.Time.Value, feature.Stroke);

                m_IsIntersectionFinished = false;
                m_IsPairwiseFinished = false;
                m_IntersectionSketch.AddStroke(stroke);
                m_PairwiseFeatureSketch.AddStroke(stroke);
            }
            catch (Exception e)
            {
                Console.WriteLine("FeatureSketch AddStroke: " + e.Message);
                //throw e;
            }
        }

        private void UpdateExtensionLengths()
        {
            double avgArcLength = AvgArcLength;

            foreach (Substroke s in m_Sketch.Substrokes)
            {
                double d1 = (s.SpatialLength + avgArcLength) / 2.0;
                double extensionLength = Math.Min(avgArcLength, d1) * Compute.THRESHOLD;
                
                if (m_ExtensionLengthsActual.ContainsKey(s))
                    m_ExtensionLengthsActual[s] = extensionLength;
                else
                    m_ExtensionLengthsActual.Add(s, extensionLength);

                if (!m_ExtensionLengthsExtreme.ContainsKey(s))
                    m_ExtensionLengthsExtreme.Add(s, Math.Max(avgArcLength, s.SpatialLength));

                if (m_ExtensionLengthsExtreme[s] < extensionLength)
                {
                    m_ExtensionLengthsExtreme[s] = extensionLength;
                    if (m_IntersectionSketch != null)
                        m_IntersectionSketch.UpdateIntersections(s);
                }
                    
            }
        }

        /// <summary>
        /// Removes the stroke from the feature sketch and all the downstream 
        /// representations and features.
        /// </summary>
        /// <param name="stroke">Stroke to Remove.</param>
        public void RemoveStroke(Substroke stroke)
        {
            if (m_Sketch.SubstrokesL.Contains(stroke))
                m_Sketch.RemoveSubstroke(stroke);

            m_SketchBox = Compute.BoundingBox(m_Sketch.Points);

            if (m_StrokeFeatures.ContainsKey(stroke))
            {
                if (m_FeatureListSingle.ContainsKey("Arc Length"))
                    m_ArcLengthSum -= (double)m_StrokeFeatures[stroke].Spatial.Length;
                if (m_FeatureListSingle.ContainsKey("Bounding Box Width")) 
                    m_BBoxAreaSum -= m_StrokeFeatures[stroke].Spatial.Area;
                if (m_FeatureListSingle.ContainsKey("Bounding Box Height")) 
                    m_BBoxHeightSum -= m_StrokeFeatures[stroke].Spatial.Height;
                if (m_FeatureListSingle.ContainsKey("Bounding Box Area")) 
                    m_BBoxWidthSum -= m_StrokeFeatures[stroke].Spatial.Width;
                if (m_FeatureListSingle.ContainsKey("Average Speed"))
                    m_StrokeSpeedSum -= m_StrokeFeatures[stroke].Features["Average Speed"].Value;

                m_StrokeFeatures.Remove(stroke);
            }

            if (m_OrderOfStrokes.ContainsValue(stroke))
            {
                int index = m_OrderOfStrokes.IndexOfValue(stroke);
                m_OrderOfStrokes.RemoveAt(index);
            }

            if (m_ExtensionLengthsActual.ContainsKey(stroke))
                m_ExtensionLengthsActual.Remove(stroke);

            if (m_ExtensionLengthsExtreme.ContainsKey(stroke))
                m_ExtensionLengthsExtreme.Remove(stroke);

            if (m_IntersectionSketch.Strokes.Contains(stroke))
                m_IntersectionSketch.RemoveStroke(stroke);

            if (m_PairwiseFeatureSketch.Strokes.Contains(stroke))
                m_PairwiseFeatureSketch.RemoveStroke(stroke);
        }

        #endregion

        #region Other Functions

        private SortedList<ulong, Substroke> GetOrderOfStrokesFromSketch(Sketch.Sketch sketch)
        {
            SortedList<ulong, Substroke> order = new SortedList<ulong, Substroke>();
            foreach (Substroke stroke in sketch.Substrokes)
            {
                if (!order.ContainsValue(stroke))
                {
                    if (stroke.XmlAttrs.Time.HasValue)
                    {
                        ulong time = stroke.XmlAttrs.Time.Value;
                        while (order.ContainsKey(time))
                            time++;

                        order.Add(time, stroke);
                    }
                }
            }

            return order;
        }

        private FeatureSingleStroke GetFeatureStrokeAndUpdateSums(Substroke stroke)
        {
            FeatureSingleStroke feature = new FeatureSingleStroke(stroke, m_Sketch, m_FeatureListSingle);
            m_StrokeFeatures.Add(stroke, feature);

            if (feature.Features.ContainsKey("Arc Length"))
                m_ArcLengthSum += feature.Features["Arc Length"].Value;
            if (feature.Features.ContainsKey("Bounding Box Width"))
                m_BBoxWidthSum += feature.Features["Bounding Box Width"].Value;
            if (feature.Features.ContainsKey("Bounding Box Height"))
                m_BBoxHeightSum += feature.Features["Bounding Box Height"].Value;
            if (feature.Features.ContainsKey("Bounding Box Area"))
                m_BBoxAreaSum += feature.Features["Bounding Box Area"].Value;
            if (feature.Features.ContainsKey("Average Pen Speed"))
                m_StrokeSpeedSum += feature.Features["Average Pen Speed"].Value;
            //if (!m_ExtensionLengthsExtreme.ContainsKey(stroke))
                //m_ExtensionLengthsExtreme.Add(stroke, Math.Max(stroke.SpatialLength, AvgArcLength) * Compute.THRESHOLD);

            return feature;
        }

        private void FindActualExtensionLength(Substroke stroke, double avgArcLength)
        {
            double d1 = (stroke.SpatialLength + avgArcLength) / 2.0;
            double extensionLength = Math.Min(avgArcLength, d1) * Compute.THRESHOLD;
            if (!m_ExtensionLengthsActual.ContainsKey(stroke))
                m_ExtensionLengthsActual.Add(stroke, extensionLength);

            UpdateExtensionLengths();
        }

        public void CheckDone()
        {
            if (m_IsBWfinished && m_IsIntersectionFinished && m_IsPairwiseFinished)
                OnCompletedFeatureSketch(new EventArgs());
        }

        /// <summary>
        /// Update both the Single-stroke features' and the pairwise features' normalizers
        /// </summary>
        public void UpdateNormalizers()
        {
            UpdateNormalizersSingle();
            UpdateNormalizersPair();
        }

        /// <summary>
        /// Function to set the normalizers for feature-stroke-pairs based on sketch size
        /// </summary>
        public void UpdateNormalizersPair()
        {
            double sketchDiagonal = Math.Sqrt(Math.Pow(m_SketchBox.Height, 2.0) + Math.Pow(m_SketchBox.Width, 2.0));

            foreach (KeyValuePair<string, bool> pair in m_FeatureListPair)
            {
                string featureName = pair.Key;
                bool useFeature = pair.Value;
                if (useFeature)
                {
                    foreach (FeatureStrokePair featureStrokePair in m_PairwiseFeatureSketch.AllFeaturePairs)
                    {
                        #region Switch
                        switch (featureName)
                        {
                            case ("Minimum Distance"):
                                featureStrokePair.Features[featureName].SetNormalizer(sketchDiagonal);
                                break;
                            case ("Maximum Distance"):
                                featureStrokePair.Features[featureName].SetNormalizer(sketchDiagonal);
                                break;
                            case ("Centroid Distance"):
                                featureStrokePair.Features[featureName].SetNormalizer(sketchDiagonal);
                                break;
                            case ("Horizontal Overlap"):
                                featureStrokePair.Features[featureName].SetNormalizer(sketchDiagonal);
                                break;
                            case ("Vertical Overlap"):
                                featureStrokePair.Features[featureName].SetNormalizer(sketchDiagonal);
                                break;
                            default:
                                break;
                        }
                        #endregion
                    }
                }
            }
        }

        /// <summary>
        /// Function to set the normalizers for each feature-stroke based on sketch averages
        /// </summary>
        public void UpdateNormalizersSingle()
        {
            double avgArcLength = AvgArcLength;
            double avgWidth = AvgBBoxWidth;
            double avgHeight = AvgBBoxHeight;
            double avgArea = AvgBBoxArea;
            double avgPenSpeed = AvgPenSpeed;

            foreach (KeyValuePair<string, bool> pair in m_FeatureListSingle)
            {
                string featureName = pair.Key;
                bool useFeature = pair.Value;
                if (useFeature)
                {
                    foreach (KeyValuePair<Substroke, FeatureSingleStroke> featureStroke in m_StrokeFeatures)
                    {
                        #region Switch
                        switch (featureName)
                        {
                            case ("Arc Length"):
                                featureStroke.Value.Features[featureName].SetNormalizer(avgArcLength);
                                break;
                            case ("Bounding Box Width"):
                                featureStroke.Value.Features[featureName].SetNormalizer(avgWidth);
                                break;
                            case ("Bounding Box Height"):
                                featureStroke.Value.Features[featureName].SetNormalizer(avgHeight);
                                break;
                            case ("Bounding Box Area"):
                                featureStroke.Value.Features[featureName].SetNormalizer(avgArea);
                                break;
                            case ("Average Pen Speed"):
                                featureStroke.Value.Features[featureName].SetNormalizer(avgPenSpeed);
                                break;
                            case ("Maximum Pen Speed"):
                                featureStroke.Value.Features[featureName].SetNormalizer(avgPenSpeed);
                                break;
                            case ("Minimum Pen Speed"):
                                featureStroke.Value.Features[featureName].SetNormalizer(avgPenSpeed);
                                break;
                            case ("Difference Between Maximum and Minimum Pen Speed"):
                                featureStroke.Value.Features[featureName].SetNormalizer(avgPenSpeed);
                                break;
                            default:
                                break;
                        }
                        #endregion
                    }
                }
            }
        }

        #endregion

        #region Getters

        /// <summary>
        /// Unique Id number for the FeatureSketch
        /// </summary>
        public Guid Id
        {
            get { return m_Id; }
        }

        /// <summary>
        /// XML Sketch that this FeatureSketch represents.
        /// </summary>
        public Sketch.Sketch Sketch
        {
            get { return m_Sketch; }
        }

        /// <summary>
        /// The average Arc Length in the sketch.
        /// </summary>
        public double AvgArcLength
        {
            get 
            {
                if (m_Sketch.Substrokes.Length > 0)
                    return m_ArcLengthSum / m_Sketch.Substrokes.Length;
                else
                    return 0.0;
            }
        }

        /// <summary>
        /// The average Width of a stroke's bounding box in the sketch.
        /// </summary>
        private double AvgBBoxWidth
        {
            get 
            { 
                if (m_Sketch.Substrokes.Length > 0)
                    return m_BBoxWidthSum / m_Sketch.Substrokes.Length;
                else
                    return 0.0;
            }
        }

        /// <summary>
        /// The average Height of a stroke's bounding box in the sketch.
        /// </summary>
        private double AvgBBoxHeight
        {
            get 
            {
                if (m_Sketch.Substrokes.Length > 0)
                    return m_BBoxHeightSum / m_Sketch.Substrokes.Length;
                else
                    return 0.0;
            }
        }

        /// <summary>
        /// The average Area of a stroke's bounding box in the sketch.
        /// </summary>
        private double AvgBBoxArea
        {
            get 
            {
                if (m_Sketch.Substrokes.Length > 0)
                    return m_BBoxAreaSum / m_Sketch.Substrokes.Length;
                else
                    return 0.0;
            }
        }

        /// <summary>
        /// The average Pen Speed of a stroke in the sketch.
        /// </summary>
        private double AvgPenSpeed
        {
            get 
            {
                if (m_Sketch.Substrokes.Length > 0) 
                    return m_StrokeSpeedSum / m_Sketch.Substrokes.Length;
                else
                    return 0.0;
            }
        }

        /// <summary>
        /// Gets all the Feature strokes for the sketch.
        /// </summary>
        public Dictionary<Substroke, FeatureSingleStroke> StrokeFeatures
        {
            get { return m_StrokeFeatures; }
        }

        /// <summary>
        /// Gets the input values for the Neural Network which will be computing the classification 
        /// for each stroke. 
        /// </summary>
        /// <param name="stroke2values">This can be used to ensure that the 
        /// stroke feature values are in the correct order.</param>
        /// <param name="normalized">Whether the values should be normalized using sketch averages and size</param>
        /// <param name="prepared">Whether the values should be prepared for the neural network using 
        /// a softmax logistic, which puts all values in the range of 0.0 - 1.0</param>
        /// <returns>All feature values for single-stroke classification.</returns>
        public double[][] GetValuesSingle(out Dictionary<Substroke, double[]> stroke2values, ValuePreparationStage stage)
        {
            stroke2values = new Dictionary<Substroke, double[]>();
            double[][] testSet = new double[m_StrokeFeatures.Count][];

            try
            {
                int m = 0;
                UpdateNormalizersSingle();

                foreach (KeyValuePair<Substroke, FeatureSingleStroke> pair in m_StrokeFeatures)
                {
                    Substroke s = pair.Key;
                    List<double> set = new List<double>();

                    if (m_StrokeFeatures.ContainsKey(s))
                    {
                        foreach (KeyValuePair<string, bool> pair2 in m_FeatureListSingle)
                        {
                            if (m_StrokeFeatures[s].Features.ContainsKey(pair2.Key) && pair2.Value)
                            {
                                if (stage == ValuePreparationStage.ActualValue)
                                    set.Add(m_StrokeFeatures[s].Features[pair2.Key].Value);
                                else
                                {
                                    double value = m_StrokeFeatures[s].Features[pair2.Key].NormalizedValue;
                                    if (stage == ValuePreparationStage.Normalized)
                                        set.Add(value);
                                    else if (stage == ValuePreparationStage.PreparedForNN)
                                    {
                                        double[] meansAndStdDevs;
                                        bool success = m_AvgsAndStdDevs.TryGetValue(pair2.Key, out meansAndStdDevs);
                                        if (success)
                                            set.Add(Compute.GetSoftmaxNormalizedValue(value, meansAndStdDevs[0], meansAndStdDevs[1]));
                                        else
                                            set.Add(value);
                                    }
                                }
                            }
                        }
                        stroke2values.Add(s, set.ToArray());
                        testSet[m++] = set.ToArray();
                    }

                }

                
            }
            catch (Exception e)
            {
                Console.WriteLine("FeatureSketch GetValuesSingle: " + e.Message);
                //throw e;
            }

            return testSet;
        }

        /// <summary>
        /// Gets the input values for the Neural Network which will be computing the classification 
        /// for each stroke. 
        /// </summary>
        /// <param name="stroke2values">This can be used to ensure that the 
        /// stroke feature values are in the correct order.</param>
        /// <returns>All feature values for single-stroke classification.</returns>
        public Dictionary<string, double[][]> GetValuesPairwise(out Dictionary<string, Dictionary<FeatureStrokePair, double[]>> pair2values, Dictionary<Substroke, string> strokeClassifications, ValuePreparationStage stage)
        {
            Dictionary<string, List<double[]>> class2Values = new Dictionary<string, List<double[]>>(m_PairwiseFeatureSketch.AllFeaturePairs.Count);   
            pair2values = new Dictionary<string, Dictionary<FeatureStrokePair, double[]>>(m_PairwiseFeatureSketch.AllFeaturePairs.Count);

            try
            {
                UpdateNormalizersPair();
             

                foreach (FeatureStrokePair strokePair in m_PairwiseFeatureSketch.AllFeaturePairs)
                {
                    Substroke s1 = strokePair.StrokeA;
                    Substroke s2 = strokePair.StrokeB;
                    List<double> set = new List<double>();

                    // Strokes are same class
                    if (strokeClassifications.ContainsKey(s1) && strokeClassifications.ContainsKey(s2) && strokeClassifications[s1] == strokeClassifications[s2])
                    {
                        string strokeClass = strokeClassifications[s1];
                        double min1 = m_PairwiseFeatureSketch.MinDistanceFromStroke(s1);
                        double min2 = m_PairwiseFeatureSketch.MinDistanceFromStroke(s2);

                        // Loop through all features
                        foreach (KeyValuePair<string, bool> useFeature in m_FeatureListPair)
                        {
                            // Special condition for Distance ratios, compute the value here!
                            if (useFeature.Key == "Distance Ratio A" || useFeature.Key == "Distance Ratio B")
                            {
                                double distance = strokePair.SubstrokeDistance.Min;
                                double value = 0.0;
                                if (useFeature.Key == "Distance Ratio A")
                                    value = Compute.GetDistanceRatio(distance, min1);
                                else
                                    value = Compute.GetDistanceRatio(distance, min2);

                                if (stage == ValuePreparationStage.ActualValue || stage == ValuePreparationStage.Normalized)
                                    set.Add(value);
                                else if (stage == ValuePreparationStage.PreparedForNN)
                                {
                                    double[] avgs;
                                    string key = strokeClass + "_" + useFeature.Key;
                                    bool success = m_AvgsAndStdDevs.TryGetValue(key, out avgs);
                                    if (success)
                                        set.Add(Compute.GetSoftmaxNormalizedValue(value, avgs[0], avgs[1]));
                                }
                            }
                            // Not a distance ratio, go about getting the value normally
                            else if (strokePair.Features.ContainsKey(useFeature.Key) && useFeature.Value)
                            {
                                if (stage == ValuePreparationStage.ActualValue)
                                    set.Add(strokePair.Features[useFeature.Key].Value);
                                else
                                {
                                    double value = strokePair.Features[useFeature.Key].NormalizedValue;
                                    if (stage == ValuePreparationStage.Normalized)
                                        set.Add(value);
                                    else if (stage == ValuePreparationStage.PreparedForNN)
                                    {
                                        double[] avgs;
                                        string key = strokeClass + "_" + useFeature.Key;
                                        bool success = m_AvgsAndStdDevs.TryGetValue(key, out avgs);
                                        if (success)
                                            set.Add(Compute.GetSoftmaxNormalizedValue(value, avgs[0], avgs[1]));
                                        else
                                            set.Add(value);
                                    }
                                }
                            }
                        }

                        // Add the data from the latest stroke pair to the list
                        // List is separated by class name
                        if (class2Values.ContainsKey(strokeClass))
                            class2Values[strokeClass].Add(set.ToArray());
                        else
                        {
                            List<double[]> values = new List<double[]>();
                            values.Add(set.ToArray());
                            class2Values.Add(strokeClass, values);
                        }

                        // Add the data from the latest stroke pair to the list
                        // List is separated by class name
                        
                        if (pair2values.ContainsKey(strokeClass))
                        {
                            if (!pair2values[strokeClass].ContainsKey(strokePair))
                                pair2values[strokeClass].Add(strokePair, set.ToArray());
                        }
                        else
                        {
                            Dictionary<FeatureStrokePair, double[]> values = new Dictionary<FeatureStrokePair, double[]>();
                            values.Add(strokePair, set.ToArray());
                            pair2values.Add(strokeClass, values);
                        }
                    }
                }

                
            }
            catch (Exception e)
            {
                Console.WriteLine("FeatureSketch GetValuesPairwise: " + e.Message);
                //throw e;
            }

            Dictionary<string, double[][]> output = new Dictionary<string, double[][]>(class2Values.Count);
            foreach (KeyValuePair<string, List<double[]>> orig in class2Values)
                output.Add(orig.Key, orig.Value.ToArray());

            return output;
        }

        /// <summary>
        /// List of which features are on/off for single-stroke classification.
        /// </summary>
        public Dictionary<string, bool> FeatureListSingle
        {
            get { return m_FeatureListSingle; }
            set { m_FeatureListSingle = value; }
        }

        /// <summary>
        /// List of which features are on/off for pairwise (grouping) classification.
        /// </summary>
        public Dictionary<string, bool> FeatureListPair
        {
            get { return m_FeatureListPair; }
            set { m_FeatureListPair = value; }
        }

        /// <summary>
        /// Gets a representation of all intersections in the sketch.
        /// </summary>
        public IntersectionSketch IntersectionSketch
        {
            get { return m_IntersectionSketch; }
        }

        /// <summary>
        /// Gets a representation of all pairwise features in the sketch.
        /// </summary>
        public PairwiseFeatureSketch PairwiseFeatureSketch
        {
            get { return m_PairwiseFeatureSketch; }
        }

        /// <summary>
        /// Gets the order of strokes in the sketch.
        /// </summary>
        public SortedList<ulong, Substroke> OrderOfStrokes
        {
            get { return m_OrderOfStrokes; }
        }

        /// <summary>
        /// Gets the bounding box around the sketch.
        /// </summary>
        public System.Drawing.RectangleF SketchBox
        {
            get { return m_SketchBox; }
        }

        /// <summary>
        /// Gets the extension lengths used for each stroke in the sketch.
        /// </summary>
        public Dictionary<Substroke, double> StrokeExtensionLengths
        {
            get { return m_ExtensionLengthsActual; }
        }

        #endregion

        #region Serialization

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public FeatureSketch(SerializationInfo info, StreamingContext context)
        {
            m_Id = (Guid)info.GetValue("Id", typeof(Guid));
            m_Sketch = (Sketch.Sketch)info.GetValue("Sketch", typeof(Sketch.Sketch));
            m_ArcLengthSum = (double)info.GetValue("ArcLengthSum", typeof(double));
            m_BBoxAreaSum = (double)info.GetValue("BBoxAreaSum", typeof(double));
            m_BBoxHeightSum = (double)info.GetValue("BBoxHeightSum", typeof(double));
            m_BBoxWidthSum = (double)info.GetValue("BBoxWidthSum", typeof(double));
            m_ExtensionLengthsActual = (Dictionary<Substroke, double>)info.GetValue("ExtensionLengthsActual", typeof(Dictionary<Substroke, double>));
            m_ExtensionLengthsExtreme = (Dictionary<Substroke, double>)info.GetValue("ExtensionLengthsExtreme", typeof(Dictionary<Substroke, double>));
            m_FeatureListPair = (Dictionary<string, bool>)info.GetValue("FeatureListPair", typeof(Dictionary<string, bool>));
            m_FeatureListSingle = (Dictionary<string, bool>)info.GetValue("FeatureListSingle", typeof(Dictionary<string, bool>));
            m_IntersectionSketch = (IntersectionSketch)info.GetValue("IntersectionSketch", typeof(IntersectionSketch));
            m_IntersectionSketch.CompletedIntersection += new IntersectionSketchCompletedEventHandler(m_IntersectionSketch_CompletedIntersection);
            m_OrderOfStrokes = (SortedList<ulong, Substroke>)info.GetValue("OrderOfStrokes", typeof(SortedList<ulong, Substroke>));
            m_PairwiseFeatureSketch = (PairwiseFeatureSketch)info.GetValue("PairwiseFeatureSketch", typeof(PairwiseFeatureSketch));
            m_PairwiseFeatureSketch.CompletedPairwiseFeatures += new PairwiseFeatureSketchCompletedEventHandler(m_PairwiseFeatureSketch_CompletedPairwiseFeatures);
            m_SketchBox = (System.Drawing.RectangleF)info.GetValue("SketchBox", typeof(System.Drawing.RectangleF));
            m_StrokeFeatures = (Dictionary<Substroke, FeatureSingleStroke>)info.GetValue("StrokeFeatures", typeof(Dictionary<Substroke, FeatureSingleStroke>));
            m_StrokeSpeedSum = (double)info.GetValue("StrokeSpeedSum", typeof(double));
            m_AvgsAndStdDevs = (Dictionary<string, double[]>)info.GetValue("AvgsAndStdDevs", typeof(Dictionary<string, double[]>));

            m_BackgroundWorker = new BackgroundWorker();
            m_BackgroundWorker.WorkerSupportsCancellation = true;
            m_BackgroundWorker.WorkerReportsProgress = true;
            m_BackgroundWorker.DoWork += new DoWorkEventHandler(m_BackgroundWorker_DoWork);
            m_BackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_BackgroundWorker_RunWorkerCompleted);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Id", m_Id);
            info.AddValue("Sketch", m_Sketch);
            info.AddValue("ArcLengthSum", m_ArcLengthSum);
            info.AddValue("BBoxAreaSum", m_BBoxAreaSum);
            info.AddValue("BBoxHeightSum", m_BBoxHeightSum);
            info.AddValue("BBoxWidthSum", m_BBoxWidthSum);
            info.AddValue("ExtensionLengthsActual", m_ExtensionLengthsActual);
            info.AddValue("ExtensionLengthsExtreme", m_ExtensionLengthsExtreme);
            info.AddValue("FeatureListPair", m_FeatureListPair);
            info.AddValue("FeatureListSingle", m_FeatureListSingle);
            info.AddValue("IntersectionSketch", m_IntersectionSketch);
            info.AddValue("OrderOfStrokes", m_OrderOfStrokes);
            info.AddValue("PairwiseFeatureSketch", m_PairwiseFeatureSketch);
            info.AddValue("SketchBox", m_SketchBox);
            info.AddValue("StrokeFeatures", m_StrokeFeatures);
            info.AddValue("StrokeSpeedSum", m_StrokeSpeedSum);
            info.AddValue("AvgsAndStdDevs", m_AvgsAndStdDevs);
        }

        #endregion
    }
}
