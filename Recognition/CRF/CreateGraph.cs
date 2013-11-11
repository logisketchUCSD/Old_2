/**
 * File: CreateGraph.cs
 * 
 * Authors: Aaron Wolin, Devin Smith, Jason Fennell, and Max Pflueger (Sketchers 2006).
 *          Code expanded by Anton Bakalov (Sketchers 2007).
 *          Harvey Mudd College, Claremont, CA 91711.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */

using System;
using ConverterXML;
using Sketch;
using Featurefy;
//using System.Threading; // FOR AN UGLY HACK
using System.Security.Cryptography;

namespace CRF
{
	// Temporary placeholder!!!!!!  Should not be a completely separate class!
	// Or maybe is just a toolkit
	public class CreateGraph
	{
        /// <summary>
        /// Due to the varying sketch sizes, we need a way to normalize the hard-coded
        /// distance thresholds in the CRF.  As of now, this is based on two sketch features,
        /// average arclength and average max distance between substrokes.  Since it is
        /// dependent on the sketch, we pass in a FeatureSketch object.
        /// </summary>
        /// <param name="dist">The originally hard-coded value</param>
        /// <returns>A normalized value appropriate for the stroke</returns>
        public static double normalizeDistance(double dist, FeatureSketch fs)
        {
            double arcLengthFactor = fs.AverageArcLength / 1600;
            double maxDistFactor = fs.AvgMaxDistBetweenSubstrokes / 6000;

            double result = dist * Math.Pow(arcLengthFactor, 0.5) * Math.Pow(maxDistFactor, 0.4);

            // DEBUG
            //System.Console.WriteLine("normDist: {0} to {1}", dist, result);

            return result;
        }

        //Just a quick substitute as an experimental control
        //public static double normalizeDistance2(double dist, FeatureSketch fs)
        //{
        //    return dist;
        //}

		public static double averageDistBetweenFrag(Node node1, Node node2)
		{
			return euclideanDistance( node1.fragFeat.Spatial.AveragePoint, node2.fragFeat.Spatial.AveragePoint);
		}


		public static double minTimeBetweenFrag(Node node1, Node node2)
		{
			double startTime1 = (double)node1.fragFeat.Spatial.FirstPoint.Time;
			double startTime2 = (double)node2.fragFeat.Spatial.FirstPoint.Time;
			double endTime1 = (double)node1.fragFeat.Spatial.LastPoint.Time;
			double endTime2 = (double)node2.fragFeat.Spatial.LastPoint.Time;

			// The two possible min differences between stroke times.
			// Always keep as a positive value
			double[] diff = new double[4];
			diff[0] = Math.Abs(((double)startTime1) - ((double)startTime2));
			diff[1] = Math.Abs(((double)startTime1) - ((double)endTime2));
			diff[2] = Math.Abs(((double)endTime1) - ((double)startTime2));
			diff[3] = Math.Abs(((double)endTime1) - ((double)endTime2));

			double minDiff = double.PositiveInfinity;

			for(int i = 0; i < 4; i++)
			{
				if(diff[i] < minDiff)
				{
					minDiff = diff[i];
				}
			}

			return (minDiff);
		}

		/// <summary>
		/// Calculates the Euclidean Distance Measure between two points
		/// </summary>
		/// <param name="p1">One endpoint</param>
		/// <param name="p2">Other endpoint</param>
		/// <returns>Returns the Euclidean Distance Measure Between Points p1 and p2</returns>
		public static double euclideanDistance(Point p1, Point p2)
		{
			double u = p1.X;
			double v = p1.Y;
			double p = p2.X;
			double q = p2.Y;

			return Math.Sqrt( (u - p) * (u - p) + (v - q) * (v - q) );
		}


		/// <summary>
		/// Calculates the Euclidean Distance Measure between two vectors of doubles
		/// </summary>
		/// <param name="u">One vector of doubles</param>
		/// <param name="v">Other vector of doubles</param>
		/// <returns>Returns the Euclidean Distance Measure between double vectors u and v</returns>
		public static double euclideanDistance(double[] u, double[] v)
		{
			double dist = 0.0;

			for(int i = 0; i < u.Length; ++i)
			{
				//dist += Math.Pow(u[i] - v[i],2.0);
				dist += ((u[i] - v[i]) * (u[i] - v[i]));
			}

			return Math.Sqrt(dist);
		}

