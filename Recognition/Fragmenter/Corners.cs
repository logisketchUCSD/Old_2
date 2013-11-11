using System;
using System.Collections.Generic;
using Sketch;
using Featurefy;

namespace Fragmenter
{
	/// <summary>
	/// Corners class. Used to find the corners of a FeatureStroke so we
	/// can fragment/segment the stroke.
	/// </summary>
	public class Corners : Featurefy.LeastSquares
	{
		#region INTERNALS

		/// <summary>
		/// The FeatureStroke to find corners for.
		/// </summary>
		private FeatureStroke fStroke;

		/// <summary>
		/// An enumeration for the type of the Fragment: Line or Arc.
		/// </summary>
		private enum FragmentType { Line, Arc };
	
		/// <summary>
		/// Constant for a small end point window
		/// </summary>
		private const int SMALL_WINDOW = 12;

		/// <summary>
		/// Constant for a large end point window
		/// </summary>
		private const int LARGE_WINDOW = 15;	
		
		#endregion
		
		#region CONSTRUCTOR

		/// <summary>
		/// Constructs a Corners object that we can find the corners for.
		/// </summary>
		/// <param name="fStroke">The FeatureStroke to find corners for</param>
		public Corners(FeatureStroke fStroke)
		{
			this.fStroke = fStroke;
		}
		
		#endregion

		#region FIND CORNERS

		/// <summary>
		/// We have two functions here so that we can use the reference variable temp to pass information about certain
		/// steps in the process back to the caller.
		/// </summary>
		/// <returns>The found corners of a FeatureStroke</returns>
		public int[] FindCorners()
		{
			int tmp = 0;
			return FindCorners(ref tmp);
		}

		/// <summary>
		/// Finds the Corners of this FeatureStroke
		/// </summary>
		/// <returns>The found corners of a FeatureStroke</returns>
		public int[] FindCorners(ref int temp) 
		{
			// Finds initial corners by seperately checking a speed and curvature threshold
			int[] initCorners = findInitCorners(0.25, 0.45, false);

			//int[] initCorners = findInitCorners(0.30, 0.20, false);
			//int[] initCorners = findInitCorners(0.10, 0.60, false);
			//int[] initCorners = findInitCorners(0.40, 0.30, false);
			//int[] initCorners = findInitCorners(0.90 * this.fStroke.Speed.AverageSpeed, this.fStroke.Curvature.AverageCurvature, false);

			temp += initCorners.Length;
			
			// Trims the close corners by a given distance threshold
			int[] trimmedCorners = trimCloseCorners(initCorners, 100);

			// Merges fragments by looking at the least squares fits between two fragments and seeing
			// if we can combine two fragments
			int[] mergedArcCorners = mergeByArc(trimmedCorners, 50.0);

			// Merges fragments by seeing if the length of one is less than a threshold of another
			int[] mergedLengthCorners = mergeByLength(mergedArcCorners, 0.20);
			
			//return mergedArcCorners;
			return mergedLengthCorners;
		}

		#endregion

		#region CORNER CALCULATIONS

		/// <summary>
		/// Find the initial corner estimations of a stroke, indicating likely places where a stroke can be split up into
		/// corresponding substrokes.
		/// </summary>
		/// <param name="speedThreshold">Percent of the average speed of the stroke</param>
		/// <param name="curveThreshold">Degrees per pixel</param>
		/// <param name="useBoth">If true, use both thresholds to determine if the point should be included in the initial
		/// set</param>
		/// <returns>The indices where the stroke should be split up</returns>
		private int[] findInitCorners(double speedThreshold, double curveThreshold, bool useBoth)
		{
			List<int> initCorners = new List<int>();
			
			int endPtWindow;
			if (this.fStroke.Points.Length > 20 && this.fStroke.Points.Length < 60)
				endPtWindow = SMALL_WINDOW;
			else if (this.fStroke.Points.Length >= 60)
				endPtWindow = LARGE_WINDOW;
			else
				endPtWindow = 0;
			
			// Add point indices that correspond to low speed
			for (int i = endPtWindow; i < this.fStroke.Speed.NormProfile.Length; i++)
			{
				if (this.fStroke.Speed.NormProfile[i] < speedThreshold) 
				{
					int loopExit;
					int minimaIndex = findLocalMinima(this.fStroke.Speed.NormProfile, speedThreshold, i, out loopExit);

					// Add the local minima (for speed) to the initial corners estimation, then increase i to start
					// again where the minima calculation exited
					if (!initCorners.Contains(minimaIndex))
						initCorners.Add(minimaIndex);
					
					i = loopExit;
				}
			}

			// Add point indices that correspond to high curvature
			for (int i = endPtWindow; i < this.fStroke.Curvature.Profile.Length; i++)
			{
				if (this.fStroke.Curvature.Profile[i] > curveThreshold) 
				{
					int loopExit;
					int maximaIndex = findLocalMaxima(this.fStroke.Curvature.Profile, curveThreshold, i, out loopExit);

					// Add the local maxima (for curvature) to the initial corners estimation, then increase i to start
					// again where the maxima calculation exited
					if (!initCorners.Contains(maximaIndex))
						initCorners.Add(maximaIndex);
					
					i = loopExit;
				}
			}

			// Sort the initial corner estimations
			initCorners.Sort();

			// If we should actually use both thresholds, remove all the points that don't meet the criteria
			// from initCorners
			if (useBoth)
			{
				List<int> redoneInit = new List<int>();

				foreach (int corner in initCorners)
				{
					if (this.fStroke.Speed.NormProfile[corner] < speedThreshold &&
						this.fStroke.Curvature.Profile[corner] < curveThreshold &&
						!redoneInit.Contains(corner))
					{
						redoneInit.Add(corner);
					}
				}

				initCorners = redoneInit;
				initCorners.Sort();
			}
			
			return initCorners.ToArray();
		}


