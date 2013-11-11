using System;
using Sketch;
//using MathNet.Numerics;
using statistic;

namespace FeatureSpace
{
	/// <summary>
	/// Slope class. Creates slope information for the Points given.
	/// Generates a tangent angle profile, as well as least squares line and 
	/// circle fits.
	/// </summary>
	[Serializable]
	public class Slope : LeastSquares
	{
		#region STRUCTS

		/// <summary>
		/// Struct holding the information concerning how a least-squares line
		/// fits over the given Points.
		/// </summary>
		[Serializable]
		public struct LsqLineFit
		{
			/// <summary>
			/// Slope of the least squares line.
			/// </summary>
			public double M;

			/// <summary>
			/// Vertical shift of the least squares line.
			/// </summary>
			public double B;

			/// <summary>
			/// Error of the least squares line fit. (actual / theoretical) * 100
			/// </summary>
			public double Error;
		}

		/// <summary>
		/// Struct holding the information concerning how a least-squares circle
		/// fits over the given Points.
		/// </summary>
		[Serializable]
		public struct LsqCircleFit
		{
			/// <summary>
			/// Center's x-coordinate of the least squares circle.
			/// </summary>
			public double X0;

			/// <summary>
			/// Center's y-coordinate of the least squares circle.
			/// </summary>
			public double Y0;

			/// <summary>
			/// Least squares circle's radius.
			/// </summary>
			public double R;

			/// <summary>
			/// Error of the least squares circle fit. (actual / theoretical) * 100
			/// </summary>
			public double Error;
		}

		#endregion
		
		#region INTERNALS
		
		/// <summary>
		/// The Points to calculate angle information for
		/// </summary>
		private Point[] points;

		/// <summary>
		/// The simplified (x, y) point array for calculation purposes
		/// </summary>
		private System.Drawing.PointF[] simplePoints;

		/// <summary>
		/// The tangent angle profile for the set of Points
		/// </summary>
		private double[] tanAngleProfile;

		/// <summary>
		/// An uncleaned tangent angle profile. Used for debugging purposes.
		/// </summary>
		private double[] uncleanProfile;

		/// <summary>
		/// The Least Squares line fit for the set of Points
		/// </summary>
		private LsqLineFit lineFit;

		/// <summary>
		/// The Least Squares circle fit for the set of Points
		/// </summary>
		private LsqCircleFit circleFit;

		#endregion

		#region CONSTRUCTOR

		/// <summary>
		/// Creates the tangent angle and least squares information for a given set of Points.
		/// The tangent angle computations are lazy, and variables are only initialized
		/// when called.
		/// </summary>
		/// <param name="points">Given Points</param>
		public Slope(Point[] points)
		{
			this.points = points;
			this.tanAngleProfile = null;
			
			initSimplePts();
			initLsq();
		}

		#endregion

		#region INITIALIZATION

		/// <summary>
		/// Initializes the System.Drawing.PointF[] array we will be using to simplify our calculations.
		/// </summary>
		private void initSimplePts()
		{
			// Initialize the simplified (x, y) points array we will be using
			System.Drawing.PointF[] pts = new System.Drawing.PointF[this.points.Length];

			for (int i = 0; i < pts.Length; i++)
			{
				pts[i] = new System.Drawing.PointF((float)this.points[i].XmlAttrs.X, (float)this.points[i].XmlAttrs.Y);
			}
			
			this.simplePoints = pts;
		}
		
		
		/// <summary>
		/// Initialize (and calculate) our Least Squares line and circle fits for the entire set of points.
		/// </summary>
		private void initLsq()
		{
			this.lineFit = new LsqLineFit();
			this.circleFit = new LsqCircleFit();
			
			// Find the least squares line fit for the entire Point array
			double m, b;
			double err = leastSquaresLineFit(simplePoints, out m, out b);

			this.lineFit.M = m;
			this.lineFit.B = b;
			this.lineFit.Error = err;

			// Find the least squares circle fit for the entire Point array
			double x0, y0, r;
			err = leastSquaresCircleFit(simplePoints, out x0, out y0, out r);

			this.circleFit.X0 = x0;
			this.circleFit.Y0 = y0;
			this.circleFit.R = r;
			this.circleFit.Error = err;
		}

		#endregion
		
		#region SLOPE COMPUTATIONS

