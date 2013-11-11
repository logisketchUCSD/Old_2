using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Threading;
using Sketch;

namespace FeatureSpace
{
    /// <summary>
    /// Contains single-stroke features
    /// </summary>
    [Serializable]
    public class FeatureSingleStroke
    {
        #region Member Variables

        /// <summary>
        /// This guid is the same as the substroke's id.
        /// </summary>
        private Guid m_Id;

        /// <summary>
        /// The sketch containing this stroke.
        /// </summary>
        private Sketch.Sketch m_Sketch;

        /// <summary>
        /// The substroke that features are being calculated for.
        /// </summary>
        private Sketch.Substroke m_Stroke;

        /// <summary>
        /// An axially aligned bounding box around the substroke.
        /// </summary>
        private System.Drawing.RectangleF m_BoundingBox;

        /// <summary>
        /// The angles calculated between consecutive sets of 3 points
        /// throughout the stroke.
        /// </summary>
        private double[] m_Thetas;

        private ArcLength m_ArcLengthProfile;

        private Slope m_Slope;

        private Curvature m_Curvature;

        private Speed m_Speed;

        private Spatial m_Spatial;

        private Fit m_Fit;

        /// <summary>
        /// A list of the calculated features, linked to string values indicating
        /// what feature it is.
        /// </summary>
        private Dictionary<string, Feature> m_Features;

        /// <summary>
        /// A list that is input into the stroke class indicating which features
        /// to calculate and use.
        /// </summary>
        private Dictionary<string, bool> m_FeaturesToUse;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor for Single Stroke Features. Calculates Single Stroke Features
        /// </summary>
        /// <param name="stroke">Stroke to find feature values for</param>
        /// <param name="sketch">Sketch the stroke resides in</param>
        /// <param name="featureList">Which features to use</param>
        public FeatureSingleStroke(Substroke stroke, Sketch.Sketch sketch, Dictionary<string, bool> featureList)
        {
            m_Id = stroke.Id;
            m_Sketch = sketch;
            m_Stroke = stroke;
            m_FeaturesToUse = featureList;

            m_Features = new Dictionary<string, Feature>();

            m_BoundingBox = Compute.BoundingBox(m_Stroke.Points);

            m_ArcLengthProfile = new ArcLength(m_Stroke.Points);
            m_Slope = new Slope(m_Stroke.Points);
            m_Speed = new Speed(m_Stroke.Points);
            m_Curvature = new Curvature(m_Stroke.Points, m_ArcLengthProfile.Profile, m_Slope.TanProfile);
            m_Spatial = new Spatial(m_Stroke.Points);
            m_Fit = new Fit(m_Stroke);
            
            if (UseThetas)
            {
                m_Thetas = Compute.FindThetas(m_Stroke.Points);
                //m_Thetas = Compute.Normalize(m_Thetas, Math.PI * 2.0);
            }
            else
                m_Thetas = new double[0];

            if (UseArcLength)
                m_Features.Add("Arc Length", new InkLength(m_ArcLengthProfile.TotalLength));

            if (UseClosedPath)
            {
                m_Features.Add("Part of a Closed Path", new PartOfClosedPath(false));
                m_Features.Add("Inside a Closed Path", new InsideClosedPath(false));
            }

            CalculateSingleStrokeFeatures();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Calculates the features which don't depend on any
        /// other strokes. These values do not change
        /// </summary>
        public void CalculateSingleStrokeFeatures()
        {
            string key = "";
            #region Size Features

            // Arc Length already added if necessary
            key = "Bounding Box Width";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse["Bounding Box Width"])
                m_Features.Add("Bounding Box Width", new Width(m_BoundingBox));

            key = "Bounding Box Height";
            if (m_FeaturesToUse.ContainsKey("Bounding Box Height") && m_FeaturesToUse["Bounding Box Height"])
                m_Features.Add("Bounding Box Height", new Height(m_BoundingBox));

            key = "Bounding Box Area";
            if (m_FeaturesToUse.ContainsKey("Bounding Box Area") && m_FeaturesToUse["Bounding Box Area"])
                m_Features.Add("Bounding Box Area", new Area(m_BoundingBox));

            key = "Path Density";
            if (m_FeaturesToUse.ContainsKey("Path Density") && m_FeaturesToUse["Path Density"])
                m_Features.Add("Path Density", new PathDensity(m_Features["Arc Length"].Value, (double)(m_BoundingBox.Width), (double)(m_BoundingBox.Height)));

            key = "End Point to Arc Length Ratio";
            if (m_FeaturesToUse.ContainsKey("End Point to Arc Length Ratio") && m_FeaturesToUse["End Point to Arc Length Ratio"])
                m_Features.Add("End Point to Arc Length Ratio", new EndPt2LengthRatio(m_Stroke.Points, m_Features["Arc Length"].Value));

            #endregion

            #region Curvature Features

            key = "Sum of Thetas";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                m_Features.Add(key, new SumTheta(m_Thetas));

            key = "Sum of Abs Value of Thetas";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                m_Features.Add(key, new SumAbsTheta(m_Thetas));

            key = "Sum of Squared Thetas";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                m_Features.Add(key, new SumSquaredTheta(m_Thetas));

            key = "Sum of Sqrt of Thetas";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                m_Features.Add(key, new SumSqrtTheta(m_Thetas));

            #endregion

            #region Speed / Time Features

            key = "Average Pen Speed";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                m_Features.Add(key, new AvgSpeed(m_Stroke.Points, m_Features["Arc Length"].Value));

            key = "Maximum Pen Speed";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                m_Features.Add(key, new MaxSpeed(m_Speed.Profile));

            key = "Minimum Pen Speed";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                m_Features.Add(key, new MinSpeed(m_Speed.Profile));

            key = "Difference Between Maximum and Minimum Pen Speed";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                m_Features.Add(key, new MaxMinDiffSpeed(m_Speed.Profile));

            key = "Time to Draw Stroke";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                m_Features.Add(key, new StrokeTime(m_Stroke.Points));

            #endregion

            key = "Number of Self Intersections";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                m_Features.Add(key, new NumSelfIntersection(m_Stroke.Points));

            key = "Self Enclosing";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
                m_Features.Add(key, new SelfEnclosing(m_Stroke.Points, m_Features["Arc Length"].Value));
        }

