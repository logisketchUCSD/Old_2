/**
 * File: InteractionFeatures.cs
 * 
 * Authors: Aaron Wolin, Devin Smith, Jason Fennell, and Max Pflueger (Sketchers 2006).
 *          Code expanded by Anton Bakalov (Sketchers 2007).
 *          Harvey Mudd College, Claremont, CA 91711.
 * 
 * This is where the CRF's interaction features are set and calculated.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */

using ConverterXML;
using Featurefy;
using Sketch;
using System;

namespace CRF
{
	public class InteractionFeatures
	{

        /*
         * Stage number represents the stage of recognition. For example, in the 
         * Gate vs. Nongate and Wire vs. Label recognition, stage 1 corresponds to
         * Gate vs. Nongate. This is necessary since we are using different combinations
         * of functions for each stage of recognition.
         */
        static int stageNumber = 1;
        public static void setStageNumber(int num)
        {
            if (num == 1 || num == 2)
                stageNumber = num;
            else
            {
                Console.WriteLine("Cannot set stageNumber to something other than 1 or 2.");
                Console.WriteLine("The current value is 1.");
            }
        }
        //public static int NUM_INTER_FEATURES = 21;
        public static int numberInterFeatures()
        {
            // Number of inter features we are using during the first stage of the multipassCRF recognition.
            if (stageNumber == 1) return 25;
            // Number of inter features we are using during the second stage of the multipassCRF recognition.
            if (stageNumber == 2) return 21;
            // Something went wrong.
            return -1;
        }

		private const double NOT_TOUCHING = 400.0;
		
		// This class will store some data members that keep track of macro characteristics of the 
		// graph that would be expensive to recalculate lots of times
		public double totalMinDistBetweenFrag;
		public double totalAverageDistBetweenFrag;
		public double totalMaxDistBetweenFrag;
		public double totalTimeBetweenFrag;
		public double totalArcLength;
		public double totalLengthOfFrag;
		public double averageSpeedOfFrag;
		public double totalMinDistBetweenEnds;
		private FeatureSketch fs;

		/// <summary>
		/// Stores macro characteristics of the CRF graph so that they don't have to be repeatedly calculated
		/// </summary>
		/// <param name="totMinDistBetweenFrag">total "minimum distance" between fragments</param>
		/// <param name="totAverageDistBetweenFrag">total "average distance" between fragments</param>
		/// <param name="totMaxDistBetweenFrag">total "maximum distance" between fragments</param>
		/// <param name="totTimeBetweenFrag">total time between fragments</param>
		/// <param name="totArcLength">total arc length of a fragment</param>
		public InteractionFeatures(double totMinDistBetweenFrag,
			double totAverageDistBetweenFrag,
			double totMaxDistBetweenFrag,
			double totTimeBetweenFrag,
			double totArcLength,
			double totLengthFrag,
			double avSpeedFrag,
			double totMinDistBetweenEnds,
			ref FeatureSketch sketch)
		{
			this.totalMinDistBetweenFrag = totMinDistBetweenFrag;
			this.totalAverageDistBetweenFrag = totAverageDistBetweenFrag;
			this.totalMaxDistBetweenFrag = totMaxDistBetweenFrag;
			this.totalTimeBetweenFrag = totTimeBetweenFrag;
			this.totalArcLength = totArcLength;
			this.totalLengthOfFrag = totLengthFrag;
			this.averageSpeedOfFrag = avSpeedFrag;
			this.totalMinDistBetweenEnds = totMinDistBetweenEnds;
			this.fs = sketch;
		}

		/// <summary>
		/// Gives the minimum euclidean distance between two strokes of the two calling nodes
		/// </summary>
		/// <param name="node1">First calling node</param>
		/// <param name="node2">Second calling node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>Minimum distance between two strokes of the two calling nodes</returns>
		private double minDistBetweenFrag(Node node1, Node node2, Substroke[] input)
		{
			double minDist = double.PositiveInfinity;
			
			for (int i = 0; i < node1.fragFeat.Points.Length; ++i)
			{
				for (int j = 0;  j < node2.fragFeat.Points.Length; ++j)
				{
					double tempDist = CreateGraph.euclideanDistance(node1.fragFeat.Points[i], node2.fragFeat.Points[j]);
					
					if (tempDist < minDist)
					{
						minDist = tempDist;
					}
				}
			}

			return minDist;
		}

		/// <summary>
		/// Determines if two fragments are (essentially) touching.
		/// </summary>
		/// <param name="node1">First calling Node</param>
		/// <param name="node2">Second calling Node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>1.0 if the minDist is less than 35, -1 otherwise</returns>
		public double touching(Node node1, Node node2, Substroke[] input)
		{
			double dist = fs.MinDistBetweenSubstrokes(node1.fragment, node2.fragment);
			double distOld = minDistBetweenFrag(node1, node2, input);
			
			double threshold = (node1.fragFeat.ArcLength.TotalLength 
				+ node2.fragFeat.ArcLength.TotalLength) * 0.01;

			threshold = Math.Max(threshold, CreateGraph.normalizeDistance(35.0, fs));


            double scale = 30; 
			return CreateGraph.tfLow(dist, threshold, scale);
		}

        /// <summary>
        /// Determines if two fragments (one of which should be a gate) are (essentially) touching.
        /// </summary>
        /// <param name="node1">First calling Node</param>
        /// <param name="node2">Second calling Node</param>
        /// <param name="input">The set of all stroke data for the graph</param>
        /// <returns>1.0 if the minDist is less than 35, -1 otherwise</returns>
        public double touchingGate(Node node1, Node node2, Substroke[] input)
        {
            if (node1.fragment.FirstLabel.Equals("Gate") ||
                node2.fragment.FirstLabel.Equals("Gate"))
            {
				double dist = fs.MinDistBetweenSubstrokes(node1.fragment, node2.fragment);

                double threshold = (node1.fragFeat.ArcLength.TotalLength
                    + node2.fragFeat.ArcLength.TotalLength) * 0.01;

                threshold = Math.Max(threshold, CreateGraph.normalizeDistance(35.0, fs));


                double scale = 30;
                return Math.Max(0, CreateGraph.tfLow(dist, threshold, scale));
            }
            else
            {
                return -1;
            }
        }

