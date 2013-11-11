/**
 * File: SiteFeatures.cs
 * 
 * Authors: Aaron Wolin, Devin Smith, Jason Fennell, and Max Pflueger (Sketchers 2006).
 *          Code expanded by Anton Bakalov (Sketchers 2007).
 *          Harvey Mudd College, Claremont, CA 91711.
 * 
 * This is where the CRF's site features are set and calculated.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */

using ConverterXML;
using Featurefy;
using System;
using Sketch;

namespace CRF
{
	public class SiteFeatures
	{
		#region Internals

		private FeatureSketch sketch;

		#endregion

		/* This variable is used by ThresholdEval.cs for automatically
         * varying a threshold parameter in order to find the value achieving
         * maximum accuracy.
         * 
         * Note that the function which parameter we are varying should be 
         * changed back to its original state. Currently "parameter" is 
         * used in CircularInkDensityHigh();
         */ 
        static double parameter;
        public static void setParameter(double param)
        {
            parameter = param;
        }

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
        //public static int NUM_SITE_FEATURES = 12;
        /// <summary>
        /// Returns the number of site features depending on what the stage number is.
        /// </summary>
        /// <returns></returns>
        public static int numberSiteFeatures()
        {
                // Number of site features we are using during the first stage of the multipassCRF recognition.
                if (stageNumber == 1) return 15; 
                // Number of site features we are using during the second stage of the multipassCRF recognition.
                if (stageNumber == 2) return 15;
                // Something went wrong.
                return -1;
        }


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
		public double graphLeftX;
		public double graphRightX;
		public double graphTopY;
		public double graphBottomY;

		/// <summary>
		/// Stores macro characteristics of the CRF graph so that they don't have to be repeatedly calculated
		/// </summary>
		/// <param name="totMinDistBetweenFrag">total "minimum distance" between fragments</param>
		/// <param name="totAverageDistBetweenFrag">total "average distance" between fragments</param>
		/// <param name="totMaxDistBetweenFrag">total "maximum distance" between fragments</param>
		/// <param name="totTimeBetweenFrag">total time between fragments</param>
		/// <param name="totArcLength">total arc length of a fragment</param>
		/// <param name="bbox">The bounding box of the entire skecth [leftx, topy, rightx, boty]</param>
		public SiteFeatures(double totMinDistBetweenFrag,
			double totAverageDistBetweenFrag,
			double totMaxDistBetweenFrag,
			double totTimeBetweenFrag,
			double totArcLength,
			double totLengthFrag,
			double avSpeedFrag,
			double totMinDistBetweenEnds,
			double[] bbox, ref FeatureSketch sketch)
		{
			this.totalMinDistBetweenFrag = totMinDistBetweenFrag;
			this.totalAverageDistBetweenFrag = totAverageDistBetweenFrag;
			this.totalMaxDistBetweenFrag = totMaxDistBetweenFrag;
			this.totalTimeBetweenFrag = totTimeBetweenFrag;
			this.totalArcLength = totArcLength;
			this.totalLengthOfFrag = totLengthFrag;
			this.averageSpeedOfFrag = avSpeedFrag;
			this.totalMinDistBetweenEnds = totMinDistBetweenEnds;
			this.graphLeftX = bbox[0];
			this.graphTopY = bbox[1];
			this.graphRightX = bbox[2];
			this.graphBottomY = bbox[3];
			this.sketch = sketch;
		}


		/// <summary>
		/// Feature function to get the minium distance from the left or right of the sketch
		/// </summary>
		/// <param name="callingNode"></param>
		/// <param name="input"></param>
		/// <returns>1 if close to edge, -1 if far away</returns>
		private double distFromLR(Node callingNode, Substroke[] input)
		{
			double fromL = callingNode.fragFeat.Spatial.UpperLeft.X - this.graphLeftX;
			double fromR = this.graphRightX - callingNode.fragFeat.Spatial.LowerRight.X;
			double dist = fromL;
			if ( fromR < dist )
			{
				dist = fromR;
			}
            double scale = 30;
			return CreateGraph.tfLow(dist, (this.graphRightX-this.graphLeftX) / 4, scale);
		}

