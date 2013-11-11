using System;
using System.Collections.Generic;
using System.Text;
using Sketch;

namespace CircuitRec
{
    #region Enums

    /// <summary>
    /// Holds the possible orientations of the symbol.
    /// </summary>
    public enum Orientation
    {
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop
    }

    /// <summary>
    /// The possible polarities of a wire (both global and local/symbol scale)
    /// </summary>
    public enum WirePolarity
    {
        Input,
        Output,
        Internal,
        Unknown
    }

    /// <summary>
    /// Possible types of symbols.
    /// </summary>
    public enum SymbolTypes
    {
        AND,
        NAND,
        OR,
        NOR,
        XOR,
        XNOR,
        NOT,
        BUBBLE,
        NOTBUBBLE,
        BUFFER,
        MUX,
        FLIPFLOP,
        FLIPFLOPEN,
        DECODER,
        FULLADDER,
        RESISTOR,
        CAPACITOR,
        INDUCTOR,
        VOLTAGESRC,
        CURRENTSRC,
        IDIODE,
        ZDIODE,
        IMPEDANCE,
        BJT,
        PMOS,
        NMOS,
        UNKNOWN
    }

    #endregion

    /// <summary>
    /// Abstract class used for bounding boxes (i.e. circuit bounding box, symbol bounding box, etc.)
    /// </summary>
    public class BoundingBox
    {
        #region Internals

        /// <summary>
        /// Minimum (leftmost) X-coordinate of the points provided.
        /// </summary>
        internal int TopLeftX;

        /// <summary>
        /// Minimum (topmost) Y-coordinate of the points provided.
        /// </summary>
        internal int TopLeftY;

        /// <summary>
        /// Maximum (rightmost) X-coordinate of the points provided.
        /// </summary>
        internal int BottomRightX;

        /// <summary>
        /// Maximum (bottommost) Y-coordinate of the points provided.
        /// </summary>
        internal int BottomRightY;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for BoundingBox.
        /// </summary>
        public BoundingBox()
        {
        }

        /// <summary>
        /// Constructor for BoundingBox.
        /// </summary>
        /// <param name="shapes">Shapes to create the bounding box around.</param>
        public BoundingBox(List<Shape> shapes)
        {
            findBoundaries(shapes);
        }

        /// <summary>
        /// Constructor for BoundingBox.
        /// </summary>
        /// <param name="tl_X">The minimum (top-left) X coordinate.</param>
        /// <param name="tl_Y">The minimum (top-left) Y coordinate.</param>
        /// <param name="br_X">The maximum (bottom-right) X coordinate.</param>
        /// <param name="br_Y">The maximum (bottom-right) Y coordinate.</param>
        public BoundingBox(int tl_X, int tl_Y, int br_X, int br_Y)
        {
            TopLeftX = tl_X;
            BottomRightX = br_X;
            TopLeftY = tl_Y;
            BottomRightY = br_Y;
        }

