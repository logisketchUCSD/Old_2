using System;

namespace FeatureSpace
{
	/// <summary>
	/// LeastSquares class. Contains methods for calculating the least squares
	/// fit for a line and for a circle.
	/// </summary>
	[Serializable]
	public class LeastSquares
	{
		#region LEAST SQUARES LINE

		/// <summary>
		/// Finds the least squares fit parameters for a line of type y = mx + b.
		/// </summary>
		/// <param name="points">Points to fit a least squares line to</param>
		/// <param name="m">Slope of the line</param>
		/// <param name="b">Vertical shift of the line</param>
		/// <returns>Error of the line fit (actual / theoretical)</returns>
		public static double leastSquaresLineFit(System.Drawing.PointF[] points, out double m, out double b)
		{
			return leastSquaresLineFit(points, 0, points.Length - 1, out m, out b);
		}
		
		
		/// <summary>
		/// Finds the least squares fit parameters for a line of type y = mx + b.
		/// </summary>
		/// <param name="points">Points to fit a least squares line to</param>
		/// <param name="startIndex">Start index of the points to use</param>
		/// <param name="endIndex">End index of the points to use</param>
		/// <param name="m">Slope of the line</param>
		/// <param name="b">Vertical shift of the line</param>
		/// <returns>Error of the line fit (actual / theoretical)</returns>
		public static double leastSquaresLineFit(System.Drawing.PointF[] points, int startIndex, int endIndex, out double m, out double b)
		{
			int n = endIndex - startIndex + 1;
			
			if (startIndex == endIndex)
			{
				m = Double.PositiveInfinity;
				b = 0.0;
				return 0.0;
			}

			double sumX = 0.0;
			double sumY = 0.0;
			double sumXX = 0.0;
			double sumYY = 0.0;
			double sumXY = 0.0;
			
			double sumDist  = 0.0;
			double errOfFit = 0.0;
			
			// Calculate the sums
			for (int i = startIndex; i <= endIndex; i++)
			{
				double currX = points[i].X;
				double currY = points[i].Y;

				sumX += currX;
				sumXX += (currX * currX);

				sumY += currY;
				sumYY += (currY * currY);
			
				sumXY += (currX * currY);
			}

			// Denominator
			double denom = ((double)n * sumXX) - (sumX * sumX);

			// Make sure we don't have a divide by 0 error
			if (denom != 0.0)
			{
				// Slope
				m = (double)((n * sumXY) - (sumX * sumY)) / denom;
				// Shift
				b = (double)((sumY * sumXX) - (sumX * sumXY)) / denom;

				for (int i = startIndex; i <= endIndex; i++)
				{
					double y = (m * points[i].X) + b;
					
					// a = -m * b, b = -a / m, c = -shift / b
					// Distance to line = |ax0 + by0 + c| / Sqrt(a^2 + b^2)
					// http://mathworld.wolfram.com/Point-LineDistance2-Dimensional.html
					
					// Let this (temp variable) b = 1.0
					double B = 1.0;
					double A = (-1.0 * m) * B;
					double C = (-1.0 * b) / B;

					double d = Math.Abs((A * points[i].X) + (B * points[i].Y) + C) / Math.Sqrt((A * A) + (B * B));

					sumDist += d;
				}
			}
			else
			{
				m = Double.PositiveInfinity;
				b = 0.0;
				
				double avgX = 0.0;
				for (int i = startIndex; i <= endIndex; i++)
				{
					avgX += points[i].X;
				}

				avgX /= (double)(endIndex - startIndex);
				
				for (int i = startIndex; i <= endIndex; i++)
				{
					sumDist += Math.Abs(points[i].X - avgX) / Math.Sqrt(avgX);
				}
			}
				
			errOfFit = sumDist / (double)(endIndex - startIndex);
			
			// Returns error of fit
			return errOfFit;
		}


