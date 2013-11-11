/*
 * File: TreeClassifier.cs
 *
 * Author: Sketchers 2010
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2010.
 *
 */

using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace DecisionTrees
{
    /// <summary>
    /// Since our decision trees were created using Weka's
    /// AdaBoost classifier (with 10 iterations), we had expected
    /// to use all 10 trees for classification. However,
    /// after testing, we determined that a single tree performed
    /// better than a combination of all 10 trees. So, some of this
    /// stuff is not REALLY necessary, but I'll leave it in for now,
    /// just in case someday somebody wants to use multiple trees.
    /// </summary>
    public class TreeClassifier
    {
        #region Constants

        private const string LESS_THAN_OR_EQUAL = "<=";
        private const string GREATER_THAN = ">";

        #endregion

        #region Internals

        /// <summary>
        /// The list of decision trees.
        /// </summary>
        private List<TreeNode> _trees;

        #endregion

        #region Constructor

        /// <summary>
        /// Default Constructor
        /// </summary>
        public TreeClassifier()
        {
            // Default Constructor does nothing.
            // You'll need to load a tree from a file.
            _trees = new List<TreeNode>();
        }

        #endregion

        #region Classify Instance

        /// <summary>
        /// Classify a single instance
        /// </summary>
        /// <param name="test"></param>
        /// <returns></returns>
        public string classifyInstance(Instance test)
        {
            Dictionary<string, float> classifications = new Dictionary<string, float>();

            // Use all the trees to classify the instance
            foreach (TreeNode tree in _trees)
            {
                KeyValuePair<string, float> result = tree.Classify(test);
                string classify = result.Key;
                float probability = result.Value;

                if (classifications.ContainsKey(classify))
                    classifications[classify] += probability * tree.Weight;
                else
                    classifications.Add(classify, probability * tree.Weight);
            }

            // Pick only the best classification,
            // based on the weights of the individual trees.
            float max = 0;
            string bestClass = null;
            foreach (string classify in classifications.Keys)
            {
                if (classifications[classify] > max)
                    bestClass = classify;
            }

            return bestClass;
        }

        #endregion

        #region Load Classifier

        /// <summary>
        /// Load a decision tree classifier made by Weka 
        /// from a text file.
        /// </summary>
        /// <param name="filename"></param>
        public void loadClassifier(string filename)
        {
            try
            {
                int count = -1;
                string line;
                bool start = false;
                string[] nodeString;
                bool lessThanOrEqual;
                TreeNode prevNode = new TreeNode();

                // Read the file and display it line by line.
                System.IO.StreamReader file =
                   new System.IO.StreamReader(filename);

                while ((line = file.ReadLine()) != null)
                {
                    if (start && (line != ""))
                    {
                        // Make a new decision tree node for each line.
                        TreeNode newNode = new TreeNode();

                        // Determine the level of hierarchy for the node.
                        newNode.Level = countOccurances(line, "|   ");
                        line = line.Replace("|   ", "");

                        // Determine the operation sign for the node.
                        // Split the line into attribute, bar value, and possibly classification.
                        if (line.Contains(LESS_THAN_OR_EQUAL))
                        {
                            lessThanOrEqual = true;
                            nodeString = line.Split(new string[] { LESS_THAN_OR_EQUAL, ":", "(", ")" },
                                                        StringSplitOptions.RemoveEmptyEntries);
                        }
                        else
                        {
                            lessThanOrEqual = false;
                            nodeString = line.Split(new string[] { GREATER_THAN, ":", "(", ")" },
                                                        StringSplitOptions.RemoveEmptyEntries);
                        }

                        // Parse the text to get the attribute and bar value.
                        newNode.DecidingAttribute = nodeString[0].Trim();
                        newNode.Bar = System.Convert.ToDouble(nodeString[1].Trim());

                        // If there is an attribute, bar, classification, and stats ratio,
                        // the node is a leaf.
                        if (nodeString.Length > 2)
                        {
                            TreeNode leaf = new TreeNode();
                            leaf.IsLeaf = true;
                            leaf.Classification = nodeString[2].Trim();
                            leaf.Parent = newNode;

                            // Determine the probability of a leaf's classification.
                            string[] stats = nodeString[3].Split(new string[] { "/" },
                                                StringSplitOptions.RemoveEmptyEntries);
                            if (stats.Length > 1)
                                leaf.Probability = 1 - (System.Convert.ToSingle(stats[1]) / System.Convert.ToSingle(stats[0]));
                            else
                                leaf.Probability = 1;

                            // Add the leaf as a child.
                            newNode.ChildNodes.Add(lessThanOrEqual, leaf);
                        }

                        // Deal with the root node.
                        // A tree should only have one root node.
                        if (newNode.Level == 0)
                        {
                            newNode.IsRoot = true;
                            if (_trees[count].DecidingAttribute == null)
                            {
                                _trees[count] = newNode;
                                _trees[count].LessThanOrEqual = lessThanOrEqual;
                            }
                            else
                            {
                                _trees[count].MergeNodes(newNode);
                                newNode = _trees[count];
                            }
                        }

                        // Deal with all other nodes.
                        else
                        {
                            // Add the node to its parent's list of children.
                            while (prevNode.Level != newNode.Level - 1)
                                prevNode = prevNode.Parent;

                            newNode.Parent = prevNode;
                            newNode.LessThanOrEqual = lessThanOrEqual;

                            // The case where the parent does not have any children yet.
                            if (prevNode.ChildNodes.Count == 0)
                                prevNode.ChildNodes.Add(prevNode.LessThanOrEqual, newNode);

                            // The case where the parent already has a child.
                            // The parent should only have one child now.
                            else
                            {
                                bool add = false;
                                foreach (TreeNode child in prevNode.ChildNodes.Values)
                                {
                                    // Modifying a child node.
                                    if ((child.ChildNodes.Count < 2) &&
                                        (newNode.Equals(child)))
                                    {
                                        child.MergeNodes(newNode);
                                        newNode = child;
                                    }

                                    // Adding a new child node.
                                    else if (prevNode.ChildNodes.Count < 2)
                                        add = true;
                                }
                                if (add == true)
                                    prevNode.ChildNodes.Add(prevNode.LessThanOrEqual, newNode);

                            }
                        }

                        // Keep track of the last node.
                        prevNode = newNode;
                        //Console.WriteLine(newNode.DecidingAttribute + " : " + newNode.Bar + " : " + prevNode.ChildNodes.Count);
                    }

                    // Starting a new tree.
                    if (line == "------------------")
                    {
                        _trees.Add(new TreeNode());
                        start = true;
                        count++;
                    }

                    // Get the tree's weight.
                    if (line.Contains("Weight:"))
                    {
                        start = false;
                        nodeString = line.Split(new string[] { ":" },
                                                StringSplitOptions.RemoveEmptyEntries);
                        _trees[count].Weight = System.Convert.ToSingle(nodeString[1].Trim());
                    }
                }
            }

            catch (Exception e)
            {
                Console.WriteLine("ERROR: Decision Tree could not be loaded.");
                Console.WriteLine(e);
            }
        }



        #endregion

        #region Helpers

        /// <summary>
        /// Helper function for loading a classifier.
        /// Counts the number of times a string occurs within a line.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static int countOccurances(string line, string target)
        {
            int count = 0;
            while (line.Contains(target))
            {
                count++;
                int index = line.IndexOf(target);
                line = line.Remove(index, target.Length);
            }

            return count;
        }

        #endregion

    }
}
