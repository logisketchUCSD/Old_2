/**
 * File: Wire.cs
 *
 * Authors: Matthew Weiner, Howard Chen, and Sam Gordon
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2007.
 * 
 */

using System;
using System.Collections;
using Sketch;
using NeuralNets;
using System.Collections.Generic;
using Featurefy;

namespace CircuitRec
{
	/// <summary>
	/// The Wire Class defines the Wire object, which can be used
    /// in a circuit to connect symbols, labels, and other wires.
    /// TODO: Neural net features only take the first two endpoints, need to use
    /// general features.  Endpoint algorithm needs to be improved.
	/// </summary>
	public class Wire : BaseWire
	{
        #region Internals

        /// <summary>
        /// The label associated with the Wire.  Null if there is no label associated.
        /// </summary>
        public Shape AssociatedLabel;

        /// <summary>
        /// An array of the endpoints of the wire.
        /// </summary>
        public Sketch.EndPoint[] EndPt;

        /// <summary>
        /// Says whether or not an endpoint is connected.  True for connected, false for unconnected (the index corresponds with EndPt's index).
        /// This is only for connections to symbols and wires, not to labels.
        /// </summary>
        public bool[] EndPtConnect;

        /// <summary>
        /// Says whether the endpoint is connected to a symbol, wire, or label.  Has the values "symbol", "wire", and "label".
        /// </summary>
        public string[] EpToSymbOrWire;

        /// <summary>
        /// A List of the wires directly connected to the Wire.
        /// </summary>
        public List<Wire> ConnectedWires;

        /// <summary>
        /// An array that has the number of connections to each endpoint (index correspond to the index of EndPt)
        /// </summary>
        internal int[] NumEndPtCon;

        /// <summary>
        /// Holds the temporary number of connections to the endpoints of the Wire.
        /// </summary>
        internal int[] TempWireCons;

        // Used for global IO detection

        /// <summary>
        /// If the Wire is connected to the input of a symbol (itself or any wire in its mesh).
        /// </summary>
        internal bool ConnToInp;

        /// <summary>
        /// If the Wire is connected to the output of a symbol (itself or any wire in its mesh).
        /// </summary>
        internal bool ConnToOut;

        #region Constants

        /// <summary>
        /// The threshold value for determining wire connections to other wires.  It is half of the length of the bounding box used.
        /// </summary>
        private double SUBSTROKE_MARGIN;

        /// <summary>
        /// The amount to increase the bounding box by when finding endpoints.
        /// </summary>
        private const int INCREMENT_MARGIN = 1;

        /// <summary>
        /// The maximum number of iterations for the first part of the endpoint algorithm (corresponds to how large the bounding boxes
        /// are in inkspace).
        /// </summary>
        private const int PREPROCESS_THRESHOLD = 100;

        /// <summary>
        /// The maximum angular difference in degrees for the first part of the endpoint algorithm (Not currently used).
        /// </summary>
        private const double ANGLE_THRESHOLD = 30;

        /// <summary>
        /// The maximum number of iterations to use in the second part of endpoint algorithm (corresponds to how large the bounding boxes
        /// are in inkspace).
        /// </summary>
        private const int MAX_ITERATIONS = 100;

        /// <summary>
        /// The number of points to skip when comparing the distance of the endpoint's substroke to the intersecting points.
        /// </summary>
        private const int COARSENESS = 1;

        /// <summary>
        /// The percentage of the minimum distance from a substroke to intersecting points to compare to an endpoint's distance.
        /// </summary>
        private const double PERCENT_OF_MINDIST = 0.7;

        /// <summary>
        /// Threshold for how close an endpoint can be to another wire to be elminated.
        /// </summary>
        private const int ENDPOINT_DIST_THRESHOLD = 40;

        /// <summary>
        /// The number of points to computer the curvature of when determining if there are hooks or not.
        /// </summary>
        private const int HOOK_NUMBER_OF_PTS = 10;

        #endregion

        // Other members

        /// <summary>
        /// GUID of the BasicWire.
        /// </summary>
        private Guid? id;

        /// <summary>
        /// Substroke that makes up the wire
        /// </summary>
        private Substroke substroke;


        #region Features

        /// <summary>
        /// Feature of  neural net.  Distance of first endpoint to nearest symbol point.
        /// </summary>
        private double p1MinDistSymb;

        /// <summary>
        /// Feature of neural net.  Distance of second endpoint to nearest symbol point.
        /// </summary>
        private double p2MinDistSymb;

        /// <summary>
        /// Feature of neural net.  Closest distance of any wire point to any label point.
        /// </summary>
        private double minDistLabel;

        /// <summary>
        /// Feature of neural net.  Distance of first endpoint to nearest wire point.
        /// </summary>
        private double p1MinDistWire;

        /// <summary>
        /// Feature of neural net.  Distance of second endpoint to nearest wire point.
        /// </summary>
        private double p2MinDistWire;

        /// <summary>
        /// Feature of neural net.  Distance of first endpoint to right perimeter line.
        /// </summary>
        private double p1ToRightPerim;

        /// <summary>
        /// Feature of neural net.  Distance of first endpoint to left perimeter line.
        /// </summary>
        private double p1ToLeftPerim;

        /// <summary>
        /// Feature of neural net.  Distance of first endpoint to top perimeter line.
        /// </summary>
        private double p1ToTopPerim;

        /// <summary>
        /// Feature of neural net.  Distance of first endpoint to bottom perimeter line.
        /// </summary>
        private double p1ToBotPerim;

        /// <summary>
        /// Feature of neural net.  Distance of second endpoint to right perimeter line.
        /// </summary>
        private double p2ToRightPerim;

        /// <summary>
        /// Feature of neural net.  Distance of second endpoint to left perimeter line.
        /// </summary>
        private double p2ToLeftPerim;

        /// <summary>
        /// Feature of neural net.  Distance of second endpoint to top perimeter line.
        /// </summary>
        private double p2ToTopPerim;

        /// <summary>
        /// Feature of neural net.  Distance of second endpoint to bottom perimeter line.
        /// </summary>
        private double p2ToBotPerim;
        #endregion

