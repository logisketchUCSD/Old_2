using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace FeatureSpace
{
    /// <summary>
    /// This class houses common computational functions within the FeatureSpace domain
    /// </summary>
    public class Compute
    {
        #region Constants / Parameters

        static public float FUDGE_FACTOR = 250.0f;

        static public float THRESHOLD = 0.10f;

        static public double StrokeTimeNormalizer = 1000.0; //10000.0;

        static public double PairwiseDistanceFactor = 10000.0;
        static public double PairwiseTimeFactor = 100000.0;

        static public double PathDensityNormalizer = 1.0; //100.0;

        static public double IntersectionNormalizer = 1.0; //10.0;

        static public double CurvatureNormalizer = 1.0; //10.0;

        static public int Hook_Max = 10;

        static public int Hook_Min = 15;

        static public double Dist_Hook = 200.0;

        static public int Pts_From_End = 10;

        #endregion

        static public double GetSoftmaxNormalizedValue(FeatureSingleStroke featureStroke, string featureName, Dictionary<string, double[]> avgsAndStdevs)
        {
            double value = featureStroke.Features[featureName].NormalizedValue;

            if (avgsAndStdevs.ContainsKey(featureName))
            {
                double mean = avgsAndStdevs[featureName][0];
                double stdDev = avgsAndStdevs[featureName][1];

                return GetSoftmaxNormalizedValue(value, mean, stdDev);
            }
            else
                return value;
        }

        static public double GetSoftmaxNormalizedValue(double value, double mean, double stdDev)
        {
            double x_prime = (value - mean) / stdDev;

            return 1.0 / (1.0 + Math.Exp(-x_prime));
        }

        static public double GetDistanceRatio(double distanceBetweenStrokes, double minDistance)
        {
            double denominator = (1.0 + distanceBetweenStrokes / PairwiseDistanceFactor);
            double numerator = (1.0 + minDistance / PairwiseDistanceFactor);
            return numerator / denominator;
        }

        #region Ink-to-Substroke

        /// <summary>
        /// Creates a Substroke from Microsoft point data
        /// </summary>
        /// <param name="stroke"></param>
        /// <returns></returns>
        static public Sketch.Substroke MakeSubstroke(Microsoft.Ink.Stroke stroke)
        {
            Sketch.Point[] SketchPoints = stripStroke(stroke);
            Sketch.XmlStructs.XmlShapeAttrs XmlAttrs = new Sketch.XmlStructs.XmlShapeAttrs(true);
            XmlAttrs.Time = SketchPoints[0].Time;
            XmlAttrs.Source = "InkOverlay";
            XmlAttrs.PenHeight = stroke.DrawingAttributes.Height;
            XmlAttrs.PenTip = stroke.DrawingAttributes.PenTip.ToString();
            XmlAttrs.PenWidth = stroke.DrawingAttributes.Width;
            XmlAttrs.Color = stroke.DrawingAttributes.Color.ToArgb();
            Sketch.Substroke s = new Sketch.Substroke(SketchPoints, XmlAttrs);

            return s;
        }

        /// <summary>
        /// Grabs the (x, y, time) data from the inkStroke
        /// </summary>
        /// <param name="inkStroke"></param>
        /// <returns></returns>
        static public Sketch.Point[] stripStroke(Microsoft.Ink.Stroke inkStroke)
        {
            ulong theTime;
            int strokeLength = inkStroke.PacketCount;
            ulong[] time = new ulong[strokeLength];

            if (inkStroke.ExtendedProperties.DoesPropertyExist(Utilities.General.theTimeGuid))
            {
                time = (ulong[])inkStroke.ExtendedProperties[Utilities.General.theTimeGuid].Data;
                if (time.Length > 0)
                    theTime = time[0];
                else
                {
                    theTime = ((ulong)DateTime.Now.ToFileTime() - 116444736000000000) / 10000;
                    for (int i = 0; i < strokeLength; i++)
                    {
                        // The multiplication by 1,000 is to convert from
                        // seconds to milliseconds.
                        //
                        // Our time is in the form of milliseconds since Jan 1, 1970
                        //
                        // NOTE: The timestamp for the stroke is made WHEN THE PEN IS LIFTED
                        time[i] = theTime - (ulong)((1 / Utilities.General.SAMPLE_RATE * 1000) * (strokeLength - i));
                    }
                }
            }
            else
            {
                theTime = ((ulong)DateTime.Now.ToFileTime() - 116444736000000000) / 10000;
                for (int i = 0; i < strokeLength; i++)
                {
                    // The multiplication by 1,000 is to convert from
                    // seconds to milliseconds.
                    //
                    // Our time is in the form of milliseconds since Jan 1, 1970
                    //
                    // NOTE: The timestamp for the stroke is made WHEN THE PEN IS LIFTED
                    time[i] = theTime - (ulong)((1 / Utilities.General.SAMPLE_RATE * 1000) * (strokeLength - i));
                }
            }

            bool includePressure = false;
            List<Guid> packetProperties = new List<Guid>(inkStroke.PacketDescription);
            if (packetProperties.Contains(Microsoft.Ink.PacketProperty.NormalPressure))
                includePressure = true;

            int[] x = inkStroke.GetPacketValuesByProperty(Microsoft.Ink.PacketProperty.X);
            int[] y = inkStroke.GetPacketValuesByProperty(Microsoft.Ink.PacketProperty.Y);
            int[] pressure = new int[x.Length];
            if (includePressure)
                pressure = inkStroke.GetPacketValuesByProperty(Microsoft.Ink.PacketProperty.NormalPressure);

            Sketch.Point[] points = new Sketch.Point[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                if (includePressure)
                    points[i] = new Sketch.Point((float)x[i], (float)y[i], (float)pressure[i]);
                else
                    points[i] = new Sketch.Point((float)x[i], (float)y[i]);

                points[i].Time = time[i];
            }

            return points;
        }

        #endregion

        #region Distance / Size Calculations

        /// <summary>
        /// Calculates the Euclidean Distance between any two points
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static public double EuclideanDistance(Sketch.Point a, Sketch.Point b)
        {
            double x2 = Math.Pow(a.X - b.X, 2.0);
            double y2 = Math.Pow(a.Y - b.Y, 2.0);

            return Math.Sqrt(x2 + y2);
        }

        /// <summary>
        /// Calculates the Euclidean Distance between any two points, using their
        /// respective (x, y) coordinates.
        /// </summary>
        /// <param name="aXY"></param>
        /// <param name="bXY"></param>
        /// <returns></returns>
        static public double EuclideanDistance(double[] aXY, double[] bXY)
        {
            double x2 = Math.Pow(aXY[0] - bXY[0], 2.0);
            double y2 = Math.Pow(aXY[1] - bXY[1], 2.0);

            return Math.Sqrt(x2 + y2);
        }

        static public float EuclideanDistance(PointF a, PointF b)
        {
            double x2 = Math.Pow(a.X - b.X, 2.0);
            double y2 = Math.Pow(a.Y - b.Y, 2.0);

            return (float)Math.Sqrt(x2 + y2);
        }

        /// <summary>
        /// This distance calculation is less expensive than doing the square-root.
        /// Save computation time when comparing many pointwise distance pairs, then 
        /// take the sqrt of the min and max to get actual values for distances.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static public double EuclideanDistanceSquared(Sketch.Point a, Sketch.Point b)
        {
            double x2 = Math.Pow(a.X - b.X, 2.0);
            double y2 = Math.Pow(a.Y - b.Y, 2.0);

            return x2 + y2;
        }

        /// <summary>
        /// Distance computation that avoids the costly sqrt calculation.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static public double DistanceSquared(Sketch.Point a, Sketch.Point b)
        {
            double x2 = Math.Pow(a.X - b.X, 2.0);
            double y2 = Math.Pow(a.Y - b.Y, 2.0);

            return x2 + y2;
        }

        /// <summary>
        /// Finds the bounding box for a stroke's points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        static public RectangleF BoundingBox(Sketch.Point[] points)
        {
            float[] MinMax = ComputeMinMax(points);

            float width = MinMax[1] - MinMax[0];
            float height = MinMax[3] - MinMax[2];

            return new RectangleF(MinMax[0], MinMax[2], width, height);
        }

        /// <summary>
        /// Finds the bounding box for a stroke's points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        static public RectangleF BoundingBox(List<Line> lines)
        {
            float[] MinMax = ComputeMinMax(lines);

            float width = MinMax[1] - MinMax[0];
            float height = MinMax[3] - MinMax[2];

            return new RectangleF(MinMax[0], MinMax[2], width, height);
        }

        /// <summary>
        /// { minX, maxX, minY, maxY }
        /// Determines the minimum and maximum values of X and Y for a set of points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        static public float[] ComputeMinMax(Sketch.Point[] points)
        {
            float minX = points[0].X;
            float maxX = points[0].X;
            float minY = points[0].Y;
            float maxY = points[0].Y;

            for (int i = 1; i < points.Length; i++)
            {
                minX = Math.Min(minX, points[i].X);
                maxX = Math.Max(maxX, points[i].X);
                minY = Math.Min(minY, points[i].Y);
                maxY = Math.Max(maxY, points[i].Y);
            }

            return new float[] { minX, maxX, minY, maxY };
        }

        /// <summary>
        /// { minX, maxX, minY, maxY }
        /// Determines the minimum and maximum values of X and Y for a set of points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        static public float[] ComputeMinMax(List<Line> lines)
        {
            float minX = lines[0].EndPoint1.X;
            float maxX = lines[0].EndPoint1.X;
            float minY = lines[0].EndPoint1.Y;
            float maxY = lines[0].EndPoint1.Y;

            for (int i = 1; i < lines.Count; i++)
            {
                minX = Math.Min(minX, lines[i].EndPoint1.X);
                maxX = Math.Max(maxX, lines[i].EndPoint1.X);
                minY = Math.Min(minY, lines[i].EndPoint1.Y);
                maxY = Math.Max(maxY, lines[i].EndPoint1.Y);
            }

            return new float[] { minX, maxX, minY, maxY };
        }

        /// <summary>
        /// { minX, maxX, minY, maxY }
        /// Determines the minimum and maximum values of X and Y for a set of points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        static public float[] ComputeMinMax(Line line)
        {
            float minX = Math.Min(line.EndPoint1.X, line.EndPoint2.X);
            float maxX = Math.Max(line.EndPoint1.X, line.EndPoint2.X);
            float minY = Math.Min(line.EndPoint1.Y, line.EndPoint2.Y);
            float maxY = Math.Max(line.EndPoint1.Y, line.EndPoint2.Y);

            return new float[] { minX, maxX, minY, maxY };
        }

        #endregion

        #region Array operators / Statistical

        /// <summary>
        /// Find the sum of the values in an array
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        static public double Sum(double[] values)
        {
            double sum = 0.0;
            for (int i = 0; i < values.Length; i++)
                sum += values[i];

            return sum;
        }

        /// <summary>
        /// Find the sum of the absolute values in an array
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        static public double SumAbs(double[] values)
        {
            double sum = 0.0;
            for (int i = 0; i < values.Length; i++)
                sum += Math.Abs(values[i]);

            return sum;
        }

        /// <summary>
        /// Find the sum of the squared values in an array
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        static public double SumSquared(double[] values)
        {
            double sum = 0.0;
            for (int i = 0; i < values.Length; i++)
                sum += Math.Pow(values[i], 2.0);

            return sum;
        }

        /// <summary>
        /// Find the sum of the square root of the absolute values in an array
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        static public double SumSqrt(double[] values)
        {
            double sum = 0.0;
            for (int i = 0; i < values.Length; i++)
                sum += Math.Sqrt(Math.Abs(values[i]));

            return sum;
        }
        
        /// <summary>
        /// This function calculates the average value from an array of doubles
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        static public double Mean(double[] values)
        {
            double sum = Sum(values);

            return sum / (double)values.Length;
        }

        /// <summary>
        /// This function calculates the median value from an array of doubles
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        static public double Median(double[] values)
        {
            List<double> listValues = new List<double>(values);

            listValues.Sort();

            if (listValues.Count % 2 == 0)
                return listValues[listValues.Count / 2 + 1];
            else
            {
                double value = listValues[listValues.Count / 2] + listValues[listValues.Count / 2 + 1];
                return value / 2.0;
            }
        }

        /// <summary>
        /// This function finds the standard deviation of a set of values
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        static public double StandardDeviation(double[] values)
        {
            if (values.Length <= 1)
                return 0.0;

            double mean = Mean(values);

            double a = 1.0 / (double)values.Length;
            double b = SumSquared(values);
            double c = Math.Pow(mean, 2.0);
            double stdDev2 = a * b - c;

            return Math.Sqrt(Math.Abs(stdDev2));
        }

        /// <summary>
        /// Find the maximum value from an array of values
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        static public double Max(double[] values)
        {
            double max = values[0];

            for (int i = 1; i < values.Length; i++)
                max = Math.Max(max, values[i]);

            return max;
        }

        /// <summary>
        /// Find the minimum value from an array of values
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        static public double Min(double[] values)
        {
            if (values.Length == 0)
                return 0.0;

            double min = values[0];

            for (int i = 1; i < values.Length; i++)
                min = Math.Min(min, values[i]);

            return min;
        }

        #endregion

        #region Profile Functions

        /// <summary>
        /// Function which calculates the theta values between all points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        static public double[] FindThetas(Sketch.Point[] allPoints)
        {
            Sketch.Point[] points = new Sketch.Point[allPoints.Length];
            points[0] = allPoints[0];
            int count = 1;

            for (int i = 1; i < allPoints.Length; i++)
            {
                if (allPoints[i].X != allPoints[i - 1].X && allPoints[i].Y != allPoints[i - 1].Y)
                {
                    points[count] = allPoints[i];
                    count++;
                }
            }

            if (count > 2)
            {
                double[] thetas = new double[count - 2];

                float dxp = new float();
                float dyp = new float();
                float dxp1 = new float();
                float dyp1 = new float();

                for (int i = 1; i < count - 1; i++)
                {
                    dxp = points[i + 1].X - points[i].X;
                    dyp = points[i + 1].Y - points[i].Y;
                    dxp1 = points[i].X - points[i - 1].X;
                    dyp1 = points[i].Y - points[i - 1].Y;

                    thetas[i - 1] = Math.Atan2(dxp * dyp1 - dxp1 * dyp, dxp * dxp1 + dyp * dyp1);
                }

                return thetas;
            }
            else
                return new double[0];
        }

        /// <summary>
        /// Function which calculates the theta values between all points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        static public double[] FindAllThetas(Sketch.Point[] points)
        {
            int count = points.Length;

            if (count > 2)
            {
                double[] thetas = new double[count];

                float dxp = new float();
                float dyp = new float();
                float dxp1 = new float();
                float dyp1 = new float();

                for (int i = 1; i < count - 2; i++)
                {
                    dxp = points[i + 1].X - points[i].X;
                    dyp = points[i + 1].Y - points[i].Y;
                    dxp1 = points[i].X - points[i - 1].X;
                    dyp1 = points[i].Y - points[i - 1].Y;

                    thetas[i] = Math.Atan2(dxp * dyp1 - dxp1 * dyp, dxp * dxp1 + dyp * dyp1);
                }

                thetas[0] = 0.0;
                thetas[thetas.Length - 1] = 0.0;

                return thetas;
            }
            else
                return new double[0];
        }

        /// <summary>
        /// Calculates the arc length profile for a stroke
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        static public double[] CalculateArcLength(double[] x, double[] y)
        {
            int length = x.Length;

            double[] distance = new double[length];
            distance[0] = 0;

            for (int i = 1; i < length; i++)
            {
                double xVal = Math.Pow(x[i] - x[i - 1], 2.0);
                double yVal = Math.Pow(y[i] - y[i - 1], 2.0);
                double next = Math.Sqrt(xVal + yVal);
                distance[i] = distance[i - 1] + next;
            }

            return distance;
        }

        /// <summary>
        /// Computes the speed profile for a given set of points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        static public double[] FindSpeeds(Sketch.Point[] points)
        {
            double SAMPLE_TIME = 7.5187969924812; // = (1000 / 133.0)
            double[] speeds = new double[points.Length];

            for (int i = 1; i < points.Length - 1; i++)
            {
                double distance = EuclideanDistance(points[i - 1], points[i + 1]);
                double time = (double)(points[i + 1].Time - points[i - 1].Time);
                if (time >= 0.0)
                    speeds[i] = distance / time;
                else
                    speeds[i] = distance / (SAMPLE_TIME * 2.0);
            }

            if (speeds.Length > 2)
            {
                speeds[0] = speeds[1];
                speeds[speeds.Length - 1] = speeds[speeds.Length - 2];
            }
            else if (speeds.Length == 1)
            {
                double distance = EuclideanDistance(points[0], points[1]);
                double time = (double)(points[0].Time - points[1].Time);
                if (time >= 0.0)
                    speeds[0] = distance / time;
                else
                    speeds[0] = distance / (SAMPLE_TIME * 2.0);

                speeds[1] = speeds[0];
            }
            else
                speeds[0] = 0.0;


            return speeds;
        }

        /// <summary>
        /// Calculates the Analytical value of curvature for each point
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        static public double[] FindCurvatures(Sketch.Point[] points)
        {
            int length = points.Length;
            int POINTS_AWAY_FROM_END = 5;

            double[] x = new double[length];
            double[] y = new double[length];

            for (int i = 0; i < length; i++)
            {
                x[i] = (double)points[i].X;
                y[i] = (double)points[i].Y;
            }

            double[] s = CalculateArcLength(x, y);

            double[] C = new double[length];

            double[] xdot = deriv1(x, s);
            double[] ydot = deriv1(y, s);

            double[] xdoubledot = deriv1(xdot, s);
            double[] ydoubledot = deriv1(ydot, s);

            double denom = new double();
            double numer = new double();

            if (length >= POINTS_AWAY_FROM_END)
            {
                for (int i = 0; i < length; i++)
                    C[i] = 0.0;
                for (int i = length - 3; i < length; i++)
                    C[i] = 0.0;
            }
            else
            {
                for (int i = 0; i < length; i++)
                    C[i] = 0.0;
            }
            for (int i = POINTS_AWAY_FROM_END; i < length - 3; i++)
            {
                denom = Math.Pow(Math.Pow(xdot[i], 2.0) + Math.Pow(ydot[i], 2.0), 1.5);
                numer = xdot[i] * ydoubledot[i] - ydot[i] * xdoubledot[i];

                if (denom != 0)
                    C[i] = Math.Abs(numer / denom);
                else
                    C[i] = 0.0;
            }

            return C;
        }

        #endregion

        /// <summary>
        /// This function normalizes the values in an array by a 
        /// given normalization factor
        /// </summary>
        /// <param name="values"></param>
        /// <param name="normalizationFactor"></param>
        /// <returns></returns>
        static public double[] Normalize(double[] values, double normalizationFactor)
        {
            double[] output = new double[values.Length];

            for (int i = 0; i < values.Length; i++)
                output[i] = values[i] / normalizationFactor;

            return output;
        }

        /// <summary>
        /// Computes the spatial derivative of variable 't'
        /// </summary>
        /// <param name="t"></param>
        /// <param name="u"></param>
        /// <returns></returns>
        static public double[] deriv1(double[] t, double[] u)
        {
            int length = t.Length;
            double[] tdot = new double[length];

            // End points
            if (length > 2)
            {
                // First point - Forward difference approximation of O[(deltaX)^2]
                if (u[2] - u[0] != 0)
                {
                    double deltaX = u[2] - u[0];
                    double numerator = -t[2] + 4.0 * t[1] - 3.0 * t[0];
                    tdot[0] = numerator / deltaX;
                }
                else
                    tdot[0] = 0.0;

                // Last point - Backward difference approximation of O[(deltaX)^2]
                if (t[length - 1] - t[length - 3] != 0)
                {
                    double deltaX = u[length - 1] - u[length - 3];
                    double numerator = 3.0 * t[length - 1] - 4.0 * t[length - 2] + t[length - 3];
                    tdot[length - 1] = numerator / deltaX;
                }
                else
                    tdot[length - 1] = 0.0;

                // Interior points - Central Difference Approximation of O[(deltaX)^2]
                for (int i = 1; i < length - 2; i++)
                {
                    if (u[i + 1] - u[i - 1] != 0)
                    {
                        double deltaX = u[i + 1] - u[i - 1];
                        double numerator = t[i + 1] - t[i - 1];
                        tdot[i] = numerator / deltaX;
                    }
                    else
                        tdot[i] = 0.0;
                }
            }

            return tdot;
        }

        /// <summary>
        /// Use a weighted moving average of the value in question
        /// along with its closest neighbor on either side to smooth
        /// the profile of the values.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        static public double[] SmoothWeightedMovingAverage(double[] values)
        {
            double a = 0.25;
            double b = 1 - 2.0 * a;

            double[] smooth = new double[values.Length];

            for (int i = 1; i < values.Length - 1; i++)
                smooth[i] = a * values[i - 1] + b * values[i] + a * values[i + 1];

            smooth[0] = values[0];
            smooth[smooth.Length - 1] = values[values.Length - 1];

            return smooth;
        }

        static public Sketch.Point[] TrimPoints(Sketch.Point[] points, double stdDevFactor)
        {
            double[] speeds = FindSpeeds(points);
            List<int> speedMinima = FindMinima(speeds);
            if (speedMinima.Count == 0)
                return points;

            int away = 5;
            if (points.Length / 2 < away)
                away = points.Length / 2;
            bool start = false;
            bool end = false;
            

            if (speedMinima[0] < away)
                start = true;
            if (speedMinima[speedMinima.Count - 1] > points.Length  - away)
                end = true;

            double[] pressures = new double[points.Length];
            for (int i = 0; i < points.Length; i++)
                pressures[i] = points[i].Pressure;

            double mean = Mean(pressures);
            double stdDev = StandardDeviation(pressures);

            double cutoff = mean - stdDev * stdDevFactor;

            List<Sketch.Point> trimmedPoints = new List<Sketch.Point>();
            
            for (int i = 0; i < away; i++)
            {
                if (points[i].Pressure < cutoff && start)
                {
                    // Don't add the point
                }
                else
                    trimmedPoints.Add(points[i]);
            }

            for (int i = away; i < points.Length - away; i++)
                trimmedPoints.Add(points[i]);
            
            for (int i = points.Length - away; i < points.Length; i++)
            {
                if (points[i].Pressure < cutoff && end)
                {
                    // Don't add the point
                }
                else
                    trimmedPoints.Add(points[i]);
            }

            Sketch.Point[] a = new Sketch.Point[trimmedPoints.Count];
            for (int i = 0; i < trimmedPoints.Count; i++)
                a[i] = trimmedPoints[i];

            return a;
        }

        static public List<int> FindMinima(double[] values)
        {
            List<int> minima = new List<int>();
            double StdDevsAway = 1.0;
            double mean = Mean(values);
            double StdDev = StandardDeviation(values);

            double Threshold = mean - StdDevsAway * StdDev;

            int sign = 0;
            double current = values[0];
            

            for (int i = 0; i < values.Length; i++)
            {
                if (sign == 0)
                {
                    if (values[i] < current)
                        sign = -1;
                    else if (values[i] > current)
                        sign = 1;
                }
                else if (sign == -1)
                {
                    if (values[i] > current)
                    {
                        sign = 1;
                        if (values[i - 1] <= Threshold)
                            minima.Add(i - 1);
                    }
                    current = values[i];
                }
                else if (sign == 1)
                {
                    if (values[i] < current)
                    {
                        sign = -1;
                        if (values[i - 1] <= Threshold)
                            minima.Add(i - 1);
                    }
                    current = values[i];
                }
            }

            return minima;
        }

        static public Sketch.Substroke DeHook(Sketch.Substroke stroke)
        {
            List<Sketch.Point> points = stroke.PointsL;
            List<Sketch.Point> dehooked = new List<Sketch.Point>(points.Count);
            List<int> BadPts = new List<int>();

            //Console.WriteLine("stroke: {0}", stroke.Id);
            //for (int i = 0; i < points.Count; i++)
                //Console.WriteLine("Point {0}: ({1}, {2})", i, points[i].X, points[i].Y);

            double maxdist = 0.0;
            for (int i = 1; i < Math.Min(Hook_Min, points.Count - Hook_Max); i++)
            {
                double dist = EuclideanDistance(points[i], points[0]);
                //Console.WriteLine("i={0}, maxdist={1}, dist={2}", i, maxdist, dist);
                if (dist > Dist_Hook)
                    break;
                if (dist >= maxdist)
                    maxdist = dist;
                else
                {
                    for (int j = 0; j < i; j++)
                    {
                        BadPts.Add(j);
                        //Console.Write("{0} ", j);
                    }
                    //Console.WriteLine();
                    break;
                }
            }
            maxdist = 0.0;
            for (int i = points.Count - 2; i > Math.Max(Hook_Max, points.Count - Hook_Min); i--)
            {
                double dist = EuclideanDistance(points[i], points[points.Count - 1]);
                //Console.WriteLine("i={0}, maxdist={1}, dist={2}", i, maxdist, dist);
                if (dist > Dist_Hook)
                    break;
                if (dist >= maxdist)
                    maxdist = dist;
                else
                {
                    for (int j = points.Count - 1; j > i; j--)
                    {
                        BadPts.Add(j);
                        //Console.Write("{0} ", j);
                    }
                    //Console.WriteLine();
                    break;
                }
            }

            // Remove bad points from list
            for (int i = 0; i < points.Count; i++)
            {
                if (!BadPts.Contains(i))
                    dehooked.Add(points[i]);
            }

            if (dehooked.Count > 0)
            {
                Sketch.Substroke s = new Sketch.Substroke(dehooked, stroke.XmlAttrs);
                s.ParentShapes = stroke.ParentShapes;
                s.ParentStroke = stroke.ParentStroke;
                
                return s;
            }
            else
                return stroke;
        }

        static public List<Sketch.Point> DeHook(List<Sketch.Point> points, int hook_max, int hook_min, double dist_hook)
        {
            List<Sketch.Point> dehooked = new List<Sketch.Point>(points.Count);
            List<int> BadPts = new List<int>();

            double maxdist = 0.0;
            for (int i = 1; i < Math.Min(hook_min, points.Count - hook_max); i++)
            {
                double dist = EuclideanDistance(points[i], points[0]);
                if (dist > dist_hook)
                    break;
                if (dist >= maxdist)
                    maxdist = dist;
                else
                {
                    for (int j = 0; j < i; j++)
                        BadPts.Add(j);
                    break;
                }
            }
            maxdist = 0.0;
            for (int i = points.Count - 2; i > Math.Max(hook_max, points.Count - hook_min); i--)
            {
                double dist = EuclideanDistance(points[i], points[points.Count - 1]);
                if (dist > dist_hook)
                    break;
                if (dist >= maxdist)
                    maxdist = dist;
                else
                {
                    for (int j = points.Count - 1; j > i; j--)
                        BadPts.Add(j);
                    break;
                }
            }

            // Remove bad points from list
            for (int i = 0; i < points.Count; i++)
            {
                if (!BadPts.Contains(i))
                    dehooked.Add(points[i]);
            }

            if (dehooked.Count > 0)
                return dehooked;
            else
                return points;
        }

        static public List<Sketch.Point> RemoveDuplicatePoints(List<Sketch.Point> pts)
        {
            List<Sketch.Point> points = new List<Sketch.Point>(pts.Count);
            points.Add(pts[0]);

            for (int i = 1; i < pts.Count; i++)
            {
                if (EuclideanDistance(pts[i], pts[i - 1]) > 0.0)
                    points.Add(pts[i]);
            }

            return points;
        }

        static public bool StrokeInsideBoundingBox(Sketch.Substroke stroke, Rectangle boundingBox)
        {
            float[] sMinMax = ComputeMinMax(stroke.Points);

            if (sMinMax[0] < boundingBox.Left)
                return false;
            else if (sMinMax[1] > boundingBox.Right)
                return false;
            else if (sMinMax[2] < boundingBox.Top)
                return false;
            else if (sMinMax[3] > boundingBox.Bottom)
                return false;
            else
                return true;
        }

        static public bool StrokeInsideBoundingBox(Sketch.Substroke stroke, RectangleF boundingBox)
        {
            float[] sMinMax = ComputeMinMax(stroke.Points);

            if (sMinMax[0] < boundingBox.Left)
                return false;
            else if (sMinMax[1] > boundingBox.Right)
                return false;
            else if (sMinMax[2] < boundingBox.Top)
                return false;
            else if (sMinMax[3] > boundingBox.Bottom)
                return false;
            else
                return true;
        }

        static public bool LineInsideBoundingBox(Line line, RectangleF boundingBox)
        {
            float[] sMinMax = ComputeMinMax(line);

            if (sMinMax[0] < boundingBox.Left)
                return false;
            else if (sMinMax[1] > boundingBox.Right)
                return false;
            else if (sMinMax[2] < boundingBox.Top)
                return false;
            else if (sMinMax[3] > boundingBox.Bottom)
                return false;
            else
                return true;
        }

        #region Intersection Functions

        /// <summary>
        /// Determines whether two rectangles overlap each other.
        /// A fudge factor is included so that they don't have to exactly overlap.
        /// </summary>
        /// <param name="a">Bounding Box of stroke a</param>
        /// <param name="b">Bounding Box of stroke b</param>
        /// <param name="fudge">Extra factor to account for inexactness</param>
        /// <returns></returns>
        static public bool OverlapBBox(RectangleF a, RectangleF b, float fudge)
        {
            bool overlap = false;

            if ((a.X >= (b.X - fudge) && a.X <= (b.X + b.Width + fudge))
                || ((a.X + a.Width) >= (b.X - fudge) && (a.X + a.Width) <= (b.X + b.Width + fudge))
                || ((b.X >= (a.X - fudge)) && (b.X <= (a.X + a.Width + fudge))))  // overlap in x
            {
                if ((a.Y >= (b.Y - fudge) && a.Y <= (b.Y + b.Height + fudge))
                    || ((a.Y + a.Height) >= (b.Y - fudge) && (a.Y + a.Height) <= (b.Y + b.Height + fudge))
                    || (b.Y >= (a.Y - fudge) && b.Y <= (a.Y + a.Height + fudge)))  // overlap in y
                    overlap = true;
            }

            return overlap;
        }

        /// <summary>
        /// Attempts to find all intersections between two strokes
        /// </summary>
        /// <param name="a">First Stroke</param>
        /// <param name="b">Second Stroke</param>
        /// <returns>List of Intersections</returns>
        static public List<Intersection> Intersect(Sketch.Substroke a, Sketch.Substroke b)
        {
            List<Intersection> intersections = new List<Intersection>();
            if (!OverlapBBox(BoundingBox(a.Points), BoundingBox(b.Points), FUDGE_FACTOR))
                return intersections;



            return intersections;
        }

        /// <summary>
        /// Attempts to find all intersections between two strokes
        /// </summary>
        /// <param name="a">First Stroke</param>
        /// <param name="b">Second Stroke</param>
        /// <returns>List of Intersections</returns>
        static public List<Intersection> Intersect(Sketch.Substroke ssA, Sketch.Substroke ssB, List<Line> linesA, List<Line> linesB, RectangleF boxA, RectangleF boxB, float FudgeFactor)
        {
            List<Intersection> intersections = new List<Intersection>();
            if (!OverlapBBox(boxA, boxB, FudgeFactor))
                return intersections;

            for (int i = 0; i < linesA.Count; i++)
            {
                for (int j = 0; j < linesB.Count; j++)
                {
                    if (Line.intersects(linesA[i], linesB[j]))
                    {
                        // Could be sped up by passing in featureStroke which already contains the ArcLength profile.
                        // However this would complicate things a little.
                        ArcLength lengthA = new ArcLength(ssA.Points);
                        ArcLength lengthB = new ArcLength(ssB.Points);
                        float aInt = -1f;
                        float bInt = -1f;

                        try
                        {
                            if (i > 0 && i < linesA.Count - 1)
                                aInt = (float)(lengthA.Profile[i - 1] / lengthA.TotalLength);
                            else if (i == 0)
                                aInt = -Line.findIntersectionDistanceAlongLineA(linesA[i], linesB[j], false);
                            else if (i == linesA.Count - 1)
                                aInt = 1f + Line.findIntersectionDistanceAlongLineA(linesA[i], linesB[j], false);

                            if (j > 0 && j < linesB.Count - 1)
                                bInt = (float)(lengthB.Profile[j - 1] / lengthB.TotalLength);
                            else if (j == 0)
                                bInt = -Line.findIntersectionDistanceAlongLineA(linesB[j], linesA[i], false);
                            else if (j == linesB.Count - 1)
                                bInt = 1f + Line.findIntersectionDistanceAlongLineA(linesB[j], linesA[i], false);

                            intersections.Add(new Intersection(ssA, ssB, aInt, bInt, linesA[i].IsEndLine, linesB[j].IsEndLine));
                        }
                        catch (Exception e3)
                        {
                            Console.WriteLine("Compute Intersect: " + e3.Message);
                            //throw e3;
                        }
                    }
                }
            }

            return intersections;
        }

        static public List<Line> getLines(List<Sketch.Point> pts)
        {
            List<Line> lines = new List<Line>(pts.Count - 1);

            for (int i = 1; i < pts.Count; i++)
            {
                if (pts[i].X == pts[i - 1].X && pts[i].Y == pts[i - 1].Y)
                    pts.Remove(pts[i]);
            }


            for (int i = 1; i < pts.Count; i++)
                lines.Add(new Line(pts[i - 1].SysDrawPointF, pts[i].SysDrawPointF, false));

            return lines;
        }

        #endregion

        #region Old Intersection Stuff

        private static int[] intersect(Sketch.Substroke a, Sketch.Substroke b, List<Line> linesA, List<Line> linesB, double arcLengthA, double arcLengthB, double avgArcLength)
        {
            int[] numIntersections = { 0, 0, 0 };

            System.Drawing.RectangleF aBbox = BoundingBox(a.Points);
            System.Drawing.RectangleF bBbox = BoundingBox(b.Points);
            double d = computeBestLength(arcLengthA, arcLengthB, avgArcLength);

            if (OverlapBBox(aBbox, bBbox, (int)d))
            {
                numIntersections[0] = L_intersection(a, b, arcLengthA, arcLengthB, avgArcLength);
                numIntersections[1] = T_intersection(linesA, linesB, arcLengthA, arcLengthB, avgArcLength);
                numIntersections[2] = X_intersection(linesA, linesB, arcLengthA, arcLengthB, avgArcLength);
            }


            return numIntersections;
        }

        /// <summary>
        /// Computes whether there is an L-intersection between the two strokes.
        /// </summary>
        /// <param name="a">First Stroke</param>
        /// <param name="b">Second Stroke</param>
        /// <param name="L1">Arc Length of first stroke</param>
        /// <param name="L2">Arc Length of second stroke</param>
        /// <param name="avgArcLength">Average Arc Length in entire sketch</param>
        /// <returns>Int variable indicating how many L-intersections there are</returns>
        private static int L_intersection(Sketch.Substroke a, Sketch.Substroke b,
            double L1, double L2, double avgArcLength)
        {
            //List<Guid> ids = new List<Guid>(0);
            int intersects = 0;
            double d = computeBestLength(L1, L2, avgArcLength);

            if (EuclideanDistance(a.Points[0], b.Points[0]) < d)
            {
                if (EuclideanDistance(a.Points[a.Points.Length - 1], b.Points[b.Points.Length - 1]) < d)
                    intersects++;
                intersects++;
            }
            else if (EuclideanDistance(a.Points[a.Points.Length - 1], b.Points[0]) < d)
            {
                if (EuclideanDistance(a.Points[0], b.Points[b.Points.Length - 1]) < d)
                    intersects++;
                intersects++;
            }
            else if (EuclideanDistance(a.Points[0], b.Points[b.Points.Length - 1]) < d)
            {
                if (EuclideanDistance(a.Points[a.Points.Length - 1], b.Points[0]) < d)
                    intersects++;
                intersects++;
            }

            else if (EuclideanDistance(a.Points[a.Points.Length - 1], b.Points[b.Points.Length - 1]) < d)
            {
                if (EuclideanDistance(a.Points[0], b.Points[0]) < d)
                    intersects++;
                intersects++;
            }

            return intersects;
        }

        /// <summary>
        /// Computes whether there is an T-intersection between the two strokes.
        /// </summary>
        /// <param name="linesA">List of lines in First Stroke</param>
        /// <param name="linesB">List of lines in Second Stroke</param>
        /// <param name="L1">Arc Length of first stroke</param>
        /// <param name="L2">Arc Length of second stroke</param>
        /// <param name="avgArcLength">Average Arc Length in entire sketch</param>
        /// <returns>Int variable indicating how many T-intersections there are</returns>
        private static int T_intersection(List<Line> linesA, List<Line> linesB,
            double L1, double L2, double avgArcLength)
        {
            //List<Guid> ids = new List<Guid>(0);
            int intersects = 0;
            int limit = 6;
            double d = computeBestLength(L1, L2, avgArcLength);

            Line[] aLines = getEndLines(linesA, d);

            for (int i = 0; i < aLines.Length; i++)
            {
                for (int j = 0 + limit; j < linesB.Count - limit; j++)
                {
                    if (Line.intersects(aLines[i], linesB[j]))
                        intersects++;
                }
            }

            return intersects;
        }

        /// <summary>
        /// Computes whether there is an X-intersection between the two strokes.
        /// </summary>
        /// <param name="a">List of lines for the First Stroke</param>
        /// <param name="b">List of lines for the Second Stroke</param>
        /// <param name="L1">Arc Length of first stroke</param>
        /// <param name="L2">Arc Length of second stroke</param>
        /// <param name="avgArcLength">Average Arc Length in entire sketch</param>
        /// <returns>Int variable indicating how many X-intersections there are</returns>
        private static int X_intersection(List<Line> a, List<Line> b,
            double L1, double L2, double avgArcLength)
        {
            int intersects = 0;
            int limit = 6;
            double d = computeBestLength(L1, L2, avgArcLength);

            for (int i = 0 + limit; i < a.Count - 1 - limit; i++)
            {
                for (int j = 0 + limit; j < b.Count - 1 - limit; j++)
                {
                    if (Line.intersects(a[i], b[j]))
                        intersects++;
                }
            }

            return intersects;
        }


        private static List<Line> getLines(Sketch.Substroke stroke)
        {
            List<Line> lines = new List<Line>(stroke.Points.Length - 1);

            List<Sketch.Point> pts = stroke.PointsL;


            for (int i = 1; i < pts.Count; i++)
            {
                if (pts[i].X == pts[i - 1].X && pts[i].Y == pts[i - 1].Y)
                    pts.Remove(pts[i]);
            }


            for (int i = 1; i < pts.Count; i++)
                lines.Add(new Line(pts[i - 1].SysDrawPointF, pts[i].SysDrawPointF, false));

            /*
            for (int i = 1; i < stroke.Points.Length; i++)
                lines.Add(new Line(stroke.Points[i - 1].SysDrawPointF, stroke.Points[i].SysDrawPointF));

            for (int i = 1; i < lines.Count; i++)
            {
                if (lines[i].sameAs(lines[i - 1]))
                    lines.Remove(lines[i - 1]);
            }
             * */

            /*
            int count = 0;

            for (int i = 1; i < stroke.Points.Length; i++)
            {
                if ((stroke.Points[i].X != stroke.Points[i - 1 - count].X) && (stroke.Points[i].Y != stroke.Points[i - 1 - count].Y))
                {
                    lines.Add(new Line(stroke.Points[i - 1 - count].SysDrawPointF, stroke.Points[i].SysDrawPointF));
                    count = 0;
                }
                else
                    count++;
            }
             * */

            return lines;
        }

        private static Line[] getEndLines(List<Line> a, double d)
        {
            Line[] lines = new Line[2];
            int limit = 10;

            if (a.Count > limit)
            {
                lines[0] = new Line(a[limit].EndPoint1, a[0].EndPoint1, true);
                lines[1] = new Line(a[a.Count - 1 - limit].EndPoint1, a[a.Count - 1].EndPoint2, true);
            }
            else if (a.Count > 0)
            {
                lines[0] = new Line(a[a.Count - 1].EndPoint2, a[0].EndPoint1, true);
                lines[1] = new Line(a[0].EndPoint1, a[a.Count - 1].EndPoint2, true);
                return lines;
            }
            else
            {
                lines[0] = new Line(new System.Drawing.PointF(0.0f, 0.0f), new System.Drawing.PointF(0.0f, 0.0f), true);
                lines[1] = new Line(new System.Drawing.PointF(0.0f, 0.0f), new System.Drawing.PointF(0.0f, 0.0f), true);
                return lines;
            }

            lines[0].extend(d);
            lines[1].extend(d);

            return lines;
        }

        /// <summary>
        /// Finds the best length to use for telling whether strokes are close to each other
        /// </summary>
        /// <param name="L1">Arc length of first stroke</param>
        /// <param name="L2">Arc length of second stroke</param>
        /// <param name="avgArcLength">Average arc length of all strokes in sketch</param>
        /// <returns>best distance</returns>
        private static double computeBestLength(double L1, double L2, double avgArcLength)
        {
            const double DISTANCE_THRESHOLD = 0.17;
            double d = new double();

            d = Math.Min(L1, L2);

            if (d > avgArcLength)
                d = avgArcLength;
            else
                d = (d + avgArcLength) / 2.0;

            d *= DISTANCE_THRESHOLD;

            return d;
        }

        #endregion
    }
}