		/// <summary>
		/// Removes corners from an array that are too close together. Also removes points that have
		/// slow speed but very low curvature.
		/// </summary>
		/// <param name="corners">The corners in a stroke</param>
		/// <param name="threshold">Threshold between indices (minimum distance between 2)</param>
		/// <returns>The trimmed corners array</returns>
		private int[] trimCloseCorners(int[] corners, int threshold)
		{
			// Check that there is an initial set of corners passed in
			if (corners == null || corners.Length == 0)
				return corners;
			
			List<int> trimmedCorners = new List<int>();
			List<int> currCluster    = new List<int>();
			
			int endPtWindow;
			if (this.fStroke.Points.Length > 20 && this.fStroke.Points.Length < 60)
				endPtWindow = SMALL_WINDOW;
			else if (this.fStroke.Points.Length >= 60)
				endPtWindow = LARGE_WINDOW;
			else
				endPtWindow = 0;
			
			// If there is only one corner the for-loop will exit immediately
			if (corners.Length == 1)
				currCluster.Add(corners[0]);

			// Goes through all the corner points and eliminates some points that are close together.
			// Also tries to eliminate "hooks" on strokes by trimming off any corners found too close to the
			// endpoints.
			for (int i = 0; i < corners.Length - 1; i++)
			{
				// If there aren't any corners in the current cluster, add the current one
				if (currCluster.Count == 0)
				{
					currCluster.Add(corners[i]);
				}
				
				double dist = distance(corners[i], corners[i + 1]);
				
				// Add the next corner to the current cluster if the distance between
				// the current corner and the next corner is less than some threshold
				if (dist < threshold)
				{
					currCluster.Add(corners[i + 1]);
				}

				// If the distance is greater than some threshold then we actually want to
				// add the "average" corner from the current cluster and restart the process
				else
				{
					// Get the middle index of the current cluster group
					int goodCorner = (int)currCluster[ (int)(currCluster.Count / 2.0) ];

					// If the split point is not located too close to the endpoints of the stroke
					if (goodCorner > endPtWindow && goodCorner < (this.fStroke.Points.Length - endPtWindow))
						trimmedCorners.Add(goodCorner);
					
					currCluster.Clear();
					currCluster.Add(corners[i + 1]);
				}
			}

			// Add the last corner from the last cluster
			if (currCluster.Count > 0)
			{
				int goodCorner = (int)currCluster[ (int)(currCluster.Count / 2.0) ];
				
				// If the split point is not located too close to the endpoints of the stroke
				if (goodCorner > endPtWindow && goodCorner < (this.fStroke.Points.Length - endPtWindow))
					trimmedCorners.Add(goodCorner);
			}		

			return trimmedCorners.ToArray();
		}