		/// <summary>
		/// Finds the total arc length for the graph
		/// </summary>
		/// <param name="fragments">All of the stroke data for the graph</param>
		/// <returns>Total arc length</returns>
		public static double totArcLength(Node[] nodes)
		{
			double totLength = 0.0;

			for (int i = 0; i < nodes.Length; i++)
			{
				totLength += nodes[i].fragFeat.ArcLength.TotalLength;
			}

			return totLength;
		}

		/// <summary>
		/// Finds the total time between pairs of strokes, over the dataset
		/// </summary>
		/// <param name="fragments"></param>
		/// <returns></returns>
		public static double totTimeBetweenFrag(Node[] nodes)
		{
			double totTime = 0.0;
		
			for(int i = 0; i < nodes.Length; i++)
			{
				for(int j = i + 1;  j < nodes.Length; j++)
				{
					totTime += minTimeBetweenFrag(nodes[i],nodes[j]);
				}
			}

			return totTime;
		}

		/// <summary>
		/// Finds the average speed of over all strokes in the dataset
		/// </summary>
		/// <param name="fragments"></param>
		/// <returns></returns>
		public static double avgSpeedOfFrag(Node[] nodes)
		{
			double avgSpeed = 0.0;

			for(int i = 0; i < nodes.Length; i++)
			{
				avgSpeed += nodes[i].fragFeat.Speed.AverageSpeed;
				//avgSpeed += nodes[i].fragFeat.Speed.AvgSpeed;
			}

			return (avgSpeed / nodes.Length);
		}

		/// <summary>
		/// Creates a smooth transfer of the output from 1 to -1 as the input crosses the threshold
		/// </summary>
		/// <param name="input">The value to create the transfer on</param>
		/// <param name="threshold">The point around which a transfer is made</param>
        /// <param name="scale">The value by which we scale the function. The higher the value, 
        ///                     the higher the slope</param>>
		/// <returns>asymptotic from 1 to -1</returns>
		public static double tfLow(double input, double threshold, double scale)
		{
			//arctan provides the smooth transfer function I desire
			//arctan goes from -1.2 to +1.2 as the input goes from -3 to +3
			//the input will be scaled to map a change of 10% threshold to 0-3
            //Note: the default value of slope is 30.0
			return (-1.0 * Math.Atan((input-threshold)/threshold * scale)) / ( Math.PI / 2);
		}

		/// <summary>
		/// Creates a smooth transfer of the output from -1 to 1 as the input crosses the threshold
		/// </summary>
		/// <param name="input">The value to create the transfer on</param>
		/// <param name="threshold">The point around which a transfer is made</param>
        /// <param name="scale">The value by which we scale the function. The higher the value, 
        ///                     the higher the slope</param>>
		/// <returns>asymptotic from -1 to 1</returns>
		public static double tfHigh(double input, double threshold, double scale)
		{
			//arctan provides the smooth transfer function I desire
			//arctan goes from -1.2 to +1.2 as the input goes from -3 to +3
			//the input will be scaled to map a change of 10% threshold to 0-3
            //Note: the default value of scale is 30.0
			return (1.0 * Math.Atan((input-threshold)/threshold * scale)) / ( Math.PI / 2);
		}

		/// <summary>
		/// Given matrix A and vector v, return the vector of the product Av
		/// </summary>
		/// <param name="A">Input matrix</param>
		/// <param name="v">Input vector</param>
		/// <returns>Matrix vector product</returns>
		public static double[] matrixTimesVector(double[][] A, double[] v)
		{
			double[] result = new double[v.Length];

			for(int i = 0; i < A.Length; i++)
			{
				for(int j = 0; j < A[i].Length; j++)
				{
					result[i] += A[i][j] * v[j];
				}
			}
            
			return result;
		}
	}
}