		/// <summary>
		/// Determines if two fragments are a small distance apart at the min distance point.
		/// </summary>
		/// <param name="node1">First calling Node</param>
		/// <param name="node2">Second calling Node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>1.0 if the minDist is less than 100, -1 otherwise</returns>
		public double minDistSmall(Node node1, Node node2, Substroke[] input)
		{
			double dist = fs.MinDistBetweenSubstrokes(node1.fragment, node2.fragment);

			double threshold = (node1.fragFeat.ArcLength.TotalLength 
				+ node2.fragFeat.ArcLength.TotalLength) * 0.03;

			threshold = Math.Max(threshold, CreateGraph.normalizeDistance(100.0, fs));


            double scale = 30;
			return CreateGraph.tfLow(dist, threshold, scale);
		}

		/// <summary>
		/// Determines if two fragments are a large distance apart at the min distance point.
		/// </summary>
		/// <param name="node1">First calling Node</param>
		/// <param name="node2">Second calling Node</param>
		/// <param name="?">The set of all stroke data for the graph</param>
		/// <returns>1.0 if the minDist is greater than 150, -1 otherwise</returns>
		public double minDistLarge(Node node1, Node node2, Substroke[] input)
		{
			double dist = fs.MinDistBetweenSubstrokes(node1.fragment, node2.fragment);

			double threshold = (node1.fragFeat.ArcLength.TotalLength 
				+ node2.fragFeat.ArcLength.TotalLength) * 0.05;

            threshold = Math.Max(threshold, CreateGraph.normalizeDistance(200.0, fs));


            double scale = 30;
			return CreateGraph.tfHigh(dist, threshold, scale);
		}

		/// <summary>
		/// Determines whether the specified Nodes interact with a corner.  A corner here is 
		/// considered to be a pair of ends that are near each other.  
		/// 
		/// Might also be called minDistEndsSmall.
		/// </summary>
		/// <param name="node1">First calling Node</param>
		/// <param name="node2">Second calling Node</param>
		/// <param name="input">set of all stroke data for the graph</param>
		/// <returns>1.0 if there is a corner, -1.0 otherwise</returns>
		public double corner(Node node1, Node node2, Substroke[] input)
		{
			double minDist = fs.MinDistBetweenSubstrokesEndpoints(node1.fragment, node2.fragment);

            double scale = 30;
            return CreateGraph.tfLow(minDist, CreateGraph.normalizeDistance(50.0, fs), scale);
		}

        /// <summary>
        /// Determines whether the specified Nodes interact with a corner.  A corner here is 
        /// considered to be a pair of ends that are near each other.  
        /// 
        /// Might also be called minDistEndsSmall.
        /// </summary>
        /// <param name="node1">First calling Node</param>
        /// <param name="node2">Second calling Node</param>
        /// <param name="input">set of all stroke data for the graph</param>
        /// <returns>1.0 if there is a corner, -1.0 otherwise</returns>
        public double cornerGate(Node node1, Node node2, Substroke[] input)
        {
            if (node1.fragment.FirstLabel.Equals("Gate") ||
                node2.fragment.FirstLabel.Equals("Gate"))
            {
				double minDist = fs.MinDistBetweenSubstrokesEndpoints(node1.fragment, node2.fragment);

                double scale = 30;
                return Math.Max(0, CreateGraph.tfLow(minDist, CreateGraph.normalizeDistance(50.0, fs), scale));
            }
            else
            {
                return -1;
            }
        }

		/// <summary>
		/// Determines whether the ends of the specified Nodes are significantly 
		/// farther apart than the closest points on those Nodes.
		/// </summary>
		/// <param name="node1">First calling Node</param>
		/// <param name="node2">Second calling Node</param>
		/// <param name="input">set of all stroke data for the graph</param>
		/// <returns>1.0 if the condition is met, -1.0 otherwise</returns>
		public double minDistEndsLarge(Node node1, Node node2, Substroke[] input)
		{
			double minDistEnds = fs.MinDistBetweenSubstrokesEndpoints(node1.fragment, node2.fragment);
			double minDist = this.minDistBetweenFrag(node1, node2, input);

			// This will avoid divide by zero problems and 
			// ensure minDistEnds is at least a decent value
            minDist = Math.Max(minDist, CreateGraph.normalizeDistance(20.0, fs));

            double scale = 30;
			return CreateGraph.tfHigh(minDistEnds/minDist, 2.0, scale);
		}

		/// <summary>
		/// Finds the minimum distance from the end points of one line to the body of another
		/// </summary>
		/// <param name="ends">Node ends to measure from</param>
		/// <param name="line">Node body to measure to</param>
		/// <returns>minimum euclidean distance</returns>
		private double minEndToLineDistance(Node ends, Node line)
		{
			double minDist = double.PositiveInfinity;
			double tempDist;
			int endIndex;

			for (int i = 0; i <= 1; i++)
			{
				// Iindex for the first of last point of ends
				endIndex = (ends.fragFeat.Points.Length * i) - i;

				for (int j = 0; j < line.fragment.Points.Length; j++)
				{
					// Grab the distance at these points
					tempDist = CreateGraph.euclideanDistance(ends.fragFeat.Points[endIndex], line.fragFeat.Points[j]);

					// If its the min, set it so
					if (tempDist < minDist)
					{
						minDist = tempDist;
					}
				}
			}

			return minDist;
		}

		/// <summary>
		/// Gives the minimum time between two strokes of the two calling nodes
		/// </summary>
		/// <param name="node1">First calling node</param>
		/// <param name="node2">Second calling node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>Minimum time between two strokes of the two calling nodes</returns>
		private double minTimeBetweenFrag(Node node1, Node node2, Substroke[] input)
		{
			double startTime1 = (double)node1.fragFeat.Spatial.FirstPoint.Time;
			double endTime1   = (double)node1.fragFeat.Spatial.LastPoint.Time;
			
			double startTime2 = (double)node2.fragFeat.Spatial.FirstPoint.Time;
			double endTime2   = (double)node2.fragFeat.Spatial.LastPoint.Time;

			// The two possible min differences between stroke times.
			// Always keep as a positive value
			double[] diff = new double[2];
			diff[0] = Math.Abs(((double)startTime1) - ((double)endTime2));
			diff[1] = Math.Abs(((double)endTime1) - ((double)startTime2));
			
			//diff[2] = Math.Abs(((double)startTime1) - ((double)startTime2));
			//diff[3] = Math.Abs(((double)endTime1) - ((double)endTime2));

			double minDiff = double.PositiveInfinity;

			for (int i = 0; i < diff.Length; i++)
			{
				if (diff[i] < minDiff)
				{
					minDiff = diff[i];
				}
			}

			return minDiff;
		}