		/// <summary>
		/// Removes corners where from the inputted corners array where user specified speed and curvature
		/// thresholds are not met. In other words, we use this to combine our previous thresholds and see if certain
		/// corners fail to be included into both categories.
		/// 
		/// The basic idea is that you can relax thresholds here and see if some points fail miserably on
		/// a certain condition.
		/// 
		/// NOTE: I found that this provided bad results, so I don't actually use it but I've included
		/// in here just the same.
		/// </summary>
		/// <param name="corners">The corners in a stroke</param>
		/// <param name="speedThreshold">Speed the corner must be below</param>
		/// <param name="curvThreshold">Curvature the corner must be below</param>
		/// <returns>The trimmed corners array</returns>
		private int[] trimNoisyCorners(int[] corners, double speedThreshold, double curvThreshold)
		{
			// Check that there is an initial set of corners passed in
			if (corners == null || corners.Length == 0)
				return corners;
			
			List<int> quietedCorners = new List<int>();

			for (int i = 0; i < corners.Length; i++)
			{
				if ( this.fStroke.Speed.NormProfile[corners[i]] < speedThreshold && 
					 this.fStroke.Curvature.Profile[corners[i]] > curvThreshold )
					quietedCorners.Add(corners[i]);
			}

			return quietedCorners.ToArray();
		}


		/// <summary>
		/// Combine substrokes whose combined arc or line segments are within some
		/// error of their individual segments summed.
		/// </summary>
		/// <param name="corners">The corners in a stroke</param>
		/// <param name="errThreshold">The error allowance that a merged fragment must be below</param>
		/// <returns>The trimmed corners array</returns>
		private int[] mergeByArc(int[] corners, double errThreshold)
		{
			// Check that there is an initial set of corners passed in
			if (corners == null || corners.Length == 0)
				return corners;
			
			List<int> mergedCorners = new List<int>();
			
			// Creates instances for the least squares variables
			double m, b, x0, y0, r;
			double errLine1, errLine2, errCirc1, errCirc2;
			double totalLineErr, totalCircErr;

			// Initialize the point array we will be using
			System.Drawing.PointF[] pts = new System.Drawing.PointF[this.fStroke.Points.Length];
			for (int i = 0; i < pts.Length; i++)
			{
				pts[i] = new System.Drawing.PointF(this.fStroke.Points[i].X, this.fStroke.Points[i].Y);
			}
			
			// Make sure that we can have a valid window on our endpoints
			if (pts.Length > 30)
				mergedCorners.Add(10);
			else
				mergedCorners.Add(0);

			// Test the beginning stroke fragment up to the first found corner
			int lastCorner, currCorner;
			
			// Merge the strokes
			for (int i = 1; i <= corners.Length; i++)
			{
				lastCorner = (int)mergedCorners[mergedCorners.Count - 1];

				if (i != corners.Length)
				{
					currCorner = corners[i];
				}
				else
				{
					if (pts.Length > 30 && corners[corners.Length - 1] < pts.Length - 10)
						currCorner = pts.Length - 10;
					else
						currCorner = pts.Length - 1;
				}
				
				FragmentType fragType1;
				FragmentType fragType2;

				errLine1 = leastSquaresLineFit(pts, lastCorner, corners[i - 1], out m, out b);
				errLine2 = leastSquaresLineFit(pts, corners[i - 1], currCorner, out m, out b);
					
				errCirc1 = leastSquaresCircleFit(pts, lastCorner, corners[i - 1], out x0, out y0, out r);
				double percentCirc1 = this.fStroke.ArcLength.GetLength(lastCorner, corners[i - 1]) / (2 * Math.PI * r);
				
				errCirc2 = leastSquaresCircleFit(pts, corners[i - 1], currCorner, out x0, out y0, out r);
				double percentCirc2 = this.fStroke.ArcLength.GetLength(corners[i - 1], currCorner) / (2 * Math.PI * r);

				// Find the type of fragment for the first set of points
				if (errCirc1 < errLine1 && percentCirc1 > 0.10)
					fragType1 = FragmentType.Arc;
				else
					fragType1 = FragmentType.Line;
				
				// Find the type of fragment for the second set of points
				if (errCirc2 < errLine2 && percentCirc2 > 0.10)
					fragType2 = FragmentType.Arc;
				else
					fragType2 = FragmentType.Line;
				
				// Check that both fragments are the same type
				if (fragType1 == fragType2)
				{
					if (fragType1 == FragmentType.Line)
					{
						totalLineErr = leastSquaresLineFit(pts, lastCorner, currCorner, out m, out b);
						
						if (totalLineErr < 10.0)
							continue;
                        else if (totalLineErr > errThreshold || totalLineErr > (1.3 * (errLine1 + errLine2)))
							mergedCorners.Add(corners[i - 1]);
					}
					else
					{
						totalCircErr = leastSquaresCircleFit(pts, lastCorner, currCorner, out x0, out y0, out r);
						double percentTotalCirc = this.fStroke.ArcLength.GetLength(lastCorner, currCorner) / (2 * Math.PI * r);

						// We have a slightly higher allowance for arcs here, which favors larger arcs
						if (totalCircErr < 12.0)
							continue;
						else if (totalCircErr > errThreshold || totalCircErr > (1.4 * (errCirc1 + errCirc2)))
							mergedCorners.Add(corners[i - 1]);
					}
				}
				else
					mergedCorners.Add(corners[i - 1]);
			}

			// Remove the initially added lastCorners of either 0 or 10
			mergedCorners.Remove(0);
			mergedCorners.Remove(10);

			return mergedCorners.ToArray();
		} 


