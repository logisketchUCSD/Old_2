using System;
using System.Collections.Generic;
using System.Text;
using Sketch;

namespace Cluster
{
    public class ClusteringResultsSketch
    {
        private Sketch.Sketch sketch;

        private List<Shape> shapes;
        private List<Cluster> clusters;

        private List<ClusteringResultsShape> resultsShape;
        private List<ClusteringResultsCluster> resultsCluster;

        private Dictionary<Shape, Cluster> shape2clusterBinding;
        private Dictionary<Cluster, Shape> cluster2shapeBinding;

        private Dictionary<Guid, string> strokeClassifications;

        private List<Cluster> PartiallyMatchingButNotBestClusters;
        private List<Cluster> CompletelyUnMatchingClusters;

        public ClusteringResultsSketch(Sketch.Sketch sketch, List<Shape> shapes, List<Cluster> clusters, Dictionary<Guid, string> strokeClassifications)
        {
            this.sketch = sketch;
            this.shapes = shapes;
            this.clusters = clusters;
            this.strokeClassifications = strokeClassifications;

            this.resultsCluster = new List<ClusteringResultsCluster>();
            this.resultsShape = new List<ClusteringResultsShape>();
            this.shape2clusterBinding = new Dictionary<Shape,Cluster>();
            this.cluster2shapeBinding = new Dictionary<Cluster,Shape>();
            this.PartiallyMatchingButNotBestClusters = new List<Cluster>();
            this.CompletelyUnMatchingClusters = new List<Cluster>();

            foreach (Shape s in shapes)
                resultsShape.Add(new ClusteringResultsShape(s, clusters, strokeClassifications));

            foreach (Cluster c in clusters)
                resultsCluster.Add(new ClusteringResultsCluster(c, shapes, strokeClassifications));

            cluster2shapeBinding = FindClusterBinding(resultsShape);

            List<Cluster> clustersRemaining = GetRemainingClusters(clusters, cluster2shapeBinding);
            List<ClusteringResultsShape> badShapes = new List<ClusteringResultsShape>();
            bool errors = CheckForErrorsInBinding(cluster2shapeBinding, resultsShape, ref badShapes);
            while (errors)
            {
                foreach (ClusteringResultsShape bad in badShapes)
                {
                    resultsShape.Remove(bad);
                    resultsShape.Add(new ClusteringResultsShape(bad.Shape, clustersRemaining, strokeClassifications));
                }
                cluster2shapeBinding = FindClusterBinding(resultsShape);

                errors = CheckForErrorsInBinding(cluster2shapeBinding, resultsShape, ref badShapes);

                clustersRemaining = GetRemainingClusters(clusters, cluster2shapeBinding);
            }

            clustersRemaining = GetRemainingClusters(clusters, cluster2shapeBinding);

            foreach (Cluster c in clustersRemaining)
            {
                if (!cluster2shapeBinding.ContainsKey(c))
                {
                    double inkMatching;
                    Shape s = FindBestShape(c, shapes, out inkMatching);
                    cluster2shapeBinding.Add(c, s);
                    if (inkMatching > 0.0)
                        PartiallyMatchingButNotBestClusters.Add(c);
                    else
                        CompletelyUnMatchingClusters.Add(c);
                }
            }
        }

        private Shape FindBestShape(Cluster c, List<Shape> shapes, out double inkMatching)
        {
            inkMatching = 0.0;
            Shape bestShape = new Shape();
            foreach (Shape s in shapes)
            {
                double inkInCommon = InkInCommon(s, c);
                if (inkInCommon > inkMatching)
                {
                    bestShape = s;
                    inkMatching = inkInCommon;
                }
            }

            return bestShape;
        }

        private List<Cluster> GetRemainingClusters(List<Cluster> clusters, Dictionary<Cluster, Shape> clusterBinding)
        {
            List<Cluster> clustersRemaining = new List<Cluster>();
            foreach (Cluster c in clusters)
            {
                bool found = false;
                foreach (KeyValuePair<Cluster, Shape> pair in clusterBinding)
                {
                    if (pair.Key == c)
                        found = true;
                }

                if (!found)
                    clustersRemaining.Add(c);
            }

            return clustersRemaining;
        }

        private Dictionary<Cluster, Shape> FindClusterBinding(List<ClusteringResultsShape> resultsShape)
        {
            Dictionary<Cluster, Shape> binding = new Dictionary<Cluster, Shape>();

            for (int i = 0; i < resultsShape.Count; i++)
            {
                bool found = false;
                List<ClusteringResultsShape> conflicting = new List<ClusteringResultsShape>();
                conflicting.Add(resultsShape[i]);
                for (int j = i + 1; j < resultsShape.Count; j++)
                {
                    if (resultsShape[i].BestCluster == resultsShape[j].BestCluster)
                    {
                        found = true;
                        conflicting.Add(resultsShape[j]);
                    }
                }

                if (!found && !binding.ContainsKey(resultsShape[i].BestCluster))
                    binding.Add(resultsShape[i].BestCluster, resultsShape[i].Shape);
                else if (!binding.ContainsKey(resultsShape[i].BestCluster))
                {
                    KeyValuePair<Cluster, Shape> pair = ResolveConflict(conflicting);
                    binding.Add(pair.Key, pair.Value);
                }
            }

            return binding;
        }

        private KeyValuePair<Cluster, Shape> ResolveConflict(List<ClusteringResultsShape> conflicting)
        {
            Dictionary<Shape, int> strokesMatching = new Dictionary<Shape, int>();
            Cluster c = conflicting[0].BestCluster;

            // Check number of strokes in common
            int most = 0;
            foreach (ClusteringResultsShape conflict in conflicting)
            {
                int numMatchingStrokes = StrokesInCommon(c, conflict.Shape);
                most = Math.Max(most, numMatchingStrokes);
                strokesMatching.Add(conflict.Shape, numMatchingStrokes);
            }
            int numWithMost = 0;
            List<ClusteringResultsShape> resultsWithMost = new List<ClusteringResultsShape>();
            foreach (ClusteringResultsShape conflict in conflicting)
            {
                if (strokesMatching[conflict.Shape] == most)
                {
                    numWithMost++;
                    resultsWithMost.Add(conflict);
                }
            }

            if (numWithMost <= 0)
                return new KeyValuePair<Cluster, Shape>(c, conflicting[0].Shape);
            else if (numWithMost == 1)
                return new KeyValuePair<Cluster, Shape>(resultsWithMost[0].BestCluster, resultsWithMost[0].Shape);
            else
            {
                // Check Ink Length in common
                double mostInkInCommon = 0.0;
                Shape best = new Shape();
                foreach (ClusteringResultsShape conflict in resultsWithMost)
                {
                    double ink = InkInCommon(conflict.Shape, c);
                    if (ink > mostInkInCommon)
                        best = conflict.Shape;
                    mostInkInCommon = Math.Max(mostInkInCommon, ink);
                }

                foreach (ClusteringResultsShape conflict in resultsWithMost)
                {
                    if (conflict.Shape == best)
                        return new KeyValuePair<Cluster, Shape>(conflict.BestCluster, conflict.Shape);
                }

                return new KeyValuePair<Cluster, Shape>(resultsWithMost[0].BestCluster, resultsWithMost[0].Shape);
            }

        }

        private bool CheckForErrorsInBinding(Dictionary<Cluster, Shape> clusterBinding, List<ClusteringResultsShape> shapeResults, ref List<ClusteringResultsShape> badShapes)
        {
            bool errors = false;
            foreach (ClusteringResultsShape shapeRes in shapeResults)
            {
                if (clusterBinding.ContainsKey(shapeRes.BestCluster) && clusterBinding[shapeRes.BestCluster] != shapeRes.Shape)
                {
                    badShapes.Add(shapeRes);
                    errors = true;
                }
            }

            return errors;
        }

        /// <summary>
        /// Determine how much ink is shared between a shape and a cluster
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private double InkInCommon(Shape s, Cluster c)
        {
            double ink = 0.0;
            foreach (Substroke shapeStroke in s.SubstrokesL)
            {
                foreach (Substroke clusterStroke in c.Strokes)
                {
                    if (shapeStroke.Id == clusterStroke.Id)
                        ink += clusterStroke.SpatialLength;
                }
            }

            return ink;
        }

        private int StrokesInCommon(Cluster c, Shape s)
        {
            int count = 0;

            foreach (Substroke clusterStroke in c.Strokes)
            {
                foreach (Substroke shapeStroke in s.SubstrokesL)
                {
                    if (clusterStroke == shapeStroke)
                        count++;
                }
            }

            return count;
        }

        public ClusteringResultsShape WorseShapeForCluster(ClusteringResultsShape shapeResult, Shape otherShape)
        {
            ClusteringResultsShape otherShapeResult = new ClusteringResultsShape(otherShape, new List<Cluster>(), new Dictionary<Guid, string>());
            foreach (ClusteringResultsShape s in resultsShape)
            {
                if (s.Shape == otherShape)
                    otherShapeResult = s;
            }


            int strokesInShapeResult = 0;
            int strokesInOtherShapeResult = 0;
            foreach (Substroke s in shapeResult.BestCluster.Strokes)
            {
                if (shapeResult.Shape.SubstrokesL.Contains(s))
                    strokesInShapeResult++;
                else if (otherShapeResult.Shape.SubstrokesL.Contains(s))
                    strokesInOtherShapeResult++;
            }

            if (strokesInShapeResult > strokesInOtherShapeResult)
                return otherShapeResult;
            else if (strokesInOtherShapeResult > strokesInShapeResult)
                return shapeResult;
            else
            {
                return shapeResult;
            }
        }

        #region Check type of shape or stroke

        /// <summary>
        /// Get the type of the shape (as a string)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private string GetShapeType(Shape s)
        {
            if (IsGate(s))
                return "Shape";
            else if (IsLabel(s))
                return "Text";
            else if (IsConnector(s))
                return "Connector";
            else
                return "none";
        }

