using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Runtime.Serialization;
using Sketch;
using Utilities;

namespace FeatureSpace
{
    public delegate void PairwiseFeatureSketchCompletedEventHandler(object sender, EventArgs e);

    [Serializable]
    public class PairwiseFeatureSketch : ISerializable
    {
        #region Member Variables

        /// <summary>
        /// Sketch
        /// </summary>
        private Sketch.Sketch m_Sketch;

        /// <summary>
        /// All Strokes in the Pairwise Feature Sketch
        /// </summary>
        private List<Substroke> m_Strokes;

        /// <summary>
        /// List of Features between a given pair of strokes
        /// </summary>
        private List<FeatureStrokePair> m_AllFeaturePairs;

        private List<StrokePair> m_AllStrokePairs;

        /// <summary>
        /// Lookup for a stroke
        /// </summary>
        private Dictionary<Substroke, List<FeatureStrokePair>> m_Stroke2Pairs;

        private Dictionary<StrokePair, FeatureStrokePair> m_Pair2FeaturePair;

        private Dictionary<string, bool> m_FeaturesToUse;

        /// <summary>
        /// Performs calculations in the background, so that 
        /// the program remains responsive
        /// </summary>
        [NonSerialized]
        private BackgroundWorker m_BackgroundWorker;

        /// <summary>
        /// Flag to know whether the background worker has finished
        /// If the bw is still busy, it adds the incoming strokes to 
        /// a queue.
        /// </summary>
        [NonSerialized]
        private bool m_IsBWfinished = true;

        /// <summary>
        /// Queue that incoming strokes are added to if the bw is busy
        /// </summary>
        private Queue<Substroke> m_StrokesToAdd;

        #endregion

        #region Events

        public event PairwiseFeatureSketchCompletedEventHandler CompletedPairwiseFeatures;

        protected virtual void OnCompletedPairwiseFeatures(EventArgs e)
        {
            if (CompletedPairwiseFeatures != null)
                CompletedPairwiseFeatures(this, e);
        }

        void m_BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                if (m_IsBWfinished)
                    OnCompletedPairwiseFeatures(new EventArgs());
                else
                {
                    AddStroke(m_StrokesToAdd.Dequeue());
                    if (m_StrokesToAdd.Count == 0)
                        m_IsBWfinished = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("PairwiseSketch BWdone: " + ex.Message);
                //throw ex;
            }
        }

        #endregion

        #region Constructors

        public PairwiseFeatureSketch(Sketch.Sketch sketch, Dictionary<string, bool> FeaturesToUse)
        {
            m_BackgroundWorker = new BackgroundWorker();
            m_BackgroundWorker.WorkerReportsProgress = true;
            m_BackgroundWorker.WorkerSupportsCancellation = true;
            m_BackgroundWorker.DoWork += new DoWorkEventHandler(bw_DoWork);
            m_BackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_BackgroundWorker_RunWorkerCompleted);
            m_Sketch = sketch;
            m_FeaturesToUse = FeaturesToUse;
            m_StrokesToAdd = new Queue<Substroke>();
            m_Strokes = new List<Substroke>();
            m_AllFeaturePairs = new List<FeatureStrokePair>();
            m_AllStrokePairs = new List<StrokePair>();
            m_Stroke2Pairs = new Dictionary<Substroke, List<FeatureStrokePair>>();
            m_Pair2FeaturePair = new Dictionary<StrokePair, FeatureStrokePair>();

