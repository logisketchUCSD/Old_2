using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.ComponentModel;
using StrokeClassifier;
using StrokeGrouper;
using Clusters;
using ImageRecognizer;
using ImageRecognizerWrapper;
using Featurefy;
using Sketch;
using Utilities;
using Microsoft.Ink;
using ImageAligner;

namespace Clusterer
{
    public delegate void InitialClustersCompleted(object sender, EventArgs e);
    public delegate void FinalClustersCompleted(object sender, EventArgs e);
    public delegate void StrokeClassificationCompleted(object sender, EventArgs e);
    public delegate void FeaturizationCompleted(object sender, EventArgs e);

    public class Clusterer
    {
        #region Member Variables

        FeatureSketch m_FeatureSketch;

        StrokeClassifier.StrokeClassifier m_Classifier;

        StrokeGrouper.StrokeGrouper m_Grouper;

        ImageAlignerRecognizer m_ImageAligner;

        ImageRecognizerWrapper.ImageRecognizer m_ImageRecognizer;

        ClustererSettings m_Settings;

        ClustererResults m_Results;
        
        #endregion

        #region Constructors

        public Clusterer(ClustererSettings settings)
        {
            m_Settings = settings;

            m_FeatureSketch = new FeatureSketch(m_Settings.ClassifierSettings.FeaturesOn, m_Settings.GrouperSettings.FeaturesOn, m_Settings.AvgsAndStdDevs);
            m_FeatureSketch.CompletedFeatureSketch += new FeatureSketchCompletedEventHandler(m_FeatureSketch_CompletedFeatureSketch);

            m_Settings.ClassifierSettings.CurrentUser = m_Settings.CurrentUser;
            m_Classifier = new StrokeClassifier.StrokeClassifier(m_FeatureSketch, m_Settings.ClassifierSettings);
            m_Classifier.StrokeClassificationCompleted += new StrokeClassificationCompletedEventHandler(m_Classifier_StrokeClassificationCompleted);

            m_Settings.GrouperSettings.CurrentUser = m_Settings.CurrentUser;
            m_Grouper = new StrokeGrouper.StrokeGrouper(m_FeatureSketch, m_Settings.GrouperSettings);
            m_Grouper.StrokeGrouperCompleted += new StrokeGrouperCompletedEventHandler(m_Grouper_StrokeGrouperCompleted);

            m_ImageAligner = ImageAlignerRecognizer.Load(settings.ImageAlignerFilename);
        }

        public Clusterer(ClustererSettings settings, FeatureSketch featureSketch)
        {
            m_Settings = settings;

            m_FeatureSketch = featureSketch;
            m_FeatureSketch.CompletedFeatureSketch += new FeatureSketchCompletedEventHandler(m_FeatureSketch_CompletedFeatureSketch);

            m_Settings.ClassifierSettings.CurrentUser = m_Settings.CurrentUser;
            m_Classifier = new StrokeClassifier.StrokeClassifier(m_FeatureSketch, m_Settings.ClassifierSettings);
            m_Classifier.StrokeClassificationCompleted += new StrokeClassificationCompletedEventHandler(m_Classifier_StrokeClassificationCompleted);

            m_Settings.GrouperSettings.CurrentUser = m_Settings.CurrentUser;
            m_Grouper = new StrokeGrouper.StrokeGrouper(m_FeatureSketch, m_Settings.GrouperSettings);
            m_Grouper.StrokeGrouperCompleted += new StrokeGrouperCompletedEventHandler(m_Grouper_StrokeGrouperCompleted);

            m_ImageAligner = ImageAlignerRecognizer.Load(settings.ImageAlignerFilename);

            m_FeatureSketch_CompletedFeatureSketch(new object(), new EventArgs());
        }     

        #endregion

        #region Events

        public event FeaturizationCompleted FeaturizingDone;

        protected virtual void OnFeaturizationCompleted(EventArgs e)
        {
            if (FeaturizingDone != null)
                FeaturizingDone(this, e);
        }

        void m_FeatureSketch_CompletedFeatureSketch(object sender, EventArgs e)
        {
            OnFeaturizationCompleted(e);

            //m_Classifier.AssignClassificationsToFeatureSketch();
        }

        public event StrokeClassificationCompleted StrokeClassificationDone;

