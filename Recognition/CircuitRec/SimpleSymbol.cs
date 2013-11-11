//Symbol class pull out a base class

/**
 * File: Symbol.cs
 *
 * Authors: Matthew Weiner, Howard Chen, and Sam Gordon
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2007.
 * 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Sketch;

namespace CircuitRec
{
    /// <summary>
    /// This class is for basic symbols that have one input side and one output side.
    /// </summary>
    public class SimpleSymbol : BaseSymbol
    {
        #region Internals

        /// <summary>
        /// The amount to increase the margin value for symbol connections by.
        /// </summary>
        private const double INCREASE_MARGIN_BY = 0.001;

        /// <summary>
        /// The amount to decrease the margin value for the symbol connections by.  It is smaller than the increase so that if
        /// the box gets too large, it can decrease in size by a smaller amount than it increased, which prevents an
        /// infinite loop.
        /// </summary>
        private const double DECREASE_MARGIN_BY = 0.0001;

        #endregion

        #region Constructor
        
        /// <summary>
        /// Constructor for SimpleSymbol.
        /// </summary>
        /// <param name="newSymbol">The shape from which the new symbol will be made.</param>
        /// <param name="name">The name of the new symbol.</param>
        /// <param name="domain">A dictionary containing the connection information for the symbols of the current domain.</param>
        
        public SimpleSymbol(Shape newSymbol, String name, Domain domain, SymbolTypes symbType)
        {
            // Initialize variables
            this.Name = name;
            this.Shape = newSymbol;
            SymbType = symbType;
            points = new List<Point>();
            substrokes = new List<Substroke>();
            ConMeshes = new List<Mesh>();
            errors = new List<ParseError>();

            // The domain sets the connection constraints
            SimpleSymbol tempsymb = domain.SetConnectionInfoSimple(this);
            this.NumInputs = tempsymb.NumInputs;
            this.NumOutputs = tempsymb.NumOutputs;
            this.InIsFixed = tempsymb.InIsFixed;
            this.OutIsFixed = tempsymb.OutIsFixed;


            // Gets points, substrokes, bounding box, 
            // orientation, and connections of the symbol
            getPoints(newSymbol);
            findBoundaries(newSymbol);
            setOrientation(newSymbol);

            // Initializing the wire lists
            ConnectedWires = new List<Wire>();
            InputWires = new List<Wire>();
            OutputWires = new List<Wire>();
        }
        #endregion

        #region Methods

        /// <summary>
        /// Debug Method.  Lists all the Wires associated with the Symbol
        /// </summary>
        internal override void ListWires()
        {
            Console.WriteLine("{0} contains the following wires:", SymbType);
            Console.WriteLine("\nInput");
            Console.WriteLine("Bounding Box: TL({0},{1}), BR({2},{3})", TopLeftX, TopLeftY, BottomRightX, BottomRightY);

            foreach (Wire wire in InputWires)
                Console.WriteLine(wire.Name);

            Console.WriteLine("\nOutput");
            foreach (Wire wire in OutputWires)
                Console.WriteLine(wire.Name);

            Console.WriteLine();
        }

        #region ConnectWireToSymbol

        /// <summary>
        /// Connects wires to the symbol based on the symbol's 
        /// shape's connections. Also uses the symbol's orientation 
        /// to determine which wires are inputs and outputs.
        /// </summary>
        /// <param name="wires">All the wires in the circuit</param>
        internal override void ConnectWireToSymbol(ref List<Wire> wires)
        {
            // Rotate the centroid's x-coordinate in the opposite 
            // direction by the gate's orientation angle.
            double x_center = this.Shape.Centroid[0] * Math.Cos(orientationAngle) +
                                this.Shape.Centroid[1] * Math.Sin(orientationAngle);

            foreach (Sketch.Shape connectedShape in this.Shape.ConnectedShapes.Keys)
            {
                if (connectedShape.Type == "Wire")// (connectedShape.Substrokes[0].Classification == "Wire")
                {
                    KeyValuePair<Wire, EndPoint> connectingWire =
                        GetClosestWireInShape(connectedShape, wires);

                    Wire wire = connectingWire.Key;
                    EndPoint endpoint = connectingWire.Value;

                    if (wire == null || endpoint == null)
                    {
                        throw new Exception("ERR: Wire is null " + (wire == null) + " Endpoint is null " + (endpoint == null));
                    }
                   
                    ConnectedWires.Add(wire);

                    // Do stuff for endpoint highlighting by finding closest
                    // endpoint to the shape. We do this instead of simply using
                    // the endpoint we just found because there seems to be
                    // a problem with equality comparison between endpoints of
                    // wires and endpoints of substrokes.
                    double distance = Double.MaxValue;
                    int index = 0;
                    for (int i = 0; i < wire.EndPt.Length; i++)
                    {
                        double ptDist = Math.Sqrt(Math.Pow(wire.EndPt[i].X - endpoint.X, 2) +
                                                  Math.Pow(wire.EndPt[i].Y - endpoint.Y, 2));
                        if (ptDist < distance)
                        {
                            distance = ptDist;
                            index = i;
                        }
                    }
                    // Set the highlighting properties
                    wire.EndPtConnect[index] = true;
                    wire.EpToSymbOrWire[index] = "symbol";

                    // Rotate the wire's endpoint that is connected to the symbol
                    // by the symbol's orientation angle.
                    double x = connectingWire.Value.X * Math.Cos(orientationAngle) +
                               connectingWire.Value.Y * Math.Sin(orientationAngle);

                    // Assume that this rotation reorients the gate left-to-right.
                    // This is only correct if the gate was originally oriented
                    // with orientation angle between -pi/2 and pi/2.
                    if (x < x_center)
                        InputWires.Add(connectingWire.Key); // input wires are on the left
                    else
                        OutputWires.Add(connectingWire.Key); // output wires are on the right
                }
            }

            // Check to make sure there are no more outputs than inputs.
            // If so, they must be switched.
            // This does not deal with the case of equal numbers of output
            // and input wires (NOT gates). This in checkConnections.
            if (OutputWires.Count > InputWires.Count)
                swapConnections();
        }


        /// <summary>
        /// Swap inputs and outputs. Call this function if you find
        /// that there are more outputs than inputs, or if you find
        /// that a symbol with equal numbers of inputs/outputs is invalid.S
        /// </summary>
        /// <param name="symbols">All the symbols in the circuit.</param>
        internal override void swapConnections()
        {
            // Swap wires
            List<Wire> temp_wires = InputWires;
            InputWires = OutputWires;
            OutputWires = temp_wires;

            // Swap meshes
            List<Mesh> temp_meshes = InputMeshes;
            InputMeshes = OutputMeshes;
            OutputMeshes = temp_meshes;
        }

        #region ConnectWireToSymbol Helper Functions

        /// <summary>
        /// Helper function for ConnectWireToSymbol.
        /// Get a wire from the already-made list of wires
        /// based on the substroke composing the wire
        /// </summary>
        /// <param name="wires">All the wires in the circuit</param>
        /// <param name="substroke">The desired wire's associated substroke</param>
        /// <returns></returns>
        private Wire GetWireBySubstroke(List<Wire> wires, Substroke substroke)
        {
            foreach (Wire wire in wires)
                if (substroke.Equals(wire.Substroke))
                    return wire;

            // If the wire doesn't exist, return null
            // This should not be hit unless this function is
            // called before the list of wires is created,
            // or if the substroke is not actually a wire.
            return null;
        }

        /// <summary>
        /// Finds the wire substroke within a wire shape
        /// that is closest to the current symbol, along
        /// with its endpoint of connectivity.
        /// 
        /// This assumes the shape argument is a wire.
        /// </summary>
        /// <param name="neighborShape"></param>
        /// <returns></returns>
        private KeyValuePair<Wire, EndPoint> GetClosestWireInShape(Sketch.Shape wireShape, List<Wire> wires)
        {
            // Find the endpoint closest to the shape in question.
            double distance = Double.MaxValue;
            Sketch.Substroke closestSubstroke = null;
            Sketch.EndPoint closestEndpoint = null;

            foreach (Sketch.Substroke stroke in wireShape.Substrokes)
            {
                foreach (Sketch.EndPoint endpoint in stroke.Endpoints)
                {
                    double ptDist = Math.Sqrt(Math.Pow(endpoint.X - this.Shape.Centroid[0], 2) +
                                              Math.Pow(endpoint.Y - this.Shape.Centroid[1], 2));
                    if (ptDist < distance)
                    {
                        distance = ptDist;
                        closestSubstroke = stroke;
                        closestEndpoint = endpoint;
                    }
                }
            }

            Wire wire = GetWireBySubstroke(wires, closestSubstroke);
            return new KeyValuePair<Wire, EndPoint>(wire, closestEndpoint);
        }

        #endregion

        #endregion

        #endregion

    }
}