        /// <summary>
        /// Constructor for BoundingBox (increase size of bounding by a chosen amount).
        /// </summary>
        /// <param name="tl_X">The minimum (top-left) X coordinate.</param>
        /// <param name="tl_Y">The minimum (top-left) Y coordinate.</param>
        /// <param name="br_X">The maximum (bottom-right) X coordinate.</param>
        /// <param name="br_Y">The maximum (bottom-right) Y coordinate.</param>
        /// <param name="threshold">Threshold value for increasing the size of the bounding box.</param>
        public BoundingBox(int tl_X, int tl_Y, int br_X, int br_Y, double threshold)
        {
            TopLeftX = (int)(tl_X - (br_X - tl_X) * threshold);
            BottomRightX = (int)(br_X + (br_X - tl_X) * threshold);
            TopLeftY = (int)(tl_Y - (br_Y - tl_Y) * threshold);
            BottomRightY = (int)(br_Y + (br_Y - tl_Y) * threshold);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Finds the boundary coordinates of the BoundingBox.
        /// </summary>
        /// <param name="shapes">The list of shapes that the bounding box is around.</param>
        protected void findBoundaries(List<Shape> shapes)
        {
            this.TopLeftX = Int32.MaxValue;
            this.TopLeftY = Int32.MaxValue;
            this.BottomRightX = Int32.MinValue;
            this.BottomRightY = Int32.MinValue;

            // Go through each shape and find the extreme coordinates of the individual extreme coordinates.
            foreach (Shape shape in shapes)
            {
                int tempMinX = (int)(shape.XmlAttrs.X.Value);
                int tempMinY = (int)(shape.XmlAttrs.Y.Value);
                int tempMaxX = (int)(shape.XmlAttrs.X.Value + shape.XmlAttrs.Width.Value);
                int tempMaxY = (int)(shape.XmlAttrs.Y.Value + shape.XmlAttrs.Height.Value);

                if (tempMinX < TopLeftX)
                    TopLeftX = tempMinX;

                if (tempMinY < TopLeftY)
                    TopLeftY = tempMinY;

                if (tempMaxX > BottomRightX)
                    BottomRightX = tempMaxX;

                if (tempMaxY > BottomRightY)
                    BottomRightY = tempMaxY;
            }
        }

        /// <summary>
        /// Finds the boundary coordinates of the BoundingBox.
        /// </summary>
        /// <param name="shape">The shape that the bounding box is around.</param>
        protected void findBoundaries(Shape shape)
        {
            this.TopLeftX = (int)(shape.XmlAttrs.X.Value);
            this.TopLeftY = (int)(shape.XmlAttrs.Y.Value);
            this.BottomRightX = (int)(shape.XmlAttrs.X.Value + shape.XmlAttrs.Width.Value);
            this.BottomRightY = (int)(shape.XmlAttrs.Y.Value + shape.XmlAttrs.Height.Value);
        }

        /// <summary>
        /// An overloaded method, determines the intersection count of Points in a bounding box.
        /// </summary>
        /// <param name="TL_X">The topleft X coordinate of the bounding box</param>
        /// <param name="TL_Y">The topleft Y coordinate of the bounding box</param>
        /// <param name="BR_X">The bottomright X coordinate of the bounding box</param>
        /// <param name="BR_Y">The bottomright Y coordinate of the bounding box</param>
        /// <param name="points">A list of Points to be checked to see if they intersect with the box</param>
        /// <returns>The number of intersections found with the bounding box</returns>
        protected int intersectCount(double TL_X, double TL_Y, double BR_X, double BR_Y, List<Point> points)
        {
            int pointcount = 0;

            foreach (Point cPoint in points)
                if (cPoint.X < BR_X && cPoint.X > TL_X &&
                    cPoint.Y < BR_Y && cPoint.Y > TL_Y)
                    pointcount++;

            return pointcount;
        }

        /// <summary>
        /// An overloaded method, takes a List of EndPoints and  looks to see how many endpoints lie within a bounding box
        /// of each endpoint.
        /// </summary>
        /// <param name="TL_X">The topleft X coordinate of the bounding box</param>
        /// <param name="TL_Y">The topleft Y coordinate of the bounding box</param>
        /// <param name="BR_X">The bottomright X coordinate of the bounding box</param>
        /// <param name="BR_Y">The bottomright Y coordinate of the bounding box</param>
        /// <param name="points">The List of EndPoint objects</param>
        /// <param name="intersect">An out parameter, stores a List of the intersecting EndPoint objects</param>
        /// <returns>An integer, the number of intersections with the bounding box</returns>
        protected int intersectCount(double TL_X, double TL_Y, double BR_X, double BR_Y,
            List<Sketch.EndPoint> points, out List<Sketch.EndPoint> intersect)
        {
            int pointcount = 0;
            intersect = new List<EndPoint>();

            foreach (EndPoint cPoint in points)
                if (cPoint.X <= BR_X & cPoint.X >= TL_X & cPoint.Y <= BR_Y & cPoint.Y >= TL_Y)
                {
                    intersect.Add(cPoint);
                    pointcount++;
                }

            return pointcount;
        }

        /// <summary>
        /// An overloaded method, takes a List of EndPoints and  looks to see how many endpoints lie within a bounding box
        /// of each endpoint.
        /// </summary>
        /// <param name="TL_X">The topleft X coordinate of the bounding box</param>
        /// <param name="TL_Y">The topleft Y coordinate of the bounding box</param>
        /// <param name="BR_X">The bottomright X coordinate of the bounding box</param>
        /// <param name="BR_Y">The bottomright Y coordinate of the bounding box</param>
        /// <param name="points">The List of EndPoint objects</param>
        /// <param name="intersect">An out parameter, stores a List of the intersecting EndPoint objects</param>
        /// <returns>An integer, the number of intersections with the bounding box</returns>
        protected int intersectCount(double TL_X, double TL_Y, double BR_X, double BR_Y,
            List<Point> points, out List<Point> intersect)
        {
            int pointcount = 0;
            intersect = new List<Point>();

            foreach (Point cPoint in points)
                if (cPoint.X <= BR_X & cPoint.X >= TL_X & cPoint.Y <= BR_Y & cPoint.Y >= TL_Y)
                {
                    intersect.Add(cPoint);
                    pointcount++;
                }

            return pointcount;
        }

        /// <summary>
        /// An overloaded method, used here for connecting wires to wires.  It takes in points from a wire and rotates them to an unrotated
        /// coordinate frame, then uses a rectangular bounding box to see if it falls within a range of the endpoint (also in the rotated
        /// coordinate frame).
        /// </summary>
        /// <param name="theta">The angle of the endpoint's local region.</param>
        /// <param name="t">The threshold value for the long direction of the skinny bounding box.</param>
        /// <param name="epXPrime">The X-coordinate of the endpoint in the rotated coordinate frame.</param>
        /// <param name="epYPrime">The Y-coordinate of the endpoint in the rotated coordinate frame.</param>
        /// <param name="feedback">Amount to scale the bounding box by</param>
        /// <param name="points">A List of Points</param>
        /// <param name="intersect">An out parameters, sotres a List of the intersecting Point objects</param>
        /// <returns>An integer, the number of intersections with the bounding box</returns>
        protected int intersectCount(double theta, double t, double epXPrime, double epYPrime, double feedback,
                                   List<Point> points, out List<Point> intersect)
        {
            // Loop through the points in the wire and see if any fall in the skinny bounding box
            int pointcount = 0;
            intersect = new List<Point>();

            // Find the threshold values for the skinny bounding box
            double maxXPrime = feedback * (epXPrime + t);
            double minXPrime = feedback * (epXPrime - t);
            double maxYPrime = feedback * (epYPrime + 0.2 * t);
            double minYPrime = feedback * (epYPrime - 0.2 * t);

            foreach (Point p in points)
            {
                // Find the rotated coordinates of the point
                double pXPrime = p.X * Math.Cos(theta) - p.Y * Math.Sin(theta);
                double pYPrime = p.X * Math.Sin(theta) + p.Y * Math.Cos(theta);

                if (pXPrime > minXPrime && pXPrime < maxXPrime && pYPrime > minYPrime && pYPrime < maxYPrime)
                {
                    intersect.Add(p);
                    pointcount++;
                }
            }

            return pointcount;
        }

        /// <summary>
        /// Checks if a point is contained within the bounding box.
        /// </summary>
        /// <param name="point">The point to be checked.</param>
        /// <returns>True if the point is contained, false if the point is not contained.</returns>
        public bool Contains(Point point)
        {
            if (point.X > this.TopLeftX && point.X < this.BottomRightX && point.Y > this.TopLeftY && point.Y < this.BottomRightY)
                return true;
            return false;
        }

        #endregion
    }

