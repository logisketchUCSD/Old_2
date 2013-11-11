/**
 * File: Mesh.cs
 *
 * Authors: Matthew Weiner and Sam Gordon
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2007.
 * 
 */

using System;
using System.Collections.Generic;

namespace CircuitRec
{

    /// <summary>
    /// Class for containing the connected wires' information that will go back to the UI.
    /// </summary>
    public class Mesh : BaseWire
    {
        // TODO: Make getters (and perhaps setters) for all of the internals so that they can be private or internal.

        #region Internals

        /// <summary>
        /// A List of the endpoints of the wires in the Mesh that are connected to wires.
        /// </summary>
        public List<Sketch.EndPoint> EpConToWire;

        /// <summary>
        /// A List of the endpoints of the wires in the Mesh that are connected to symbols.  Right now endpoints of internal wires
        /// that were connected to labels are set back as connected to symbols, even if it was connected to a wire before.
        /// </summary>
        public List<Sketch.EndPoint> EpConToSymb;

        /// <summary>
        /// A List of the endpoints of the wires in the Mesh that are connected to labels.  This will only be for global inputs and
        /// outputs.  Even if the endpoint is connected to a label, EndPtConnect for that endpoint will be false.
        /// </summary>
        public List<Sketch.EndPoint> EpConToLabel;

        /// <summary>
        /// A List of the endpoints of the wires in the Mesh that are not connected.
        /// </summary>
        public List<Sketch.EndPoint> EpUnCon;

        /// <summary>
        /// A List of SimpleSymbols and ComplexSymbols that are connected to the Mesh.
        /// </summary>
        public List<BaseSymbol> ConSymb;

        /// <summary>
        /// A List of labels that are associated with the wires that are a part of the Mesh.
        /// </summary>
        public List<Sketch.Shape> AssociatedLabel;

        /// <summary>
        /// The number of inputs the mesh connects to
        /// </summary>
        private int iCount;

        /// <summary>
        /// The number of outputs the mesh connects to
        /// </summary>
        private int oCount;

        /// <summary>
        /// The input (either a gate or a label) to the mesh.
        /// </summary>
        private BaseSymbol input;

        private double marginWireConn;

        /// <summary>
        /// The shape in the sketch corresponding to this particular mesh.
        /// </summary>
        public Sketch.Shape shape;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for Mesh.
        /// </summary>
        public Mesh(Wire start, double mWireConn)
        {
            // Initialize fields
            // TODO: When bussizes are detected, this needs to be changed from 1 to take in the correct bussize.
            Bussize = 1;
            AllConnectedWires = new List<Wire>();
            EpConToSymb = new List<Sketch.EndPoint>();
            EpConToWire = new List<Sketch.EndPoint>();
            EpConToLabel = new List<Sketch.EndPoint>();
            EpUnCon = new List<Sketch.EndPoint>();
            ConSymb = new List<BaseSymbol>();
            AssociatedLabel = new List<Sketch.Shape>();
            errors = new List<ParseError>();
            marginWireConn = mWireConn;
            input = null;

            AllConnectedWires.Add(start);
        }

        public Mesh(Sketch.Shape mesh, double mWireConn)
        {
            Bussize = 1;
            AllConnectedWires = new List<Wire>();
            EpConToSymb = new List<Sketch.EndPoint>();
            EpConToWire = new List<Sketch.EndPoint>();
            EpConToLabel = new List<Sketch.EndPoint>();
            EpUnCon = new List<Sketch.EndPoint>();
            ConSymb = new List<BaseSymbol>();
            AssociatedLabel = new List<Sketch.Shape>();
            errors = new List<ParseError>();
            marginWireConn = mWireConn;
            input = null;

            foreach (Sketch.Substroke sub in mesh.SubstrokesL)
            {
                Wire wire = new Wire(sub, marginWireConn);
                this.AllConnectedWires.Add(wire);

            }


        }



