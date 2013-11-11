/**
 * File: Label.cs
 *
 * Authors: Matthew Weiner, Howard Chen, and Sam Gordon
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2007.
 * 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Ink;
using TextRecognition;
using Sketch;

namespace CircuitRec
{
	/// <summary>
	/// This class describes an object that is a Label of a Circuit
	/// </summary>
	public class Label
	{
		#region Internals

        /// <summary>
        /// A List of the points in the Label.
        /// </summary>
        private List<Point> labelPoints;

        /// <summary>
        /// A List of the substrokes int the Label.
        /// </summary>
		private List<Substroke> substrokes;

        /// <summary>
        /// The results of the text recognition of the Label.
        /// </summary>
        private string textValue;

        /// <summary>
        /// The wire closest to the Label.
        /// </summary>
        private Wire closestWire;

        /// <summary>
        /// The Mesh that the Label is associated with.
        /// </summary>
        private Mesh associatedSWire;

        /// <summary>
        /// The average X-coordinate of the Label.
        /// </summary>
        private double averageX;

        /// <summary>
        /// The average Y-coordinate of the Label.
        /// </summary>
        private double averageY;

        /// <summary>
        /// Specifies how many alternates to return for the text recognition.
        /// </summary>
        private const int NUMBER_OF_ALTERNATES = 5;

        /// <summary>
        /// The array of alternates of the text recognition of the Label.  The top choice is the first element,
        /// followed by the next best, etc.
        /// </summary>
        private string[] alternateText;
		
		#endregion

		#region Constructor

        /// <summary>
        /// Constructor for Label.
        /// </summary>
        /// <param name="label">The Shape of the Label</param>
		public Label(Sketch.Shape label, ref WordList wordList)
		{
            labelPoints = new List<Point>();
            substrokes = new List<Substroke>();

            // Adds the points and substrokes of the label
			getPoints(label);

            // Implement the text recognition of the label (choose the first element from the list of alternates since this is the top choice)
            this.alternateText = TextRecognition.TextRecognition.recognizeAlternates(label,wordList,RecognitionModes.Coerce,NUMBER_OF_ALTERNATES);
            this.textValue = alternateText[0];

            // Debug
            //Console.WriteLine(textValue);
            
            // Finds the average coordinates of the label
            averageCoords();
		}
        
        /// <summary>
        /// Constructor for Label.
        /// </summary>
        public Label()
        {
        }

		#endregion

		#region Methods

		/// <summary>
		/// Extracts the Points from the Shape of the Label
		/// </summary>
		/// <param name="label">The Shape from which to extract the Points</param>
        private void getPoints(Sketch.Shape label)
		{
			List<Point> labelpoints = new List<Point>();
            List<Substroke> subs = new List<Substroke>();
            foreach (Substroke sub in label.Substrokes)
            {
                subs.Add(sub);
                foreach (Point p in sub.PointsL)
                {
                    labelpoints.Add(p);
                }
            }

            this.substrokes = subs;
            this.labelPoints = labelpoints;
		
		}

        /// <summary>
        /// Finds the average coordinates for the label.
        /// </summary>
        private void averageCoords()
        {
            double avgX = 0;
            double avgY = 0;
            int totalpoints = labelPoints.Count;
            foreach (Point p in labelPoints)
            {
                avgX += p.X;
                avgY += p.Y;
            }

            this.averageX = avgX / totalpoints;
            this.averageY = avgY / totalpoints;
        }

        #endregion

        #region Getters and Setters

        /// <summary>
        /// A List of the points in the Label.
        /// </summary>
        public List<Point> Points
            {
            get
            {
                return this.labelPoints;
            }
        }

        /// <summary>
        /// A List of the substrokes int the Label.
        /// </summary>
        public List<Substroke> Substrokes
            {
            get
            {
                return this.substrokes;
            }
        }

        /// <summary>
        /// The result of the text recognition on the Label.
        /// </summary>
        /// <returns>The value of the label (ie. A, B, C, Cin, Cout, X, Y, etc.) as a string</returns>
        public string Text
        {
            get
            {
                return this.textValue;
            }
        }

        /// <summary>
        /// The wire closest to the Label.
        /// </summary>
        public Wire ClosestWire
        {
            get
            {
                return this.closestWire;
            }
            set
            {
                this.closestWire = value;
            }
        }

        /// <summary>
        /// The Mesh that the Label is associated with.
        /// </summary>
        public Mesh AssociatedSWire
        {
            get
            {
                return this.associatedSWire;
            }
            set
            {
                this.associatedSWire = value;
            }
        }

        /// <summary>
        /// The average X-coordinate of the Label.
        /// </summary>
        public double AverageX
        {
            get
            {
                return this.averageX;
            }
        }

        /// <summary>
        /// The average Y-coordinate of the Label.
        /// </summary>
        public double AverageY
        {
            get
            {
                return this.averageY;
            }
        }

        /// <summary>
        /// The alternate results of the text recognition.
        /// </summary>
        public string[] AlternateText
        {
            get
            {
                return this.alternateText;
            }
        }

		#endregion
	}
}