        protected virtual void OnStrokeClassificationCompleted(EventArgs e)
        {
            if (StrokeClassificationDone != null)
                StrokeClassificationDone(this, e);
        }

        void m_Classifier_StrokeClassificationCompleted(object sender, EventArgs e)
        {
            try
            {
                StrokeClassifierResult resultClassifier = m_Classifier.Result;
                ApplyClassifications(resultClassifier);

                OnStrokeClassificationCompleted(e);

                m_Grouper.UpdateStrokeClassifications(resultClassifier.AllClassifications);
                m_Grouper.Group();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Clusterer ClassificationDone: " + ex.Message);
                //throw ex;
            }
        }

        void m_Grouper_StrokeGrouperCompleted(object sender, EventArgs e)
        {
            try
            {
                StrokeGrouperResult results = m_Grouper.Results;

                GroupSketch(results);

                OnInitialClustersCompleted(e);

                //RecognizeAndLabelSketch();
           }
            catch (Exception ex)
            {
                Console.WriteLine("Clusterer GrouperDone: " + ex.Message);
                //throw ex;
            }
        }

        public event InitialClustersCompleted InitialClustersDone;

        protected virtual void OnInitialClustersCompleted(EventArgs e)
        {
            if (InitialClustersDone != null)
                InitialClustersDone(this, e);
        }

        public event FinalClustersCompleted FinalClustersDone;

        protected virtual void OnFinalClustersCompleted(EventArgs e)
        {
            if (FinalClustersDone != null)
                FinalClustersDone(this, e);
        }
        #endregion

        #region Add/Remove Methods

        public void AddStroke(Sketch.Substroke substroke)
        {
            try
            {
                m_Classifier.Result = null;
                m_Grouper.Results = null;
                m_FeatureSketch.AddStroke(substroke);
            }
            catch (Exception e)
            {
                Console.WriteLine("Clusterer AddStroke: " + e.Message);
            }
        }

        public void AddStroke(Sketch.Stroke stroke)
        {
            try
            {
                m_Classifier.Result = null;
                m_Grouper.Results = null;
                m_FeatureSketch.AddStroke(stroke);
            }
            catch (Exception e)
            {
                Console.WriteLine("Clusterer AddStroke: " + e.Message);
            }
        }

        public void RemoveStroke(Substroke sub)
        {
            try
            {
                m_Classifier.Result = null;
                m_Grouper.Results = null;
                m_FeatureSketch.RemoveStroke(sub);
            }
            catch (Exception e)
            {
                Console.WriteLine("Clusterer RemoveStroke: " + e.Message);
            }
        }

        #endregion

        #region Getters

        public List<Substroke> Strokes
        {
            get { return m_FeatureSketch.Sketch.SubstrokesL; }
        }

        public FeatureSketch FeatureSketch
        {
            get
            {
                return m_FeatureSketch;
            }
        }

        public StrokeClassifier.StrokeClassifier Classifier
        {
            get { return m_Classifier; }
        }

        public StrokeGrouper.StrokeGrouper Grouper
        {
            get { return m_Grouper; }
        }

        public ClustererSettings Settings
        {
            get { return m_Settings; }
        }
        
        public User CurrentUser
        {
            get { return m_Settings.CurrentUser; }
            set { m_Settings.CurrentUser = value; }
        }

        public PlatformUsed CurrentPlatform
        {
            get { return m_Settings.CurrentPlatform; }
            set { m_Settings.CurrentPlatform = value; }
        }

        public ImageAlignerRecognizer Recognizer
        {
            get { return m_ImageAligner; }
            set { m_ImageAligner = value; }
        }

        #endregion

        private void ApplyClassifications(StrokeClassifierResult result)
        {
            Sketch.Sketch sketch = m_FeatureSketch.Sketch;
            if (result == null)
                result = m_Classifier.Result;

            if (result == null)
                throw new Exception("Stroke Classifications empty");

            foreach (Substroke s in sketch.Substrokes)
            {
                s.Classification = result.GetClassification(s);
                if (s.ParentShapes == null || s.ParentShapes.Count == 0)
                {
                    List<Sketch.Substroke> strokes = new List<Sketch.Substroke>();
                    strokes.Add(s);
                    Shape shape = new Sketch.Shape(strokes, new Sketch.XmlStructs.XmlShapeAttrs(true));
                    shape.Type = s.Classification;
                    sketch.AddShape(shape);
                }
            }
        }