    /// <summary>
    /// The base class for Wire and Mesh.
    /// </summary>
    public abstract class BaseWire : BoundingBox
    {
        #region Internals

        /// <summary>
        /// Has the value "Input", "Output", or "Internal" based on if the BasicWire is a global input, output, or internal wire.
        /// </summary>
        public WirePolarity IOType;

        /// <summary>
        /// The bus size of the Mesh.
        /// </summary>
        public int Bussize;

        /// <summary>
        /// A List of all the wires in the mesh of the Wire.
        /// </summary>
        public List<Wire> AllConnectedWires;

        /// <summary>
        /// Unique name of the Wire in this sketch.
        /// </summary>
        public string Name;

        /// <summary>
        /// A List of the errors that occured during the mesh creation.
        /// </summary>
        protected List<ParseError> errors;

        #endregion

        #region Getters

        /// <summary>
        /// A List of errors that occured with the BaseWire.
        /// </summary>
        public List<ParseError> Errors
        {
            get
            {
                return this.errors;
            }
        }

        #endregion
    }

    /// <summary>
    /// The base class for SimpleSymbols and ComplexSymbols.
    /// </summary>
    public abstract class BaseSymbol : BoundingBox
    {
        #region Internals

        /// <summary>
        /// The type of the Symbol.
        /// </summary>
        public SymbolTypes SymbType;

        /// <summary>
        /// The shape that is parent to the symbol.
        /// </summary>
        public Shape Shape;