        #endregion

        #region Constructor

        /// <summary>
       /// Constructor for Wire.
       /// </summary>
       /// <param name="newWire">The Shape object containing the Wire</param>
       /// <param name="marginvalue">The value of the margin for the wire</param>
       /// <param name="substroke_margin">The value of the margin for the substroke</param>

        public Wire(Substroke newWire, double substroke_margin)
        {
            // Initialize variables
            errors = new List<ParseError>();
            ConnectedWires = new List<Wire>();
            AllConnectedWires = new List<Wire>();
            id = newWire.XmlAttrs.Id;
            substroke = newWire;
            this.SUBSTROKE_MARGIN = substroke_margin;

            // Find endpoints and determines their attributes
            this.EndPt = newWire.Endpoints; 
            if (EndPt.Length < 2)
                EndPt = findAllEndPoints().ToArray();
            

            // Initialize these to the length of the endpoint array
            TempWireCons = new int[EndPt.Length];
            NumEndPtCon = new int[EndPt.Length];
            EndPtConnect = new bool[EndPt.Length];
            EpToSymbOrWire = new string[EndPt.Length];
            ConnToInp = false;
            ConnToOut = false;
        }

        /// <summary>
        /// Constructor for Wire.
        /// </summary>
        public Wire()
        {
        }

        #endregion
        
        #region Methods

        /// <summary>
        /// This function returns the probability that a wire is actually a wire
        /// </summary>
        /// <returns></returns>
        protected float likelinessWire()
        {
            //The base belief is just the average belief that each substroke is a wire
            float basebelief = 0;
            if (substroke.Classification == "Wire")
                    basebelief = 1.0f;


            List<System.Drawing.PointF> pointfs = new List<System.Drawing.PointF>();
            foreach (Point p in this.substroke.PointsL)
            {
                pointfs.Add(p.SysDrawPointF);
            }
            //Calculate circleness (wires should not be circlelike)
            double x0, y0, r;
            double circleError = Featurefy.LeastSquares.leastSquaresCircleFit(pointfs.ToArray(), out x0, out y0, out r);
            float circleness = 0f;
            if (circleError.Equals(0))
                circleness = 1f;

            // Compare wire with its least squares line
            double m, b;
            double lineError = Featurefy.LeastSquares.leastSquaresLineFit(pointfs.ToArray(), out m, out b);
            float lineness = 0f;
            if (lineError.Equals(0))
                lineness = 1f;


            return basebelief;// +lineness - circleness;
        }
     
        #region Endpoint Algorithms

        /// <summary>
        /// Remove all hooks from substrokes and update the strokes and substrokes
        /// Uses the dehooking code in Featurefy.Compute (Eric's code)
        /// </summary>
        private void removeHooks()
        {
            Substroke temp = Featurefy.Compute.DeHook(substroke);
            substroke = temp;
        }

        /// <summary>
        /// Finds all the endpoints of each substroke in the Wire.
        /// </summary>
        /// <returns>A List of all the possible endpoints, or both endpoints from each substroke in the wire.</returns>
        private List<Sketch.EndPoint> findAllEndPoints()
        {
            // The start and end points of the substroke are always the first and last points in the PointsL List,
            // so add these to the possible endpoints for all the substrokes in the Wire.
            List<Sketch.EndPoint> epts = new List<Sketch.EndPoint>();

            // Remove hooks in each substroke in the Wire.
            removeHooks();

            // The endpoints are the first and last points in the list of points of each substroke
            epts.Add(new Sketch.EndPoint(substroke.PointsL[0], substroke));
            epts.Add(new Sketch.EndPoint(substroke.PointsL[substroke.PointsL.Count - 1], substroke));


            // Determine the local slope and type (ie left, right, etc) of each endpoint
            foreach (Sketch.EndPoint ep in epts)
            {
                ep.DetermineSlope(ep.ParentSub);
            }

            
            // Finds the type of each endpoint
            foreach (Sketch.EndPoint ep in epts)
            {
                foreach (Sketch.EndPoint ep2 in epts)
                {
                    // Find the type of the endpoint
                    if (ep.ParentSub.XmlAttrs.Id.Equals(ep2.ParentSub.XmlAttrs.Id) && !ep.Equals(ep2))
                    {
                        ep.DetermineType(ep2);
                    }
                }
            }
            return epts;
        }

 
        /*
        /// <summary>
        /// Finds the two final endpoints of the Wire Object (actually returns only 2 now)
        /// TODO:  See if this function actually does anything in the first half
        /// </summary>
        /// <param name="endpoints">The list of all the possible endpoints</param>
        /// <param name="increased">Whether or not the margin was increased on the previous pass (0 or 1)</param>
        private EndPoint[] findEndPoints(List<EndPoint> endpoints)
        {
            // Return an error if there is less than two endpoints found from findAllEndpoints
            if (endpoints.Count < 2)
            {
                errors.Add(new ParseError(this, "Two endpoints could not be found for a wire."));
                return new EndPoint[1];
            }

            // Hold the endpoints from the previous pass in case the number of endpoints becomes smaller than two.
            List<EndPoint> previousPass = new List<EndPoint>();
            Dictionary<EndPoint, List<Guid?>> previousDict = new Dictionary<EndPoint, List<Guid?>>();

            // IMPORTANT: Dictionary maps endpoints to parent substrokes of endpoints that the endpoint has combined 
            // from the preprocessEndpoints algorithm so that in the combineIfNearWire algorithm the endpoint cannot be
            // eliminated by points in that substroke.
            Dictionary<EndPoint, List<Guid?>> endpoint2CombinedSubId = new Dictionary<EndPoint, List<Guid?>>();

            foreach (EndPoint ep in endpoints)
            {
                endpoint2CombinedSubId.Add(ep, new List<Guid?>());
            }

            // Deal with overstroking of parallel lines.  Combines and eliminates endpoints based on their distance from
            // each other and their types.
            // Stop if there are two endpoints, run the preprocessing
            // algorithm if there are more than two endpoints, and if there are less than two endpoints, take the
            // previous list of endpoints (with more than two endpoints) and go on to the next algorithm
            int numIterations = 0;
            while (numIterations < PREPROCESS_THRESHOLD)
            {
                if (endpoints.Count == 2)
                    break;
                else if (endpoints.Count > 2)
                {
                    previousPass = new List<EndPoint>(endpoints);
                    endpoints = preprocessEndpoints(endpoints, ref endpoint2CombinedSubId, numIterations);
                }
                else
                {
                    endpoints = previousPass;
                    break;
                }

                numIterations++;
            }


            // increased: false if it is the first pass or true if the margin was increased in the last pass
            bool increased = false;

            numIterations = 0;
            previousPass = new List<EndPoint>();

            // Eliminates endpoints that are close to points from other substrokes in the same wire
            while (numIterations < MAX_ITERATIONS)
            {
                if (endpoints.Count == 2)
                {
                    break;
                }
                else if (endpoints.Count > 2)
                {
                    if (!increased)
                        increased = true;
                    else
                        SUBSTROKE_MARGIN += (INCREMENT_MARGIN);

                    previousPass = new List<EndPoint>(endpoints);
                    endpoints = combineIfNearWire(endpoints, ref endpoint2CombinedSubId);
                }
                // Use the endpoints from the previous pass and decrease the size of the bounding box by half the
                // amount to try to not eliminate too many endpoints.  Do not store previousPass since may have to
                // decrease bounding box size again
                else //(endpoints.Count < 2)
                {
                    SUBSTROKE_MARGIN -= (INCREMENT_MARGIN / 2);
                    endpoints = combineIfNearWire(previousPass, ref previousDict);
                }

                numIterations++;
            }
         
           //Strip internal endpoints
            List<EndPoint> finalendpoints = new List<EndPoint>();
            foreach (EndPoint point in endpoints)
            {
                if (!point.InternalEndPoint)
                    finalendpoints.Add(point);
            }
            endpoints = finalendpoints;

            // Create and load the surviving endpoints into the endpoint array
            int i = 0;
            EndPoint[] endPt = new EndPoint[endpoints.Count];
            foreach (EndPoint point in endpoints)
            {
                endPt[i] = point;
                i++;
            }
            return endPt;

        }
        */