        /// <summary>
        /// Calculates the features which depend on other strokes 
        /// but are not going to change.
        /// </summary>
        public void CalculateMultiStrokeFeatures(SortedList<ulong, Substroke> order, System.Drawing.RectangleF sketchBox)
        {
            string key = "Time to Previous Stroke";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
            {
                if (!m_Features.ContainsKey(key))
                    m_Features.Add(key, new TimeToPrevious(order, m_Stroke));
                else
                    m_Features[key] = new TimeToPrevious(order, m_Stroke);
            } 
            
            key = "Time to Next Stroke";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
            {
                if (!m_Features.ContainsKey(key))
                    m_Features.Add(key, new TimeToNext(order, m_Stroke));
                else
                    m_Features[key] = new TimeToNext(order, m_Stroke);
            }

            key = "Distance To Left or Right Edge";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
            {
                if (!m_Features.ContainsKey(key))
                    m_Features.Add(key, new DistanceToLREdge(sketchBox, m_BoundingBox));
                else
                    m_Features[key] = new DistanceToLREdge(sketchBox, m_BoundingBox);
            }

            key = "Distance To Top or Bottom Edge";
            if (m_FeaturesToUse.ContainsKey(key) && m_FeaturesToUse[key])
            {
                if (!m_Features.ContainsKey(key))
                    m_Features.Add(key, new DistanceToTBEdge(sketchBox, m_BoundingBox));
                else
                    m_Features[key] = new DistanceToTBEdge(sketchBox, m_BoundingBox);
            }
        }

        public void SetIntersectionFeatures(Dictionary<string, int> intersections)
        {
            if (intersections == null)
                return;

            foreach (KeyValuePair<string, int> pair in intersections)
            {
                if (pair.Key == "Number of 'LL' Intersections")
                {
                    if (!m_Features.ContainsKey(pair.Key))
                        m_Features.Add(pair.Key, new NumLLIntersection(pair.Value));
                    else
                        m_Features[pair.Key] = new NumLLIntersection(pair.Value);
                }
                else if (pair.Key == "Number of 'XX' Intersections")
                {
                    if (!m_Features.ContainsKey(pair.Key))
                        m_Features.Add(pair.Key, new NumXXIntersection(pair.Value));
                    else
                        m_Features[pair.Key] = new NumXXIntersection(pair.Value);
                }
                else if (pair.Key == "Number of 'LX' Intersections")
                {
                    if (!m_Features.ContainsKey(pair.Key))
                        m_Features.Add(pair.Key, new NumLXIntersection(pair.Value));
                    else
                        m_Features[pair.Key] = new NumLXIntersection(pair.Value);
                }
                else if (pair.Key == "Number of 'XL' Intersections")
                {
                    if (!m_Features.ContainsKey(pair.Key))
                        m_Features.Add(pair.Key, new NumXLIntersection(pair.Value));
                    else
                        m_Features[pair.Key] = new NumXLIntersection(pair.Value);
                }
                else
                    throw new Exception("Unknown type of intersection");
            }
        }

