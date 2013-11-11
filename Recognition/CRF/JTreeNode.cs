/* JTreeNode.cs
 * 
 * Eric Doi
 * Adapted from MIT class JTreeNode.
 * ==========
 * Created:   Thu Oct 24 14:03:33 2002<br>
 * Copyright: Copyright (C) 2001 by MIT.  All rights reserved.<br>
 * 
 * @author <a href="mailto:calvarad@fracas.ai.mit.edu">christine alvarado</a>
 * @version $Id: JTreeNode.java,v 1.8 2005/01/27 22:14:57 hammond Exp $
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Set;

namespace CRF
{
    public class JTreeNode
    {

	    private Set.HashSet<PotentialNode> m_nodes;
	    private Set.HashSet<JTreeEdge> m_edges;
	    /** The underlying nodes that have been assigned to this JTree node */
	    private Set.HashSet<PotentialNode> m_includedHere;
	    private Set.HashSet<int> m_observed;

	    private Potential m_potential;

	    private int m_id;

	    public static int s_current_id = 0;

	    public JTreeNode(Set.HashSet<PotentialNode> nodes)
	    {
		    m_id = s_current_id;
		    s_current_id++;
		    m_nodes = new Set.HashSet<PotentialNode>(nodes);
		    m_edges = new Set.HashSet<JTreeEdge>();
		    m_includedHere = new Set.HashSet<PotentialNode>();
		    m_observed = new Set.HashSet<int>();
	    }

	    public Potential Potential
	    {
            get
            {
                return m_potential;
            }
	    }

	    public Set.HashSet<PotentialNode> Nodes
	    {
            get
            {
                return new Set.HashSet<PotentialNode>(m_nodes);
            }
	    }

	    public bool hasSubset(JTreeNode jtn)
	    {
            foreach (PotentialNode n in jtn.Nodes)
            {
                if (!m_nodes.Contains(n))
                    return false;
            }
            return true;
	    }

	    public void addEdge(JTreeEdge e)
	    {
		    m_edges.Add(e);
	    }

	    public bool containsNode(PotentialNode un)
	    {
		    return m_nodes.Contains(un);
	    }

	    public Set.HashSet<JTreeEdge> Edges
	    {
            get
            {
                return new Set.HashSet<JTreeEdge>(m_edges);
            }
	    }

        /* I don't think this function gets used
	    public HashSet<PotentialNode> getChildrenAwayFromEdge(JTreeEdge edge)
	    {
		    HashSet ret = new HashSet();
            foreach(JTreeEdge e1 in m_edges)
		    {
                if (!e1.equals(edge))
			    {
				    JTreeNode n = e1.getOtherNode(this);
				    ret.addAll(n.getChildrenAwayFromEdge(e1));
			    }
		    }
		    return ret;
	    }
        */

	    public void addEvidence(int id, int val)
	    {
		    // Enter this evidence by reducing the size of the prob tables
		    // containing this id
		    m_observed.Add(id);
		    // First find the node with this id.  It is a precondition that
		    // we enter evidence into the JTreeNode that the underlying node
		    // has been assigned to.
		    foreach(PotentialNode un in m_includedHere)
		    {
                if (id == un.getId())
			    {
				    // slice this node's prob table and its neighbors'
				    Set.HashSet<PotentialNode> neighbors = un.getNeighbors();
				    un.observe(id, val);

				    foreach(PotentialNode un2 in neighbors)
                    {
					    un2.observe(id, val);
				    }
				    break;
			    }
		    }

	    }

	    public void addToGroup(PotentialNode un)
	    {
		    m_includedHere.Add(un);
	    }

	    public void resetEvidence()
	    {
            foreach (PotentialNode un in m_includedHere)
		    {
			    un.reset();
		    }
		    m_observed = new Set.HashSet<int>();
	    }

        /// <summary>
        /// We have to initialize this node before it can send or receive messages
        /// if any of the evidence has changed, which I guess it has otherwise
        /// we wouldn't be resending message.
        /// </summary>
	    public void initialize()
	    {
		    //LOG.debug( "Initializing node " + this );
		    m_potential = new Potential();
		    foreach (PotentialNode un in m_includedHere)
		    {
                //LOG.debug( "Adding node " + un + " to JTreeNode " + this );
			    //LOG.debug( "Multiplying " + m_potential + " by " +un.getProbTable() );
			    m_potential = m_potential.multiply(un.getProbTable());
			    //LOG.debug( "Result is " + m_potential );
		    }
	    }

        /// <summary>
        /// Pass a message from this node to the edge e.  This method
        /// changes the value stored in the edge.
        /// </summary>
        /// <param name="e"></param>
	    public void sendMessage(JTreeEdge e)
	    {
		    //LOG.debug( "Sending a message from " + this + " to " + e );
		    Set.HashSet<PotentialNode> seps = e.SeparatorNodes;
		    List<int> overlap = new List<int>();
            
			foreach (PotentialNode un in m_nodes)
		    {
			    if (seps.Contains(un) && !m_observed.Contains(un.getId()))
			    {
				    overlap.Add(un.getId());
			    }
		    }

		    Potential message = m_potential.marginalize(overlap);
		    e.receiveMessage(message);
	    }

	    public void receiveMessage(Potential p)
	    {
		    m_potential = m_potential.multiply(p);
	    }

        /// <summary>
        /// Get the normalized distribution of values for the node in question
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
	    public ArrayList getValues(int id)
	    {
            List<int> singleton = new List<int>();
            singleton.Add(id);
            Potential p = m_potential.marginalize(singleton);
		    ArrayList values = p.Table;
		    ArrayList ret = new ArrayList(values.Count);
		    double total = 0;
            foreach (object o in values)
		    {
                if (o is double)
                {
                    double d = (double)o;
                    total += d;
                }
                else
                    System.Console.Out.WriteLine("JTreeNode.getValues: expected all doubles in marginalized Potential");
		    }
            foreach (object o in values)
            {
                if (o is double)
                {
                    double d = (double)o;
                    ret.Add( (d / total) );
                }
            }
		    return ret;
	    }

        /// <summary>
        /// Get the normalized distribution of values for the nodes in question.
        /// Expects IDs to all be in this jtree node.
        /// Returns a potential because the marginalize function may return a different variable ordering
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Potential getValues(List<int> ids)
        {
            Potential p = m_potential.marginalize(ids);
            //ArrayList values = p.Table;

            //double normal = normalizeAdder(values);
            //ArrayList normalizedValues = normalizeHelper(values, normal);
            Potential ret = p.normalize(); //new Potential(normalizedValues, p.Variables);
            
            // Here I assumeD that the variable order remains unchanged from the list passed in,
            // and just returnED the ArrayList (without p's variable list).
            // But it turns out marginalize can change the variable order.
            /* debug
            System.Console.Write("In jtn get array list... the input var list is: ");
            foreach (int var in ids)
                System.Console.Write("{0}, ", var);
            System.Console.WriteLine();
            System.Console.Write("               but the potential's var list is: ");
            foreach (int var in p.getVariables())
                System.Console.Write("{0}, ", var);
            System.Console.WriteLine();
            */

            return ret;
        }


        public int Id
        {
            get
            {
                return m_id;
            }
        }

	    public override String ToString()
	    {
		    string ret = "";
		    ret += "JTreeNode: ";
		    ret += m_id;
            ret += " with nodes ";
            foreach (PotentialNode node in m_nodes)
            {
                ret += node.getId();
                ret += " ";
            }
		    ret += "  and included nodes: ";
            foreach (PotentialNode inc in m_includedHere)
            {
                ret += inc.getId();
                ret += " ";
            }
		    return ret;
	    }

        public String ToGMString() // Prints for GraphMagics debugging purposes
        {
            string ret = "";
            ret += "JTreeNode: ";
            ret += (Id + 1);
            ret += " with nodes ";
            foreach (PotentialNode node in m_nodes)
            {
                ret += (node.getId() + 1);
                ret += " ";
            }
            ret += "  and included nodes: ";
            foreach (PotentialNode inc in m_includedHere)
            {
                ret += (inc.getId() + 1);
                ret += " ";
            }
            return ret;
        }
    }// JTreeNode

}





