/**
 * File: EndPoint.cs
 *
 * Authors: Matthew Weiner and Sam Gordon
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2007.
 * 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Sketch;

namespace CircuitRec
{
    /// <summary>
    /// The EndPoint Class is used to differentiate endpoints from regular Points
    /// </summary>
    public class EndPoint : Point
    {
        #region Internals

        /// <summary>
        /// Slope threshold for saying a wire is horizontal.  If it is between -0.3 and 0.3, it will be horizontal.
        /// </summary>
        private const double MINIMUM_SLOPE_THRESHOLD = 0.3;

        /// <summary>
        /// Slope threshold for saying a wire is vertical.  If it is below -2 or above 2, it will be vertical.
        /// </summary>
        private const double MAXIMUM_SLOPE_THRESHOLD = 2.0;

        /// <summary>
        /// The fraction of total points to take when determining the slope of the endpoint.
        /// </summary>
        private const int FRACTION_OF_POINTS = 3;

        /// <summary>
        /// The orientation of the endpoint (Left, Right, Top, Bottom, TopLeft, etc.)
        /// </summary>
        private string type;

        /// <summary>
        /// A List of the points in the substroke that the EndPoint belongs to.
        /// </summary>
        private List<Point> substrokePoints;

        /// <summary>
        /// An ArrayList of the points that will be used for the line fit of the EndPoint to determine its type.
        /// </summary>
        private ArrayList line;
        
        /// <summary>
        /// The offset of the line fit to the region around the EndPoint.
        /// </summary>
        private double b;

        /// <summary>
        /// The slope of the line fit to the region around the EndPoint.
        /// </summary>
        private double m;

        /// <summary>
        /// The substroke that the EndPoint belongs to.
        /// </summary>
        private Substroke parentSub;

        /// <summary>
        /// The substroke that the EndPoint connects to
        /// </summary>
        private Substroke connectedSub;

        /// <summary>
        /// Determines if the wire is an internal or external endpoint
        /// </summary>
        private bool internalEndPoint;

        #endregion

        #region Constructor
        
        /// <summary>
        /// Creates a new EndPoint from XML Attributes.
        /// </summary>
        /// <param name="XmlAttrs">The attributes of the EndPoint</param>
        public EndPoint(XmlStructs.XmlPointAttrs XmlAttrs)
        {
            this.XmlAttrs = XmlAttrs;
        }

        /// <summary>
        /// Creates a new EndPoint from an existing Point.
        /// </summary>
        /// <param name="point">The original Point</param>
        public EndPoint(Point point)
            : this(point.XmlAttrs.Clone())
        {
            //calls the main constructor from here
        }

        /// <summary>
        /// Creates an endpoint from a point and a parent substroke.
        /// </summary>
        /// <param name="point">The original Point</param>
        /// <param name="sub">The parent Substroke</param>
        public EndPoint(Point point, Substroke sub)
        {
            this.parentSub = sub;
            this.XmlAttrs = point.XmlAttrs;
        }

        #endregion

        #region Methods



        /// <summary>
        /// Copies the type of the SubSlope from one EndPoint to another
        /// </summary>
        /// <param name="copyfrom">The EndPoint to be copied</param>
        /// <param name="copyto">The EndPoint to copy to</param>
        internal static void CopySubSlopeType(EndPoint copyfrom, EndPoint copyto)
        {
            copyto.m = copyfrom.Slope;
            copyto.type = copyfrom.Type;
            copyto.parentSub = copyfrom.parentSub;
        }

        /// <summary>
        /// Determines the slope of a line fit to the points around the EndPoint
        /// </summary>
        /// <param name="substroke">The parent Substroke</param>
        internal void DetermineSlope(Sketch.Substroke substroke)
        {
            substrokePoints = new List<Point>(substroke.PointsL);
            
            substrokePoints.Sort();
            int index = substrokePoints.IndexOf((Point)this);

            // If the index of the endpoint is at the beginning of the sorted Points list
            if (index < substrokePoints.Count / 2)
            {
                line = new ArrayList(substrokePoints.GetRange(0, substrokePoints.Count / FRACTION_OF_POINTS));
            }
            else
            {
                line = new ArrayList(substrokePoints.GetRange(substrokePoints.Count - substrokePoints.Count / FRACTION_OF_POINTS, substrokePoints.Count / FRACTION_OF_POINTS));
            }

            // Find the least squares fit to the points near the endpoint
            leastSquares();
        }

        /// <summary>
        /// Determines the type of the EndPoint based on the slope of the Points
        /// around it and the other EndPoint of the Wire
        /// </summary>
        /// <param name="other">The other EndPoint of the Wire</param>
        internal void DetermineType(EndPoint other)
        {
            // If the line is horizontal
            if (Math.Abs(this.m) < MINIMUM_SLOPE_THRESHOLD)
            {
                if (this.X <= other.X)
                {
                    this.type = "left";
                }
                else
                {
                    this.type = "right";
                }
            }
            // If the line is vertical
            else if (Math.Abs(this.m) > MAXIMUM_SLOPE_THRESHOLD)
            {
                if (this.Y <= other.Y)
                {
                    this.type = "top";
                }
                else
                {
                    this.type = "bottom";
                }
            }
            // Both X and Y coordinate will be smaller or larger for diagonal lines, so only need to check one
            else if ((this.m >= -MAXIMUM_SLOPE_THRESHOLD) && (this.m <= -MINIMUM_SLOPE_THRESHOLD))
            {
                if (this.X <= other.X)
                {
                    this.type = "topleft";
                }
                else
                {
                    this.type = "bottomright";
                }
            }
            else
            {
                if (this.X <= other.X)
                {
                    this.type = "bottomleft";
                }
                else
                {
                    this.type = "topright";
                }
            }


        }

        /// <summary>
        /// Initiates finding the total least squares regression line for the 
        /// points surrounding the EndPoint
        /// </summary>
        private void leastSquares()
        {
            System.Drawing.PointF[] pointf = new System.Drawing.PointF[line.Count];
            for (int i = 0; i < line.Count; i++ )
            {
                
                Point l = (Point)line[i];
                pointf[i] = new System.Drawing.PointF(l.X, l.Y);
            }

            double error = leastSquaresLineFit(pointf, out this.m, out this.b);
            this.m = -this.m;
        }

        protected double leastSquaresLineFit(System.Drawing.PointF[] points, out double m, out double b)
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
		protected double leastSquaresLineFit(System.Drawing.PointF[] points, int startIndex, int endIndex, out double m, out double b)
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

        #endregion

        #region Getters and Setters

        /// <summary>
        /// The type of the EndPoint (Left, Right, Top, Bottom, TopLeft, etc.)
        /// </summary>
        /// <returns>The type/orientation of the wire.</returns>
        public string Type
        {
            get
            {
                return this.type;
            }
            set
            {
                this.type = value;
            }
        }


        public bool InternalEndPoint
        {
            get
            {
                return this.internalEndPoint;
            }
            set
            {
                this.internalEndPoint = value;
            }

        }

        /// <summary>
        /// The slope of the wire around the EndPoint.
        /// </summary>
        /// <returns>The slope of the wire around the EndPoint.</returns>
        public double Slope
        {
            get
            {
                return this.m;
            }
        }

        /// <summary>
        /// The substroke that the EndPoint belongs to.
        /// </summary>
        public Substroke ParentSub
        {
            get
            {
                return this.parentSub;
            }
            set
            {
                this.parentSub = value;
            }
        }

        /// <summary>
        /// The substroke that this endpoint is connected to
        /// </summary>
        public Substroke ConnectedSub
        {
            get { return this.connectedSub; }
            set { this.connectedSub = value; }
        }

        #endregion
    }
}