		/// <summary>
		/// Finds the tangent angle at the current index of the stroke.
		/// </summary>
		/// <param name="index">Index of the stroke</param>
		/// <returns>Tangent angle (in radians)</returns>
		private double findTangentAngle(int index)
		{
			// Use the points between (index - window) and (index + window)
			int window = 5;

			// Tangent Angle to return
			double tanAngle = 0.0;

			// Indices for the points on the stroke
			int startIndex = index - window;
			int endIndex = index + window;

			// Fix the start and end indices so that they are not out of bounds
			if (startIndex < 0)
				startIndex = 0;
			if (endIndex > this.points.Length - 1)
				endIndex = this.points.Length - 1;

			// Calculate the least squares line fit
			double m, m_top, m_bot, b;
			double lsqlErr = leastSquaresLineFit(simplePoints, startIndex, endIndex, out m, out m_top, out m_bot, out b);

			// Calculate the least squares circle fit
			double x0, y0, r;
			double lsqcErr = leastSquaresCircleFit(simplePoints, startIndex, endIndex, out x0, out y0, out r);

			// Use the better fit, favoring lines over circles
			if (lsqlErr < 3.3 || lsqlErr < lsqcErr)
			{
				if (m == System.Double.PositiveInfinity)
					tanAngle = Math.PI / 2;
				else
					tanAngle = Math.Atan2(m_top, m_bot);
			}
			else
			{
				circleTangent(simplePoints[index], x0, y0, r, out m, out m_top, out m_bot, out b);
				tanAngle = Math.Atan2(m_top, m_bot);
			}
			
			return tanAngle;
		}

		
		/// <summary>
		/// Computes the tangent angle at every Point.
		/// </summary>
		/// <returns>A double array of size points.Length that is indexed with the tangent angle information</returns>
		private double[] calcTanAngleProfile()
		{
			double[] profile = new double[this.points.Length];

			for (int i = 0; i < profile.Length; i++)
			{
				profile[i] = findTangentAngle(i);
			}

			this.uncleanProfile = profile;
			return cleanProfile(profile);
		}
	

		/// <summary>
		/// 
		/// </summary>
		/// <param name="profile"></param>
		/// <returns></returns>
		private double[] cleanProfile(double[] profile)
		{
			for (int i = 0; i < profile.Length - 1; i++)
			{
				double modTest = profile[i + 1] - profile[i];
				int divBy = (int)(Math.Abs(profile[i + 1] - profile[i]) / 2.5);

				if (modTest < -2.5)
					profile[i + 1] += (Math.PI * divBy);
				else if (modTest > 2.5)
					profile[i + 1] -= (Math.PI * divBy);
			}

			return profile;
		}
		

		/// <summary>
		/// Finds the tangent of a point on the given circle.
		/// </summary>
		/// <param name="p">Point on the circle</param>
		/// <param name="x0">Center x-coordinate of the circle</param>
		/// <param name="y0">Center y-coordinate of the circle</param>
		/// <param name="r">Radius of the circle</param>
		/// <param name="m">Slope of the tangent line</param>
		/// <param name="b">Vertical shift of the tangent line</param>
		private void circleTangent(System.Drawing.PointF p, double x0, double y0, double r, out double m, out double b)
		{
			// Slope through the center point
			double mcp = (p.Y - y0) / (p.X - x0);
			
			// Slope of the tangent, since mcp * mtgt = -1;
			double mtgt = -1 / mcp;

			m = mtgt;
			b = (mtgt * -p.X) + p.Y;
		}


		/// <summary>
		/// Finds the tangent of a point on the given circle.
		/// </summary>
		/// <param name="p">Point on the circle</param>
		/// <param name="x0">Center x-coordinate of the circle</param>
		/// <param name="y0">Center y-coordinate of the circle</param>
		/// <param name="r">Radius of the circle</param>
		/// <param name="m">Slope of the tangent line</param>
		/// <param name="m_top">Numerator of the slope, m</param>
		/// <param name="m_bot">Denominator of the slope, m</param>
		/// <param name="b">Vertical shift of the tangent line</param>
		private void circleTangent(System.Drawing.PointF p, double x0, double y0, double r, out double m, 
			out double m_top, out double m_bot, out double b)
		{
			// Slope through the center point
			double mcp = (p.Y - y0) / (p.X - x0);
			
			if (mcp == 0)
				mcp = 0.001;

			// Slope of the tangent, since mcp x mtgt = -1;
			double mtgt = -1 / mcp;

			m = mtgt;
			m_top = -1;
			m_bot = mcp;

			b = (mtgt * -p.X) + p.Y;
		}