		/// <summary>
		/// Feature function to get the minium distance from the left or right of the sketch
		/// </summary>
		/// <param name="callingNode"></param>
		/// <param name="input"></param>
		/// <returns>1 if close to edge, -1 if far away</returns>
		private double distFromTB(Node callingNode, Substroke[] input)
		{
			double fromTop = callingNode.fragFeat.Spatial.UpperLeft.Y - this.graphTopY;
			double fromBot = this.graphBottomY - callingNode.fragFeat.Spatial.LowerRight.Y;
			double dist = fromTop;
			if ( fromBot < dist )
			{
				dist = fromBot;
			}
            double scale = 30;
			return CreateGraph.tfLow(dist, (this.graphBottomY-this.graphTopY) / 4, scale);
		}

		/// <summary>
		/// Feature function to get the euclidean distance between the endpoints of a stroke
		/// </summary>
		/// <param name="callingNode">Node that the feature function is being evaluated around.</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>Euclidean distance between the endpoints of a stroke</returns>
		private double distBetweenEndpoints(Node callingNode, Substroke[] input)
		{
			return callingNode.fragFeat.Spatial.DistanceFromFirstToLast;
		}

		/// <summary>
		/// Determines if the two ends of this stroke are far apart
		/// </summary>
		/// <param name="callingNode">Node to evaluate on</param>
		/// <param name="input">The set of all stroke data in the graph</param>
		/// <returns>1 if far apart, -1 otherwise</returns>
		public double distBetweenEndsLarge(Node callingNode, Substroke[] input)
		{
			double dist = distBetweenEndpoints(callingNode, input);

			// transfer at dist > 70% of arclength
			if(callingNode.fragFeat.ArcLength.TotalLength == 0.0)
			{
				//This stroke has a length of zero, so this feature is meaningless
				return 0.0;
			}
            double scale = 30;
			return CreateGraph.tfHigh(dist, (callingNode.fragFeat.ArcLength.TotalLength * 0.7), scale);
		}

		/// <summary>
		/// Determines if the two endso of this stroke are close together
		/// </summary>
		/// <param name="callingNode">Node to evaluate on</param>
		/// <param name="input">The set of all stroke data in the graph</param>
		/// <returns></returns>
		public double distBetweenEndsSmall(Node callingNode, Substroke[] input)
		{
			double dist = distBetweenEndpoints(callingNode, input);

			// transfer at dist < 20% of arclength
			if(callingNode.fragFeat.ArcLength.TotalLength == 0.0)
			{
				//This stroke has a length of zero, so this feature is meaningless
				return 0.0;
			}

            double scale = 30;
			return CreateGraph.tfLow(dist, (callingNode.fragFeat.ArcLength.TotalLength * 0.2), scale);
		}

		/// <summary>
		/// Gives the "normalized" arc length of the stroke associated with the node.  This is the arc length
		/// of the stroke divided by the totalArcLength of the graph.
		/// </summary>
		/// <param name="callingNode">Node that the feature function is being evaluated around.</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>Arc length of stroke normalized by total arc length</returns>
		private double normalizedArcLength(Node callingNode, Substroke[] input)
		{
			return (callingNode.fragFeat.ArcLength.TotalLength / totalArcLength);
		}

		/// <summary>
		/// Determines whether this Node falls into the category of short
		/// </summary>
		/// <param name="callingNode">Node that the feature function is being evaluated around</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>1 if this Node is short, -1 otherwise</returns>
		public double arcLengthShort(Node callingNode, Substroke[] input)
		{
			// Data analysis should be performed to determine what a good threshold is for this
			// NOTE: Aaron changed this value from 1000 to 300
            double scale = 30;
			return CreateGraph.tfLow(callingNode.fragFeat.ArcLength.TotalLength, 300, scale);
		}

		/// <summary>
		/// Determines whether this Node falls into the category of long
		/// </summary>
		/// <param name="callingNode">Node that the feature function is being evaluated around</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>1 if this Node is long, -1 otherwise</returns>
		public double arcLengthLong(Node callingNode, Substroke[] input)
		{
			// Data analysis should be performed to determine what a good threshold is for this
			// NOTE: Aaron changed this value from 2000 to 2000
            double scale = 30;
			return CreateGraph.tfHigh(callingNode.fragFeat.ArcLength.TotalLength, 2000, scale);
		}

		/// <summary>
		/// This function gets the average angle of a given fragment.  
		/// </summary>
		/// <param name="callingNode">Node that the feature function is being evaluated around.</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>Average angle that a fragment is oriented at</returns>
		private double averageSlope(Node callingNode, Substroke[] input)
		{
			return callingNode.fragFeat.Slope.Direction;
		}

