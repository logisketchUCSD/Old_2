/*
 * VERY IMPORTANT NOTE:
 * 
 * The Fragmenter can be compiled as a Console Application or a Class Library.
 * The Console Application is mainly used for debugging purposes or to fragment existing XML files, whereas the 
 * Class Library is used in other programs such as JntToXml.
 * 
 * Right click on the Fragmenter Project and change the Output Type to reflect what you want.
 */


using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using System.Windows.Forms;

using ConverterXML;
using Sketch;
using Featurefy;

namespace Fragmenter
{
	/// <summary>
	/// Fragmenter application. Takes in MIT XML files and outputs the fragmented (a.k.a. segmented)
	/// versions of those files.
	/// </summary>
	class Fragmenter
	{
		#region MAIN CONSOLE APPLICATION
		
		// For debugging purposes:
		// -d -g "C:\Documents and Settings\Da Vinci\My Documents\Visual Studio Projects\branches\redesign\LabeledData\TestCases\convertedJnt" 
		// -d -g "C:\Documents and Settings\Da Vinci\Desktop\convertedJnt"
		// -d -g "C:\Documents and Settings\Da Vinci\Desktop\SURVEY DATA"

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			ArrayList argArray = new ArrayList(args);			
			int numArgs = argArray.Count;
			bool showDebugWindow = false;

			string[] files;
 
			// Check if any arguments were entered
			if (numArgs == 0)
			{
				Console.WriteLine("*****************************************************************");
				Console.WriteLine("*** Fragmenter.exe");
				Console.WriteLine("*** by Aaron Wolin, Devin Smith, Jason Fennell, and Max Pflueger.");
				Console.WriteLine("*** Harvey Mudd College, Claremont, CA 91711.");
				Console.WriteLine("*** Sketchers 2006.");
				Console.WriteLine("***");
				Console.WriteLine("*** Usage: Fragmenter.exe (-c | -d directory | -r) [-g]");
				Console.WriteLine("*** Usage: Fragmenter.exe input1.xml [input2.xml ...] [-g]");
				Console.WriteLine("***");
				Console.WriteLine("*** -c: convert all files in current directory");
				Console.WriteLine("*** -d directory: convert all files in the specified directory");
				Console.WriteLine("*** -r: recursively convert files from the current directory");
				Console.WriteLine("*** -g: display a debug window for the file");
			
				return;
			}
			else
			{
				// See if we wish to display the Debug window
				if (argArray.Contains("-g"))
				{
					showDebugWindow = true;
					argArray.Remove("-g");
				}
			
				// Convert everything in this directory
				if (argArray.Contains("-c"))
				{
					files = Directory.GetFiles(Directory.GetCurrentDirectory());
				}

				// Convert everything in specified directory
				else if (argArray.Contains("-d")) 
				{
					if (argArray.IndexOf("-d") + 1  >= argArray.Count)	// Are we in range?
					{
						Console.Error.WriteLine("No directory specified.");
						return;
					}
					else if (!Directory.Exists((string)argArray[argArray.IndexOf("-d") + 1])) // Does dir exist?
					{
						Console.Error.WriteLine("Directory doesn't exist.");
						return;
					}
					else
						files = Directory.GetFiles((string)argArray[argArray.IndexOf("-d") + 1]);
				}

				// Recursive from current dir
				else if (argArray.Contains("-r"))
				{
					// Get recursive files
					ArrayList rFiles = new ArrayList();
					DirSearch(Directory.GetCurrentDirectory(), ref rFiles);
						
					// Get current dir files
					string [] currDir = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.xml");

					files = new string[rFiles.Count + currDir.Length];

					// Populate both recursive and current into files
					int current;
					for (current = 0; current < currDir.Length; ++current)
						files[current] = currDir[current];

					foreach (string s in rFiles)
					{
						files[current++] = s;
					}
				}

				// Convert only the specified files
				else 
				{
					files = (string[])argArray.ToArray(typeof(string));
				}
			}
			
			// Loop through the file input and convert each one
			foreach (string input in files)
			{
				Sketch.Sketch sketch = null;
				
				// Try reading the file
				try
				{
					// Original sketch
					sketch = (new ReadXML(input)).Sketch;

					// Cleaned sketch
					sketch = (new Featurefy.CleanSketch(sketch)).CleanedSketch;
				}
				catch (Exception e)
				{
					MessageBox.Show(e.Message);	
				}

				// Try writing the file
				try
				{
					Featurefy.FeatureStroke[] fStrokes;
					sketch = fragmentSketch(sketch, out fStrokes);

					// Show the Debug window if we specified it
					if (showDebugWindow && fStrokes != null)
					{
						try
						{
							Application.Run( new DebugForm(fStrokes, Path.GetFileNameWithoutExtension(input)) );
						}
						catch (Exception debugException)
						{
							MessageBox.Show(debugException.Message);
							continue;
						}
					}

					// Make the XML
					MakeXML xml = new ConverterXML.MakeXML(sketch);
				
					string subDir = Path.GetDirectoryName(input) + "\\fragmentedXml";
					Directory.CreateDirectory(subDir);
					string output = subDir + "\\" + Path.GetFileNameWithoutExtension(input) + ".fragged.xml";
			
					// Write the file
					xml.WriteXML(output);

					Console.WriteLine("   Wrote: " + output);
				}
				catch (Exception writeException)
				{
					Console.WriteLine(writeException.Message);
					continue;
				}
			}
		}

		#endregion

		#region DIRECTORY SEARCHING

		/// <summary>
		/// Perform a recursive directory search. http://support.microsoft.com/default.aspx?scid=kb;en-us;303974
		/// </summary>
		/// <param name="sDir">Directory to search recursively</param>
		/// <param name="rFiles">Array to add the files to</param>
		static void DirSearch(string sDir, ref ArrayList rFiles) 
		{
			try	
			{
				foreach (string d in Directory.GetDirectories(sDir)) 
				{
					foreach (string f in Directory.GetFiles(d, "*.xml")) 
					{
						rFiles.Add(f);
					}

					DirSearch(d, ref rFiles);
				}
			}
			catch (System.Exception excpt) 
			{
				Console.WriteLine(excpt.Message);
			}
		}

		#endregion

		#region FRAGMENT STROKE

		/// <summary>
		/// Fragments a given Sketch.Sketch and returns the fragmented version.
		/// Also outputs the Featurefied strokes.
		/// </summary>
		/// <param name="sketch">Sketch to fragment</param>
		/// <param name="fStrokes">FeatureStrokes to be created and output</param>
		/// <returns>Fragmented sketch</returns>
		public static Sketch.Sketch fragmentSketch(Sketch.Sketch sketch, out Featurefy.FeatureStroke[] fStrokes)
		{
			Sketch.Stroke[] strokes = sketch.Strokes;

			// Create the featured strokes
			fStrokes = new Featurefy.FeatureStroke[strokes.Length];
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

			// NOTE... WE REALLY DONT NEED TO RETURN THIS SKETCH, IT IS ALREADY MODIFIED
			return sketch;
		}

		#endregion
	}
}