        /*
        #region Endpoint Algorithm Helper Functions

        /// <summary>
        /// Determines if an endpoint is an internal or external endpoint of a substroke
        /// </summary>
        private void DetermineInternal(EndPoint ep)
        {
            // The maximum distance two points can be apart and still be considered the same point
            int SAMEPOINTTHRESHOLD = 50;
            ep.InternalEndPoint = false;
            foreach (Substroke sub in substrokes)
            {
                if (sub.Equals(ep.ParentSub))
                    continue;
                Point e1 = sub.Points[0];
                if (Math.Abs(e1.X - ep.X)<SAMEPOINTTHRESHOLD && Math.Abs(e1.Y - ep.Y)<SAMEPOINTTHRESHOLD)
                    ep.InternalEndPoint = true;
                Point e2 = sub.Points[sub.PointsL.Count - 1];
                if (Math.Abs(e2.X - ep.X) < SAMEPOINTTHRESHOLD && Math.Abs(e2.Y - ep.Y) < SAMEPOINTTHRESHOLD)
                    ep.InternalEndPoint = true;
            }
        }



        /// <summary>
        /// Eliminates endpoints that are parallel to other substrokes and within a distance threshold.  This is
        /// to deal with overstroking.
        /// </summary>
        /// <param name="endpoints">The current list of endpoints.</param>
        /// <param name="endpoint2CombinedSubId">Dictionary mapping endpoints to the substrokes of the endpoints they have combined with.</param>
        /// <param name="threshold">Distance under which endpoint types should be compared.</param>
        /// <returns>The new list of endpoints.</returns>
        private List<EndPoint> preprocessEndpoints(List<EndPoint> endpoints, ref Dictionary<EndPoint, List<Guid?>> endpoint2CombinedSubId, int threshold)
        {
            List<EndPoint> toRemove = new List<EndPoint>();

            // Compare all the remaining endpoints to all of the other remaining endpoints in the wire
            foreach (EndPoint firstEp in endpoints)
            {
                foreach (EndPoint secondEp in endpoints)
                {
                    // Check if the endpoints are close enough, if they endpoints have not already been affected by the algorithm, and
                    // if the endpoints are not on the same substroke.  TODO: add in the code to deal with endpoints that are on the same
                    // substroke
                    if (Dist(firstEp, secondEp) < threshold && !firstEp.ParentSub.XmlAttrs.Id.Equals(secondEp.ParentSub.XmlAttrs.Id) &&
                        !toRemove.Contains(firstEp) && !toRemove.Contains(secondEp))
                    {
                        // Check if the endpoints have the same type (left and left, left and topleft, etc.) (Commented out part checks
                        // that the angle between the two endpoints is under a certain threshold -- NOT TESTED)
                        //if (Math.Abs((Math.Atan2(firstEp.Slope,1)*180/Math.PI) - (Math.Atan2(secondEp.Slope,1)*180/Math.PI)) < ANGLE_THRESHOLD)
                        if ((firstEp.Type.Contains("left") && secondEp.Type.Contains("left")) ||
                            (firstEp.Type.Contains("right") && secondEp.Type.Contains("right")) ||
                            (firstEp.Type.Contains("top") && secondEp.Type.Contains("top")) ||
                            (firstEp.Type.Contains("bottom") && secondEp.Type.Contains("bottom")))
                        {
                            switch (firstEp.Type)
                            {
                                // Combine or eliminate based on the type then add the substroke GUID that the endpoint eliminates to a
                                // dictionary mapping the endpoint to the substroke GUIDs that the endpoint eliminates
                                case "left":
                                    if (firstEp.X < secondEp.X && !toRemove.Contains(secondEp))
                                    {
                                        toRemove.Add(secondEp);
                                        endpoint2CombinedSubId[firstEp].Add(secondEp.ParentSub.XmlAttrs.Id);
                                    }
                                    else if (!toRemove.Contains(firstEp))
                                    {
                                        toRemove.Add(firstEp);
                                        endpoint2CombinedSubId[secondEp].Add(firstEp.ParentSub.XmlAttrs.Id);
                                    }
                                    break;
                                case "right":
                                    if (firstEp.X > secondEp.X && !toRemove.Contains(secondEp))
                                    {
                                        toRemove.Add(secondEp);
                                        endpoint2CombinedSubId[firstEp].Add(secondEp.ParentSub.XmlAttrs.Id);
                                    }
                                    else if (!toRemove.Contains(firstEp))
                                    {
                                        toRemove.Add(firstEp);
                                        endpoint2CombinedSubId[secondEp].Add(firstEp.ParentSub.XmlAttrs.Id);
                                    }
                                    break;
                                case "top":
                                    if (firstEp.Y < secondEp.Y && !toRemove.Contains(secondEp))
                                    {
                                        toRemove.Add(secondEp);
                                        endpoint2CombinedSubId[firstEp].Add(secondEp.ParentSub.XmlAttrs.Id);
                                    }
                                    else if (!toRemove.Contains(firstEp))
                                    {
                                        toRemove.Add(firstEp);
                                        endpoint2CombinedSubId[secondEp].Add(firstEp.ParentSub.XmlAttrs.Id);
                                    }
                                    break;
                                case "bottom":
                                    if (firstEp.Y > secondEp.Y && !toRemove.Contains(secondEp))
                                    {
                                        toRemove.Add(secondEp);
                                        endpoint2CombinedSubId[firstEp].Add(secondEp.ParentSub.XmlAttrs.Id);
                                    }
                                    else if (!toRemove.Contains(firstEp))
                                    {
                                        toRemove.Add(firstEp);
                                        endpoint2CombinedSubId[secondEp].Add(firstEp.ParentSub.XmlAttrs.Id);
                                    }
                                    break;
                                case "topleft":
                                    if ((firstEp.X < secondEp.X || firstEp.Y < secondEp.Y) && !toRemove.Contains(secondEp))
                                    {
                                        toRemove.Add(secondEp);
                                        endpoint2CombinedSubId[firstEp].Add(secondEp.ParentSub.XmlAttrs.Id);
                                    }
                                    else if (!toRemove.Contains(firstEp))
                                    {
                                        toRemove.Add(firstEp);
                                        endpoint2CombinedSubId[secondEp].Add(firstEp.ParentSub.XmlAttrs.Id);
                                    }
                                    break;
                                case "topright":
                                    if ((firstEp.X > secondEp.X || firstEp.Y < secondEp.Y) && !toRemove.Contains(secondEp))
                                    {
                                        toRemove.Add(secondEp);
                                        endpoint2CombinedSubId[firstEp].Add(secondEp.ParentSub.XmlAttrs.Id);
                                    }
                                    else if (!toRemove.Contains(firstEp))
                                    {
                                        toRemove.Add(firstEp);
                                        endpoint2CombinedSubId[secondEp].Add(firstEp.ParentSub.XmlAttrs.Id);
                                    }
                                    break;
                                case "bottomright":
                                    if ((firstEp.X > secondEp.X || firstEp.Y > secondEp.Y) && !toRemove.Contains(secondEp))
                                    {
                                        toRemove.Add(secondEp);
                                        endpoint2CombinedSubId[firstEp].Add(secondEp.ParentSub.XmlAttrs.Id);
                                    }
                                    else if (!toRemove.Contains(firstEp))
                                    {
                                        toRemove.Add(firstEp);
                                        endpoint2CombinedSubId[secondEp].Add(firstEp.ParentSub.XmlAttrs.Id);
                                    }
                                    break;
                                case "bottomleft":
                                    if ((firstEp.X < secondEp.X || firstEp.Y > secondEp.Y) && !toRemove.Contains(secondEp))
                                    {
                                        toRemove.Add(secondEp);
                                        endpoint2CombinedSubId[firstEp].Add(secondEp.ParentSub.XmlAttrs.Id);
                                    }
                                    else if (!toRemove.Contains(firstEp))
                                    {
                                        toRemove.Add(firstEp);
                                        endpoint2CombinedSubId[secondEp].Add(firstEp.ParentSub.XmlAttrs.Id);
                                    }
                                    break;
                                default:
                                    toRemove.Add(secondEp);
                                    endpoint2CombinedSubId[firstEp].Add(secondEp.ParentSub.XmlAttrs.Id);
                                    break;

                            }
                        }

                        // If the types do not match, then eliminate both endpoints
                        else
                        {
                            if (!toRemove.Contains(firstEp))
                                toRemove.Add(firstEp);
                            if (!toRemove.Contains(secondEp))
                                toRemove.Add(secondEp);
                        }
                    }
                }
            }

            // Remove the endpoints that were eliminated
            foreach (EndPoint ep in toRemove)
            {
                endpoints.Remove(ep);
            }

            return endpoints;
        }
        
        /// <summary>
        /// Eliminates endpoints that are near other substrokes in the same wire (near means relatively closer than the other
        /// points in the same substroke).  The dictionary maps an endpoint to the substroke GUIDs that the endpoint eliminated
        /// in preprocessEndpoints, and the endpoint cannot be eliminated by points in a substroke that are mapped to that endpoint.
        /// </summary>
        /// <param name="pointlist">Current list of the endpoints.</param>
        /// <param name="endpoint2CombinedSubstroke">Dictionary mapping the endpoint to the substroke GUIDs that the endpoint eliminated
        /// in preprocessEndpoints.</param>
        /// <returns>New list of the endpoints.</returns>
        //private List<EndPoint> combineIfNearWire(List<EndPoint> pointlist, ref Dictionary<EndPoint, List<Guid?>> endpoint2CombinedSubstroke)
        //{
        //    List<EndPoint> toRemove = new List<EndPoint>();

        //    foreach (EndPoint ep in pointlist)
        //    {
        //        foreach (Substroke sub in this.Substrokes)
        //        {
        //            // Checks if the endpoint did not eliminate an endpoint from the current substroke
        //            if (!endpoint2CombinedSubstroke[ep].Contains(sub.XmlAttrs.Id))
        //            {
        //                List<Point> intersectList = new List<Point>();

        //                // Checks if the endpoint is near points on the current substroke
        //                if (intersectCount(ep.X - SUBSTROKE_MARGIN, ep.Y - SUBSTROKE_MARGIN, ep.X + SUBSTROKE_MARGIN,
        //                    ep.Y + SUBSTROKE_MARGIN, sub.PointsL, out intersectList) > 0 && !ep.ParentSub.XmlAttrs.Id.Equals(sub.XmlAttrs.Id)
        //                    && !toRemove.Contains(ep))
        //                {
        //                    // Checks if the endpoint is closer to points in the current substroke that intersect the bounding box than other
        //                    // points in the endpoint's substroke
        //                    if (checkIfClosest(ep, intersectList, COARSENESS))
        //                        toRemove.Add(ep);
        //                }
        //            }
        //        }
        //    }

        //    // Make a new list to avoid shallow copy issues
        //    List<EndPoint> pointlist_remain = new List<EndPoint>(pointlist);
        //    foreach (EndPoint ep in toRemove)
        //    {
        //        pointlist_remain.Remove(ep);
        //    }

        //    return pointlist_remain;
        //}


        /// <summary>
        /// Checks if the endpoint is the closest point within the same substroke to the points from other substrokes that
        /// fall within the bounding box
        /// </summary>
        /// <param name="ep">The current endpoint.</param>
        /// <param name="intersectList">The list of points that intersect the endpoint's bounding box.</param>
        /// <returns>True if the endpoint is the closest point in its substroke to the intersecting points.</returns>
        private bool checkIfClosest(EndPoint ep, List<Point> intersectList, int coarseness)
        {
            // Find the distance from the endpoint to the closest point in intersectList
            double minEndpointDist = Double.PositiveInfinity;
            foreach (Point p in intersectList)
            {
                double localDist = Dist(ep, p);
                if (localDist < minEndpointDist)
                    minEndpointDist = localDist;
            }

            // Find the distance from the other points in the endpoint's substroke to the points that intersected the bounding
            // box with a chosen coarseness
            double minSameSubstrokeDist = Double.PositiveInfinity;
            for (int i = 0; i < ep.ParentSub.PointsL.Count; i += coarseness)
            {
                if (ep.ParentSub.PointsL[i].X != ep.X && ep.ParentSub.PointsL[i].Y != ep.Y)
                {
                    foreach (Point p in intersectList)
                    {
                        double localDist = Dist(ep.ParentSub.PointsL[i], p);
                        if (localDist < minSameSubstrokeDist)
                            minSameSubstrokeDist = localDist;

                    }
                }
            }

            // Checks if the endpoint is closer to the intersecting points than the other points in the endpoint's substroke
            // (gives a 30% margin of error for the endpoint distance and also allows the endpoint to be within some absolute distance
            // of the intersected points if the substrokes intersect and the endpoint is drawn past the intersection)
            if ((minEndpointDist * PERCENT_OF_MINDIST) < minSameSubstrokeDist || Math.Abs(minEndpointDist-minSameSubstrokeDist) < ENDPOINT_DIST_THRESHOLD)
                return true;

            return false;
        }

        #endregion

        */