		/// <summary>
		/// This function gets the average speed of the pen while drawing a given fragment.
		/// </summary>
		/// <param name="callingNode">Node that the feature function is being evaluated around.</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>Average speed of pen while drawing stroke</returns>
		private double averageSpeed(Node callingNode, Substroke[] input)
		{
			return ( callingNode.fragFeat.Speed.AverageSpeed / averageSpeedOfFrag );
			//return ( callingNode.fragFeat.Speed.AvgSpeed / averageSpeedOfFrag );
		}

		/// <summary>
		/// This function gets the average curvature of a stroke over a fragment.
		/// </summary>
		/// <param name="callingNode"></param>
		/// <param name="input"></param>
		/// <returns>Average curvature</returns>
		private double averageCurvature(Node callingNode, Substroke[] input)
		{
			//don't use this, it doesn't work
			return (callingNode.fragFeat.Curvature.AverageCurvature);  // NORMALIZE ME?
		}

		/// <summary>
		/// This function find the total angle (in radians) that a stroke turns over its length.
		/// </summary>
		/// <param name="callingNode">Node that the feature function is being evaluated on.</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>Angle turned by stroke in radians.</returns>
		private double turning(Node callingNode, Substroke[] input)
		{
			// End point window so we don't count initial hooks
			const int WINDOW = 5;
			
			double sumDeltaAngle = 0.0;
			
			// Make sure we have something relevant to compute
			if (callingNode.fragFeat.Points.Length < (WINDOW * 2) + 1)
				return sumDeltaAngle;

			// Make room for the data, and initialize to our first case
			double prevX = (callingNode.fragFeat.Points[WINDOW]).X;
			double prevY = (callingNode.fragFeat.Points[WINDOW]).Y;
			double X = (callingNode.fragFeat.Points[WINDOW + 1]).X;
			double Y = (callingNode.fragFeat.Points[WINDOW + 1]).Y;

			// The change in X and Y
			double delX = X - prevX;
			double delY = Y - prevY;

			// ah-Ha, the angle
			double prevDirection = Math.Atan2(delY, delX);

			// Make some space we will need
			double newDirection;
			double deltaAngle;

			int length = callingNode.fragFeat.Points.Length - WINDOW;
			for (int i = WINDOW + 2; i < length; i++)
			{
				// Update the previous values
				prevX = X;
				prevY = Y;

				// Grab the new values
				X = (callingNode.fragFeat.Points[i]).X;
				Y = (callingNode.fragFeat.Points[i]).Y;

				// Find the new deltas
				delX = X - prevX;
				delY = Y - prevY;

				// Find the new direction
				newDirection = Math.Atan2(delY, delX);

				// Find the change from the previous dirction
				deltaAngle = newDirection - prevDirection;

				// Not so fast, we're not done yet
				// deltaAngle has to be in the range +pi to -pi
				deltaAngle = (deltaAngle % (2 * Math.PI));
				if (deltaAngle > Math.PI)
				{
					deltaAngle -= (2 * Math.PI);
				}

				// And finally add it to the sum
				sumDeltaAngle += deltaAngle;

				// Some bookkeeping
				prevDirection = newDirection;
			}

			return Math.Abs(sumDeltaAngle);
		}

		/// <summary>
		/// Determines if the stroke underwent no net angle change
		/// </summary>
		/// <param name="callingNode">Node that the feature function is being evaluated on</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>1 if turned ~0 deg, -1 otherwise</returns>
		public double turningZero(Node callingNode, Substroke[] input)
		{
			//angle turned is near zero?
			double deltaAngle = turning(callingNode, input);

			// NOTE: Aaron changed value from 17.5 (0.305) to 20
            double scale = 30;
			return CreateGraph.tfLow(deltaAngle, 0.349, scale); //approx 17.5 degrees
		}

		/// <summary>
		/// Determines if the stroke underwent a small angle change
		/// </summary>
		/// <param name="callingNode">Node that the feature function is being evaluated on</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>1 if turned small angle, -1 otherwise</returns>
		public double turningSmall(Node callingNode, Substroke[] input)
		{
			//angle turned is small
			double deltaAngle = turning(callingNode, input);

			// band is 17.5 (0.305) to 217.5 degrees (3.80)
			// NOTE: Aaron changed this value to 20 - 180
            double scale = 30;
			double upperLimit = CreateGraph.tfLow(deltaAngle, 3.14, scale); // <217.5 degrees
			double lowerLimit = CreateGraph.tfHigh(deltaAngle, 0.349, scale); // >17.5 degrees
			return upperLimit * lowerLimit; //multiply them to create a band of approx 1
		}