        /// <summary>
        /// Determine whether a shape is part of a 'gate' by way of its xml.type
        /// </summary>
        /// <param name="s">Shape</param>
        /// <returns></returns>
        private bool IsGate(Shape s)
        {
            List<string> gateShapes = new List<string>();
            gateShapes.Add("AND");
            gateShapes.Add("OR");
            gateShapes.Add("NAND");
            gateShapes.Add("NOR");
            gateShapes.Add("NOT");
            gateShapes.Add("NOTBUBBLE");
            gateShapes.Add("BUBBLE");
            gateShapes.Add("XOR");
            gateShapes.Add("XNOR");
            gateShapes.Add("LabelBox");
            gateShapes.Add("Male");
            gateShapes.Add("Female");

            return gateShapes.Contains(s.XmlAttrs.Type);
        }

        /// <summary>
        /// Determine whether a substroke is part of a 'gate' by way of its xml.type
        /// </summary>
        /// <param name="s">Substroke</param>
        /// <returns>whether it is a gate</returns>
        private bool IsGate(Substroke s)
        {
            List<string> gateShapes = new List<string>();
            gateShapes.Add("AND");
            gateShapes.Add("OR");
            gateShapes.Add("NAND");
            gateShapes.Add("NOR");
            gateShapes.Add("NOT");
            gateShapes.Add("NOTBUBBLE");
            gateShapes.Add("BUBBLE");
            gateShapes.Add("XOR");
            gateShapes.Add("XNOR");
            gateShapes.Add("LabelBox");
            gateShapes.Add("Male");
            gateShapes.Add("Female");

            return gateShapes.Contains(s.FirstLabel);
        }

        /// <summary>
        /// Determine whether a shape is part of a 'label' by way of its xml.type
        /// </summary>
        /// <param name="s">Shape</param>
        /// <returns>whether it is a gate</returns>
        private bool IsLabel(Shape s)
        {
            List<string> labelShapes = new List<string>();
            labelShapes.Add("Label");
            labelShapes.Add("Text");

            return labelShapes.Contains(s.XmlAttrs.Type);
        }

        /// <summary>
        /// Determine whether a substroke is part of a 'label' by way of its xml.type
        /// </summary>
        /// <param name="s">Substroke</param>
        /// <returns>whether it is a gate</returns>
        private bool IsLabel(Substroke s)
        {
            List<string> labelShapes = new List<string>();
            labelShapes.Add("Label");
            labelShapes.Add("Text");

            return labelShapes.Contains(s.FirstLabel);
        }

        /// <summary>
        /// Determine whether a shape is part of a 'connector' by way of its xml.type
        /// </summary>
        /// <param name="s">Shape</param>
        /// <returns>whether it is a connector</returns>
        private bool IsConnector(Shape s)
        {
            List<string> connectorShapes = new List<string>();
            connectorShapes.Add("Wire");
            connectorShapes.Add("ChildLink");
            connectorShapes.Add("Marriage");
            connectorShapes.Add("Divorce");

            return connectorShapes.Contains(s.XmlAttrs.Type);
        }

        /// <summary>
        /// Determine whether a substroke is part of a 'connector' by way of its xml.type
        /// </summary>
        /// <param name="s">Substroke</param>
        /// <returns>whether it is a connector</returns>
        private bool IsConnector(Substroke s)
        {
            List<string> connectorShapes = new List<string>();
            connectorShapes.Add("Wire");
            connectorShapes.Add("ChildLink");
            connectorShapes.Add("Marriage");
            connectorShapes.Add("Divorce");

            return connectorShapes.Contains(s.FirstLabel);
        }

        #endregion

        #region Getters

        public Sketch.Sketch Sketch
        {
            get { return sketch; }
        }

        public List<ClusteringResultsShape> ResultsShape
        {
            get { return resultsShape; }
        }

        public List<ClusteringResultsCluster> ResultsCluster
        {
            get { return resultsCluster; }
        }

        public Dictionary<Guid, string> StrokeClassifications
        {
            get { return strokeClassifications; }
        }

        public int NumPerfect
        {
            get
            {
                int perfect = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    if (result.Correctness == 2)
                        perfect++;
                }

                return perfect;
            }
        }

