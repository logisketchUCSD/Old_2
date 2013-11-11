using System;
using System.Collections.Generic;
using Sketch;
using MathNet.Numerics.LinearAlgebra;

namespace FeatureSpace
{
	/// <summary>
	/// Curvature class. Creates curvature profiles for the Points
	/// given, where each point can have a curvature in degrees/pixel.
	/// </summary>
	[Serializable]
	public class Curvature : LeastSquares
	{
		#region INTERNALS
		
		/// <summary>
		/// The Points to calculate curvature information for
		/// </summary>
		private List<Point> points;

		/// <summary>
		/// The arc length profile for the set of Points
		/// </summary>
		private double[] arcLengthProfile;

		/// <summary>
		/// The tangent angle profile for the set of Points
		/// </summary>
		private double[] tanAngleProfile;

        /// <summary>
        /// The angle at each point according to the cosine rule
        /// </summary>
        private double[] cosRuleAngleProfile = null;

		/// <summary>
		/// The average curvature for the set of Points
		/// </summary>
		private double avgCurv;

		/// <summary>
		/// The minimum curvature for the set of Points
		/// </summary>
		private double minCurv;

		/// <summary>
		/// The maximum curvature for the set of Points
		/// </summary>
		private double maxCurv;

		/// <summary>
		/// The (un-normalized) curvature profile for the set of Points
		/// </summary>
		private double[] curvProfile;

		/// <summary>
		/// The normalized curvature profile for the set of Points
		/// </summary>
		private double[] normCurvProfile;

		/// <summary>
		/// Average curvature profile
		/// </summary>
		private double[] absCurvProfile;

		/// <summary>
		/// Average (absolute value) curvature for the set of points
		/// </summary>
		private double? avgAbsCurv;

		/// <summary>
		/// Rubine parameters
		/// </summary>
		private double? subAbsCurv;
		private double? sumSquaredCurv;
		private double? sumSqrtCurv;

		/// <summary>
		/// The total angle traversed
		/// </summary>
		private double? _totalAngle = null;
        private double? _totalAbsAngle = null;
        private double? _totalSqaredAngle = null;

		/// <summary>
		/// Constant for a small end point window
		/// </summary>
		private const int SMALL_WINDOW = 5;

		/// <summary>
		/// Constant for a large end point window
		/// </summary>
		private const int LARGE_WINDOW = 15;

        /// <summary>
        /// Small neighborhood of context for curvature
        /// </summary>
        private int CONTEXT = 4;

        /// <summary>
        /// Makes life a bit easier.
        /// </summary>
        private const double twopi = Math.PI * 2;
		
		#endregion

		#region CONSTRUCTOR

		/// <summary>
		/// Creates the curvature information for a given set of Points.
		/// The computations are lazy, and variables are only initialized
		/// when called.
		/// </summary>
		/// <param name="points">Given Points</param>
		/// <param name="arcLengthProfile">The arc length profile for the given Points</param>
		/// <param name="tanAngleProfile">The tangent angle profile for the given Points</param>
		public Curvature(Point[] points, double[] arcLengthProfile, double[] tanAngleProfile)
		{
			this.points = removeDuplicates(points);
			this.arcLengthProfile = arcLengthProfile;
			this.tanAngleProfile = tanAngleProfile;

			this.curvProfile = null;
			this.normCurvProfile = null;
			this.avgCurv = -1.0;
			this.minCurv = Double.PositiveInfinity;
			this.maxCurv = Double.NegativeInfinity;

            if (this.points.Count > 20) CONTEXT = 2;
            else CONTEXT = 1;
		}

		private List<Point> removeDuplicates(Point[] points)
		{
			Set.Set<Point> newP = new Set.TreeSet<Point>(new Point.PointXYComparer());
			foreach (Point p in points)
			{
				newP.Add(p);
			}
			return newP.AsList();
		}

		#endregion

		#region CURVATURE COMPUTATIONS

        /// <summary>
        /// Find the direction of a substroke at a certain point.
        /// context = 0 just looks at the preceeding point to index.
        /// </summary>
        /// <param name="index">the index of the point, must be greater than 0</param>
        /// <returns>the direction of s at the given point</returns>
        private double direction(int index)
        {
            return direction(points[index], points[index + 1]);
        }

        private static double direction(Point p1, Point p2)
        {
            return direction(p1.Y, p2.Y, p1.X, p2.X);
        }

        /// <summary>
        /// Wrapper around atan2 in case i want to change my units
        /// currently: -PI to PI
        /// </summary>
        /// <param name="y1">Y value of first point</param>
        /// <param name="y2">Y value of second point</param>
        /// <param name="x1">X value of first point</param>
        /// <param name="x2">X value of second point</param>
        /// <returns></returns>
        private static double direction(double y1, double y2, double x1, double x2)
        {
            double atan = Math.Atan2(y2 - y1, x2 - x1);
            return atan;
        }