        #endregion

        #region Neural Network Functions

        /// <summary>
        /// Extracts features from the wire to be used by the Neural Net
        /// </summary>
        /// <param name="wires">A list of all the Wires in the circuit</param>
        /// <param name="circuit">The Circuit object</param>
        /// <param name="symbols">A list of all the Symbols in the circuit</param>
        /// <param name="labels">A list of all the Labels in the circuit</param>
        internal void FeatureExtract(List<Wire> wires, BoundingBox circuit, List<BaseSymbol> symbols, List<Shape> labels)
        {
            // Find the distance of the wire endpoints from the perimeter lines
            this.distWireToPerim(circuit);

            // Find the distance of the wire endpoints from the from the bounding lines for each
            // symbol, and choose the smallest value
            this.distWireToSymbol(symbols);

            // Find the closest distance to other wires; check the distance of both ends of the wire
            // to the ends of all other wires and three intermediate points in each wire
            this.distWireToWire(wires);

            // Find the closest distance to labels; check the distance of every point of the
            // wire and label
            this.distWireToLabel(labels);
        }

        /// <summary>
        /// Normalizes the features before use by the Neural Net.  Returns an List since the neural net
        /// takes one as an input.
        /// </summary>
        /// <param name="maxdistwire">The maximum distance from the Wire to another Wire</param>
        /// <param name="maxdistsymb">The maximum distance from the Wire to a Symbol</param>
        /// <param name="maxdistlabel">The maximum distance from the Wie to a Label</param>
        /// <param name="circuitwidth">The width of the entire circuit</param>
        /// <param name="circuitheight">The height of the entire circuit</param>
        /// <param name="labelcount">The number of Labels associated with the circuit</param>
        /// <returns>An ArrayList of all the normalized features</returns>
        internal ArrayList Normalize(double maxdistwire, double maxdistsymb, double maxdistlabel, double circuitwidth,
                                     double circuitheight, int labelcount)
        {
            // If there are no labels, we do not want the feature to be NaN, so set to 0.5.
            if (labelcount == 0)
                this.minDistLabel = .5;
            else
                this.minDistLabel = this.minDistLabel / maxdistlabel;
            
            // Normalize all the features
            this.p1MinDistSymb = this.p1MinDistSymb / maxdistsymb;
            this.p1MinDistWire = this.p1MinDistWire / maxdistwire;
            this.p2MinDistSymb = this.p2MinDistSymb / maxdistsymb;
            this.p2MinDistWire = this.p2MinDistWire / maxdistwire;

            this.p1ToBotPerim = this.p1ToBotPerim / circuitheight;
            this.p1ToLeftPerim = this.p1ToLeftPerim / circuitwidth;
            this.p1ToRightPerim = this.p1ToRightPerim / circuitwidth;
            this.p1ToTopPerim = this.p1ToTopPerim / circuitheight;

            this.p2ToBotPerim = this.p2ToBotPerim / circuitheight;
            this.p2ToLeftPerim = this.p2ToLeftPerim / circuitwidth;
            this.p2ToRightPerim = this.p2ToRightPerim / circuitwidth;
            this.p2ToTopPerim = this.p2ToTopPerim / circuitheight;

            // Set the input array to the neural net, and the order that the features are written in dependent on the time
            // of the endpoints (that was how the neural net was trained)
            double[] input = new double[13];
            if (this.EndPt[0].Time >= this.EndPt[1].Time)
            {
                input = new double[]{this.p1ToLeftPerim,
			                         this.p1ToRightPerim,this.p1ToTopPerim,this.p1ToBotPerim,this.p2ToLeftPerim,
								     this.p2ToRightPerim,this.p2ToTopPerim,this.p2ToBotPerim,
								     this.p1MinDistSymb,this.p1MinDistWire,this.p2MinDistSymb,
								     this.p2MinDistWire,this.minDistLabel};
              }
            else
            {
                input = new double[]{this.p2ToLeftPerim,
			                         this.p2ToRightPerim,this.p2ToTopPerim,this.p2ToBotPerim,this.p1ToLeftPerim,
								     this.p1ToRightPerim,this.p1ToTopPerim,this.p1ToBotPerim,
								     this.p2MinDistSymb,this.p2MinDistWire,this.p1MinDistSymb,
								     this.p1MinDistWire,this.minDistLabel};
            }

            // The output is an ArrayList since the Neural Net takes an ArrayList as an input
            ArrayList inputAL = new ArrayList(input);
            return inputAL;
        }