        public int NumConditionalPerfect
        {
            get
            {
                int conditionalperfect = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    if (result.Correctness == 1)
                        conditionalperfect++;
                }

                return conditionalperfect;
            }
        }

        public int NumConditionalPerfectNoMatchingStrokes
        {
            get
            {
                int conditionalperfect = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    if (result.Correctness == 0)
                        conditionalperfect++;
                }

                return conditionalperfect;
            }
        }



        public int NumShapes
        {
            get
            {
                int count = 0;
                foreach (Shape s in sketch.ShapesL)
                {
                    string type = GetShapeType(s);
                    if (type == "Shape")
                        count++;
                }

                return count;
            }
        }

        public int NumTexts
        {
            get
            {
                int count = 0;
                foreach (Shape s in sketch.ShapesL)
                {
                    string type = GetShapeType(s);
                    if (type == "Text")
                        count++;
                }

                return count;
            }
        }

        public int NumConnectors
        {
            get
            {
                int count = 0;
                foreach (Shape s in sketch.ShapesL)
                {
                    string type = GetShapeType(s);
                    if (type == "Connector")
                        count++;
                }

                return count;
            }
        }



        public int NumPerfectShapes
        {
            get
            {
                int perfect = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    if (result.Correctness == 2 && GetShapeType(result.Shape) == "Shape")
                        perfect++;
                }

                return perfect;
            }
        }

        public int NumPerfectTexts
        {
            get
            {
                int perfect = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    if (result.Correctness == 2 && GetShapeType(result.Shape) == "Text")
                        perfect++;
                }

                return perfect;
            }
        }

        public int NumPerfectConnectors
        {
            get
            {
                int perfect = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    if (result.Correctness == 2 && GetShapeType(result.Shape) == "Connector")
                        perfect++;
                }

                return perfect;
            }
        }



        public int NumCondPerfectShapes
        {
            get
            {
                int perfect = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    if (result.Correctness == 1 && GetShapeType(result.Shape) == "Shape")
                        perfect++;
                }

                return perfect;
            }
        }

        public int NumCondPerfectTexts
        {
            get
            {
                int perfect = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    if (result.Correctness == 1 && GetShapeType(result.Shape) == "Text")
                        perfect++;
                }

                return perfect;
            }
        }

        public int NumCondPerfectConnectors
        {
            get
            {
                int perfect = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    if (result.Correctness == 1 && GetShapeType(result.Shape) == "Connector")
                        perfect++;
                }

                return perfect;
            }
        }



        public int NumTotalErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    errors += result.Errors.Count;
                }

                return errors;
            }
        }

        public int NumSplitErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    foreach (ClusteringError error in result.Errors)
                    {
                        if (error.IsSplitError)
                            errors++;
                    }
                }

                return errors;
            }
        }

        public int NumMergeErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    foreach (ClusteringError error in result.Errors)
                    {
                        if (error.IsMergeError)
                            errors++;
                    }
                }

                return errors;
            }
        }



        public int NumMergedShapeErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    foreach (ClusteringError error in result.Errors)
                    {
                        if (error.IsMergedShapeError)
                            errors++;
                    }
                }

                return errors;
            }
        }

        public int NumMergedWireErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    foreach (ClusteringError error in result.Errors)
                    {
                        if (error.IsMergedWireError)
                            errors++;
                    }
                }

                return errors;
            }
        }

        public int NumMergedTextErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    foreach (ClusteringError error in result.Errors)
                    {
                        if (error.IsMergedTextError)
                            errors++;
                    }
                }

                return errors;
            }
        }



        public int NumMergedShapeToShapeErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    string type = GetShapeType(result.Shape);
                    if (type == "Shape")
                    {
                        foreach (ClusteringError error in result.Errors)
                        {
                            List<string> typesInvolved = error.TypesInvolved;
                            if (typesInvolved.Contains("Shape"))
                                errors++;
                        }
                    }
                }

                return errors;
            }
        }

        public int NumMergedShapeToTextErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    string type = GetShapeType(result.Shape);
                    if (type == "Text")
                    {
                        foreach (ClusteringError error in result.Errors)
                        {
                            List<string> typesInvolved = error.TypesInvolved;
                            if (typesInvolved.Contains("Shape"))
                                errors++;
                        }
                    }
                }

                return errors;
            }
        }

        public int NumMergedShapeToConnectorErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    string type = GetShapeType(result.Shape);
                    if (type == "Connector")
                    {
                        foreach (ClusteringError error in result.Errors)
                        {
                            List<string> typesInvolved = error.TypesInvolved;
                            if (typesInvolved.Contains("Shape"))
                                errors++;
                        }
                    }
                }

                return errors;
            }
        }



        public int NumMergedTextToShapeErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    string type = GetShapeType(result.Shape);
                    if (type == "Shape")
                    {
                        foreach (ClusteringError error in result.Errors)
                        {
                            List<string> typesInvolved = error.TypesInvolved;
                            if (typesInvolved.Contains("Text"))
                                errors++;
                        }
                    }
                }

                return errors;
            }
        }

        public int NumMergedTextToTextErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    string type = GetShapeType(result.Shape);
                    if (type == "Text")
                    {
                        foreach (ClusteringError error in result.Errors)
                        {
                            List<string> typesInvolved = error.TypesInvolved;
                            if (typesInvolved.Contains("Text"))
                                errors++;
                        }
                    }
                }

                return errors;
            }
        }

        public int NumMergedTextToConnectorErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    string type = GetShapeType(result.Shape);
                    if (type == "Connector")
                    {
                        foreach (ClusteringError error in result.Errors)
                        {
                            List<string> typesInvolved = error.TypesInvolved;
                            if (typesInvolved.Contains("Text"))
                                errors++;
                        }
                    }
                }

                return errors;
            }
        }




        public int NumMergedConnectorToShapeErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    string type = GetShapeType(result.Shape);
                    if (type == "Shape")
                    {
                        foreach (ClusteringError error in result.Errors)
                        {
                            List<string> typesInvolved = error.TypesInvolved;
                            if (typesInvolved.Contains("Connector"))
                                errors++;
                        }
                    }
                }

                return errors;
            }
        }

        public int NumMergedConnectorToTextErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    string type = GetShapeType(result.Shape);
                    if (type == "Text")
                    {
                        foreach (ClusteringError error in result.Errors)
                        {
                            List<string> typesInvolved = error.TypesInvolved;
                            if (typesInvolved.Contains("Connector"))
                                errors++;
                        }
                    }
                }

                return errors;
            }
        }

        public int NumMergedConnectorToConnectorErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    string type = GetShapeType(result.Shape);
                    if (type == "Connector")
                    {
                        foreach (ClusteringError error in result.Errors)
                        {
                            List<string> typesInvolved = error.TypesInvolved;
                            if (typesInvolved.Contains("Connector"))
                                errors++;
                        }
                    }
                }

                return errors;
            }
        }



        public int NumMergedNOTBUBBLEToShapeErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    string type = GetShapeType(result.Shape);
                    if (type == "Shape")
                    {
                        foreach (ClusteringError error in result.Errors)
                        {
                            List<string> typesInvolved = error.TypesInvolved;
                            if (typesInvolved.Contains("Shape") && error.InvolvesNOTBUBBLE)
                                errors++;
                        }
                    }
                }

                return errors;
            }
        }

        public int NumMergedNOTBUBBLEisOnlyError
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    string type = GetShapeType(result.Shape);
                    if (type == "Shape")
                    {
                        foreach (ClusteringError error in result.Errors)
                        {
                            List<string> typesInvolved = error.TypesInvolved;
                            if (typesInvolved.Contains("Shape") && error.InvolvesNOTBUBBLE && error.InvolvedClusters.Count == 1)
                            {
                                List<Shape> shapes = new List<Shape>();
                                foreach (Substroke stroke in error.InvolvedClusters[0].Strokes)
                                {
                                    if (!shapes.Contains(stroke.ParentShapes[0]))
                                        shapes.Add(stroke.ParentShapes[0]);
                                }
                                // If the only error is a merged NOTBUBBLE there will be exactly two shapes
                                if (shapes.Count == 2)
                                    errors++;
                            }
                        }
                    }
                }

                return errors;
            }
        }




        public int NumSplitShapeErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    string type = GetShapeType(result.Shape);
                    if (type == "Shape")
                    {
                        foreach (ClusteringError error in result.Errors)
                        {
                            if (error.IsSplitError)
                                errors++;
                        }
                    }
                }

                return errors;
            }
        }

        public int NumSplitTextErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    string type = GetShapeType(result.Shape);
                    if (type == "Text")
                    {
                        foreach (ClusteringError error in result.Errors)
                        {
                            if (error.IsSplitError)
                                errors++;
                        }
                    }
                }

                return errors;
            }
        }

        public int NumSplitConnectorErrors
        {
            get
            {
                int errors = 0;
                foreach (ClusteringResultsShape result in resultsShape)
                {
                    string type = GetShapeType(result.Shape);
                    if (type == "Connector")
                    {
                        foreach (ClusteringError error in result.Errors)
                        {
                            if (error.IsSplitError)
                                errors++;
                        }
                    }
                }

                return errors;
            }
        }



        public int NumExtraClusters
        {
            get
            {
                return CompletelyUnMatchingClusters.Count;
            }
        }



        public double InkMatchingPercentage
        {
            get
            {
                double inkMatching = 0.0;
                double totalInk = 0.0;

                foreach (ClusteringResultsShape result in resultsShape)
                {
                    inkMatching += result.InkMatchingLength;
                    totalInk += result.ShapeArcLength;
                }

                if (totalInk == 0.0)
                    return 0.0;
                else
                    return inkMatching / totalInk;
            }
        }

        public double InkExtraPercentageTotal
        {
            get
            {
                double inkExtra = 0.0;
                double totalInk = 0.0;

                foreach (ClusteringResultsShape result in resultsShape)
                {
                    inkExtra += result.InkExtraLength;
                    totalInk += result.ShapeArcLength;
                }

                foreach (Cluster c in PartiallyMatchingButNotBestClusters)
                    inkExtra += ClusterLength(c);

                foreach (Cluster c in CompletelyUnMatchingClusters)
                    inkExtra += ClusterLength(c);

                if (totalInk == 0.0)
                    return 0.0;
                else
                    return inkExtra / totalInk;
            }
        }

        public double InkExtraPercentageBestMatches
        {
            get
            {
                double inkExtra = 0.0;
                double totalInk = 0.0;

                foreach (ClusteringResultsShape result in resultsShape)
                {
                    inkExtra += result.InkExtraLength;
                    totalInk += result.ShapeArcLength;
                }

                if (totalInk == 0.0)
                    return 0.0;
                else
                    return inkExtra / totalInk;
            }
        }

        public double InkExtraPercentagePartialMatchesNotBest
        {
            get
            {
                double inkExtra = 0.0;
                double totalInk = 0.0;

                foreach (ClusteringResultsShape result in resultsShape)
                    totalInk += result.ShapeArcLength;

                foreach (Cluster c in PartiallyMatchingButNotBestClusters)
                    inkExtra += ClusterLength(c);

                if (totalInk == 0.0)
                    return 0.0;
                else
                    return inkExtra / totalInk;
            }
        }

        public double InkExtraPercentageCompletelyUnMatched
        {
            get
            {
                double inkExtra = 0.0;
                double totalInk = 0.0;

                foreach (ClusteringResultsShape result in resultsShape)
                    totalInk += result.ShapeArcLength;

                foreach (Cluster c in CompletelyUnMatchingClusters)
                    inkExtra += ClusterLength(c);

                if (totalInk == 0.0)
                    return 0.0;
                else
                    return inkExtra / totalInk;
            }
        }

        private double ClusterLength(Cluster c)
        {
            double length = 0.0;
            foreach (Substroke s in c.Strokes)
                length += s.SpatialLength;

            return length;
        }

        
        public bool ContainsCorrectnessLevel(int level)
        {
            foreach (ClusteringResultsShape results in this.resultsShape)
            {
                if (results.Correctness == level)
                    return true;
            }

            return false;
        }

        #endregion
    }

    public class ClusteringResultsShape
    {
        #region Internals

        /// <summary>
        /// 2  = Correct
        /// 1  = Conditional Correct
        /// 0  = No matching strokes (subcase of Conditional Correct)
        /// -1 = Error
        /// -2 = No Match Found
        /// </summary>
        private int correctness;

        /// <summary>
        /// Shape being matched
        /// </summary>
        private Shape shape;

        /// <summary>
        /// Best matching cluster
        /// </summary>
        private Cluster bestCluster;

        /// <summary>
        /// Errors found in matching
        /// </summary>
        private List<ClusteringError> errors;

        /// <summary>
        /// Arc Length of ink that is found matching the shape
        /// </summary>
        private double InkMatching;

        /// <summary>
        /// Length of ink in the shape
        /// </summary>
        private double ShapeInkLength;

        /// <summary>
        /// Ink in the best matching cluster that isn't part of the shape
        /// </summary>
        private double InkExtra;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor. Determines correctness of match and errors if there are any.
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="allClusters"></param>
        /// <param name="strokeClassifications"></param>
        public ClusteringResultsShape(Shape shape, List<Cluster> allClusters, Dictionary<Guid, string> strokeClassifications)
        {
            this.shape = shape;
            this.bestCluster = new Cluster();
            this.errors = new List<ClusteringError>();
            this.InkMatching = 0.0;
            this.InkExtra = 0.0;
            this.ShapeInkLength = ShapeLength(shape);

            correctness = DetermineCorrectness(shape, allClusters, ref bestCluster, strokeClassifications);

            if (correctness == 2)
                this.InkMatching = this.ShapeInkLength;
            else if (correctness == 1)
                this.InkMatching = InkInCommon(shape, bestCluster);
            else if (correctness == 0)
                this.InkMatching = 0.0;
            else if (correctness == -1)
            {
                this.errors = DetermineErrors(shape, bestCluster, allClusters, strokeClassifications);
                this.InkMatching = InkInCommon(shape, bestCluster);
                this.InkExtra = ClusterLength(bestCluster) - this.InkMatching;
            }
        }

        #endregion

        #region Getters

        public int Correctness
        {
            get { return correctness; }
        }

        public Shape Shape
        {
            get { return shape; }
        }

        public Cluster BestCluster
        {
            get { return bestCluster; }
        }

        public List<ClusteringError> Errors
        {
            get { return errors; }
        }

        public double InkMatchingLength
        {
            get { return this.InkMatching; }
        }

        public double InkExtraLength
        {
            get { return this.InkExtra; }
        }

        public double InkMatchingPercentage
        {
            get { return InkMatching / ShapeInkLength; }
        }

        public double ShapeArcLength
        {
            get { return ShapeInkLength; }
        }

        #endregion

        #region Matching Functions

        /// <summary>
        /// Determines how correct of a match there is between a shape and all available clusters.
        /// The best cluster match found is stored in the 'bestCluster' member variable.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="clusters"></param>
        /// <param name="strokeClassifications"></param>
        /// <returns></returns>
        private int DetermineCorrectness(Shape s, List<Cluster> clusters, ref Cluster bestCluster, Dictionary<Guid, string> strokeClassifications)
        {
            int bestCorrectness = -2;

            List<Substroke> strokes = GetCorrectlyLabeledStrokes(s, strokeClassifications);

            if (strokes.Count == 0)
                return 0;
            
            // Determine which cluster best matches the shape
            foreach (Cluster c in clusters)
            {
                int match = Match(s, c, strokeClassifications);
                if (match > bestCorrectness)
                    bestCluster = c;
                else if (match == bestCorrectness)
                {
                    // Pick the cluster with more ink in common
                    bestCluster = MoreInkInCommon(s, c, bestCluster);
                }
                bestCorrectness = Math.Max(match, bestCorrectness);
            }

            if (bestCorrectness > -2)
            {
                // Determine whether the cluster contains extra strokes that shouldn't be there
                foreach (Substroke sub in bestCluster.Strokes)
                {
                    bool found = false;
                    foreach (Substroke shapeStroke in shape.SubstrokesL)
                    {
                        if (sub.Id == shapeStroke.Id)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found) // Merge Error
                    {
                        bestCorrectness = -1;
                        break;
                    }
                }
            }
            
            return bestCorrectness;
        }

        /// <summary>
        /// Determines how well a shape and cluster match
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <param name="strokeClassifications"></param>
        /// <returns></returns>
        private int Match(Shape s, Cluster c, Dictionary<Guid, string> strokeClassifications)
        {
            string type = GetShapeType(s);

            if (c.Type != type)
                return -2;

            // Create List of Correctly labeled strokes
            List<Substroke> strokes = GetCorrectlyLabeledStrokes(s, strokeClassifications);

            // Look up table linking strokes in the shape to whether or not their match was found in the cluster
            Dictionary<Substroke, bool> shapeStrokesFound = new Dictionary<Substroke, bool>(strokes.Count);
            bool strokeMissing = false;

            // Loop through each stroke in the shape and see if you can find a match in the cluster
            foreach (Substroke substroke in strokes)
            {
                bool found = false;
                foreach (Substroke clusterStroke in c.Strokes)
                {
                    if (substroke.Id == clusterStroke.Id)
                        found = true;
                }

                if (!found)
                    strokeMissing = true;

                shapeStrokesFound.Add(substroke, found);
            }

            if (!strokeMissing) // All strokes expected (based on classification) were found
            {
                // All strokes were correctly classified and found
                if (strokes.Count == s.SubstrokesL.Count)
                    return 2;
                else // All strokes that were correctly classified were found, but some were incorrectly classified
                    return 1;
            }
            else // Not all strokes that were expected were found
            {
                foreach (KeyValuePair<Substroke, bool> pair in shapeStrokesFound)
                {
                    // Check to see if there is any match between the shape and cluster
                    if (pair.Value)
                        return -1;
                }

                // If no match, return -2 to indicate that the shape and cluster don't have any correlation
                return -2;
            }
        }

        /// <summary>
        /// Determines which errors are present in the best match
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private List<ClusteringError> DetermineErrors(Shape s, Cluster c, List<Cluster> allClusters, Dictionary<Guid, string> strokeClassifications)
        {
            List<ClusteringError> errors = new List<ClusteringError>();

            // Create List of Correctly labeled strokes
            List<Substroke> strokes = GetCorrectlyLabeledStrokes(s, strokeClassifications);

            // Determine whether the cluster contains extra strokes that shouldn't be there
            List<Substroke> incorrectStrokes = new List<Substroke>();
            foreach (Substroke sub in c.Strokes)
            {
                bool found = false;
                foreach (Substroke shapeStroke in strokes)
                {
                    if (sub.Id == shapeStroke.Id)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    incorrectStrokes.Add(sub);
            }

            List<Shape> shapesInvolved = new List<Shape>();
            foreach (Substroke sub in incorrectStrokes)
            {
                if (IsGate(sub) && !shapesInvolved.Contains(sub.ParentShapes[0]))
                {
                    errors.Add(new ClusteringError(s, c, "Merged Shape"));
                    shapesInvolved.Add(sub.ParentShapes[0]);
                }
                else if (IsConnector(sub) && !shapesInvolved.Contains(sub.ParentShapes[0]))
                {
                    errors.Add(new ClusteringError(s, c, "Merged Wire"));
                    shapesInvolved.Add(sub.ParentShapes[0]);
                }
                else if (IsLabel(sub) && !shapesInvolved.Contains(sub.ParentShapes[0]))
                {
                    errors.Add(new ClusteringError(s, c, "Merged Text"));
                    shapesInvolved.Add(sub.ParentShapes[0]);
                }
            }

            // Determine whether the shape is broken into two or more clusters (Split Error)
            foreach (Substroke shapeStroke in strokes)
            {
                bool found = false;
                foreach (Substroke clusterStroke in c.Strokes)
                {
                    if (shapeStroke.Id == clusterStroke.Id)
                        found = true;
                }

                if (!found)
                {
                    List<Cluster> splitClusters = FindOtherClustersInShape(s, c, allClusters);
                    errors.Add(new ClusteringError(s, splitClusters, "Split"));
                }
            }

            return errors;
        }

        /// <summary>
        /// If there is a split, find what other clusters are in the shape
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <param name="others"></param>
        /// <returns></returns>
        private List<Cluster> FindOtherClustersInShape(Shape s, Cluster c, List<Cluster> others)
        {
            List<Cluster> matchingClusters = new List<Cluster>(1);
            matchingClusters.Add(c);

            foreach (Cluster cluster in others)
            {
                bool found = false;
                foreach (Substroke clusterStroke in cluster.Strokes)
                {
                    foreach (Substroke shapeStroke in s.SubstrokesL)
                    {
                        if (shapeStroke.Id == clusterStroke.Id)
                            found = true;
                    }
                }

                if (found)
                    matchingClusters.Add(cluster);
            }

            return matchingClusters;
        }

        /// <summary>
        /// Determine which cluster has more ink in common with a shape
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <returns></returns>
        private Cluster MoreInkInCommon(Shape s, Cluster c1, Cluster c2)
        {
            double arclength1 = InkInCommon(s, c1);
            double arclength2 = InkInCommon(s, c2);

            if (arclength1 > arclength2)
                return c1;
            else
                return c2;
        }

        /// <summary>
        /// Determine how much ink is shared between a shape and a cluster
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private double InkInCommon(Shape s, Cluster c)
        {
            double ink = 0.0;
            foreach (Substroke shapeStroke in s.SubstrokesL)
            {
                foreach (Substroke clusterStroke in c.Strokes)
                {
                    if (shapeStroke.Id == clusterStroke.Id)
                        ink += clusterStroke.SpatialLength;
                }
            }

            return ink;
        }

        private double ShapeLength(Shape s)
        {
            double ShapeLength = 0.0;
            foreach (Substroke sub in s.SubstrokesL)
                ShapeLength += sub.SpatialLength;
            
            return ShapeLength;
        }

        private double ClusterLength(Cluster c)
        {
            double length = 0.0;
            foreach (Substroke s in c.Strokes)
                length += s.SpatialLength;

            return length;
        }

        #endregion

        #region Check type of shape or stroke


        /// <summary>
        /// Gets a subset of the list of strokes in a shape which have been correctly classified.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="strokeClassifications"></param>
        /// <returns></returns>
        private List<Substroke> GetCorrectlyLabeledStrokes(Shape s, Dictionary<Guid, string> strokeClassifications)
        {
            string type = GetShapeType(s);

            // Create List of Correctly labeled strokes
            List<Substroke> strokes = new List<Substroke>();
            foreach (Substroke sub in s.SubstrokesL)
            {
                if (strokeClassifications.ContainsKey(sub.Id) && strokeClassifications[sub.Id] == type)
                    strokes.Add(sub);
            }

            return strokes;
        }

        /// <summary>
        /// Get the type of the shape (as a string)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private string GetShapeType(Shape s)
        {
            if (IsGate(s))
                return "Other";
            else if (IsLabel(s))
                return "Label";
            else if (IsConnector(s))
                return "Connector";
            else
                return "none";
        }

        /// <summary>
        /// Determine whether a shape is part of a 'gate' by way of its xml.type
        /// </summary>
        /// <param name="s">Shape</param>
        /// <returns></returns>
        private bool IsGate(Shape s)
        {
            List<string> gateShapes = new List<string>();
            gateShapes.Add("AND");
            gateShapes.Add("OR");
            gateShapes.Add("NAND");
            gateShapes.Add("NOR");
            gateShapes.Add("NOT");
            gateShapes.Add("NOTBUBBLE");
            gateShapes.Add("BUBBLE");
            gateShapes.Add("XOR");
            gateShapes.Add("XNOR");
            gateShapes.Add("LabelBox");
            gateShapes.Add("Male");
            gateShapes.Add("Female");

            return gateShapes.Contains(s.XmlAttrs.Type);
        }

        /// <summary>
        /// Determine whether a substroke is part of a 'gate' by way of its xml.type
        /// </summary>
        /// <param name="s">Substroke</param>
        /// <returns>whether it is a gate</returns>
        private bool IsGate(Substroke s)
        {
            List<string> gateShapes = new List<string>();
            gateShapes.Add("AND");
            gateShapes.Add("OR");
            gateShapes.Add("NAND");
            gateShapes.Add("NOR");
            gateShapes.Add("NOT");
            gateShapes.Add("NOTBUBBLE");
            gateShapes.Add("BUBBLE");
            gateShapes.Add("XOR");
            gateShapes.Add("XNOR");
            gateShapes.Add("LabelBox");
            gateShapes.Add("Male");
            gateShapes.Add("Female");

            return gateShapes.Contains(s.FirstLabel);
        }

        /// <summary>
        /// Determine whether a shape is part of a 'label' by way of its xml.type
        /// </summary>
        /// <param name="s">Shape</param>
        /// <returns>whether it is a gate</returns>
        private bool IsLabel(Shape s)
        {
            List<string> labelShapes = new List<string>();
            labelShapes.Add("Label");
            labelShapes.Add("Text");

            return labelShapes.Contains(s.XmlAttrs.Type);
        }

        /// <summary>
        /// Determine whether a substroke is part of a 'label' by way of its xml.type
        /// </summary>
        /// <param name="s">Substroke</param>
        /// <returns>whether it is a gate</returns>
        private bool IsLabel(Substroke s)
        {
            List<string> labelShapes = new List<string>();
            labelShapes.Add("Label");
            labelShapes.Add("Text");

            return labelShapes.Contains(s.FirstLabel);
        }

        /// <summary>
        /// Determine whether a shape is part of a 'connector' by way of its xml.type
        /// </summary>
        /// <param name="s">Shape</param>
        /// <returns>whether it is a connector</returns>
        private bool IsConnector(Shape s)
        {
            List<string> connectorShapes = new List<string>();
            connectorShapes.Add("Wire");
            connectorShapes.Add("ChildLink");
            connectorShapes.Add("Marriage");
            connectorShapes.Add("Divorce");

            return connectorShapes.Contains(s.XmlAttrs.Type);
        }

        /// <summary>
        /// Determine whether a substroke is part of a 'connector' by way of its xml.type
        /// </summary>
        /// <param name="s">Substroke</param>
        /// <returns>whether it is a connector</returns>
        private bool IsConnector(Substroke s)
        {
            List<string> connectorShapes = new List<string>();
            connectorShapes.Add("Wire");
            connectorShapes.Add("ChildLink");
            connectorShapes.Add("Marriage");
            connectorShapes.Add("Divorce");

            return connectorShapes.Contains(s.FirstLabel);
        }

        #endregion
    }

    public class ClusteringResultsCluster
    {
        #region Internals

        /// <summary>
        /// 1  = Correct
        /// 0  = Conditional Correct
        /// -1 = Error
        /// -2 = No Match Found
        /// </summary>
        private int correctness;

        /// <summary>
        /// Cluster Being Matched
        /// </summary>
        private Cluster cluster;

        /// <summary>
        /// Best matching shape
        /// </summary>
        private Shape bestShape;

        /// <summary>
        /// Errors found in matching
        /// </summary>
        private List<ClusteringError> errors;

        #endregion

        #region Constructors

        public ClusteringResultsCluster(Cluster cluster, List<Shape> allShapes, Dictionary<Guid, string> strokeClassifications)
        {
            this.cluster = cluster;
            this.bestShape = new Shape();
            this.errors = new List<ClusteringError>();

            correctness = DetermineCorrectness(cluster, allShapes, ref bestShape, strokeClassifications);

            if (correctness == -1)
                this.errors = DetermineErrors(cluster, bestShape, allShapes, strokeClassifications);
        }

        #endregion

        #region Matching Functions

        /// <summary>
        /// Determines how correct of a match there is between a shape and all available clusters.
        /// The best cluster match found is stored in the 'bestCluster' member variable.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="clusters"></param>
        /// <param name="strokeClassifications"></param>
        /// <returns></returns>
        private int DetermineCorrectness(Cluster c, List<Shape> shapes, ref Shape bestShape, Dictionary<Guid, string> strokeClassifications)
        {
            int bestCorrectness = -2;

            // Determine which cluster best matches the shape
            foreach (Shape s in shapes)
            {
                int match = Match(s, c, strokeClassifications);
                if (match > bestCorrectness)
                    bestShape = s;
                else if (match == bestCorrectness)
                {
                    // Pick the cluster with more ink in common
                    bestShape = MoreInkInCommon(c, s, bestShape);
                }
                bestCorrectness = Math.Max(match, bestCorrectness);
            }

            if (bestCorrectness > -2)
            {
                // Determine whether the cluster contains extra strokes that shouldn't be there
                foreach (Substroke sub in bestShape.SubstrokesL)
                {
                    bool found = false;
                    foreach (Substroke clusterStroke in c.Strokes)
                    {
                        if (sub.Id == clusterStroke.Id)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found) // Merge Error
                        bestCorrectness = -1;
                }
            }

            return bestCorrectness;
        }

        /// <summary>
        /// Determines how well a shape and cluster match
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <param name="strokeClassifications"></param>
        /// <returns></returns>
        private int Match(Shape s, Cluster c, Dictionary<Guid, string> strokeClassifications)
        {
            string type = GetShapeType(s);

            if (c.Type != type)
                return -2;

            // Create List of Correctly labeled strokes
            List<Substroke> strokes = GetCorrectlyLabeledStrokes(s, strokeClassifications);

            // Look up table linking strokes in the shape to whether or not their match was found in the cluster
            Dictionary<Substroke, bool> shapeStrokesFound = new Dictionary<Substroke, bool>(strokes.Count);
            bool strokeMissing = false;

            // Loop through each stroke in the shape and see if you can find a match in the cluster
            foreach (Substroke substroke in strokes)
            {
                bool found = false;
                foreach (Substroke clusterStroke in c.Strokes)
                {
                    if (substroke.Id == clusterStroke.Id)
                        found = true;
                }

                if (!found)
                    strokeMissing = true;

                shapeStrokesFound.Add(substroke, found);
            }

            if (!strokeMissing) // All strokes expected (based on classification) were found
            {
                // All strokes were correctly classified and found
                if (strokes.Count == s.SubstrokesL.Count)
                    return 1;
                else // All strokes that were correctly classified were found, but some were incorrectly classified
                    return 0;
            }
            else // Not all strokes that were expected were found
            {
                foreach (KeyValuePair<Substroke, bool> pair in shapeStrokesFound)
                {
                    // Check to see if there is any match between the shape and cluster
                    if (pair.Value)
                        return -1;
                }

                // If no match, return -2 to indicate that the shape and cluster don't have any correlation
                return -2;
            }
        }

        /// <summary>
        /// Determines which errors are present in the best match
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private List<ClusteringError> DetermineErrors(Cluster c, Shape s, List<Shape> allShapes, Dictionary<Guid, string> strokeClassifications)
        {
            List<ClusteringError> errors = new List<ClusteringError>();

            // Create List of Correctly labeled strokes
            List<Substroke> strokes = GetCorrectlyLabeledStrokes(s, strokeClassifications);

            // Determine whether the cluster contains extra strokes that shouldn't be there
            foreach (Substroke sub in c.Strokes)
            {
                bool found = false;
                foreach (Substroke shapeStroke in strokes)
                {
                    if (sub.Id == shapeStroke.Id)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found && strokeClassifications[sub.Id] == "Other")
                    errors.Add(new ClusteringError(s, c, "Merged Shape"));
                else if (!found && strokeClassifications[sub.Id] == "Wire")
                    errors.Add(new ClusteringError(s, c, "Merged Wire"));
                else if (!found && strokeClassifications[sub.Id] == "Label")
                    errors.Add(new ClusteringError(s, c, "Merged Text"));
            }

            // Determine whether the shape is broken into two or more clusters (Split Error)
            foreach (Substroke shapeStroke in strokes)
            {
                bool found = false;
                foreach (Substroke clusterStroke in c.Strokes)
                {
                    if (shapeStroke.Id == clusterStroke.Id)
                        found = true;
                }

                if (!found)
                {
                    List<Shape> splitShapes = FindOtherShapesInCluster(s, c, allShapes);
                    errors.Add(new ClusteringError(splitShapes, c, "Split"));
                }
            }

            return errors;
        }

        /// <summary>
        /// If there is a split, find what other clusters are in the shape
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <param name="others"></param>
        /// <returns></returns>
        private List<Shape> FindOtherShapesInCluster(Shape s, Cluster c, List<Shape> others)
        {
            List<Shape> matchingShapes = new List<Shape>(1);
            matchingShapes.Add(s);

            foreach (Shape shape in others)
            {
                bool found = false;
                foreach (Substroke shapeStroke in shape.SubstrokesL)
                {
                    foreach (Substroke clusterStroke in c.Strokes)
                    {
                        if (shapeStroke.Id == clusterStroke.Id)
                            found = true;
                    }
                }

                if (found)
                    matchingShapes.Add(shape);
            }

            return matchingShapes;
        }

        /// <summary>
        /// Determine which cluster has more ink in common with a shape
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <returns></returns>
        private Shape MoreInkInCommon(Cluster c, Shape s1, Shape s2)
        {
            double arclength1 = InkInCommon(s1, c);
            double arclength2 = InkInCommon(s2, c);

            if (arclength1 > arclength2)
                return s1;
            else
                return s2;
        }

        /// <summary>
        /// Determine how much ink is shared between a shape and a cluster
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private double InkInCommon(Shape s, Cluster c)
        {
            double ink = 0.0;
            foreach (Substroke shapeStroke in s.SubstrokesL)
            {
                foreach (Substroke clusterStroke in c.Strokes)
                {
                    if (shapeStroke.Id == clusterStroke.Id)
                        ink += clusterStroke.SpatialLength;
                }
            }

            return ink;
        }

        /// <summary>
        /// Gets a subset of the list of strokes in a shape which have been correctly classified.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="strokeClassifications"></param>
        /// <returns></returns>
        private List<Substroke> GetCorrectlyLabeledStrokes(Shape s, Dictionary<Guid, string> strokeClassifications)
        {
            string type = GetShapeType(s);

            // Create List of Correctly labeled strokes
            List<Substroke> strokes = new List<Substroke>();
            foreach (Substroke sub in s.SubstrokesL)
            {
                if (strokeClassifications.ContainsKey(sub.Id) && strokeClassifications[sub.Id] == type)
                    strokes.Add(sub);
            }

            return strokes;
        }

        /// <summary>
        /// Get the type of the shape (as a string)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private string GetShapeType(Shape s)
        {
            if (IsGate(s))
                return "Other";
            else if (IsLabel(s))
                return "Label";
            else if (IsConnector(s))
                return "Wire";
            else
                return "none";
        }

        #endregion

        #region Check type of shape or stroke

        /// <summary>
        /// Determine whether a shape is part of a 'gate' by way of its xml.type
        /// </summary>
        /// <param name="s">Shape</param>
        /// <returns></returns>
        private bool IsGate(Shape s)
        {
            List<string> gateShapes = new List<string>();
            gateShapes.Add("AND");
            gateShapes.Add("OR");
            gateShapes.Add("NAND");
            gateShapes.Add("NOR");
            gateShapes.Add("NOT");
            gateShapes.Add("NOTBUBBLE");
            gateShapes.Add("BUBBLE");
            gateShapes.Add("XOR");
            gateShapes.Add("XNOR");
            gateShapes.Add("LabelBox");
            gateShapes.Add("Male");
            gateShapes.Add("Female");

            return gateShapes.Contains(s.XmlAttrs.Type);
        }

        /// <summary>
        /// Determine whether a substroke is part of a 'gate' by way of its xml.type
        /// </summary>
        /// <param name="s">Substroke</param>
        /// <returns>whether it is a gate</returns>
        private bool IsGate(Substroke s)
        {
            List<string> gateShapes = new List<string>();
            gateShapes.Add("AND");
            gateShapes.Add("OR");
            gateShapes.Add("NAND");
            gateShapes.Add("NOR");
            gateShapes.Add("NOT");
            gateShapes.Add("NOTBUBBLE");
            gateShapes.Add("BUBBLE");
            gateShapes.Add("XOR");
            gateShapes.Add("XNOR");
            gateShapes.Add("LabelBox");
            gateShapes.Add("Male");
            gateShapes.Add("Female");

            return gateShapes.Contains(s.FirstLabel);
        }

        /// <summary>
        /// Determine whether a shape is part of a 'label' by way of its xml.type
        /// </summary>
        /// <param name="s">Shape</param>
        /// <returns>whether it is a gate</returns>
        private bool IsLabel(Shape s)
        {
            List<string> labelShapes = new List<string>();
            labelShapes.Add("Label");
            labelShapes.Add("Text");

            return labelShapes.Contains(s.XmlAttrs.Type);
        }

        /// <summary>
        /// Determine whether a substroke is part of a 'label' by way of its xml.type
        /// </summary>
        /// <param name="s">Substroke</param>
        /// <returns>whether it is a gate</returns>
        private bool IsLabel(Substroke s)
        {
            List<string> labelShapes = new List<string>();
            labelShapes.Add("Label");
            labelShapes.Add("Text");

            return labelShapes.Contains(s.FirstLabel);
        }

        /// <summary>
        /// Determine whether a shape is part of a 'connector' by way of its xml.type
        /// </summary>
        /// <param name="s">Shape</param>
        /// <returns>whether it is a connector</returns>
        private bool IsConnector(Shape s)
        {
            List<string> connectorShapes = new List<string>();
            connectorShapes.Add("Wire");
            connectorShapes.Add("ChildLink");
            connectorShapes.Add("Marriage");
            connectorShapes.Add("Divorce");

            return connectorShapes.Contains(s.XmlAttrs.Type);
        }

        /// <summary>
        /// Determine whether a substroke is part of a 'connector' by way of its xml.type
        /// </summary>
        /// <param name="s">Substroke</param>
        /// <returns>whether it is a connector</returns>
        private bool IsConnector(Substroke s)
        {
            List<string> connectorShapes = new List<string>();
            connectorShapes.Add("Wire");
            connectorShapes.Add("ChildLink");
            connectorShapes.Add("Marriage");
            connectorShapes.Add("Divorce");

            return connectorShapes.Contains(s.FirstLabel);
        }

        #endregion
    }

    public class ClusteringError
    {
        private Guid id;
        private string type;
        private List<Shape> involvedShapes;
        private List<Cluster> involvedClusters;

        /// <summary>
        /// Error Types: "Merged Shape", "Merged Wire", "Merged Text", "Split"
        /// </summary>
        /// <param name="shapes"></param>
        /// <param name="clusters"></param>
        /// <param name="errorType"></param>
        public ClusteringError(List<Shape> shapes, List<Cluster> clusters, string errorType)
        {
            id = Guid.NewGuid();
            type = errorType;
            involvedShapes = shapes;
            involvedClusters = clusters;
        }

        /// <summary>
        /// Error Types: "Merged Shape", "Merged Wire", "Merged Text", "Split"
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="cluster"></param>
        /// <param name="errorType"></param>
        public ClusteringError(Shape shape, Cluster cluster, string errorType)
        {
            id = Guid.NewGuid();
            type = errorType;
            involvedShapes = new List<Shape>(1);
            involvedShapes.Add(shape);
            involvedClusters = new List<Cluster>(1);
            involvedClusters.Add(cluster);
        }

        /// <summary>
        /// Error Types: "Merged Shape", "Merged Wire", "Merged Text", "Split"
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="clusters"></param>
        /// <param name="errorType"></param>
        public ClusteringError(Shape shape, List<Cluster> clusters, string errorType)
        {
            id = Guid.NewGuid();
            type = errorType;
            involvedShapes = new List<Shape>(1);
            involvedShapes.Add(shape);
            involvedClusters = clusters;
        }

        /// <summary>
        /// Error Types: "Merged Shape", "Merged Wire", "Merged Text", "Split"
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="clusters"></param>
        /// <param name="errorType"></param>
        public ClusteringError(List<Shape> shapes, Cluster cluster, string errorType)
        {
            id = Guid.NewGuid();
            type = errorType;
            involvedShapes = shapes;
            involvedClusters = new List<Cluster>();
            involvedClusters.Add(cluster);
        }

        public bool Involves(Cluster cluster)
        {
            foreach (Cluster c in involvedClusters)
            {
                if (cluster.Id == c.Id)
                    return true;
            }

            return false;
        }

        public bool Involves(Shape shape)
        {
            foreach (Shape s in involvedShapes)
            {
                if (shape.Id == s.Id)
                    return true;
            }

            return false;
        }

        #region GETTERS

        public Guid Id
        {
            get { return id; }
        }

        public string ErrorType
        {
            get { return type; }
        }

        public List<Shape> InvolvedShapes
        {
            get { return involvedShapes; }
        }

        public List<Cluster> InvolvedClusters
        {
            get { return involvedClusters; }
        }

        public bool IsSplitError
        {
            get
            {
                if (type == "Split")
                    return true;
                else
                    return false;
            }
        }

        public bool IsMergeError
        {
            get
            {
                if (type.Contains("Merge"))
                    return true;
                else
                    return false;
            }
        }

        public bool IsMergedShapeError
        {
            get
            {
                if (type == "Merged Shape")
                    return true;
                else
                    return false;
            }
        }

        public bool IsMergedWireError
        {
            get
            {
                if (type == "Merged Wire")
                    return true;
                else
                    return false;
            }
        }

        public bool IsMergedTextError
        {
            get
            {
                if (type == "Merged Text")
                    return true;
                else
                    return false;
            }
        }

        public bool MergedSameType
        {
            get
            {
                if (involvedShapes.Count > 0 && involvedClusters.Count > 0)
                {
                    bool[] othersInvolved = OthersInvoled(involvedClusters[0], involvedShapes[0]);
                    if (IsGate(involvedShapes[0]) && othersInvolved[2])
                        return true;
                    else if (IsLabel(involvedShapes[0]) && othersInvolved[1])
                        return true;
                    else if (IsConnector(involvedShapes[0]) && othersInvolved[0])
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Get the number of types of strokes involved in the error.
        /// This does NOT include strokes in the involved shape.
        /// Example: Shape 'A' is a Gate (4 strokes), Cluster includes 3 of 4 strokes in Shape 'A'
        ///     Cluster also includes 1 wire stroke and 1 gate stroke from Shape 'B'
        ///     Returns: 2 types involved.
        /// </summary>
        public int NumTypesInvolved
        {
            get
            {
                bool[] othersInvolved = OthersInvoled(involvedClusters[0], involvedShapes[0]);
                int count = 0;
                foreach (bool type in othersInvolved)
                {
                    if (type)
                        count++;
                }

                return count;
            }
        }

        /// <summary>
        /// Types: Connector, Text, Shape
        /// </summary>
        public List<string> TypesInvolved
        {
            get
            {
                bool[] othersInvolved = OthersInvoled(involvedClusters[0], involvedShapes[0]);
                List<string> involved = new List<string>();
                if (othersInvolved[0])
                    involved.Add("Connector");
                if (othersInvolved[1])
                    involved.Add("Text");
                if (othersInvolved[2])
                    involved.Add("Shape");

                return involved;
            }
        }

        public bool InvolvesNOTBUBBLE
        {
            get
            {
                foreach (Shape s in involvedShapes)
                {
                    if (s.XmlAttrs.Type == "NOTBUBBLE")
                        return true;
                }

                foreach (Cluster c in involvedClusters)
                {
                    foreach (Substroke s in c.Strokes)
                    {
                        if (s.FirstLabel == "NOTBUBBLE")
                            return true;
                    }
                }

                return false;
            }
        }

        public bool MultipleShapes
        {
            get
            {
                if (involvedShapes.Count > 1)
                    return true;
                else
                    return false;
            }
        }

        public bool MultipleClusters
        {
            get
            {
                if (involvedClusters.Count > 1)
                    return true;
                else
                    return false;
            }
        }

        #endregion

        #region Check type of shape or stroke

        /// <summary>
        /// Determine whether a shape is part of a 'gate' by way of its xml.type
        /// </summary>
        /// <param name="s">Shape</param>
        /// <returns></returns>
        private bool IsGate(Shape s)
        {
            List<string> gateShapes = new List<string>();
            gateShapes.Add("AND");
            gateShapes.Add("OR");
            gateShapes.Add("NAND");
            gateShapes.Add("NOR");
            gateShapes.Add("NOT");
            gateShapes.Add("NOTBUBBLE");
            gateShapes.Add("BUBBLE");
            gateShapes.Add("XOR");
            gateShapes.Add("XNOR");
            gateShapes.Add("LabelBox");
            gateShapes.Add("Male");
            gateShapes.Add("Female");

            return gateShapes.Contains(s.XmlAttrs.Type);
        }

        /// <summary>
        /// Determine whether a substroke is part of a 'gate' by way of its xml.type
        /// </summary>
        /// <param name="s">Substroke</param>
        /// <returns>whether it is a gate</returns>
        private bool IsGate(Substroke s)
        {
            List<string> gateShapes = new List<string>();
            gateShapes.Add("AND");
            gateShapes.Add("OR");
            gateShapes.Add("NAND");
            gateShapes.Add("NOR");
            gateShapes.Add("NOT");
            gateShapes.Add("NOTBUBBLE");
            gateShapes.Add("BUBBLE");
            gateShapes.Add("XOR");
            gateShapes.Add("XNOR");
            gateShapes.Add("LabelBox");
            gateShapes.Add("Male");
            gateShapes.Add("Female");

            return gateShapes.Contains(s.FirstLabel);
        }

        private bool[] OthersInvoled(Cluster c, Shape s)
        {
            List<Substroke> NonInvolvedSubstrokes = new List<Substroke>();
            foreach (Substroke sub in c.Strokes)
            {
                if (!s.SubstrokesL.Contains(sub))
                    NonInvolvedSubstrokes.Add(sub);
            }
            
            // 0 - Wire
            // 1 - Label
            // 2 - Gate
            bool[] otherTypesInvolved = new bool[3];
            otherTypesInvolved[0] = otherTypesInvolved[1] = otherTypesInvolved[2] = false;

            foreach (Substroke sub in NonInvolvedSubstrokes)
            {
                if (IsConnector(sub))
                    otherTypesInvolved[0] = true;
                else if (IsLabel(sub))
                    otherTypesInvolved[1] = true;
                else if (IsGate(sub))
                    otherTypesInvolved[2] = true;
            }

            return otherTypesInvolved;
        }

        /// <summary>
        /// Determine whether a shape is part of a 'label' by way of its xml.type
        /// </summary>
        /// <param name="s">Shape</param>
        /// <returns>whether it is a gate</returns>
        private bool IsLabel(Shape s)
        {
            List<string> labelShapes = new List<string>();
            labelShapes.Add("Label");
            labelShapes.Add("Text");

            return labelShapes.Contains(s.XmlAttrs.Type);
        }

        /// <summary>
        /// Determine whether a substroke is part of a 'label' by way of its xml.type
        /// </summary>
        /// <param name="s">Substroke</param>
        /// <returns>whether it is a gate</returns>
        private bool IsLabel(Substroke s)
        {
            List<string> labelShapes = new List<string>();
            labelShapes.Add("Label");
            labelShapes.Add("Text");

            return labelShapes.Contains(s.FirstLabel);
        }

        /// <summary>
        /// Determine whether a shape is part of a 'connector' by way of its xml.type
        /// </summary>
        /// <param name="s">Shape</param>
        /// <returns>whether it is a connector</returns>
        private bool IsConnector(Shape s)
        {
            List<string> connectorShapes = new List<string>();
            connectorShapes.Add("Wire");
            connectorShapes.Add("ChildLink");
            connectorShapes.Add("Marriage");
            connectorShapes.Add("Divorce");

            return connectorShapes.Contains(s.XmlAttrs.Type);
        }

        /// <summary>
        /// Determine whether a substroke is part of a 'connector' by way of its xml.type
        /// </summary>
        /// <param name="s">Substroke</param>
        /// <returns>whether it is a connector</returns>
        private bool IsConnector(Substroke s)
        {
            List<string> connectorShapes = new List<string>();
            connectorShapes.Add("Wire");
            connectorShapes.Add("ChildLink");
            connectorShapes.Add("Marriage");
            connectorShapes.Add("Divorce");

            return connectorShapes.Contains(s.FirstLabel);
        }

        #endregion
    }

    public class ClusterResults
    {
        private List<Shape> _CorrectShapes; // Hand Labeled Clusters
        private List<Cluster> _MachineClusters; // Machine Generated Clusters
        private List<Guid> _ShapeIDs;
        private Dictionary<Guid, double> _ShapeInkMatchPercentage; // Percent (0.0 - 1.0) Ink Matching for each shape
        private Dictionary<Guid, double> _ShapeInkExtraPercentage; // Percent of Ink in cluster that isn't in shape
        private int _NumPerfect; // Number of Perfectly Matching Clusters
        private double _TotalExpectedArcLength; // Arc Length of all strokes in _CorrectShapes
        private double _TotalMatchingArcLength; // Total Arc Length of Matching Strokes
        private double _TotalExtraArcLength;    // Arc Length of unmatching strokes
        // Machine clusters that have a perfect (ArcLength Wise) match to a hand labeled cluster
        private List<Cluster> _MatchingCompleteMachineClusters;
        // Machine clusters that have all the hand labeled cluster's ink, but have extra ink
        private List<Cluster> _MatchingCompleteExtraMachineClusters;
        // Machine clusters that have some of the hand labeled cluster's ink, but also have extra ink
        private List<Cluster> _MatchingPartialExtraMachineClusters;
        // Machine clusters that have a connection, and are the best matching machine cluster for a given hand cluster
        private List<Cluster> _MatchingPartialMissingMachineClusters;
        // Machine clusters that have some connection, but aren't the best matching machine cluster for a given hand cluster
        private List<Cluster> _UnMatchingPartialMachineClusters; 
        // Machine clusters that have absolutely no connection to a hand labeled cluster
        private List<Cluster> _UnMatchingCompleteMachineClusters; 
        // Number of times in a sketch that partial missing machine clusters can be combined to get the actual hand cluster
        private int _CompleteablePartialClusters;


        public ClusterResults(List<Shape> hand, List<Cluster> machine)
        {
            _CorrectShapes = hand;
            _MachineClusters = machine;
            _UnMatchingCompleteMachineClusters = new List<Cluster>();
            _UnMatchingPartialMachineClusters = new List<Cluster>();
            _MatchingPartialMissingMachineClusters = new List<Cluster>();
            _MatchingCompleteMachineClusters = new List<Cluster>();
            _MatchingCompleteExtraMachineClusters = new List<Cluster>();
            _MatchingPartialExtraMachineClusters = new List<Cluster>();
            _ShapeIDs = new List<Guid>(hand.Count);
            _ShapeInkMatchPercentage = new Dictionary<Guid, double>(hand.Count);
            _ShapeInkExtraPercentage = new Dictionary<Guid, double>(hand.Count);
            _CompleteablePartialClusters = 0;
            _NumPerfect = 0;
            _TotalExpectedArcLength = 0.0;
            _TotalExtraArcLength = 0.0;
            _TotalMatchingArcLength = 0.0;

            CalculateInfo();
        }

        private void CalculateInfo()
        {
            List<Guid> RemainingClusters = new List<Guid>(_MachineClusters.Count);
            List<Guid> RemainingShapes = new List<Guid>(_CorrectShapes.Count);
            foreach (Cluster c in _MachineClusters)
                RemainingClusters.Add(c.Id);
            foreach (Shape s in _CorrectShapes)
                RemainingShapes.Add(s.Id);            

            _NumPerfect = 0;

            FindPerfectClusterMatches(ref RemainingClusters, ref RemainingShapes);

            FindPartialClusterMatches(ref RemainingClusters, ref RemainingShapes);

            FindUnmatchingClusters(ref RemainingClusters, ref RemainingShapes);

            FindCompleteableClusters(ref RemainingClusters, ref RemainingShapes);            
        }

        private void FindPerfectClusterMatches(ref List<Guid> RemainingClusters, ref List<Guid> RemainingShapes)
        {
            foreach (Shape shape in _CorrectShapes)
            {
                _ShapeIDs.Add(shape.Id);
                if (RemainingShapes.Contains(shape.Id))
                {
                    foreach (Cluster cluster in _MachineClusters)
                    {
                        if (RemainingClusters.Contains(cluster.Id))
                        {
                            if (ClustersMatch(shape, cluster))
                            {
                                RemainingClusters.Remove(cluster.Id);
                                RemainingShapes.Remove(shape.Id);
                                _NumPerfect++;
                                _ShapeInkMatchPercentage.Add(shape.Id, 1.0);
                                _ShapeInkExtraPercentage.Add(shape.Id, 0.0);
                                _TotalExpectedArcLength += GetTotalArcLength(shape);
                                _TotalMatchingArcLength += GetTotalArcLength(shape);
                                _MatchingCompleteMachineClusters.Add(cluster);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void FindPartialClusterMatches(ref List<Guid> RemainingClusters, ref List<Guid> RemainingShapes)
        {
            foreach (Shape shape in _CorrectShapes)
            {
                if (RemainingShapes.Contains(shape.Id))
                {
                    double TotalExpectedArcLength = GetTotalArcLength(shape);
                    _TotalExpectedArcLength += TotalExpectedArcLength;

                    double maxAccuracy = 0.0;
                    double maxAccuracyArcLength = 0.0;
                    Cluster maxAccuracyCluster = new Cluster();
                    foreach (Cluster cluster in _MachineClusters)
                    {
                        if (RemainingClusters.Contains(cluster.Id))
                        {
                            double arcLength;
                            double accuracy = FindInkMatch(shape, cluster, out arcLength);
                            if (accuracy > maxAccuracy)
                            {
                                maxAccuracy = accuracy;
                                maxAccuracyCluster = cluster;
                                maxAccuracyArcLength = arcLength;
                            }
                        }
                    }

                    _ShapeInkMatchPercentage.Add(shape.Id, maxAccuracy);

                    double TotalMatchingArcLength = maxAccuracyArcLength;
                    _TotalMatchingArcLength += TotalMatchingArcLength;
                    double extraInk = GetTotalArcLength(maxAccuracyCluster) - TotalMatchingArcLength;
                    _ShapeInkExtraPercentage.Add(shape.Id, extraInk / TotalExpectedArcLength);
                    if (maxAccuracy > 0.0)
                    {
                        RemainingClusters.Remove(maxAccuracyCluster.Id);
                        _TotalExtraArcLength += extraInk;
                        if (maxAccuracy == 1.0 && extraInk == 0.0)
                        {
                            _MatchingCompleteMachineClusters.Add(maxAccuracyCluster);
                            _NumPerfect++;
                        }
                        else if (maxAccuracy == 1.0 && extraInk > 0.0)
                            _MatchingCompleteExtraMachineClusters.Add(maxAccuracyCluster);
                        else if (extraInk == 0.0)
                            _MatchingPartialMissingMachineClusters.Add(maxAccuracyCluster);
                        else if (extraInk > 0.0)
                            _MatchingPartialExtraMachineClusters.Add(maxAccuracyCluster);
                    }
                }
            }
        }

        private void FindUnmatchingClusters(ref List<Guid> RemainingClusters, ref List<Guid> RemainingShapes)
        {
            List<Cluster> rClusters = new List<Cluster>(RemainingClusters.Count);
            foreach (Guid id in RemainingClusters)
            {
                foreach (Cluster machineCluster in _MachineClusters)
                {
                    if (id == machineCluster.Id)
                        rClusters.Add(machineCluster);
                }
            }

            foreach (Cluster machineCluster in rClusters)
            {
                bool connectionToHandClusters = false;
                foreach (Shape handCluster in _CorrectShapes)
                {
                    double arcLength;
                    if (FindInkMatch(handCluster, machineCluster, out arcLength) > 0.0)
                        connectionToHandClusters = true;
                }

                if (connectionToHandClusters)
                    _UnMatchingPartialMachineClusters.Add(machineCluster);
                else
                    _UnMatchingCompleteMachineClusters.Add(machineCluster);
            }
        }

        private void FindCompleteableClusters(ref List<Guid> RemainingClusters, ref List<Guid> RemainingShapes)
        {
            List<Guid> matchingPartialIDs = new List<Guid>(_MatchingPartialMissingMachineClusters.Count);
            List<Guid> unmatchingPartialIDs = new List<Guid>(_UnMatchingPartialMachineClusters.Count);
            foreach (Cluster c in _MatchingPartialMissingMachineClusters)
                matchingPartialIDs.Add(c.Id);
            foreach (Cluster c in _UnMatchingPartialMachineClusters)
                unmatchingPartialIDs.Add(c.Id);

            foreach (Cluster matchingC in _MatchingPartialMissingMachineClusters)
            {
                if (matchingPartialIDs.Contains(matchingC.Id))
                {
                    foreach (Cluster unMatchingC in _UnMatchingPartialMachineClusters)
                    {
                        if (unmatchingPartialIDs.Contains(unMatchingC.Id))
                        {
                            if (JoinClustersFormsCompleteShape(matchingC, unMatchingC))
                            {
                                matchingPartialIDs.Remove(matchingC.Id);
                                unmatchingPartialIDs.Remove(unMatchingC.Id);
                                _CompleteablePartialClusters++;
                            }
                        }
                    }
                }
            }
        }

        private bool JoinClustersFormsCompleteShape(Cluster a, Cluster b)
        {
            Cluster temp = a;
            temp.addStroke(b.Strokes);

            bool matchFound = false;

            foreach (Shape s in _CorrectShapes)
            {
                bool shapeMatches = true;
                foreach (Substroke stroke in temp.Strokes)
                {
                    if (!s.SubstrokesL.Contains(stroke))
                        shapeMatches = false;
                }

                if (shapeMatches)
                    matchFound = true;

                /*if (ClustersMatch(s, temp))
                {
                    matchFound = true;
                    break;
                }*/
            }

            return matchFound;
        }

        /// <summary>
        /// Determine whether a hand-labeled cluster matches a machine-generated (NN) cluster
        /// </summary>
        /// <param name="shape">Hand-labeled cluster</param>
        /// <param name="cluster">Machine-generated (NN) cluster</param>
        /// <returns>Whether they contain the same strokes</returns>
        private bool ClustersMatch(Shape shape, Cluster cluster)
        {
            if (shape.SubstrokesL.Count != cluster.Strokes.Count)
                return false;
            else
            {
                bool found = false;

                foreach (Substroke s1 in shape.SubstrokesL)
                {
                    found = false;
                    foreach (Substroke s2 in cluster.Strokes)
                    {
                        if (s1.Id == s2.Id)
                            found = true;
                    }
                }

                if (!found)
                    return false;
            }

            return true;
        }

        private double FindInkMatch(Shape shape, Cluster cluster, out double matchingArcLength)
        {
            List<Guid> ShapeStrokesRemaining = new List<Guid>(shape.SubstrokesL.Count);
            List<Guid> ClusterStrokesRemaining = new List<Guid>(cluster.Strokes.Count);
            foreach (Substroke s in shape.SubstrokesL)
                ShapeStrokesRemaining.Add(s.Id);
            foreach (Substroke s in cluster.Strokes)
                ClusterStrokesRemaining.Add(s.Id);

            bool AnyFound = false;

            foreach (Substroke shapeStrokes in shape.SubstrokesL)
            {
                if (ShapeStrokesRemaining.Contains(shapeStrokes.Id))
                {
                    foreach (Substroke clusterStrokes in cluster.Strokes)
                    {
                        if (ClusterStrokesRemaining.Contains(clusterStrokes.Id))
                        {
                            if (shapeStrokes.Id == clusterStrokes.Id)
                            {
                                ShapeStrokesRemaining.Remove(shapeStrokes.Id);
                                ClusterStrokesRemaining.Remove(clusterStrokes.Id);
                                AnyFound = true;
                            }
                        }
                    }
                }
            }

            if (AnyFound)
            {
                double arcLengthExpected = GetTotalArcLength(shape);
                matchingArcLength = GetMatchingArcLength(shape, cluster);
                return matchingArcLength / arcLengthExpected;
            }
            else
            {
                matchingArcLength = 0.0;
                return 0.0;
            }
        }

        private double GetTotalArcLength(Shape shape)
        {
            double length = 0.0;

            foreach (Substroke stroke in shape.SubstrokesL)
                length += stroke.SpatialLength;            

            return length;
        }

        private double GetTotalArcLength(Cluster cluster)
        {
            double length = 0.0;

            foreach (Substroke stroke in cluster.Strokes)
                length += stroke.SpatialLength;

            return length;
        }

        private double GetMatchingArcLength(Shape shape, Cluster cluster)
        {
            List<Substroke> matchingStrokes = new List<Substroke>();

            foreach (Substroke shapeStrokes in shape.SubstrokesL)
            {
                foreach (Substroke clusterStrokes in cluster.Strokes)
                {
                    if (shapeStrokes.Id == clusterStrokes.Id)
                    {
                        matchingStrokes.Add(clusterStrokes);
                        break;
                    }
                }
            }

            double length = 0.0;

            foreach (Substroke s in matchingStrokes)
                length += s.SpatialLength;

            return length;
        }


        #region Getters
        public int NumExpectedClusters
        {
            get { return _CorrectShapes.Count; }
        }

        public int NumMachineClusters
        {
            get { return _MachineClusters.Count; }
        }

        public int NumPerfectMatches
        {
            get { return _NumPerfect; }
        }

        public double CorrectInkAccuracy
        {
            get { return _TotalMatchingArcLength / _TotalExpectedArcLength * 100.0; }
        }

        public double MissingInkAccuracy
        {
            get { return (_TotalExpectedArcLength - _TotalMatchingArcLength) / _TotalExpectedArcLength * 100.0; }
        }

        public double ExtraInkAccuracy
        {
            get { return _TotalExtraArcLength / _TotalExpectedArcLength * 100.0; }
        }

        public double TotalExpectedArcLength
        {
            get { return _TotalExpectedArcLength; }
        }

        public double TotalMatchingArcLength
        {
            get { return _TotalMatchingArcLength; }
        }

        public double TotalExtraArcLength
        {
            get { return _TotalExtraArcLength; }
        }

        public Dictionary<Guid, double> ShapeInkAccuracy
        {
            get { return _ShapeInkMatchPercentage; }
        }

        public Dictionary<Guid, double> ShapeExtraInk
        {
            get { return _ShapeInkExtraPercentage; }
        }

        public List<Guid> ShapeIDs
        {
            get { return _ShapeIDs; }
        }

        /// <summary>
        /// Number of occurances of: (a = InkAccuracy, e = ExtraInk)
        ///  [0]: a = 100%, e = 0%
        ///  [1]: 100% > a >= 80%, e = 0%
        ///  [2]: 80% > a >= 60%, e = 0%
        ///  [3]: 60% > a >= 40%, e = 0%
        ///  [4]: 40% > a >= 20%, e = 0%
        ///  [5]: 20% > a > 0%, e = 0%
        ///  [6]: a = 0%
        ///  [7]: a = 100%, e > 0%
        ///  [8]: 100% > a >= 80%, e > 0%
        ///  [9]: 80% > a >= 60%, e > 0%
        ///  [10]: 60% > a >= 40%, e > 0%
        ///  [11]: 40% > a >= 20%, e > 0%
        ///  [12]: 20% > a > 0%, e > 0%
        /// </summary>
        public int[] Percentiles
        {
            get
            {
                int[] percentiles = new int[14];
                for (int i = 0; i < percentiles.Length; i++)
                    percentiles[i] = 0;

                foreach (Guid id in _ShapeIDs)
                {
                    if (_ShapeInkMatchPercentage[id] == 1.0)
                    {
                        if (_ShapeInkExtraPercentage[id] == 0.0)
                            percentiles[0]++;
                        else if (_ShapeInkExtraPercentage[id] > 0.0)
                            percentiles[7]++;
                        else
                            percentiles[13]++;
                    }
                    else if (_ShapeInkMatchPercentage[id] < 1.0 && _ShapeInkMatchPercentage[id] >= 0.8)
                    {
                        if (_ShapeInkExtraPercentage[id] == 0.0)
                            percentiles[1]++;
                        else if (_ShapeInkExtraPercentage[id] > 0.0)
                            percentiles[8]++;
                        else
                            percentiles[13]++;
                    }
                    else if (_ShapeInkMatchPercentage[id] < 0.8 && _ShapeInkMatchPercentage[id] >= 0.6)
                    {
                        if (_ShapeInkExtraPercentage[id] == 0.0)
                            percentiles[2]++;
                        else if (_ShapeInkExtraPercentage[id] > 0.0)
                            percentiles[9]++;
                        else
                            percentiles[13]++;
                    }
                    else if (_ShapeInkMatchPercentage[id] < 0.6 && _ShapeInkMatchPercentage[id] >= 0.4)
                    {
                        if (_ShapeInkExtraPercentage[id] == 0.0)
                            percentiles[3]++;
                        else if (_ShapeInkExtraPercentage[id] > 0.0)
                            percentiles[10]++;
                        else
                            percentiles[13]++;
                    }
                    else if (_ShapeInkMatchPercentage[id] < 0.4 && _ShapeInkMatchPercentage[id] >= 0.2)
                    {
                        if (_ShapeInkExtraPercentage[id] == 0.0)
                            percentiles[4]++;
                        else if (_ShapeInkExtraPercentage[id] > 0.0)
                            percentiles[11]++;
                        else
                            percentiles[13]++;
                    }
                    else if (_ShapeInkMatchPercentage[id] < 0.2 && _ShapeInkMatchPercentage[id] > 0.0)
                    {
                        if (_ShapeInkExtraPercentage[id] == 0.0)
                            percentiles[5]++;
                        else if (_ShapeInkExtraPercentage[id] > 0.0)
                            percentiles[12]++;
                        else
                            percentiles[13]++;
                    }
                    else if (_ShapeInkMatchPercentage[id] == 0.0)
                    {
                        percentiles[6]++;
                    }
                    else
                        percentiles[13]++;
                }

                return percentiles;
            }
        }

        /// <summary>
        /// Gets the number of machine clusters that fall in different categories
        /// categories:
        ///   [0]: Number of completely matching clusters.
        ///   [1]: Number of machine clusters containing all of one hand cluster, but has extra ink.
        ///   [2]: Number of machine clusters containing some ink of a hand cluster, NO extra ink.
        ///   [3]: Number of machine clusters containing some ink of a hand cluster, has extra ink.
        ///   [4]: Number of machine clusters containing some ink of a hand cluster, but is not the best matching for that hand cluster.
        ///   [5]: Number of machine clusters containing ink that doesn't fit any hand cluster ink.
        ///   [6]: Number of times the machine clusters containing partial ink can be combined to get a complete hand cluster.
        /// </summary>
        public int[] MachineClusterCategories
        {
            get
            {
                int[] categories = new int[7];

                categories[0] = _MatchingCompleteMachineClusters.Count;
                categories[1] = _MatchingCompleteExtraMachineClusters.Count;
                categories[2] = _MatchingPartialMissingMachineClusters.Count;
                categories[3] = _MatchingPartialExtraMachineClusters.Count;
                categories[4] = _UnMatchingPartialMachineClusters.Count;
                categories[5] = _UnMatchingCompleteMachineClusters.Count;
                categories[6] = _CompleteablePartialClusters;

                return categories;
            }
        }
        #endregion
    }
}