		/// <summary>
		/// Determines if little time passed between drawing two strokes. Threshold is at 2 seconds.
		/// </summary>
		/// <param name="node1">First calling node</param>
		/// <param name="node2">Second calling node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>1.0 if the time difference is small, -1.0 otherwise</returns>
		public double minTimeSmall(Node node1, Node node2, Substroke[] input)
		{
			double minTime = this.minTimeBetweenFrag(node1, node2, input);

			// should be 2 seconds
			// NOTE: Aaron changed this to 800 ms since 2 seconds is still pretty long
            double scale = 30;
			return CreateGraph.tfLow(minTime, 800.0, scale);
		}

		/// <summary>
		/// Determines is much time passed between drawing two strokes. Threshold is at 3 seconds.
		/// </summary>
		/// <param name="node1">First calling node</param>
		/// <param name="node2">Second calling node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>1.0 if the time difference is large, -1.0 otherwise</returns>
		public double minTimeLarge(Node node1, Node node2, Substroke[] input)
		{
			double minTime = this.minTimeBetweenFrag(node1, node2, input);

			// should be 3 seconds
            double scale = 30;
			return CreateGraph.tfHigh(minTime, 5000.0, scale);
		}

		/// <summary>
		/// Gives the euclidean distance between the average points of the two strokes of the two calling nodes
		/// </summary>
		/// <param name="node1">First calling node</param>
		/// <param name="node2">Second calling node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>Euclidean distance between the average point of strokes associated with node1 and node2</returns>
		private double distBetweenAvgPts(Node node1, Node node2, Substroke[] input)
		{
			return node1.fragFeat.Spatial.AveragePoint.distance(node2.fragFeat.Spatial.AveragePoint);
		}

		/// <summary>
		/// Determines if the average points of the specified nodes are close together.
		/// </summary>
		/// <param name="node1">First calling node</param>
		/// <param name="node2">Second calling node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>1.0 if the average points are close, -1.0 otherwise</returns>
		public double distAvgPtsSmall(Node node1, Node node2, Substroke[] input)
		{
			double dist = this.distBetweenAvgPts(node1, node2, input);

			// should be about 2 cm on the tablet surface
            double scale = 30;
            return CreateGraph.tfLow(dist, CreateGraph.normalizeDistance(400.0, fs), scale);
		}

        /// <summary>
        /// Determines if the average points of the specified nodes (one of which should be a 
        /// gate) are close together.
        /// </summary>
        /// <param name="node1">First calling node</param>
        /// <param name="node2">Second calling node</param>
        /// <param name="input">The set of all stroke data for the graph</param>
        /// <returns>1.0 if the average points are close, -1.0 otherwise</returns>
        public double distAvgPtsSmallGate(Node node1, Node node2, Substroke[] input)
        {
            if (node1.fragment.FirstLabel.Equals("Gate") ||
               node2.fragment.FirstLabel.Equals("Gate"))
            {
                double dist = this.distBetweenAvgPts(node1, node2, input);

                // should be about 2 cm on the tablet surface
                double scale = 30;
                return Math.Max(0, CreateGraph.tfLow(dist, CreateGraph.normalizeDistance(400.0, fs), scale));
            }
            else
            {
                return -1;
            }
        }

		/// <summary>
		/// Determines if the average points of the specified nodes are far apart.
		/// </summary>
		/// <param name="node1">First calling node</param>
		/// <param name="node2">Second calling node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>1.0 if the average points are far, -1.0 otherwise</returns>
		public double distAvgPtsLarge(Node node1, Node node2, Substroke[] input)
		{
			double dist = this.distBetweenAvgPts(node1, node2, input);

			// should be about 5 cm on the tablet surface
            double scale = 30;
            return CreateGraph.tfHigh(dist, CreateGraph.normalizeDistance(1000.0, fs), scale);
		}


        /// <summary>
        /// Determines if the average points of the specified nodes (one of which should be
        /// a gate) are far apart.
        /// </summary>
        /// <param name="node1">First calling node</param>
        /// <param name="node2">Second calling node</param>
        /// <param name="input">The set of all stroke data for the graph</param>
        /// <returns>1.0 if the average points are far, -1.0 otherwise</returns>
        public double distAvgPtsLargeGate(Node node1, Node node2, Substroke[] input)
        {
            if (node1.fragment.FirstLabel.Equals("Gate") ||
                node2.fragment.FirstLabel.Equals("Gate"))
            {
                double dist = this.distBetweenAvgPts(node1, node2, input);

                // should be about 5 cm on the tablet surface
                double scale = 30;
                return Math.Max(0, CreateGraph.tfHigh(dist, CreateGraph.normalizeDistance(1000.0, fs), scale));
            }
            else
            {
                return -1;
            }
        }


		/// <summary>
		/// Determines if the maximum distance between the specified strokes is small with respect to the lengths of the strokes
		/// </summary>
		/// <param name="node1">First calling node</param>
		/// <param name="node2">Second calling node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>1.0 if the max dist is small, -1.0 otherwise</returns>
		public double maxDistSmall(Node node1, Node node2, Substroke[] input)
		{
			double maxDist = fs.MaxDistBetweenSubstrokes(node1.fragment, node2.fragment);
			//this function will be highly correlated to the length of the strokes, so
			// I need to normalize to make this a meaningful interaction
			// note that the nature of this normalizer should make a maxDist > 2x normalizer possible only in unusual cases
			double normalizer = Math.Max(node1.fragFeat.ArcLength.TotalLength, node2.fragFeat.ArcLength.TotalLength);

			//Make sure we don't divide by zero
			if(normalizer == 0.0)
				return 0.0;

            double scale = 30;
			return CreateGraph.tfLow(maxDist, normalizer *1.1, scale);
		}

		/// <summary>
		/// Determines if the maximum distance between the specified strokes is large with respect to the lengths of the strokes
		/// </summary>
		/// <param name="node1">First calling node</param>
		/// <param name="node2">Second calling node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>1.0 if the max dist is large, -1.0 otherwise</returns>
		public double maxDistLarge(Node node1, Node node2, Substroke[] input)
		{
			double maxDist = fs.MaxDistBetweenSubstrokes(node1.fragment, node2.fragment);
			//this function will be highly correlated to the length of the strokes, so
			// I need to normalize to make this a meaningful interaction
			// note that the nature of this normalizer should make a maxDist > 2x normalizer possible only in unusual cases
			double normalizer = Math.Max(node1.fragFeat.ArcLength.TotalLength, node2.fragFeat.ArcLength.TotalLength);

			//Make sure we don't divide by zero
			if (normalizer == 0.0)
				return 0.0;

            double scale = 30;
			return CreateGraph.tfHigh(maxDist, normalizer *1.4, scale);
		}