        /// <summary>
        /// Runs the normalized data through the back propagation network.
        /// </summary>
        /// <param name="inputAL">The features to be used by the network</param>
        /// <param name="bpNetwork">The backprop network</param>
        /// <returns>If the Wire is an input, an output, or an internal wire</returns>
        internal static WirePolarity SetIO(ArrayList inputAL, BackProp bpNetwork)
        {
            // Run normalized data through backpropagation network
            double[] outputs = bpNetwork.Run(inputAL);

            // Based on the output of the neural net, it selects 1 if the output corresponding to an 
            // input wire is closer to one than the other two possibilities.  If it is not, then it 
            // selects 2 if the output corresponding to an output wire is closer to one than
            // the internal wire possibility, and if both of these conditions are false
            // it chooses 3 for internal wire
            int inpdist = (int)Math.Round(outputs[0]);
            int outdist = (int)Math.Round(outputs[1]);
            int intdist = (int)Math.Round(outputs[2]);

            WirePolarity output;
            if (inpdist == 1 && outdist != 1 && intdist != 1)
                output = WirePolarity.Input;
            else if (inpdist != 1 && outdist == 1 && intdist != 1)
                output = WirePolarity.Output;
            else if (inpdist != 1 && outdist != 1 && intdist == 1)
                output = WirePolarity.Internal;
            else
                output = WirePolarity.Unknown;

            return output;
        }
        
