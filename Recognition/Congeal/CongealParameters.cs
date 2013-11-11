using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;

namespace Congeal
{
	public class CongealParameters
	{
		#region Public Data

		/// <summary>
		/// Scale along the x-axis (log)
		/// </summary>
		public double x_scale = 0.0;
		/// <summary>
		/// Scale along the y-axis (log)
		/// </summary>
		public double y_scale = 0.0;
		/// <summary>
		/// Skew along the x-axis
		/// </summary>
		public double x_skew = 0.0;
		/// <summary>s
		/// Skew along the y-axis
		/// </summary>
		public double y_skew = 0.0;
		/// <summary>
		/// Translation in the x direction
		/// </summary>
		public double t_x = 0.0;
		/// <summary>
		/// Translation in the y direction
		/// </summary>
		public double t_y = 0.0;
		/// <summary>
		/// Radians of rotation (private and accessed through the degrees verison, theta)
		/// </summary>
		private double _theta = 0.0;
		/// <summary>
		/// Image parameters
		/// </summary>
		public double height, width;

		#endregion

		#region Constructors

		public CongealParameters(double mheight, double mwidth)
		{
			height = mheight;
			width = mwidth;
		}

		public CongealParameters(double mheight, double mwidth, double mt_x, double mt_y, double mtheta, double mx_scale, double my_scale, double mx_skew, double my_skew)
		{
			t_x = mt_x;
			t_y = mt_y;
			theta = mtheta;
			x_scale = mx_scale;
			y_scale = my_scale;
			x_skew = mx_skew;
			y_skew = my_skew;
			height = mheight;
			width = mwidth;
		}

		/// <summary>
		/// Appends another vector of CongealParameters to this one
		/// </summary>
		/// <param name="other">The other of which we spake</param>
		public void append(CongealParameters other)
		{
			if (height != other.height && width != other.width)
				throw new Exception("Image parameters must match!");
			t_x += other.t_x;
			t_y += other.t_y;
			_theta = _theta + other._theta;
			if (_theta > 2 * Math.PI)
			{
				_theta -= 2 * Math.PI;
			}
			if (_theta < -2 * Math.PI)
			{
				_theta += 2 * Math.PI;
			}
			x_scale += other.x_scale;
			y_scale += other.y_scale;
			x_skew += other.x_skew;
			y_skew += other.y_skew;
			//Console.WriteLine("New CP: {0},{1},{2},{3},{4},{5},{6}", t_x, t_y, theta, x_scale, y_scale, x_skew, y_skew);
		}

		/// <summary>
		/// Copy constructor
		/// </summary>
		/// <param name="p">CP to copy from</param>
		public CongealParameters(CongealParameters p)
		{
			t_x = p.t_x;
			t_y = p.t_y;
			height = p.height;
			width = p.width;
			_theta = p._theta;
			x_scale = p.x_scale;
			y_scale = p.y_scale;
			x_skew = p.x_skew;
			y_skew = p.y_skew;
		}

		#endregion

		/// <summary>
		/// Returns the warp represented by these parameters, doing the multiplication
		/// in the correct order.
		/// <param name="centroid_x">The X-coordinate of the centroid around which to perform the warp</param>
		/// <param name="centroid_y">The Y-coordinate of the centroid around which to perform the warp</param>
		/// </summary>
		public Warp getWarp(double centroid_x, double centroid_y)
		{
			Warp w = new Warp(t_x, t_y, _theta, x_scale, y_scale, x_skew, y_skew, centroid_x, centroid_y);
			if (w.ContainsNaN)
				return new Warp();
			return w;
		}

		#region Accessors

		/// <summary>
		/// Returns the warp represented by these parameters, doing the multipliocations in the correct order.
		/// Note: This accessor performs warps around the center of the image. If you want to perform
		/// warps around another point, use the getWarp function.
		/// </summary>
		public Warp warp
		{
			get
			{
				return getWarp(width / 2, height / 2);
			}
		}

		/// <summary>
		/// Angle of rotation, in degrees
		/// </summary>
		public double theta
		{
			get
			{
				return _theta * (180 / Math.PI);
			}
			set
			{
				_theta = (value%360) * (Math.PI / 180.0);
			}
		}

		public double norm
		{
			get
			{
				return Math.Sqrt(Math.Pow(x_scale, 2) + Math.Pow(y_scale, 2) + Math.Pow(x_skew, 2) + Math.Pow(y_skew, 2) + Math.Pow(t_x, 2) + Math.Pow(t_y, 2) + Math.Pow(_theta, 2));
			}
		}

		public new string ToString()
		{
			return String.Format("CongealParameters: t_x={0}, t_y={1}, theta={2} " + Environment.NewLine + "\tx_scale = {3}, y_scale={4}, x_skew={5}, y_skew={6}", t_x, t_y, _theta, x_scale, y_scale, x_skew, y_skew);
		}

		#endregion

		#region Modifiers

		/// <summary>
		/// Invert the effects of these transforms
		/// </summary>
		public void Invert()
		{
			x_scale = -x_scale;
			y_scale = -y_scale;
			x_skew = -x_skew;
			y_skew = -y_skew;
			t_x = -t_x;
			t_y = -t_y;
			_theta = -_theta;
		}

		/// <summary>
		/// Scalar multiplication
		/// </summary>
		/// <param name="p">The CP</param>
		/// <param name="scalar"></param>
		/// <returns></returns>
		public static CongealParameters operator *(double scalar, CongealParameters p)
		{
			return new CongealParameters(p.height, p.width, p.t_x * scalar, p.t_y * scalar, p._theta * scalar, p.x_scale * scalar, p.y_scale * scalar, p.x_skew * scalar, p.y_skew * scalar);
		}

		/// <summary>
		/// Scalar multiplication
		/// </summary>
		/// <param name="p">The CP</param>
		/// <param name="scalar">The scalar</param>
		/// <returns></returns>
		public static CongealParameters operator *(CongealParameters p, double scalar)
		{
			return new CongealParameters(p.height, p.width, p.t_x * scalar, p.t_y * scalar, p._theta * scalar, p.x_scale * scalar, p.y_scale * scalar, p.x_skew * scalar, p.y_skew * scalar);
		}

		#endregion
	}
}