		/// <summary>
		/// This method decides if two nodes have perpendicular strokes.  If the strokes are within 15 degrees of being perpendicular
		/// then this returns a positive values.  If the storkes are more than 15 degrees from perpendicular, then returns negative values.
		/// The farther from the threshold, the more strongly this will be positive or negative.
		/// </summary>
		/// <param name="node1">First calling node</param>
		/// <param name="node2">Second calling node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>Positive is strokes are perpendicular enough, negative if strokes are parallel enough</returns>
		public double arePerpendicular(Node node1, Node node2, Substroke[] input)
		{
			double angle1 = node1.fragFeat.Slope.Direction;
			double angle2 = node2.fragFeat.Slope.Direction;

			double diffBetweenStrokes = Math.Abs(angle1 - angle2);
			
			// Edited by Eric
            diffBetweenStrokes = Math.Min(diffBetweenStrokes, (Math.PI * 2) - diffBetweenStrokes); // First, measure the angle the smaller way

            // Then take the absolute difference between 90 degrees and the angle
			double diff = Math.Abs(diffBetweenStrokes - (Math.PI / 2) );

			// Create a transfer at 15 degrees; when the difference of the angles from 90 degrees is less than
			// 15 degrees, tfLow will return a positive number.  When the difference is more than 15 degrees, it will
			// return a negative number.
            double scale = 30;
			return CreateGraph.tfLow(diff, .2617993877991494365, scale);
		}

		/// <summary>
		/// This method decides if two nodes have parallel strokes.  If the strokes are within 15 degrees of being parallel
		/// then this returns a positive values.  If the strokes are more than 15 degrees from parallel, then returns negative values.
		/// The farther from the threshold, the more strongly this will be positive or negative.
		/// </summary>
		/// <param name="node1">First calling node</param>
		/// <param name="node2">Second calling node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>Positive is strokes are parallel enough, negative if strokes are perpendicular enough</returns>
		public double areParallel(Node node1, Node node2, Substroke[] input)
		{
			double angle1 = node1.fragFeat.Slope.Direction;
			double angle2 = node2.fragFeat.Slope.Direction;

			double diffBetweenStrokes = Math.Abs(angle1 - angle2);
			
			// Added by Eric
            diffBetweenStrokes = Math.Min(diffBetweenStrokes, (Math.PI * 2) - diffBetweenStrokes); // First, measure the angle the smaller way

            // Two strokes can be parallel if their difference is close to 0 OR to 180; take the smaller of the angle and its complement
            double diff = Math.Min(diffBetweenStrokes, Math.PI - diffBetweenStrokes);

			// Create a transfer at 15 degrees.  When the difference of the angles is less than
			// 10 degrees, tfLow will return a positive number.  When the difference is more than 15 degrees, it will
			// return a negative number.
            double scale = 30;
			return CreateGraph.tfLow(diff, .2617993877991494365, scale);
		}


		/// <summary>
		/// Calculates the angle between two touching nodes.
		/// </summary>
		/// <param name="node1">First calling node</param>
		/// <param name="node2">Second calling node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>An angle if they are touching, otherwise NOT_TOUCHING</returns>
		private double calcAngle(Node node1, Node node2, Substroke[] input)
		{
			if (touching(node1, node2, input) > 0)
			{
				double angle1 = node1.fragFeat.Slope.Direction;
				double angle2 = node2.fragFeat.Slope.Direction;

				double diff = Math.Abs(angle1 - angle2);
						
				// Make sure that the angle compensates for 2 angles on opposite
				// sides of a circle (such as ~360 and ~0)
				diff = Math.Min(diff, 360 - diff);

				return diff;
			}

			return NOT_TOUCHING;
		}
				
		
		/// <summary>
		/// Returns a positive number if the angle between two touching strokes
		/// is less than a set threshold, otherwise returns a negative value.
		/// </summary>
		/// <param name="node1">First calling node</param>
		/// <param name="node2">Second calling node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>Greater than 0 if angle is small, less than 0 otherwise</returns>
		public double angleSmall(Node node1, Node node2, Substroke[] input)
		{
			double angle = calcAngle(node1, node2, input);

			// Small angles < 60 degrees
            if (angle != NOT_TOUCHING)
            {
                double scale = 30;
                return CreateGraph.tfLow(angle, Math.PI / 3, scale);
            }
            else
                return -1.0;
		}


		/// <summary>
		/// Returns a positive number if the angle between two touching strokes
		/// is greater than a set threshold, otherwise returns a negative value.
		/// </summary>
		/// <param name="node1">First calling node</param>
		/// <param name="node2">Second calling node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>Greater than 0 if angle is large, less than 0 otherwise</returns>
		public double angleLarge(Node node1, Node node2, Substroke[] input)
		{
			double angle = calcAngle(node1, node2, input);

			// Small angles < 60 degrees
            if (angle != NOT_TOUCHING)
            {
                double scale = 30;
                return CreateGraph.tfHigh(angle, Math.PI * 2 / 3, scale);
            }
            else
                return -1.0;
		}

