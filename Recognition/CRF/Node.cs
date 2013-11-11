/**
 * File: Node.cs
 * 
 * Authors: Aaron Wolin, Devin Smith, Jason Fennell, and Max Pflueger (Sketchers 2006).
 *          Code expanded by Anton Bakalov (Sketchers 2007).
 *          Harvey Mudd College, Claremont, CA 91711.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */

using System.Collections;
using System.Collections.Generic;
using ConverterXML;
using Sketch;
using Featurefy;

namespace CRF
{
	/// <summary>
	/// Node stores a node in the CRF graph.  It also stores its connections to other nodes and its connection to a Fragment.  
	/// During inference the Node will assume labels and probabilities associated with those labels.
	/// </summary>
	public class Node
	{
		/// <summary>
		/// fragment stores a pointer to the Fragment associated with this Node.
		/// </summary>
		public Substroke fragment;

		/// <summary>
		/// Stores feature data for the nodes fragment to minimize casting costs
		/// </summary>
		public FeatureStroke fragFeat;

		/// <summary>
		/// neighbors is an List of pointers to Nodes, representing the neighbors of this Node.
		/// </summary>
		public List<Node> neighbors;

		/// <summary>
		/// Each element of this array list will be a 2D array of doubles.  This array will store the interaction potentials between
		/// this node and one of its neighbors, for all possible pairs of labels.
		/// 
		/// This array is added to the List at the same time a Node is added to neighbors, so a given node and its interaction potential
		/// table will have the same index in these two Lists
		/// </summary>
		public List<double[][]> interactionFeatureVals;

		/// <summary>
		/// Should be the same size and shape as interaction feature vals, but it is calculated by doing inference on the graph.
		///  This value should not be considered to represent the current parameter set for training CRF's.
		/// </summary>
		public List<double[][]> interactionBeliefVals;

		/// <summary>
		/// siteFeatureVals is an array of the evaluated values of the site potentials for each label.  
		/// Each element has a corresponding label, and its value is the probability that that label applies to this Node.
		/// </summary>
		public double[] siteFeatureVals;

		/// <summary>
		/// This holds the 'belief' that this node is a given label once inference has been done on the CRF
		/// </summary>
		public double[] siteBelief;

		/// <summary>
		/// The number of different labels that a Node can be classified as.
		/// </summary>
		public int numLabels;

		/// <summary>
		/// The index of this node in the CRF's node array
		/// </summary>
		public int index;

		/// <summary>
		/// The values of the individual site feature functions for this node, indexed by label then feature number
		/// </summary>
		public double[][] siteFeatureFunctionVals;

		/// <summary>
		/// The values of the individual interaction feature functions for this node.  Each element is a 3D array that corresponds to the 
		/// a neighbor of this node.  The 3D array is indexed by label, label, then feature number
		/// </summary>
		public List<double[][][]> interactionFeatureFunctionVals;

		/// <summary>
		/// Constructor requires the input of a pointer to a fragment that this Node will represent.
		/// </summary>
		/// <param name="fragment">The fragment that this node represents in a CRF</param>
		/// <param name="numLabels">The number of differnt labels that this node can be classified as</param>
		/// <param name="index">Keeps track of the index of this node in the CRF's node array so that some
		/// nicer looping mechanisms can be used</param>
		public Node(FeatureStroke fs, int numLabels, int index)
		{
			this.fragment = fs.Substroke;
			fragFeat = fs;
			this.numLabels = numLabels;
			neighbors = new List<Node>();
			interactionFeatureVals = new List<double[][]>();
			interactionFeatureFunctionVals = new List<double[][][]>();
			interactionBeliefVals = new List<double[][]>();
			siteFeatureVals = new double[numLabels];
			siteFeatureFunctionVals = new double[numLabels][];
			siteBelief = new double[numLabels];
			this.index = index;
		}

        /// <summary>
        /// Constructor.  Shallow-copies a node except for the neighbors list.
        /// </summary>
        /// <param name="node">The node to copy</param>
        public Node(Node n)
        {
            fragment = n.fragment;
            fragFeat = n.fragFeat;
            neighbors = new List<Node>();
            interactionFeatureVals = n.interactionFeatureVals;
            interactionBeliefVals = n.interactionBeliefVals;
            siteFeatureVals = n.siteFeatureVals;
            siteBelief = n.siteBelief;
            numLabels = n.numLabels;
            index = n.index;
            siteFeatureFunctionVals = n.siteFeatureFunctionVals;
            interactionFeatureFunctionVals = n.interactionFeatureFunctionVals;
        }

		public void addNeighbor(Node n)
		{
			neighbors.Add(n);
			
			double[][] interPotential = new double[numLabels][];
			double[][][] interFeatures = new double[numLabels][][];
			double[][] belief = new double[numLabels][];

			for(int i = 0; i < numLabels; i++)
			{
				interPotential[i] = new double[numLabels];
				interFeatures[i] = new double[numLabels][];
				belief[i] = new double[numLabels];
			}

			interactionFeatureVals.Add(interPotential);
			interactionFeatureFunctionVals.Add(interFeatures);
			interactionBeliefVals.Add(belief);
		}
	}
}