        /// <summary>
        /// The unique name of the Symbol in the sketch.
        /// </summary>
        public string Name;

        /// <summary>
        /// A List of the Meshes that are connected to the Symbol.
        /// </summary>
        public List<Mesh> ConMeshes;

        /// <summary>
        /// A List of all wires connected to the symbol.
        /// </summary>
        public List<Wire> ConnectedWires;

        /// <summary>
        /// A List of the wires that are inputs to the symbol.
        /// </summary>
        public List<Wire> InputWires;

        /// <summary>
        /// A List of the wires that are outputs to the symbol.
        /// </summary>
        public List<Wire> OutputWires;

        /// <summary>
        /// Lists of meshes that are inputs / outputs to the symbol.
        /// </summary>
        public List<Mesh> InputMeshes;
        public List<Mesh> OutputMeshes;


        /// <summary>
        /// The minimum or fixed number of inputs to the Symbol taken from the domain file.
        /// </summary>
        internal int NumInputs;

        /// <summary>
        /// The minimum or fixed number of outputs of the Symbol taken from the domain file.
        /// </summary>
        internal int NumOutputs;

        /// <summary>
        /// True if the number of inputs specified by NumInputs is fixed, false if the number of inputs is a minimum (taken from the domain file).
        /// </summary>
        internal bool InIsFixed;

        /// <summary>
        /// True if the number of outputs specified by NumOutputs is fixed, false if the number of outputs is a minimum (taken from the domain file).
        /// </summary>
        internal bool OutIsFixed;

        /// <summary>
        /// A list of the points in the Symbol.
        /// </summary>
        protected List<Point> points;
        
        /// <summary>
        /// A List of the substrokes in the Symbol
        /// </summary>
        protected List<Substroke> substrokes;

        /// <summary>
        /// This BasicSymbol's orientation
        /// </summary>
        protected Orientation thisOrientation;

        /// <summary>
        /// The BasicSymbol's orientation angle
        /// </summary>
        protected double orientationAngle;

        /// <summary>
        /// A List of errors that occured with the Symbol.
        /// </summary>
        protected List<ParseError> errors;

        #endregion

        #region Methods

        /// <summary>
        /// Used to get the substrokes and points within a Shape.
        /// </summary>
        /// <param name="temp">The shape</param>
        protected void getPoints(Shape temp)
        {
            // Goes through the entire Shape and locates all the points within it
            foreach (Substroke sub in temp.Substrokes)
            {
                this.substrokes.Add(sub);
                foreach (Point p in sub.PointsL)
                    this.points.Add(p);
            }
        }

        /// <summary>
        /// Sets the orientation of the symbol to the same orientation as the shape.
        /// </summary>
        /// <param name="symbol"></param>
        protected void setOrientation(Shape shape)
        {
            thisOrientation = Orientation.LeftToRight;
            orientationAngle = shape.Orient;
        }


        /// <summary>
        ///Calculates and sets the outputMesh data member.
        ///Requires that ConMeshes and OutputWires are already
        ///complete.
        /// </summary>
        public void CalcInputAndOutputMeshes()
        {
            InputMeshes = new List<Mesh>();
            OutputMeshes = new List<Mesh>();

            foreach (Mesh mesh in ConMeshes)
                foreach (Wire wire in mesh.AllConnectedWires)
                {
                    if (InputWires.Contains(wire))
                    {
                        InputMeshes.Add(mesh);
                        break;
                    }
                    else if (OutputWires.Contains(wire))
                    {
                        OutputMeshes.Add(mesh);
                        break;
                    }
                }
        }

        /// <summary>
        /// Require all symbols to have this method so that wires can connect to them.
        /// </summary>
        /// <param name="wires">A list of wires in the sketch</param>
        internal abstract void ConnectWireToSymbol(ref List<Wire> wires);

        /// <summary>
        /// Turns all input wires/meshes into output wires/meshes (and vice versa).
        /// </summary>
        /// <param name="symbols"></param>
        internal abstract void swapConnections();
        
        /// <summary>
        /// Debug method to list all of the connected wires.
        /// </summary>
        internal abstract void ListWires();

        #endregion

        #region Getters and Setters

        /// <summary>
        /// A list of the points in the Symbol.
        /// </summary>
        public List<Point> Points
        {
            get
            {
                return this.points;
            }
        }

