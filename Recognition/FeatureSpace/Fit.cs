using System;
using System.Collections.Generic;
using System.Text;
using Sketch;

namespace FeatureSpace
{
	/// <summary>
	/// This class analyzes regression fits to the curve. Currently, it only handles line fits
	/// </summary>
	[Serializable]
	public class Fit
	{
		#region Internals

		private double? _lineError = null;
		private double? _m = null;
		private double? _b = null;
		private Substroke _substroke;

		#endregion

		#region Constructors

		/// <summary>
		/// Create a new Fit
		/// </summary>
		/// <param name="k">The substroke to analyze</param>
		public Fit(Substroke k)
		{
			_substroke = k;
		}

		#endregion

		#region Accessors

		/// <summary>
		/// The error on the least-squares line fit
		/// </summary>
		public double LineFitError
		{
			get
			{
				if (_lineError == null)
					computeLineFit();
				return _lineError.Value;
			}
		}

		/// <summary>
		/// The slope of a least-squeares line fit to this stroke
		/// </summary>
		public double M
		{
			get
			{
				if (_m == null)
					computeLineFit();
				return _m.Value;
			}
		}

		/// <summary>
		/// The y-intercept of a least-squares line fit to this stroke
		/// </summary>
		public double B
		{
			get
			{
				if (_b == null)
					computeLineFit();
				return _b.Value;
			}
		}

		/// <summary>
		/// The line segment corresponding to the linear fit to this data
		/// </summary>
		public LineSegment FittedLineSegment
		{
			get
			{
				if (_m == null || _b == null)
					computeLineFit();
				return new LineSegment(_substroke.Endpoints[0], _substroke.Endpoints[1], _m.Value, _b.Value);
			}
		}

		#endregion

		#region Computation

		/// <summary>
		/// Compute the least-squares line fit to this data
		/// </summary>
		private void computeLineFit()
		{
			double m, b;
			_lineError = LeastSquares.leastSquaresLineFit(_substroke.PointsAsPointFs, out m, out b);
			_m = m;
			_b = b;
		}

		#endregion

		/// <summary>
		/// Compute all features. Useful for serialization.
		/// </summary>
		internal void computeAll()
		{
			Object _;
			_ = LineFitError;
			_ = M;
			_ = B;
			_ = FittedLineSegment;
		}
	}
}
