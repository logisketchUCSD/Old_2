/* PotentiaNode.cs
 * 
 * Eric Doi
 * Modified from MIT class UndirectedNode.  Contains a pointer to the original
 * CRF node.
 * ==========
 * Created:   Thu Oct 24 13:57:55 2002<br>
 * Copyright: Copyright (C) 2001 by MIT.  All rights reserved.<br>
 * 
 * @author <a href="mailto:calvarad@fracas.ai.mit.edu">christine alvarado</a>
 * @version $Id: UndirectedNod.java,v 1.2 2003/03/06 01:08:51 moltmans Exp $
 */

using System;
using System.Collections.Generic;
using System.Text;
using Set;

namespace CRF
{
    public class PotentialNode
    {
        private Node originalNode;

	    /** The nodes keep track of their own ids */
	    private int m_id;

        private Set.HashSet<PotentialNode> m_neighbors;
	    private Potential m_probTable;
	    private Potential m_originalProbTable;

        // If no neighbors are given, create a new HashSet.  Neighbors can be added later.
        public PotentialNode(int id, Node parent, Potential probTable)
        {
            originalNode = parent;
            m_id = id;
            m_probTable = probTable;
            m_originalProbTable = probTable;
            m_neighbors = new Set.HashSet<PotentialNode>();
        }

        // Typically, PotentialNodes should be constructed from an id and a parent CRF node.
        // We should be able to figure out the Potentials from the parent, but I'm not sure
        // yet so I'll assume we take it as a parameter
        public PotentialNode(int id, Node parent, Potential probTable, Set.HashSet<PotentialNode> neighbors)
        {
            originalNode = parent;
            m_id = id;
            m_probTable = probTable;
            m_originalProbTable = probTable;
            m_neighbors = new Set.HashSet<PotentialNode>(neighbors);
        }

	    /** Update the prob table to reflect the new evidence
	     *  (take a slice)
	     */
	    public void observe(int id, int val)
	    {
		    m_probTable = m_probTable.enterEvidence(id, val);
	    }

	    public void reset()
	    {
		    m_probTable = m_originalProbTable;
	    }

	    public Potential getProbTable()
	    {
		    return m_probTable;
	    }

	    public void addNeighbors(IEnumerable<PotentialNode> neighbors)
	    {
            foreach (PotentialNode neighbor in neighbors)
		        m_neighbors.Add(neighbor);
	    }

	    public Set.HashSet<PotentialNode> getNeighbors()
	    {
		    return new Set.HashSet<PotentialNode>(m_neighbors);
	    }

	    public int getId()
	    {
		    return m_id;
	    }

	    public String toString()
	    {
		    return ("[PotNode with id:" + m_id + "]");
	    }

    }// PotentialNode
}