		/// <summary>
		/// Combines substrokes that are adjacent where one is less than some percentage threshold
		/// of the other's length.
		/// </summary>
		/// <param name="corners">The corners in a stroke</param>
		/// <param name="lengthThreshold">The percentage threshold that a substroke must be below to be merged</param>
		/// <returns>The trimmed corners array</returns>
		private int[] mergeByLength(int[] corners, double lengthThreshold)
		{
			// Check that there is an initial set of corners passed in
			if (corners == null || corners.Length == 0)
				return corners;
			
			List<int> mergedCorners = new List<int>();
			
			// Initialize the point array we will be using
			System.Drawing.PointF[] pts = new System.Drawing.PointF[this.fStroke.Points.Length];
			for (int i = 0; i < pts.Length; i++)
			{
				pts[i] = new System.Drawing.PointF(this.fStroke.Points[i].X, this.fStroke.Points[i].Y);
			}
			
			// Creates instances for the least squares variables
			double m, b, x0, y0, r;
			double errLine1, errLine2, errCirc1, errCirc2;
			double totalLineErr, totalCircErr;
			
			int lastCorner, currCorner;
			mergedCorners.Add(0);
			
			for (int i = 1; i <= corners.Length; i++)
			{
				lastCorner = (int)mergedCorners[mergedCorners.Count - 1];

				if (i != corners.Length)
				{
					currCorner = corners[i];
				}
				else
				{
					currCorner = this.fStroke.Points.Length - 1;
				}

				FragmentType fragType1;
				FragmentType fragType2;

				errLine1 = leastSquaresLineFit(pts, lastCorner, corners[i - 1], out m, out b);
				errLine2 = leastSquaresLineFit(pts, corners[i - 1], currCorner, out m, out b);
					
				errCirc1 = leastSquaresCircleFit(pts, lastCorner, corners[i - 1], out x0, out y0, out r);
				double percentCirc1 = this.fStroke.ArcLength.GetLength(lastCorner, corners[i - 1]) / (2 * Math.PI * r);
				
				errCirc2 = leastSquaresCircleFit(pts, corners[i - 1], currCorner, out x0, out y0, out r);
				double percentCirc2 = this.fStroke.ArcLength.GetLength(corners[i - 1], currCorner) / (2 * Math.PI * r);

				// Find the type of fragment for the first set of points
				if (errCirc1 < errLine1 && percentCirc1 > 0.10)
					fragType1 = FragmentType.Arc;
				else
					fragType1 = FragmentType.Line;
				
				// Find the type of fragment for the second set of points
				if (errCirc2 < errLine2 && percentCirc2 > 0.10)
					fragType2 = FragmentType.Arc;
				else
					fragType2 = FragmentType.Line;


				double length1 = distance(lastCorner, corners[i - 1]);
				double length2 = distance(corners[i - 1], currCorner);
				
				
				if (length1 / length2 < lengthThreshold || length2 / length1 < lengthThreshold)
				{
					// If the first fragment is larger than the second, use its type as the default
					if (length1 > length2)
					{
						if (fragType1 == FragmentType.Line)
						{
							totalLineErr = leastSquaresLineFit(pts, lastCorner, currCorner, out m, out b);
						
							if (totalLineErr > 1.1 * (errLine1 + errLine2))
								mergedCorners.Add(corners[i - 1]);
						}
						else
						{
							totalCircErr = leastSquaresCircleFit(pts, lastCorner, currCorner, out x0, out y0, out r);
						
							if (totalCircErr > 1.1 * (errCirc1 + errCirc2))
								mergedCorners.Add(corners[i - 1]);
						}
					}
					else
					{
						if (fragType2 == FragmentType.Line)
						{
							totalLineErr = leastSquaresLineFit(pts, lastCorner, currCorner, out m, out b);
						
							if (totalLineErr > 1.1 * (errLine1 + errLine2))
								mergedCorners.Add(corners[i - 1]);
						}
						else
						{
							totalCircErr = leastSquaresCircleFit(pts, lastCorner, currCorner, out x0, out y0, out r);
						
							if (totalCircErr > 1.1 * (errCirc1 + errCirc2))
								mergedCorners.Add(corners[i - 1]);
						}
					}
				}
				else
				{
					mergedCorners.Add(corners[i - 1]);
				}
			}
			
			// Remove the initially added corner 0 (used in conjunction with the lastCorner variable)
			mergedCorners.Remove(0);

			return mergedCorners.ToArray();
		}

