#define HILL_CLIMBING

/*
 * File: Designation.cs
 *
 * Author: Jason Fennell, James Brown
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2008.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 * 
 * 
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Metrics;
using MathNet.Numerics.Distributions;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Aligner;

namespace Congeal
{
    /// <summary>
    /// The Metrics we currently support for classification. One way to classify
    /// is to take the minimum value ouput by each metric.
    /// </summary>
    public enum classifyMetric
    {
		/// <summary>
		/// Directed, modified hausdorff distances. This may be able to detect some cases of over/under grouping
		/// </summary>
		DirectedModifiedHausdorff,
		/// <summary>
		/// Directed Hausdorff distance with a 6% cutoff for resistance to outliers, as described in the Kara paper
		/// </summary>
		DirectedPartialHausdorff,
		/// <summary>
		/// Use Hausdorff distance
		/// </summary>
        Hausdorff,
		/// <summary>
		/// Use Hausdorff distance without congealing
		/// </summary>
        HausdorffNC,
		/// <summary>
		/// Use Entropy distance (an overlapping similarity measure)
		/// </summary>
        Entropy,
		/// <summary>
		/// Use Tanimoto distance (an overlapping similarity measure)
		/// </summary>
        TANIMOTO,
		/// <summary>
		/// Use Yule distance (an overlapping similarity measure)
		/// </summary>
        YULE,
		/// <summary>
		/// Attempt to use an actual probability. Liek, zomg.
		/// </summary>
		Probability,
		/// <summary>
		/// Combine the distance metrics, as described in the Kara paper
		/// </summary>
        ALL
    };

    /// <summary>
    /// Each Designation respresents a single class of data (for example, a gate or a wire). This
	/// class is what does the bulk of the legwork in congealing, and is capable of both training and
	/// classification. 
	/// 
	/// To save training data, serialize one of these
    /// </summary>
	[Serializable]
	public class Designation
	{
		#region Members/Internals

		private Sketch.Shape canonicalShape;

		[NonSerialized]
		private List<ImageTransform> m_images = new List<ImageTransform>();
		private List<AvgImage> models;
		private List<Warp> warps;
		private bool isTrained = false;
		private string m_name;
		[NonSerialized]
		private int num_desigs;

		private int m_width;
		private int m_height;

		private int DebugCounter = 0;

		private static double maxScalar = 1.5;
		private static double maxClassifyScalar = 1.00;
		private static double slope = -0.10;
		private static double classifySlope = -.05;

		private double STARTING_TEMPERATURE = 1;
		// Temperature (i.e., activity) will drop off proportional to e^ this
		private double EXPONENTIAL_DROPOFF_RATE = 50;

		#if HILL_CLIMBING
		private int DEFAULT_ITERATIONS = 5;
		private double DEFAULT_TARGET_ENERGY = 0.005;
		#endif

		#if SIMULATED_ANNEALING
        private int DEFAULT_ITERATIONS = 5000;
        private double DEFAULT_TARGET_ENERGY = 50;
		#endif
		[NonSerialized]
		private Random gen;

		#endregion

		#region Accessors

		/// <summary>
		/// The width of the trained image
		/// </summary>
		public int Width
		{
			get
			{
				return m_width;
			}
		}

		/// <summary>
		/// The height of the trained image
		/// </summary>
		public int Height
		{
			get
			{
				return m_height;
			}
		}

		/// <summary>
		/// The name of the trained image
		/// </summary>
		public string Name
		{
			get
			{
				return m_name;
			}
		}

		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new Designation from bitmaps
		/// </summary>
		/// <param name="bitmaps">List of images, all pictures of the same type of symbol.</param>
		/// <param name="label">the name of the symbol.
		/// <example>For example, AND</example></param>
		/// <param name="num_designations">The total number of designations being used</param>
		/// <param name="canonical">A canonical example of this class</param>
		public Designation(List<TrainingDataPreprocessor.TrainingData.GateImage> bitmaps, string label, int num_designations, Sketch.Shape canonical)
		{
			//m_Warps = new UnitWarps(bitmaps[0].Width, bitmaps[0].Height);

			m_width = bitmaps[0].bitmap.Width;
			m_height = bitmaps[0].bitmap.Height;
			m_name = label;
			for (int bmpIdx = 0; bmpIdx < bitmaps.Count; ++bmpIdx)
			{
				ImageTransform newIT = new ImageTransform(bitmaps[bmpIdx].bitmap);
				m_images.Add(newIT);
			}

			// Create a new random number generator and seed it with the current time
			gen = new Random(DateTime.Now.GetHashCode());
			models = new List<AvgImage>();
			warps = new List<Warp>();
			num_desigs = num_designations;
			canonicalShape = canonical;
		}

		/// <summary>
		/// Creates a new Designation from Sketch objects
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="sketches">List of sketches, all representing the same type of symbol</param>
		/// <param name="label">name of the symbol. <example>ie. NOR</example></param>
		/// <param name="num_designations">The total number of designations</param>
		/// <param name="canonical">A canonical example of this class</param>
		public Designation(int width, int height, List<Sketch.Sketch> sketches, string label, int num_designations, Sketch.Shape canonical)
		{
			m_width = width;
			m_height = height;
			this.m_name = label;
			for (int i = 0; i < sketches.Count; ++i)
			{
				m_images.Add(new ImageTransform(width, height, sketches[i]));
			}

			// Create a new random number generator and seed it with the current time
			gen = new Random(DateTime.Now.GetHashCode());
			models = new List<AvgImage>();
			warps = new List<Warp>();

			num_desigs = num_designations;
			canonicalShape = canonical;
		}


		#endregion

		#region Training

		/// <summary>
		/// Trains this Designation instance using default numbers of iterations and
		/// a default epsilon
		/// </summary>
		public void train()
		{
			    train(DEFAULT_ITERATIONS, DEFAULT_TARGET_ENERGY);
		}

		#region Simulated Annealing

		#if SIMULATED_ANNEALING
		/// <summary>
		/// Train this Designation instance by congealing its ImageTransforms together and using
		/// simulated annealing for optimization
		/// </summary>
		/// <param name="maxIters">Maximum number of congealing iterations</param>
		/// <param name="target_entropy">Entropy target</param>
		public void train(int maxIters, double target_energy)
		{
			AvgImage average_image = new AvgImage(m_images);
            int numImgs = m_images.Count;
			double e = getEnergy(average_image);
			double initial_e = e;
			double next_e = 0.0;
			double prob = 0.0;
			models.Add(average_image);
			#if DEBUG
				Console.WriteLine("Beginning training for gate " + m_name);
			#endif
			int step = 0;

			// Debugging variables
			int bad_added = 0;
			int good_added = 0;
			double bad = 0.0D;

            Aligner.BruteForceFourWayAlign b = new BruteForceFourWayAlign(new SymbolRec.Image.Image(Width, Height, canonicalShape));
            Console.Write("Performing pre-alignment... 00% Completed");
            for(int sidx = 0; sidx < numImgs; ++sidx)
            {
                double pct = (double)sidx / (double)numImgs;
                Console.Write("\b\b\b\b\b\b\b\b\b\b\b\b\b");
                if (pct > 0.995)
                    Console.Write("\b");
                Console.Write("{0:00%} Completed", pct);

                CongealParameters bp = new CongealParameters(m_height, m_width);
                bp.theta = b.align(m_images[sidx].LatentImage);
                if (bp.theta != 0)
                    m_images[sidx].noundo_append(bp);
            }
            Console.Write(Environment.NewLine);
            average_image = new AvgImage(m_images);
            e = getEnergy(average_image);

			while (step < maxIters && e > target_energy)
			{
				if (step % 100 == 0)
				{
					#if DEBUG
						Console.WriteLine("At iteration {0}, energy={1}", step, e);

						if (!Directory.Exists(Util.getDir() + @"\output\"))
							Directory.CreateDirectory(Util.getDir() + @"\output\");
						average_image.writeImage(String.Format("{0}{1}{2}o{3}_CO.png", Util.getDir() + @"\output\", m_name, step, maxIters));
					#else
						Console.Write(".");
					#endif
					normalize_its();
				}
				// Get a random nearby image by applying warps. Note which of the subimages we modified
				ImageTransform currentIT = m_images[gen.Next(0, m_images.Count)];
				CongealParameters newp = neighbor(currentIT.LastCP, currentIT.LastCPWasApplied);
				currentIT.append(newp);
				AvgImage next = new AvgImage(m_images);
				// Find the next entropy
				next_e = getEnergy(next);
				// What's the probabilty that we bust a move?
				prob = move_prob(e, next_e, temp(((double)step) / ((double)maxIters)));
				if (prob != 1.0D)
					bad += prob;
				if (prob > gen.NextDouble())
				{
					if (prob != 1.0D)
						bad_added++;
					else
						good_added++;
						average_image = next;
					e = next_e;
					average_image = next;
					models.Add(next);
				}
				else
				{
				    currentIT.undoCPAppend();
				}
				++step;
			}
			Console.Write(Environment.NewLine);
			Console.WriteLine("Final entropy={0}, a decrease of {1}", e, initial_e - e);
			#if DEBUG
				Console.WriteLine("Added {0} good transforms and {1} bad transforms", good_added, bad_added);
				Console.WriteLine("Average badness was {0}", bad / (step - good_added));
				if (!Directory.Exists(Util.getDir() + @"\output\"))
					Directory.CreateDirectory(Util.getDir() + @"\output\");
				average_image.writeImage(String.Format("{0}{1}{2}_CO.png", Util.getDir() + @"\output\", m_name, step));
			#endif

			// Store the transforms we used so we have them when classifying!
			foreach (ImageTransform it in m_images)
			{
				warps.Add(it.Warp);
			}

			isTrained = true;
		}
		#endif
		#endregion


		/// <summary>
		/// Normalize the ImageTransforms using the algorithm from the Congealing paper
		/// (that is to say, make it so that each transform has a 0 mean)
		/// </summary>
		private void normalize_its()
		{
			double t_x = 0.0F;
			double t_y = 0.0F;
			double x_scale = 0.0F;
			double y_scale = 0.0F;
			double x_skew = 0.0F;
			double y_skew = 0.0F;
			double theta = 0.0F;
			int count = m_images.Count;
			foreach (ImageTransform it in m_images)
			{
				t_x += it.CPs.t_x;
				t_y += it.CPs.t_y;
				x_scale += it.CPs.x_scale;
				y_scale += it.CPs.y_scale;
				x_skew += it.CPs.x_skew;
				y_skew += it.CPs.y_skew;
				theta += it.CPs.theta;
			}
			t_x /= count;
			t_y /= count;
			x_scale /= count;
			y_scale /= count;
			x_skew /= count;
			y_skew /= count;
			theta /= count;
			if (!(t_x == 0 && t_y == 0 && x_scale == 0 && y_scale == 0 && x_skew == 0 && y_skew == 0))
			{
				//Console.WriteLine("Current errors: {0},{1},{2},{3},{4},{5},{6}", t_x, t_y, x_scale, y_scale, x_skew, y_skew, theta);
				for (int idx = 0; idx < count; ++idx)
				{
					m_images[idx].normalize(t_x, t_y, x_scale, y_scale, x_skew, y_skew, theta);
				}
			}
		}

		#region Hill Climbing

		#if HILL_CLIMBING
		public void train(int maxIters, double epsilon)
		{
			//drawAllImages(m_images, "init");

			int numImgs = m_images.Count;
			AvgImage current = new AvgImage(m_images);
			double e = current.Entropy;
			models.Add(current);

			double deltaE = Double.PositiveInfinity;

			int transformsTaken = 0;

			int step = 0;

            if (canonicalShape != null)
            {
                if (!Directory.Exists(String.Format("{0}\\output\\", Util.getDir())))
                    Directory.CreateDirectory(String.Format("{0}\\output\\", Util.getDir()));
			    current.writeImage(String.Format("{0}\\output\\{1}_before_alignment.png", Util.getDir(), m_name));


    			int rotations = 0;
			    // Console.Write("Performing pre-alignment... 00% Completed");
			    BruteForceFourWayAlign b = new BruteForceFourWayAlign(new SymbolRec.Image.Image(m_width, m_height, new SymbolRec.Substrokes(canonicalShape.SubstrokesL)));
			    // Force alignment of images (i.e., brute force through)
			    for (int idx = 0; idx < numImgs; ++idx)
			    {
    				//double pct = (double)idx / (double)numImgs;
				    //Console.Write("\b\b\b\b\b\b\b\b\b\b\b\b\b");
				    //if (pct > 0.995)
				    //	Console.Write("\b");
				    //Console.Write("{0:00%} Completed", pct);
				    CongealParameters bp = new CongealParameters(m_images[idx].Height, m_images[idx].Width);
    
				    bp.theta = b.align(m_images[idx].LatentImage);
				    if (bp.theta != 0)
				    {
    					m_images[idx].noundo_append(bp);
					    ++rotations;
				    }
			    }
                //Console.Write(Environment.NewLine);
                //Console.WriteLine("Finished pre-alignment, {0} rotations performed", rotations);
            }

    		current = new AvgImage(m_images);
			e = current.Entropy;
			double lastE = e;

            while ((step < maxIters) && (deltaE > epsilon))
			{
				//Console.WriteLine("Starting iteration {0}, energy={1}, {2} transforms taken so far", step, e, transformsTaken);
				if (!Directory.Exists(Util.getDir() + @"\output\"))
					Directory.CreateDirectory(Util.getDir() + @"\output\");
				current.writeImage(String.Format(
					"{0}{1}{2}o{3}_OLD.png", Util.getDir() + @"\output\", m_name, step, maxIters));

				int numsteps = (int)(numImgs * -(maxScalar/slope) * 7); // This is the number of steps in this iteration
				int itersteps = 0;
				//Console.Write("Iteration {0}/{1} Status: ", step, maxIters);
				//Console.Write("00% Completed");


				int direction = 1; // Positive or negative transforms

				//for (double k = maxScalar; k > 0; k += slope)
                for (double k = maxScalar; k > 0; k += slope) 
				{
					for (int j = 0; j < numImgs; ++j)
					{
						//drawAllImages(m_images, String.Format("{0}_{1}", j, step));

						//Console.WriteLine("Step {0}, Image {1}, Scalar {2}, Entropy {3}", step, j, k, current.Entropy);
						for (int paramIdx = 0; paramIdx < 7; ++paramIdx) // NOTE THE 7 -- SKEWING IS NOT DISABLED!
						{
                            //percent_done = (double)itersteps / (double)numsteps;
                            //Console.Write("\b\b\b\b\b\b\b\b\b\b\b\b\b");
                            //if (percent_done > 0.995)
                            //    Console.Write("\b"); // Fix for 100%
                            //Console.Write("{0:00%} Completed", percent_done);
                            ++itersteps;
							CongealParameters p = new CongealParameters(Height, Width);
							switch (paramIdx)
							{
								case 0:
									p.t_x = k * direction;
									break;
								case 1:
									p.t_y = k * direction;
									break;
								case 2:
									p.theta = k * direction;
									break;
								case 3:
									// Ensure that we can't scale out of frame in one step
									p.x_scale = k / 5.0 * direction;
									break;
								case 4:
									// Ensure that we can't scale out of frame in one step
									p.y_scale = k / 5.0 * direction;
									break;
								case 5:
									// Shearing tends to go bad fast. So keep it to a minimum
									p.x_skew = k / 10.0 * direction;
									break;
								case 6:
									// Shearing tends to go bad fast. So keep it to a minimum
									p.y_skew = k / 10.0 * direction;
									break;
							}
							m_images[j].append(p);
							AvgImage new_image = new AvgImage(m_images);
							double enew = new_image.Entropy;
							if (enew < e)
							{
								e = enew;
								current = new_image;
								warps.Add(p.warp);
								++transformsTaken;
							}
							else
							{
								m_images[j].undoCPAppend();
								p.Invert();
								m_images[j].append(p);
								new_image = new AvgImage(m_images);
								enew = new_image.Entropy;
								if (enew < e)
								{
									direction *= -1;
									e = enew;
									current = new_image;
									warps.Add(p.warp);
									++transformsTaken;
								}
								else
								{
									m_images[j].undoCPAppend();
								}
							}
						}
					}
					normalize_its();
				}
				deltaE = Math.Abs(e - lastE);
				lastE = e;
				Console.Write(Environment.NewLine);
				++step;
			}
			models.Add(new AvgImage(m_images));
			//Console.WriteLine("Final energy={0}", e);
			if (!Directory.Exists(Util.getDir() + @"\output\"))
				Directory.CreateDirectory(Util.getDir() + @"\output\");
			current.writeImage(String.Format("{0}{1}_HILL.png", Util.getDir() + @"\output\", m_name));

			isTrained = true;
		}
		#endif

		#endregion

		#endregion

		#region Shared Simulated Annealing Stuff

		private double getEnergy(AvgImage image)
		{
			double energy = image.Entropy;
			foreach (ImageTransform it in m_images)
				energy += (Math.Pow(it.CPs.norm, 2));
			return energy;

		}

		private double getClassificationEnergy(AvgImage image, ImageTransform t)
		{
			double energy = image.getTrainingEntropy(t);
			return energy;
		}

		/// <summary>
		/// Helper function for the simulated annealing search. Finds a neighbor of the current
		/// AvgImage state
		/// </summary>
		/// <param name="start">The starting image</param>
		/// <param name="current_entropy">The current entropy</param>
		private void it_neighbor(ref ImageTransform which, double current_entropy)
		{
			// Calculate some parameters
			int translate = (int)((0.5 - gen.NextDouble()) * maxScalar);
			double scale = (0.5 - gen.NextDouble()) / 10.0;
			double shear = (0.5 - gen.NextDouble()) / 10000.0;
			double degrees = (0.5 - gen.NextDouble()) * 2;
			int which_warp = gen.Next(0, 7);

			CongealParameters p = new CongealParameters(m_height, m_width);
			switch (which_warp)
			{
				case 0:
					p.t_x = translate;
					break;
				case 1:
					p.t_y = translate;
					break;
				case 2:
					p.theta = degrees;
					break;
				case 3:
					p.x_scale = scale;
					break;
				case 4:
					p.y_scale = scale;
					break;
				case 5:
					p.x_skew = shear;
					break;
				case 6:
					p.y_skew = shear;
					break;
			}
			which.append(p);
		}

		/// <summary>
		/// Find a neighbor to the current transform state
		/// </summary>
		/// <param name="last_applied">The last transform we tried to apply</param>
		/// <param name="kept">Whether we kept the last transform</param>
		/// <returns>A new CongealParameters</returns>
		private CongealParameters neighbor(CongealParameters last_applied, bool kept)
		{
			// If the last transform was good, keep going in that direction, maybe
			if (gen.NextDouble() > 0.05 && kept)
				return last_applied * gen.NextDouble() * 2.0;

			// Otherwise try something new
			int translate = (int)((0.5 - gen.NextDouble()) * maxScalar);
			double scale = (0.5 - gen.NextDouble()) / 100.0;
			double shear = (0.5 - gen.NextDouble()) / 10000.0;
			double degrees = (0.5 - gen.NextDouble()) * maxScalar;
			int which_warp = gen.Next(0, 7);

			CongealParameters p = new CongealParameters(m_height, m_width);
			switch (which_warp)
			{
				case 0:
					p.t_x = translate;
					break;
				case 1:
					p.t_y = translate;
					break;
				case 2:
					p.theta = degrees;
					break;
				case 3:
					p.x_scale = scale;
					break;
				case 4:
					p.y_scale = scale;
					break;
				case 5:
					p.x_skew = shear;
					break;
				case 6:
					p.y_skew = shear;
					break;
			}
			return p;
		}
		
		/// <summary>
		/// Helper function for the simulated annealing search. Finds a neighbor of the current
		/// AvgImage state
		/// </summary>
		/// <param name="start">The starting image</param>
		/// <param name="current_entropy">The current entropy</param>
		/// <returns>The next neighbor state</returns>
		private AvgImage neighbor(ref ImageTransform which, double current_entropy)
		{
			it_neighbor(ref which, current_entropy);
			return new AvgImage(m_images);
		}

		/// <summary>
		/// Helper function to set the annealing schedule. Currently just linearly decreases
		/// </summary>
		/// <param name="r">The fraction of the annealing that has finished</param>
		/// <returns>The "temperature"</returns>
		private double temp(double r)
		{
			return STARTING_TEMPERATURE * Math.Exp(-EXPONENTIAL_DROPOFF_RATE * r);
		}

		/// <summary>
		/// Helper function to probabilistically determine whether to transition to the next
		/// state in a simulated annealing search. Very highly penalizes energy differencesd
		/// </summary>
		/// <param name="current_energy">The current energy</param>
		/// <param name="next_energy">The energy of the possible next state</param>
		/// <param name="temperature">The "temperature"</param>
		/// <returns>The probability of the move (in [0, 1]).</returns>
		private double move_prob(double current_energy, double next_energy, double temperature)
		{
			if (next_energy < current_energy)
				return 1.0;
			else
			{
				double p = Math.Exp((current_energy - next_energy) / temperature);
				if (p > 1.0 || p < 0.0)
					throw new Exception(String.Format("Probabilty is out of bounds; got p={0}", p));
				return p;
			}
		}

		#endregion

		#region Classification

		/// <summary>
		/// Classify an image, using congealing
		/// </summary>
		/// <param name="b">The shape to classify</param>
		/// <param name="id">ID</param>
		/// <param name="Metric">Classification metric</param>
		/// <param name="congealed"></param>
		/// <returns></returns>
		public Dictionary<string, double> classify(Sketch.Shape b, string id, classifyMetric Metric, out SymbolRec.Image.Image congealed)
		{
			return classify(b, id, DEFAULT_ITERATIONS, DEFAULT_TARGET_ENERGY, Metric, out congealed);
		}

		/// <summary>
		/// Does all the setup, then
		/// gets distance between an input bitmap and this designation for a particular
		/// classification metric.
		/// </summary>
		/// <param name="shape">The shape to recognize</param>
		/// <param name="id">An ID</param>
		/// <param name="maxIters">Maximum number of iterations to congeal for</param>
		/// <param name="target_energy">The target energy for congealing</param>
		/// <param name="congealed">The congealed image</param>
		/// <param name="Metric">The classification metric to use</param>
		/// <returns>The distance, in one or more formats</returns>
		public Dictionary<string, double> classify(Sketch.Shape shape, string id, int maxIters, double target_energy, classifyMetric Metric, out SymbolRec.Image.Image congealed)
		{
			Dictionary<string, double> distances = new Dictionary<string, double>();
			if (!isTrained)
				throw new Exception("This Designation instance must be trained before it can perform classification.");

			Sketch.Shape aligned_shape;
			if (canonicalShape != null)
			{
				// Pre-align the shape
				List<SingleStrokeAlign.AlignFeature> alignFeatures = new List<SingleStrokeAlign.AlignFeature>();
				alignFeatures.Add(SingleStrokeAlign.AlignFeature.AngleTraveled);
				alignFeatures.Add(SingleStrokeAlign.AlignFeature.LengthRank);
				alignFeatures.Add(SingleStrokeAlign.AlignFeature.AngleTraveledRank);
				IAlign prealigner = new SingleStrokeAlign(getKeystrokes()[0], canonicalShape, alignFeatures);
				aligned_shape = prealigner.align(shape);
			}
			else
			{
				aligned_shape = shape;
			}


			// Then rasterize it
			ShapeTransform t = new ShapeTransform(shape, m_width, m_height, id);
			models[models.Count - 1].Image.ClearCache();

			if (Metric == classifyMetric.HausdorffNC)
			{
				ImageDistance hd = new ImageDistance(t.LatentImage, models[models.Count - 1].Image);
				distances["Hausdorff"] = hd.Hausdorff;
				congealed = t.LatentImage;
				return distances;
			}
			else //then we need to congeal
			{
				#region Congealing
				int step = 0;
				//int numModels = models.Count;
				//maxIters = Math.Min(maxIters, numModels);  // Cannot have more iterations than the number of training steps
				ImageDistance w = new ImageDistance(models[models.Count - 1].Image, t.LatentImage);
				double e = w.Tanimoto + w.Yule;
				//double e = models[models.Count - 1].getTrainingEntropy(t);

				int direction = 1;


				//t.LatentImage.writeToBitmap(String.Format("{0}\\output\\image_{1}_pre_congeal_for{2}.png", Util.getDir(), "1", m_name));
				double lastE = e;
				double deltaE = Double.PositiveInfinity;

				int applied = 0;

				while (step < maxIters && deltaE > target_energy)
				{
					for (double k = maxClassifyScalar; k >= -classifySlope; k += classifySlope)
					{
						for (int paramIdx = 0; paramIdx < 7; ++paramIdx)
						{
							CongealParameters p = new CongealParameters(Height, Width);
							switch (paramIdx)
							{
								case 0:
									p.t_x = k * direction * (-1 / classifySlope);
									break;
								case 1:
									p.t_y = k * direction * (-1 / classifySlope);
									break;
								case 2:
									p.theta = k * direction * 5.0;
									break;
								case 3:
									// Ensure that we can't scale out of frame in one step
									p.x_scale = Math.Log(k) / 5.0 * direction;
									break;
								case 4:
									// Ensure that we can't scale out of frame in one step
									p.y_scale = Math.Log(k) / 5.0 * direction;
									break;
								case 5:
									// Shearing tends to go bad fast. So keep it to a minimum
									p.x_skew = k / 10.0 * direction;
									break;
								case 6:
									// Shearing tends to go bad fast. So keep it to a minimum
									p.y_skew = k / 10.0 * direction;
									break;
							}
							t.append(p);
							ImageDistance q = new ImageDistance(models[models.Count - 1].Image, t.LatentImage);
							double enew = q.Tanimoto + q.Yule;
							//double enew = models[models.Count - 1].getTrainingEntropy(t);
							if (enew < e)
							{
								e = enew;
								++applied;
							}
							else
							{
								t.undoCPAppend();
								p.Invert();
								t.append(p);
								enew = models[models.Count - 1].getTrainingEntropy(t);
								if (enew < e)
								{
									direction *= -1;
									e = enew;
									++applied;
								}
								else

									t.undoCPAppend();
							}
						}
					}
					++step;
					deltaE = e - lastE;
					lastE = e;
				}
				/*
				Console.WriteLine("Applied {0} transforms in congealing", applied);
				Console.WriteLine("\tD{7} Final CPs: t_x: {0:0.00}, t_y:{1:0.00}, theta:{2:0.00}, x_s:{3:0.00}, y_s:{4:0.00}, x_k:{5:0.00}, y_k:{6:0.00}", t.CPs.t_x, t.CPs.t_y, t.CPs.theta, t.CPs.x_scale, t.CPs.y_scale, t.CPs.x_skew, t.CPs.y_skew, m_name);
				t.LatentImage.writeToBitmap(String.Format("{0}\\output\\{1}_post_congealing.png", Util.getDir(), m_name));
				*/
				#endregion
				// Now we have t, which is our congealed image-to-be-classified.  We want to
				// change this into a probability
				// t.LatentImage is the resultant latent image I_L
				// t.Warp is the transformation that got it here U
				// P(I_L | U, designation/this) = HausdorffDistanceClassifier(I_L, model[finalIndex])
				// T = U^{-1}
				// P(T) = probabilityOfTransform(T);
				// return P(I_L | U, this) * P(T), then top function will return the function that maximizes this.

				// Should be working but still untested
				//Commented our right now.

			}
			double MaxHausdorffDistance = Math.Sqrt(Math.Pow(m_width, 2) + Math.Pow(m_height, 2));
			ImageDistance d = new ImageDistance(t.LatentImage, models[models.Count - 1].Image);
			if (Metric == classifyMetric.DirectedModifiedHausdorff)
			{
				double ab = d.DirectedModifiedHausdorff_AB;
				double ba = d.DirectedModifiedHausdorff_BA;
				distances["ModifiedHausdorff"] = Math.Max(ab, ba);
				//Console.WriteLine("For D={2}, ab={0}, ba={1}, ret={3}", ab, ba, m_name, distance);
			}
			else if (Metric == classifyMetric.DirectedPartialHausdorff)
			{
				double ab = d.DirectedHausdorff_AB;
				double ba = d.DirectedHausdorff_BA;
				distances["PartialHausdorff"] = Math.Max(ab, ba);
				//Console.WriteLine("For D={2}, ab={0}, ba={1}, ret={3}", ab, ba, m_name, distances["PartialHausdorff"]);
			}
			else if (Metric == classifyMetric.Hausdorff)
			{
				distances["Hausdorff"] = d.Hausdorff;
			}
			else if (Metric == classifyMetric.TANIMOTO)
			{
				distances["Tanimoto"] = 1.0 - d.Tanimoto;
			}
			else if (Metric == classifyMetric.YULE)
			{
				distances["Yule"] = 1.0 - d.Yule;
			}
			else if (Metric == classifyMetric.Entropy)
			{
				distances["Entropy"] = Math.Abs(models[models.Count - 1].getTrainingEntropy(t) - models[models.Count - 1].Entropy) / (Width * Height);
			}
			/*
			else if (Metric == classifyMetric.Probability)
			{
				/* And now, for some Math 62
				 * 
				 * We want to find P(designation | image, transform)
				 * Let I = image, U = transform, and D = designation
				 * 
				 * P(D|I,U) = (P(U|D,I) P(I|D) P(D)) / P(I|U)P(U)
				 * 
				 * P(I|D) = (1/z)Exp(-H^2/2) where H is the Hausdorff Distance, according to the Miller paper
				 * P(D) we can assume is just 1/number of possibly classifications
				 * P(U) comes from probabilityOfTransform(U^-1)
				 * 
				 * I think P(I|U) is just 1, since I is a product of U. That just leaves
				 * P(U|D, I).
				 * /
				double z = 1.0; // Aribtary constant
				double P_U = probabilityOfTransform(t.Warp.Inverse);
				double P_D = 1.0 / num_desigs;
				double P_U_D_I = 1.0; // TODO: This is DEFINITELY not true
				double P_I_D = (1 / z) * t.MinHausdorff(models);
				double P_I_U = 1.0; // TODO: Check this with somebody who knows more probability than I.

				distance = (P_U_D_I * P_I_D * P_D) / (P_I_U * P_U);
				double z = 1.0 / 1.0;
				int num_desigs = 8;
				distances["Probability"] = (1.0d * z * t.MinHausdorff(models) * 1 / num_desigs) / (1.0 * probabilityOfTransform(t.Warp.Inverse));
			}
			*/
			else if (Metric == classifyMetric.ALL)
			{
				/*
				double p = probabilityOfTransform(t);
				Console.WriteLine("Designation {0}", m_name);
				Console.WriteLine("\tProbability of Transform: {0}",p );
				Console.WriteLine("\t[Pre]  T: {0:0.00}, Y:{1:0.00}, H:{2:0.00}, MH:{3:0.00}", d.Tanimoto, d.Yule, d.Hausdorff, d.ModifiedHausdorff);
				Console.WriteLine("\t[Post] T: {0:0.00}, Y:{1:0.00}, H:{2:0.00}, MH:{3:0.00}", d.Tanimoto * p, d.Yule * p, d.Hausdorff * p, d.ModifiedHausdorff* p);
				 */
				distances["Hausdorff"] = d.Hausdorff;
				distances["ModifiedHausdorff"] = d.ModifiedHausdorff;
				distances["Yule"] = d.Yule;
				distances["Tanimoto"] = d.Tanimoto;
				distances["Entropy"] = 1.0 - (Math.Abs(models[models.Count - 1].getTrainingEntropy(t)) / (Width * Height));
			}
			else
			{ //They didn't properly define a distance metric.
				distances["Unknown"] = Double.PositiveInfinity;
			}
			congealed = t.LatentImage;
			return distances;
		}

		/*
        /// <summary>
        /// Returns a list of distances, corresponding to classify() for each of the metrics.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="id"></param>
        /// <param name="maxIters"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public List<double> classifyALL(Bitmap b, string id, int maxIters, double epsilon)
        {
            Console.Write(".");
            if (!isTrained)
            {
                throw new Exception("This Designation instance must be trained before it can perform classification.");
            }
            List<double> results = new List<double>();
            ImageTransform t = new ImageTransform(b, id);
            int numIters = 0;
            int numModels = models.Count;
            maxIters = Math.Min(maxIters, numModels);  // Cannot have more iterations than the number of training steps
            double lastIterationTEntropy = models[numIters].getTrainingEntropy(t);

            double TEntropyDelta = double.PositiveInfinity;

            double lastTEntropy = lastIterationTEntropy;

            UnitWarps m_Warps = new UnitWarps(b.Width, b.Height);

            double distance;
            distance = getDistance(t, models[models.Count - 1], ImageDistance.HAUSDORFF);
            results.Add(distance);

            //then we need to congeal
            for (numIters = 0; numIters < numModels; numIters++)
            {

                for (int i = maxScalar; i > 0; i--)
                {
                    int scalarVal = i;
                    // For w and its inverse, see if applying it will increase the Entropy of t added to the avgImg.
                    // TODO: Make it so there is not horrible code duplication here...
                    // TODO: Make this use simulated annealing instead of hill climbing... easier if above todo is done
                    lastTEntropy = applyClassifyWarps(t, numIters, lastTEntropy, scalarVal); //Modifies currentIT by applying warps
                }

                // Update the termination parameters
                TEntropyDelta = Math.Abs(lastTEntropy - lastIterationTEntropy);
                lastIterationTEntropy = lastTEntropy;
            }


            // Now we have t, which is our congealed image-to-be-classified.  We want to
            // change this into a probability
            // t.LatentImage is the resultant latent image I_L
            // t.Warp is the transformation that got it here U
            // P(I_L | U, designation/this) = HausdorffDistanceClassifier(I_L, model[finalIndex])
            // T = U^{-1}
            // P(T) = probabilityOfTransform(T);
            // return P(I_L | U, this) * P(T), then top function will return the function that maximizes this.


            // Should be working but still untested
            // Commented our right now.




            results.Add(getDistance(t, models[models.Count - 1], ImageDistance.HAUSDORFF));
            results.Add(getDistance(t, models[models.Count - 1], ImageDistance.YULE));
            results.Add(getDistance(t, models[models.Count - 1], ImageDistance.TANIMOTO));

            //Need to decrease numIters by one because that's the last time it successfully went through the for loop.

            double lastTrainedEntropy = models[numIters - 1].Entropy;
            // trying out using change in entropy as a distance metric.
            results.Add(Math.Abs(lastTrainedEntropy - lastIterationTEntropy));
            results.Add(probabilityOfTransform(t.Warp));

            return results;
		}
		 */

		#endregion

		#region HelperFunctions

		/// <summary>
		/// Helper for classify. gets distance between a test image and the average image.
		/// using a particular metric.
		/// </summary>
		/// <param name="t"></param>
		/// <param name="avgImage"></param>
		/// <param name="metric"></param>
		/// <returns></returns>
		internal static double getDistance(ImageTransform t, AvgImage avgImage, int metric)
		{
			SymbolRec.Image.Image latent = t.LatentImage;
			if (metric == Metrics.ImageDistance.HAUSDORFF)
			{
				return avgImage.HausdorffDistance(latent);
			}
			else
			{
				SymbolRec.Image.Image avgIm = avgImage.Image;
				ImageDistance id = new ImageDistance(latent, avgIm);
				return id.distance(metric);
			}
		}

		/// <summary>
		/// Estimate the probability of a tranform using the custom Warp distance metric along with 
		/// Kernel Density Estimation
		/// </summary>
		/// <param name="w"></param>
		/// <returns></returns>
		private double probabilityOfTransform(ShapeTransform w)
		{
			double avg = 0;
			foreach (Warp transform in warps)
			{
				avg += (transform.Matrix - MathNet.Numerics.LinearAlgebra.Matrix.Identity(transform.Matrix.RowCount, transform.Matrix.ColumnCount)).NormF();
			}
			avg /= warps.Count;
			double normdist = (w.Warp.Matrix - MathNet.Numerics.LinearAlgebra.Matrix.Identity(w.Warp.Matrix.RowCount, w.Warp.Matrix.ColumnCount)).NormF();
			double entropy = models[models.Count - 1].getTrainingEntropy(w);
			return (normdist/avg) * entropy;

			/*
			double p = 0.0;
			int numTransforms = warps.Count;

			foreach (Warp transform in warps)
			{
				p += Warp.distance(w, transform);
			}

			return (p / numTransforms);
			*/ 
		}


		/// <summary>
		/// Serializes the designation. Useful so that we don't have to retrain every time.
		/// </summary>
		/// <param name="d"></param>
		/// <param name="path"></param>
		public static void saveTraining(Designation d, string path)
		{
			FileStream fs = new FileStream(path, FileMode.Create);
			BinaryFormatter bf = new BinaryFormatter();
			bf.Serialize(fs, d);
			fs.Close();
		}

		/// <summary>
		/// Loads a saved designation. Must be a file created with the saveTraining() method.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static Designation LoadDesignation(string path, int number_of_designations)
		{
			FileStream fs = new FileStream(path, FileMode.Open);
			BinaryFormatter bf = new BinaryFormatter();
			Designation d = (Designation)bf.Deserialize(fs);
			fs.Close();
			d.gen = new Random(DateTime.Now.GetHashCode());
			d.num_desigs = number_of_designations;
			return d;
		}

		/// <summary>
		/// Used for debugging.
		/// </summary>

		public void testLoad()
		{
			Console.WriteLine("\n--------");
			Console.WriteLine(DebugCounter);
			Console.WriteLine(isTrained);
			Console.WriteLine(m_height);
			Console.WriteLine(m_images);
			Console.WriteLine(m_name);
			Console.WriteLine(m_width);
			Console.WriteLine(maxScalar);
			Console.WriteLine("writing before image");
			this.models[0].writeImage(@"output/" + m_name + "_model0.bmp");
			Console.WriteLine("writing after image");
			this.models[models.Count - 1].writeImage(@"output/" + m_name + "_modelLast.bmp");

			Console.WriteLine("---------");
		}

		private List<Sketch.Substroke> getKeystrokes()
		{
			List<Sketch.Substroke> k = new List<Sketch.Substroke>();
			foreach (Sketch.Substroke ss in canonicalShape.SubstrokesL)
			{
				System.Drawing.Color lineColor = System.Drawing.Color.FromArgb((int)ss.XmlAttrs.Color);
				if (lineColor.B > 0 || lineColor.R > 0 || lineColor.G > 0 )
					k.Add(ss);
			}
			return k;
		}

		private void drawAllImages(List<ImageTransform> images, string suffix)
		{
			for(int idx = 0; idx < images.Count; ++idx)
			{
				ImageTransform i = images[idx];
				i.writeImage(Util.getDir() + "\\output\\image" +idx + "n" + suffix + ".png");
			}
		}

		/// <summary>
		/// Write the canonical image as a PNG to the specified file
		/// </summary>
		/// <param name="filename">The filename to write to</param>
		public void writeCanonicalBitmap(string filename)
		{
			SymbolRec.Image.Image i = new SymbolRec.Image.Image(64, 64, new SymbolRec.Substrokes(canonicalShape.SubstrokesL));
			i.writeToBitmap(filename);
		}

		/// <summary>
		/// Write the final model to a file
		/// </summary>
		/// <param name="filename"></param>
		public void writeLastModel(string filename)
		{
			models[models.Count - 1].Image.writeToBitmap(filename);
		}

		/// <summary>
		/// Get a cloud of points that may be missing from this image
		/// </summary>
		/// <param name="im"></param>
		/// <returns></returns>
		public IEnumerable<Sketch.Point> MissingPoints(SymbolRec.Image.Image im)
		{
			double[][] pixels = models[models.Count - 1].getPixels();
			for (int row = 0; row < Height; ++row)
			{
				for (int col = 0; col < Width; ++col)
				{
					if (pixels[row][col] > SymbolRec.Image.Image.ON_THRESHOLD && im[col, row] == 0)
						yield return im.PixelToPoint(col, row);
				}
			}
		}

		#endregion
		}
}
