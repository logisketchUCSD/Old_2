/*
 * File: ClustererSettings.cs
 *
 * Author: Eric Peterson
 * Eric.J.Peterson@gmail.com
 * University of California, Riverside
 * Smart Tools Lab 2009
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Utilities;
using StrokeClassifier;
using StrokeGrouper;
using ImageRecognizer;
using ImageRecognizerWrapper;

namespace Clusterer
{
    [Serializable]
    public class ClustererSettings : ISerializable
    {
        #region Member Variables

        User m_User;

        PlatformUsed m_Platform;

        StrokeClassifierSettings m_ClassifierSettings;

        StrokeGrouperSettings m_GrouperSettings;

        ImageRecognizerSettings m_ImageRecognizerSettings;

        string m_ImageAlignerFilename;

        Dictionary<string, double[]> m_AvgsAndStdDevs;

        int m_SearchNeighborhoodCount = 4;

        double m_SearchNeighborhoodRadius = 500.0;

        #endregion

        #region Constructors

        public ClustererSettings()
        {
            m_ClassifierSettings = new StrokeClassifierSettings();
            m_GrouperSettings = new StrokeGrouperSettings();
            m_ImageRecognizerSettings = new ImageRecognizerSettings();
            m_AvgsAndStdDevs = new Dictionary<string, double[]>();
        }

        public ClustererSettings(StrokeClassifierSettings classifierSettings)
        {
            m_ClassifierSettings = classifierSettings;
            m_GrouperSettings = new StrokeGrouperSettings();
            m_ImageRecognizerSettings = new ImageRecognizerSettings();
            m_AvgsAndStdDevs = new Dictionary<string, double[]>();
        }

        public ClustererSettings(StrokeClassifierSettings classifierSettings, ImageRecognizerSettings imageSettings)
        {
            m_ClassifierSettings = classifierSettings;
            m_GrouperSettings = new StrokeGrouperSettings();
            m_ImageRecognizerSettings = imageSettings;
            m_AvgsAndStdDevs = new Dictionary<string, double[]>();
        }

        public ClustererSettings(StrokeClassifierSettings classifierSettings, StrokeGrouperSettings grouperSettings)
        {
            m_ClassifierSettings = classifierSettings;
            m_GrouperSettings = grouperSettings;
            m_ImageRecognizerSettings = new ImageRecognizerSettings();
            m_AvgsAndStdDevs = new Dictionary<string, double[]>();
        }

        public ClustererSettings(StrokeClassifierSettings classifierSettings, StrokeGrouperSettings grouperSettings, ImageRecognizerSettings imageSettings)
        {
            m_ClassifierSettings = classifierSettings;
            m_GrouperSettings = grouperSettings;
            m_ImageRecognizerSettings = imageSettings;
            m_AvgsAndStdDevs = new Dictionary<string, double[]>();
        }

        public ClustererSettings(StrokeGrouperSettings grouperSettings)
        {
            m_ClassifierSettings = new StrokeClassifierSettings();
            m_GrouperSettings = grouperSettings;
            m_ImageRecognizerSettings = new ImageRecognizerSettings();
            m_AvgsAndStdDevs = new Dictionary<string, double[]>();
        }

        public ClustererSettings(StrokeGrouperSettings grouperSettings, ImageRecognizerSettings imageSettings)
        {
            m_ClassifierSettings = new StrokeClassifierSettings();
            m_GrouperSettings = grouperSettings;
            m_ImageRecognizerSettings = imageSettings;
            m_AvgsAndStdDevs = new Dictionary<string, double[]>();
        }

        public ClustererSettings(ImageRecognizerSettings imageSettings)
        {
            m_ClassifierSettings = new StrokeClassifierSettings();
            m_GrouperSettings = new StrokeGrouperSettings();
            m_ImageRecognizerSettings = imageSettings;
            m_AvgsAndStdDevs = new Dictionary<string, double[]>();
        }

        #endregion

        #region Getters

        public User CurrentUser
        {
            get { return m_User; }
            set { m_User = value; }
        }

        public PlatformUsed CurrentPlatform
        {
            get { return m_Platform; }
            set { m_Platform = value; }
        }

        public StrokeClassifierSettings ClassifierSettings
        {
            get { return m_ClassifierSettings; }
            set { m_ClassifierSettings = value; }
        }

        public StrokeGrouperSettings GrouperSettings
        {
            get { return m_GrouperSettings; }
            set { m_GrouperSettings = value; }
        }

        public ImageRecognizerSettings ImageRecoSettings
        {
            get { return m_ImageRecognizerSettings; }
            set { m_ImageRecognizerSettings = value; }
        }

        public string ImageAlignerFilename
        {
            get { return m_ImageAlignerFilename; }
            set { m_ImageAlignerFilename = value; }
        }

        public Dictionary<string, double[]> AvgsAndStdDevs
        {
            get { return m_AvgsAndStdDevs; }
            set 
            { 
                m_AvgsAndStdDevs = value;
                m_ClassifierSettings.AvgsAndStdDevs = m_AvgsAndStdDevs;
                m_GrouperSettings.AvgsAndStdDevs = m_AvgsAndStdDevs;
            }
        }

        public int SearchNeighborhoodCount
        {
            get { return m_SearchNeighborhoodCount; }
            set { m_SearchNeighborhoodCount = value; }
        }

        public double SearchNeighborhoodRadius
        {
            get { return m_SearchNeighborhoodRadius; }
            set { m_SearchNeighborhoodRadius = value; }
        }

        #endregion

        #region Serialization/Loading/Saving

        /// <summary>
        /// Deserialization Constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public ClustererSettings(SerializationInfo info, StreamingContext ctxt)
        {
            //Get the values from info and assign them to the appropriate properties
            m_AvgsAndStdDevs = (Dictionary<string, double[]>)info.GetValue("AvgsAndStdDevs", typeof(Dictionary<string, double[]>));
            m_ClassifierSettings = (StrokeClassifierSettings)info.GetValue("ClassifierSettings", typeof(StrokeClassifierSettings));
            m_Platform = (PlatformUsed)info.GetValue("CurrentPlatform", typeof(PlatformUsed));
            m_User = (User)info.GetValue("CurrentUser", typeof(User));
            m_GrouperSettings = (StrokeGrouperSettings)info.GetValue("GrouperSettings", typeof(StrokeGrouperSettings));
            m_ImageRecognizerSettings = (ImageRecognizerSettings)info.GetValue("ImageRecognizerSettings", typeof(ImageRecognizerSettings));
            m_SearchNeighborhoodCount = (int)info.GetValue("SearchNeighborhoodCount", typeof(int));
            m_SearchNeighborhoodRadius = (double)info.GetValue("SearchNeighborhoodRadius", typeof(double));
        }

        /// <summary>
        /// Serialization Function
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("AvgsAndStdDevs", m_AvgsAndStdDevs);
            info.AddValue("ClassifierSettings", m_ClassifierSettings);
            info.AddValue("CurrentPlatform", m_Platform);
            info.AddValue("CurrentUser", m_User);
            info.AddValue("GrouperSettings", m_GrouperSettings);
            info.AddValue("ImageRecognizerSettings", m_ImageRecognizerSettings);
            info.AddValue("SearchNeighborhoodCount", m_SearchNeighborhoodCount);
            info.AddValue("SearchNeighborhoodRadius", m_SearchNeighborhoodRadius);
        }

        public static ClustererSettings Load(string filename)
        {
            System.IO.Stream stream = System.IO.File.Open(filename, System.IO.FileMode.Open);
            BinaryFormatter bformatter = new BinaryFormatter();
            ClustererSettings obj = (ClustererSettings)bformatter.Deserialize(stream);
            stream.Close();

            return obj;
        }

        public void Save(string filename)
        {
            System.IO.Stream stream = System.IO.File.Open(filename, System.IO.FileMode.Create);
            BinaryFormatter bformatter = new BinaryFormatter();
            bformatter.Serialize(stream, this);
            stream.Close();
        }

        #endregion
    }
}