		#region OLD PAR/PERP
//		/// <summary>
//		/// This method classifies if two strokes are perpendicular or not with light tolerance.
//		/// </summary>
//		/// <param name="node1">First calling node</param>
//		/// <param name="node2">Second calling node</param>
//		/// <param name="input">The set of all stroke data for the graph</param>
//		/// <returns>1.0 if the strokes are perpendicular, 0.0 if they are not</returns>
//		public double perpendicularLight(Node node1, Node node2, Substroke[] input)
//		{
//			double angle1 = node1.fragment.direction();
//			double angle2 = node2.fragment.direction();
//
//			double diff = Math.Abs(angle1 - angle2);
//			diff = Math.Abs(diff - 1.57079632675);  // Shift angle down by 0 degrees for easy measurement
//
//			// This corresponds to an angle of 10 degrees.  This is the tolerance in
//			// which something will be considered perpendicular
//			if (diff < .174532925194444)
//			{
//				return 1.0;
//			}
//
//			return -1.0;
//			//return 0.0;
//		}
//		/// <summary>
//		/// This method classifies if two strokes are perpendicular or not with medium tolerance.
//		/// </summary>
//		/// <param name="node1">First calling node</param>
//		/// <param name="node2">Second calling node</param>
//		/// <param name="input">The set of all stroke data for the graph</param>
//		/// <returns>1.0 if the strokes are perpendicular, 0.0 if they are not</returns>
//		public double perpendicularMid(Node node1, Node node2, Substroke[] input)
//		{
//			double angle1 = node1.fragment.direction();
//			double angle2 = node2.fragment.direction();
//
//			double diff = Math.Abs(angle1 - angle2);
//			diff = Math.Abs(diff - 1.57079632675);  // Shift angle down by 0 degrees for easy measurement
//
//			// This corresponds to an angle of 5 degrees.  This is the tolerance in
//			// which something will be considered perpendicular
//			if (diff < .0872664625972222)
//			{
//				return 1.0;
//			}
//
//			return -1.0;
//			//return 0.0;
//		}
//
//		/// <summary>
//		/// This method classifies if two strokes are perpendicular or not with heavy tolerance.
//		/// </summary>
//		/// <param name="node1">First calling node</param>
//		/// <param name="node2">Second calling node</param>
//		/// <param name="input">The set of all stroke data for the graph</param>
//		/// <returns>1.0 if the strokes are perpendicular, 0.0 if they are not</returns>
//		public double perpendicularHeavy(Node node1, Node node2, Substroke[] input)
//		{
//			double angle1 = node1.fragment.direction();
//			double angle2 = node2.fragment.direction();
//
//			double diff = Math.Abs(angle1 - angle2);
//			diff = Math.Abs(diff - 1.57079632675);  // Shift angle down by 0 degrees for easy measurement
//
//			// This corresponds to an angle of 1 degree.  This is the tolerance in
//			// which something will be considered perpendicular
//			if (diff < .0174532925194444)
//			{
//				return 1.0;
//			}
//
//			return -1.0;
//			//return 0.0;
//		}
//
//		/// <summary>
//		/// This method classifies if two strokes are parallel or not with light tolerance.
//		/// </summary>
//		/// <param name="node1">First calling node</param>
//		/// <param name="node2">Second calling node</param>
//		/// <param name="input">The set of all stroke data for the graph</param>
//		/// <returns>1.0 if the strokes are parallel, 0.0 if they are not</returns>
//		public double parallelLight(Node node1, Node node2, Substroke[] input)
//		{
//			double angle1 = node1.fragment.direction();
//			double angle2 = node2.fragment.direction();
//
//			double diff = Math.Abs(angle1 - angle2);
//
//			// This corresponds to an angle of 10 degrees.  This is the tolerance in
//			// which something will be considered parallel
//			if (diff < .174532925194444)
//			{
//				return 1.0;
//			}
//
//			return -1.0;
//			//return 0.0;
//		}
//		/// <summary>
//		/// This method classifies if two strokes are parallel or not with medium tolerance.
//		/// </summary>
//		/// <param name="node1">First calling node</param>
//		/// <param name="node2">Second calling node</param>
//		/// <param name="input">The set of all stroke data for the graph</param>
//		/// <returns>1.0 if the strokes are parallel, 0.0 if they are not</returns>
//		public double parallelMid(Node node1, Node node2, Substroke[] input)
//		{
//			double angle1 = node1.fragment.direction();
//			double angle2 = node2.fragment.direction();
//
//			double diff = Math.Abs(angle1 - angle2);
//
//			// This corresponds to an angle of 5 degrees.  This is the tolerance in
//			// which something will be considered parallel
//			if (diff < .0872664625972222)
//			{
//				return 1.0;
//			}
//
//			return -1.0;
//			//return 0.0;
//		}
//
//		/// <summary>
//		/// This method classifies if two strokes are parallel or not with heavy tolerance.
//		/// </summary>
//		/// <param name="node1">First calling node</param>
//		/// <param name="node2">Second calling node</param>
//		/// <param name="input">The set of all stroke data for the graph</param>
//		/// <returns>1.0 if the strokes are parallel, 0.0 if they are not</returns>
//		public double parallelHeavy(Node node1, Node node2, Substroke[] input)
//		{
//			double angle1 = node1.fragment.direction();
//			double angle2 = node2.fragment.direction();
//
//			double diff = Math.Abs(angle1 - angle2);
//
//			// This corresponds to an angle of 1 degree.  This is the tolerance in
//			// which something will be considered parallel
//			if (diff < .0174532925194444)
//			{
//				return 1.0;
//			}
//
//			return -1.0;
//			//return 0.0;
//		}
		#endregion

//		/// <summary>
//		/// Normalized dot product of the strokes from nodes 1 and 2
//		/// </summary>
//		/// <param name="node1">First calling node</param>
//		/// <param name="node2">Second calling node</param>
//		/// <param name="input">The set of all stroke data for the graph</param>
//		/// <returns>The dot product of the vectors represented by lines fitted to the stroke data, normalized
//		/// by the square root of the sum of the squares of the norms of the vectors of each line</returns>
//		public double dotProduct(Node node1, Node node2, Substroke[] input)
//		{
//// FIXME!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
//
//			// Represent vectors by their endpoints.  Create them by shifting the back of the vectors to the origin
//			Point vec1 = new Point(Convert.ToString(node1.fragment.getEndPoint().X - node1.fragment.getStartPoint().X),
//				Convert.ToString(node1.fragment.getEndPoint().Y - node1.fragment.getStartPoint().Y), "","","","");
//			Point vec2 = new Point(Convert.ToString(node2.fragment.getEndPoint().X - node2.fragment.getStartPoint().X),
//				Convert.ToString(node2.fragment.getEndPoint().Y - node2.fragment.getStartPoint().Y), "","","","");
//
//			double dotProd = ((vec1.X * vec2.X) + (vec1.Y * vec2.Y));
//			//double norm1 = Math.Sqrt((vec1.X * vec1.X) + (vec1.Y * vec1.Y));  Don't need sqrt because the normalizing factor just squares this
//			double norm1 = ((vec1.X * vec1.X) + (vec1.Y * vec1.Y));
//			//double norm2 = Math.Sqrt((vec2.X * vec2.X) + (vec2.Y * vec2.Y));
//			double norm2 = ((vec2.X * vec2.X) + (vec2.Y * vec2.Y));
//
//			// normalizing factor = sqrt( ||vec1||^2 + ||vec2||^2 ) OLD
//			// normalizing factor = ||vec1|| + ||vec2||
//			double normalizing = norm1+norm2;
//
//			return (dotProd / normalizing);
//		}