        /// <summary>
        /// Constructor for Mesh.
        /// </summary>
        public Mesh()
        {
            // Initialize fields
            // TODO: When bussizes are detected, this needs to be changed from 1 to take in the correct bussize.
            Bussize = 1;
            AllConnectedWires = new List<Wire>();
            EpConToSymb = new List<Sketch.EndPoint>();
            EpConToWire = new List<Sketch.EndPoint>();
            EpConToLabel = new List<Sketch.EndPoint>();
            EpUnCon = new List<Sketch.EndPoint>();
            ConSymb = new List<BaseSymbol>();
            AssociatedLabel = new List<Sketch.Shape>();
            errors = new List<ParseError>();
            input = null;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks that a given mesh is still valid in that every wire is connected to everyother wire
        /// This method should be called anytime the mesh is modified to assure validity
        /// </summary>
        /// <returns></returns>
        public bool validMesh()
        {
            foreach (Wire w in this.AllConnectedWires)
            {
                w.ConnectedWires = new List<Wire>();
                w.ConnectWireToWire(this.AllConnectedWires, marginWireConn);
            }
            Wire start = this.AllConnectedWires[0];
            List<Wire> connectedWires = FindConnectedWires(start);
            foreach (Wire w in this.AllConnectedWires)
            {
                if (!connectedWires.Contains(w))
                    return false;
            
            }
            return true;
        }


        /// <summary>
        /// This function finds all wires that are connected (including by transitivity) to w and returns them as a queue
        /// </summary>
        /// <param name="w"></param>
        /// <returns></returns>
        public List<Wire> FindConnectedWires(Wire w)
        {
            List<Wire> queue = new List<Wire>();
            queue.Add(w);
            bool moreadded = true;

            // Finds all of the wires directly and indirectly attached to the current mesh
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
            return queue;
        }
        /// <summary>
        /// Merges two Meshes into one new Mesh.
        /// </summary>
        /// <param name="sw1">The first Mesh to merge.</param>
        /// <param name="sw2">The second Mesh to merge.</param>
        /// <returns>The new merged Mesh.</returns>
        // May not need all these foreach loops separately
        internal static Mesh MergeMeshes(Mesh mesh1, Mesh mesh2)
        {
            Console.WriteLine("Meshes {0} and {1} merging",mesh1.Name,mesh2.Name);
            Mesh newmesh = new Mesh();
            // Merge the connected wires.
            foreach (Wire w in mesh1.AllConnectedWires)
            {
                newmesh.AllConnectedWires.Add(w);
            }
            foreach (Wire w in mesh2.AllConnectedWires)
            {
                newmesh.AllConnectedWires.Add(w);
            }
            // Merge the lists of endpoints that are connected to wires
            foreach (Sketch.EndPoint ep in mesh1.EpConToWire)
            {
                newmesh.EpConToWire.Add(ep);
            }
            foreach (Sketch.EndPoint ep in mesh2.EpConToWire)
            {
                newmesh.EpConToWire.Add(ep);
            }
            // Merge the lists of endpoints that are connected to symbols
            foreach (Sketch.EndPoint ep in mesh1.EpConToSymb)
            {
                newmesh.EpConToSymb.Add(ep);
            }
            foreach (Sketch.EndPoint ep in mesh2.EpConToSymb)
            {
                newmesh.EpConToSymb.Add(ep);
            }
            // Merge the lists of endpoints that are connected to labels
            foreach (Sketch.EndPoint ep in mesh1.EpConToLabel)
            {
                newmesh.EpConToLabel.Add(ep);
            }
            foreach (Sketch.EndPoint ep in mesh2.EpConToLabel)
            {
                newmesh.EpConToLabel.Add(ep);
            }
            // Merge the lists of endpoints that are unconnected
            foreach (Sketch.EndPoint ep in mesh1.EpUnCon)
            {
                newmesh.EpUnCon.Add(ep);
            }
            foreach (Sketch.EndPoint ep in mesh2.EpUnCon)
            {
                newmesh.EpUnCon.Add(ep);
            }
            // Merge the list of connected symbols
            foreach (BaseSymbol s in mesh1.ConSymb)
            {
                if (!newmesh.ConSymb.Contains(s))
                    newmesh.ConSymb.Add(s);
            }
            foreach (BaseSymbol s in mesh2.ConSymb)
            {
                if (!newmesh.ConSymb.Contains(s))
                    newmesh.ConSymb.Add(s);
            }
            // Merge teh list of associated labels
            foreach (Sketch.Shape lab in mesh1.AssociatedLabel)
                newmesh.AssociatedLabel.Add(lab);
            foreach (Sketch.Shape lab in mesh2.AssociatedLabel)
            newmesh.AssociatedLabel.Add(lab);

            // Name and IOType should be the same for both of the wires that are being merged
            newmesh.Name = mesh1.Name;

            // If the mesh types are not the same, record the error and choose the IOType to be the same as
            // mesh1 so that the circuit recognition can continue
            if (mesh1.IOType != mesh2.IOType)
            {
                List<Mesh> errorMeshes = new List<Mesh>();
                errorMeshes.Add(mesh1);
                errorMeshes.Add(mesh2);
                newmesh.errors.Add(new ParseError(errorMeshes, "At least one global input and output are in the same mesh."));
            }
            newmesh.IOType = mesh1.IOType;

            return newmesh;
        }

        /// <summary>
        /// Apply a label to a mesh
        /// </summary>
        /// <param name="cur_mesh"></param>
        /// <returns></returns>
        public bool LabelMesh()
        {
            // Sees if more than one label with a different name is associated with the same mesh, TODO: MAY CAUSE PROBLEMS WITH BUSSES SINCE HAVE
            // SAME FIRST LETTER BUT DIFFERENT AFTERWARDS
            bool error = false;
            foreach (Sketch.Shape lab in this.AssociatedLabel)
            {
                foreach (Sketch.Shape lab2 in this.AssociatedLabel)
                {
                    if (lab.Name != lab2.Name)
                        error = true;

                }
            }
            // If there is an error, just choose the first label to keep the program running
            if (this.AssociatedLabel.Count > 1 && error)
            {
                List<object> errorsLabel = new List<object>();
                errorsLabel.Add(this);
                foreach (Sketch.Shape lab in this.AssociatedLabel)
                    errorsLabel.Add(lab);
                //TODO ADDFEEDBACK
                errors.Add(new ParseError(errorsLabel, "More than one label with different names were given to the same mesh"));
                this.Name = this.AssociatedLabel[0].Name;
            }

            if (this.AssociatedLabel.Count == 1)
                this.Name = this.AssociatedLabel[0].Name;
            return error;
        }

        protected float likelinessMesh()
        {
            
            float basebelief = 0;
            foreach (Wire w in this.AllConnectedWires)
            {
                basebelief += w.Belief/this.AllConnectedWires.Count;
            }
            return basebelief;
        }

        #endregion

        #region Getters

        public int ICount
        {
            get { return iCount; }
            set { iCount = value; }
        }

        public int OCount
        {
            get { return oCount; }
            set { oCount = value; }
        }

        public float Belief
        {
            get { return this.likelinessMesh(); }
        }


        #endregion
    }
}