        private double curvature(int n)
        {
            return curvature(n, CONTEXT);
        }

		/// <summary>
		/// Gets the angle ABC
		/// </summary>
		/// <param name="Pa">A</param>
		/// <param name="Pb">B</param>
		/// <param name="Pc">C</param>
		/// <returns>The angle at B</returns>
		public static double cosineRuleAngle(Point Pa, Point Pb, Point Pc)
		{
			double a = Pb.distance(Pc);
			double b = Pa.distance(Pb);
			double c = Pa.distance(Pc);
			double quant = (Math.Pow(a, 2) + Math.Pow(b, 2) - Math.Pow(c, 2)) / (2 * a * b);

			if (quant < -1 || quant > 1)
			{
				if (Math.Abs(quant) - 1d < 1e-5)
				{
					if (quant < -1) return 0;
					else return Math.PI;
				}
				throw new Exception("Pa, Pb, Pc create a triangle that disobeys the triangle inequality.  Congratulations.");
			}
            
            // take cross product to find the sign of the angle
            int sign = ((Pb.X - Pa.X) * (Pc.Y - Pb.Y) - (Pc.X - Pb.X) * (Pb.Y - Pa.Y)) > 0 ? 1 : -1;

			return (Math.PI - Math.Acos(quant))*sign;
		}

        private void calculateAngleProfile()
        {
            if (cosRuleAngleProfile == null)
                cosRuleAngleProfile = new double[points.Count];
            for (int i = 1; i < points.Count - 1; i++)
            {
                cosRuleAngleProfile[i] = cosineRuleAngle(points[i - 1], points[i], points[i + 1]);
            }
        }

        /// <summary>
        /// Find the curvature of the stroke at a certain point
        /// </summary>
        /// <param name="n">the index of the point</param>
        /// <param name="k">number of points to take for context, >= 1</param>
        /// <returns></returns>
        private double curvature(int n, int k)
        {
			return curvature(n, k, calculateTotalAngle(n, k));
        }

		private double curvature(int n, int k, double sum)
		{
			while ((n + k) > arcLengthProfile.Length - 1 || (n - k) < 0)
				--k;
            double plen = (arcLengthProfile[n + k] - arcLengthProfile[n - k]);
            if (plen == 0) return 0;
            double curv = Math.Abs(sum) / plen;

            if (curv > maxCurv) maxCurv = curv;
            if (curv < minCurv) minCurv = curv;

            return curv;
		}

		private double absCurvature(int n)
		{
			return absCurvature(n, CONTEXT);
		}

		/// <summary>
		/// Like curvature, but without a sign
		/// </summary>
		/// <param name="n"></param>
		/// <param name="k"></param>
		/// <returns></returns>
		private double absCurvature(int n, int k)
		{
			return curvature(n, k, calculateTotalAbsAngle(n, k));
		}

        /// <summary>
        /// Shifts a given angle into the range -PI to PI
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        private static double phi(double angle)
        {
            while (angle > Math.PI) angle -= twopi;
            while (angle < -Math.PI) angle += twopi;
            return angle;
        }


		/// <summary>
		/// Compute the average curvature of the set of Points.
		/// Does not take into account a certain number of Points on each end, since these
		/// sections are known to contain hooks.
		/// </summary>
		/// <returns>The average curvature</returns>
		private double averageCurvature()
		{
			double avgCurv = 0.0;

			// Don't count any points that were at the ends of the stroke
			int endPtWindow;
			if (this.points.Count > 20 && this.points.Count < 60)
				endPtWindow = SMALL_WINDOW;
			else if (this.points.Count >= 60)
				endPtWindow = LARGE_WINDOW;
			else
				endPtWindow = 1;

			for (int i = endPtWindow; i < Profile.Length - endPtWindow; i++)
			{
				avgCurv += Profile[i];
			}

			return avgCurv / ((double)Profile.Length - (2 * endPtWindow));
		}

		private double averageAbsCurvature()
		{
			double avgCurv = 0.0;
			// Don't count any points that were at the ends of the stroke
			int endPtWindow;
			if (this.points.Count > 20 && this.points.Count < 60)
				endPtWindow = SMALL_WINDOW;
			else if (this.points.Count >= 60)
				endPtWindow = LARGE_WINDOW;
			else
				endPtWindow = 1;

			for (int i = endPtWindow; i < Profile.Length - endPtWindow; i++)
			{
				avgCurv += Math.Abs(Profile[i]);
			}

			return avgCurv / ((double)Profile.Length - (2 * endPtWindow));
		}
		