		/// <summary>
		/// Cross-Stem T-Junction
		/// Returns 1.0 if a likely T-Junction exists with input 1 as the cross(-) and input 2 as the stem (|) of a T-Junction.
		/// Also see its twin brother SC_TJxn, which is exacly the same with the inputs switched.
		/// </summary>
		/// <param name="cross">potential cross node</param>
		/// <param name="stem">potential stem node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>likelihood of cross-stem T-Junction</returns>
		public double CS_TJxn(Node cross, Node stem, Substroke[] input)
		{
			//find the minimum distance of and end of stem to the body of cross
			double minDistEndBody = minEndToLineDistance(stem, cross);

			//find the minimum distance between endpoints
			double minDistEnds = fs.MinDistBetweenSubstrokesEndpoints(cross.fragment, stem.fragment);

			//check the difference in minimum Dists
			// minDistEnds should be significantly greater than minDistEndBody to 
			// qualify a T-Junction
			if (minDistEnds > 2.5 * minDistEndBody)
			{
				//min dist ratio criteria satisfied
				return 1.0;
			}
			else
			{
				return -1.0;
				//return 0.0;
			}
		}

		/// <summary>
		/// Stem-Cross T-Junction
		/// Returns 1.0 if a likely T-Junction exists with input 1 as the stem(|) and input 2 as the cross(-) of a T-Junction.
		/// Also see its twin brother CS_TJxn, which is exacly the same with the inputs switched.
		/// </summary>
		/// <param name="stem">potential stem node</param>
		/// <param name="cross">potential cross node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>likelihood of stem-cross T-Junction</returns>
		public double SC_TJxn(Node stem, Node cross, Substroke[] input)
		{
			//find the minimum distance of and end of stem to the body of cross
			double minDistEndBody = minEndToLineDistance(stem, cross);

			//find the minimum distance between endpoints
			double minDistEnds = fs.MinDistBetweenSubstrokesEndpoints(stem.fragment, cross.fragment);

			//check the difference in minimum Dists
			// minDistEnds should be significantly greater than minDistEndBody to 
			// qualify a T-Junction
			if (minDistEnds > 2.5 * minDistEndBody) 
			{
				//min dist ratio criteria satisfied
				return 1.0;
			}
			else
			{
				return -1.0;
				//return 0.0;
			}
		}

		/// <summary>
		/// This is a standard biasing function, that returns 1.0 given any input
		/// </summary>
		/// <param name="node1">First calling node</param>
		/// <param name="node2">Second calling node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>1.0, always</returns>
		public double biasFunction(Node node1, Node node2, Substroke[] input)
		{
			return 1.0;
		}

		/// <summary>
		/// INCOMPLETE
		/// Determines whether the specified Nodes form a cross
		/// </summary>
		/// <param name="node1">First calling Node</param>
		/// <param name="node2">Second calling Node</param>
		/// <param name="input">set of all stroke data for the graph</param>
		/// <returns>1.0 if there is a cross, -1.0 otherwise</returns>
		public double cross(Node node1, Node node2, Substroke[] input)
		{
			//if(!intersecting)
			//	return -1.0;
			//else
			//Point center = intersection point
			//check node1 start and end are at least 10% node1 length from center //ARBITRARY NUMBER
			// ditto for node2
			//if tests are passed return 1.0

			return 0.0;
		}

		/// <summary>
		/// If either possible grouping of ends into pairs has both endpoints within 5% of the average arc length
		/// of each other, then the ends are considered close.
		/// </summary>
		/// <param name="node1">First calling node</param>
		/// <param name="node2">Second calling node</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>Returns 1.0 if both pairs of endpoints of the two strokes are sufficiently close</returns>
		public double endsClose(Node node1, Node node2, Substroke[] input)
		{
			ArcLength arc1 = new ArcLength(node1.fragment.Points);
			ArcLength arc2 = new ArcLength(node2.fragment.Points);

			double fivePercentAvgArcLength = .05 * ((node1.fragFeat.ArcLength.TotalLength + node2.fragFeat.ArcLength.TotalLength) / 2);

			int end1 = 0;
			int end1other = node1.fragFeat.Points.Length - 1;
			int end2 = 0;
			int end2other = node2.fragFeat.Points.Length - 1;

			if( (CreateGraph.euclideanDistance( node1.fragFeat.Points[end1], node2.fragFeat.Points[end2] ) <= fivePercentAvgArcLength) &&
				(CreateGraph.euclideanDistance( node1.fragFeat.Points[end1other], node2.fragFeat.Points[end2other] ) <= fivePercentAvgArcLength ))
			{
				return 1.0;
			}

			end1 = node1.fragFeat.Points.Length - 1;
			end1other = 0;

			if( (CreateGraph.euclideanDistance( node1.fragFeat.Points[end1], node2.fragFeat.Points[end2] ) <= fivePercentAvgArcLength) &&
				(CreateGraph.euclideanDistance( node1.fragFeat.Points[end1other], node2.fragFeat.Points[end2other] ) <= fivePercentAvgArcLength ))
			{
				return 1.0;
			}
			return -1.0;
			//return 0.0;
		}

		
		/// <summary>
		/// If the absolute difference between the timestamps of the endpoints of the strokes is small enough, then
		/// assume that they were originally part of the same stroke and thus the pen was not lifted between them.
		/// </summary>
		/// <param name="node1"></param>
		/// <param name="node2"></param>
		/// <param name="input"></param>
		/// <returns>1.0 if the pen was lifted between drawing the two strokes</returns>
		public double penLifted(Node node1, Node node2, Substroke[] input)
		{
			// Check if children of the same stroke.
			if(node1.fragment.ParentStroke.Equals(node2.fragment.ParentStroke))
			{
				return 1.0;
			}
			else
			{
				return -1.0;
			}
		}

        /// <summary>
        /// If the absolute difference between the timestamps of the endpoints of the strokes is small enough, then
        /// assume that they were originally part of the same stroke and thus the pen was not lifted between them.
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        /// <param name="input"></param>
        /// <returns>1.0 if the pen was lifted between drawing the two strokes</returns>
        public double penLiftedGate(Node node1, Node node2, Substroke[] input)
        {
            if (node1.fragment.FirstLabel.Equals("Gate") ||
                node2.fragment.FirstLabel.Equals("Gate"))
            {
                // Check if children of the same stroke.
                if (node1.fragment.ParentStroke.Equals(node2.fragment.ParentStroke))
                {
                    return 1.0;
                }
                else
                {
                    return 0.0;
                }
            }
            else 
            {
                return -1;
            }
        }