            foreach (Substroke stroke in m_Sketch.Substrokes)
            {
                if (!m_Strokes.Contains(stroke))
                    m_Strokes.Add(stroke);

                List<FeatureStrokePair> pairs = new List<FeatureStrokePair>(m_Strokes.Count);
                foreach (Substroke s in m_Strokes)
                {
                    if (stroke != s)
                    {
                        try
                        {
                            StrokePair strokePair = new StrokePair(s, stroke);
                            FeatureStrokePair pair = new FeatureStrokePair(stroke, s, strokePair, m_FeaturesToUse);
                            pairs.Add(pair);

                            if (!m_Pair2FeaturePair.ContainsKey(strokePair))
                                m_Pair2FeaturePair.Add(strokePair, pair);

                            m_AllStrokePairs.Add(strokePair);
                            m_AllFeaturePairs.Add(pair);

                            if (m_Stroke2Pairs.ContainsKey(s))
                                m_Stroke2Pairs[s].Add(pair);

                        }
                        catch (Exception exc)
                        {
                            Console.WriteLine("PairwiseSketch Constructor: " + exc.Message);
                            //throw exc;
                        }
                    }
                }

                if (!m_Stroke2Pairs.ContainsKey(stroke))
                    m_Stroke2Pairs.Add(stroke, pairs);
            } 
        }

        #endregion

        #region Methods

        public void AddStroke(Substroke stroke)
        {
            try
            {
                if (!m_BackgroundWorker.IsBusy)
                {
                    if (m_Strokes.Contains(stroke))
                        return;

                    if (m_Stroke2Pairs.ContainsKey(stroke))
                        return;

                    m_BackgroundWorker.RunWorkerAsync(stroke);
                }
                else
                {
                    m_IsBWfinished = false;
                    m_StrokesToAdd.Enqueue(stroke);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("PairwiseSketch AddStroke: " + e.Message);
                //throw e;
            }
        }

        public void RemoveStroke(Substroke stroke)
        {
            if (m_Strokes.Contains(stroke))
                m_Strokes.Remove(stroke);

            if (m_Stroke2Pairs.ContainsKey(stroke))
            {
                foreach (FeatureStrokePair pair in m_Stroke2Pairs[stroke])
                {
                    if (m_AllStrokePairs.Contains(pair.StrokePair))
                        m_AllStrokePairs.Remove(pair.StrokePair);
                    m_AllFeaturePairs.Remove(pair);
                }

                m_Stroke2Pairs.Remove(stroke);
            }

            List<StrokePair> toRemove = new List<StrokePair>();
            foreach (KeyValuePair<StrokePair, FeatureStrokePair> pair in m_Pair2FeaturePair)
                if (pair.Key.Includes(stroke))
                    toRemove.Add(pair.Key);

            foreach (StrokePair pair in toRemove)
                if (m_Pair2FeaturePair.ContainsKey(pair))
                    m_Pair2FeaturePair.Remove(pair);
        }

        private void FindPairInfo(List<Substroke> strokes, int indexOfStroke)
        {
            if (indexOfStroke >= strokes.Count)
                return;

            Substroke stroke = strokes[indexOfStroke];
            lock (stroke)
            {
                int size = Math.Max(1, indexOfStroke - 1);
                List<FeatureStrokePair> pairs = new List<FeatureStrokePair>(size);
                for (int i = 0; i < indexOfStroke; i++)
                {
                    Substroke s = strokes[i];
                    lock (s)
                    {
                        try
                        {
                            StrokePair strokePair = new StrokePair(s, stroke);
                            FeatureStrokePair pair = new FeatureStrokePair(stroke, s, strokePair, m_FeaturesToUse);
                            pairs.Add(pair);
                            
                            lock (m_Pair2FeaturePair)
                            {
                                if (!m_Pair2FeaturePair.ContainsKey(strokePair))
                                    m_Pair2FeaturePair.Add(strokePair, pair);
                            }
                            lock (m_AllStrokePairs)
                            {
                                m_AllStrokePairs.Add(strokePair);
                            }
                            lock (m_AllFeaturePairs)
                            {
                                m_AllFeaturePairs.Add(pair);
                            }
                            lock (m_Stroke2Pairs)
                            {
                                if (m_Stroke2Pairs.ContainsKey(s))
                                    m_Stroke2Pairs[s].Add(pair);
                            }
                        }
                        catch (Exception exc)
                        {
                            Console.WriteLine("PairwiseSketch FindInfo: " + exc.Message);
                            //throw exc;
                        }
                    }
                }

                if (!m_Strokes.Contains(stroke))
                    m_Strokes.Add(stroke);

                if (!m_Stroke2Pairs.ContainsKey(stroke))
                    m_Stroke2Pairs.Add(stroke, pairs);
            }
        }

        #endregion

        #region Background / Threading stuff

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            Substroke stroke = (Substroke)e.Argument;
            lock (stroke)
            {
                List<FeatureStrokePair> pairs = new List<FeatureStrokePair>(m_Strokes.Count);
                foreach (Substroke s in m_Strokes)
                {
                    lock (s)
                    {
                        try
                        {
                            StrokePair strokePair = new StrokePair(s, stroke);
                            FeatureStrokePair pair = new FeatureStrokePair(stroke, s, strokePair, m_FeaturesToUse);
                            pairs.Add(pair);
                            
                            lock (m_Pair2FeaturePair)
                            {
                                if (!m_Pair2FeaturePair.ContainsKey(strokePair))
                                    m_Pair2FeaturePair.Add(strokePair, pair);
                            }
                            lock (m_AllStrokePairs)
                            {
                                m_AllStrokePairs.Add(strokePair);
                            }
                            lock (m_AllFeaturePairs)
                            {
                                m_AllFeaturePairs.Add(pair);
                            }
                            lock (m_Stroke2Pairs)
                            {
                                if (m_Stroke2Pairs.ContainsKey(s))
                                    m_Stroke2Pairs[s].Add(pair);
                            }
                        }
                        catch (Exception exc)
                        {
                            Console.WriteLine("PairwiseSketch doWork: " + exc.Message);
                            //throw exc;
                        }
                    }
                }

                if (!m_Strokes.Contains(stroke))
                    m_Strokes.Add(stroke);

                if (!m_Stroke2Pairs.ContainsKey(stroke))
                    m_Stroke2Pairs.Add(stroke, pairs);
            }
        }

        #endregion

        #region Getters

        public FeatureStrokePair FeatureStrokePair(StrokePair strokePair)
        {
            if (m_Pair2FeaturePair.ContainsKey(strokePair))
                return m_Pair2FeaturePair[strokePair];
            else
                return new FeatureStrokePair(strokePair.Stroke1, strokePair.Stroke2, strokePair, m_FeaturesToUse);

        }

        public double MinDistanceFromStroke(Substroke stroke)
        {
            double min = double.PositiveInfinity;
            if (m_Stroke2Pairs.ContainsKey(stroke))
            {
                List<FeatureStrokePair> pairs = m_Stroke2Pairs[stroke];
                foreach (FeatureStrokePair pair in pairs)
                    min = Math.Min(min, pair.SubstrokeDistance.Min);
            }

            return min;
        }

        public List<Substroke> Strokes
        {
            get { return m_Strokes; }
        }

        public Dictionary<Substroke, List<SubstrokeDistance>> AllDistances
        {
            get 
            {
                Dictionary<Substroke, List<SubstrokeDistance>> distances = new Dictionary<Substroke, List<SubstrokeDistance>>();

                foreach (KeyValuePair<Substroke, List<FeatureStrokePair>> pair in m_Stroke2Pairs)
                {
                    List<SubstrokeDistance> d = new List<SubstrokeDistance>();

                    foreach (FeatureStrokePair val in pair.Value)
                        d.Add(val.SubstrokeDistance);

                    distances.Add(pair.Key, d);
                }

                return distances;
            }
        }

        public List<FeatureStrokePair> AllFeaturePairs
        {
            get
            {
                return m_AllFeaturePairs;
            }
        }

        public List<StrokePair> AllStrokePairs
        {
            get
            {
                return m_AllStrokePairs;
            }
        }

        #endregion

        #region Serialization

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public PairwiseFeatureSketch(SerializationInfo info, StreamingContext context)
        {
            m_AllStrokePairs = (List<StrokePair>)info.GetValue("AllStrokePairs", typeof(List<StrokePair>));
            m_AllFeaturePairs = (List<FeatureStrokePair>)info.GetValue("AllPairs", typeof(List<FeatureStrokePair>));
            m_Sketch = (Sketch.Sketch)info.GetValue("Sketch", typeof(Sketch.Sketch));
            m_Stroke2Pairs = (Dictionary<Substroke, List<FeatureStrokePair>>)info.GetValue("Stroke2Pairs", typeof(Dictionary<Substroke, List<FeatureStrokePair>>));
            m_Pair2FeaturePair = (Dictionary<StrokePair, FeatureStrokePair>)info.GetValue("Pair2FeaturePair", typeof(Dictionary<StrokePair, FeatureStrokePair>));
            m_Strokes = (List<Substroke>)info.GetValue("Strokes", typeof(List<Substroke>));
            m_StrokesToAdd = (Queue<Substroke>)info.GetValue("StrokesToAdd", typeof(Queue<Substroke>));
            
            m_BackgroundWorker = new BackgroundWorker();
            m_BackgroundWorker.WorkerSupportsCancellation = true;
            m_BackgroundWorker.WorkerReportsProgress = true;
            m_BackgroundWorker.DoWork += new DoWorkEventHandler(bw_DoWork);
            m_BackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_BackgroundWorker_RunWorkerCompleted);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("AllStrokePairs", m_AllStrokePairs);
            info.AddValue("AllPairs", m_AllFeaturePairs);
            info.AddValue("Sketch", m_Sketch);
            info.AddValue("Stroke2Pairs", m_Stroke2Pairs);
            info.AddValue("Pair2FeaturePair", m_Pair2FeaturePair);
            info.AddValue("Strokes", m_Strokes);
            info.AddValue("StrokesToAdd", m_StrokesToAdd);
        }

        #endregion
    }
}