        /// <summary>
        /// A List of the substrokes in the Symbol.
        /// </summary>
        public List<Substroke> Substrokes
        {
            get
            {
                return this.substrokes;
            }
        }

        /// <summary>
        /// A List of errors that occured with the Symbol.
        /// </summary>
        public List<ParseError> Errors
        {
            get
            {
                return this.errors;
            }
        }

        #endregion

    }

    /// <summary>
    /// Allows different domains (i.e. digital and analog) to be recognized.  Holds the connection information for the symbols.
    /// </summary>
    public class Domain
    {
        #region Internals

        /// <summary>
        /// Maps the type of the symbol to the connection constraints.
        /// </summary>
        internal Dictionary<SymbolTypes, string[]> SymbolType2ConnectionConstraints;

        /// <summary>
        /// A List of the errors that occur when creating the domain.
        /// </summary>
        private List<ParseError> errors;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new domain object that is used for determing the number of connections to the symbol.
        /// </summary>
        /// <param name="filepath">The filepath of the domain file.</param>
        public Domain(string filepath)
        {
            this.SymbolType2ConnectionConstraints = new Dictionary<SymbolTypes, string[]>();
            this.errors = new List<ParseError>();

            // Parse the domain file
            System.IO.StreamReader sr = new System.IO.StreamReader(filepath);
            string line = sr.ReadLine();
            while (line != null && line != "")
            {
                string[] words = line.Split(' ');
                string[] def = new string[words.Length - 1];
                for (int i = 0; i < def.Length; i++)
                {
                    def[i] = words[i + 1];
                }

                // Find the type of the symbol (need temporary bool since that is what the method outputs)
                // An error will be recorded if there is a symbol name that is not in the SymbolType enum.
                SymbolTypes thisType = new SymbolTypes();
                bool? temp = SymbolFactory.SimpleOrComplexSymbol(words[0], out thisType);

                if (temp == null)
                {
                    errors.Add(new ParseError(null,"The domain file specified an invalid symbol type."));
                }
                else
                {
                    SymbolType2ConnectionConstraints.Add(thisType, def);
                    line = sr.ReadLine();
                }
            }

            sr.Close();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Used to determine the number of connections required for the SimpleSymbol.  Follows visitor pattern.
        /// </summary>
        /// <param name="symb">The SimpleSymbol that will have the number of connections determined.</param>
        internal SimpleSymbol SetConnectionInfoSimple(SimpleSymbol symb)
        {
            // Used to determine the number of connections required for the symbol
            string[] defs = this.SymbolType2ConnectionConstraints[symb.SymbType];
            symb.NumInputs = Int32.Parse(defs[0]);
            symb.NumOutputs = Int32.Parse(defs[1]);
            symb.InIsFixed = (defs[2].Equals("F"));
            symb.OutIsFixed = (defs[3].Equals("F"));

            return symb;
        }

        /// <summary>
        /// Used to determine the number of connection required for a ComplexSymbol.  Follows visitor pattern.
        /// </summary>
        /// <param name="symb">The ComplexSymbol that will have the number of connections determined.</param>
        /// <returns></returns>
        internal ComplexSymbol SetConnectionInfoComplex(ComplexSymbol symb)
        {
            // Used to determine the number of connections required for the symbol
            string[] defs = this.SymbolType2ConnectionConstraints[symb.SymbType];
            symb.NumInputs = Int32.Parse(defs[0]);
            symb.NumOutputs = Int32.Parse(defs[1]);
            symb.InIsFixed = (defs[2].Equals("F"));
            symb.OutIsFixed = (defs[3].Equals("F"));
            symb.NumTop = Int32.Parse(defs[4]);
            symb.NumBottom = Int32.Parse(defs[5]);
            symb.TopIsFixed = (defs[6].Equals("F"));
            symb.BottomIsFixed = (defs[7].Equals("F"));

            return symb;
        }

        #endregion

        #region Getters

        /// <summary>
        /// A List of the errors that occured while creating the domain file.
        /// </summary>
        public List<ParseError> Errors
        {
            get
            {
                return this.errors;
            }
        }

        #endregion

    }

    /// <summary>
    /// Provides a mapping of the label of a symbol shape to the type of the symbol (SimpleSymbol
    /// or ComplexSymbol).  TODO: Update to just use a Dictionary.
    /// </summary>
    public class SymbolFactory
    {
        #region Methods