		/// <summary>
		/// This function find the weighted average of the change in angle (in radians) across 3 points, 
		/// weighted by distance.
		/// </summary>
		/// <param name="callingNode">Node that the function is being evaluated on.</param>
		/// <returns>Average (by distance) angle turned by triplets of points.</returns>
		private double turningWeighted(Node callingNode)
		{
			double sumDeltaAngleWeighted = 0.0;
			//note that this will be storing a value slightly different than the real stroke length
			double totalDistance = 0.0;

			//make sure we have something to compute
			if (callingNode.fragFeat.Points.Length < 3)
				return sumDeltaAngleWeighted;

			//make room for the data, and initialize to our first case
			double prevX = (callingNode.fragFeat.Points[0]).X;
			double prevY = (callingNode.fragFeat.Points[0]).Y;
			double X = (callingNode.fragFeat.Points[1]).X;
			double Y = (callingNode.fragFeat.Points[1]).Y;

			//the change in X and Y
			double delX = X - prevX;
			double delY = Y - prevY;

			//ah-Ha, the angle
			double prevDirection = Math.Atan2(delY, delX);

			//make some space we will need
			double newDirection;
			double deltaAngle;
			double deltaDist;

			for(int i=2; i<callingNode.fragFeat.Points.Length; i++)
			{
				//update the previous values
				prevX = X;
				prevY = Y;

				//grab the new values
				X = (callingNode.fragFeat.Points[i]).X;
				Y = (callingNode.fragFeat.Points[i]).Y;

				//find the new deltas
				delX = X - prevX;
				delY = Y - prevY;

				//find the new direction
				newDirection = Math.Atan2(delY, delX);

				//find the change from the previous dirction
				deltaAngle = newDirection - prevDirection;

				//not so fast, we're not done yet
				// deltaAngle has to be in the range +pi to -pi
				deltaAngle = (deltaAngle % (2*Math.PI));
				if (deltaAngle > Math.PI)
				{
					deltaAngle -= 2*Math.PI;
				}

				//find the distance by which to weight this change in angle
				//  divide by 2 to avoid double counting
				deltaDist = CreateGraph.euclideanDistance(callingNode.fragFeat.Points[i-1], callingNode.fragFeat.Points[i]) / 2;

				//and finally add it to the sum
				sumDeltaAngleWeighted += deltaAngle * deltaDist;
				totalDistance += deltaDist;

				//some bookkeeping
				prevDirection = newDirection;
			}

			//Dividing by totalDistance creates the weighted average of change in angles
			return Math.Abs(sumDeltaAngleWeighted / totalDistance);
		}

		/// <summary>
		/// Determines if one node is straight and the other is curved. Returns 1 if its true, -1 if its not.
		/// Uses turningWeighted to determine the degree of curvature.
		/// Note that this function exists in two mirror image versions.
		/// </summary>
		/// <param name="straight">The node that must be straight</param>
		/// <param name="curved">The node that must be curved</param>
		/// <param name="input">All the stroke data for the graph</param>
		/// <returns>1 if the condition is true, -1 otherwise</returns>
		public double straightCurved(Node straight, Node curved, Substroke[] input)
		{
			double sCurvature = turningWeighted(straight);
			double cCurvature = turningWeighted(curved);

			//decision at 0.033
			if(sCurvature < 0.033 && cCurvature >= 0.033)
				return 1.0;
			else
				return -1.0;

			//return 0.0;
		}

		/// <summary>
		/// Determines if one node is straight and the other is curved. Returns 1 if its true, -1 if its not.
		/// Uses turningWeighted to determine the degree of curvature.
		/// Note that this function exists in two mirror image versions.
		/// </summary>
		/// <param name="curved">The node that must be curved</param>
		/// <param name="straight">The node that must be straight</param>
		/// <param name="input">All the stroke data for the graph</param>
		/// <returns>1 if the condition is true, -1 otherwise</returns>
		public double curvedStraight(Node curved, Node straight, Substroke[] input)
		{
			double sCurvature = turningWeighted(straight);
			double cCurvature = turningWeighted(curved);

			//decision at 0.033
			if(sCurvature < 0.033 && cCurvature >= 0.033)
				return 1.0;
			else
				return -1.0;

			//return 0.0;
		}

		/// <summary>
		/// Determines if both lines are straight.  Returns 1 if its true, -1 if its not.
		/// Uses turningWeighted to determine the degree of curvature.
		/// </summary>
		/// <param name="straight1">The first line</param>
		/// <param name="straight2">The second line</param>
		/// <param name="input">All the stroke data for the graph</param>
		/// <returns>1 if the condition is true, -1 otherwise</returns>
		public double straightStraight(Node straight1, Node straight2, Substroke[] input)
		{
			double sCurvature1 = turningWeighted(straight1);
			double sCurvature2 = turningWeighted(straight2);

			//decision at 0.033
			if(sCurvature1 < 0.033 && sCurvature2 < 0.033)
				return 1.0;
			else
				return -1.0;

			//return 0.0;
		}

		/// <summary>
		/// Determines if both lines are curved.  Returns 1 if its true, -1 if its not.
		/// Uses turningWeighted to determine the degree of curvature.
		/// </summary>
		/// <param name="straight1">The first line</param>
		/// <param name="straight2">The second line</param>
		/// <param name="input">All the stroke data for the graph</param>
		/// <returns>1 if the condition is true, -1 otherwise</returns>
		public double curvedCurved(Node curved1, Node curved2, Substroke[] input)
		{
			double cCurvature1 = turningWeighted(curved1);
			double cCurvature2 = turningWeighted(curved2);

			//decision at 0.033
			if(cCurvature1 >= 0.033 && cCurvature2 >= 0.033)
				return 1.0;
			else
				return -1.0;
            

			//return 0.0;
		}