        #endregion

        #region Getters & Properties

        public Dictionary<string, Feature> Features
        {
            get { return m_Features; }
            set { m_Features = value; }
        }

        public Substroke Stroke
        {
            get { return m_Stroke; }
        }

        private bool UseThetas
        {
            get 
            {
                if (m_FeaturesToUse.ContainsKey("Sum of Thetas") && m_FeaturesToUse["Sum of Thetas"])
                    return true;
                else if (m_FeaturesToUse.ContainsKey("Sum of Abs Value of Thetas") && m_FeaturesToUse["Sum of Abs Value of Thetas"])
                    return true;
                else if (m_FeaturesToUse.ContainsKey("Sum of Squared Thetas") && m_FeaturesToUse["Sum of Squared Thetas"])
                    return true;
                else if (m_FeaturesToUse.ContainsKey("Sum of Sqrt of Thetas") && m_FeaturesToUse["Sum of Sqrt of Thetas"])
                    return true;
                else
                    return false;
            }
        }

        private bool UseSpeeds
        {
            get
            {
                if (m_FeaturesToUse.ContainsKey("Average Pen Speed") && m_FeaturesToUse["Average Pen Speed"])
                    return true;
                else if (m_FeaturesToUse.ContainsKey("Maximum Pen Speed") && m_FeaturesToUse["Maximum Pen Speed"])
                    return true;
                else if (m_FeaturesToUse.ContainsKey("Minimum Pen Speed") && m_FeaturesToUse["Minimum Pen Speed"])
                    return true;
                else if (m_FeaturesToUse.ContainsKey("Difference Between Maximum and Minimum Pen Speed") && m_FeaturesToUse["Difference Between Maximum and Minimum Pen Speed"])
                    return true;
                else
                    return false;
            }
        }

        private bool UseArcLength
        {
            get
            {
                if (m_FeaturesToUse.ContainsKey("Arc Length") && m_FeaturesToUse["Arc Length"])
                    return true;
                else if (m_FeaturesToUse.ContainsKey("Path Density") && m_FeaturesToUse["Path Density"])
                    return true;
                else if (m_FeaturesToUse.ContainsKey("End Point to Arc Length Ratio") && m_FeaturesToUse["End Point to Arc Length Ratio"])
                    return true;
                else if (m_FeaturesToUse.ContainsKey("Self Enclosing") && m_FeaturesToUse["Self Enclosing"])
                    return true;
                else if (m_FeaturesToUse.ContainsKey("Average Pen Speed") && m_FeaturesToUse["Average Pen Speed"])
                    return true;
                else
                    return false;
            }
        }

        public bool UseClosedPath
        {
            get
            {
                if (m_FeaturesToUse.ContainsKey("Part of a Closed Path") && m_FeaturesToUse["Part of a Closed Path"])
                    return true;
                else if (m_FeaturesToUse.ContainsKey("Inside a Closed Path") && m_FeaturesToUse["Inside a Closed Path"])
                    return true;
                else
                    return false;
            }
        }

        public bool UseIntersections
        {
            get
            {
                if (m_FeaturesToUse.ContainsKey("Number of 'LL' Intersections") && m_FeaturesToUse["Number of 'LL' Intersections"])
                    return true;
                else if (m_FeaturesToUse.ContainsKey("Number of 'XX' Intersections") && m_FeaturesToUse["Number of 'XX' Intersections"])
                    return true;
                else if (m_FeaturesToUse.ContainsKey("Number of 'LX' Intersections") && m_FeaturesToUse["Number of 'LX' Intersections"])
                    return true;
                else if (m_FeaturesToUse.ContainsKey("Number of 'XL' Intersections") && m_FeaturesToUse["Number of 'XL' Intersections"])
                    return true;
                else
                    return false;
            }
        }

        public Spatial Spatial
        {
            get { return m_Spatial; }
        }

        public Speed Speed
        {
            get { return m_Speed; }
        }

        #endregion
    }
}
