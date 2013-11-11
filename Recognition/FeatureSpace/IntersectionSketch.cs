using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Runtime.Serialization;
using Sketch;

namespace FeatureSpace
{
    public delegate void IntersectionSketchCompletedEventHandler(object sender, EventArgs e);

    [Serializable]
    public class IntersectionSketch : ISerializable
    {
        #region Member Variables

        /// <summary>
        /// Sketch
        /// </summary>
        private Sketch.Sketch m_Sketch;

        /// <summary>
        /// All Strokes in the Intersection Sketch
        /// </summary>
        private List<Substroke> m_Strokes;

        /// <summary>
        /// List of Intersections between a given pair of strokes
        /// </summary>
        private List<IntersectionPair> m_AllIntersections;

        /// <summary>
        /// Lookup for a stroke
        /// </summary>
        private Dictionary<Substroke, List<IntersectionPair>> m_Stroke2Intersections;

        /// <summary>
        /// Bounding boxes for each stroke, 
        /// stored so that they don't need to be recalculated
        /// </summary>
        private Dictionary<Substroke, System.Drawing.RectangleF> m_Boxes;

        /// <summary>
        /// Lines connecting points in each stroke, 
        /// stored so that they don't need to be recalculated
        /// </summary>
        private Dictionary<Substroke, List<Line>> m_Lines;

        private Dictionary<Substroke, double> m_ExtensionLengthsActual_Copy;
        private Dictionary<Substroke, double> m_ExtensionLengthsExtreme_Copy;

        /// <summary>
        /// Performs intersection calculations in the background, so that 
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

        private Queue<Substroke> m_StrokesToUpdate;

        #endregion

        #region Events

        public event IntersectionSketchCompletedEventHandler CompletedIntersection;

        protected virtual void OnCompletedIntersection(EventArgs e)
        {
            if (CompletedIntersection != null)
                CompletedIntersection(this, e);
        }

        void m_BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                if (m_IsBWfinished)
                    OnCompletedIntersection(new EventArgs());
                else
                {
                    AddStroke(m_StrokesToAdd.Dequeue());
                    if (m_StrokesToAdd.Count == 0)
                        m_IsBWfinished = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("IntersectionSketch BWdone: " + ex.Message);
                //throw ex;
            }
        }

        #endregion

        #region Constructors

        public IntersectionSketch(Sketch.Sketch sketch, Dictionary<Substroke, double> extensionLengthsActual, Dictionary<Substroke, double> extensionLengthsExtreme)
        {
            m_BackgroundWorker = new BackgroundWorker();
            m_BackgroundWorker.WorkerReportsProgress = true;
            m_BackgroundWorker.WorkerSupportsCancellation = true;
            m_BackgroundWorker.DoWork += new DoWorkEventHandler(bw_DoWork);
            m_BackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_BackgroundWorker_RunWorkerCompleted);
            m_Sketch = sketch;
            m_StrokesToAdd = new Queue<Substroke>();
            m_StrokesToUpdate = new Queue<Substroke>();

            m_Strokes = new List<Substroke>();
            m_Boxes = new Dictionary<Substroke, System.Drawing.RectangleF>();
            m_Lines = new Dictionary<Substroke, List<Line>>();

            foreach (Substroke s in m_Sketch.Substrokes)
            {
                if (!m_Strokes.Contains(s))
                    m_Strokes.Add(s);

                if (!m_Boxes.ContainsKey(s))
                    m_Boxes.Add(s, Compute.BoundingBox(s.Points));

                if (!m_Lines.ContainsKey(s))
                    m_Lines.Add(s, Compute.getLines(s.PointsL));
            }

            m_ExtensionLengthsActual_Copy = extensionLengthsActual;
            m_ExtensionLengthsExtreme_Copy = extensionLengthsExtreme;

            m_AllIntersections = new List<IntersectionPair>();
            m_Stroke2Intersections = new Dictionary<Substroke, List<IntersectionPair>>();

            for (int i = 0; i < m_Sketch.Substrokes.Length; i++)
                FindIntersections(m_Sketch.SubstrokesL, i);
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

                    if (m_Stroke2Intersections.ContainsKey(stroke))
                        return;