		/// <summary>
		/// Finds the least squares fit parameters for a line of type y = mx + b.
		/// </summary>
		/// <param name="points">Points to fit a least squares line to</param>
		/// <param name="m">Slope of the line</param>
		/// <param name="m_top">Numerator of the slope</param>
		/// <param name="m_bot">Denominator of the slope</param>
		/// <param name="b">Vertical shift of the line</param>
		/// <returns>Error of the line fit (actual / theoretical)</returns>
		public static double leastSquaresLineFit(System.Drawing.PointF[] points, out double m, out double m_top, 
			out double m_bot, out double b)
		{
			return leastSquaresLineFit(points, 0, points.Length - 1, out m, out m_top, out m_bot, out b);
		}
		
		
		/// <summary>
		/// Finds the least squares fit parameters for a line of type y = mx + b.
		/// </summary>
		/// <param name="points">Points to fit a least squares line to</param>
		/// <param name="startIndex">Start index of the points to use</param>
		/// <param name="endIndex">End index of the points to use</param>
		/// <param name="m">Slope of the line</param>
		/// <param name="m_top">Numerator of the slope</param>
		/// <param name="m_bot">Denominator of the slope</param>
		/// <param name="b">Vertical shift of the line</param>
		/// <returns>Error of the line fit (actual / theoretical)</returns>
		public static double leastSquaresLineFit(System.Drawing.PointF[] points, int startIndex, int endIndex, 
			out double m, out double m_top, out double m_bot, out double b)
		{
			int n = endIndex - startIndex + 1;
			
			if (startIndex == endIndex)
			{
				m = m_top = Double.PositiveInfinity;
				m_bot = 1.0;
				b = 0.0;
				return 0.0;
			}

			double sumX = 0.0;
			double sumY = 0.0;
			double sumXX = 0.0;
			double sumYY = 0.0;
			double sumXY = 0.0;
			
			double sumDist  = 0.0;
			double errOfFit = 0.0;
			
			// Calculate the sums
			for (int i = startIndex; i <= endIndex; i++)
			{
				double currX = points[i].X;
				double currY = points[i].Y;

				sumX += currX;
				sumXX += (currX * currX);

				sumY += currY;
				sumYY += (currY * currY);
			
				sumXY += (currX * currY);
			}

			// Denominator
			double denom = ((double)n * sumXX) - (sumX * sumX);

			// Make sure we don't have a divide by 0 error
			if (denom != 0.0)
			{
				// Slope
				m_top = (double)((n * sumXY) - (sumX * sumY));
				m_bot = denom;
				
				m = m_top / m_bot;

				// Shift
				b = (double)((sumY * sumXX) - (sumX * sumXY)) / denom;

				for (int i = startIndex; i <= endIndex; i++)
				{
					double y = (m * points[i].X) + b;
					
					// a = -m * b, b = -a / m, c = -shift / b
					// Distance to line = |ax0 + by0 + c| / Sqrt(a^2 + b^2)
					// http://mathworld.wolfram.com/Point-LineDistance2-Dimensional.html
					
					// Let this (temp variable) b = 1.0
					double B = 1.0;
					double A = (-1.0 * m) * B;
					double C = (-1.0 * b) / B;

					double d = Math.Abs((A * points[i].X) + (B * points[i].Y) + C) / Math.Sqrt((A * A) + (B * B));

					sumDist += d;
				}
			}
			else
			{
				m = m_top = Double.PositiveInfinity;
				m_bot = 1.0;
				b = 0.0;
				
				double avgX = 0.0;
				for (int i = startIndex; i <= endIndex; i++)
				{
					avgX += points[i].X;
				}

				avgX /= (double)(endIndex - startIndex);
				
				for (int i = startIndex; i <= endIndex; i++)
				{
					sumDist += Math.Abs(points[i].X - avgX) / Math.Sqrt(avgX);
				}
			}
				
			errOfFit = sumDist / (double)(endIndex - startIndex);
			
			// Returns error of fit
			return errOfFit;
		}

		#endregion

		#region LEAST SQUARES CIRCLE

