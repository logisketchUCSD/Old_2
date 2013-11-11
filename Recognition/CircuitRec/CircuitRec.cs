/**
 * File: CircuitRec.cs
 *
 * Authors: Matthew Weiner, Howard Chen, and Sam Gordon
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2007.
 * 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Sketch;
using System.Windows.Forms;
using NeuralNets;
using Microsoft.Ink;
using System.Diagnostics;

namespace CircuitRec
{
    /// <summary>
    /// The CircuitRec class is the main class used to recognize the circuits.  It creates all
    /// of the wires, symbols, labels, meshes, endpoints, and the circuit.  It is the class that
    /// utilitizes all of the other classes in the project.
    /// </summary>
    public class CircuitRec
    {
        #region Internals

        /// <summary>
        /// Cicuit information is printed when this is set to true.
        /// </summary>
        private const bool debug = false;

        /// <summary>
        /// The base value in inkspace from which the endpoint algorithm starts looking to combine endpoints.
        /// </summary>
        private const double SUBSTROKE_MARGIN = 1;

        /// <summary>
        /// Margin Value for determining Symbol Connections
        /// </summary>
        private const double MARGIN_SYMBOL_CONN = 0.15;

        /// <summary>
        /// An empirically determined number used to divide the average wire endpoint distance to other wires.  Used for
        /// wire to wire connections.
        /// </summary>
        private const double THRESHOLD_DIVIDER = 2.5;

        /// <summary>
        /// The distance in inkspace of the maximum distance a label can be away and still be associated with a wire.
        /// </summary>
        private const double LABEL_DIST_THRESHOLD = 800;

        /// <summary>
        /// The coarseness with which to iterate through the label points when associating labels with wires.
        /// </summary>
        private const int LABEL_COARSENESS = 3;

        /// <summary>
        /// Threshold for determining wire connections (it is the maximum distance from the endpoint of a wire to any other wire.
        /// The amount the maxdistwire is divided by is emperically determined
        /// </summary>
        private double marginWireConn;

        /// <summary>
        /// Holds the extreme coordinates for all Wires and Symbols in the sketch.
        /// </summary>
        private BoundingBox circuit;

        /// <summary>
        /// A List of all the wires in the current sketch.
        /// </summary>
        private List<Wire> wires;

        /// <summary>
        /// A List of all the SimpleSymbls and ComplexSymbols in the current sketch.
        /// </summary>
        private List<BaseSymbol> symbols;

        /// <summary>
        /// A List of all the labels in the current sketch.
        /// </summary>
        private List<Shape> labels;

        /// <summary>
        /// A list of all of the shapes in the current sketch
        /// </summary>
        private List<Shape> circuitShapes;

        /// <summary>
        /// A dictionary with the current domain's connection information.
        /// </summary>
        private Domain domain;

        /// <summary>
        /// The WordList used for text recognition of labels.
        /// </summary>
        private WordList wordList;

        /// <summary>
        /// The current sketch object.
        /// </summary>
        private Sketch.Sketch sketch;

        /// <summary>
        /// The normalization parameters for the neural net
        /// </summary>
        private double[] normParams;

        /// <summary>
        /// A List of all the Meshes (meshes/connected wires) in the current sketch.
        /// </summary>
        private List<Mesh> meshes;

        /// <summary>
        /// A List of errors that occured during the circuit recognition.
        /// </summary>
        private List<ParseError> errors;

        /// <summary>
        /// A mapping of wires to the Mesh that they belong to
        /// </summary>
        private Dictionary<Substroke, Mesh> substrokeToWire;

        /// <summary>
        /// A mapping of substrokes to circuit objects.
        /// </summary>
        private Dictionary<Guid?, object> outputDict;

        /// <summary>
        /// A dictionary to keep track of each shape in the sketch,
        /// using their unique shape names as keys.
        /// </summary>
        private Dictionary<string, Shape> shapeNames;

        /// <summary>
        /// A list that keeps track of the names of shapes with errors.
        /// </summary>
        private List<Shape> circuitErrors;

        /// <summary>
        /// Counts for keeping track of the number of each type of shape
        /// </summary>
        private int WireCount;
        private int SimpleSymbolCount;
        private int ComplexSymbolCount;

        /// <summary>
        /// Information about the circuit structure.
        /// Specifically, a dictionary with the symbols' names as keys, and pairs 
        /// containing the symbols' types and their inputs as values.
        /// </summary>
        private Dictionary<string, KeyValuePair<string, List<string>>> circuitStructure;
        private List<KeyValuePair<string, string>> circuitOutputs;
        private List<string> circuitInputs;

        #endregion

        #region Constructor

        /// <summary>
        /// Default Constructor
        /// </summary>
        public CircuitRec(string dir)
        {
            wires = new List<Wire>();
            meshes = new List<Mesh>();
            symbols = new List<BaseSymbol>();
            labels = new List<Shape>();
            errors = new List<ParseError>();
            substrokeToWire = new Dictionary<Substroke, Mesh>();
            circuitErrors = new List<Shape>();

            // If the wordlist passed in is null, load the default wordlist
            this.wordList = TextRecognition.TextRecognition.createLabelWordList();
            dir += @"..\..\digital_domain.txt";
            this.domain = new Domain(dir);
        }
        
        /// <summary>
        /// Constructor for CircuitRec.
        /// </summary>
        /// <param name="curdomain">The Domain object for the current domain.</param>
        public CircuitRec(Domain curdomain, WordList wordList)
        {
            wires = new List<Wire>();
            meshes = new List<Mesh>();
            symbols = new List<BaseSymbol>();
            labels = new List<Shape>();
            errors = new List<ParseError>();
            substrokeToWire = new Dictionary<Substroke, Mesh>();
            circuitErrors = new List<Shape>();

            // If the wordlist passed in is null, load the default wordlist
            if (wordList == null)
                this.wordList = TextRecognition.TextRecognition.createLabelWordList();
            else
                this.wordList = wordList;
            if (curdomain == null)
                this.domain = new Domain(Application.ExecutablePath + @"..\..\..\digital_domain.txt");
            else
                this.domain = curdomain;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Runs the different methods of the circuit recognition algorithm.  Call
        /// this after making a CircuitRec object to recognize the labeled circuit.
        /// </summary>
        /// <param name="sketch">A sketch object that is labeled.</param>
        /// <returns>A boolean indicating whether the circuit is valid for simulation.</returns>
        public bool Run(Sketch.Sketch sketch)
        {
            this.sketch = sketch;

            // Throw an exception if there is no sketch.
            if (this.sketch == null)
            {
                throw new ParseError(null,"The sketch is empty.");
            }

            parseCircuit();
            ioDetermination();
            makeMeshes();
            organizeResults();
            if (debug) printDictionaryEtc();
            return checkResults();
        }

        #region Recognize Wires / Meshes / Shapes

        /// <summary>
        /// The Main body of CircuitRec, this takes a FeatureSketch, recognizes the wires and determines how the circuit connects together, and
        /// then returns a feature sketch that has updated probabilities
        /// </summary>
        /// <param name="sketch">FeatureSketch to recognize</param>
        /// <returns>Recognized FeatureSketch</returns>
        public Featurefy.FeatureSketch RecognizeWires(Featurefy.FeatureSketch sketch)
        {
            this.sketch = sketch.Sketch;

            // Throw an exception if there is no sketch.
            if (this.sketch == null)
            {
                throw new ParseError(null, "The sketch is empty.");
            }

            parseCircuit();
            ioDetermination();
            makeMeshes();
            Sketch.Sketch modifiedSketch = new Sketch.Sketch();
            foreach (Sketch.Shape s in this.sketch.ShapesL)
            {
                if (s.Type != "Wire" && s.Type != "Mesh")
                {
                    modifiedSketch.AddShape(s);
                    continue;
                }
            }
            foreach (Mesh m in meshes)
            {

                Sketch.Shape cur_mesh = new Shape();
                cur_mesh.XmlAttrs.Type = "Wire";
                cur_mesh.XmlAttrs.Name = m.Name;
                foreach (Wire w in wires)
                {
                    cur_mesh.AddSubstroke(w.Substroke);
                }

                modifiedSketch.AddShape(cur_mesh);
            }
            Featurefy.FeatureSketch rSketch = new Featurefy.FeatureSketch(ref this.sketch);
            rSketch.FeatureListSingle = sketch.FeatureListSingle;
            rSketch.FeatureListPair = sketch.FeatureListPair;
            return rSketch;

        }

        /// <summary>
        /// Tries to recognize the mesh containing the input wire.  If the mesh containing that wire is not a valid mesh then it will
        /// reevaluate every mesh otherwise simply calculates the 
        /// </summary>
        /// <param name="cur_wire"></param>
        /// <returns></returns>
        public double RecognizeMesh(Substroke s, Featurefy.FeatureSketch fsketch)
        {
           
            if (!(this.substrokeToWire.ContainsKey(s)))
            {
                Wire cur_wire = LoadWire(s);
                Mesh tempmesh = new Mesh();
                tempmesh.AllConnectedWires.Add(cur_wire);
                this.substrokeToWire.Add(cur_wire.Substroke, tempmesh);
                meshes.Add(tempmesh);
            }
            Mesh cur_mesh = this.substrokeToWire[s];
            if (!cur_mesh.validMesh())
            {
                meshes.Remove(cur_mesh);
                Wire start = LoadWire(s);
                cur_mesh = new Mesh(start, marginWireConn);
                               //Need to reevaluate everything (maybe there is a smarter way to do this)
                fsketch = this.RecognizeWires(fsketch);
                cur_mesh = this.substrokeToWire[s];
            }
            return EvaluateMesh(cur_mesh);
        }

        public double RecognizeShape(Shape s, Featurefy.FeatureSketch fs)
        {
            this.sketch = fs.Sketch;
            parseCircuit();
            Mesh cur_mesh = new Mesh(s, marginWireConn);
            if (cur_mesh.validMesh())
                return EvaluateMesh(cur_mesh);
            else
                return 0.0;

        }


        /// <summary>
        /// Evaluates the current mesh
        /// </summary>
        /// <param name="cur_mesh"></param>
        /// <returns></returns>
        private double EvaluateMesh(Mesh cur_mesh)
        {
            //Determine IO (shouldn't be necessary but just check again)
            DetermineIOOfMesh(cur_mesh);
            //Check for right IO
            AdjustWirePolarity(cur_mesh);
            bool correctPolarity = DetermineWirePolarity(cur_mesh);
            //Check for right Type
            bool correctLabels = cur_mesh.LabelMesh();
            //Check for right 
            double prob = (double)cur_mesh.Belief;
            if (!correctPolarity)
                prob *= .5;
            if (!correctLabels)
                prob *= .5;

            return prob;
        }

#endregion

            #region Parse Circuit

        /// <summary>
        /// External method for evaluating the circuit and determine normalized parameters namely the marginWireConn
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public double parseCircuit(Sketch.Sketch s)
        {
            this.sketch = s;
            parseCircuit();
            return marginWireConn;
        }

        /// <summary>
        /// Creates Lists for all of the wires, symbols, and labels, as well as creating a circuit object.
        /// This connects the wires to the symbols and wires to wires, and it gathers the information
        /// for determining whether wires and inputs, outputs, or internal.  TODO: Break this method down into
        /// into smaller methods.
        /// </summary>
        private void parseCircuit()
        {
            //Generate Lists
            InitializeLists();

            // Creates the Circuit Bounding Box by locating the extremes
            circuit = new BoundingBox(circuitShapes);

            // Locate all possible wires around each Symbol
            // Stores how many symbols are connected to each wire and vice versa.
            foreach (BaseSymbol symb in symbols)
            {
                symb.ConnectWireToSymbol(ref wires);

                // Method for Debug
                //symb.ListWires();
            }


            // Calculate Features, Normalization Parameters, and MarginWireConn
            DetermineNormalizedParameters();
            
            // Connect wires that are close to other wires and unconnected
            ConnectCloseWires();

            // Debug
            if (debug)
            {
                foreach (Wire wire in wires)
                {
                    Console.Write(wire.Name + ": ");
                    for (int i = 0; i < wire.EndPt.Length; i++)
                    {
                        if (i < wire.EndPt.Length - 1)
                            Console.Write("P" + (i + 1) + ":" + wire.NumEndPtCon[i] + ", ");
                        else
                            Console.Write("P" + (i + 1) + ":" + wire.NumEndPtCon[i] + "\n");
                    }
                }
            }

        }

        #region Parse Circuit Helper Functions


        /// <summary>
        /// Creates the lists of all wires, symbols, and labels
        /// </summary>
        private void InitializeLists()
        {
            wires = new List<Wire>();
            meshes = new List<Mesh>();
            symbols = new List<BaseSymbol>();
            labels = new List<Shape>();
            substrokeToWire = new Dictionary<Substroke, Mesh>();
            circuitShapes = new List<Shape>();
            shapeNames = new Dictionary<string, Shape>();
            outputDict = new Dictionary<Guid?, object>();

            // Used to keep track of total wires and symbols in the circuit
            WireCount = 0;
            SimpleSymbolCount = 0;
            ComplexSymbolCount = 0;


            // Loads Wires and Symbols into their lists
            foreach (Shape shape in sketch.Shapes)
            {
                Debug.Assert(shape.Name != null, "Error: a shape does not have a name when it should");
                //if (shape.Name == null) return;
                //{
                //    Console.WriteLine("ERR: Current shape does not have a name.");
                //    Console.WriteLine("    Did you forget to recognize before simulating the circuit?");
                //    //return;
                //}

                if (!shapeNames.ContainsKey(shape.Name))
                    shapeNames.Add(shape.Name, shape);

                if (shape.Type.Equals("Wire"))
                {
                    circuitShapes.Add(shape);
                    Mesh newmesh = new Mesh();
                    foreach (Substroke s in shape.SubstrokesL)
                    {
                        Wire wire = LoadWire(s);
                        newmesh.AllConnectedWires.Add(wire);
                    }
                    newmesh.shape = shape;
                    meshes.Add(newmesh);
                    AddToOutput(shape.Substrokes, newmesh);
                }
                else if (shape.XmlAttrs.Type.Equals("Label"))
                {

                    if (shape.Name == null)
                    {
                        string label = TextRecognition.TextRecognition.recognize(shape, wordList, RecognitionModes.Coerce).Match;
                        shape.Name = label;
                        AddToOutput(shape.Substrokes, label);
                    }
                    labels.Add(shape);
                }
                else if (shape.XmlAttrs.Type.Equals("Other"))
                {
                    //Do nothing for now
                }
                else if (shape.XmlAttrs.Type.Equals("LabelBox"))
                {
                    //Do nothing for now
                }
                else
                {
                    SymbolTypes thisType = new SymbolTypes();
                    // If the shape has a label that is not in the current domain it will not get added to symbols
                    // and an error will be recorded.
                    if (SymbolFactory.SimpleOrComplexSymbol(shape.XmlAttrs.Type, out thisType) == null)
                    {
                        errors.Add(new ParseError(shape, "This object does not belong in the current domain."));
                    }
                    // If the shape is a simple symbol
                    else if (SymbolFactory.SimpleOrComplexSymbol(shape.XmlAttrs.Type, out thisType).Value)
                    {
                        circuitShapes.Add(shape);
                        String name = shape.Type + "_" + SimpleSymbolCount;
                        SimpleSymbol newsymbol = new SimpleSymbol(shape, name, domain, thisType);
                        symbols.Add(newsymbol);
                        SimpleSymbolCount++;
                        AddToOutput(shape.Substrokes, newsymbol);
                    }
                    // If the shape is a complex symbol
                    else
                    {
                        circuitShapes.Add(shape);
                        String name = shape.Type + "_" + ComplexSymbolCount;
                        ComplexSymbol newsymbol = new ComplexSymbol(shape, name, domain, thisType);
                        symbols.Add(newsymbol);
                        ComplexSymbolCount++;
                        AddToOutput(shape.Substrokes, newsymbol);
                    }
                }
            }

            // Cannot connect a circuit that does not have any wires, symbols, and/or labels
            // The user should have done all the partial recognition in the previous recognition phase
            // Throw an exception since recognition cannot continue
            if (wires.Count == 0 || symbols.Count == 0 || labels.Count == 0)
            {
                string errorList = "There were none of the following in the sketch: ";

                if (wires.Count == 0)
                    errorList += "wires ";
                if (symbols.Count == 0)
                    errorList += "symbols ";
                if (labels.Count == 0)
                    errorList += "labels";

                errors.Add(new ParseError(null, errorList));
            }
        }

        private void AddToOutput(Substroke[] substrokes, Object element)
        {
            foreach (Substroke sub in substrokes)
                outputDict.Add(sub.XmlAttrs.Id, element);
        }

        private Wire LoadWire(Substroke sub)
        {
            Wire newwire = new Wire(sub, SUBSTROKE_MARGIN);

            // Only create the wire if there are at least two endpoints 
            // (at least since will be elminating the assumption of two endpoints)
            if (true) //(newwire.EndPt.Length >= 2)
            {
                newwire.Name = ("wire" + WireCount);
                wires.Add(newwire);
                WireCount++;
            }
            // Record the errors now since the wires with less than two endpoints will not be added to the
            // overall list of wires of the circuit
            else
            {
                foreach (ParseError e in newwire.Errors)
                {
                    errors.Add(e);
                }
            }
            return newwire;
        }


        /// <summary>
        /// Determine the features of the wires and then calculate the maxwiredist and marginWireConn,
        /// the distance from one wire to another wire
        /// </summary>
        private void DetermineNormalizedParameters()
        {
            // Determine Features (do this here because the next step requires the features to be found, and the next
            // step is required to find the bounding box for connecting wires to wires)
            foreach (Wire cur_wire in wires)
            {
                cur_wire.FeatureExtract(wires, circuit, symbols, labels);
            }



            // Determine normalization parameters (done here so that do not have to determine the maxwiredists twice since it
            // will be used to determine the threshold for connecting wires to wires)
            List<Double> maxwiredists = new List<Double>();
            normParams = Wire.NormParams(wires, circuit, out maxwiredists);

            marginWireConn = 0;
            double num = 0;
            foreach (double d in maxwiredists)
            {
                marginWireConn += d;
                num++;
            }

            // Divide average wire dist by an empirically determined number
            marginWireConn = (marginWireConn / num) / THRESHOLD_DIVIDER;

            // Debug
            if (debug) Console.WriteLine("Wire Margin: " + marginWireConn);
        }

        /// <summary>
        /// Connect wires that have unconnected endpoints and are within 
        /// a certain threshold (the margineWireConn)
        /// </summary>
        private void ConnectCloseWires()
        {
            foreach (Wire new_wire in wires)
            {
                // If any of the endpoints of the wire are not connected,
                // run the wire to wire connection algorithm
                bool notConnected = false;
                for (int i = 0; i < new_wire.EndPtConnect.Length; i++)
                {
                    if (!new_wire.EndPtConnect[i])
                    {
                        notConnected = true;
                        break;
                    }
                }
                if (notConnected)
                    new_wire.ConnectWireToWire(wires, marginWireConn);
            }
        }


        #endregion 


        #endregion

            #region I/O Determination
        /// <summary>
        /// Determines if wires are global inputs, outputs, or internal wires.  This is valid only for
        /// the digital domain.  Analog circuits do not require these global inputs or outputs.
        /// </summary>
        private void ioDetermination()
        {

            // Determine if the wire is connected to the inputs and/or outputs of a symbol.  First have to find all of the wires
            // each wire is connected to. (This part is probably inefficient, could use some tips on this)
            connectConnectedWires(ref wires);

            // Sees if the wires are connected to inputs or outputs or both of symbols.  Used later to determine global IO characteristics.
            countConnectionsToIO(ref wires);

            // Associate labels with the closest wires
            associateLabels(ref wires, ref labels);

            // Determine whether the wires are global inputs, outputs, or internals based on the number of connections
            // of the wire endpoints and if the endpoint is connected to inputs to a symbol or outputs of a symbol, or both

            // First pass looks for wires that are definitely inputs or outputs
            List<Wire> classified = new List<Wire>();
            List<Wire> notclassified = new List<Wire>(wires);
            foreach (Wire w in wires)
            {
                int endpointConCount = countEndpointConnections(w);
                if (endpointConCount == (w.EndPt.Length - 2) && w.ConnToInp && !w.ConnToOut && w.AssociatedLabel != null)
                {
                    w.IOType = WirePolarity.Input;
                    classified.Add(w);
                }
                else if (endpointConCount == (w.EndPt.Length - 1) && w.ConnToInp && !w.ConnToOut && w.AssociatedLabel != null)
                {
                    w.IOType = WirePolarity.Input;
                    classified.Add(w);
                }
                else if (endpointConCount == (w.EndPt.Length - 2) && !w.ConnToInp && w.ConnToOut && w.AssociatedLabel != null)
                {
                    w.IOType = WirePolarity.Output;
                    classified.Add(w);
                }
                else if (endpointConCount == (w.EndPt.Length - 1) && !w.ConnToInp && w.ConnToOut && w.AssociatedLabel != null)
                {
                    w.IOType = WirePolarity.Output;
                    classified.Add(w);
                }

                // To handle feedback from the output.  It is common enough to have this case, but it will get some internal wires wrong that are
                // labeled but only have one connection
                else if (endpointConCount == (w.EndPt.Length - 1) && w.ConnToInp && w.ConnToOut && w.AssociatedLabel != null)
                {
                    w.IOType = WirePolarity.Output;
                    classified.Add(w);
                }
            }
            foreach (Wire c in classified)
                notclassified.Remove(c);
            
            // Second pass looks for wires that are connected to the previously identified inputs and outputs, since they will be internal
            // May still need this step in case grouper does not get all of the wires grouped correctly and there are still connections made
            // in the connect wire to wire phase
            List<Wire> nextpass = new List<Wire>();
            foreach (Wire n in notclassified)
            {
                foreach (Wire c in classified)
                {
                    if (n.AllConnectedWires.Contains(c))
                    {
                        n.IOType = WirePolarity.Internal;
                        nextpass.Add(n);
                    }
                }
            }
            foreach (Wire next in nextpass)
            {
                classified.Add(next);
                notclassified.Remove(next);
            }

            // Third pass looks for wires that are definitely internal wires (2 connections and connected to symbol input and output, 1 connection
            // and connected to symbol input and output, and does not have a label associated with it)
            nextpass.Clear();
            foreach (Wire n in notclassified)
            {
                int endpointConCount = countEndpointConnections(n);
                if (endpointConCount == n.EndPt.Length && n.ConnToInp && n.ConnToOut)
                {
                    n.IOType = WirePolarity.Internal;
                    nextpass.Add(n);
                }
                // All the ones with these conditions with a label were made to be outputs due to feedback, so the rest have to be internals
                else if (endpointConCount == (n.EndPt.Length - 1) && n.ConnToInp && n.ConnToOut)
                {
                    n.IOType = WirePolarity.Internal;
                    nextpass.Add(n);
                }
                // All input and output wires have labels associated with them, so if a wires does not, it is internal
                else if (n.AssociatedLabel == null)
                {
                    n.IOType = WirePolarity.Internal;
                    nextpass.Add(n);
                }
            }
            foreach (Wire next in nextpass)
            {
                notclassified.Remove(next);
            }

            // The fourth pass takes the remaining unclassified wires and puts it through the 13 feature neural net
            if (notclassified.Count > 0)
            {
                // Initialize neural net
                string cur_path = Path.GetDirectoryName(Application.ExecutablePath) + @"\fulltrain467_13.bp";
                BackProp bpNetwork = new BackProp(cur_path);

                foreach (Wire n in notclassified)
                {
                    // Debug
                    //Console.WriteLine("**********Using neural net for {0}**********", n.Name);

                    ArrayList inputAL = n.Normalize(normParams[0], normParams[1], normParams[2],
                                               normParams[3], normParams[4], labels.Count);
                    n.IOType = Wire.SetIO(inputAL, bpNetwork);

                    // Cannot use wires that have an unknown polarity, so record the error
                    if (n.IOType == WirePolarity.Unknown)
                        errors.Add(new ParseError(n, "The global I/O type was not able to be determined for this wire."));
                }
            }

            // Debug
            /*foreach (Wire w in wires)
            {
                Console.WriteLine("\n" + w.Name + ": " + w.IOType);
                Console.WriteLine("Number of cons: {0}, Conntoinp: {1}, Conntoout: {2}, AssociatedLabel?: {3}", countEndpointConnections(w), w.ConnToInp, w.ConnToOut, w.AssociatedLabel != null);
            }
            Console.WriteLine();
            */

            // So that internal wires do not have connections to labels shown in the GUI.  It is just symbol right now since there will be
            // no differentiation between connections to wires or to symbols in the GUI
            foreach (Wire w in wires)
            {
                if (w.IOType == WirePolarity.Internal)
                {
                    if (w.EpToSymbOrWire[0] == "label")
                        w.EpToSymbOrWire[0] = "symbol";
                    if (w.EpToSymbOrWire[1] == "label")
                        w.EpToSymbOrWire[1] = "symbol";
                }
            }

        }

        /// <summary>
        /// Determines the IO of a mesh
        /// </summary>
        /// <param name="cur_mesh"></param>
        private void DetermineIOOfMesh(Mesh cur_mesh)
        {
            // Remove all prior info about IO
            foreach (Wire w in cur_mesh.AllConnectedWires)
            {
                w.IOType = WirePolarity.Unknown;
                w.AssociatedLabel = null;
            }
            // Sees if the wires are connected to inputs or outputs or both of symbols.  Used later to determine global IO characteristics.
            countConnectionsToIO(ref cur_mesh.AllConnectedWires);

            // Associate labels with the closest wires
            associateLabels(ref cur_mesh.AllConnectedWires, ref labels);

            // Determine whether the wires are global inputs, outputs, or internals based on the number of connections
            // of the wire endpoints and if the endpoint is connected to inputs to a symbol or outputs of a symbol, or both

            // First pass looks for wires that are definitely inputs or outputs
            List<Wire> classified = new List<Wire>();
            List<Wire> notclassified = new List<Wire>(cur_mesh.AllConnectedWires);
            foreach (Wire w in wires)
            {
                int endpointConCount = countEndpointConnections(w);
                if (endpointConCount == (w.EndPt.Length - 2) && w.ConnToInp && !w.ConnToOut && w.AssociatedLabel != null)
                {
                    w.IOType = WirePolarity.Input;
                    classified.Add(w);
                }
                else if (endpointConCount == (w.EndPt.Length - 1) && w.ConnToInp && !w.ConnToOut && w.AssociatedLabel != null)
                {
                    w.IOType = WirePolarity.Input;
                    classified.Add(w);
                }
                else if (endpointConCount == (w.EndPt.Length - 1) && !w.ConnToInp && w.ConnToOut && w.AssociatedLabel != null)
                {
                    w.IOType = WirePolarity.Output;
                    classified.Add(w);
                }

                // To handle feedback from the output.  It is common enough to have this case, but it will get some internal wires wrong that are
                // labeled but only have one connection
                else if (endpointConCount == (w.EndPt.Length - 1) && w.ConnToInp && w.ConnToOut && w.AssociatedLabel != null)
                {
                    w.IOType = WirePolarity.Output;
                    classified.Add(w);
                }
            }
            foreach (Wire c in classified)
                notclassified.Remove(c);

            // Second pass looks for wires that are connected to the previously identified inputs and outputs, since they will be internal
            // May still need this step in case grouper does not get all of the wires grouped correctly and there are still connections made
            // in the connect wire to wire phase
            List<Wire> nextpass = new List<Wire>();
            foreach (Wire n in notclassified)
            {
                foreach (Wire c in classified)
                {
                    if (n.AllConnectedWires.Contains(c))
                    {
                        n.IOType = WirePolarity.Internal;
                        nextpass.Add(n);
                    }
                }
            }
            foreach (Wire next in nextpass)
            {
                classified.Add(next);
                notclassified.Remove(next);
            }

            // Third pass looks for wires that are definitely internal wires (2 connections and connected to symbol input and output, 1 connection
            // and connected to symbol input and output, and does not have a label associated with it)
            nextpass.Clear();
            foreach (Wire n in notclassified)
            {
                int endpointConCount = countEndpointConnections(n);
                if (endpointConCount == n.EndPt.Length && n.ConnToInp && n.ConnToOut)
                {
                    n.IOType = WirePolarity.Internal;
                    nextpass.Add(n);
                }
                // All the ones with these conditions with a label were made to be outputs due to feedback, so the rest have to be internals
                else if (endpointConCount == (n.EndPt.Length - 1) && n.ConnToInp && n.ConnToOut)
                {
                    n.IOType = WirePolarity.Internal;
                    nextpass.Add(n);
                }
                // All input and output wires have labels associated with them, so if a wires does not, it is internal
                else if (n.AssociatedLabel == null)
                {
                    n.IOType = WirePolarity.Internal;
                    nextpass.Add(n);
                }
            }
            foreach (Wire next in nextpass)
            {
                notclassified.Remove(next);
            }

            // The fourth pass takes the remaining unclassified wires and puts it through the 13 feature neural net
            if (notclassified.Count > 0)
            {
                // Initialize neural net
                string cur_path = Path.GetDirectoryName(Application.ExecutablePath) + @"\fulltrain467_13.bp";
                BackProp bpNetwork = new BackProp(cur_path);

                foreach (Wire n in notclassified)
                {
                    // Debug
                    if (debug) Console.WriteLine("**********Using neural net for {0}**********", n.Name);

                    ArrayList inputAL = n.Normalize(normParams[0], normParams[1], normParams[2],
                                               normParams[3], normParams[4], labels.Count);
                    n.IOType = Wire.SetIO(inputAL, bpNetwork);

                    // Cannot use wires that have an unknown polarity, so record the error
                    if (n.IOType == WirePolarity.Unknown)
                        errors.Add(new ParseError(n, "The global I/O type was not able to be determined for this wire."));
                }
            }

            // Debug
            if (debug)
            {
                foreach (Wire w in wires)
                {
                    Console.WriteLine("\n" + w.Name + ": " + w.IOType);
                    Console.WriteLine("Number of cons: {0}, Conntoinp: {1}, Conntoout: {2}, AssociatedLabel?: {3}", countEndpointConnections(w), w.ConnToInp, w.ConnToOut, w.AssociatedLabel != null);
                }
                Console.WriteLine();
            }


            // So that internal wires do not have connections to labels shown in the GUI.  It is just symbol right now since there will be
            // no differentiation between connections to wires or to symbols in the GUI
            foreach (Wire w in wires)
            {
                if (w.IOType == WirePolarity.Internal)
                {
                    if (w.EpToSymbOrWire[0] == "label")
                        w.EpToSymbOrWire[0] = "symbol";
                    if (w.EpToSymbOrWire[1] == "label")
                        w.EpToSymbOrWire[1] = "symbol";
                }
            }

        }

        #region IO Determination Helper Functions

        /// <summary>
        /// Connect all of the wires that are connected together in the wire to wire connection step.
        /// This connects all of the wires that may not be directly connected to each other (i.e.
        /// wire 1 connects to wire 2, and wire 3 connects to wire 2, then this connects wires 1 and 3).
        /// </summary>
        /// <param name="wires">The List of Wires in the sketch.</param>
        private void connectConnectedWires(ref List<Wire> wires)
        {
            List<Wire> cwires = new List<Wire>();
            foreach (Wire w in wires)
            {
                if (!cwires.Contains(w))
                {
                    List<Wire> queue = new List<Wire>();
                    queue.Add(w);
                    bool moreadded = true;

                    // Finds all of the wires connected to the current wire (all that are directly and indirectly connected)
                    while (moreadded)
                    {
                        moreadded = false;
                        List<Wire> tobeadded = new List<Wire>();
                        foreach (Wire q in queue)
                        {
                            foreach (Wire con in q.ConnectedWires)
                            {
                                if (!queue.Contains(con))
                                {
                                    tobeadded.Add(con);
                                    moreadded = true;
                                }
                            }
                        }
                        foreach (Wire wire in tobeadded)
                            queue.Add(wire);
                    }

                    // Adds all of the connected wires to the AllConnectedWires field for the current wire.
                    foreach (Wire q in queue)
                    {
                        if (!w.AllConnectedWires.Contains(q))
                            w.AllConnectedWires.Add(q);
                        cwires.Add(q);
                    }

                    // Adds all of the connected wires of the current wire to the wires it is connected to
                    if (queue.Count > 1)
                    {
                        foreach (Wire q in queue)
                        {
                            q.AllConnectedWires.Clear();
                            q.AllConnectedWires.Add(w);
                            foreach (Wire w2 in w.AllConnectedWires)
                            {
                                if (!q.Equals(w2))
                                    q.AllConnectedWires.Add(w2);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sees if the wires are connected to inputs or outputs or both of symbols.
        /// Used later to determine global IO characteristics.
        /// </summary>
        /// <param name="wires">The List of the Wires in the sketch.</param>
        private void countConnectionsToIO(ref List<Wire> wires)
        {
            List<Wire> cwires = new List<Wire>();
            foreach (Wire w in wires)
            {
                if (!cwires.Contains(w))
                {
                    // Go through each symbol and see if the current wire is an input to a symbol or a connected
                    // wire is an input to a symbol.  If it is true, mark the wire and all of the connected wires
                    // as connected to an input of a symbol.  Do the same for outputs of symbols.
                    foreach (BaseSymbol s in symbols)
                    {
                        // If the current wire is an input to a symbol and it is not already marked as an input
                        if (s.InputWires.Contains(w) && !w.ConnToInp)
                        {
                            w.ConnToInp = true;
                            cwires.Add(w);
                            foreach (Wire con in w.AllConnectedWires)
                            {
                                con.ConnToInp = true;
                                cwires.Add(con);
                            }
                        }
                        // If the current wire is an output to a symbol and it is not already marked as an output
                        if (s.OutputWires.Contains(w) && !w.ConnToOut)
                        {
                            w.ConnToOut = true;
                            foreach (Wire con in w.AllConnectedWires)
                            {
                                con.ConnToOut = true;
                                cwires.Add(con);
                            }
                        }
                        // Check all of the connected wires to see if the current wire is an input and/or output of a symbol
                        // TODO: Make an else if statement an check if ConnToInp AND ConnToOut is false
                        else
                        {
                            foreach (Wire con in w.AllConnectedWires)
                            {
                                if (s.InputWires.Contains(con) && !w.ConnToInp)
                                {
                                    w.ConnToInp = true;
                                    cwires.Add(w);
                                    foreach (Wire con2 in w.AllConnectedWires)
                                    {
                                        con2.ConnToInp = true;
                                        cwires.Add(con2);
                                    }
                                }
                                if (s.OutputWires.Contains(con) && !w.ConnToOut)
                                {
                                    w.ConnToOut = true;
                                    cwires.Add(w);
                                    foreach (Wire con2 in w.AllConnectedWires)
                                    {
                                        con2.ConnToOut = true;
                                        cwires.Add(con2);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Associate the closest wire/label pairs with each other.  This continues until all the labels
        /// are associated with wires or all wires are associated with labels or nothing has changed since
        /// the rest of the labels are too far from the wires (as determined by a threshold).
        /// </summary>
        /// <param name="wires">A List of all the Wires in the sketch.</param>
        /// <param name="labels">A List of all the Labels in the sketch.</param>
        private void associateLabels(ref List<Wire> wires, ref List<Shape> labels)
        {
            List<Shape> templabels = new List<Shape>(labels);
            List<Wire> tempwires = new List<Wire>(wires);

            while (templabels.Count > 0 && tempwires.Count > 0)
            {
                double minDist = Double.PositiveInfinity;
                Wire closestWire = new Wire();
                Shape closestLabel = new Shape();
                int closestEp = -1;

                // Checks if anything has changed
                bool hasChanged = false;

                // Find the minimum distance between every wire and every label (average coordinates of label),
                // and save the wire and label that are closest.
                foreach (Wire w in tempwires)
                {
                    foreach (Shape label in templabels)
                    {
                        for (int j = 0; j < label.Points.Count; j += LABEL_COARSENESS)
                        {
                            for (int i = 0; i < w.EndPt.Length; i++)
                            {
                                double localDist = Wire.Dist(w.EndPt[i], label.Points[j].X, label.Points[j].Y);
                                if (localDist < minDist && localDist < LABEL_DIST_THRESHOLD)
                                {
                                    closestWire = w;
                                    closestLabel = label;
                                    minDist = localDist;
                                    closestEp = i;
                                    hasChanged = true;
                                }
                            }
                        }
                    }
                }
                // If something has changed, make the wire and label associations
                if (hasChanged)
                {
                    closestWire.AssociatedLabel = closestLabel;
                    if (closestEp != -1)
                        closestWire.EpToSymbOrWire[closestEp] = "label";
                    templabels.Remove(closestLabel);
                    tempwires.Remove(closestWire);
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Counts the total number of connections the wire has.
        /// </summary>
        /// <param name="wire">The current wire.</param>
        /// <returns>The total number of connections the wire has.</returns>
        private int countEndpointConnections(Wire wire)
        {
            int connectionCount = 0;
            for (int i = 0 ; i < wire.EndPt.Length ; i++)
            {
                connectionCount += wire.NumEndPtCon[i];
            }
            return connectionCount;
        }

        #endregion

        #endregion

            #region Create Meshes
        /// <summary>
        /// Combines connected wires into Meshes, finds which labels are closest to which Meshes,
        /// combines wires with the same label into Meshes, and stores which symbols are connected to
        /// Meshes and which Meshes are connected to which symbols. TODO: Split this method into smaller ones.
        /// </summary>
        private void makeMeshes()
        {
            // Create all of the meshes by finding all of the wire connections and putting them into one object
            meshes = createMeshes();

            // Give the rest of the Meshes names (the internal wires)
            int intcount = 0;
            foreach (Mesh m in meshes)
            {
                if (m.Name == null)
                {
                    m.Name = "INT_" + intcount;
                    intcount++;
                }
            }

            // Look through each symbol and see all of the Meshes that are connected to it
            foreach (BaseSymbol s in symbols)
            {
                foreach (Mesh m in meshes)
                {
                    if (m.ConSymb.Contains(s))
                    {
                        s.ConMeshes.Add(m);
                    }
                }

                // Debug
                if (debug)
                {
                    Console.WriteLine("\nMeshes connected to " + s.SymbType);
                    foreach (Mesh m in s.ConMeshes)
                    {
                        Console.WriteLine(m.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Creates the List of meshes (an object representing connected wires) in the sketch.
        /// </summary>
        /// <returns>A List of meshes in the sketch.</returns>
        private List<Mesh> createMeshes()
        {
            foreach (Mesh mesh in meshes)
            {
                foreach (Wire wire in mesh.AllConnectedWires)
                {
                    ConnectConnectedEndpoints(mesh, wire);
                    ConnectMeshToSymbols(mesh, wire);
                    AssociateLabels(mesh, wire);
                }

                AdjustWirePolarity(mesh);
                bool tooManyLabels = mesh.LabelMesh();
                bool extraConnections = DetermineWirePolarity(mesh);
            }

            return meshes;
        }

        #region Mesh Helper Functions

        /// <summary>
        /// Merge Meshes that have the same label on an input or output so that different wires representing the same input can be
        /// highlighted together in the mesh highlighting
        /// </summary>
        /// <param name="meshes"></param>
        private void mergeMeshes()
        {
            // Sort the Meshes that have the same label as other Meshes by label
            List<List<Mesh>> ordered = new List<List<Mesh>>();
            List<Mesh> added = new List<Mesh>();
            foreach (Mesh m in meshes)
            {
                List<Mesh> subordered = new List<Mesh>();
                if (m.Name != null && !added.Contains(m))
                {
                    subordered.Add(m);
                    added.Add(m);
                    foreach (Mesh m2 in meshes)
                    {
                        if (!m.Equals(m2) && m.Name.Equals(m2.Name))
                        {
                            subordered.Add(m2);
                            added.Add(m2);
                        }
                    }

                    ordered.Add(subordered);
                }
            }

            // Remove the Meshes that will be merged
            foreach (Mesh m in added)
            {
                meshes.Remove(m);
            }

            // Merge the Meshes
            foreach (List<Mesh> lmesh in ordered)
            {
                if (lmesh.Count > 1)
                {
                    for (int i = 1; i < lmesh.Count; i++)
                    {
                        lmesh[0] = Mesh.MergeMeshes(lmesh[0], lmesh[i]);
                    }
                }

                meshes.Add(lmesh[0]);
            }
        }

        private void AdjustWirePolarity(Mesh cur_mesh)
        {
            cur_mesh.ICount = 0;
            cur_mesh.OCount = 0;
            foreach (Wire q in cur_mesh.AllConnectedWires)
            {
                if (q.IOType == WirePolarity.Input)
                    cur_mesh.ICount++;
                else if (q.IOType == WirePolarity.Output)
                    cur_mesh.OCount++;
            }
        }

        /// <summary>
        /// Determines the WirePolarity of a mesh.  It returns 0 if there is an error.  Otherwise it returns the number of extra inputs and outputs
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private bool DetermineWirePolarity(Mesh m)
        {

            
            // If the wire has multiple input/output wires in it, an error will be thrown.  If it has only one, the IOType of the Mesh
            // will be set, and if it has zero, the IOType of the Mesh will be set as internal
            if ((m.ICount + m.OCount) == 0)
            {
                m.IOType = WirePolarity.Internal;
                return true;
            }
            else if (m.ICount == 1 && m.OCount == 0)
            {
                m.IOType = WirePolarity.Input;
                return true;
            }
            else if (m.ICount == 0 && m.OCount == 1)
            {
                m.IOType = WirePolarity.Output;
                return true;
            }

            // Record the error and choose the mesh type as input if no outputs, output if no inputs, and if there are both choose the
            // IOType with the largest count or if they are the same choose input.  Do this to keep the program running.
            else if (m.ICount > 1 && m.OCount == 0)
            {
                //TODO ADDFEEDBACK
                errors.Add(new ParseError(m, "More than one global input in the same mesh."));
                m.IOType = WirePolarity.Input;
                return false;
            }
            else if (m.ICount == 0 && m.OCount > 1)
            {
                //TODO ADDFEEDBACK
                errors.Add(new ParseError(m, "More than one global output in the same mesh."));
                m.IOType = WirePolarity.Output;
                return false;
            }
            else //(icount >= 1 && ocount >= 1)
            {
                //TODO ADDFEEDBACK
                errors.Add(new ParseError(m, "At least one global input and output are in the same mesh."));
                if (m.ICount >= m.OCount)
                    m.IOType = WirePolarity.Input;
                else
                    m.IOType = WirePolarity.Output;
                return false;
            }
        }

        /// <summary>
        /// Adjust the mesh's lists of endpoints appropriately for the new wire
        /// </summary>
        /// <param name="cur_mesh"></param>
        /// <param name="q"></param>
        private static void ConnectConnectedEndpoints(Mesh cur_mesh, Wire q)
        {
            for (int i = 0; i < q.EndPt.Length; i++)
            {
                if (q.EndPtConnect[i] && q.EpToSymbOrWire[i] == "symbol")
                    cur_mesh.EpConToSymb.Add(q.EndPt[i]);
                else if (q.EndPtConnect[i] && q.EpToSymbOrWire[i] == "wire")
                    cur_mesh.EpConToWire.Add(q.EndPt[i]);
                else if (q.EpToSymbOrWire[i] == "label")
                    cur_mesh.EpConToLabel.Add(q.EndPt[i]);
                else if (!q.EndPtConnect[i])
                    cur_mesh.EpUnCon.Add(q.EndPt[i]);
            }
        }

        /// <summary>
        /// Adds the wire's labels to the current mesh's list of labels
        /// </summary>
        /// <param name="cur_mesh"></param>
        /// <param name="q"></param>
        private static void AssociateLabels(Mesh cur_mesh, Wire q)
        {
            // Right now, lets the Mesh have multiple labels but will throw an error if this happens
            if (q.AssociatedLabel != null && (q.IOType == WirePolarity.Input || q.IOType == WirePolarity.Output))
            {
                cur_mesh.AssociatedLabel.Add(q.AssociatedLabel);
            }
        }

        /// <summary>
        /// Connects the mesh to symbols connected to the wire
        /// </summary>
        /// <param name="cur_mesh"></param>
        /// <param name="q"></param>
        private void ConnectMeshToSymbols(Mesh cur_mesh, Wire q)
        {
            // Add the symbols that the Mesh is connected to
            foreach (BaseSymbol s in symbols)
            {
                if (s.InputWires.Contains(q) || s.OutputWires.Contains(q))
                {
                    cur_mesh.ConSymb.Add(s);
                }
            }
        }

        #endregion


        #endregion

            #region Organize Results

        /// <summary>
        /// Compile all the output information about the
        /// circuit, including the circuit structure,
        /// all global outputs, and all global inputs.
        /// </summary>
        private void organizeResults()
        {
            // Calculate all the input and output meshes in each symbol
            foreach (BaseSymbol symbol in symbols)
                symbol.CalcInputAndOutputMeshes();
            
            findCircuitOutputs();
            findCircuitInputs();

            bool valid = tryMakingCircuitStructure();
            if (!valid)
                fixCircuitStructure();
        }

            #region Make Circuit Structure

        /// <summary>
        /// Generates the circuitStructure for the circuit recognizer.
        /// Specifically, it generates a dictionary containing information
        /// about each symbol in the circuit: its name, type, and inputs.
        /// 
        /// ASSUMPTIONS:
        ///   * All meshes are completely and correctly defined / recognized
        ///     (i.e. no wires are left out of a mesh).
        ///   * Also assumes that no two labels in the circuit are the same.
        /// </summary>
        /// <returns>A boolean representing if there were any errors.</returns>
        private bool tryMakingCircuitStructure()
        {
            bool valid = true; // Temporarily set to true
            circuitStructure = new Dictionary<string, KeyValuePair<string, List<string>>>();

            //// Calculate all the input and output meshes in each symbol
            //foreach (BaseSymbol symbol in symbols)
            //    symbol.CalcInputAndOutputMeshes();

            // For each symbol in the circuit, create and add an entry to 
            // the dictionary
            foreach (BaseSymbol symbol in symbols)
            {
                List<string> inputSymbols = new List<string>();

                // Find the names of all the inputs to this symbol
                foreach (Mesh mesh in symbol.InputMeshes)
                {
                    string inputName = null;

                    // First try to do this by matching
                    // corresponding input and output meshes
                    foreach (BaseSymbol candidateSymbol in symbols)
                        if (candidateSymbol.OutputMeshes.Contains(mesh))
                        {
                            inputName = candidateSymbol.Shape.Name;
                            break;
                        }

                    // If we couldn't find a match above, the input
                    // must be a label
                    if ((inputName == null) && (mesh.AssociatedLabel.Count > 0))
                        inputName = mesh.AssociatedLabel[0].Name;

                    // Is the circuit structure valid?
                    valid = !(inputName == null);

                    // Store the name we find
                    inputSymbols.Add(inputName);
                }

                KeyValuePair<string, List<string>> symbolInfo = 
                    new KeyValuePair<string, List<string>>(symbol.SymbType.ToString(), inputSymbols);

                if (circuitStructure.ContainsKey(symbol.Shape.Name))
                    symbol.Shape.Name = symbol.Name;
                circuitStructure.Add(symbol.Shape.Name, symbolInfo);
            }

            return valid;
        }

        #endregion

            #region Fix Circuit Structure

        /// <summary>
        /// If the circuit structure finds any symbols with null
        /// inputs, swap the inputs and outputs and try again.
        /// 
        /// Note: This only swaps connections if there are an
        /// equal number of inputs/outputs. This is because we do
        /// not want to give symbols more outputs than inputs.
        /// </summary>
        private void fixCircuitStructure()
        {
            foreach (BaseSymbol symbol in symbols)
            {
                if ((circuitStructure[symbol.Shape.Name].Value.Contains(null)) &&
                    (symbol.InputMeshes.Count == symbol.OutputMeshes.Count))
                {
                    symbol.swapConnections();
                    tryMakingCircuitStructure();
                }
            }
        }

        #endregion

            #region Find Circuit Inputs / Outputs

        /// <summary>
        /// Calculate what the outputs of the entire circuit are,
        /// including their names and the name of the gate they're
        /// connected to.
        /// </summary>
        private void findCircuitOutputs()
        {
            circuitOutputs = new List<KeyValuePair<string, string>>();

            foreach (Mesh mesh in meshes)
            {
                string outputName = null;
                string outputSymbol = null;
                
                if (mesh.IOType == WirePolarity.Output)
                    foreach (BaseSymbol symbol in symbols)
                        if (symbol.OutputMeshes.Contains(mesh))
                        {
                            if (mesh.AssociatedLabel.Count > 0)
                                outputName = mesh.AssociatedLabel[0].Name;

                            outputSymbol = symbol.Shape.Name;

                            break;
                        }

                if (outputName != null && outputSymbol != null)
                    circuitOutputs.Add(new KeyValuePair<string, string>(outputName, outputSymbol));
            }
        }

        /// <summary>
        /// Generate a list of inputs to the circuit.
        /// </summary>
        private void findCircuitInputs()
        {
            circuitInputs = new List<string>();

            foreach (Mesh mesh in meshes)
            {
                string inputName = null;

                if (mesh.IOType == WirePolarity.Input)
                {
                    if (mesh.AssociatedLabel.Count > 0)
                        inputName = mesh.AssociatedLabel[0].Name;
                    //foreach (Wire wire in mesh.AllConnectedWires)
                    //    if (wire.AssociatedLabel != null)
                    //        inputName = wire.AssociatedLabel.Name;
                }

                if (inputName != null)
                    circuitInputs.Add(inputName);
            }
        }


            #endregion

            #endregion

            #region Check Results

        /// <summary>
        /// Makes sure all inputs are connected properly,
        /// and no wires are connected to multiple outputs.
        /// </summary>
        /// <returns>A boolean telling whether or not the circuit is valid.</returns>
        private bool checkResults()
        {
            bool valid = true;

            // A circuit with no gates is not valid.
            if (symbols.Count == 0)
                valid = false;

            //// Make sure all the labels in the sketch are accounted for.
            //if (LabelShapes.Count != circuitInputs.Count + circuitOutputs.Count)
            //{
            //    List<string> outputNames = new List<string>();
            //    foreach (KeyValuePair<string, string> output in circuitOutputs)
            //        outputNames.Add(output.Key);

            //    foreach (string labelName in LabelNames)
            //        if (!(circuitInputs.Contains(labelName)) &&
            //            !(outputNames.Contains(labelName)))
            //            circuitErrors.Add(shapeNames[labelName]);

            //    valid = false;
            //}

            // Make sure all gate inputs are connected to something
            foreach (string gateName in circuitStructure.Keys)
                if (circuitStructure[gateName].Value.Contains(null))
                {
                    circuitErrors.Add(shapeNames[gateName]);
                    valid = false;
                }

            // Make sure all gate outputs are not connected to any other outputs.
            foreach (BaseSymbol symbol in symbols)
                foreach (Mesh outputMesh in symbol.OutputMeshes)
                    foreach (BaseSymbol otherSymbol in symbols)
                        if ((symbol != otherSymbol) &&
                            (otherSymbol.OutputMeshes.Contains(outputMesh)))
                        {
                            circuitErrors.Add(shapeNames[outputMesh.shape.Name]);
                            valid = false;
                        }

            bool connNOTBUBBLE = false;

            foreach (string gateName in circuitStructure.Keys)
            {
                if (gateName.Contains("NOTBUBBLE"))
                {
                    foreach (KeyValuePair<string, List<string>> gateName2 in circuitStructure.Values)
                    {
                        if (gateName2.Value.Contains(gateName))
                        {
                            connNOTBUBBLE = true;
                        }
                    }
                }
                else
                {
                    connNOTBUBBLE = true;
                }
            }
            if (!connNOTBUBBLE)
            {
                valid = false;
            }

            return valid;
        }

        #endregion

            #region Printing (for debugging)

        private void printDictionaryEtc()
        {

            Console.WriteLine("-----------------------------------------------");

            foreach (string symbol in circuitStructure.Keys)
            {
                Console.WriteLine(symbol + ", a " + circuitStructure[symbol].Key + " has inputs:");
                foreach (string input in circuitStructure[symbol].Value)
                {
                    if (input == null)
                        Console.WriteLine("  NULL!!!!!");
                    else
                        Console.WriteLine("  " + input);
                }
                Console.WriteLine("");
            }

            Console.WriteLine("");
            Console.WriteLine("The circuit inputs are: ");
            foreach (string input in circuitInputs)
                Console.WriteLine("  " + input);

            Console.WriteLine("");
            Console.WriteLine("The circuit outputs are: ");
            foreach (KeyValuePair<string, string> pair in circuitOutputs)
                Console.WriteLine("  " + pair.Key + ", connected to a " + pair.Value);
        }

        #endregion

        #endregion

        #region Getters and Setters

        /// <summary>
        /// Gets the dictionary of the gates in the circuit,
        /// along with all their inputs.
        /// </summary>
        public Dictionary<string, KeyValuePair<string, List<string>>> CircuitStructure
        {
            get
            {
                return this.circuitStructure;
            }
        }

        /// <summary>
        /// Gets the list of all global outputs of the circuit
        /// </summary>
        public List<KeyValuePair<string, string>> CircuitOutputs
        {
            get
            {
                return this.circuitOutputs;
            }
        }

        /// <summary>
        /// Gets the list of all global inputs to the circuit
        /// </summary>
        public List<string> CircuitInputs
        {
            get
            {
                return this.circuitInputs;
            }
        }

        /// <summary>
        /// Gets the dictionary mapping all shape names
        /// to their original shapes.
        /// </summary>
        public Dictionary<string, Shape> ShapeNames
        {
            get
            {
                return this.shapeNames;
            }
        }

        /// <summary>
        /// Mapping of substrokes' GUID? to the label, symbol, or mesh that the substroke belongs to.
        /// </summary>
        /// <returns>Mapping of substrokes' GUID? to the label, symbol, or mesh that the substroke belongs to.</returns>
        public Dictionary<Guid?, object> Substroke2CircuitMap
        {
            get
            {
                return this.outputDict;
            }
        }

        /// <summary>
        /// The List of Meshes in the sketch.
        /// </summary>
        public List<Mesh> Meshes
        {
            get
            {
                return this.meshes;
            }
        }

        /// <summary>
        /// The List of Symbols in the sketch.
        /// </summary>
        public List<BaseSymbol> Symbols
        {
            get
            {
                return this.symbols;
            }
        }

        /// <summary>
        /// The List of Labels in the sketch.
        /// </summary>
        public List<Shape> LabelShapes
        {
            get
            {
                return this.labels;
            }
        }

        /// <summary>
        /// The List of Labels in the sketch.
        /// </summary>
        public List<String> LabelNames
        {
            get
            {
                List<string> labelnames = new List<string>();
                foreach (Shape label in this.labels)
                    labelnames.Add(label.Name);
                return labelnames;
            }
        }

        /// <summary>
        /// The List of all the wires in the current sketch.
        /// </summary>
        public List<Wire> Wires
        {
            get
            {
                return this.wires;
            }
        }

        /// <summary>
        /// The List of the errors that occured during circuit construction.
        /// </summary>
        public List<ParseError> Errors
        {
            get
            {
                return this.errors;
            }
        }

        /// <summary>
        /// The list of shape names which had errors during circuit construction.
        /// </summary>
        public List<Shape> CircuitErrors
        {
            get
            {
                return this.circuitErrors;
            }
        }



        #endregion

        #region Refiner Interfacing Code

        /// <summary>
        /// Adds a wire to the list of wires and creates a new mesh containing it
        /// </summary>
        /// <param name="w"></param>
        private void AddWire(Wire w)
        {
            if (!this.wires.Contains(w))
            {
                this.wires.Add(w);
                w.ConnectWireToWire(wires, marginWireConn);
                Mesh newMesh = new Mesh();
                newMesh.AllConnectedWires.Add(w);
                meshes.Add(newMesh);
                this.substrokeToWire.Add(w.Substroke, newMesh);
            }
        }

        /// <summary>
        /// Removes a wire from the list of wires and its appropriate mesh
        /// </summary>
        /// <param name="w"></param>
        private void RemoveWire(Wire w)
        {
            if (this.wires.Contains(w))
            {
                this.wires.Remove(w);
                Mesh m = this.substrokeToWire[w.Substroke];
                m.AllConnectedWires.Remove(w);
                if (m.AllConnectedWires.Count == 0)
                    this.meshes.Remove(m);
                this.substrokeToWire.Remove(w.Substroke);
                foreach (Wire v in w.AllConnectedWires)
                {
                    v.AllConnectedWires.Remove(w);
                }
                foreach (Wire v in w.ConnectedWires)
                {
                    v.ConnectedWires.Remove(w);
                }
            }
        }

        /// <summary>
        /// Find a wire by looking up the appropriate substroke
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private Wire LookUpWireBySubstroke(Substroke s)
        {
            foreach (Wire w in wires)
            {
                if (w.Substroke.Equals(s))
                    return w;
            }
            return null;
        }

        public void AddStroke(Sketch.Stroke s)
        {
            foreach (Substroke sub in s.SubstrokesL)
            {
                Wire w = LookUpWireBySubstroke(sub);
                if (w == null)
                {
                    w = new Wire(sub, marginWireConn);
                    this.AddWire(w);
                }
            }
        }

        public void RemoveStroke(Sketch.Stroke s)
        {
            foreach (Substroke sub in s.SubstrokesL)
            {
                Wire w = LookUpWireBySubstroke(sub);
                if (w != null)
                {
                    this.RemoveWire(w);
                }
            }
        }

        public Double MarginWireConn
        {
            get { return marginWireConn; }
        }

        #endregion
    }

    #region Parse Error Exception

    /// <summary>
    /// Exception for CircuitRec to use.  It stores a List of the objects that caused the exception and a message
    /// describing what the error is.  The object can be shape, label, mesh, wire, basesymbol, or null.
    /// </summary>
    public class ParseError : System.ApplicationException
    {
        public List<object> errors = new List<object>();

        /// <summary>
        /// Constructor for ParseError
        /// </summary>
        /// <param name="element">The element that caused an error.</param>
        /// <param name="message">Message describing the error.</param>
        public ParseError(object element, string message) : base(message)
        {
            errors.Add(element);
        }

        /// <summary>
        /// Constructor for ParseError
        /// </summary>
        /// <param name="elements">The elements that caused an error.</param>
        /// <param name="message">Message describing the error.</param>
        public ParseError(List<object> elements, string message) : base(message)
        {
            errors = elements;
        }
    }

    #endregion

}
