/**
 * File: CRF.cs
 * 
 * Authors: Aaron Wolin, Devin Smith, Jason Fennell, Max Pflueger (Sketchers 2006); Eric Doi
 *          Code expanded by Anton Bakalov (Sketchers 2007).
 *          Harvey Mudd College, Claremont, CA 91711.
 * 
 * A CRF class with functionality for three different inference methods:
 *    1) Loopy belief propagation - fast approximation, but does not always converge accurately
 *    2) Exact inference - slow; computes all joint probabilities and marginalizes as necessary
 *    3) Junction tree algorithm - also computes exact probabilities, but is faster than (2).
 * The method used is set with the INFER_MODE constant.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 * 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Set;
using System.IO;
using ConverterXML;
using Sketch;
using LoopyBP;
using mit.ai.mrf;
using MathNet.Numerics.Distributions;

namespace CRF
{
	public class CRF
	{
		#region INTERNAL DATA MEMBERS
        enum InferenceType { loopybp_infer, exact_infer, jtree_infer };

        //private const InferenceType INFER_MODE = InferenceType.loopybp_infer;
        //private const InferenceType INFER_MODE = InferenceType.exact_infer;
        private const InferenceType INFER_MODE = InferenceType.jtree_infer;

		// TODO: Set these to something sensible...
        public const double DISTANCE_THRESHOLD = 25;//100//200; //100;//400.0; //200 is about a cm
        public const int TIME_THRESHOLD = 100;//800 //1000; //800;//2000; // I think this is in ms

		// TODO: Also set these to something sensible (values for conjugate gradient convergence)
		public const double ERROR_TOLERANCE = .000001;
		public const double PARAMETER_TOLERANCE = .01;

		// Value for loopyBP convergence.  Set it to something sensible
		public const double ERROR_TOL = .001;

		// The number of labels/classes that a given node can take on.
		public int numLabels;

		// The true node labels.  Only initialized if this is a training data set.
		public int[] trueLabels;

		// The number of feature functions per each label and per each label pair respectively
        public int numSiteFeatures = SiteFeatures.numberSiteFeatures();//NUM_SITE_FEATURES;
        public int numInteractionFeatures = InteractionFeatures.numberInterFeatures();//NUM_INTER_FEATURES;

		// Delegate that will be used to store feature functions for site potentials
		public delegate double siteDelegate(Node callingNode, Substroke[] input);

		// Delegate that will be used to store feature functions for interaction potentials
		public delegate double interactionDelegate(Node node1, Node node2, Substroke[] input);

		// Keeps track of parameters for feature functions
		public double[][] siteParam;
		public double[][][] interactionParam;
		
		// Keep track of graph details
		public Node[] nodes;
		public int numEdges;
		public Substroke[] fragments;

        // If junction tree inference is used, keep the tree around
        private JTree jtree;
        private Hashtable exactConfigurationTable;

		// When probability is calculated, keep track of the normalizing factor
		public double logZ;

		/// <summary>
		/// The featurefied sketch
		/// </summary>
		public Featurefy.FeatureSketch sketch;

		/// <summary>
		/// The RNG
		/// </summary>
		private static NormalDistribution gaussian = new NormalDistribution(0.5, 0.25);

		// This is to deal with a problem with our mathlab interface.  It has an initialization call that can only be called once
		// per run of the program
		private bool loopyBPInitialized;

		#endregion

		#region CONSTRUCTORS
		/// <summary>
		/// This constructor makes a new CRF from scratch.  It initalizes all of the arrays that hold feature functions and parameters,
		/// and sets the inital parameters to be random values from the normal Gaussian distribution.  
		/// </summary>
		/// <param name="numberLabels">How many different labels there are to choose from to classify a given node</param>
		/// <param name="numberSiteFeatures">Array that holds the number of feature functions that will be use for a given label class</param>
		/// <param name="numberInteractionFeatures">2D array that holds the number of feature functions to be used for each pair of label classes</param>
		public CRF(int numberLabels, bool loopyBPinit)
		{
			// Initialize some of the CRF's internal data members
			numLabels = numberLabels;

			// Deal with multiple CRF's
			this.loopyBPInitialized = loopyBPinit;

			// Initialize our CRF with random gaussian parameters
			siteParam = new double[numLabels][];
			interactionParam = new double[numLabels][][];

			// Site Potential initialization
			for (int i = 0; i < numLabels; i++)
			{
				siteParam[i] = new double[numSiteFeatures];

				for (int j = 0; j < numSiteFeatures; j++)
				{
					//if this is label 0, params will be set to 0
					// This is necessary to eliminate redundant parameters
					if (i == 0)
					{
						siteParam[i][j] = 0.0;
						//siteParam[i][j] = CreateGraph.randGaussian();
					}
					else
					{
						// Intialize the parameters with random values in [0,1]
						double d = NextGaussianDouble();
						siteParam[i][j] = d;
					}
				}
			
				// Interaction Potential initalization
				interactionParam[i] = new double[numLabels][];

				for (int j = 0; j < numLabels; j++)
				{
					interactionParam[i][j] = new double[numInteractionFeatures];

					for (int k = 0; k < numInteractionFeatures; k++)
					{
						// Parameters will be set to 0 for interaction 0,0 to remove redundant parameters
						if (i == 0 && j == 0)
						{
							//siteParam[i][j] = 0.0;				// <-- WHY SITE PARAM HERE?!?!?!?  Aaron is confused...
							interactionParam[i][j][k] = 0.0;		// Aaron added this...

							//interactionParam[i][j][k] = CreateGraph.randGaussian();
						}
						else
						{
							// Initalize the parameters randomly
							double d = NextGaussianDouble();
							interactionParam[i][j][k] = d;
						}
					}
				}
			}
		}

		/// <summary>
		/// Constructor to load CRF from file
		/// </summary>
		/// <param name="filename">File from which to load CRF</param>
		public CRF(string filename, bool loopyBPinit)
		{
			// Deal with multiple CRF's
			this.loopyBPInitialized = loopyBPinit;

			// Hack to get numLabels, so that everything else works out
			StreamReader sr = new StreamReader(filename);
			numLabels = Convert.ToInt32(sr.ReadLine());
			sr.Close();

			siteParam = new double[numLabels][];
			interactionParam = new double[numLabels][][];

			for (int i = 0; i < numLabels; i++)
			{
				siteParam[i] = new double[numSiteFeatures];
				interactionParam[i] = new double[numLabels][];

				for (int j = 0; j < numLabels; j++)
				{
					interactionParam[i][j] = new double[numInteractionFeatures];
				}
			}

			loadCRF(filename);
		}

		#endregion

		#region PARAMETER MANIPULATION
		/// <summary>
		/// This function saves all of the CRF info to an output file so that
		/// the CRF doesn't have to be trained every time it is run.
		/// 
		/// The format of the file is as follows:
		/// numLabels
		/// all of the parameters of the CRF, line by line, in the format used by loadParameters()
		/// </summary>
		/// <param name="filename">File where the CRF will be saved</param>
		public void saveCRF(string filename)
		{
			// I think filename needs to be the full path to the file
			StreamWriter sw = new StreamWriter(filename);

			// Labels
			sw.WriteLine(numLabels);

			double[] parameters = getParameters();

			foreach(double i in parameters)
			{
				sw.WriteLine(i);
			}

			sw.Close();
		}


		/// <summary>
		/// This function loads all of the CRF info from a file so that
		/// the CRF doesn't have to be trained every time it is run.
		/// 
		/// The format of the file is as follows:
		/// numLabels
		/// all of the parameters of the CRF, line by line, in the format used by loadParameters()
		/// </summary>
		/// <param name="filename">Filename to load CRF from</param>
		public void loadCRF(string filename)
		{
			StreamReader sr = new StreamReader(filename);

			// Get labels
			this.numLabels = Convert.ToInt32(sr.ReadLine());

			// Set up array to hold parameters
			int totSiteFeatures = numLabels * numSiteFeatures;
			int totInteractionFeatures = numLabels * numLabels * numInteractionFeatures;
			double[] parameters = new double[totSiteFeatures + totInteractionFeatures];

			// Load parameters from file
			for(int i = 0; i < parameters.Length; i++)
			{
				parameters[i] = Convert.ToDouble(sr.ReadLine());
			}

			// Put the parameters in the CRF
			setParameters(parameters);

			sr.Close();
		}


		/// <summary>
		/// This function takes in an array of new parameters to be loaded into the CRF.  The old parameters are
		/// returned by the function in the same format, as defined in getParameters comment
		/// </summary>
		/// <param name="newParams">The new parameters to be loaded</param>
		/// <returns>The CRF's old parameters</returns>
		public double[] loadParameters(double[] newParams)
		{
			double[] oldParams = getParameters();
			setParameters(newParams);
			return oldParams;
		}


		/// <summary>
		/// This function takes in an array of new parameters to be loaded into the CRF.  The old parameters are
		/// returned by the function in the same format.
		/// 
		/// The format is defined as followed:
		/// The first numLabels * numSiteFeatures places in the array hold the site feature parameters, ordered by label.
		/// Thus, the first numSiteFeatures entries are parameters for label 0, then the next numSiteFeatures entries are parameters
		/// for label 1, etc.
		/// 
		/// The remaining numLabels * numLabels * numInteractionFeatures places hold the interaction feature parameters.
		/// The first numLabels * numInteractionFeatures of these parameters corresponds to one of the labels being 0.  From this block
		/// the first numInteractionFeatures correspond to the other label being 0.  Within this block, there are all of the interaction features
		/// for the 0-0 pair.  This continues for the rest of the array (the format is similar to the way site feature parameters are stored)
		/// </summary>
		/// <param name="newParams">The new parameters to be loaded</param>
		public void setParameters(double[] newParams)
		{
			// Number that the interaction features are all shifted out by in parameter arrays
			int totSiteFeatures = numLabels * numSiteFeatures;

			for (int i = 0; i < numLabels; i++)
			{
				int prod1 = i * numSiteFeatures;
				
				// Site features
				for (int f = 0;  f < numSiteFeatures; f++)
				{
					siteParam[i][f] = newParams[prod1 + f];
				}

				int prod2 = i * numLabels * numInteractionFeatures;
				
				// Interaction features
				for (int j = 0; j < numLabels; j++)
				{
					int prod3 = j * numInteractionFeatures;

					for (int f = 0; f < numInteractionFeatures; f++)
					{
						interactionParam[i][j][f] = newParams[totSiteFeatures + prod2 + prod3 + f];
					}
				}
			}
		}


		/// <summary>
		/// Get the parameters from this CRF and return them in vector format.  Format is explained in getParamters comment.
		/// </summary>
		/// <returns>vector formatted parameters</returns>
		public double[] getParameters()
		{
			// Number that the interaction features are all shifted out by in parameter arrays
			int totSiteFeatures = numLabels * numSiteFeatures;
			int totInteractionFeatures = numLabels * numLabels * numInteractionFeatures;

			double[] parameters = new double[totSiteFeatures + totInteractionFeatures];

			for (int i = 0; i < numLabels; i++)
			{
				int prod1 = i * numSiteFeatures;

				// Site features
				for (int f = 0;  f < numSiteFeatures; f++)
				{
					parameters[prod1 + f] = siteParam[i][f];
				}
				
				int prod2 = i * numLabels * numInteractionFeatures;

				// Interaction features
				for (int j = 0; j < numLabels; j++)
				{
					int prod3 = j * numInteractionFeatures;

					for (int f = 0; f < numInteractionFeatures; f++)
					{
						parameters[totSiteFeatures + prod2 + prod3 + f] = interactionParam[i][j][f];
					}
				}
			}

			return parameters;
		}

		#endregion

		#region GRAPH CREATION/DESTRUCTION
		/// <summary>
		/// Create the graph that stores all of the dependency relationships between strokes and calculate all of the
		/// potential function values for the graph (both site and interatction potentials are calculated)
		/// </summary>
		public void initGraph(ref Featurefy.FeatureSketch fs)
		{
			initGraph(fs.Sketch.Substrokes, ref fs);
		}
		/// <summary>
		/// Create the graph that stores all of the dependency relationships between strokes and calculate all of the
		/// potential function values for the graph (both site and interatction potentials are calculated)
		/// </summary>
		public void initGraph(Substroke[] frag, ref Featurefy.FeatureSketch fs)
		{
			sketch = fs;
			initGraph(frag);
		}

		/// <summary>
		/// Create the graph that stores all of the dependency relationships between strokes and calculate all of the
		/// potential function values for the graph (both site and interatction potentials are calculated)
		/// </summary>
		/// <param name="frag">This contains all of the raw Stroke data that the CRF will have access to</param>
		public void initGraph(Substroke[] frag)
		{
            //double debuggy = CreateGraph.normalizeDistance2(DISTANCE_THRESHOLD, sketch);// DEBUG

			fragments = frag;	// Store all of the fragments internally in the CRF
            nodes = new Node[frag.Length];
			numEdges = 0;

			// Create one node for every fragment
			for(int i = 0; i < frag.Length; ++i)
			{
                Substroke temp = frag[i];
                nodes[i] = new Node(sketch.GetFeatureStrokeForSubstroke(frag[i]), numLabels, i);
			}

			// Create the connections in the graph
			for(int i = 0; i < frag.Length; i++)
			{
				for(int j = 0; j < frag.Length; j++)
				{
					// We don't want nodes connected to themselves, but we want them connected if they meet either threshold requirement
					if((i != j) && 
						((sketch.MinDistBetweenSubstrokes(nodes[i].fragment, nodes[j].fragment) <= CreateGraph.normalizeDistance(DISTANCE_THRESHOLD, sketch)) ||
						(CreateGraph.minTimeBetweenFrag(nodes[i], nodes[j]) <= TIME_THRESHOLD)))
					{
						// Place an edge between nodes i and j, the other way will be added later (or before) in the loop
						// when i takes the current value of j and j takes the current value of i
						nodes[i].addNeighbor(nodes[j]);
							
						// Only increment node count once per edge
						if ( i < j )
						{
							numEdges++;
						}
					}
				}
			}

			// This is a bit of code I used to do some data analysis.
			//  It would be nice to have command line options for data analysis.
			//Console.WriteLine("XXXXXXXXXXXXXXXXXXXXXXX");
			//for(int i=0; i< nodes.Length; ++i)
			//{
			//	Console.WriteLine("{0}", InteractionFeatures.turningWeighted(nodes[i]));
			//}
			//Console.WriteLine("YYYYYYYYYYYYYYYYYYYYYYY");
			
			loadFeatures();  // feature functions need the stroke data
		}

		/// <summary>
		/// This method clears away the a specific CRF graph & data, allowing us to load a new dataset
		/// </summary>
		public void clearGraph() 
		{
			// I hope C# garbage collects and doesn't get memory leaks...
			nodes = null;
			fragments = null;
			trueLabels = null;
		}

		#endregion

		# region FEATURE CREATION/EVALUATION

		/// <summary>
		/// Load the values of all of the feature functions into the CRF.  This function needs fragment data
		/// to run and thus needs initCRF to be run first.  Probably not something to worry about though,
		/// as initCRF calls loadFeatures() in the proper place.
		/// </summary>
		public void loadFeatures()
		{
			// Calculate a bunch of macro characteristics of the graph here and give them to 
			// SiteFeatures and InteractionFeatures as constructor arguments so that they
			// won't have to be calculated more than once
//			double totalMinDistBetweenFrag = CreateGraph.totMinDistBetweenFrag(fragments);
//			double totalAverageDistBetweenFrag = CreateGraph.totAverageDistBetweenFrag(fragments);
//			double totalMaxDistBetweenFrag = CreateGraph.totMaxDistBetweenFrag(fragments);
//			double totalTimeBetweenFrag = CreateGraph.totTimeBetweenFrag(fragments);
//			double totalArcLength = CreateGraph.totArcLength(fragments);
//			double totalLengthOfFrag = CreateGraph.totLengthOfFrag(fragments);
//			double averageSpeedOfFrag = CreateGraph.avgSpeedOfFrag(fragments);
//			double totalMinDistBetweenEnds = CreateGraph.totMinDistBetweenEnds(fragments);
			double totalMinDistBetweenFrag = sketch.TotalMinDistBetweenSubstrokes;
			double totalAverageDistBetweenFrag = sketch.TotalAvgDistBetweenSubstrokes;
			double totalMaxDistBetweenFrag = sketch.TotalMaxDistBetweenSubstrokes;
			double totalTimeBetweenFrag = CreateGraph.totTimeBetweenFrag(nodes);
			double totalArcLength = sketch.TotalArcLength;
			double totalLengthOfFrag = sketch.TotalDistance;
			double averageSpeedOfFrag = sketch.AverageAverageSpeed;
			double totalMinDistBetweenEnds = sketch.TotalMinDistanceBetweenEndpoints;
			double[] bbox = sketch.BBox.ToArray();


			SiteFeatures site = new SiteFeatures(totalMinDistBetweenFrag,
												totalAverageDistBetweenFrag,
												totalMaxDistBetweenFrag,
												totalTimeBetweenFrag,
												totalArcLength,
												totalLengthOfFrag,
												averageSpeedOfFrag,
												totalMinDistBetweenEnds,
												bbox, ref sketch);
			InteractionFeatures inter = new InteractionFeatures(totalMinDistBetweenFrag,
												totalAverageDistBetweenFrag,
												totalMaxDistBetweenFrag,
												totalTimeBetweenFrag,
												totalArcLength,
												totalLengthOfFrag,
												averageSpeedOfFrag,
												totalMinDistBetweenEnds, ref sketch);

			// Load all of the numerical feature vectors into nodes for future ease of use
			foreach (Node n1 in nodes)
			{
				for (int i = 0; i < numLabels; i++)
				{
					n1.siteFeatureFunctionVals[i] = site.evalSiteFeatures(n1, fragments);

					// ERROR CHECKING
//					for(int k = 0; k < numSiteFeatures; k++)
//					{
//						if(n1.siteFeatureFunctionVals[i][k].Equals(double.NaN))
//						{
//							Console.WriteLine("Node indexed by {0} has site feature function at {1} with value: {2}",
//								n1.index, k, n1.siteFeatureFunctionVals[i][k]);
//						}
//
//						if(n1.siteFeatureFunctionVals[i][k].Equals(double.PositiveInfinity))
//						{
//							Console.WriteLine("Node indexed by {0} has site feature function at {1} with value: {2}",
//								n1.index, k, n1.siteFeatureFunctionVals[i][k]);
//						}
//
//						if(n1.siteFeatureFunctionVals[i][k].Equals(double.NegativeInfinity))
//						{
//							Console.WriteLine("Node indexed by {0} has site feature function at {1} with value: {2}",
//								n1.index, k, n1.siteFeatureFunctionVals[i][k]);
//						}
//					}
				}

				foreach (Node n2 in n1.neighbors)
				{
					int n1IndexOfn2 = n1.neighbors.IndexOf(n2);
					double[][][] intFFVs = (double[][][])n1.interactionFeatureFunctionVals[n1IndexOfn2];

					for (int i = 0; i < numLabels; i++)
					{
						for (int j = 0; j < numLabels; j++)
						{
							intFFVs[i][j] = inter.evalInteractionFeatures(n1,n2,fragments);
							
							//((double[][][])n2.interactionFeatureFunctionVals[n2IndexOfn1])[i][j] = inter.evalInteractionFeatures(n2,n1,fragments);

							// ERROR CHECKING
//							for(int k = 0; k < numInteractionFeatures; k++)
//							{
//								if(((double[][][])n1.interactionFeatureFunctionVals[n1IndexOfn2])[i][j][k].Equals(double.NaN))
//								{
//									Console.WriteLine("Node indexed by {0} interactiong with node indexed by {1} has its {2}th interaction feature function value: {3}",
//										n1.index, n2.index, k, ((double[][][])n1.interactionFeatureFunctionVals[n1IndexOfn2])[i][j][k]);
//								}
//
//								if(((double[][][])n1.interactionFeatureFunctionVals[n1IndexOfn2])[i][j][k].Equals(double.PositiveInfinity))
//								{
//									Console.WriteLine("Node indexed by {0} interactiong with node indexed by {1} has its {2}th interaction feature function value: {3}",
//										n1.index, n2.index, k, ((double[][][])n1.interactionFeatureFunctionVals[n1IndexOfn2])[i][j][k]);
//								}
//
//								if(((double[][][])n1.interactionFeatureFunctionVals[n1IndexOfn2])[i][j][k].Equals(double.NegativeInfinity))
//								{
//									Console.WriteLine("Node indexed by {0} interactiong with node indexed by {1} has its {2}th interaction feature function value: {3}",
//										n1.index, n2.index, k, ((double[][][])n1.interactionFeatureFunctionVals[n1IndexOfn2])[i][j][k]);
//								}
//							}
						}
					}

					n1.interactionFeatureFunctionVals[n1IndexOfn2] = intFFVs;
				}
			}
        }

		/// <summary>
		/// Takes all of the feature functions and parameters and calculates the local array of features for each node
		/// </summary>
		public void calculateFeatures()
		{
			foreach (Node n1 in nodes)
			{
				for (int a = 0; a < numLabels; a++)
				{
					double dotProd = ConjugateGradient.dot(n1.siteFeatureFunctionVals[a], siteParam[a]);
					n1.siteFeatureVals[a] = Math.Exp(dotProd);

					if (double.IsNaN(n1.siteFeatureVals[a]) || double.IsInfinity(n1.siteFeatureVals[a]))
						break;

					// ERROR CHECKING
//					if(n1.siteFeatureVals[a].Equals(double.NaN))
//					{
//						Console.WriteLine("Node index {0}, site feature function {1}, is {2}",n1.index, a, n1.siteFeatureVals[a]);
//					}
//
//					if(n1.siteFeatureVals[a].Equals(double.PositiveInfinity))
//					{
//						Console.WriteLine("Node index {0}, site feature function {1}, is {2}",n1.index, a, n1.siteFeatureVals[a]);
//					}
//
//					if(n1.siteFeatureVals[a].Equals(double.NegativeInfinity))
//					{
//						Console.WriteLine("Node index {0}, site feature function {1}, is {2}",n1.index, a, n1.siteFeatureVals[a]);
//					}
				}

				foreach (Node n2 in n1.neighbors)
				{
					int n1IndexOfn2 = n1.neighbors.IndexOf(n2);
					//int n2IndexOfn1 = n2.neighbors.IndexOf(n1);

					double[][]   intFVs  = (double[][])n1.interactionFeatureVals[n1IndexOfn2];
					double[][][] intFFVs = (double[][][])n1.interactionFeatureFunctionVals[n1IndexOfn2];
						
					for (int a = 0; a < numLabels; a++)
					{
						for (int b = 0; b < numLabels; b++)
						{
							double dotProd = ConjugateGradient.dot(intFFVs[a][b], interactionParam[a][b]);

							// This is n1's copy of n1's effect on n2
							intFVs[a][b] = Math.Exp(dotProd);

							if (double.IsNaN(intFVs[a][b]) || double.IsInfinity(intFVs[a][b]))
								break;

							// ERROR CHECKING
//							if(((double[][])n1.interactionFeatureVals[n1IndexOfn2])[a][b].Equals(double.NaN))
//							{
//								Console.WriteLine("Node index {0} to {1}, interaction potential {2} {3}, is {4}", n1.index, n2.index, a, b, ((double[][])n1.interactionFeatureVals[n1IndexOfn2])[a][b]);
//							}
//
//							if(((double[][])n1.interactionFeatureVals[n1IndexOfn2])[a][b].Equals(double.PositiveInfinity))
//							{
//								Console.WriteLine("Node index {0} to {1}, interaction potential {2} {3}, is {4}", n1.index, n2.index, a, b, ((double[][])n1.interactionFeatureVals[n1IndexOfn2])[a][b]);
//							}
//
//							if(((double[][])n1.interactionFeatureVals[n1IndexOfn2])[a][b].Equals(double.NegativeInfinity))
//							{
//								Console.WriteLine("Node index {0} to {1}, interaction potential {2} {3}, is {4}", n1.index, n2.index, a, b, ((double[][])n1.interactionFeatureVals[n1IndexOfn2])[a][b]);
//							}
						}
					}

					n1.interactionFeatureVals[n1IndexOfn2] = intFVs;
					n1.interactionFeatureFunctionVals[n1IndexOfn2] = intFFVs;
				}
			}
			return;
		}


		#endregion

		/// <summary>
		/// Find the adjacency matrix of the graph contained in the CRF. If two nodes are adjacent,
		/// the matrix will store an edge number > 0 in the corresponding spot.
		/// </summary>
		/// <returns>An adjacency matrix of the CRF</returns>
		private int[][] getAdjacencyMatrix()
		{
			int[][] adjMat = new int[nodes.Length][];
			int edgeNum = 1;

			for (int i = 0; i < nodes.Length; i++)
			{
				adjMat[i] = new int[nodes.Length];

				for (int j = 0; j < nodes.Length; j++)
				{
					// Check whether two nodes are neighbors
					if (nodes[i].neighbors.Contains(nodes[j])) 
					{
						// Store the edge number of a new edge in the i,j index of the adjacency matrix
						if (i <= j)
						{
							adjMat[i][j] = edgeNum;
							++edgeNum;
						}
						// Otherwise retrieve the edge number from the previously stored edge
						else
						{
							adjMat[i][j] = adjMat[j][i];
						}
					}
					else
					{
						adjMat[i][j] = 0;
					}
				}
			}

			return adjMat;
        }

        #region graph_printing_(debug)

        /// <summary>
        /// Create a file containing graph data that can be read by a tool called Graph Magics.
        /// The format is a neighbors list.
        /// 
        /// Takes a string path and id for labeling the graph.
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="graphId"></param>
        public void printGraphForGraphMagics(string path, string graphId)
        {
            StreamWriter f = new StreamWriter(path + "/graph_graphMagics(" + graphId + ").txt");
            f.WriteLine("{0}", nodes.Length.ToString()); // line 1: number of nodes

            for (int i = 0; i < nodes.Length; i++)
            {
                f.Write("{0}", nodes[i].neighbors.Count.ToString());
                for (int j = 0; j < nodes.Length; j++)
                {
                    // Check whether the pair are neighbors
                    if (nodes[i].neighbors.Contains(nodes[j]))
                    {
                        // do not consider nodes to have edges to themselves
                        if (i != j)
                        {
                            f.Write(" {0}", (j+1).ToString()); // vertices start numbering from 1
                        }
                        else
                        {
                            System.Console.Out.WriteLine("Nodes are their own neighbors; count may be off.");
                        }
                    }
                }
                f.WriteLine();
            }
            f.Close();
        }


        /// <summary>
        /// Create a file containing graph data that can be read by a tool called gteditor
        /// for easy display and interaction.  The format is xml.
        /// 
        /// Takes a string path and id for labeling the graph.
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="graphId"></param>
        public void printGraphForGteditor(string path, string graphId)
        {
            StreamWriter f = new StreamWriter(path + "/graph_gteditor(" + graphId + ").txt");
            f.WriteLine("<graph>");
            f.WriteLine("  <properties>");
            f.WriteLine("    <zoomLevel>1.0</zoomLevel>");
            f.WriteLine("    <defaultShape>CIRCLE</defaultShape>");
            f.WriteLine("    <defaultInsertShape>DEFAULT</defaultInsertShape>");
            f.WriteLine("    <backgroundColor>");
            f.WriteLine("      <red>255</red>");
            f.WriteLine("      <green>255</green>");
            f.WriteLine("      <blue>255</blue>");
            f.WriteLine("      <alpha>255</alpha>");
            f.WriteLine("    </backgroundColor>");
            f.WriteLine("    <defaultUseGraphBackground>true</defaultUseGraphBackground>");
            f.WriteLine("    <defaultVertexBackgroundColor>");
            f.WriteLine("      <red>255</red>");
            f.WriteLine("      <green>255</green>");
            f.WriteLine("      <blue>255</blue>");
            f.WriteLine("      <alpha>255</alpha>");
            f.WriteLine("    </defaultVertexBackgroundColor>");
            f.WriteLine("    <defaultVertexForegroundColor>");
            f.WriteLine("      <red>0</red>");
            f.WriteLine("      <green>0</green>");
            f.WriteLine("      <blue>0</blue>");
            f.WriteLine("      <alpha>255</alpha>");
            f.WriteLine("    </defaultVertexForegroundColor>");
            f.WriteLine("  </properties>");

            // vertex list output
            f.WriteLine("  <vertexList>");
            for (int i = 0; i < nodes.Length; i++)
            {
                f.WriteLine("    <vertex>");
                f.WriteLine("      <xPosition>10.0</xPosition>");
                f.WriteLine("      <yPosition>10.0</yPosition>");
                f.WriteLine("      <useGraphBackround>true</useGraphBackround>");
                f.WriteLine("      <backgroundColor reference=\"../../../properties/defaultVertexBackgroundColor\"/>");
                f.WriteLine("      <foregroundColor reference=\"../../../properties/defaultVertexForegroundColor\"/>");
                f.WriteLine("      <shape reference=\"../../../properties/defaultInsertShape\"/>");
                f.WriteLine("      <displayLabel>false</displayLabel>");
                f.WriteLine("      <label></label>");
                f.WriteLine("      <vertexIndex>{0}</vertexIndex>", i.ToString());
                f.WriteLine("    </vertex>");
            }
            f.WriteLine("  </vertexList>");

            // edge list output.  print only one edge between two connected nodes.
            f.WriteLine("  <edgeList>");

			for (int i = 0; i < nodes.Length; i++)
			{
				for (int j = 0; j < nodes.Length; j++)
				{
					// Check whether two nodes are neighbors
					if (nodes[i].neighbors.Contains(nodes[j])) 
					{
						// only consider one edge of a pair, and do not consider nodes to have edges to themselves
						if (i < j)
						{
                            f.WriteLine("    <edge>");
                            f.WriteLine("      <sourceVertexId>{0}</sourceVertexId>", j.ToString());
                            f.WriteLine("      <targetVertexId>{0}</targetVertexId>", i.ToString());
                            f.WriteLine("    </edge>");
						}
					}
				}
			}
            f.WriteLine("  </edgeList>");
            f.WriteLine("</graph>");
            f.Close();
        }
        #endregion

        /// <summary>
		/// This function will go through the nodes and find the most likely set of labels.
		/// Calling this function before infer() has been called would be meaningless and/or dangerous.
		/// </summary>
		/// <param name="labels">Pass in uninitalized int array that will contain the outputted labels</param>
		/// <param name="probs">Pass in uninialized doublew array that will contian the outputted probabilities of each label</param>
		/// <returns>array containing the index of the most likely label for each node</returns>
		public void findLabels(out int[] labels, out double[] probs)
		{
			// This will store the labels as we find them
			labels = new int[nodes.Length];

			// This will store the probabilities of each label as we find them
			probs = new double[nodes.Length];

			int highScoreLabel = 0;		// Used inside the loop to store what the current highest belief is in
			double highScore = 0.0;		// Used inside the loop to store the current highest belief

			// Loop through all nodes
			for (int i = 0; i < nodes.Length; i++)
			{
				// Reset our values for the new node
				highScoreLabel = 0;
				highScore = nodes[i].siteBelief[0];

				// Loop through all labels, note we start at 1 because we took care of index 0 above
				for (int j=1; j<numLabels; j++)
				{
					// If we have a new highScore record it
					if (nodes[i].siteBelief[j] > highScore)
					{
						highScore = nodes[i].siteBelief[j];
						highScoreLabel = j;
					}
				}

				// Record the label & prob for node i
				labels[i] = highScoreLabel;
				probs[i] = highScore;
				
				//Console.WriteLine("Label: node {0} is {1} with belief {2}", i, highScoreLabel, highScore);
			}
		}

		/// <summary>
		/// This is a function set up for debugging so that we may easily switch between the version of infer that
		/// uses Matlab and the version of infer than uses the MIT ported code.
		///
		/// </summary>
		public void infer()
        {
            #region debug
            /* Debugging stuff */
			/*string FILE_NAME = "EdgeBels-Matlab.txt";
			StreamWriter Tex = new StreamWriter(FILE_NAME, true);
			
			Tex.WriteLine( "\nCalling infer on CRF with {0} nodes", nodes.Length );
			Tex.Close(); */
			/* End of debugging stuff */

			/* Debugging stuff */
			/*String FILE_NAME2 = "EdgeBels-MIT.txt";
			StreamWriter Tex2 = new StreamWriter(FILE_NAME2, true);
			Tex2.WriteLine( "\nCalling infer on CRF with {0} nodes", nodes.Length );
			Tex2.Close(); */
			/* End of debugging stuff */

			//inferMatlab();
            //testJTree();
            //printBeliefsAndLogZ();
            #endregion

            switch (INFER_MODE)
            {
                case InferenceType.loopybp_infer:
                    inferCSharp();
                    break;
                case InferenceType.exact_infer:
                    inferExact();
                    break;
                case InferenceType.jtree_infer:
                    inferJTree(true); // run jtree with edge belief calculation (slower)
                    //inferJTree(false); // run jtree without edge belief calculation (faster)
                    break;
                default:
                    inferJTree(true);
                    break;
            }
        }

        /// <summary>
        /// Calculates the LOG of the joint probability of the graph using default inference mode
        /// </summary>
        /// <param name="labels">An array of label assignments for each node</param>
        /// <returns></returns>
        public double getLogJoint(int[] labels)
        {
            if (labels.Length != nodes.Length)
            {
                System.Console.Error.WriteLine("Error: Cannot get joint probability; incomplete label assignment");
                return -1.0;
            }

            double ret = 0.0;

            switch (INFER_MODE)
            {
                case InferenceType.loopybp_infer:
                    ret = getLogJointFromLBP(labels);
                    break;
                case InferenceType.exact_infer:
                    ret = getLogJointFromExact(labels);
                    break;
                case InferenceType.jtree_infer:
                    ret = getLogJointFromJtree(labels);
                    break;
                default:
                    ret = -1.0;
                    break;
            }

            return ret;
        }

        /// <summary>
        /// Calculates the LOG of the joint probability of the graph using MIT exact inference
        /// </summary>
        /// <param name="labels">An array of label assignments for each node</param>
        /// <returns></returns>
        public double getLogJointFromExact(int[] labels)
        {
            if (exactConfigurationTable == null)
            {
                System.Console.Error.WriteLine("Error: Cannot get exact joint probability before running exact inference.");
                return -1.0;
            }

            // The configuration table in the MIT code is keyed by strings.  Yes, this is hacky, and will have to be changed if the domain
            // has double digit label IDs... but for now, this is sufficient.  See the function arrayToString in the MIT code.
            string index = "";
            foreach (int x in labels)
                index += x.ToString();

            double ret = 0.0;
            ret += Math.Log( (double)exactConfigurationTable[index]);
            ret -= this.logZ; // subtract the normalization factor

            return ret;
        }

        /// <summary>
        /// Calculates the LOG of the joint probability of the graph using MIT loopy BP results
        /// </summary>
        /// <param name="labels">An array of label assignments for each node</param>
        /// <returns></returns>
        public double getLogJointFromLBP(int[] labels)
        {
            if (this.logZ == 0.0)
            {
                System.Console.Error.WriteLine("Error: Cannot get loopyBP joint probability before running inference.");
                return -1.0;
            }

            double ret = 0.0;

            for (int i = 0; i < nodes.Length; i++)
            {
                // Site features, we want the log of the features
                ret += Math.Log(nodes[i].siteFeatureVals[this.trueLabels[i]]);

                // Interaction features, we want the log of the features
                for (int j = i + 1; j < nodes.Length; j++)
                {
                    if (nodes[i].neighbors.Contains(nodes[j]))
                    {
                        int iIndexOfj = nodes[i].neighbors.IndexOf(nodes[j]);
                        double[][] iFVs = (double[][])nodes[i].interactionFeatureVals[iIndexOfj];
                        ret += Math.Log(iFVs[this.trueLabels[i]][this.trueLabels[j]]);
                    }
                }
            }

            ret -= this.logZ;

            return ret;
        }

        #region INFER junctionTree

        /// <summary>
        /// Uses the junction tree algorithm to perform exact inference on the CRF.  This involves
        /// triangulating the graph, finding cliques, creating a junction tree, and then passing
        /// messages.  The logZ value is not calculated, but the edge beliefs calculation
        /// is optional.
        /// </summary>
        public void inferJTree(bool getEdgeBels)
        {
            // System.Console.WriteLine("Calling JTree infer");
            this.jtree = getJTree();
            
            // Query and store the belief data for each node
            for (int i = 0; i < nodes.Length; i++)
            {
                // temporary debug!
                //System.Console.WriteLine("Jtree Node {0}:", i);
                ArrayList nodeBeliefs = jtree.query(i);

                int j = 0;
                foreach (double d in nodeBeliefs)
                {
                    nodes[i].siteBelief[j] = d;
                    // temporary debug!
                    //System.Console.WriteLine("     " + nodes[i].siteBelief[j]);
                    ++j;
                }
            }

            if (getEdgeBels)
            {
                int[][] adjMat = this.getAdjacencyMatrix();
                for (int i = 0; i < nodes.Length; ++i)
                {
                    for (int j = i + 1; j < nodes.Length; ++j)
                    {
                        if (adjMat[i][j] > 0)
                        {
                            int iIndexOfj = nodes[i].neighbors.IndexOf(nodes[j]);
                            int jIndexOfi = nodes[j].neighbors.IndexOf(nodes[i]);

                            // First, get the probability table
                            List<int> idList = new List<int>();
                            idList.Add(i);
                            idList.Add(j);
                            
                            //ArrayList edgeQuery = jtree.query(idList);
                            //Potential edgePotential = new Potential(edgeQuery, idList);
                            Potential edgePotential = jtree.query(idList);

                            // Pulled these out of the inner loops for simplicity
                            double[][] i_edgeBels = nodes[i].interactionBeliefVals[iIndexOfj];
                            double[][] j_edgeBels = nodes[j].interactionBeliefVals[jIndexOfi];
                            
                            // Now, for each label pair, store the marginalized belief.
                            for (int a = 0; a < numLabels; a++)
                            {
                                for (int b = 0; b < numLabels; b++)
                                {
                                    Dictionary<int, int> evidence = new Dictionary<int, int>();
                                    evidence[i] = a;
                                    evidence[j] = b;

                                    double prob = edgePotential.enterFullEvidence(evidence);
                                    i_edgeBels[a][b] = prob;
                                    j_edgeBels[b][a] = prob;

                                    #region debug
                                    /*
                                    //debug
                                    //System.Console.WriteLine("Reducing to get edge bels.  i = {0}, j = {1}", i, j);
                                    //System.Console.Write("original vars: ");
                                    //foreach (int blah in edgePotential.getVariables())
                                    //    System.Console.Write("{0}, ", blah);
                                    Potential reduced = edgePotential.enterEvidence(i, a);
                                    //System.Console.WriteLine();
                                    //System.Console.Write("reduced vars: ");
                                    //foreach (int blah in reduced.getVariables())
                                    //    System.Console.Write("{0}, ", blah);
                                    //System.Console.WriteLine();
                                    Potential reduced2 = reduced.enterEvidence(j, b);
                                    ArrayList reduced2Table = reduced2.Table;
                                    if ((reduced2Table[0] is double) && (reduced2Table.Count == 1))
                                    {
                                        i_edgeBels[a][b] = (double)(reduced2Table[0]);
                                        j_edgeBels[b][a] = (double)(reduced2Table[0]);
                                    }
                                    else
                                    {
                                        System.Console.Error.WriteLine("Problem getting edge beliefs from JTree: potential table has not been reduced to a single value.");
                                        System.Console.Error.WriteLine("== edgepot:");
                                        edgePotential.print();
                                        System.Console.Error.WriteLine("== reduced:");
                                        reduced.print();
                                        System.Console.Error.WriteLine("== reduced2:");
                                        reduced2.print();
                                    }
                                    */
                                    #endregion debug
                                }
                            }

                            // Restore the belief data
                            nodes[i].interactionBeliefVals[iIndexOfj] = i_edgeBels;
                            nodes[j].interactionBeliefVals[jIndexOfi] = j_edgeBels;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the LOG of the joint probability of the graph after inferJtree has been called.
        /// It is equal to the product of all clique potentials divided by all separator
        /// potentials.
        /// </summary>
        /// <param name="labels">An array of label assignments for each node</param>
        /// <returns></returns>
        public double getLogJointFromJtree(int[] labels)
        {
            //debug
            //System.Console.WriteLine("In getJointFromJtree.  Getting joint potentials. =====================");

            if (jtree == null)
            {
                System.Console.Error.WriteLine("Error: Cannot get joint probability before running junction tree inference.");
                return -1.0;
            }

            // It will help to have a full dictionary from variables to labels
            Dictionary<int, int> fullMap = new Dictionary<int, int>();
            for (int i = 0; i < labels.Length; ++i)
            {
                fullMap[i] = labels[i];
                //System.Console.WriteLine("fullmap of " + i + " is " + fullMap[i]);
            }

            double ret = 0.0;
            //System.Console.WriteLine("============");
            foreach (JTreeNode jtn in this.jtree.Nodes)
            {
                Potential p = jtn.Potential;
                Potential pNormed = p.normalize();
                double prob = Math.Log( pNormed.enterFullEvidence(fullMap) );
                //System.Console.WriteLine("   jointlog += " + prob); //debug
                //p.print();
                //System.Console.WriteLine("end print");
                ret += prob;
            }

            foreach (JTreeEdge edge in this.jtree.Edges)
            {
                Potential p = edge.UpdatedPotential; // edges track "current" and "updated" potentials.  I think we want updated.
                Potential pNormed = p.normalize();
                double prob = Math.Log( pNormed.enterFullEvidence(fullMap) );
                //System.Console.WriteLine("   jointlog -= " + prob); //debug
                ret -= prob;
            }
            return ret;
        }

        /// <summary>
        /// For now the ordering is dumb and just goes by id
        /// </summary>
        /// <returns></returns>
        public int[] getElimOrdering()
        {
            int [] order = new int[nodes.Length];
            for (int i = 0; i < nodes.Length; ++i)
            {
                order[i] = i;
            }
            return order;
        }

        /// <summary>
        /// Returns a node list which is a shallow copy of nodes except for the neighbors
        /// list, which is created new.  We add edges so that the graph is triangulated
        /// (no chordless cycles longer than three nodes)
        /// 
        /// The triangulation algorithm requires an ordering by which to check nodes
        /// </summary>
        /// <param name="eliminationOrdering"></param>
        /// <returns></returns>
        public Node[] getTriangulatedGraph(int[] eliminationOrdering)
        {
            Node[] ret = new Node[nodes.Length];

            // Copy the nodes list, except for the neighbors
            for (int i = 0; i < nodes.Length; ++i)
            {
                ret[i] = new Node(nodes[i]);
                
                /* //DEBUG
                System.Console.Out.WriteLine();
                System.Console.Out.Write("A: node_{0}'s neighbors {1}:", i.ToString(), nodes[i].neighbors.Count.ToString());
                for (int j = 0; j < nodes.Length; ++j)
                {
                    if (nodes[i].neighbors.Contains(nodes[j])) System.Console.Out.Write(" {0}", j.ToString());
                }
                */
            }

            // Now that the nodes have been created, we can recreate the neighbor lists
            // NOTE: We aren't using the addNeighbor function because that affects the other
            // members.  We want to make sure we're modifying the neighbor list directly.
            for (int i = 0; i < nodes.Length; ++i)
            {
                for (int j = 0; j < nodes.Length; ++j)
                {
                    if (nodes[i].neighbors.Contains(nodes[j]))
                        ret[i].neighbors.Add(ret[j]);
                }
            }

            /* //DEBUG
            for (int i = 0; i < nodes.Length; ++i)
            {
                System.Console.Out.WriteLine();
                System.Console.Out.Write("B: ret__{0}'s neighbors {1}:", i.ToString(), ret[i].neighbors.Count.ToString());
                for (int j = 0; j < nodes.Length; ++j)
                {
                    if (ret[i].neighbors.Contains(ret[j])) System.Console.Out.Write(" {0}", j.ToString());
                }
            }
            */

            Set.HashSet<Node> eliminated = new Set.HashSet<Node>();
            foreach (int i in eliminationOrdering)
            {
                Node elim = ret[i];
                eliminated.Add(elim);

                // Get the neighbors of this node that haven't already been
                // eliminated.  Don't count itself.
                Set.HashSet<Node> remaining = new Set.HashSet<Node>();
                foreach (Node n in elim.neighbors)
                {
                    if (!eliminated.Contains(n) && elim != n)
                        remaining.Add(n);
                }

                // Then add edges between each so that they form a clique
                foreach (Node n in remaining)
                {
                    foreach (Node m in remaining)
                    {
                        if ( !(n == m || n.neighbors.Contains(m)) ) // We don't need to add self-edges or pre-existing ones
                        {
                            n.neighbors.Add(m);
                            /* DEBUG
                            if (n == ret[1])
                            {
                                System.Console.Out.Write("========== Adding to 1's neighbors: ");
                                for (int xx = 0; xx < ret.Length; ++xx)
                                {
                                    if (ret[xx] == m) System.Console.Out.Write("{0}", xx.ToString());
                                    //if (isit == m) System.Console.Out.Write("yes, it's in ret. ");
                                }
                                System.Console.Out.WriteLine();
                            }
                            */
                        }
                    }
                }
            }

            /* //DEBUG
            for (int i = 0; i < nodes.Length; ++i)
            {
                System.Console.Out.WriteLine();
                System.Console.Out.Write("C: ret__{0}'s neighbors {1}:", i.ToString(), ret[i].neighbors.Count.ToString());
                for (int j = 0; j < nodes.Length; ++j)
                {
                    if (ret[i].neighbors.Contains(ret[j])) System.Console.Out.Write(" {0}", j.ToString());
                }     
            }
            */

            //if (nodes.Length == ret.Length) System.Console.Out.WriteLine("nodes and ret have same length; all is well.");
            //else System.Console.Out.WriteLine("nodes and ret DO NOT HAVE THE SAME LENGTH???????");
            return ret;
        }

        /// <summary>
        /// Returns a Potential node list equivalent to the given nodes array.
        /// </summary>
        /// <returns></returns>
        public List<PotentialNode> getPotentialGraph(Node[] graph)
        {
            // debug
            //System.Console.WriteLine("======  Getting potential graph ======");

            List<PotentialNode> ret = new List<PotentialNode>();

            for (int i = 0; i < graph.Length; ++i)
            {
                Node n = graph[i];

                // For now, I'm trying an assignment of edge potentials to the node
                // that has the lower ID of the pair.  This seems to work.

                ArrayList table = new ArrayList();
                List<int> values = new List<int>();

                values.Add(i); // add itself to its variable list

                foreach (double d in n.siteFeatureVals) // it's important that we maintain the order
                    table.Add(d);

                Potential probTable = new Potential(table, values);

                // When creating the triangulated graph, neighbors are added but the interaction/site
                // feature value arrays are not updated.  Thus, the size of these arrays is smaller
                // than the neighbor list, and we need to be careful.

                // debug
                //System.Console.WriteLine("================================= " + n.index);

                for (int nbi = 0; nbi < n.interactionFeatureVals.Count; ++nbi)
                {
                    Node nb = n.neighbors[nbi];
                    /*
                    if (n.index > nb.index) //debug: what if we assign edges differently?
                    {
                        List<int> edgeValues = new List<int>();
                        edgeValues.Add(n.index);
                        edgeValues.Add(nb.index);

                        ArrayList edgeTable = new ArrayList(); // a nested list to replace the 2D array

                        double[][] potentialSlice = n.interactionFeatureVals[nbi];
                        for (int r = 0; r < potentialSlice.Length; ++r)
                        {
                            ArrayList nodeValsB = new ArrayList();
                            for (int c = 0; c < potentialSlice[r].Length; ++c)
                            {
                                nodeValsB.Add(potentialSlice[r][c]);
                            }
                            edgeTable.Add(nodeValsB);
                        }
                        Potential edgePotential = new Potential(edgeTable, edgeValues);
                        probTable = probTable.multiply(edgePotential); // Multiply the node's potential by the edge potential
                    }*/

                    
                    if (n.index < nb.index) // if the index is lower, create and add the edge potential
                    {
                        List<int> edgeValues = new List<int>();
                        edgeValues.Add(n.index);
                        edgeValues.Add(nb.index);

                        ArrayList edgeTable = new ArrayList(); // a nested list to replace the 2D array

                        double[][] potentialSlice = n.interactionFeatureVals[nbi];
                        for (int r = 0; r < potentialSlice.Length; ++r)
                        {
                            ArrayList nodeValsB = new ArrayList();
                            for (int c = 0; c < potentialSlice[r].Length; ++c)
                            {
                                nodeValsB.Add(potentialSlice[r][c]);
                            }
                            edgeTable.Add(nodeValsB);
                        }

                        // debug
                        //System.Console.WriteLine("======================== " + nb.index);
                        //System.Console.WriteLine("site pot:");
                        //probTable.print();
                        //System.Console.WriteLine();
                        //System.Console.WriteLine("edge pot:");
                        

                        Potential edgePotential = new Potential(edgeTable, edgeValues);
                        
                        
                        //edgePotential.print();
                        //System.Console.WriteLine();
                        //end debug

                        probTable = probTable.multiply(edgePotential); // Multiply the node's potential by the edge potential

                        //debug
                        //System.Console.WriteLine("site x edge pot:");
                        //probTable.print();
                        //end debug
                    }
                }
                
                PotentialNode newNode = new PotentialNode(i, n, probTable);
                ret.Add(newNode);               
            }

            // Now we can add the node pointers for the neighbor lists
            for (int i = 0; i < graph.Length; ++i)
            {
                Node n = graph[i];

                // For a PotentialNode, neighbors are tracked with PotentialNode pointers;
                // we have to convert:
                Set.HashSet<PotentialNode> neighborList = new Set.HashSet<PotentialNode>();
                for (int j = 0; j < graph.Length; ++j)
                {
                    if (n.neighbors.Contains(graph[j]))
                        neighborList.Add(ret[j]);
                }

                ret[i].addNeighbors(neighborList);
            }
            // debug
            //System.Console.WriteLine("======  Done getting potential graph ======");

            return ret;
        }

        
        /// <summary>
        /// Returns a junction tree built from the CRF nodes
        /// </summary>
        /// <returns></returns>
        public JTree getJTree()
        {
            Node[] triangulated = getTriangulatedGraph(getElimOrdering());
            List<PotentialNode> potGraph = getPotentialGraph(triangulated);

            // find cliques and edges
            Set.HashSet<JTreeNode> cliques = new Set.HashSet<JTreeNode>();
            Set.HashSet<PotentialNode> removed = new Set.HashSet<PotentialNode>();
            List<JTreeEdge> allEdges = new List<JTreeEdge>();

            foreach (int id in getElimOrdering())
            {
                PotentialNode pn = potGraph[id];
                Set.HashSet<PotentialNode> neighbors = pn.getNeighbors();
                foreach (PotentialNode removee in removed)
                    neighbors.Remove(removee);
                neighbors.Add(pn);
                JTreeNode jtn = new JTreeNode(neighbors);
                // see if the clique is already a subset of another clique

                bool subset = false;
                List<JTreeEdge> tempEdges = new List<JTreeEdge>();

                foreach (JTreeNode c1 in cliques)
                {
                    tempEdges.Add(new JTreeEdge(c1, jtn));
                    if (c1.hasSubset(jtn))
                    {
                        subset = true;
                        break;
                    }
                }
                if (!subset)
                {
                    cliques.Add(jtn);
                    allEdges.AddRange(tempEdges);
                }
                removed.Add(pn);
            }
            
            // build tree
            allEdges.Sort(JTreeEdge.sortBackward());

            Dictionary<JTreeNode, Set.HashSet<JTreeNode>> currentSet = new Dictionary<JTreeNode, Set.HashSet<JTreeNode>>();
            Set.HashSet<JTreeEdge> edgeSet = new Set.HashSet<JTreeEdge>();

            foreach (JTreeEdge e in allEdges)
            {
                JTreeNode first = e.Node1;
                JTreeNode second = e.Node2;

                Set.HashSet<JTreeNode> cSet1;
                if (currentSet.ContainsKey(first))
                    cSet1 = currentSet[first];
                else
                    cSet1 = null;

                Set.HashSet<JTreeNode> cSet2;
                if (currentSet.ContainsKey(second))
                    cSet2 = currentSet[second];
                else
                    cSet2 = null;

                if (cSet1 == null)
                {
                    cSet1 = new Set.HashSet<JTreeNode>();
                    cSet1.Add(first);
                    currentSet[first] = cSet1;
                }
                if (cSet2 == null)
                {
                    cSet2 = new Set.HashSet<JTreeNode>();
                    cSet2.Add(second);
                    currentSet[second] = cSet2;
                }
                if (!cSet1.Contains(second))
                {
                    edgeSet.Add(e);
                    first.addEdge(e);
                    second.addEdge(e);
                    foreach (JTreeNode jtn2 in cSet2)
                        cSet1.Add(jtn2);
                    foreach (JTreeNode jtn1 in cSet1)
                    {
                        currentSet[jtn1] = cSet1; // may be overwriting old values
                    }
                }
            }

            Dictionary<int, PotentialNode> idMap = new Dictionary<int,PotentialNode>();
            foreach (PotentialNode pn in potGraph)
            {
                idMap.Add(pn.getId(), pn);
            }
            JTree ret = new JTree(cliques, edgeSet, idMap);
            JTreeNode root = ret.setRoot(); // set the root, but we don't need it

            return ret;
        }


        public void printBeliefsAndLogZ()
        {
            System.Console.WriteLine("======= Printing beliefs =======");
            
            int[][] adjMat = this.getAdjacencyMatrix();
            
            for (int i = 0; i < nodes.Length; i++)
            {
                System.Console.WriteLine("=== node {0} ===", i);

                for (int j = 0; j < numLabels; j++)
                {
                    System.Console.WriteLine("    site[{0}] = {1}", j, nodes[i].siteBelief[j]);
                }
                System.Console.WriteLine();
                for (int j = 0; j < nodes.Length; j++)
                {
                    if (adjMat[i][j] > 0)
                    {
                        System.Console.WriteLine("  --- edge_{0} ---", j);
                        int iIndexOfj = nodes[i].neighbors.IndexOf(nodes[j]);
                        //int jIndexOfi = nodes[j].neighbors.IndexOf(nodes[i]);

                        // Pulled these out of the inner loops so we don't have to keep casting
                        double[][] ni_iBVs = (double[][])nodes[i].interactionBeliefVals[iIndexOfj];
                        //double[][] nj_iBVs = (double[][])nodes[j].interactionBeliefVals[jIndexOfi];

                        double sum = 0.0;
                        for (int a = 0; a < numLabels; a++)
                        {
                            for (int b = 0; b < numLabels; b++)
                            {
                                //int eIndex = adjMat[i][j] - 1;

                                System.Console.WriteLine("    ({0},{1}): {2}", a, b, ni_iBVs[a][b]);
                                sum += ni_iBVs[a][b];
                                // Reverse a & b to preserve the directionality of the interaction.
                                //nj_iBVs[b][a] = edgeBels[eIndex][a][b];
                            }
                        }
                        System.Console.WriteLine("    sum: " + sum);
                    }
                }

            }

            


            System.Console.WriteLine("Printing LogZ: " + logZ);
        }

        // For debugging
        public void testJTree()
        {
            JTree testTree = getJTree();
            System.Console.WriteLine("=====JTree test=====");
            testTree.printGM();

            /*
            // Write to file
            System.Console.WriteLine("Querying JTree. Output to file jtree_out.txt");
            TextWriter oldOut = Console.Out;
            FileStream ostrm;
            StreamWriter writer;
            try
            {
                ostrm = new FileStream("../../singleStrokeLabeling/tests/jtree_out.txt", FileMode.OpenOrCreate, FileAccess.Write);
                writer = new StreamWriter(ostrm);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot open jtree_out.txt for writing");
                Console.WriteLine(e.Message);
                return;
            }
            Console.SetOut(writer);
            */

            inferExact(); // run exact inference and store beliefs in nodes for comparison
            System.Console.WriteLine("Printing marginal residuals between jtree and exact calculations.");

            // Query
            // query w/o printing to print init stuff
            ArrayList temp = testTree.query(0);
            System.Console.WriteLine();
            foreach (int nodeID in getElimOrdering())
            {
                //System.Console.Write("Node " + nodeID + ":");
                ArrayList nodeBeliefs = testTree.query(nodeID);

                int j = 0;
                foreach (double d in nodeBeliefs)
                {
                    // if we ran csharp inference before this
                    double diff = nodes[nodeID].siteBelief[j] - d;
                    ++j;
                    // end if
                    System.Console.WriteLine("Node " + nodeID + ":" + diff);
                }
            }
            /*
            // Close file
            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();
             * */
            Console.WriteLine("Jtree test end");
        }

        #endregion


        /// <summary>
        /// The method will call a function to use the exact inference functionality in the MIT loopyBP code.
        /// Mostly the same as inferCSharp(), except neither edge beliefs nor logZ are calculated, and useMITExact is used
        /// instead of loopyBPUseMIT()
        /// </summary>
        public void inferExact()
        {
            //Console.WriteLine("Calling MIT INFER EXACT");
            // Get the adjacency matrix
            int[][] adjMat = this.getAdjacencyMatrix();

            // An array keeping track of which strokes are labeled.
            bool[] isEvidenceNode = new bool[nodes.Length];

            // Put all of the site potentials into a 2D matrix, indexed by node then label
            double[][] sitePot = new double[nodes.Length][];

            // MIT LoopyBP site potentials
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].fragment.FirstLabel.Equals("Gate"))
                {
                    isEvidenceNode[i] = true;
                }
                else
                {
                    isEvidenceNode[i] = false;
                }

                sitePot[i] = new double[numLabels];

                for (int j = 0; j < numLabels; j++)
                {
                    sitePot[i][j] = nodes[i].siteFeatureVals[j];
                    //					if (sitePot[i][j].Equals(double.NaN) || double.IsInfinity(sitePot[i][j]))
                    //					{
                    //						Console.WriteLine("NaN or Infinity Caught! at sitePot[{0}][{1}] in infer()", i, j);
                    //					}
                }
            }

            double[][][] interPot = new double[numEdges][][];

            for (int i = 0; i < nodes.Length; i++)
            {
                for (int j = i + 1; j < nodes.Length; j++)
                {
                    if (adjMat[i][j] > 0)
                    {
                        int iIndexOfj = nodes[i].neighbors.IndexOf(nodes[j]);
                        interPot[adjMat[i][j] - 1] = (double[][])nodes[i].interactionFeatureVals[iIndexOfj];  // Indexed by edgenum-1, lable, label

                        for (int p = 0; p < numLabels; p++)
                        {
                            for (int q = 0; q < numLabels; q++)
                            {
                                double currInterPot = interPot[adjMat[i][j] - 1][p][q];
                            }
                        }
                    }
                }
            }

            // Belief variables for the MIT LoopyBP code
            double[][] siteBels = new double[nodes.Length][];
            double[][][] edgeBels = new double[numEdges][][];
            Hashtable configTableRef = new Hashtable();

            // DO INFERENCE.  It returns logZ, too.
            double partitionVal = LoopyBP.LoopyBP.useMITExact(sitePot, interPot, adjMat, numEdges, numLabels,
                ref siteBels, ref edgeBels, ref configTableRef, isEvidenceNode);

            this.logZ = Math.Log(partitionVal); // set logZ
            this.exactConfigurationTable = configTableRef; // store the config table

            // Now reformat the data and place it where it needs to go
            // Store the belief data in each node
            for (int i = 0; i < nodes.Length; i++)
            {
                // temporary debug!
                // System.Console.WriteLine("Exact Node {0}:", i);

                for (int j = 0; j < numLabels; j++)
                {
                    nodes[i].siteBelief[j] = siteBels[i][j];
                    // temporary debug!
                    //System.Console.WriteLine("     " + nodes[i].siteBelief[j]);
                }

                // Store the belief data for each edge
                for (int j = i + 1; j < nodes.Length; j++)
                {
                    if (adjMat[i][j] > 0)
                    {
                        int iIndexOfj = nodes[i].neighbors.IndexOf(nodes[j]);
                        int jIndexOfi = nodes[j].neighbors.IndexOf(nodes[i]);

                        // Pulled these out of the inner loops so we don't have to keep casting
                        double[][] ni_iBVs = (double[][])nodes[i].interactionBeliefVals[iIndexOfj];
                        double[][] nj_iBVs = (double[][])nodes[j].interactionBeliefVals[jIndexOfi];

                        for (int a = 0; a < numLabels; a++)
                        {
                            for (int b = 0; b < numLabels; b++)
                            {
                                int eIndex = adjMat[i][j] - 1;

                                ni_iBVs[a][b] = edgeBels[eIndex][a][b];

                                // Reverse a & b to preserve the directionality of the interaction.
                                nj_iBVs[b][a] = edgeBels[eIndex][a][b];
                            }
                        }

                        // Restore the belief data
                        nodes[i].interactionBeliefVals[iIndexOfj] = ni_iBVs;
                        nodes[j].interactionBeliefVals[jIndexOfi] = nj_iBVs;
                    }
                }

            }

        }


        /// <summary>
        /// The method will call a function to do inference on the CRF.  Currently that method is loopyBP.
        /// Most of this function consists of packaging all of the potential functions so that loopyBP will
        /// be able you use them (it is implemented in Matlab).  It places all of the belief information it
        /// gains from loopyBP in the belief storage arrays in the nodes of the CRF.  This method also stores
        /// logZ in the CRF
        /// </summary>
        public void inferCSharp()
		{
			//Console.WriteLine("Calling MIT INFER");
			// Get the adjacency matrix
			int[][] adjMat = this.getAdjacencyMatrix();

            // An array keeping track of which strokes a labeled.
            bool[] isEvidenceNode = new bool[nodes.Length];

			// Put all of the site potentials into a 2D matrix, indexed by node then label
			double[][] sitePot = new double[nodes.Length][];

			// MIT LoopyBP site potentials
			for (int i = 0; i < nodes.Length; i++)
			{
                if (nodes[i].fragment.FirstLabel.Equals("Gate"))
                {
                    isEvidenceNode[i] = true;
                }
                else
                {
                    isEvidenceNode[i] = false;
                }

				sitePot[i] = new double[numLabels];

				for (int j = 0; j < numLabels; j++)
				{
					sitePot[i][j] = nodes[i].siteFeatureVals[j];
//					if (sitePot[i][j].Equals(double.NaN) || double.IsInfinity(sitePot[i][j]))
//					{
//						Console.WriteLine("NaN or Infinity Caught! at sitePot[{0}][{1}] in infer()", i, j);
//					}
				}
			}
			
			#region loop explanation
			/**
			 * Put all of the interaction potentials into a 3D matrix.
			 * The first index represents the 'edge number' of an interaction potential.  These are found
			 * by looping through the upper triangle of the adjacency matrix of the graph, 
			 *   ----->
			 *    ---->
			 *     --->
			 *      -->
			 * evaluating along rows from top to bottom, as shown above, and numbering all the 1's (edges) sequentially.
			 * The second and third index correspond to labels
			 */
			#endregion
			double[][][] interPot = new double[numEdges][][];			
			
			for (int i = 0; i < nodes.Length; i++)
			{
				for (int j = i + 1; j < nodes.Length; j++)
				{
					if (adjMat[i][j] > 0)
					{
						int iIndexOfj = nodes[i].neighbors.IndexOf(nodes[j]);
						interPot[adjMat[i][j] - 1] = (double[][])nodes[i].interactionFeatureVals[iIndexOfj];  // Indexed by edgenum-1, lable, label

						for (int p = 0; p < numLabels; p++)
						{
							for (int q = 0; q < numLabels; q++)
							{
								double currInterPot = interPot[adjMat[i][j] - 1][p][q];

//								if (currInterPot.Equals(double.NaN) || double.IsInfinity(currInterPot))
//								{
//									Console.WriteLine("NaN Caught! at interPot[{0}][{1}][{2}] in infer()", adjMat[i][j] - 1, p, q);
//								}
							}
						}
					}
				}
			}

			// Belief variables for the MIT LoopyBP code
			double[][] siteBels = new double[nodes.Length][];
			double[][][] edgeBels = new double[numEdges][][];

			#region DEBUG CODE
			
//			// For Matlab vs MIT debugging...
//			sitePot = new double[2][];
//			sitePot[0] = new double[3]{0.4, 0.5, 0.8};
//			sitePot[1] = new double[3]{0.6, 0.6, 0.2};
//			
/*			// Simple example: 3 nodes, 2 edges
			sitePot = new double[3][];			
			sitePot[0] = new double[2]{0.4,0.6};
			sitePot[1] = new double[2]{0.5,0.5};
			sitePot[2] = new double[2]{0.8,0.2};

			interPot = new double[2][][];
			interPot[0] = new double[2][];
			interPot[0][0] = new double[2]{0.9, 0.1};
			interPot[0][1] = new double[2]{0.1, 0.9};

			interPot[1] = new double[2][];
			interPot[1][0] = new double[2]{0.9, 0.1};
			interPot[1][1] = new double[2]{0.1, 0.9};

			numLabels = 2;
			numEdges = 2;

			adjMat = new int[3][];
			adjMat[0] = new int[3]{0, 1, 0};
			adjMat[1] = new int[3]{1, 0, 2};
			adjMat[2] = new int[3]{0, 2, 0};

			siteBels = new double[3][];

			edgeBels = new double[2][][];
 */
//
//			belBP = new double[numLabels * 3];
//			belE = new double[(numLabels * numLabels) * numEdges];

			#endregion	 

			// DO INFERENCE
			LoopyBP.LoopyBP.loopyBPUse_MIT(sitePot, interPot, adjMat, numEdges, numLabels, 5000,
				ref siteBels, ref edgeBels, isEvidenceNode);

			// Now reformat the data and place it where it needs to go
			// Store the belief data in each node
			for (int i = 0; i < nodes.Length; i++)
			{
                // temporary debug!
                //System.Console.WriteLine("Loopy Node {0}:", i);

				for (int j = 0; j < numLabels; j++)
				{
					nodes[i].siteBelief[j] = siteBels[i][j];
                    // temporary debug!
                    //System.Console.WriteLine("     " + nodes[i].siteBelief[j]);
				}

				// Store the belief data for each edge
				for (int j = i + 1; j < nodes.Length; j++)
				{
					if (adjMat[i][j] > 0)
					{
						int iIndexOfj = nodes[i].neighbors.IndexOf(nodes[j]);
						int jIndexOfi = nodes[j].neighbors.IndexOf(nodes[i]);

						// Pulled these out of the inner loops so we don't have to keep casting
						double[][] ni_iBVs = (double[][])nodes[i].interactionBeliefVals[iIndexOfj];
						double[][] nj_iBVs = (double[][])nodes[j].interactionBeliefVals[jIndexOfi];
						
						for (int a = 0; a < numLabels; a++)
						{
							for (int b = 0; b < numLabels; b++)
							{
								int eIndex = adjMat[i][j] - 1;
								
								ni_iBVs[a][b] = edgeBels[eIndex][a][b];
								
								// Reverse a & b to preserve the directionality of the interaction.
								nj_iBVs[b][a] = edgeBels[eIndex][a][b];
							}
						}

						// Restore the belief data
						nodes[i].interactionBeliefVals[iIndexOfj] = ni_iBVs;
						nodes[j].interactionBeliefVals[jIndexOfi] = nj_iBVs;
					}
				}
			}

			// Calculate the logZ from our beliefs
			calcLogZ();
		}


		/// <summary>
		/// Calculate logZ using Bethe free energy approximation of the entropy term.
		/// Based on Kevin Murphy's CRF Toolkit		
		/// </summary>
		private void calcLogZ()
		{
			double E1 = 0.0;
			double E2 = 0.0;
			double H1 = 0.0;
			double H2 = 0.0;

			for (int n1 = 0; n1 < nodes.Length; n1++)
			{
				double[] b = new double[numLabels];

				// b holds the belief for node n1.  If it is 0 then change
				// to 1, to prevent division errors and since it doesn't matter
				// what the value is if its 0.
				for (int j = 0; j < numLabels; j++)
				{
					if (nodes[n1].siteBelief[j].Equals(0.0))
					{
						b[j] = 1.0;
					}
					else
					{
						b[j] = nodes[n1].siteBelief[j];
					}
				}

				double bDotLogb = 0.0;
				for (int j = 0; j < numLabels; j++)
				{
					bDotLogb += b[j] * Math.Log(b[j]);
				}
				H1 += (nodes[n1].neighbors.Count - 1) * bDotLogb;

				double bDotLogEvidence = 0.0;
				for (int j = 0; j < numLabels; j++)
				{
					bDotLogEvidence += b[j] * Math.Log(nodes[n1].siteFeatureVals[j]);
				}
				E1 -= bDotLogEvidence;

				for (int n2 = n1 + 1; n2 < nodes.Length; n2++)
				{
					if (nodes[n1].neighbors.Contains(nodes[n2]))
					{
						int n1IndexOfn2 = nodes[n1].neighbors.IndexOf(nodes[n2]);
						double[][] n1n2Pot = ((double[][])nodes[n1].interactionFeatureVals[n1IndexOfn2]);
						double[][] belief = ((double[][])nodes[n1].interactionBeliefVals[n1IndexOfn2]);

						double piecewiseMultbeliefLogbelief = 0.0;
						double piecewiseMultbeliefLogPot = 0.0;
						
						for (int l1 = 0; l1 < numLabels; l1++)
						{
							for (int l2 = 0; l2 < numLabels; l2++)
							{
								if (belief[l1][l2] == 0)
								{
									belief[l1][l2] = 1;
								}

								piecewiseMultbeliefLogbelief += belief[l1][l2] * Math.Log(belief[l1][l2]);
								piecewiseMultbeliefLogPot += belief[l1][l2] * Math.Log(n1n2Pot[l1][l2]);
							}
						}
						
						H2 -= piecewiseMultbeliefLogbelief;
						E2 -= piecewiseMultbeliefLogPot;
					}
				}
			}

			this.logZ = -1 * ( (E1 + E2) - (H1 + H2) );
		}

		#region MATLAB INFER

		/// <summary>
		/// The method will call a function to do inference on the CRF.  Currently that method is loopyBP.
		/// Most of this function consists of packaging all of the potential functions so that loopyBP will
		/// be able you use them (it is implemented in Matlab).  It places all of the belief information it
		/// gains from loopyBP in the belief storage arrays in the nodes of the CRF.  This method also stores
		/// logZ in the CRF
		/// </summary>
		public void inferMatlab()
		{
			// Get the adjacency matrix
			int[][] adjMat = this.getAdjacencyMatrix();

			// Put all of the site potentials into a 2D matrix, indexed by label then node
			double[][] sitePot = new double[numLabels][];
			
			for (int i = 0; i < numLabels; i++)
			{
				sitePot[i] = new double[nodes.Length];

				for (int j = 0; j < nodes.Length; j++)
				{
					sitePot[i][j] = nodes[j].siteFeatureVals[i];
//					if (sitePot[i][j].Equals(double.NaN))
//					{
//						Console.WriteLine("NaN Caught! at sitePot[{0}][{1}] in infer()", i, j);
//					}
				}
			}

			#region loop explanation
			/**
			 * Put all of the interaction ptoentials into a 3D matrix.
			 * The first index represents the 'edge number' of an interaction potential.  These are found
			 * by looping through the upper triangle of the adjacency matrix of the graph, 
			 *   ----->
			 *    ---->
			 *     --->
			 *      -->
			 * evaluating along rows from top to bottom, as shown above, and numbering all the 1's (edges) sequentially.
			 * The second and third index correspond to labels
			 */
			#endregion
			double[][][] interPot = new double[numEdges][][];			
			
			for (int i = 0; i < nodes.Length; i++)
			{
				for (int j = i + 1; j < nodes.Length; j++)
				{
					if (adjMat[i][j] > 0)
					{
						int iIndexOfj = nodes[i].neighbors.IndexOf(nodes[j]);
						interPot[adjMat[i][j] - 1] = (double[][])nodes[i].interactionFeatureVals[iIndexOfj];  // Indexed by edgenum-1, lable, label

//						for(int p = 0; p < numLabels; p++)
//						{
//							for(int q = 0; q < numLabels; q++)
//							{
//								if (interPot[edgeNum][p][q].Equals(double.NaN))
//								{
//									Console.WriteLine("NaN Caught! at interPot[{0}][{1}][{2}] in infer()", edgeNum,p,q);
//								}
//							}
//						}
					}
				}
			}

			// Create a place for the output (Matlab variables)
			double[] belBP = new double[numLabels * nodes.Length];
			double[] belE = new double[(numLabels * numLabels) * numEdges];
			double logZ = 0.0;
			
			#region DEBUG CODE
			
			//			// For Matlab vs MIT debugging...
			//			sitePot = new double[2][];
			//			sitePot[0] = new double[3]{0.4, 0.5, 0.8};
			//			sitePot[1] = new double[3]{0.6, 0.6, 0.2};
			//			
			//			// Simple example: 3 nodes, 2 edges
			//			sitePot2 = new double[3][];			
			//			sitePot2[0] = new double[2]{0.4,0.6};
			//			sitePot2[1] = new double[2]{0.5,0.5};
			//			sitePot2[2] = new double[2]{0.8,0.2};
			//
			//			interPot = new double[2][][];
			//			interPot[0] = new double[2][];
			//			interPot[0][0] = new double[2]{0.9, 0.1};
			//			interPot[0][1] = new double[2]{0.1, 0.9};
			//
			//			interPot[1] = new double[2][];
			//			interPot[1][0] = new double[2]{0.9, 0.1};
			//			interPot[1][1] = new double[2]{0.1, 0.9};
			//
			//			numLabels = 2;
			//			numEdges = 2;
			//
			//			adjMat = new int[3][];
			//			adjMat[0] = new int[3]{0, 1, 0};
			//			adjMat[1] = new int[3]{1, 0, 2};
			//			adjMat[2] = new int[3]{0, 2, 0};
			//
			//			siteBels = new double[3][];
			//			edgeBels = new double[2][][];
			//
			//			belBP = new double[numLabels * 3];
			//			belE = new double[(numLabels * numLabels) * numEdges];

			#endregion

			// DO INFERENCE
			if (loopyBPInitialized == false)
			{
				LoopyBP.LoopyBP.loopyBPUse(ref belBP, ref belE, ref logZ, adjMat, interPot, sitePot, numLabels, loopyBPInitialized);
				loopyBPInitialized = true;
			}
			else
			{
				LoopyBP.LoopyBP.loopyBPUse(ref belBP, ref belE, ref logZ, adjMat, interPot, sitePot, numLabels, loopyBPInitialized);
			}

			// Now reformat the data and place it where it needs to go.  This means that belBP will be stored in
			// the correct nodes, belE will be stored in the correct pairs of nodes (where each node gets a copy)
			// and logZ will be stored as a memeber of CRF
			this.logZ = logZ;

			// Store the belief data in each node
			for (int i = 0; i < nodes.Length; i++)
			{
				for (int j = 0; j < numLabels; j++)
				{
					// belBP was a 2D matrix with labels in rows and nodes in columns.  This was compressed
					// columnwise into a 1D array, so to get belief of node i, label j, we take the i*numLabels + j spot in the array
//					if(belBP[(i * numLabels) + j].Equals(double.NaN))
//					{
//						Console.WriteLine("The site belief at {0} * numLabels + {1} is NaN", i, j);
//					}

					nodes[i].siteBelief[j] = belBP[(i * numLabels) + j];
				}

				// Store the belief data for each edge
				for (int j = i + 1; j < nodes.Length; j++)
				{
					if (adjMat[i][j] > 0)
					{
						int iIndexOfj = nodes[i].neighbors.IndexOf(nodes[j]);
						int jIndexOfi = nodes[j].neighbors.IndexOf(nodes[i]);

						// Pulled these out of the inner loops so we don't have to keep casting
						double[][] ni_iBVs = (double[][])nodes[i].interactionBeliefVals[iIndexOfj];
						double[][] nj_iBVs = (double[][])nodes[j].interactionBeliefVals[jIndexOfi];
						
						for (int a = 0; a < numLabels; a++)
						{
							for (int b = 0; b < numLabels; b++)
							{
//								if(belE[(numLabels*((edgeNumbers[i][j] - 1) + b * numEdges)) + a].Equals(double.NaN))
//								{
//									Console.WriteLine("Node {0} and Node {1} have NaN interaction at {2} {3}", i, j, a, b);
//								}
			
								#region index explanation
								/**
								 * So this godawful expression is the result of matlab compressing a 3D array into a 1D array in a strange way
								 * Lets say we start off with an array indexed by edge number e, label a, and label b as [e][a][b].  Furthermore,
								 * let E be the number of edges and L be the number of labels.
								 * 
								 * The inital 3D [e][a][b] array makes a fair amount of sense.  However, it is converted to a 2D array where
								 * the there are L rows indexed by a, and L*E columns.  The columns are weird.  They are arranged as
								 * (label1, edge1), (label1, edge2), ..., (label2,edge1),(label2,edge2),...
								 * which is basically an interleaving of the old matricies.  Thus the index becomes
								 * [a][e + E*b]
								 *
								 * Finally, this 2D array is made 1D columnwise and the index becomes [L*(e + E*b) + a], which is what we have
								 * here.
								 *
								 * Note that I subtract 1 from edgeNumbers because I want it to start at 0 now 
								 */
								#endregion
								int index = (numLabels * ((adjMat[i][j] - 1) + b * numEdges)) + a;
								
								ni_iBVs[a][b] = belE[index];
							
								// Reverse a & b to preserve the directionality of the interaction.
								nj_iBVs[b][a] = belE[index];
							}
						}

						// Restore the belief data
						nodes[i].interactionBeliefVals[iIndexOfj] = ni_iBVs;
						nodes[j].interactionBeliefVals[jIndexOfi] = nj_iBVs;
					}
				}
			}
		}

		#endregion

		#region INFER2 attempt
        // old and unused
		public void infer2()
		{
			bool converged = false;
			int maxIter = 100;

			double[][] prodOfMesg = new double[nodes.Length][];
			double[][] oldBelief = new double[nodes.Length][];
			double[][] error = new double[nodes.Length][];

			for (int i = 0; i < nodes.Length; i++)
			{
				prodOfMesg[i] = new double[numLabels];
				oldBelief[i] = new double[numLabels];
				error[i] = new double[numLabels];
			}

			// What I do is store messages in a three dimensional array.  The first two indices
			// indicate the sending and receiving nodes respectively, and the final index indicates
			// the label that this message corresponds to
			//double[][] oldMesg = new double[numEdges][];
			double[][][] oldMesg = new double[nodes.Length][][];
			double[][][] newMesg = new double[nodes.Length][][];
			for (int i = 0; i < nodes.Length; i++)
			{
				oldMesg[i] = new double[nodes.Length][];
				newMesg[i] = new double[nodes.Length][];

//				for(int j = 0; j < nodes.Length; j++)
//				{
//					//oldMesg[i][j] = new double[numLabels]; This is initalized differently depending on if a label exists
//					//newMesg[i][j] = new double[numLabels];
//				}
			}

			#region INITALIZATION
			// Initalize the belief and message matrices
			for (int n1 = 0; n1 < nodes.Length; n1++)
			{
				// Initialize prodOfMesg w/ the local evidence for a node, which is the vector of 
				// site potentials for that node
				for (int p = 0; p < numLabels; p++)
				{
					prodOfMesg[n1][p] = nodes[n1].siteFeatureVals[p];
					oldBelief[n1][p] = nodes[n1].siteFeatureVals[p];
				}

				// WARNING WILL ROBINSON!!!  the following commented out code will make prodOfMesg[n1]
				// point to the same object as nodes[n1].siteFeatureVales
				//prodOfMesg[n1] = nodes[n1].siteFeatureVals;
				//oldBelief[n1] = nodes[n1].siteFeatureVals;

				// Find all of n1's neighbors and initalize the mesages between them to be of
				// equal probability
				for (int n2 = 0; n2 < nodes.Length; n2++)
				{
					if (nodes[n1].neighbors.Contains(nodes[n2]))
					{
						//oldMesg[nodes[n1].index][nodes[n2].index] = new double[numLabels];
						//newMesg[nodes[n1].index][nodes[n2].index] = new double[numLabels];
						oldMesg[n1][n2] = new double[numLabels];
						newMesg[n1][n2] = new double[numLabels];
						
						for (int i = 0; i < numLabels; i++)
						{
							//oldMesg[nodes[n1].index][nodes[n2].index][i] = 1 / numLabels;
							oldMesg[n1][n2][i] = (1.0 / numLabels);
						}
					}
					else
					{
						// Mark non-neighbors with empty arrays of probabilities
						//oldMesg[nodes[n1].index][nodes[n2].index] = new double[0];
						//newMesg[nodes[n1].index][nodes[n2].index] = new double[0];
						oldMesg[n1][n2] = new double[0];
						newMesg[n1][n2] = new double[0];
					}
				}
			}
			#endregion

			for (int i = 0; i < maxIter; i++)
			{
				// Delete me
				//Console.WriteLine();
				//Console.WriteLine();
				//Console.WriteLine("ITERATION {0} OF NODE BELIEFS",i);

				#region calculate edge values
				for (int n1 = 0; n1 < nodes.Length; n1++)
				{
					// Loop through n1's neighbors
					for (int n2 = 0; n2 < nodes.Length; n2++)
					{
						if (nodes[n1].neighbors.Contains(nodes[n2]))
						{
							double[][] n1n2Pot;

							// Taking this from the matlab code to guarantee that potential is
							// from upper triangle of matrix.  This may have some symmetry assumptions
							// or just be a holdover from matlab not fully populating its potential matrix
							// in order to save time
							// !!!
							// QUITE POSSIBLY SUPERFLUOUS / WRONG WITH MY IMPLEMENTATION
							if (n1 < n2)
							{
								// Get potential n1,n2
								int n1IndexOfn2 = nodes[n1].neighbors.IndexOf(nodes[n2]);
								n1n2Pot = ((double[][])nodes[n1].interactionFeatureVals[n1IndexOfn2]);
							}
							else
							{
								// Get potential n2,n1
								int n2IndexOfn1 = nodes[n2].neighbors.IndexOf(nodes[n1]);
								n1n2Pot = ((double[][])nodes[n2].interactionFeatureVals[n2IndexOfn1]);
							}

							// Matlab computes the product of all incoming messages except for the
							// one from n2 by dividing out the old n2 from the old message here.
							// I'm worried about division by zero errors, so I'm going to implement
							// the method that I understand first

							// More traditional way of computing this is to go through all of the 
							// neighbors that are _not_ n2, and multiplying their old messages by
							// the localEvidence (potential)
							//double[] temp = nodes[n1].siteFeatureVals;
							double[] temp = new double[numLabels];
							for (int p = 0; p < numLabels; p++)
							{
								temp[p] = nodes[n1].siteFeatureVals[p];
							}

							for (int n3 = 0; n3 < nodes.Length; n3++)
							{
								if (nodes[n3].neighbors.Contains(nodes[n1]))
								{
									if( n2 == n3 )
									{
										continue;
									}

									for(int p = 0; p < temp.Length; p++)
									{
										temp[p] *= oldMesg[n3][n1][p];
									}
								}
							}

							// Calculate and normalize new message
							double[] newM = CreateGraph.matrixTimesVector(n1n2Pot, temp);
							double norm = 0.0;
							for (int p = 0; p < newM.Length; p++)
							{
								norm += newM[p];
							}

							// DELETE ME!!! ERROR CHECKING
							if (norm == 0)
							{
								Console.WriteLine("Going to have a division by zero error as soon as anything divides by norm!!!");
							}

							for (int p = 0; p < newM.Length; p++)
							{
								newMesg[n1][n2][p] = newM[p] / norm;
							}	
						}
					}
				}
				#endregion

				double[][] oldProdOfMesg = prodOfMesg;

				// Multiply in all all of the messages from neighboring nodes n2
				// to node n1
				for (int n1 = 0; n1 < nodes.Length; n1++)
				{
					//prodOfMesg[n1] = nodes[n1].siteFeatureVals;\
					for (int p = 0; p < numLabels; p++)
					{
						prodOfMesg[n1][p] = nodes[n1].siteFeatureVals[p];
					}

					for (int n2 = 0; n2 < nodes.Length; n2++)
					{
						if(nodes[n1].neighbors.Contains(nodes[n2]))
						{
							for (int p = 0; p < numLabels; p++)
							{
								// The order is important in newMesg!!!
								// The messages are from neighbors to n1
								prodOfMesg[n1][p] *= newMesg[n2][n1][p];
							}
						}
					}

					// Now need to normalize the prodOfMesg in order to store it as a belief
					// TODO: Move normalization code into original loop for efficiency
					double norm = 0.0;
					for (int p = 0; p < prodOfMesg[n1].Length; p++)
					{
						norm += prodOfMesg[n1][p];
					}
					for (int p = 0; p < prodOfMesg[n1].Length; p++)
					{
						nodes[n1].siteBelief[p] = prodOfMesg[n1][p] / norm;
						//newBelief[n1][p] = prodOfMesg[n1][p] / norm;
					}
				}

				converged = true;
				for (int a = 0; a < nodes.Length; a++)
				{
					for (int b = 0; b < numLabels; b++)
					{
						//Console.WriteLine("Starting error testing for node {0} and label {1}",a,b);

						error[a][b] = Math.Abs(nodes[a].siteBelief[b] - oldBelief[a][b]);
						if(error[a][b] >= ERROR_TOL)
						{
							converged = false;
						}

						//Console.WriteLine("The difference between new belief {0} and old belief {1} is {2}",nodes[a].siteBelief[b],oldBelief[a][b],error[a][b]);

						// Take care of this now so that we don't have to do more looping
						oldBelief[a][b] = nodes[a].siteBelief[b];
					}
				}

				if (converged)
				{
					// Delete me!!!
					//Console.WriteLine("It took {0} iterations for loopyBP to converge",i);
					break;  // Still need to do the edge calculations
				}

				//oldMesg = newMesg;
				for (int a = 0; a < nodes.Length; a++)
				{
					for (int b = 0; b < nodes.Length; b++)
					{
						for (int c = 0; c < newMesg[a][b].Length; c++)
						{
							oldMesg[a][b][c] = newMesg[a][b][c];
						}
					}
				}

				//if(i > 80)
				//	Console.WriteLine("i is currently {0}",i);
			}

			#region EDGE BELIEF COMPUTATION
			// Now compute the edge belief
			/*Summary of this function:
			Loop through all nodes, and each of the neighbors of each of those nodes
			For each node-neighbor pair, find the last message that was sent from node
				to neighbor and from neighbor to node (make sure this message is not zero)
			Store beli, which is the vector of beliefs at node i elementwise divided by
				the message into this node from its neighbor
			Store belj, which is the vector of beliefs at node j elementwise divided by
				the message into this node from its neighbor
			Make belij, which is a numLabels by numLabels matrix (assuming nodes i & j have the 
				same number of labels) that contains all possible products of an element of beli
				with an element of belj
			Piecewise multiply belij with the potential for the i-j edge to get the edge belief matrix.
				The elements of the matrix in the product end up matching up if indexed by labels.
				NOTE: Remember to normalize the edge belief!!!
			*/
			for (int n1 = 0; n1 < nodes.Length; n1++)
			{
				for (int n2 = 0; n2 < nodes.Length; n2++)
				{
					if (nodes[n1].neighbors.Contains(nodes[n2]))
					{
						double[] n1Bel = new double[numLabels];
						double[] n2Bel = new double[numLabels];

						for (int i = 0; i < numLabels; i++)
						{
							// Adds error checking to make sure we don't have division by zero...
							//newMesg[n2][n1][i] == 0 ? n1Bel[i] = nodes[n1].siteBelief[i] : n1Bel[i] = nodes[n1].siteBelief[i] / newMesg[n2][n1][i];
							//newMesg[n1][n2][i] == 0 ? n1Bel[i] = nodes[n1].siteBelief[i] : n1Bel[i] = nodes[n1].siteBelief[i] / newMesg[n1][n2][i];

							if (newMesg[n1][n2][i] == 0)
							{
								Console.WriteLine("Mesg[{0}][{1}][{2}] is 0...",n1,n2,i);
								n1Bel[i] = nodes[n1].siteBelief[i];
							}
							else
							{
								n1Bel[i] = nodes[n1].siteBelief[i] / newMesg[n2][n1][i];
							}

							if (newMesg[n2][n1][i] == 0)
							{
								Console.WriteLine("Mesg[{0}][{1}][{2}] is 0...",n2,n1,i);
								n1Bel[i] = nodes[n1].siteBelief[i];
							}
							else
							{
								n1Bel[i] = nodes[n1].siteBelief[i] / newMesg[n1][n2][i];
							}

							//n1Bel[i] = nodes[n1].siteBelief[i] / newMesg[n2][n1][i];
							//n2Bel[i] = nodes[n2].siteBelief[i] / newMesg[n1][n2][i];
						}

						for (int i = 0; i < numLabels; i++)
						{
							for (int j = 0; j < numLabels; j++)
							{
								int n1IndexOfn2 = nodes[n1].neighbors.IndexOf(nodes[n2]);
								((double[][])nodes[n1].interactionBeliefVals[n1IndexOfn2])[i][j] = n1Bel[i] * n2Bel[j] * ((double[][])nodes[n1].interactionFeatureVals[n1IndexOfn2])[i][j];
							}
						}
					}
				}
			}  // End of edge belief computation
			#endregion
		}

	
		/// <summary>
		/// Find the belief for all of the edges
		/// Based on Kevin Murphy's CRF Toolkit
		/// </summary>
		private void calEdgeBel()
		{
			for(int n1 = 0; n1 < nodes.Length; n1++)
			{
				//for(int n2 = 0; n2 < nodes.Length; n2++)
				for(int n2 = 0; n2 < nodes.Length; n2++)
				{
					// Matlab has some error checking to make sure that the edge belief used
					// is from the upper right triangle of the potential matrix

				}
			}
		}
	
		#endregion

		#region MISC

		/// <summary>
		/// Get a random double in the range [0, 1]. These should be normally distributed
		/// around 0.5 with sigma=0.25
		/// </summary>
		/// <returns></returns>
		public static double NextGaussianDouble()
		{
			double g = gaussian.NextDouble();
			while (0 < g || g > 1)
				g = gaussian.NextDouble();
			return g;
		}

		#endregion

	}
}