        public void GroupSketch(StrokeGrouperResult result)
        {
            Sketch.Sketch sketch = m_FeatureSketch.Sketch;
            if (result == null)
                result = m_Grouper.GroupWithWeka(m_Classifier.Result.AllClassifications);

            if (result == null)
                throw new Exception("Grouping results empty");

            sketch.RemoveGroups();

            foreach (Substroke s in sketch.SubstrokesL)
                if (s.ParentShapes.Count > 0 && s.ParentShapes[0].Type == "UNKNOWN")
                    s.ParentShapes[0].Type = s.Classification;
            

            foreach (Utilities.StrokePair pair in result.JoinedPairs)
            {
                string label = result.GetLabelForJoinedPair(pair);
                sketch.GetSubstroke(pair.Stroke1.Id).Classification = label;
                sketch.GetSubstroke(pair.Stroke2.Id).Classification = label;
                Sketch.Shape shape1 = sketch.GetSubstroke(pair.Stroke1.Id).ParentShapes[0];
                Sketch.Shape shape2 = sketch.GetSubstroke(pair.Stroke2.Id).ParentShapes[0];
                if (shape1 != shape2 && shape1.SubstrokesL.Count != 0 && shape2.SubstrokesL.Count != 0)
                {
                    shape1.Type = label;
                    shape2.Type = label;
                    shape1 = sketch.mergeShapes(shape1, shape2);
                    shape1.Probability = 0f;
                }
            }
        }

        public void RecognizeAndLabelSketch()
        {
            Sketch.Sketch sketch = m_FeatureSketch.Sketch;

            // Recognize each of the gate shapes
            Dictionary<Shape, ImageTemplateResult> results = new Dictionary<Shape, ImageTemplateResult>();
            List<Shape> badShapes = new List<Shape>();
            foreach (Shape shape in sketch.Shapes)
            {
                if (shape.Substrokes.Length == 0)
                    sketch.RemoveShape(shape);

                if (shape.Type == "Gate" || General.IsGate(shape))
                {
                    ImageTemplateResult result = m_ImageAligner.Recognize(shape);
                    if (result != null)
                    {
                        // Go through result and assign labels and probabilities
                        // Remove extra strokes
                        if (result.Errors.Count == 0)
                        {
                            shape.Type = result.Name;
                            shape.Probability = (float)result.Score;
                        }
                        else
                        {
                            List<ImageMatchError> resolved = new List<ImageMatchError>();
                            foreach (ImageMatchError error in result.Errors)
                            {
                                if (error.Type == ErrorType.Extra)
                                {
                                    shape.RemoveSubstroke(error.OffendingStroke);
                                    resolved.Add(error);
                                }
                            }

                            foreach (ImageMatchError error in resolved)
                                result.RemoveError(error);

                            if (result.Errors.Count > 0)
                                badShapes.Add(shape);

                            shape.Type = result.Name;
                            shape.Probability = (float)result.Score;
                        }

                        results.Add(shape, result);
                    }
                }
            }

        }

        #region Uncommented Code That Might Go Away Again
        public ImageRecognizerResults GetImageResults(List<Cluster> clusters)
        {
            return m_ImageRecognizer.RecognizeST(clusters);
        }

        public ImageRecognizerWrapper.ImageRecognizer ImageRecognizer
        {
            get { return m_ImageRecognizer; }
        }

        #endregion