        /// <summary>
        /// Decides whether a symbol is a SimpleSymbol or a ComplexSymbol based on the type and also assign the type of the symbol.
        /// </summary>
        /// <param name="symboltype">The type of the symbol.</param>
        /// <returns>A boolean that is true if the symbol is a SimpleSymbol and false if the symbol is a ComplexSymbol</returns>
        internal static bool? SimpleOrComplexSymbol(string symboltype, out SymbolTypes symbtypeenum)
        {
            // isSimple is true if the symbol is a SimpleSymbol and is false if the symbol is a ComplexSymbol
            bool? isSimple = null;

            // Digital (potentially not complete)
            if (symboltype == "AND")
            {
                isSimple = true;
                symbtypeenum = SymbolTypes.AND;
            }
            else if (symboltype == "NAND")
            {
                isSimple = true;
                symbtypeenum = SymbolTypes.NAND;
            }
            else if (symboltype == "OR")
            {
                isSimple = true;
                symbtypeenum = SymbolTypes.OR;
            }
            else if (symboltype == "NOR")
            {
                isSimple = true;
                symbtypeenum = SymbolTypes.NOR;
            }
            else if (symboltype == "XOR")
            {
                isSimple = true;
                symbtypeenum = SymbolTypes.XOR;
            }
            else if (symboltype == "XNOR")
            {
                isSimple = true;
                symbtypeenum = SymbolTypes.XNOR;
            }
            else if (symboltype == "NOT")
            {
                isSimple = true;
                symbtypeenum = SymbolTypes.NOT;
            }
            else if (symboltype == "BUBBLE")
            {
                isSimple = true;
                symbtypeenum = SymbolTypes.BUBBLE;
            }
            else if (symboltype == "NOTBUBBLE")
            {
                isSimple = true;
                symbtypeenum = SymbolTypes.NOTBUBBLE;
            }
            else if (symboltype == "BUFFER")
            {
                isSimple = true;
                symbtypeenum = SymbolTypes.BUFFER;
            }
            else if (symboltype == "MUX")
            {
                isSimple = false;
                symbtypeenum = SymbolTypes.MUX;
            }
            else if (symboltype == "FLIPFLOP")
            {
                isSimple = false;
                symbtypeenum = SymbolTypes.FLIPFLOP;
            }
            else if (symboltype == "DECODER")
            {
                isSimple = false;
                symbtypeenum = SymbolTypes.DECODER;
            }
            else if (symboltype == "FULLADDER")
            {
                isSimple = false;
                symbtypeenum = SymbolTypes.FULLADDER;
            }

            // Analog TODO: symboltypes checked may not be correct or complete
            else if (symboltype == "RESISTOR")
            {
                isSimple = true;
                symbtypeenum = SymbolTypes.RESISTOR;
            }
            else if (symboltype == "CAPACITOR")
            {
                isSimple = true;
                symbtypeenum = SymbolTypes.CAPACITOR;
            }
            else if (symboltype == "INDUCTOR")
            {
                isSimple = true;
                symbtypeenum = SymbolTypes.INDUCTOR;
            }
            else if (symboltype == "VOLTAGESRC")
            {
                isSimple = true;
                symbtypeenum = SymbolTypes.VOLTAGESRC;
            }
            else if (symboltype == "CURRENTSRC")
            {
                isSimple = true;
                symbtypeenum = SymbolTypes.CURRENTSRC;
            }
            else if (symboltype == "IDIODE")
            {
                isSimple = true;
                symbtypeenum = SymbolTypes.IDIODE;
            }
            else if (symboltype == "ZDIODE")
            {
                isSimple = true;
                symbtypeenum = SymbolTypes.ZDIODE;
            }
            else if (symboltype == "IMPEDANCE")
            {
                isSimple = true;
                symbtypeenum = SymbolTypes.IMPEDANCE;
            }
            else if (symboltype == "BJT")
            {
                isSimple = false;
                symbtypeenum = SymbolTypes.BJT;
            }
            else if (symboltype == "NMOS")
            {
                isSimple = false;
                symbtypeenum = SymbolTypes.NMOS;
            }
            else if (symboltype == "PMOS")
            {
                isSimple = false;
                symbtypeenum = SymbolTypes.PMOS;
            }

            // Return UNKNOWN if the symbol type is not recognized (isSimple is null)
            else
            {
                symbtypeenum = SymbolTypes.UNKNOWN;
            }

            return isSimple;
        }

        #endregion
    }
}
