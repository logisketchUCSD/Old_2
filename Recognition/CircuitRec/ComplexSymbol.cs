using System;
using System.Collections.Generic;
using System.Text;
using Sketch;

namespace CircuitRec
{
    /// <summary>
    /// For symbols that have inputs or outputs on more than two sides.
    /// </summary>
    public class ComplexSymbol : BaseSymbol
    {
        #region Internals

        /// <summary>
        /// Angular threshold for connecting wires to ComplexSymbols in degrees
        /// </summary>
        private const double ANGLE_THRESHOLD = 45;

        /// <summary>
        /// The wires that are connected to the top of the symbol (CLK, select, or other control signals).
        /// </summary>
        public List<Wire> TopWires;

        /// <summary>
        /// The wires that are connected to the bottom of the symbol (RESET, EN, or other control signals).
        /// </summary>
        public List<Wire> BottomWires;

        /// <summary>
        /// The number of wires that can be attached to the top of the ComplexSymbol (CLK side or select signal side).
        /// </summary>
        internal int NumTop;

        /// <summary>
        /// True if the number of top wires specified by NumTop is fixed, false if the number of top wires is a minimum (taken from the domain file).
        /// </summary>
        internal bool TopIsFixed;

        /// <summary>
        /// The number of wires that can be attached to the bottom of the ComplexSymbol (RESET or EN side).
        /// </summary>
        internal int NumBottom;

        /// <summary>
        /// True if the number of bottom wires specified by NumBottom is fixed, false if the number of bottom wires is a minimum (taken from the domain file).
        /// </summary>
        internal bool BottomIsFixed;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for ComplexSymbol.
        /// </summary>
        /// <param name="newSymbol">The shape that is a ComplexSymbol.</param>
        /// <param name="name">The name given to the ComplexSymbol.</param>
        /// <param name="domain">The current domain.</param>
        /// <param name="symbType">The type of the symbol.</param>
        public ComplexSymbol(Shape newSymbol, String name, Domain domain, SymbolTypes symbType)
        {
            // Initialize variables
            Name = name;
            SymbType = symbType;
            points = new List<Point>();
            substrokes = new List<Substroke>();
            ConMeshes = new List<Mesh>();
            errors = new List<ParseError>();

            // The domain sets the connection constraints
            ComplexSymbol tempsymb = domain.SetConnectionInfoComplex(this);
            this.NumInputs = tempsymb.NumInputs;
            this.NumOutputs = tempsymb.NumOutputs;
            this.InIsFixed = tempsymb.InIsFixed;
            this.OutIsFixed = tempsymb.OutIsFixed;
            this.NumTop = tempsymb.NumTop;
            this.NumBottom = tempsymb.NumBottom;
            this.TopIsFixed = tempsymb.TopIsFixed;
            this.BottomIsFixed = tempsymb.BottomIsFixed;


            // Gets points, substrokes, bounding box, and orientation of the symbol
            getPoints(newSymbol);
            findBoundaries(newSymbol);
            setOrientation(newSymbol);

            // Initializing the lists
            InputWires = new List<Wire>();
            OutputWires = new List<Wire>();
            TopWires = new List<Wire>();
            BottomWires = new List<Wire>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Debug method that lists all of the wires that are connected to the ComplexSymbol.
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

            Console.WriteLine("\nTop");
            foreach (Wire wire in TopWires)
                Console.WriteLine(wire.Name);

            Console.WriteLine("\nBottom");
            foreach (Wire wire in BottomWires)
                Console.WriteLine(wire.Name);

            Console.WriteLine();
        }

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
                if (connectedShape.Substrokes[0].Classification == "Wire")
                {
                    KeyValuePair<Wire, EndPoint> connectingWire =
                        GetClosestWireInShape(connectedShape, wires);

                    Wire wire = connectingWire.Key;
                    EndPoint endpoint = connectingWire.Value;

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
            // If so, they must be switched. This will only occur if the
            // assumption above was incorrect, and the orientation angle
            // is between pi/2 and 3*pi/2.
            if (OutputWires.Count > InputWires.Count)
            {
                List<Wire> temp = InputWires;
                InputWires = OutputWires;
                OutputWires = InputWires;
            }
        }

        /// <summary>
        /// Swap inputs and outputs. Call this function if you find
        /// that there are more outputs than inputs, or if you find
        /// that a symbol with equal numbers of inputs/outputs is invalid.
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
        
        #region Helper functions

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
    }
}