        #region Unused Code
        /*
        Dictionary<int, Cluster> m_Hash2Cluster;

        List<Cluster> m_ParentClusters;

        Dictionary<Cluster, Dictionary<int, ImageScore>> m_Parent2SortedTopMatches;
        */
        /*
        
        void m_ImageRecognizer_RecognitionCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                
                List<Cluster> scored = (List<Cluster>)e.Result;

                foreach (Cluster c in scored)
                {
                    int hash = c.HashForImageReco;

                    Cluster check;
                    // Should come back true
                    bool found = m_Hash2Cluster.TryGetValue(hash, out check);
                    if (found)
                    {
                        if (c.HasBeenScored)
                            m_Hash2Cluster[hash] = c;
                        else if (check.HasBeenScored)
                        {
                            c.Score = check.Score;
                            m_Hash2Cluster[hash] = c;
                        }
                    }
                    else
                    {
                        m_Hash2Cluster.Add(hash, c);
                    }
                }


                Console.WriteLine("Finished Recog.  at {0}:{1}:{2}.{3}",
                    DateTime.Now.Hour.ToString(), DateTime.Now.Minute.ToString(),
                    DateTime.Now.Second.ToString(), DateTime.Now.Millisecond.ToString());


                // determine whether the parent or one of the children is best
                m_Parent2SortedTopMatches = new Dictionary<Cluster, Dictionary<int, ImageScore>>();
                List<Cluster> scoredParents = new List<Cluster>();

                foreach (Cluster c in m_ParentClusters)
                {
                    if (c != null && c.HasBeenScored)
                    {
                        Dictionary<ImageScore, int> topMatches = new Dictionary<ImageScore, int>(c.Children.Count + 1);
                        ImageScore score = c.Score.TopMatch;
                        if (score != null)
                            topMatches.Add(score, c.HashForImageReco);
                        foreach (Cluster child in c.Children)
                        {
                            if (child != null && child.HasBeenScored)
                            {
                                ImageScore childScore = child.Score.TopMatch;
                                if (childScore != null)
                                    topMatches.Add(child.Score.TopMatch, child.HashForImageReco);
                            }
                        }

                        Dictionary<int, ImageScore> sortedTopMatches = ImageScore.SortTopMatches(topMatches);
                        m_Parent2SortedTopMatches.Add(c, sortedTopMatches);
                    }
                }

                OnFinalClustersCompleted(new EventArgs());
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("ImageRecognizerCompleted: {0}", ex.Message);
            }
        }

        */