		/// <summary>
		/// Compute the direction between the two points
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <returns></returns>
		private double direction(Point p1, Point p2)
		{
			//It should be as if p1 was at the origin
			int x = Convert.ToInt32(p2.X) - Convert.ToInt32(p1.X);
			//Use a -Y since we are not in cartiesian
			int y = Convert.ToInt32(p1.Y) - Convert.ToInt32(p2.Y);

			/*
			Console.WriteLine("p1 x:{0} y:{1}", p1.X, p1.Y);
			Console.WriteLine("p2 x:{0} y:{1}", p2.X, p2.Y);
			Console.WriteLine("x:{0} y:{1}", x, y);
			*/
		
			//Argument takes y then x
			return Math.Atan2((double)y, (double)x);
		}

		#endregion

		#region GETTERS & SETTERS

		/// <summary>
		/// Returns the tangent angle profile for the set of Points.
		/// Calculates it once if no profile currently exists.
		/// </summary>
		public double[] TanProfile
		{
			get
			{
				if (this.tanAngleProfile == null)
				{
					this.tanAngleProfile = calcTanAngleProfile();
				}

				return this.tanAngleProfile;
			}
		}

		/// <summary>
		/// Returns the avergage slope of the Points.
		/// Calculates it once if no average slope variable currently exists.
		/// </summary>
		public double AverageSlope
		{
			get
			{
				int length = this.TanProfile.Length;
				double totalSlope = 0.0;
				for(int i = 0; i < length; ++i)
					totalSlope += this.tanAngleProfile[i];

				return totalSlope / length;
			}
		}

		/// <summary>
		/// FILL ME IN
		/// </summary>
		public double Var
		{
			get
			{
				statistic.statistics test = new statistic.statistics(this.TanProfile);			
				return test.var();
			}
		}

		/// <summary>
		/// FILL ME IN
		/// </summary>
		public double Q1
		{
			get
			{
				statistic.statistics test = new statistic.statistics(this.TanProfile);			
				return test.Q1();
			}
		}

		/// <summary>
		/// FILL ME IN
		/// </summary>
		public double Q2
		{
			get
			{
				statistic.statistics test = new statistic.statistics(this.TanProfile);			
				return test.Q2();
			}
		}

		/// <summary>
		/// FILL ME IN
		/// </summary>
		public double Q3
		{
			get
			{
				statistic.statistics test = new statistic.statistics(this.TanProfile);			
				return test.Q3();
			}
		}

		/// <summary>
		/// FILL ME IN
		/// </summary>
		public double Range
		{
			get
			{
				statistic.statistics test = new statistic.statistics(this.TanProfile);			
				return test.range();
			}
		}

		/// <summary>
		/// FILL ME IN
		/// </summary>
		public double IQ
		{
			get
			{
				statistic.statistics test = new statistic.statistics(this.TanProfile);			
				return test.IQ();
			}
		}

		/// <summary>
		/// FILL ME IN
		/// </summary>
		public double MiddleOfRange
		{
			get
			{
				statistic.statistics test = new statistic.statistics(this.TanProfile);			
				return test.middle_of_range();
			}
		}

		/// <summary>
		/// FILL ME IN
		/// </summary>
		public double S
		{
			get
			{
				statistic.statistics test = new statistic.statistics(this.TanProfile);			
				return test.s();
			}
		}

		/// <summary>
		/// FILL ME IN
		/// </summary>
		public double Yule
		{
			get
			{
				statistic.statistics test = new statistic.statistics(this.TanProfile);			
				return test.YULE();
			}
		}


		/// <summary>
		/// Get the Direction
		/// </summary>
		public double Direction
		{
			get
			{
				return this.direction(this.points[0], this.points[this.points.Length - 1]);
			}
		}


		/// <summary>
		/// Returns the Least Squares line fit for the set of Points.
		/// </summary>
		public LsqLineFit LeastSquaresLineFit
		{
			get
			{
				return this.lineFit;
			}
		}


		/// <summary>
		/// Returns the Least Squares circle fit for the set of Points.
		/// </summary>
		public LsqCircleFit LeastSquaresCircleFit
		{
			get
			{
				return this.circleFit;
			}
		}


		#endregion

		/// <summary>
		/// Compute all features in this object. Useful for serialization.
		/// </summary>
		internal void computeAll()
		{
			Object _ = TanProfile;
		}
	}
}