		/// <summary>
		/// Determines if the stroke underwent approx 1 full rotation (217.5 to 450 degrees)
		/// </summary>
		/// <param name="callingNode">Node that the feature function is being evaluated on</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>1 if turned one rotation, -1 otherwise</returns>
		public double turning360(Node callingNode, Substroke[] input)
		{
			//angle turned is near one revolution
			double deltaAngle = turning(callingNode, input);

			// band is 217.5 (3.80) to 450 (7.85) degrees
			// NOTE: Aaron changed this value to 290 - 430
            double scale = 30;
			double upperLimit = CreateGraph.tfLow(deltaAngle, 7.50, scale); // <450 degrees
			double lowerLimit = CreateGraph.tfHigh(deltaAngle, 5.06, scale); // >217.5 degrees
			return upperLimit * lowerLimit; //multiply them to create a band of approx 1
		}

		/// <summary>
		/// Determines if the stroke underwent a large amount of turning (>450 degrees)
		/// </summary>
		/// <param name="callingNode">Node that the feature function is being evaluated on</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>1 if turned large amount, -1 otherwise</returns>
		public double turningLarge(Node callingNode, Substroke[] input)
		{
			//angle turned is large
			double deltaAngle = turning(callingNode, input);

			// NOTE: Aaron changed this value to > 430
            double scale = 30;
			return CreateGraph.tfHigh(deltaAngle, 7.50, scale); //approx 450 degrees
		}

		/// <summary>
		/// This is a standard biasing function, that returns 1.0 given any input
		/// </summary>
		/// <param name="callingNode">Node that the feature function is being evaluated around.</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>1.0, always</returns>
		public double biasFunction(Node callingNode, Substroke[] input)
		{
			return 1.0;
		}

		/// <summary>
		/// Determines whether or not the specified stroke forms a loop, based on proximity of end points
		/// </summary>
		/// <param name="callingNode">Node around which to evaluate the feature function</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns>1.0 if loop, 0.0 if not</returns>
		public double loop(Node callingNode, Substroke[] input)
		{
			//will store the distance between the end points
			double distBetweenEnds;

			Point end1 = callingNode.fragFeat.Spatial.FirstPoint;
			Point end2 = callingNode.fragFeat.Spatial.LastPoint;

			distBetweenEnds = CreateGraph.euclideanDistance(end1, end2);
			if(distBetweenEnds < CreateGraph.normalizeDistance(200, sketch))
			{
				return 1.0;
			}
			else
			//	return 0.0;
				return -1.0;
		}
		
		/// <summary>
		/// Decides whether the calling node has corners on both ends
		/// </summary>
		/// <param name="callingNode">Node on which to evaluate feature function</param>
		/// <param name="input">The set of all stoke data for the graph</param>
		/// <returns>1.0 if there are two corners, -1.0 otherwise</returns>
		public double twoCorners(Node callingNode, Substroke[] input)
		{
			double minEndDist1 = double.PositiveInfinity;
			double minEndDist2 = double.PositiveInfinity;
			double tempDist;

			Point callingFirst = callingNode.fragFeat.Spatial.FirstPoint;
			Point callingLast  = callingNode.fragFeat.Spatial.LastPoint;

			foreach(Node neighbor in callingNode.neighbors)
			{
				Point neighborFirst = neighbor.fragFeat.Spatial.FirstPoint;
				Point neighborLast = neighbor.fragFeat.Spatial.LastPoint;

				//Compare each end of the calling node to each end of each of its neighbors
				tempDist = CreateGraph.euclideanDistance(callingFirst, neighborFirst);
				if(tempDist < minEndDist1)
				{
					minEndDist1 = tempDist;
				}

				tempDist = CreateGraph.euclideanDistance(callingFirst, neighborLast);
				if(tempDist < minEndDist1)
				{
					minEndDist1 = tempDist;
				}

				tempDist = CreateGraph.euclideanDistance(callingLast, neighborFirst);
				if(tempDist < minEndDist2)
				{
					minEndDist2 = tempDist;
				}

				tempDist = CreateGraph.euclideanDistance(callingLast, neighborLast);
				if(tempDist < minEndDist2)
				{
					minEndDist2 = tempDist;
				}
			}

			//This creates a circular region about the origin in dist1 by dist2 space
			double combinedDist = Math.Sqrt(minEndDist1*minEndDist1 + minEndDist2*minEndDist2);
            double scale = 30;
			return CreateGraph.tfLow(combinedDist, CreateGraph.normalizeDistance(120, sketch), scale); 

			//if(minEndDist1 < 100 && minEndDist2 < 100) //ARBITRARY VALUES!!!!!!!!!!!!
			//{
			//	return 1.0;
			//}
			//else
			//	return 0.0;
			//	return -1.0;
		}