        #region Searching
        /*
        
        public List<Cluster> CreateClusters(StrokeGrouperResult results)
        {
            CreateClusters(results, m_FeatureSketch.PairwiseFeatureSketch.AllDistances);
            return m_ParentClusters;
        } 
        
        public void CreateClusters(StrokeGrouperResult results, Dictionary<Substroke, List<SubstrokeDistance>> distances)
        {
            try
            {
                // Lists for merging clusters
                List<Cluster> clusters = new List<Cluster>();
                List<Cluster> newClusters = new List<Cluster>();

                // Classification data
                List<string> allClasses = m_Settings.GrouperSettings.Domain.Classes;
                Dictionary<Substroke, string> strokeClassifications = m_Classifier.Result.AllClassifications;

                // Create 2 stroke clusters
                foreach (string name in allClasses)
                {
                    List<StrokePair> pairsInClass = results.Pairs(name);
                    foreach (StrokePair pair in pairsInClass)
                        if (results.IsJoined(pair))
                            clusters.Add(new Cluster(pair.Stroke1, pair.Stroke2, name, distances));
                }

                // Create single stroke clusters
                foreach (Substroke s in Strokes)
                {
                    bool found = false;
                    foreach (Cluster c in clusters)
                        if (c.Contains(s))
                        {
                            found = true;
                            break;
                        }

                    if (!found && strokeClassifications.ContainsKey(s))
                        clusters.Add(new Cluster(s, strokeClassifications[s], distances));
                }

                // Flag to know when merging has finished, initialized to true in order to enter 'while' loop
                bool mergeOccured = true;

                // Merge Clusters
                while (mergeOccured)
                {
                    // Change flag which ends the while loop if no merges occur before the end of this loop
                    mergeOccured = false;

                    // List of already merged clusters so that we don't add a merged cluster, then
                    // add a subset of that merged cluster
                    List<Cluster> alreadyMerged = new List<Cluster>();

                    // Outer loop through clusters
                    for (int i = 0; i < clusters.Count; i++)
                    {
                        // Ignore clusters that have already been added
                        if (!alreadyMerged.Contains(clusters[i]))
                        {
                            for (int j = i + 1; j < clusters.Count; j++)
                            {
                                foreach (Substroke s in clusters[j].Strokes)
                                {
                                    if (clusters[i].Contains(s))
                                    {
                                        Cluster c = Cluster.MergeClusters(clusters[i], clusters[j]);
                                        newClusters.Add(c);
                                        if (!alreadyMerged.Contains(clusters[i]))
                                            alreadyMerged.Add(clusters[i]);
                                        if (!alreadyMerged.Contains(clusters[j]))
                                            alreadyMerged.Add(clusters[j]);
                                        mergeOccured = true;
                                        break;
                                    }
                                }
                            }

                            if (!alreadyMerged.Contains(clusters[i]))
                                newClusters.Add(clusters[i]);
                        }
                    }

                    clusters = newClusters;
                    newClusters = new List<Cluster>();
                }

                m_ParentClusters = clusters;
            }
            catch (Exception e)
            {
                Console.WriteLine("Clusterer CreateClusters: " + e.Message);
            }
        }

        public List<Cluster> CreateClustersST(StrokeGrouperResult results, Dictionary<Substroke, List<SubstrokeDistance>> distances)
        {
            try
            {
                // Lists for merging clusters
                List<Cluster> clusters = new List<Cluster>();
                List<Cluster> newClusters = new List<Cluster>();

                // Classification data
                List<string> allClasses = m_Settings.GrouperSettings.Domain.Classes;
                Dictionary<Substroke, string> strokeClassifications = m_Classifier.Result.AllClassifications;

                // Create 2 stroke clusters
                foreach (string name in allClasses)
                {
                    List<StrokePair> pairsInClass = results.Pairs(name);
                    foreach (StrokePair pair in pairsInClass)
                        if (results.IsJoined(pair))
                            clusters.Add(new Cluster(pair.Stroke1, pair.Stroke2, name, distances));
                }

                // Create single stroke clusters
                foreach (Substroke s in Strokes)
                {
                    bool found = false;
                    foreach (Cluster c in clusters)
                        if (c.Contains(s))
                        {
                            found = true;
                            break;
                        }

                    if (!found && strokeClassifications.ContainsKey(s))
                        clusters.Add(new Cluster(s, strokeClassifications[s], distances));
                }

                // Flag to know when merging has finished, initialized to true in order to enter 'while' loop
                bool mergeOccured = true;

                // Merge Clusters
                while (mergeOccured)
                {
                    // Change flag which ends the while loop if no merges occur before the end of this loop
                    mergeOccured = false;

                    // List of already merged clusters so that we don't add a merged cluster, then
                    // add a subset of that merged cluster
                    List<Cluster> alreadyMerged = new List<Cluster>();

                    // Outer loop through clusters
                    for (int i = 0; i < clusters.Count; i++)
                    {
                        // Ignore clusters that have already been added
                        if (!alreadyMerged.Contains(clusters[i]))
                        {
                            for (int j = i + 1; j < clusters.Count; j++)
                            {
                                foreach (Substroke s in clusters[j].Strokes)
                                {
                                    if (clusters[i].Contains(s))
                                    {
                                        Cluster c = Cluster.MergeClusters(clusters[i], clusters[j]);
                                        newClusters.Add(c);
                                        if (!alreadyMerged.Contains(clusters[i]))
                                            alreadyMerged.Add(clusters[i]);
                                        if (!alreadyMerged.Contains(clusters[j]))
                                            alreadyMerged.Add(clusters[j]);
                                        mergeOccured = true;
                                        break;
                                    }
                                }
                            }

                            if (!alreadyMerged.Contains(clusters[i]))
                                newClusters.Add(clusters[i]);
                        }
                    }

                    clusters = newClusters;
                    newClusters = new List<Cluster>();
                }

                m_ParentClusters = clusters;
                return clusters;
            }
            catch (Exception e)
            {
                Console.WriteLine("Clusterer CreateClusters: " + e.Message);
            }

            return null;
        }

        private void SearchClusters()
        {
            Dictionary<int, Cluster> toBeRecognized = new Dictionary<int, Cluster>();
            List<Cluster> allCurrentClusters = new List<Cluster>(m_ParentClusters.Count * 50);

            lock (m_ParentClusters)
            {
                Console.WriteLine("Starting Search at {0}:{1}:{2}.{3}", 
                    DateTime.Now.Hour.ToString(), DateTime.Now.Minute.ToString(), 
                    DateTime.Now.Second.ToString(), DateTime.Now.Millisecond.ToString());
                foreach (Cluster c in m_ParentClusters)
                    c.FillModificationsListNClosest(m_Settings.SearchNeighborhoodCount);

                // Get all children / derivatives from the original clusters
                foreach (Cluster c in m_ParentClusters)
                {
                    if (c.ClassName != "Wire")// && c.ClassName != "Label")
                    {
                        int parentHash = c.HashForImageReco;
                        

                        Cluster checkParent;
                        bool foundParent = m_Hash2Cluster.TryGetValue(parentHash, out checkParent);
                        if (!foundParent)
                        {
                            if (!allCurrentClusters.Contains(c))
                                allCurrentClusters.Add(c);

                            m_Hash2Cluster.Add(parentHash, c);

                            if (!toBeRecognized.ContainsKey(parentHash))
                                toBeRecognized.Add(parentHash, c);
                        }
                        else
                        {
                            if (checkParent.HasBeenScored)
                            {
                                c.Score = checkParent.Score;
                                checkParent = c;
                                //m_Hash2Cluster[parentHash] = c;
                            }
                            else
                            {
                                m_Hash2Cluster[parentHash] = c;
                                if (!toBeRecognized.ContainsKey(parentHash))
                                    toBeRecognized.Add(parentHash, c);
                            }
                        }

                        Dictionary<int, Cluster> children = c.GetChildren(c, 0, 0);
                        List<Cluster> childClusters = new List<Cluster>(children.Values);
                        c.Children = childClusters;

                        foreach (KeyValuePair<int, Cluster> entry in children)
                        {
                            int hash = entry.Key;
                            Cluster child = entry.Value;

                            Cluster cluster;
                            bool foundCluster = m_Hash2Cluster.TryGetValue(hash, out cluster);
                            if (foundCluster) // Have seen this arrangement before :)
                            {
                                if (cluster.HasBeenScored) // And this arrangement has been recognized :)
                                {
                                    child.Score = cluster.Score;
                                    cluster = child;
                                    //m_Hash2Cluster[hash] = child;
                                }
                                else // Need to recognize this
                                {
                                    m_Hash2Cluster[hash] = child;
                                    if (!toBeRecognized.ContainsKey(hash))
                                        toBeRecognized.Add(hash, child);
                                }
                            }
                            else // Haven't seen this arrangement of strokes before (with this className)
                            {
                                if (!allCurrentClusters.Contains(child))
                                    allCurrentClusters.Add(child);

                                // Now we've seen it
                                m_Hash2Cluster.Add(hash, child);
                                
                                // And hopefully soon we'll have recognized it
                                if (!toBeRecognized.ContainsKey(hash))
                                    toBeRecognized.Add(hash, child);
                            }
                        }
                    }
                }
            }

            foreach (KeyValuePair<int, Cluster> entry in m_Hash2Cluster)
            {
                if (!entry.Value.HasBeenScored && !toBeRecognized.ContainsKey(entry.Key))
                    toBeRecognized.Add(entry.Key, entry.Value);
            }

            int recognizedClusters = 0;
            int unrecognizedClusters = 0;
            int parentClusters = 0;
            int childClusterCount = 0;
            foreach (Cluster c in allCurrentClusters)
            {
                if (c.IsParent)
                    parentClusters++;
                else
                    childClusterCount++;

                if (c.HasBeenScored)
                    recognizedClusters++;
                else
                    unrecognizedClusters++;
            }
            Console.WriteLine("Starting Recog. at {0}:{1}:{2}.{3}",
                DateTime.Now.Hour.ToString(), DateTime.Now.Minute.ToString(),
                DateTime.Now.Second.ToString(), DateTime.Now.Millisecond.ToString());

            Console.WriteLine("Before Recognition: {0} Parents ({1} m_PC's) + {2} Children = {3} Total; {4} Recognized + {5} Unrecognized ({6} toBeRecognized)", 
                parentClusters, m_ParentClusters.Count, childClusterCount, allCurrentClusters.Count, recognizedClusters, unrecognizedClusters, toBeRecognized.Count);



            // Evaluate any new clusters
            try
            {
                //m_ImageRecognizer.Recognize(new List<Cluster>(toBeRecognized.Values));
                ImageRecognizerResults results = m_ImageRecognizer.RecognizeST(new List<Cluster>(toBeRecognized.Values));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Search Clusters: {0}", ex.Message);
            }
        }
        
        public List<Cluster> GetSomeClusters(System.Drawing.Point pt)
        {
            foreach (KeyValuePair<Cluster, Dictionary<int, ImageScore>> entry in m_Parent2SortedTopMatches)
            {
                Cluster c = entry.Key;
                if (Utilities.Compute.PtInsideRectangle(pt, c.BoundingBox))
                {
                    List<Cluster> values = new List<Cluster>();
                    foreach (KeyValuePair<int, ImageScore> result in entry.Value)
                    {
                        if (m_Hash2Cluster.ContainsKey(result.Key))
                            values.Add(m_Hash2Cluster[result.Key]);
                    }


                    return values;
                }
            }

            return new List<Cluster>();
        }
        */
        #endregion

