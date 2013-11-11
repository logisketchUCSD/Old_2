using System;
using Sketch;

namespace FeatureSpace
{
	/// <summary>
	/// Speed class. Creates a speed profile for the Points given,
	/// where each Point has a speed/point value.
	/// </summary>
	[Serializable]
	public class Speed
	{
		#region INTERNALS

		/// <summary>
		/// The Points to calculate speed information for
		/// </summary>
		private Point[] points;

		/// <summary>
		/// Average speed of the Points
		/// </summary>
		private double avgSpeed;

		/// <summary>
		/// Minimum speed of the Points
		/// </summary>
		private double minSpeed;

		/// <summary>
		/// Maximum speed of the Points
		/// </summary>
		private double maxSpeed;
		
		/// <summary>
		/// Un-normalized speed profile of the Points
		/// </summary>
		private double[] speedProfile;

		/// <summary>
		/// Normalized speed profile of the Points
		/// </summary>
		private double[] normSpeedProfile;

		/// <summary>
		/// Constant for a small end point window
		/// </summary>
		private const int SMALL_WINDOW = 5;

		/// <summary>
		/// Constant for a large end point window
		/// </summary>
		private const int LARGE_WINDOW = 15;
		
		#endregion

		#region CONSTRUCTOR

		/// <summary>
		/// Creates the speed information for a given set of Points.
		/// The computations are lazy, and variables are only initialized
		/// when called.
		/// </summary>
		/// <param name="points">Given Points</param>
		public Speed(Point[] points)
		{
			this.points = points;
			this.avgSpeed = -1.0;
			this.minSpeed = Double.PositiveInfinity;
			this.maxSpeed = Double.NegativeInfinity;

			this.speedProfile = null;
			this.normSpeedProfile = null;
		}

		#endregion

		#region SPEED COMPUTATIONS

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
		/// Computes the speed at the given point index by averaging from one point ahead and one behind.
		/// </summary>
		/// <param name="index">The point's index</param>
		/// <returns>The speed at the given index</returns>
		private double speed(int index)
		{
			// We will estimate using the point to the left, and the point to the right of the index
			int a = index - 1;
			int b = index + 1;
			
			// Make sure we are using valid points
			if (a < 0)
				a = 0;
			
			if (b > this.points.Length - 1)
				b = this.points.Length - 1;
			
			// Here is the distance
			double distance1 = distance(a, index);
			double distance2 = distance(index, b);

			// Here are the representative points
			Point p1 = this.points[a];
			Point p3 = this.points[b];

			// Here is the time 
			long deltaTime = Math.Abs(Convert.ToInt64(p1.XmlAttrs.Time) - Convert.ToInt64(p3.XmlAttrs.Time));

			// Make sure we do not divide by a zero time
			if (deltaTime == 0)
				deltaTime = 1;

			// Speed = distance / time
			double speed = (distance1 + distance2) / (double)deltaTime;

			return speed;
		}


		/// <summary>
		/// Computes the average speed by summing up the speed at each index and dividing by n.
		/// Does not take into account a certain number of Points on each end, since these
		/// sections are known to be slow.
		/// </summary>
		/// <returns>The average speed of the stroke</returns>
		private double averageSpeed()
		{
			double avgSpeed = 0.0;
		
			// Don't count any points that were at the ends of the stroke
			int endPtWindow;
			if (this.points.Length > 20 && this.points.Length < 60)
				endPtWindow = SMALL_WINDOW;
			else if (this.points.Length >= 60)
				endPtWindow = LARGE_WINDOW;
			else
				endPtWindow = 0;

			// Sum up all the point's speed
			for (int i = endPtWindow; i < Profile.Length - endPtWindow; i++)
			{
				avgSpeed += Profile[i];
			}

			// Divide by the number of points we are averaging
			avgSpeed /= ((double)Profile.Length - (2 * endPtWindow));

			return avgSpeed;
		}