		/// <summary>
		/// Computes the curvature at each Point and creates a double array of it all.
		/// </summary>
		/// <returns>The double array of curvature values</returns>
		private double[] calcCurvProfile()
		{
			double[] profile = new double[points.Count];
			
			for (int i = CONTEXT; i < points.Count-CONTEXT; i++)
			{
        		profile[i] = curvature(i);
			}

            if (Double.IsNegativeInfinity(maxCurv)) maxCurv = 0.0;
            if (Double.IsPositiveInfinity(minCurv)) minCurv = maxCurv;

			return profile;
			//return smoothFilter(profile);
		}

		private double[] calcAbsCurvProfile()
		{
			double[] profile = new double[points.Count];

			for(int i = CONTEXT; i < points.Count - CONTEXT; ++i)
				profile[i] = absCurvature(i);
			return profile;
		}

		
		/// <summary>
		/// Computes the normalized curvature at each point and creates a double 
		/// array of it all.
		/// </summary>
		/// <returns>The double array of curvature values</returns>
		private double[] calcNormCurvProfile()
		{
			double[] profile = new double[this.Profile.Length];
			
			for (int i = 0; i < profile.Length; i++)
			{
				profile[i] = Profile[i] / AverageCurvature;
			}

			return profile;
		}


		private double[] smoothFilter(double[] profile)
		{
			double[] smoothProfile = new double[profile.Length];

			smoothProfile[0] = profile[0];
			
			for (int i = 1; i < profile.Length - 1; i++)
			{
				smoothProfile[i] = (profile[i - 1] + profile[i + 1]) / 2.0;
			}

			smoothProfile[smoothProfile.Length - 1] = profile[profile.Length - 1];
			
			return smoothProfile;
		}

		/// <summary>
		/// Calculate the total angle traveled over some window
		/// </summary>
		/// <param name="n">Center point</param>
		/// <param name="k">Width</param>
		/// <returns></returns>
		private double calculateTotalAngle(int n, int k)
		{
			double sum = 0;
			List<double> ds = new List<double>();

            if (cosRuleAngleProfile == null)
                calculateAngleProfile();

			for (int i = n - k + 1; i <= n + k; i++)
            {
				if (i < points.Count - 1)
				{
					double angle = cosRuleAngleProfile[i];
					ds.Add(angle);
				}
            }

            for (int i = 0; i < ds.Count-1; i++)
            {
                //sum += phi(ds[i + 1] - ds[i]);
				sum += phi(ds[i]);
            }
			return sum;
		}


		/// <summary>
		/// Calculate the total absolute angle traveled over some window
		/// </summary>
		/// <param name="n">Center point</param>
		/// <param name="k">Width</param>
		/// <returns></returns>
		private double calculateTotalAbsAngle(int n, int k)
		{
			double sum = 0;
			List<double> ds = new List<double>();

            if (cosRuleAngleProfile == null)
                calculateAngleProfile();

			for (int i = n - k + 1; i <= n + k; i++)
			{
				if (i < points.Count - 1)
				{
					double angle = Math.Abs(cosRuleAngleProfile[i]);
					ds.Add(angle);
				}
			}

			for (int i = 0; i < ds.Count - 1; i++)
			{
				sum += phi(ds[i]);
			}
			return sum;
		}
        #endregion

        #region GETTERS & SETTERS

        /// <summary>
        /// Returns the average curvature for the set of Points.
        /// Calculates it once if no average curvature currently exists.
        /// </summary>
        public double AverageCurvature
		{
			get
			{
				if (this.avgCurv == -1.0)
				{
					this.avgCurv = averageCurvature();
				}

				return this.avgCurv;
			}
		}


		/// <summary>
		/// Returns the minimum curvature for the set of Points.
		/// Calculates it once if no minimum curvature currently exists.
		/// </summary>
		public double MinimumCurvature
		{
			get
			{
				if (this.minCurv == Double.PositiveInfinity)
				{
					this.curvProfile = calcCurvProfile();
					//this.newCurvCalculator();
				}

				return this.minCurv;
			}
		}


		/// <summary>
		/// Returns the maximum curvature for the set of Points.
		/// Calculates it once if no maximum curvature currently exists.
		/// </summary>
		public double MaximumCurvature
		{
			get
			{
				if (this.maxCurv == Double.NegativeInfinity)
				{
					this.curvProfile = calcCurvProfile();
					//this.newCurvCalculator();
				}

				return this.maxCurv;
			}
		}
		