        /// <summary>
        /// Finds the normalization parameters for the I/O determination
        /// </summary>
        /// <param name="wires">The list of Wires in the circuit</param>
        /// <param name="circuit">The entire Circuit Object</param>
        /// <returns>The normalization parameters</returns>
        internal static double[] NormParams(List<Wire> wires, BoundingBox circuit, out List<Double> maxwiredists)
        {
            // The normalization parameters for the perimeter features is the width and height of the circuit.
            double circuitwidth = circuit.BottomRightX - circuit.TopLeftX;
            double circuitheight = circuit.BottomRightY - circuit.TopLeftY;

            // The normailzation parameters for the other features are the maximum distance from an endpoint
            // to a symbol, to a wire, or from any point to a label
            double maxdistsymb = Double.NegativeInfinity;
            double maxdistwire = Double.NegativeInfinity;
            double maxdistlabel = Double.NegativeInfinity;

            List<Double> forThresh = new List<Double>();
            foreach (Wire tempwire in wires)
            {
                double tempmaxdist = Math.Max(tempwire.p1MinDistWire, tempwire.p2MinDistWire);
                forThresh.Add(tempmaxdist);
                maxdistwire = Math.Max(maxdistwire, tempmaxdist);
                tempmaxdist = Math.Max(tempwire.p1MinDistSymb, tempwire.p2MinDistSymb);
                maxdistsymb = Math.Max(maxdistsymb, tempmaxdist);
                maxdistlabel = Math.Max(maxdistlabel, tempwire.minDistLabel);
            }
            maxwiredists = new List<double>(forThresh);
            double[] dists = new double[] {maxdistwire, maxdistsymb, maxdistlabel,
                                           circuitwidth, circuitheight};
            return dists;
        }

        /// <summary>
        /// Finds the smallest distance from the current wire's endpoints to other wires
        /// </summary>
        /// <param name="wire">The current Wire</param>
        /// <param name="wires">A list of all the Wires</param>
        /// <returns>An array of minimum distances from each endpoint to another Wire</returns>
        private void distWireToWire(List<Wire> wires)
        {
            double sPdist = Double.PositiveInfinity;
            double ePdist = Double.PositiveInfinity;
            // Cycle through each wire except the current one and find the minimum distance
            // from the endpoints to each point in the wire
            foreach (Wire tempwire in wires)
            {
                if (!(this.Name.Equals(tempwire.Name)))
                {
                    foreach (Point p in tempwire.substroke.PointsL)
                    {
                        double tempdistp1 = Dist(p, EndPt[0]);
                        double tempdistp2 = Dist(p, EndPt[1]);
                        sPdist = Math.Min(sPdist, tempdistp1);
                        ePdist = Math.Min(ePdist, tempdistp2);
                    }
                }
            }
            this.p1MinDistWire = sPdist;
            this.p2MinDistWire = ePdist;
        }

