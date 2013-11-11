/*
 * File: Fragment.cs
 *
 * Author: Originally unknown, modified by James Brown
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2006-2008.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */

using System;
using System.Collections.Generic;
using Featurefy;

namespace Fragmenter
{
	/// <summary>
	/// Interaction class for managing fragmenting
	/// </summary>
	public static class Fragment
	{
		/// <summary>
		/// Fragments a given Sketch.Sketch.
		/// </summary>
		/// <param name="sketch">Sketch to fragment</param>
		public static void fragmentSketch(Sketch.Sketch sketch)
		{
			// Cleaned sketch
			sketch = (new Featurefy.CleanSketch(sketch)).CleanedSketch;
				
			Sketch.Stroke[] strokes = sketch.Strokes;

			// Create the featured strokes
			Featurefy.FeatureStroke[] fStrokes = new Featurefy.FeatureStroke[strokes.Length];
			for (int i = 0; i < strokes.Length; i++)
			{
				fStrokes[i] = new FeatureStroke(strokes[i]);
			}

			// Break up the Sketch's strokes based on the Corners found for the FeatureStrokes
			for (int i = 0; i < fStrokes.Length; i++)
			{
				// Find the corners
				int[] corners = new Corners(fStrokes[i]).FindCorners();

				// Split the Stroke at those corners
				strokes[i].SplitStrokeAt(corners);
			}
		}

		/// <summary>
		/// Unfragments a given Sketch.Sketch
		/// </summary>
		/// <param name="sketch">Sketch to unfragment</param>
		public static void unFragmentSketch(Sketch.Sketch sketch)
		{
			for (int i = 0; i < sketch.Strokes.Length; ++i)
			{
				Sketch.Stroke stroke = sketch.Strokes[i];
				if(stroke.Substrokes.Length > 1)
				{
					Sketch.Substroke first = stroke.Substrokes[0];
					List<Sketch.Substroke> sss = new List<Sketch.Substroke>(stroke.Substrokes);
					for (int j = 1; j < sss.Count; ++j)
						first.AddSubstroke(sss[j]);
					stroke.UpdateAttributes();
				}
			}
		}
	}
}