                    if (m_Boxes.ContainsKey(stroke))
                        return;

                    if (m_Lines.ContainsKey(stroke))
                        return;

                    List<Line> lines = Compute.getLines(stroke.PointsL);

                    Line[] endLines = null;
                    if (m_ExtensionLengthsExtreme_Copy.ContainsKey(stroke))
                        endLines = getEndLines(lines, m_ExtensionLengthsExtreme_Copy[stroke]);
                    else
                    {
                        Console.WriteLine("Extension not available for stroke {0}", stroke.Id.ToString());
                        return;
                    }

                    lines.Insert(0, endLines[0]);
                    lines.Add(endLines[1]);
                    m_Lines.Add(stroke, lines);

                    m_Boxes.Add(stroke, Compute.BoundingBox(lines));

                    m_BackgroundWorker.RunWorkerAsync(stroke);
                }
                else
                {
                    m_IsBWfinished = false;
                    if (!m_StrokesToAdd.Contains(stroke))
                        m_StrokesToAdd.Enqueue(stroke);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("IntersectionSketch AddStroke: " + e.Message);
                //throw e;
            }
        }

        public void RemoveStroke(Substroke stroke)
        {
            if (m_Strokes.Contains(stroke))
                m_Strokes.Remove(stroke);

            if (m_Stroke2Intersections.ContainsKey(stroke))
            {
                foreach (IntersectionPair pair in m_Stroke2Intersections[stroke])
                    m_AllIntersections.Remove(pair);

                m_Stroke2Intersections.Remove(stroke);
            }

            if (m_Boxes.ContainsKey(stroke))
                m_Boxes.Remove(stroke);

            if (m_Lines.ContainsKey(stroke))
                m_Lines.Remove(stroke);
        }

        public void UpdateIntersections(Substroke stroke)
        {
            try
            {
                RemoveStroke(stroke);
                m_IsBWfinished = true;
                AddStroke(stroke);

            }
            catch (Exception e)
            {
                Console.WriteLine("IntersectionSketch UpdateIntersections: " + e.Message);
                //throw e;
            }
        }

        private Line[] getEndLines(List<Line> a, double d)
        {
            Line[] lines = new Line[2];
            int limit = 10;

            // New Method Stuff
            System.Drawing.PointF lineStart = new System.Drawing.PointF();
            System.Drawing.PointF lineEnd = new System.Drawing.PointF();
            if (a.Count > 0)
            {
                lineStart = a[0].EndPoint1;
                lineEnd = a[a.Count - 1].EndPoint2;
            }
            // End New Method Stuff

            if (a.Count > limit)
            {
                lines[0] = new Line(a[limit].EndPoint1, a[0].EndPoint1, true);
                lines[1] = new Line(a[a.Count - 1 - limit].EndPoint1, a[a.Count - 1].EndPoint2, true);
            }
            else if (a.Count > 0)
            {
                lines[0] = new Line(a[a.Count - 1].EndPoint2, a[0].EndPoint1, true);
                lines[1] = new Line(a[0].EndPoint1, a[a.Count - 1].EndPoint2, true);
                return lines;
            }
            else
            {
                lines[0] = new Line(new System.Drawing.PointF(0.0f, 0.0f), new System.Drawing.PointF(0.0f, 0.0f), true);
                lines[1] = new Line(new System.Drawing.PointF(0.0f, 0.0f), new System.Drawing.PointF(0.0f, 0.0f), true);
                return lines;
            }

            lines[0].extend(d);
            lines[1].extend(d);

            // New Method Stuff
            Line line1 = new Line(lines[0].EndPoint2, lineStart, true);
            Line line2 = new Line(lines[1].EndPoint2, lineEnd, true);

            return new Line[2] { line1, line2 };
            // End New Method Stuff

            //return lines;
        }

        private void FindIntersections(List<Substroke> strokes, int indexOfStroke)
        {
            if (indexOfStroke >= strokes.Count)
                return;

            Substroke stroke = strokes[indexOfStroke];
            lock (stroke)
            {
                int size = Math.Max(1, indexOfStroke - 1);
                List<IntersectionPair> pairs = new List<IntersectionPair>(size);
                for (int i = 0; i < indexOfStroke; i++)
                {
                    Substroke s = strokes[i];
                    lock (s)
                    {
                        try
                        {
                            IntersectionPair pair = new IntersectionPair(stroke, s, m_Boxes[stroke], m_Boxes[s], m_Lines[stroke], m_Lines[s]);
                            pairs.Add(pair);
                            lock (m_AllIntersections)
                            {
                                m_AllIntersections.Add(pair);
                            }
                            lock (m_Stroke2Intersections)
                            {
                                if (m_Stroke2Intersections.ContainsKey(s))
                                    m_Stroke2Intersections[s].Add(pair);
                            }
                        }
                        catch (Exception exc)
                        {
                            Console.WriteLine("IntersectionSketch FindIntersections: " + exc.Message);
                            //throw exc;
                        }
                    }
                }

                if (!m_Strokes.Contains(stroke))
                    m_Strokes.Add(stroke);

                if (!m_Stroke2Intersections.ContainsKey(stroke))
                    m_Stroke2Intersections.Add(stroke, pairs);
            }
        }

        #endregion

        #region Background / Threading stuff

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            Substroke stroke = (Substroke)e.Argument;
            lock (stroke)
            {
                List<IntersectionPair> pairs = new List<IntersectionPair>(m_Strokes.Count);
                foreach (Substroke s in m_Strokes)
                {
                    lock (s)
                    {
                        try
                        {
                            IntersectionPair pair = new IntersectionPair(stroke, s, m_Boxes[stroke], m_Boxes[s], m_Lines[stroke], m_Lines[s]);
                            pairs.Add(pair);
                            lock (m_AllIntersections)
                            {
                                m_AllIntersections.Add(pair);
                            }
                            lock (m_Stroke2Intersections)
                            {
                                if (m_Stroke2Intersections.ContainsKey(s))
                                    m_Stroke2Intersections[s].Add(pair);
                            }
                        }
                        catch (Exception exc)
                        {
                            Console.WriteLine("IntersectionSketch DoWork: " + exc.Message);
                            //throw exc;
                        }
                    }
                }

                if (!m_Strokes.Contains(stroke))
                    m_Strokes.Add(stroke);
                else
                {
                    Console.WriteLine("How'd we get here?");
                }

                if (!m_Stroke2Intersections.ContainsKey(stroke))
                    m_Stroke2Intersections.Add(stroke, pairs);
                else
                {
                    Console.WriteLine("How'd we get here?");
                }
            }

            if (m_Strokes.Count != m_Lines.Count || m_Stroke2Intersections.Count != m_Lines.Count)
                Console.WriteLine("AAAAAAAAAAAAAAAAA");
        }

        private void DoWorkFindIntersections(object strokeData)
        {
            Substroke stroke = (Substroke)strokeData;
            lock (stroke)
            {
                List<IntersectionPair> pairs = new List<IntersectionPair>(m_Strokes.Count);
                foreach (Substroke s in m_Strokes)
                {
                    lock (s)
                    {
                        IntersectionPair pair = new IntersectionPair(stroke, s, m_Boxes[stroke], m_Boxes[s], m_Lines[stroke], m_Lines[s]);
                        pairs.Add(pair);
                    }
                }

                lock (m_Strokes) { m_Strokes.Add(stroke); }
                lock (m_Stroke2Intersections) { m_Stroke2Intersections.Add(stroke, pairs); }
            }
        }

        #endregion

        #region Getters

        public Dictionary<string, int> GetIntersectionCounts(Substroke stroke)
        {
            if (!m_Stroke2Intersections.ContainsKey(stroke))
                return null;

            List<IntersectionPair> intersections = m_Stroke2Intersections[stroke];
            Dictionary<string, int> counts = new Dictionary<string, int>(4);
            counts.Add("Number of 'LL' Intersections", 0);
            counts.Add("Number of 'XX' Intersections", 0);
            counts.Add("Number of 'LX' Intersections", 0);
            counts.Add("Number of 'XL' Intersections", 0);

            foreach (IntersectionPair pair in intersections)
            {
                if (!pair.IsEmpty)
                {
                    foreach (Intersection intersection in pair.Intersections)
                    {
                        float a = intersection.GetIntersectionPoint(stroke);
                        float b = intersection.GetOtherStrokesIntersectionPoint(stroke);
                        if (a != -1.0f && b != -1.0f)
                        {
                            bool aL = false;
                            bool bL = false;
                            float aThresh = 0f;
                            float bThresh = 0f;

                            if (m_ExtensionLengthsActual_Copy.ContainsKey(stroke) && m_ExtensionLengthsExtreme_Copy.ContainsKey(stroke))
                                aThresh = (float)(m_ExtensionLengthsActual_Copy[stroke] / m_ExtensionLengthsExtreme_Copy[stroke]);
                            else
                                Console.WriteLine("Stroke not found in extension dictionary.");

                            Substroke otherStroke = intersection.SubStrokeA;
                            if (stroke == otherStroke)
                                otherStroke = intersection.SubStrokeB;

                            if (m_ExtensionLengthsActual_Copy.ContainsKey(otherStroke) && m_ExtensionLengthsExtreme_Copy.ContainsKey(otherStroke))
                                bThresh = (float)(m_ExtensionLengthsActual_Copy[otherStroke] / m_ExtensionLengthsExtreme_Copy[otherStroke]);
                            else
                                Console.WriteLine("Stroke not found in extension dictionary.");

                            if ((a <= Compute.THRESHOLD && a >= -aThresh) || (a >= (1.0 - Compute.THRESHOLD) && (a <= 1.0 + aThresh)))
                                aL = true;

                            if ((b <= Compute.THRESHOLD && b >= -bThresh) || (b >= (1.0 - Compute.THRESHOLD) && (b <= 1.0 + bThresh)))
                                bL = true;

                            if (aL && bL)
                                counts["Number of 'LL' Intersections"]++;
                            else if (aL)
                                counts["Number of 'LX' Intersections"]++;
                            else if (bL)
                                counts["Number of 'XL' Intersections"]++;
                            else
                                counts["Number of 'XX' Intersections"]++;
                        }
                    }
                }
            }

            return counts;
        }

        public IntersectionPair GetIntersectionPair(Substroke a, Substroke b)
        {
            if (m_Stroke2Intersections.ContainsKey(a))
            {
                foreach (IntersectionPair pair in m_Stroke2Intersections[a])
                {
                    if (pair.Contains(b.Id))
                        return pair;
                }

                return null;
            }
            else if (m_Stroke2Intersections.ContainsKey(b))
            {
                foreach (IntersectionPair pair in m_Stroke2Intersections[b])
                {
                    if (pair.Contains(a.Id))
                        return pair;
                }

                return null;
            }
            else
                return null;
        }

        public List<Substroke> Strokes
        {
            get { return m_Strokes; }
        }

        public Dictionary<Substroke, List<Line>> Lines
        {
            get { return m_Lines; }
        }

        #endregion

        #region Serialization

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public IntersectionSketch(SerializationInfo info, StreamingContext context)
        {
            m_AllIntersections = (List<IntersectionPair>)info.GetValue("AllIntersections", typeof(List<IntersectionPair>));
            m_Boxes = (Dictionary<Substroke, System.Drawing.RectangleF>)info.GetValue("Boxes", typeof(Dictionary<Substroke, System.Drawing.RectangleF>));
            m_Lines = (Dictionary<Substroke, List<Line>>)info.GetValue("Lines", typeof(Dictionary<Substroke, List<Line>>));
            m_Sketch = (Sketch.Sketch)info.GetValue("Sketch", typeof(Sketch.Sketch));
            m_Stroke2Intersections = (Dictionary<Substroke, List<IntersectionPair>>)info.GetValue("Stroke2Intersections", typeof(Dictionary<Substroke, List<IntersectionPair>>));
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
            info.AddValue("AllIntersections", m_AllIntersections);
            info.AddValue("Boxes", m_Boxes);
            info.AddValue("Lines", m_Lines);
            info.AddValue("Sketch", m_Sketch);
            info.AddValue("Stroke2Intersections", m_Stroke2Intersections);
            info.AddValue("Strokes", m_Strokes);
            info.AddValue("StrokesToAdd", m_StrokesToAdd);
        }

        #endregion
    }
}