		/// <summary>
		/// Computes the speed at each point and creates a double array, where each index contains
		/// that Stroke point's corresponding speed.
		/// </summary>
		/// <returns>The double[] array of speed</returns>
		private double[] calcSpeedProfile()
		{
			double[] profile = new double[this.points.Length];
			
			for (int i = 0; i < profile.Length; i++)
			{
				profile[i] = speed(i);
			}

			return smoothProfile(profile);
			//return profile;
		}


		/// <summary>
		/// Computes the semi-normalized speed at each point and creates a double array, where each index contains
		/// that Stroke point's corresponding speed.
		/// 
		/// It's not a true normalization since it is more of a percentage change from the average speed.
		/// </summary>
		/// <returns>The double[] array of speed</returns>
		private double[] calcNormSpeedProfile()
		{
			double[] profile = new double[this.points.Length];
			
			for (int i = 0; i < Profile.Length; i++)
			{
				profile[i] = (Profile[i] / AverageSpeed);
			}

			return profile;
		}


		/// <summary>
		/// Smooths the speed profile by setting the speed at each point to the average of
		/// the speed for the Points immediately surrounding it.
		/// </summary>
		/// <param name="profile">Profile to smooth</param>
		/// <returns>Smoothed profile</returns>
		private double[] smoothProfile(double[] profile)
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
		/// Calculates the minimum and maximum speeds from the Points.
		/// </summary>
		private void calcMinAndMax()
		{
			for (int i = 0; i < Profile.Length; i++)
			{
				double speed = Profile[i];

				// Set the min and max speed values if they have changed
				if (speed < this.minSpeed)
					this.minSpeed = speed;
				if (speed > this.maxSpeed)
					this.maxSpeed = speed;
			}
		}

		#endregion

		#region GETTERS & SETTERS

		/// <summary>
		/// Returns the average speed of the Points.
		/// Calculates it once if no average speed variable currently exists.
		/// </summary>
		public double AverageSpeed
		{
			get
			{
				if (this.avgSpeed == -1.0)
				{
					this.avgSpeed = averageSpeed();
				}

				return this.avgSpeed;
			}
		}


		/// <summary>
		/// Returns the minimum speed of the Points.
		/// Calculates it once if no minimum speed variable currently exists.
		/// </summary>
		public double MinimumSpeed
		{
			get
			{
				if (this.minSpeed == Double.PositiveInfinity)
				{
					calcMinAndMax();
				}
				
				return this.minSpeed;
			}
		}


		/// <summary>
		/// Returns the maximum speed of the Points.
		/// Calculates it once if no maximum speed variable currently exists.
		/// </summary>
		public double MaximumSpeed
		{
			get
			{
				if (this.maxSpeed == Double.NegativeInfinity)
				{
					calcMinAndMax();
				}
				
				return this.maxSpeed;
			}
		}

		
		/// <summary>
		/// Returns the (un-normalized) speed profile for the Points.
		/// Calculates it once if no profile currently exists.
		/// </summary>
		public double[] Profile
		{
			get 
			{
				if (this.speedProfile == null)
				{
					this.speedProfile = calcSpeedProfile();
				}
				
				return this.speedProfile;
			}
		}


		/// <summary>
		/// Returns the speed profile normalized by the average speed for the Points.
		/// Calculates it once if no profile currently exists.
		/// </summary>
		public double[] NormProfile
		{
			get 
			{
				if (this.normSpeedProfile == null)
				{
					this.normSpeedProfile = calcNormSpeedProfile();
				}
				
				return this.normSpeedProfile;
			}
		}

		#endregion

		/// <summary>
		/// Compute all of the values for this object. Useful for serialization, etc.
		/// </summary>
		internal void computeAll()
		{
			Object _;
			_ = AverageSpeed;
			_ = MinimumSpeed;
			_ = MaximumSpeed;
			_ = Profile;
		}
	}
}