		/// <summary>
		/// Returns the (un-normalized) curvature profile for the set of Points.
		/// Calculates it once if no profile currently exists.
		/// </summary>
		public double[] Profile
		{
			get
			{
				if (this.curvProfile == null)
				{
					this.curvProfile = calcCurvProfile();
					//this.newCurvCalculator();
				}

				return this.curvProfile;
			}
		}

		/// <summary>
		/// The absolute curvature profile for the set of points
		/// </summary>
		public double[] AbsProfile
		{
			get
			{
				if (absCurvProfile == null)
					absCurvProfile = calcAbsCurvProfile();
				return absCurvProfile;
			}
		}

		/// <summary>
		/// Returns the normalized curvature profile for the set of Points.
		/// Calculates it once if no profile currently exists.
		/// </summary>
		public double[] NormProfile
		{
			get
			{
				if (this.normCurvProfile == null)
				{
					this.normCurvProfile = calcNormCurvProfile();
				}

				return this.normCurvProfile;
			}
		}

		/// <summary>
		/// Get the total curvature of this stroke. May, in fact, be an average of some sort.
		/// </summary>
		public double TotalCurvature
		{
			get
			{
				return curvature(points.Count / 2, Math.Max((points.Count / 2)-SMALL_WINDOW/2, 0));
			}
		}

		/// <summary>
		/// Get the total angle traversed by this stroke
		/// </summary>
		public double TotalAngle
		{
			get
			{
				if (_totalAngle == null)
					_totalAngle = calculateTotalAngle(points.Count/2, points.Count/2 - SMALL_WINDOW);
				return (double)_totalAngle;
			}
		}

        /// <summary>
        /// Get the sum of the absolute value of the angles between the points of this stroke.
        /// </summary>
        public double TotalAbsAngle
        {
            get
            {
                if (_totalAbsAngle == null)
                {
                    if (cosRuleAngleProfile == null)
                        calculateAngleProfile();
                    _totalAbsAngle = 0d;
                    for (int i = 1; i < points.Count - 1; i++)
                        _totalAbsAngle += Math.Abs(cosRuleAngleProfile[i]);
                }
                return _totalAbsAngle.Value;
            }
        }

        /// <summary>
        /// return sum of squares of angles between points.
        /// </summary>
        public double TotalSquaredAngle
        {
            get
            {
                if (_totalSqaredAngle == null)
                {
                    if (cosRuleAngleProfile == null)
                        calculateAngleProfile();

                    _totalSqaredAngle = 0d;
                    for (int i = 1; i < points.Count - 1; i++)
                        _totalSqaredAngle += cosRuleAngleProfile[i] * cosRuleAngleProfile[i];
                }
                return _totalSqaredAngle.Value;
            }
        }

		/// <summary>
		/// Get the average absolute curvature traveled by this stroke
		/// </summary>
		public double AverageAbsCurvature
		{
			get
			{
				if (avgAbsCurv == null)
					avgAbsCurv = averageAbsCurvature();
				return avgAbsCurv.Value;
			}
		}

		/// <summary>
		/// Sum of the absolute curvatures
		/// </summary>
		public double SumAbsCurvature
		{
			get
			{
				if (subAbsCurv == null)
				{
					subAbsCurv = 0.0;
					foreach(double curv in Profile)
						subAbsCurv += Math.Abs(curv);
				}
				return subAbsCurv.Value;
			}
		}

		/// <summary>
		/// Sum of the squared curvatures
		/// </summary>
		public double SumSquaredCurvature
		{
			get
			{
				if (sumSquaredCurv == null)
				{
					sumSquaredCurv = 0.0;
					foreach (double curv in Profile)
						sumSquaredCurv += Math.Pow(curv, 2);
				}
				return sumSquaredCurv.Value;
			}
		}

		/// <summary>
		/// Sum of the square roots of the curvatures
		/// </summary>
		public double SumSqrtCurvature
		{
			get
			{
				if (sumSqrtCurv == null)
				{
					sumSqrtCurv = 0.0;
					foreach (double curv in Profile)
						sumSqrtCurv += Math.Sqrt(Math.Abs(curv));
				}
				return sumSqrtCurv.Value;
			}
		}
		#endregion

		/// <summary>
		/// Compute all features. Useful for serialization.
		/// </summary>
		internal void computeAll()
		{
			Object _;
			_ = SumSqrtCurvature;
			_ = SumSquaredCurvature;
			_ = SumAbsCurvature;
			_ = AverageAbsCurvature;
			_ = TotalSquaredAngle;
			_ = TotalAngle;
			_ = TotalAbsAngle;
			_ = TotalCurvature;
			_ = Profile;
			_ = AbsProfile;
			_ = NormProfile;
		}
	}
}