        /// <summary>
        /// Finds the smallest distance from the current wire's endpoints from symbols
        /// </summary>
        /// <param name="symbols">A list of all the Symbols in the Circuit</param>
        private void distWireToSymbol(List<BaseSymbol> symbols)
        {
            double sPdist = Double.PositiveInfinity;
            double ePdist = Double.PositiveInfinity;
            // Cycle through each symbol except the current one and find the minimum distance
            // from the endpoints to each point in the symbol
            foreach (BaseSymbol symb in symbols)
            {
                foreach (Point p in symb.Points)
                {
                    double tempdistp1 = Dist(p, EndPt[0]);
                    double tempdistp2 = Dist(p, EndPt[1]);
                    sPdist = Math.Min(sPdist, tempdistp1);
                    ePdist = Math.Min(ePdist, tempdistp2);
                }
            }

            this.p1MinDistSymb = sPdist;
            this.p2MinDistSymb = ePdist;
        }

        /// <summary>
        /// Finds the distance of the current wire's endpoints to the perimeter lines
        /// </summary>
        /// <param name="circuit">The entire Circuit</param>
        private void distWireToPerim(BoundingBox circuit)
        {
            this.p1ToLeftPerim = Math.Abs(this.EndPt[0].X - circuit.TopLeftX);
            this.p1ToRightPerim = Math.Abs(this.EndPt[0].X - circuit.BottomRightX);
            this.p1ToTopPerim = Math.Abs(this.EndPt[0].Y - circuit.TopLeftY);
            this.p1ToBotPerim = Math.Abs(this.EndPt[0].Y - circuit.BottomRightY);

            this.p2ToLeftPerim = Math.Abs(this.EndPt[1].X - circuit.TopLeftX);
            this.p2ToRightPerim = Math.Abs(this.EndPt[1].X - circuit.BottomRightX);
            this.p2ToTopPerim = Math.Abs(this.EndPt[1].Y - circuit.TopLeftY);
            this.p2ToBotPerim = Math.Abs(this.EndPt[1].Y - circuit.BottomRightY);
        }

        /// <summary>
        /// Finds the distance of the current wire's endpoints to any point in a label.
        /// </summary>
        /// <param name="labels">A list of all the Labels in the circuit</param>
        private void distWireToLabel(List<Shape> labels)
        {
            double p1tol = Double.PositiveInfinity;
            double p2tol = Double.PositiveInfinity;

            // Cycle through all the points in all the labels and all the points in the wire.
            foreach (Shape templabel in labels)
            {
                foreach (Point lpoint in templabel.Points)
                {
                    double p1localdist = Dist(lpoint, EndPt[0]);
                    double p2localdist = Dist(lpoint, EndPt[1]);
                    p1tol = Math.Min(p1tol, p1localdist);
                    p2tol = Math.Min(p2tol, p2localdist);
                }

            }
            this.minDistLabel = Math.Min(p1tol, p2tol);
        }