		/// <summary>
		/// Returns the ink density of a stroke.  This is defined as the arc length squared over area of the smallest
		/// rectangle that contains the stroke.
		/// </summary>
		/// <param name="callingNode">Node on which to evaluate feature function</param>
		/// <param name="input">The set of all stoke data for the graph</param>
		/// <returns>Ink density of a stroke</returns>
		public double squareInkDensity(Node callingNode, Substroke[] input)
		{
            double totalInkDenstity = Math.Pow(totalArcLength, 2.0) / 
                ((graphRightX - graphLeftX) * (graphTopY - graphBottomY) + 1);
			return callingNode.fragFeat.ArcLength.InkDensity / totalInkDenstity; // NORMALIZE ME??
		}

        public double squareInkDensityHigh(Node callingNode, Substroke[] input)
        {
            double density = callingNode.fragFeat.ArcLength.InkDensity;
            double scale = 100;
            return CreateGraph.tfHigh(density, 24, scale); 
            //return CreateGraph.tfHigh(density, parameter, scale);
        }


        public double squareInkDensityLow(Node callingNode, Substroke[] input)
        {
            double density = callingNode.fragFeat.ArcLength.InkDensity;
            double scale = 90;
            return CreateGraph.tfLow(density, 5, scale);
        }

		/// <summary>
		/// Returns the circular ink density of a stroke.  This is defined to be the arc length squared divided
		/// by the area of the bounding circle around the stroke (bounding circle is the smallest circle that contains the bounding box)
		/// </summary>
		/// <param name="callingNode">Node on which to evaluate feature function</param>
		/// <param name="input">The set of all stroke data for the graph</param>
		/// <returns></returns>
		public double circularInkDensity(Node callingNode, Substroke[] input)
		{
            double width = graphRightX - graphLeftX;
            double height = graphTopY - graphBottomY;
            double denominator = 4 * Math.Pow (totalArcLength, 2) / (Math.PI * (Math.Pow(width, 2) + Math.Pow(height, 2)) + 1);
            return callingNode.fragFeat.ArcLength.CircularInkDensity / denominator;  // NORMALIZE ME???
			//return 0.0;
		}

        /// <summary>
        /// Determines whether the ink density is high.
        /// </summary>
        /// <param name="callingNode">Node on which to evaluate feature function</param>
        /// <param name="input">The set of all stoke data for the graph</param>
        /// <returns>1 if high, -1 if not</returns>
        public double circularInkDensityHigh(Node callingNode, Substroke[] input)
        {
            double density = callingNode.fragFeat.ArcLength.CircularInkDensity;
            //if (density > 4) // tested 4
            //    return 1;
            //else
            //    return -1;
            double scale = 100;
            return CreateGraph.tfHigh(density, 4.5, scale); 
        }

        /// <summary>
        /// Determines whether the ink density is low.
        /// </summary>
        /// <param name="callingNode">Node on which to evaluate feature function</param>
        /// <param name="input">The set of all stoke data for the graph</param>
        /// <returns>1 if low, -1 if not</returns>
        public double circularInkDensityLow(Node callingNode, Substroke[] input)
        {
            double density = callingNode.fragFeat.ArcLength.CircularInkDensity;
            //if (density < 1.5) //tested 1.5
            //    return 1;
            //else
            //    return -1;
            double scale = 100;

            //return CreateGraph.tfLow(density, 0.9, scale); 
            return CreateGraph.tfLow(density, 0.9, scale);
        }

		/// <summary>
		/// Multi-stroke closed shape thingie.
		/// </summary>
		/// <param name="callingNode"></param>
		/// <param name="input"></param>
		/// <returns></returns>
		private double multiStrokeClosedShape(Node callingNode, Substroke[] input)
		{
			if (sketch.InClosedShape(callingNode.fragment))
				return 1.0;
			else
				return -1.0;
		}

