using System;
using Sketch;

namespace FeatureSpace
{
	/// <summary>
	/// ArcLength class. Creates arc length profiles for the Points
	/// given, where each Point has it's associated arc length from the
	/// start of the Stroke or Substroke to that Point.
	/// </summary>
	[Serializable]
	public class ArcLength
	{
		#region INTERNALS

		/// <summary>
		/// The Points to calculate arc length information for
		/// </summary>
		private Point[] points;

		/// <summary>
		/// The arc length profile for the set of Points
		/// </summary>
		private double[] arcLengthProfile;

		
		private object minx;
		private object maxx;

		private object miny;
		private object maxy;

		#endregion

		#region CONSTRUCTOR
		
		/// <summary>
		/// Creates the arc length information for a given set of Points.
		/// The computations are lazy, and variables are only initialized
		/// when called.
		/// </summary>
		/// <param name="points">Given Points</param>
		public ArcLength(Point[] points)
		{
			this.points = points;
			this.arcLengthProfile = null;
			this.minx = null;
			this.maxx = null;
			this.miny = null;
			this.maxy = null;
		}

		#endregion

		#region LENGTH COMPUTATIONS

		/// <summary>
		/// Computes the Euclidean distance between two Points.
		/// </summary>
		/// <param name="a">First Point's index</param>
		/// <param name="b">Second Point's index</param>
		/// <returns>The Euclidean distance between the given Points</returns>
		private double distance(int a, int b)
		{
			Point p1 = this.points[a];
			Point p2 = this.points[b];

			double x2 = Math.Pow(p1.X - p2.X, 2.0);
			double y2 = Math.Pow(p1.Y - p2.Y, 2.0);
			
			return Math.Sqrt(x2 + y2);
		}
		
		
		/// <summary>
		/// Computes the total arc length of these Points
		/// </summary>
		/// <returns>The total arc length of the points</returns>
		private double arcLength()
		{
			if(this.arcLengthProfile == null)
				this.arcLengthProfile = this.calcArcLengthProfile();

			return Profile[Profile.Length - 1];
		}

		
		/// <summary>
		/// Calculates the arc length profile of the stroke.
		/// </summary>
		/// <returns>The arc length profile of the stroke</returns>
		private double[] calcArcLengthProfile()
		{
			double[] profile = new double[this.points.Length];
			
			profile[0] = 0.0;
			for (int i = 1; i < profile.Length; i++)
			{
				profile[i] = profile[i - 1] + distance(i - 1, i);
			}
			
			return profile;
		}

		
		/// <summary>
		/// Computes the arc length between two indices of a stroke.
		/// </summary>
		/// <param name="a">Lower index</param>
		/// <param name="b">Upper index</param>
		/// <returns></returns>
		public double GetLength(int a, int b)
		{
			return (Profile[b] - Profile[a]);
		}

		#endregion

		#region MIN MAX COMP

		/// <summary>
		/// Find the minimum and maximum values for x and y
		/// </summary>
		private void computeMinMax()
		{
			minx = float.PositiveInfinity;
			maxx = float.NegativeInfinity;

			miny = float.PositiveInfinity;
			maxy = float.NegativeInfinity;

			//Loop through all the points
			float x, y;
			int length = this.points.Length;
			for(int i = 0; i < length; ++i)
			{
				x = this.points[i].X;
				y = this.points[i].Y;

				//Find the largest x
				if(x > (float)maxx)
					maxx = x;

				//Find the smallest x
				if(x < (float)minx)
					minx = x;

				//Find the largest y
				if(y > (float)maxy)
					maxy = y;

				//Find the smallest y
				if(y < (float)miny)
					miny = y;
			}
		}


		#endregion

		#region GETTERS & SETTERS

		/// <summary>
		/// Get the Width
		/// </summary>
		public float Width
		{
			get
			{
				if(minx == null || maxx == null || miny == null || maxy == null)
					this.computeMinMax();
				
				return (float)maxx - (float)minx;
			}
		}


		/// <summary>
		/// Get the Height
		/// </summary>
		public float Height
		{
			get
			{
				if(minx == null || maxx == null || miny == null || maxy == null)
					this.computeMinMax();
	
				return (float)maxy - (float)miny;
			}
		}


		/// <summary>
		/// Get the Diagonal
		/// </summary>
		public double Diagonal
		{
			get
			{
				float h = this.Height;
				float w = this.Width;
				return Math.Sqrt(h * h + w * w);
			}
		}
		

		/// <summary>
		/// Get the InkDensity
		/// </summary>
		public double InkDensity
		{
			get
			{
				return Math.Pow(TotalLength, 2.0) / (Width * Height + 1.0);
			}
		}

		/// <summary>
		/// Get the CircularInkDensity
		/// </summary>
		public double CircularInkDensity
		{
			get
			{
				return 4 * TotalLength * TotalLength / (Math.PI * (this.Width * this.Width + this.Height * this.Height) + 1);
			}
		}


		/// <summary>
		/// Returns the arc length profile for the set of Points.
		/// Calculates it once if no profile currently exists.
		/// </summary>
		public double[] Profile
		{
			get
			{
				if (arcLengthProfile == null)
				{
					this.arcLengthProfile = calcArcLengthProfile();
				}

				return this.arcLengthProfile;
			}
		}

		
		/// <summary>
		/// Returns the total arc length of the stroke.
		/// </summary>
		public double TotalLength
		{
			get
			{
				return arcLength();
			}
		}

		
		#endregion

		/// <summary>
		/// Compute all features in this object. Useful for serialization.
		/// </summary>
		internal void computeAll()
		{
			Object _;
			_ = TotalLength;
			_ = InkDensity;
			_ = CircularInkDensity;
			_ = Diagonal;
			_ = Height;
			_ = Width;
		}
	}
}