        /// <summary>
        /// Returns the distance between two Points
        /// </summary>
        /// <param name="pt1">The first Point</param>
        /// <param name="pt2">The second Point</param>
        /// <returns>The distance between the two Points.</returns>
        internal static double Dist(Point pt1, Point pt2)
        {
            double dist = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y));
            return dist;
        }

        /// <summary>
        /// Returns the distance between two Points
        /// </summary>
        /// <param name="pt1">The first point as a Point object</param>
        /// <param name="pt2x">The X value of the second point</param>
        /// <param name="pt2y">The Y value of the second point</param>
        /// <returns></returns>
        internal static double Dist(Point pt1, double pt2x, double pt2y)
        {
            double dist = Math.Sqrt((pt1.X - pt2x) * (pt1.X - pt2x) + (pt1.Y - pt2y) * (pt1.Y - pt2y));
            return dist;
        }

        #endregion

        #region ConnectWireToWire

        /// <summary>
        /// Determines the wire to wire connections for each wire in the circuit.  It rotates the coordinates of the
        /// endpoints to a coordinate frame by the negative of the angle of the slope.  A skinny bounding box is
        /// created (the threshold along the major axis and 0.4 of the threshold along the minor axis), and then
        /// all the wires are searched through to see if their rotated coordinates fall in the bounding box.  If they
        /// do, the wire that is closest that intersects the bounding box is connected to the current wire.
        /// </summary>
        /// <param name="wires">A list of all the Wire objects</param>
        /// <param name="MV">The bounding box width/height</param>
        internal void ConnectWireToWire(List<Wire> wires, double MV)
        {
            // Initialize these lists to the appropriate length
            List<bool> epcon = new List<bool>();
            foreach (Sketch.EndPoint ep in this.EndPt)
            {
                epcon.Add(false);
            }
            List<double> minDist = new List<double>();
            foreach (Sketch.EndPoint ep in this.EndPt)
            {
                minDist.Add(Double.PositiveInfinity);               
            }
            List<Wire> epConToWire = new List<Wire>();
            foreach (Sketch.EndPoint ep in this.EndPt)
            {
                epConToWire.Add(new Wire()); 
            }
            List<List<Point>> epIntersectList = new List<List<Point>>();
            foreach (Sketch.EndPoint ep in this.EndPt)
            {
                epIntersectList.Add(new List<Point>());
            }
            
            // Look at each endpoint for connections to other wires
            int index = 0;
            foreach (Sketch.EndPoint ep in this.EndPt)
            {
                // Find the skinny bounding box (rectangular with the long edge aligned with the slope of the endpoint)
                double theta = Math.Atan2(ep.Slope, 1);
                
                // Debug
                if (!this.EndPtConnect[index])
                    { };//Console.WriteLine("\n{0}, Endpoint {1} has a slope of {2}/angle of {3}", this.Name, index, ep.Slope, theta*180/Math.PI);

                // Find rotated coordinates
                double epXPrime = ep.X * Math.Cos(theta) - ep.Y * Math.Sin(theta);
                double epYPrime = ep.X * Math.Sin(theta) + ep.Y * Math.Cos(theta);

                // Of the wires that intersect the bounding box, the one closest to the endpoint is taken as the wire to connect to
                foreach (Wire w in wires)
                {
                    List<Point> intersectpts;

                    if (!w.id.Equals(this.id) && intersectCount(theta, MV, epXPrime, epYPrime, 1, w.substroke.PointsL, out intersectpts) > 0 && !this.EndPtConnect[index])
                    {
                        double localMinDist = Double.PositiveInfinity;
                        Point localClosestPoint = new Point();
                        foreach (Point p in intersectpts)
                        {
                            double localDist = Dist(p, ep);
                            if (localDist < localMinDist)
                            {
                                localMinDist = localDist;
                                localClosestPoint = p;
                            }
                        }

                        if (localMinDist < minDist[index])
                        {
                            minDist[index] = localMinDist;
                            epcon[index] = true;
                            epConToWire[index] = w;
                        }
                    }
                }
                index++;
            }

            // Connects the wires together
            index = 0;
            if (epcon[0] || epcon[1])
            {
                foreach (Sketch.EndPoint ep in this.EndPt)
                {
                    if (epcon[index])
                    {
                        // Set the values for the current end point and wire
                        this.EndPtConnect[index] = true;
                        this.EpToSymbOrWire[index] = "wire";

                        if (!this.ConnectedWires.Contains(epConToWire[index]))
                            this.ConnectedWires.Add(epConToWire[index]);

                        // Set the value for the other wire
                        if (!epConToWire[index].ConnectedWires.Contains(this))
                            epConToWire[index].ConnectedWires.Add(this);

                        // Add to the number of connections for the endpoints
                        this.NumEndPtCon[index]++;
                    }
                    index++;
                }
            }
        }

        /// <summary>
        /// Determines the wire to wire connections for each wire in the circuit.  This function can be used with feedback,
        /// it adjusts the size of the bounding box by a scaling factor
        /// 
        /// It rotates the coordinates of the
        /// endpoints to a coordinate frame by the negative of the angle of the slope.  A skinny bounding box is
        /// created (the threshold along the major axis and 0.4 of the threshold along the minor axis), and then
        /// all the wires are searched through to see if their rotated coordinates fall in the bounding box.  If they
        /// do, the wire that is closest that intersects the bounding box is connected to the current wire.
        /// </summary>
        /// <param name="wires">A list of all the Wire objects</param>
        /// <param name="MV">The bounding box width/height</param>
        /// <param name="feedback">The scaling factor determined from feedback</param>
        internal void ConnectWireToWire(List<Wire> wires, double MV, double feedback)
        {
            // Initialize these lists to the appropriate length
            List<bool> epcon = new List<bool>();
            foreach (Sketch.EndPoint ep in this.EndPt)
            {
                epcon.Add(false);
            }
            List<double> minDist = new List<double>();
            foreach (Sketch.EndPoint ep in this.EndPt)
            {
                minDist.Add(Double.PositiveInfinity);
            }
            List<Wire> epConToWire = new List<Wire>();
            foreach (Sketch.EndPoint ep in this.EndPt)
            {
                epConToWire.Add(new Wire());
            }
            List<List<Point>> epIntersectList = new List<List<Point>>();
            foreach (Sketch.EndPoint ep in this.EndPt)
            {
                epIntersectList.Add(new List<Point>());
            }

            // Look at each endpoint for connections to other wires
            int index = 0;
            foreach (Sketch.EndPoint ep in this.EndPt)
            {
                // Find the skinny bounding box (rectangular with the long edge aligned with the slope of the endpoint)
                double theta = Math.Atan2(ep.Slope, 1);

                // Debug
                if (!this.EndPtConnect[index])
                    Console.WriteLine("\n{0}, Endpoint {1} has a slope of {2}/angle of {3}", this.Name, index, ep.Slope, theta * 180 / Math.PI);

                // Find rotated coordinates
                double epXPrime = ep.X * Math.Cos(theta) - ep.Y * Math.Sin(theta);
                double epYPrime = ep.X * Math.Sin(theta) + ep.Y * Math.Cos(theta);

                // Of the wires that intersect the bounding box, the one closest to the endpoint is taken as the wire to connect to
                foreach (Wire w in wires)
                {
                    List<Point> intersects;

                    if (!w.id.Equals(this.id) && intersectCount(theta, MV, epXPrime, epYPrime, feedback, w.substroke.PointsL, out intersects) > 0 && !this.EndPtConnect[index])
                    {
                        double localMinDist = Double.PositiveInfinity;
                        Point localClosestPoint = new Point();
                        foreach (Point p in intersects)
                        {
                            double localDist = Dist(p, ep);
                            if (localDist < localMinDist)
                            {
                                localMinDist = localDist;
                                localClosestPoint = p;
                            }
                        }

                        if (localMinDist < minDist[index])
                        {
                            minDist[index] = localMinDist;
                            epcon[index] = true;
                            epConToWire[index] = w;
                        }
                    }
                }
                index++;
            }

            // Connects the wires together
            index = 0;
            if (epcon[0] || epcon[1])
            {
                foreach (Sketch.EndPoint ep in this.EndPt)
                {
                    if (epcon[index])
                    {
                        // Set the values for the current end point and wire
                        this.EndPtConnect[index] = true;
                        this.EpToSymbOrWire[index] = "wire";

                        if (!this.ConnectedWires.Contains(epConToWire[index]))
                            this.ConnectedWires.Add(epConToWire[index]);

                        // Set the value for the other wire
                        if (!epConToWire[index].ConnectedWires.Contains(this))
                            epConToWire[index].ConnectedWires.Add(this);

                        // Add to the number of connections for the endpoints
                        this.NumEndPtCon[index]++;
                    }
                    index++;
                }
            }
        }

        #endregion

        #endregion

        #region Getters and Setters

        /// <summary>
        /// GUID of the Wire
        /// </summary>
        /// <returns>The ID of the Wire</returns>
        public Guid? ID
        {
            get
            {
                return this.id;
            }
        }

        /// <summary>
        /// The List of Points contained in the Wire object
        /// </summary>
        /// <returns>A List of Points</returns>
        public List<Point> Points
        {
            get
            {
                return this.substroke.PointsL;
            }
        }
        /// <summary>
        /// The List of the substrokes of the Wire.
        /// </summary>
        public Substroke Substroke
        {
            get
            {
                return this.substroke;
            }
        }

        public float Belief
        {
            get { return this.likelinessWire(); }
        }

        #endregion
    }
}