		/// <summary>
		/// Determines if both strokes are within a similar bounding box.
		/// The bounding box area for both strokes is extended by some threshold, then the strokes are 
		/// determined to be within a similar bounding box.
		///  
		/// Returns 1.0 if the bounding boxes are similar, -1.0 otherwise
		/// </summary>
		/// <param name="node1">First node</param>
		/// <param name="node2">Second node</param>
		/// <param name="input">All the stroke data for the graph</param>
		/// <returns>1.0 if the bounding boxes are similar, -1.0 otherwise</returns>
		public double similarlyBounded(Node node1, Node node2, Substroke[] input)
		{
			// Get the first node's bounding box features and area
			System.Drawing.PointF n1UpperLeft  = node1.fragFeat.Spatial.UpperLeft;
			System.Drawing.PointF n1LowerRight = node1.fragFeat.Spatial.LowerRight;
			double areaN1 = (n1LowerRight.X - n1UpperLeft.X) * (n1LowerRight.Y - n1UpperLeft.Y);

			// Get the second node's bounding box features and area
			System.Drawing.PointF n2UpperLeft  = node2.fragFeat.Spatial.UpperLeft;
			System.Drawing.PointF n2LowerRight = node2.fragFeat.Spatial.LowerRight;
			double areaN2 = (n2LowerRight.X - n2UpperLeft.X) * (n2LowerRight.Y - n2UpperLeft.Y);

			// 15% greater than the original area's added together
			double threshold = 1.15;			

			// Get the total bounding box x- and y-axis lengths
			double totalX = Math.Max(n1LowerRight.X, n2LowerRight.X) - Math.Min(n1UpperLeft.X, n2UpperLeft.X);
			double totalY = Math.Max(n1LowerRight.Y, n2LowerRight.Y) - Math.Min(n1UpperLeft.Y, n2UpperLeft.Y);

			// If the total area is less than either bounding box's area within the threshold, then
			// we can conclude that the nodes are relatively grouped together
			if (totalX * totalY < (areaN1 + areaN2) * threshold)
				return 1.0;
			else
				return -1.0;
		}



		/// <summary>
		/// This method returns an array containing delegates referencing every function in this class.  This will allow the CRF
		/// to load all of its interaction features without having the number of interaction features specified as an argument.
		/// </summary>
		/// <returns>An array of delegates containing all of the interaction feature functions</returns>
		public CRF.interactionDelegate[] getInteractionFeatures()
		{
			CRF.interactionDelegate[] interactionFeatures = new CRF.interactionDelegate[numberInterFeatures()];

			//interactionFeatures[4] = new CRF.interactionDelegate(this.minDistBetweenFrag);
			interactionFeatures[0] = new CRF.interactionDelegate(this.touching);
			//interactionFeatures[1] = new CRF.interactionDelegate(this.minDistBetweenEnds);
			interactionFeatures[1] = new CRF.interactionDelegate(this.minDistSmall);
			interactionFeatures[2] = new CRF.interactionDelegate(this.minDistLarge);
			//interactionFeatures[4] = new CRF.interactionDelegate(this.maxDistBetweenFrag);
			interactionFeatures[3] = new CRF.interactionDelegate(this.maxDistSmall);
			interactionFeatures[4] = new CRF.interactionDelegate(this.maxDistLarge);
			interactionFeatures[5] = new CRF.interactionDelegate(this.corner);
			interactionFeatures[6] = new CRF.interactionDelegate(this.minDistEndsLarge);
			//interactionFeatures[2] = new CRF.interactionDelegate(this.minTimeBetweenFrag);
			//interactionFeatures[3] = new CRF.interactionDelegate(this.distBetweenAvgPts);
			interactionFeatures[7] = new CRF.interactionDelegate(this.distAvgPtsSmall);
			interactionFeatures[8] = new CRF.interactionDelegate(this.distAvgPtsLarge);
			interactionFeatures[9] = new CRF.interactionDelegate(this.arePerpendicular);
			interactionFeatures[10] = new CRF.interactionDelegate(this.areParallel);
			//interactionFeatures[7] = new CRF.interactionDelegate(this.dotProduct);
			interactionFeatures[11] = new CRF.interactionDelegate(this.biasFunction);
			//interactionFeatures[16] = new CRF.interactionDelegate(this.cross);
			interactionFeatures[12] = new CRF.interactionDelegate(this.endsClose);
			interactionFeatures[13] = new CRF.interactionDelegate(this.penLifted);
			interactionFeatures[14] = new CRF.interactionDelegate(this.straightCurved);
			interactionFeatures[15] = new CRF.interactionDelegate(this.curvedStraight);
			interactionFeatures[16] = new CRF.interactionDelegate(this.straightStraight);
			interactionFeatures[17] = new CRF.interactionDelegate(this.curvedCurved);
			
			// New IFs added by Aaron
		    interactionFeatures[18] = new CRF.interactionDelegate(this.similarlyBounded);
			interactionFeatures[19] = new CRF.interactionDelegate(this.angleSmall);
			interactionFeatures[20] = new CRF.interactionDelegate(this.angleLarge);

            // These feature functions are to be used in a multipass CRF in which the first stage of recognition
            // is gate vs. nongate.
            //interactionFeatures[12] = new CRF.interactionDelegate(this.cornerGate);
            //interactionFeatures[13] = new CRF.interactionDelegate(this.distAvgPtsLargeGate);
            //interactionFeatures[14] = new CRF.interactionDelegate(this.distAvgPtsSmallGate);
            //interactionFeatures[15] = new CRF.interactionDelegate(this.endsCloseGate);
            //interactionFeatures[16] = new CRF.interactionDelegate(this.penLiftedGate);
            //interactionFeatures[17] = new CRF.interactionDelegate(this.touchingGate);

            if (stageNumber == 1)
            {
                interactionFeatures[21] = new CRF.interactionDelegate(this.CS_TJxn);
                interactionFeatures[22] = new CRF.interactionDelegate(this.SC_TJxn); 
                interactionFeatures[23] = new CRF.interactionDelegate(this.minTimeSmall);
                interactionFeatures[24] = new CRF.interactionDelegate(this.minTimeLarge);
            }

            if (stageNumber == 2)
            { 
                // Nothing to do here.
            }

			return interactionFeatures;
		}

		/// <summary>
		/// Returns an array of double that are the values of all of the functions in this class, evaluated
		/// on the passed Nodes and Stroke array
		/// </summary>
		/// <param name="node1">Node to evaluate class on</param>
		/// <param name="node2">Other node to evaluate class on</param>
		/// <param name="input">Overall strok data to evaluate on</param>
		/// <returns>Array of all the output of all the functions in this class evaluated on the passed data</returns>
		public double[] evalInteractionFeatures(Node node1, Node node2, Substroke[] input)
		{
			CRF.interactionDelegate[] interactionFeatures = getInteractionFeatures();
			double[] evaledInteractionFeatures = new double[numberInterFeatures()];

			for(int i = 0; i < interactionFeatures.Length; i++)
			{
				evaledInteractionFeatures[i] = interactionFeatures[i](node1, node2, input);
			}

			return evaledInteractionFeatures;
		}
	}
}