		/// <summary>
		/// Finds the LSQ Circle fit for a series of points and returns the uncertainty.
		/// Uses out variables x0, y0, and r to represent a circle calculation.
		/// Circle: r = Math.Sqrt((x - x0)^2 + (y - y0)^2)  
		/// </summary>
		/// <param name="points">Points to use for the LSQ fit</param>
		/// <param name="x0">The circle's center point x-coordinate</param>
		/// <param name="y0">The circle's center point y-coordinate</param>
		/// <param name="r">The circle's radius</param>
		/// <returns>Error of the fit (actual / theoretical)</returns>
		public static double leastSquaresCircleFit(System.Drawing.PointF[] points, out double x0, out double y0, out double r)
		{
			return leastSquaresCircleFit(points, 0, points.Length - 1, out x0, out y0, out r);
		}
		
		
		/// <summary>
		/// Finds the LSQ Circle fit for a series of points and returns the uncertainty.
		/// Uses out variables x0, y0, and r to represent a circle calculation.
		/// Circle: r = Math.Sqrt((x - x0)^2 + (y - y0)^2)  
		/// </summary>
		/// <param name="points">Points to use for the LSQ fit</param>
		/// <param name="startIndex">Start index of the points to use</param>
		/// <param name="endIndex">End index of the points to use</param>
		/// <param name="x0">The circle's center point x-coordinate</param>
		/// <param name="y0">The circle's center point y-coordinate</param>
		/// <param name="r">The circle's radius</param>
		/// <returns>Error of the fit (actual / theoretical)</returns>
		public static double leastSquaresCircleFit(System.Drawing.PointF[] points, int startIndex, int endIndex, 
			out double x0, out double y0, out double r)
		{
			if (startIndex == endIndex)
			{
				x0 = y0 = r = 0.0;
				return 0.0;
			}

			/*
			 * Calculation will be of the form:
			 * 
			 * [A B C]^T = M^-1 * V
			 * 
			 * [A]   [ sum (xi^2 + yi^2)^2    sum (xi^2 + yi^2)*xi   sum (xi^2 + yi^2)*yi ]^-1  [ sum (xi^2 + yi^2) ]
			 * [B] = [ sum (xi^2 + yi^2)*xi   sum (xi^2)             sum (xi * yi)        ]  *  [ sum (xi)          ]
			 * [C]   [ sum (xi^2 + yi^2)*yi   sum (xi * yi)          sum (yi^2)           ]     [ sum (yi)          ]
			 * 
			 * http://www.orbitals.com/self/least/least.htm
			 */ 
			 
			MathNet.Numerics.LinearAlgebra.Matrix ABC = new MathNet.Numerics.LinearAlgebra.Matrix(3, 1, 0.0);
			MathNet.Numerics.LinearAlgebra.Matrix M = new MathNet.Numerics.LinearAlgebra.Matrix(3, 3, 0.0);
			MathNet.Numerics.LinearAlgebra.Matrix V = new MathNet.Numerics.LinearAlgebra.Matrix(3, 1, 0.0);
			
			for (int i = startIndex; i <= endIndex; i++)
			{
				double x = points[i].X;
				double x2 = x * x;

				double y = points[i].Y;
				double y2 = y * y;

				double addx2y2 = x2 + y2;

				M[0,0] += addx2y2 * addx2y2;
				M[0,1] += addx2y2 * x;
				M[0,2] += addx2y2 * y;
				M[1,0] += addx2y2 * x;
				M[1,1] += x2;
				M[1,2] += x * y;
				M[2,0] += addx2y2 * y;
				M[2,1] += x * y;
				M[2,2] += y2;

				V[0,0] += addx2y2;
				V[1,0] += x;
				V[2,0] += y;
			}
			
			if (M.LUDecomposition.IsNonSingular)
			{
				ABC = M.Inverse() * V;
			}
			else
			{
				//Console.WriteLine("Matrix M = " + M.ToString() + " has no inverse");
				
				double m, b;
				m = b = x0 = y0 = r = 0.0;
				return leastSquaresLineFit(points, out m, out b);
			}

			/*
			 * x0 = -B / (2 * A);
			 * y0 = -C / (2 * A);
			 * r  = Math.Sqrt((4 * A) + (B * B) + (C * C)) / (2 * A);
			 */

			x0 = -ABC[1,0] / (2 * ABC[0,0]);
			y0 = -ABC[2,0] / (2 * ABC[0,0]);
			r  = Math.Abs( Math.Sqrt( (4 * ABC[0,0]) + (ABC[1,0] * ABC[1,0]) + (ABC[2,0] * ABC[2,0]) ) / (2 * ABC[0,0]) );

			// Check that we didn't get any divide-by-0 errors
			if (ABC[0,0] == 0.0 || 
				// For some reason this next part doesn't want to work...
				x0 == System.Double.NegativeInfinity || x0 == System.Double.PositiveInfinity || x0 == System.Double.NaN ||
				y0 == System.Double.NegativeInfinity || y0 == System.Double.PositiveInfinity || y0 == System.Double.NaN ||
				r  == System.Double.NegativeInfinity || r  == System.Double.PositiveInfinity || r  == System.Double.NaN)
			{
				x0 = y0 = r = 0.0;
				return 0.0;
			}

			// Calculate the error of the fit
			double sumDist  = 0.0;
			double errOfFit = 0.0;

			for (int i = startIndex; i <= endIndex; i++)
			{
				// Distance to circumference = |(distance between point and center) - radius|
				double d = Math.Abs( Math.Sqrt(Math.Pow(points[i].X - x0, 2) + Math.Pow(points[i].Y - y0, 2)) - r );

				sumDist += d;
			}
				
			errOfFit = sumDist /= (double)(endIndex - startIndex);

			// Uncertainty or Error
			return errOfFit;
		}

		#endregion
	}
}
