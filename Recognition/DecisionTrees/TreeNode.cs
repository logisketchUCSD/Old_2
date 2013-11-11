using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace DecisionTrees
{
    public class TreeNode
    {
        #region Constants

        private const int MAX_DISCRETE_VALUES = 8;
        private const double EPSILON = 1E-3;

        #endregion

        #region Member Variables

        /// <summary>
        /// Indicates whether the node is at the very top of the tree (has no parent)
        /// </summary>
        private bool _isRoot;

        /// <summary>
        /// Indicates whether the node is at the very bottom of the tree (has no children)
        /// </summary>
        private bool _isLeaf;

        /// <summary>
        /// Unique Id for the node
        /// </summary>
        private Guid _Id;

        /// <summary>
        /// Name of attribute that is used to decide what to do next
        /// </summary>
        private string _decidingAttribute;

        /// <summary>
        /// If this node is a leaf node, this will be the classification, otherwise
        /// it will be null
        /// </summary>
        private string _classification;

        /// <summary>
        /// Index of a value to its resulting action.
        /// If this node is a leaf node this object will be null.
        /// </summary>
        private Dictionary<object, TreeNode> _valueToChildren;

        /// <summary>
        /// The dividing bar value of a node. If a feature value is 
        /// greater than the bar value, proceed to one child node. If a 
        /// feature value is less than or equal to the bar value,
        /// proceed to the other child node.
        /// </summary>
        private object _bar;

        /// <summary>
        /// Whether the decision tree is discrete or continuous.
        /// </summary>
        private bool _isDiscrete;

        /// <summary>
        /// The type of the value. (int, double, float, etc).
        /// </summary>
        private TypeCode _type;

        /// <summary>
        /// Level of hierarchy on the tree.
        /// Root = zero. As you go down the tree, level increases.
        /// </summary>
        private int _level;

        /// <summary>
        /// Keeps track of its parent
        /// </summary>
        private TreeNode _parent;

        /// <summary>
        /// Keeps track of whether if it is a less than or equal
        /// </summary>
        private bool _lessThanOrEqual;

        /// <summary>
        /// The tree's weight.
        /// </summary>
        private float _weight;

        /// <summary>
        /// A leaf node's probability for a correct classification.
        /// This is only set for leaf nodes.
        /// </summary>
        private float _probability;

        #endregion

        #region Constructors

        /// <summary>
        /// Default Constructor
        /// </summary>
        public TreeNode()
        {
            // Do nothing
            _type = TypeCode.Double;
            ChildNodes = new Dictionary<object, TreeNode>();
        }

        #endregion

        #region Classification

        /// <summary>
        /// Get the classification of the Instance
        /// </summary>
        /// <param name="I">Instance with feature values</param>
        /// <returns>Classification of Instance I, with the probability.</returns>
        public KeyValuePair<string, float> Classify(Instance I)
        {
            string cls;
            float prob;

            if (_isLeaf)
            {
                cls = _classification;
                prob = _probability;
                return new KeyValuePair<string, float>(cls, prob);
            }

            object value = I.GetValue(_decidingAttribute);

            // Our decision trees are not discrete,
            // so this should skip to the else case.
            if (_isDiscrete)
            {
                if (_valueToChildren.ContainsKey(value))
                    return _valueToChildren[value].Classify(I);
                else
                {
                    cls = _classification;
                    prob = _probability;
                    return new KeyValuePair<string, float>(cls, prob);
                }
            }
            else
            {
                bool lessThanOrEqual = IsValueLessThanOrEqualToBar(value);
                return _valueToChildren[(object)lessThanOrEqual].Classify(I);
            }
        }

        /// <summary>
        /// Helper to determine whether a generic object is less than or equal
        /// to the bar that determines the split for this node.
        /// </summary>
        /// <param name="value">Generic value to compare to the cutoff</param>
        /// <returns>Is the value less than or equal to the bar</returns>
        private bool IsValueLessThanOrEqualToBar(object value)
        {
            switch (_type)
            {
                case TypeCode.Decimal:
                    return ((decimal)value <= (decimal)_bar);
                case TypeCode.Double:
                    if (value.GetType() != _bar.GetType())
                        value = General.ConvertType(value, TypeCode.Double);
                    return ((double)value <= (double)_bar);
                case TypeCode.Int16:
                    return ((short)value <= (short)_bar);
                case TypeCode.Int32:
                    return ((int)value <= (int)_bar);
                case TypeCode.Int64:
                    return ((long)value <= (long)_bar);
                case TypeCode.Single:
                    return ((float)value <= (float)_bar);
                case TypeCode.UInt16:
                    return ((ushort)value <= (ushort)_bar);
                case TypeCode.UInt32:
                    return ((uint)value <= (uint)_bar);
                case TypeCode.UInt64:
                    return ((ulong)value <= (ulong)_bar);
                default:
                    throw new Exception("Unknown Continuous Type");
            }
        }

        #endregion

        #region General Methods

        /// <summary>
        /// Checks if the input tree node is the same as the current node.
        /// Looks at the attribute and bar value.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool Equals(TreeNode node)
        {
            if ((node.DecidingAttribute == null) || (this.DecidingAttribute == null) ||
                (node.Bar == null) || (this.Bar == null))
                return false;

            if ((node.DecidingAttribute.Equals(this.DecidingAttribute)) &&
                (node.Bar.Equals(this.Bar)))
                return true;

            return false;
        }

        /// <summary>
        /// Merges a node into the current node.
        /// Adds the node's children to this current node.
        /// </summary>
        /// <param name="node"></param>
        public void MergeNodes(TreeNode node)
        {
            this.LessThanOrEqual = node.LessThanOrEqual;
            foreach (object key in node.ChildNodes.Keys)
                this.ChildNodes.Add(key, node.ChildNodes[key]);
        }

        #endregion

        #region Getters/Setters

        /// <summary>
        /// Gets and sets the node's deciding attribute
        /// </summary>
        public string DecidingAttribute
        {
            get { return _decidingAttribute; }
            set { _decidingAttribute = value; }
        }

        /// <summary>
        /// Gets and sets the bar split value for this node
        /// </summary>
        public object Bar
        {
            get { return _bar; }
            set { _bar = value; }
        }

        /// <summary>
        /// Gets and sets the node's children
        /// </summary>
        public Dictionary<object, TreeNode> ChildNodes
        {
            get { return _valueToChildren; }
            set { _valueToChildren = value; }
        }

        /// <summary>
        /// Gets and sets whether the node is a root
        /// </summary>
        public bool IsRoot
        {
            get { return _isRoot; }
            set { _isRoot = value; }
        }

        /// <summary>
        /// Gets and sets whether the node is a leaf
        /// </summary>
        public bool IsLeaf
        {
            get { return _isLeaf; }
            set { _isLeaf = value; }
        }

        /// <summary>
        /// Gets the node's ID
        /// </summary>
        public Guid ID
        {
            get 
            {
                if (_Id == null)
                    _Id = Guid.NewGuid();
                return _Id; 
            }
        }

        /// <summary>
        /// Gets and sets the node's classification (if any).
        /// </summary>
        public string Classification
        {
            get { return _classification; }
            set { _classification = value; }
        }

        /// <summary>
        /// Gets and sets whether the node is discrete or continuous.
        /// </summary>
        public bool IsDiscrete
        {
            get { return _isDiscrete; }
            set { _isDiscrete = value; }
        }

        /// <summary>
        /// Gets and sets the node type (double, int, float, etc).
        /// </summary>
        public TypeCode Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// Gets and sets the level of hierarchy of the node.
        /// </summary>
        public int Level
        {
            get { return _level; }
            set { _level = value; }
        }

        /// <summary>
        /// Gets and sets the parent of the node
        /// </summary>
        public TreeNode Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        /// <summary>
        /// Gets and sets whether it has a ">" or "<="
        /// </summary>
        public bool LessThanOrEqual
        {
            get { return _lessThanOrEqual; }
            set { _lessThanOrEqual = value; }
        }

        /// <summary>
        /// Gets and sets the tree's weight.
        /// </summary>
        public float Weight
        {
            get { return _weight; }
            set { _weight = value; }
        }

        /// <summary>
        /// Gets and sets the node's probability of classification.
        /// </summary>
        public float Probability
        {
            get { return _probability; }
            set { _probability = value; }
        }

        #endregion

        #region Unused Code

        #region Constructors

        ///// <summary>
        ///// Constructor which starts 
        ///// </summary>
        ///// <param name="trainingInstances"></param>
        //public TreeNode(List<Instance> trainingInstances, bool isRoot)
        //    : this(trainingInstances, isRoot, 3)
        //{
        //}

        //public TreeNode(List<Instance> trainingInstances, bool isRoot, int minNumObjectsPerNode)
        //{
        //    _Id = Guid.NewGuid();
        //    _isRoot = isRoot;
        //    _isLeaf = false;
        //    _classification = null;
        //    _minNumObjects = minNumObjectsPerNode;

        //    BuildTree(trainingInstances);
        //}

        #endregion

        #region Tree Building

        ///// <summary>
        ///// Builds the tree based on the training instances supplied
        ///// </summary>
        ///// <param name="trainingInstances">instances to use in training</param>
        //private void BuildTree(List<Instance> trainingInstances)
        //{
        //    // ***** Count the instances to see what the class distribution is ***** //
        //    Dictionary<string, int> classCounts = new Dictionary<string, int>();
        //    foreach (Instance I in trainingInstances)
        //    {
        //        string cls = I.Classification;
        //        if (!classCounts.ContainsKey(cls))
        //            classCounts.Add(cls, 0);

        //        classCounts[cls]++;
        //    }

        //    string bestClass = null;
        //    int numBest = -1;
        //    foreach (KeyValuePair<string, int> kvp in classCounts)
        //    {
        //        if (kvp.Value > numBest)
        //        {
        //            bestClass = kvp.Key;
        //            numBest = kvp.Value;
        //        }
        //    }
        //    _classification = bestClass;
        //    // ***** End instance distribution counting ***** //


        //    // ***** Examine stopping cases ***** //
        //    if (classCounts.Count == 0 || trainingInstances.Count == 0)
        //    {
        //        // Shouldn't get here, but if for some reason there are no training
        //        // instances - stop building the tree

        //        throw new Exception("Error in building tree - no training instances for this node");
        //    }
        //    else if (classCounts.Count == 1)
        //    {
        //        // All training instances are of the same class
        //        // This node's classification is this class

        //        _classification = trainingInstances[0].Classification;
        //        _isLeaf = true;
        //        return;
        //    }
        //    else if (trainingInstances.Count < 2 * _minNumObjects)
        //    {
        //        // Not enough instances to split the node
        //        // Most frequent class is made the node's classification

        //        _isLeaf = true;
        //        _classification = bestClass;
        //        return;
        //    }
        //    // ***** End Stopping cases section ***** //


        //    // ***** Find Information and Gain for each attribute ***** //
        //    List<string> attributes = trainingInstances[0].AttributeNames;
        //    if (attributes.Count == 0)
        //        throw new Exception("No attributes - unable to calculate gain/info for splitting the node.");

        //    Dictionary<string, double> attributeToGain = new Dictionary<string, double>(attributes.Count);
        //    Dictionary<string, double> attributeToInfo = new Dictionary<string, double>(attributes.Count);
        //    Dictionary<string, bool> attributeIsDiscrete = new Dictionary<string, bool>(attributes.Count);
        //    Dictionary<string, object> attributeToBar = new Dictionary<string, object>(attributes.Count);
        //    Dictionary<string, TypeCode> attributeToType = new Dictionary<string, TypeCode>(attributes.Count);

        //    foreach (string att in attributes)
        //    {
        //        double info;
        //        bool isDiscrete;
        //        object bar;
        //        TypeCode type;
        //        double gain = GetInfoAndGain(trainingInstances, att, out info, out isDiscrete, out bar, out type);
        //        attributeToGain.Add(att, gain);
        //        attributeToInfo.Add(att, info);
        //        attributeIsDiscrete.Add(att, isDiscrete);
        //        attributeToBar.Add(att, bar);
        //        attributeToType.Add(att, type);
        //    }
        //    // ***** End Information and Gain computation ***** //


        //    // ***** Decide which attribute is best to split ***** //
        //    // Attribute with highest "worth" will be chosen as the splitting attribute

        //    // First need the average gain for all attributes
        //    double avgGain = 0.0;
        //    foreach (double g in attributeToGain.Values)
        //        avgGain += g;

        //    avgGain /= attributeToGain.Count;

        //    // Now calculate worth for each attribute
        //    double bestWorth = -EPSILON;
        //    string bestAttribute = null;
        //    bool bestAttIsDiscrete = false;
        //    TypeCode bestType = TypeCode.Empty;

        //    foreach (string att in attributes)
        //    {
        //        double worth = GetWorth(attributeToInfo[att], attributeToGain[att], avgGain);
        //        if (worth > bestWorth)
        //        {
        //            bestWorth = worth;
        //            bestAttribute = att;
        //            bestAttIsDiscrete = attributeIsDiscrete[bestAttribute];
        //            bestType = attributeToType[bestAttribute];
        //        }
        //    }

        //    // No information gain --> create leaf node
        //    if (bestAttribute == null)
        //    {
        //        _classification = bestClass;
        //        _isLeaf = true;
        //        return;
        //    }

        //    _decidingAttribute = bestAttribute;

        //    // ***** End splitting selection ***** //


        //    // ***** Recurse on remaining data for each split (each branch) ***** //

        //    _valueToChildren = new Dictionary<object, TreeNode>();

        //    if (bestAttIsDiscrete)
        //    {
        //        _isDiscrete = true;
        //        _type = bestType;

        //        Dictionary<object, List<Instance>> valueToInstances = new Dictionary<object, List<Instance>>();

        //        foreach (Instance I in trainingInstances)
        //        {
        //            object o = I.GetValue(bestAttribute);
        //            if (!valueToInstances.ContainsKey(o))
        //                valueToInstances.Add(o, new List<Instance>());

        //            valueToInstances[o].Add(I);
        //        }

        //        foreach (KeyValuePair<object, List<Instance>> kvp in valueToInstances)
        //        {
        //            TreeNode child = new TreeNode(kvp.Value, false, _minNumObjects);
        //            _valueToChildren.Add(kvp.Key, child);
        //        }
        //    }
        //    else
        //    {
        //        // Create children when best attribute is continuous
        //        _isDiscrete = false;
        //        _type = bestType;
        //        _bar = attributeToBar[bestAttribute];

        //        Dictionary<object, List<Instance>> valueToInstances = new Dictionary<object, List<Instance>>();

        //        foreach (Instance I in trainingInstances)
        //        {
        //            object o = I.GetValue(bestAttribute);
        //            bool isLess = IsValueLessThanOrEqualToBar(o);
        //            if (!valueToInstances.ContainsKey((object)isLess))
        //                valueToInstances.Add(isLess, new List<Instance>());

        //            valueToInstances[isLess].Add(I);
        //        }

        //        foreach (KeyValuePair<object, List<Instance>> kvp in valueToInstances)
        //        {
        //            TreeNode child = new TreeNode(kvp.Value, false, _minNumObjects);
        //            _valueToChildren.Add(kvp.Key, child);
        //        }
        //    }


        //    // ***** End tree building ***** //


        //    // ***** Is this a good split to make? ***** //
        //    // Check to see if this splitting gives us better results than simply taking 
        //    // the most frequent class and making this a leaf node with that classification

        //    double naiveRate = (double)numBest / trainingInstances.Count;
        //    int numCorrect = 0;
        //    foreach (Instance I in trainingInstances)
        //    {
        //        string cls = this.Classify(I).Key;
        //        if (cls == I.Classification)
        //            numCorrect++;
        //    }
        //    double currentRate = (double)numCorrect / trainingInstances.Count;

        //    if (naiveRate > currentRate)
        //    {
        //        _classification = bestClass;
        //        _isLeaf = true;
        //        _valueToChildren = null;
        //        return;
        //    }

        //    // ***** End goodness check ***** //
        //}


        ///// <summary>
        ///// Calculates the information and gain in a series of values (all values for a given attribute)
        ///// Automatically detects standard value types.
        ///// </summary>
        ///// <param name="trainingInstances">All instances which may contain relevant values</param>
        ///// <param name="att">Attribute to select values for</param>
        ///// <param name="info">Information in the series of values</param>
        ///// <param name="isDiscrete">Value indicating whether the attribute is discrete or continuous</param>
        ///// <param name="bar">The cutoff value if the attribute is continuous</param>
        ///// <param name="t">Type of value (e.g. string, int, double)</param>
        ///// <returns>Gain for the series of values</returns>
        //private double GetInfoAndGain(List<Instance> trainingInstances, string att,
        //    out double info, out bool isDiscrete, out object bar, out TypeCode t)
        //{
        //    t = TypeCode.Empty;
        //    bool different = false;
        //    //List<KeyValuePair<object, string>> valuesToClasses = new List<KeyValuePair<object, string>>(trainingInstances.Count);
        //    //Dictionary<KeyValuePair<object, string>, double> weights = new Dictionary<KeyValuePair<object, string>, double>(trainingInstances.Count);
        //    foreach (Instance I in trainingInstances)
        //    {
        //        object o = I.GetValue(att);
        //        if (o is string && (string)o == "?")
        //        {
        //            //valuesToClasses.Add(new KeyValuePair<object, string>(o, I.Classification));
        //            continue;
        //        }

        //        TypeCode type = General.GetTypeCode(o);

        //        if (t == TypeCode.Empty)
        //            t = type;
        //        else if (type != t || type == TypeCode.Empty)
        //        {
        //            t = General.GetMoreGeneralType(t, type);
        //            different = true;
        //            //t = TypeCode.String;
        //            //break;
        //            //throw new Exception("TreeNode: All values for an attribute must have the same (non-null) type");
        //        }

        //        //KeyValuePair<object, string> kvp = new KeyValuePair<object, string>(o, I.Classification);
        //        //valuesToClasses.Add(kvp);
        //        //weights.Add(kvp, I.Weight);
        //    }

        //    if (different)
        //    {
        //        foreach (Instance I in trainingInstances)
        //            I.SetType(att, t);
        //    }

        //    return GetInfoAndGain(trainingInstances, att, t, out info, out isDiscrete, out bar);
        //    //return GetInfoAndGain(valuesToClasses, weights, t, out info, out isDiscrete, out bar);
        //}


        ///// <summary>
        ///// Calculates the information and gain in the values.
        ///// </summary>
        ///// <param name="instances">All instances which may contain relevant values</param>
        ///// <param name="att">Attribute to select values for</param>
        ///// <param name="t">Type of value (e.g. string, int, double)</param>
        ///// <param name="info">Information in the values</param>
        ///// <param name="isDiscrete">Value indicating whether the attribute is discrete or continuous</param>
        ///// <param name="bar">The cutoff value if the attribute is continuous</param>
        ///// <returns>Gain of the values</returns>
        //private double GetInfoAndGain(List<Instance> instances, string att, TypeCode t,
        //    out double info, out bool isDiscrete, out object bar)
        //{
        //    info = 0.0;
        //    bar = new object();
        //    Dictionary<object, double> valueFreq;
        //    Dictionary<object, Dictionary<string, double>> freq = CountDiscreteFrequency(instances, att, out valueFreq);
        //    int numDiscreteValues = freq.Count;

        //    switch (t)
        //    {
        //        case TypeCode.Boolean:
        //            isDiscrete = true;
        //            return GetInfoAndGainDiscrete(instances, att, t, freq, valueFreq, ref info);

        //        case TypeCode.Byte:
        //            throw new Exception("This value type has not been added.");

        //        case TypeCode.Char:
        //            isDiscrete = true;
        //            return GetInfoAndGainDiscrete(instances, att, t, freq, valueFreq, ref info);

        //        case TypeCode.DateTime:
        //            throw new Exception("This value type has not been added.");

        //        case TypeCode.Decimal:
        //            isDiscrete = false;
        //            return GetInfoAndGainContinuous(instances, att, t, ref info, out bar);

        //        case TypeCode.Double:
        //            isDiscrete = false;
        //            return GetInfoAndGainContinuous(instances, att, t, ref info, out bar);

        //        case TypeCode.Int16:
        //            numDiscreteValues = freq.Count;
        //            if (numDiscreteValues < MAX_DISCRETE_VALUES)
        //            {
        //                isDiscrete = true;
        //                return GetInfoAndGainDiscrete(instances, att, t, freq, valueFreq, ref info);
        //            }
        //            else
        //            {
        //                isDiscrete = false;
        //                return GetInfoAndGainContinuous(instances, att, t, ref info, out bar);
        //            }

        //        case TypeCode.Int32:
        //            numDiscreteValues = freq.Count;
        //            if (numDiscreteValues < MAX_DISCRETE_VALUES)
        //            {
        //                isDiscrete = true;
        //                return GetInfoAndGainDiscrete(instances, att, t, freq, valueFreq, ref info);
        //            }
        //            else
        //            {
        //                isDiscrete = false;
        //                return GetInfoAndGainContinuous(instances, att, t, ref info, out bar);
        //            }

        //        case TypeCode.Int64:
        //            numDiscreteValues = freq.Count;
        //            if (numDiscreteValues < MAX_DISCRETE_VALUES)
        //            {
        //                isDiscrete = true;
        //                return GetInfoAndGainDiscrete(instances, att, t, freq, valueFreq, ref info);
        //            }
        //            else
        //            {
        //                isDiscrete = false;
        //                return GetInfoAndGainContinuous(instances, att, t, ref info, out bar);
        //            }

        //        case TypeCode.Single:
        //            isDiscrete = false;
        //            return GetInfoAndGainContinuous(instances, att, t, ref info, out bar);

        //        case TypeCode.String:
        //            isDiscrete = true;
        //            return GetInfoAndGainDiscrete(instances, att, t, freq, valueFreq, ref info);

        //        case TypeCode.UInt16:
        //            numDiscreteValues = freq.Count;
        //            if (numDiscreteValues < MAX_DISCRETE_VALUES)
        //            {
        //                isDiscrete = true;
        //                return GetInfoAndGainDiscrete(instances, att, t, freq, valueFreq, ref info);
        //            }
        //            else
        //            {
        //                isDiscrete = false;
        //                return GetInfoAndGainContinuous(instances, att, t, ref info, out bar);
        //            }

        //        case TypeCode.UInt32:
        //            numDiscreteValues = freq.Count;
        //            if (numDiscreteValues < MAX_DISCRETE_VALUES)
        //            {
        //                isDiscrete = true;
        //                return GetInfoAndGainDiscrete(instances, att, t, freq, valueFreq, ref info);
        //            }
        //            else
        //            {
        //                isDiscrete = false;
        //                return GetInfoAndGainContinuous(instances, att, t, ref info, out bar);
        //            }

        //        case TypeCode.UInt64:
        //            numDiscreteValues = freq.Count;
        //            if (numDiscreteValues < MAX_DISCRETE_VALUES)
        //            {
        //                isDiscrete = true;
        //                return GetInfoAndGainDiscrete(instances, att, t, freq, valueFreq, ref info);
        //            }
        //            else
        //            {
        //                isDiscrete = false;
        //                return GetInfoAndGainContinuous(instances, att, t, ref info, out bar);
        //            }

        //        default:
        //            throw new Exception("Unknown type");
        //    }
        //}


        ///// <summary>
        ///// Counts the instances to create a frequency distribution (for discrete values)
        ///// </summary>
        ///// <param name="instances">All instances which may contain relevant values</param>
        ///// <param name="att">Attribute to select values for</param>
        ///// <param name="valueFrequencies">Frequency distribution irrespective of class</param>
        ///// <returns>Frequency Distribution according to value and class</returns>
        //private Dictionary<object, Dictionary<string, double>> CountDiscreteFrequency(List<Instance> instances,
        //    string att, out Dictionary<object, double> valueFrequencies)
        //{
        //    Dictionary<object, Dictionary<string, double>> count = new Dictionary<object, Dictionary<string, double>>();
        //    valueFrequencies = new Dictionary<object, double>();
        //    foreach (Instance I in instances)
        //    {
        //        object o = I.GetValue(att);
        //        string cls = I.Classification;
        //        double weight = I.Weight;

        //        if (!count.ContainsKey(o))
        //            count.Add(o, new Dictionary<string, double>());

        //        if (!count[o].ContainsKey(cls))
        //            count[o].Add(cls, 0);

        //        count[o][cls] += weight;

        //        if (!valueFrequencies.ContainsKey(o))
        //            valueFrequencies.Add(o, 0);

        //        valueFrequencies[o] += weight;
        //    }

        //    return count;
        //}


        ///// <summary>
        ///// Calculates the information and gain in a series of discrete values
        ///// </summary>
        ///// <param name="instances">All instances which may contain relevant values</param>
        ///// <param name="att">Attribute to select values for</param>
        ///// <param name="t">Type of value (e.g. string, int, double)</param>
        ///// <param name="freq">Frequency Distribution according to value and class</param>
        ///// <param name="valueFreq">Frequency distribution irrespective of class</param>
        ///// <param name="info">Information in the values</param>
        ///// <returns>Gain for the series of values</returns>
        //private double GetInfoAndGainDiscrete(List<Instance> instances, string att, TypeCode t,
        //    Dictionary<object, Dictionary<string, double>> freq, Dictionary<object, double> valueFreq,
        //    ref double info)
        //{
        //    // Known items
        //    double totalItems = 0.0;
        //    foreach (double d in valueFreq.Values)
        //        totalItems += d;

        //    double knownItems = totalItems;

        //    if (valueFreq.ContainsKey((object)"?"))
        //        knownItems -= valueFreq[(object)"?"];

        //    if (knownItems <= 0.0)
        //    {
        //        info = 0.0;
        //        return -EPSILON;
        //    }

        //    double unknownRate = 1.0 - (knownItems / totalItems);

        //    // Compute the information in the series of values
        //    info = TotalInfo(new List<double>(valueFreq.Values)) / totalItems;

        //    // Check to make sure there are at least 2 values with MinNumObjects per value
        //    int goodValue = 0;
        //    foreach (double count in valueFreq.Values)
        //        if (count >= _minNumObjects)
        //            goodValue++;

        //    if (goodValue < 2)
        //        return -EPSILON;

        //    // Compute the base info
        //    Dictionary<string, double> classCounts = new Dictionary<string, double>();
        //    foreach (Dictionary<string, double> dic in freq.Values)
        //    {
        //        foreach (KeyValuePair<string, double> kvp in dic)
        //        {
        //            if (!classCounts.ContainsKey(kvp.Key))
        //                classCounts.Add(kvp.Key, 0.0);

        //            classCounts[kvp.Key] += kvp.Value;
        //        }
        //    }

        //    double baseInfo = TotalInfo(new List<double>(classCounts.Values)) / knownItems;

        //    // Compute the gain
        //    List<List<double>> frequencies = new List<List<double>>();
        //    foreach (KeyValuePair<object, double> kvp in valueFreq)
        //    {
        //        if (kvp.Key is string && (string)kvp.Key == "?")
        //            continue;

        //        frequencies.Add(new List<double>(freq[kvp.Key].Values));
        //    }

        //    double gain = ComputeGain(baseInfo, unknownRate, frequencies, totalItems);

        //    return gain;
        //}


        ///// <summary>
        ///// Gets the numerical amount of information stored in a list of values
        ///// </summary>
        ///// <param name="values"></param>
        ///// <returns>Total information in the list of values</returns>
        //private double TotalInfo(List<double> values)
        //{
        //    double sum = 0.0;
        //    int totalItems = 0;
        //    foreach (int count in values)
        //    {
        //        if (count > 0)
        //        {
        //            sum += count * Math.Log(count);
        //            totalItems += count;
        //        }
        //    }

        //    return (totalItems * Math.Log(totalItems) - sum);
        //}


        ///// <summary>
        ///// Finds the information gain based on the information available and the frequency distribution
        ///// </summary>
        ///// <param name="BaseInfo">Information available</param>
        ///// <param name="UnknownRate">Value between 0.0 and 1.0 indicating the percentage of cases 
        ///// that have an unknown value (weighted)</param>
        ///// <param name="frequenciesOfClassesPerValue"></param>
        ///// <param name="TotalItems">Total number of items that are beign used for gain calculation (weighted)</param>
        ///// <returns></returns>
        //private double ComputeGain(double BaseInfo, double UnknownRate,
        //    List<List<double>> frequenciesOfClassesPerValue, double TotalItems)
        //{
        //    // Compute the gain
        //    double CurrentInfo = 0.0;

        //    foreach (List<double> valueFreq in frequenciesOfClassesPerValue)
        //        CurrentInfo += TotalInfo(valueFreq);

        //    double gain = (1.0 - UnknownRate) * (BaseInfo - CurrentInfo / TotalItems);

        //    return gain;
        //}


        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="instances"></param>
        ///// <param name="att"></param>
        ///// <param name="t"></param>
        ///// <param name="info"></param>
        ///// <param name="bar"></param>
        ///// <returns></returns>
        //private double GetInfoAndGainContinuous(List<Instance> instances, string att, TypeCode t, ref double info, out object bar)
        //{
        //    List<double> ValFreq = new List<double>(3);
        //    for (int i = 0; i < 3; i++)
        //        ValFreq.Add(0.0);

        //    double weightedKnowns = 0.0;
        //    double weightedUnknowns = 0.0;

        //    // Known items
        //    List<Instance> knowns = new List<Instance>();
        //    List<Instance> unknowns = new List<Instance>();
        //    foreach (Instance I in instances)
        //    {
        //        double weight = I.Weight;
        //        if (I.GetValue(att) == (object)"?")
        //        {
        //            weightedUnknowns += weight;
        //            unknowns.Add(I);
        //        }
        //        else
        //        {
        //            weightedKnowns += weight;
        //            knowns.Add(I);
        //        }
        //    }

        //    double unknownRate = 1.0 - (weightedKnowns / (weightedKnowns + weightedUnknowns));

        //    foreach (Instance unknown in unknowns)
        //    {
        //        double weight = unknown.Weight;
        //        ValFreq[0] += weight;
        //    }

        //    // Sort the list to make split points easier
        //    knowns.Sort(new InstanceSorter(att));

        //    // Get the frequencies and base info
        //    Dictionary<string, double> Freq1 = new Dictionary<string, double>();
        //    Dictionary<string, double> Freq2 = new Dictionary<string, double>();
        //    Dictionary<Instance, double> SplitGain = new Dictionary<Instance, double>();
        //    Dictionary<Instance, double> SplitInfo = new Dictionary<Instance, double>();

        //    foreach (Instance known in knowns)
        //    {
        //        double weight = known.Weight;

        //        if (!Freq2.ContainsKey(known.Classification))
        //            Freq2.Add(known.Classification, 0.0);

        //        Freq2[known.Classification] += weight;
        //        SplitGain.Add(known, -EPSILON);
        //        SplitInfo.Add(known, 0.0);
        //    }

        //    if (Freq2.Count == 0 || knowns.Count == 0)
        //    {
        //        bar = new object();
        //        return 0.0;
        //    }

        //    double BaseInfo = TotalInfo(new List<double>(Freq2.Values)) / weightedKnowns;

        //    // Don't want to split too small or too large
        //    int MinSplit = (int)(0.10 * weightedKnowns / Freq2.Count);
        //    if (MinSplit <= _minNumObjects)
        //        MinSplit = _minNumObjects;
        //    else if (MinSplit > 25)
        //        MinSplit = 25;

        //    // Find the split
        //    double AvgGain = 0.0;
        //    double LowItems = 0.0;
        //    int numTries = 0;
        //    for (int i = 0; i < knowns.Count; i++)
        //    {
        //        double weight = knowns[i].Weight;
        //        LowItems += weight;

        //        string cls = knowns[i].Classification;
        //        if (!Freq1.ContainsKey(cls))
        //            Freq1.Add(cls, 0.0);

        //        Freq1[cls] += weight;
        //        Freq2[cls] -= weight;

        //        if (LowItems < MinSplit)
        //            continue;
        //        else if (LowItems > knowns.Count - MinSplit)
        //            break;

        //        object v0 = knowns[i].GetValue(att);
        //        object v1 = knowns[i + 1].GetValue(att);

        //        if (IsValueLessThan(v0, v1))
        //        {
        //            ValFreq[1] = LowItems;
        //            ValFreq[2] = weightedKnowns - LowItems;

        //            // Check to make sure there are at least 2 values with MinNumObjects per value
        //            int goodValue = 0;
        //            for (int j = 1; j < 3; j++)
        //                if (ValFreq[j] >= _minNumObjects)
        //                    goodValue++;

        //            if (goodValue < 2)
        //            {
        //                bar = new object();
        //                return -EPSILON;
        //            }

        //            List<List<double>> frequencies = new List<List<double>>();
        //            frequencies.Add(new List<double>(Freq1.Values));
        //            frequencies.Add(new List<double>(Freq2.Values));

        //            SplitGain[knowns[i]] = ComputeGain(BaseInfo, unknownRate, frequencies, weightedKnowns);
        //            SplitInfo[knowns[i]] = TotalInfo(ValFreq);
        //            AvgGain += SplitGain[knowns[i]];
        //            numTries++;
        //        }
        //    }

        //    double ThreshCost = Math.Log(numTries) / (weightedKnowns + weightedUnknowns);

        //    double BestVal = 0.0;
        //    Instance BestIndex = null;
        //    foreach (Instance known in knowns)
        //    {
        //        double value = SplitGain[known] - ThreshCost;
        //        if (value > BestVal)
        //        {
        //            BestIndex = known;
        //            BestVal = value;
        //        }
        //    }

        //    if (BestIndex != null)
        //    {
        //        info = SplitInfo[BestIndex];
        //        bar = BestIndex.GetValue(att);
        //        return BestVal;
        //    }
        //    else
        //    {
        //        bar = new object();
        //        info = 0.0;
        //        return -EPSILON;
        //    }
        //}


        //private bool IsValueLessThan(object v0, object v1)
        //{
        //    if (v0 is int && v1 is int)
        //        return ((int)v0 < (int)v1);
        //    else if (v0 is double && v1 is double)
        //        return ((double)v0 < (double)v1 - 1e-5);
        //    else if (v0 is float && v1 is float)
        //        return ((float)v0 < (float)v1 - 1e-5);
        //    else if (v0 is decimal && v1 is decimal)
        //        return ((decimal)v0 < (decimal)v1);
        //    if (v0 is short && v1 is short)
        //        return ((short)v0 < (short)v1);
        //    if (v0 is long && v1 is long)
        //        return ((long)v0 < (long)v1);
        //    if (v0 is ushort && v1 is ushort)
        //        return ((ushort)v0 < (ushort)v1);
        //    if (v0 is uint && v1 is uint)
        //        return ((uint)v0 < (uint)v1);
        //    if (v0 is ulong && v1 is ulong)
        //        return ((ulong)v0 < (ulong)v1);

        //    return true;
        //}


        ///// <summary>
        ///// Gets a measure of the attribute's "worth" based on how much information
        ///// and gain there is for this attribute. 
        ///// </summary>
        ///// <param name="info">Information stored in the values</param>
        ///// <param name="gain">Gain in the values</param>
        ///// <param name="minGain">Minimum gain to be considered worthy (is the average gain for all attributes)</param>
        ///// <returns>Worth of the attribute</returns>
        //private double GetWorth(double info, double gain, double minGain)
        //{
        //    if (gain >= minGain - EPSILON && info > EPSILON)
        //        return gain / info;
        //    else
        //        return -EPSILON;
        //}

        #endregion

        #endregion
    }
}