		/// <summary>
		/// Width of the node's bounding box, normalized by sketch width
		/// </summary>
		/// <param name="callingNode"></param>
		/// <param name="input"></param>
		/// <returns></returns>
		private double Width(Node callingNode, Substroke[] input)
		{
			return (callingNode.fragFeat.Spatial.Width / sketch.BBox.Width);
		}

		private double Height(Node callingNode, Substroke[] input)
		{
			return (callingNode.fragFeat.Spatial.Height / sketch.BBox.Height);
		}

		/// <summary>
		/// This method returns an array containing delegates referencing every function in this class.  This will allow the CRF
		/// to load all of its site features without having the number of site features specified as an argument.
		/// </summary>
		/// <returns>An array of delegates containing all of the site feature functions</returns>
		public CRF.siteDelegate[] getSiteFeatures()
		{
			CRF.siteDelegate[] siteFeatures = new CRF.siteDelegate[numberSiteFeatures()];

			//siteFeatures[0] = new CRF.siteDelegate(this.normalizedArcLength);
			//siteFeatures[1] = new CRF.siteDelegate(this.averageSlope); //this seems like a possibly counter-productive site feature
			//siteFeatures[2] = new CRF.siteDelegate(this.averageSpeed); //I don't think we need this
			siteFeatures[0] = new CRF.siteDelegate(this.biasFunction);
			//siteFeatures[4] = new CRF.siteDelegate(this.loop); //obsolete by distBetweenEndsSmall
			siteFeatures[1] = new CRF.siteDelegate(this.distBetweenEndsLarge); //uncomment
			siteFeatures[2] = new CRF.siteDelegate(this.distBetweenEndsSmall); //uncomment
			siteFeatures[3] = new CRF.siteDelegate(this.arcLengthShort);
			siteFeatures[4] = new CRF.siteDelegate(this.arcLengthLong);
			siteFeatures[5] = new CRF.siteDelegate(this.turningZero); //uncomment
			siteFeatures[6] = new CRF.siteDelegate(this.turningSmall); 
			siteFeatures[7] = new CRF.siteDelegate(this.turning360); //uncomment
			siteFeatures[8] = new CRF.siteDelegate(this.turningLarge);
			//siteFeatures[12] = new CRF.siteDelegate(this.squareInkDensity);
			//siteFeatures[13] = new CRF.siteDelegate(this.circularInkDensity);
            //siteFeatures[10] = new CRF.siteDelegate(this.circularInkDensityHigh);
            //siteFeatures[11] = new CRF.siteDelegate(this.circularInkDensityLow); //uncomment one of these
            siteFeatures[9] = new CRF.siteDelegate(this.distFromLR); //uncomment

            if (stageNumber == 1)
            {
                siteFeatures[10] = new CRF.siteDelegate(this.distFromTB); //uncomment
                siteFeatures[11] = new CRF.siteDelegate(this.twoCorners); //uncomment arb
            }
            if (stageNumber == 2)
            {
                siteFeatures[10] = new CRF.siteDelegate(this.squareInkDensityHigh);
                siteFeatures[11] = new CRF.siteDelegate(this.squareInkDensityLow);
            }
			siteFeatures[12] = new CRF.siteDelegate(this.multiStrokeClosedShape);
			siteFeatures[13] = new CRF.siteDelegate(this.Width);
			siteFeatures[14] = new CRF.siteDelegate(this.Height);

            return siteFeatures;
		}

		/// <summary>
		/// Returns an array of double that are the values of all of the functions in this class, evaluated
		/// on the passed Node and Stroke array
		/// </summary>
		/// <param name="callingNode">Node to evaluate class on</param>
		/// <param name="input">Overall stroke data to evaluate on</param>
		/// <returns>Array of all the output of all the functions in this class evaluated on the passed data</returns>
		public double[] evalSiteFeatures(Node callingNode, Substroke[] input)
		{
			CRF.siteDelegate[] siteFeatures = getSiteFeatures();
			double[] evaledSiteFeatures = new double[numberSiteFeatures()];

			for(int i = 0; i < siteFeatures.Length; i++)
			{
				evaledSiteFeatures[i] = siteFeatures[i](callingNode, input);
			}

			return evaledSiteFeatures;
		}
	}
}