		#endregion

		#region SLOPE & DISTANCE

		/// <summary>
		/// Computes the slope angle between two indices Arctan(rise over run)
		/// </summary>
		/// <param name="a">Index for first point</param>
		/// <param name="b">Index for second point</param>
		/// <returns>The slope angle between point[a] point[b] in degrees</returns>
		public double slopeAngle(int a, int b)
		{
			// Here are the representative points
			Point p1 = this.fStroke.Points[a];
			Point p2 = this.fStroke.Points[b];

			// Get the run and rise
			int deltaX = Convert.ToInt32(p2.X) - Convert.ToInt32(p1.X);
			// We must use a -Y since the coordinate system is not cartesion
			int deltaY = Convert.ToInt32(p2.Y) - Convert.ToInt32(p1.Y);
			deltaY *= -1;
			
			// We do not want to divide by zero.
			// Set deltaX to the minimum value if it is 0.
			if (deltaX == 0)
				deltaX = 1;

			// Console.WriteLine("p1 x:{0} y:{1}", p1.X, p1.Y);
			// Console.WriteLine("p2 x:{0} y:{1}", p2.X, p2.Y);
			// Console.WriteLine("x:{0} y:{1}", deltaX, deltaY);
			
			// Convert from radians to degrees
			double deg = Math.Atan2((double)deltaY, (double)deltaX) * (180 / Math.PI);
			
			return deg;
		}


		/// <summary>
		/// Finds the Euclidean distance between two Points.
		/// </summary>
		/// <param name="a">Lower index</param>
		/// <param name="b">Upper index</param>
		/// <returns></returns>
		private double distance(int a, int b)
		{
			Point p1 = this.fStroke.Points[a];
			Point p2 = this.fStroke.Points[b];

			double x2 = Math.Pow(p1.X - p2.X, 2.0);
			double y2 = Math.Pow(p1.Y - p2.Y, 2.0);
			
			return Math.Sqrt(x2 + y2);
		}	

		#endregion
		
		#region MINIMA & MAXIMA
		
		/// <summary>
		/// Finds a local minima within an array of doubles.
		/// </summary>
		/// <param name="values">Values to look for a local minima</param>
		/// <param name="threshold">The threshold line that the values must be less than before we assume a minima has been found</param>
		/// <param name="startIndex">Where to start looking for the minima in the values array</param>
		/// <param name="loopExit">Where we stopped looking</param>
		/// <returns>The local minima's index within values</returns>
		private int findLocalMinima(double[] values, double threshold, int startIndex, out int loopExit)
		{
			int i = startIndex;
			int minimaIndex = i;
			double minima = values[i];
			
			// Keep checking values until the points start increasing over the threshold again
			while (i < values.Length && values[i] <= threshold)
			{
				if (values[i] < minima)
				{
					minima = values[i];
					minimaIndex = i;
				}

				i++;
			}

			// Index where we have risen above the minima threshold again
			loopExit = i;

			return minimaIndex;
		}


		/// <summary>
		/// Finds local maxima within an array of doubles
		/// </summary>
		/// <param name="values">Values to look for a local maxima</param>
		/// <param name="threshold">The threshold line that the values must be greater than before we assume a maxima has been found</param>
		/// <param name="startIndex">Where to start looking for the maxima in the values array</param>
		/// <param name="loopExit">Where we stopped looking</param>
		/// <returns>The local maxima's index within values</returns>
		private int findLocalMaxima(double[] values, double threshold, int startIndex, out int loopExit)
		{
			int i = startIndex;
			int maximaIndex = i;
			double maxima = values[i];
			
			// Keep checking values until the points start decreasing under the threshold again
			while (i < values.Length && values[i] >= threshold)
			{
				if (values[i] > maxima)
				{
					maxima = values[i];
					maximaIndex = i;
				}

				i++;
			}

			// Index where we have fallen below the maxima threshold again
			loopExit = i;

			return maximaIndex;
		}

		#endregion
	}
}