        /*
        public Dictionary<Substroke, System.Drawing.Color> ClusterColors
        {
            get
            {
                Dictionary<Substroke, System.Drawing.Color> colors = new Dictionary<Substroke, System.Drawing.Color>();
                for (int i = 0; i < m_ParentClusters.Count; i++)
                {
                    foreach (Substroke s in m_ParentClusters[i].Strokes)
                    {
                        if (!colors.ContainsKey(s))
                            colors.Add(s, GetColor(i));
                    }
                }

                return colors;
            }
        }

        public Dictionary<Substroke, System.Drawing.Color> TopCompleteMatchColors
        {
            get
            {
                Dictionary<Substroke, System.Drawing.Color> colors = new Dictionary<Substroke, System.Drawing.Color>();
                int n = 0;
                foreach (KeyValuePair<Cluster, Dictionary<int, ImageScore>> entry in m_Parent2SortedTopMatches)
                {
                    List<int> hashes = new List<int>(entry.Value.Keys);
                    foreach (int hash in hashes)
                    {
                        if (m_Hash2Cluster.ContainsKey(hash) && m_Hash2Cluster[hash].Score.TopMatch.Completeness == SymbolCompleteness.Complete)
                        {
                            Cluster c = m_Hash2Cluster[hash];
                            foreach (Substroke s in c.Strokes)
                            {
                                if (!colors.ContainsKey(s))
                                    colors.Add(s, GetColor(n));
                            }
                            n++;
                            break;
                        }
                    }
                }

                return colors;
            }
        }

        public Dictionary<Substroke, System.Drawing.Color> TopMatchColors
        {
            get
            {
                Dictionary<Substroke, System.Drawing.Color> colors = new Dictionary<Substroke, System.Drawing.Color>();
                int n = 0;
                foreach (KeyValuePair<Cluster, Dictionary<int, ImageScore>> entry in m_Parent2SortedTopMatches)
                {
                    List<int> hashes = new List<int>(entry.Value.Keys);
                    if (hashes.Count > 0 && m_Hash2Cluster.ContainsKey(hashes[0]))
                    {
                        Cluster c = m_Hash2Cluster[hashes[0]];
                        foreach (Substroke s in c.Strokes)
                        {
                            if (!colors.ContainsKey(s))
                                colors.Add(s, GetColor(n));
                        }
                        n++;
                    }
                }

                return colors;
            }
        }
        
        public System.Drawing.Color GetColor(int index)
        {
            while (index > 20)
                index -= 20;
            switch (index)
            {
                case (0):
                    return System.Drawing.Color.SpringGreen;
                case (1):
                    return System.Drawing.Color.Red;
                case (2):
                    return System.Drawing.Color.Green;
                case (3):
                    return System.Drawing.Color.RosyBrown;
                case (4):
                    return System.Drawing.Color.Salmon;
                case (5):
                    return System.Drawing.Color.Brown;
                case (6):
                    return System.Drawing.Color.Magenta;
                case (7):
                    return System.Drawing.Color.Maroon;
                case (8):
                    return System.Drawing.Color.Olive;
                case (9):
                    return System.Drawing.Color.Orange;
                case (10):
                    return System.Drawing.Color.Plum;
                case (11):
                    return System.Drawing.Color.SeaGreen;
                case (12):
                    return System.Drawing.Color.YellowGreen;
                case (13):
                    return System.Drawing.Color.AliceBlue;
                case (14):
                    return System.Drawing.Color.Coral;
                case (15):
                    return System.Drawing.Color.Cyan;
                case (16):
                    return System.Drawing.Color.DarkOrange;
                case (17):
                    return System.Drawing.Color.DarkGreen;
                case (18):
                    return System.Drawing.Color.DarkCyan;
                case (19):
                    return System.Drawing.Color.DeepPink;
                case (20):
                    return System.Drawing.Color.Fuchsia;
                case (21):
                    return System.Drawing.Color.Lavender;
                default:
                    return System.Drawing.Color.Lime;
            }
        }
        */

        #endregion
    }
}