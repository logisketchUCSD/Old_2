/* JTree.cs
 * 
 * Eric Doi
 * Adapted from MIT class JTree.
 * ==========
 * Created:   Thu Oct 24 14:05:16 2002<br>
 * Copyright: Copyright (C) 2001 by MIT.  All rights reserved.<br>
 * 
 * @author <a href="mailto:calvarad@fracas.ai.mit.edu">christine alvarado</a>
 * @version $Id: JTree.java,v 1.5 2005/01/27 22:14:57 hammond Exp $
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Set;

namespace CRF
{
    public class JTree
    {
        private bool DEBUG = false;

        private Set.HashSet<JTreeNode> m_jTreeNodes;
        private Set.HashSet<JTreeEdge> m_jTreeEdges;
        private JTreeNode m_root;
        private bool m_initialized;
        
        private Dictionary<int, JTreeNode> m_nodeAssignments;

        /* It is useful to have a map from ids to undirectedNodes */
        private Dictionary<int, PotentialNode> m_idMap;

        /* Have we added an evidence since the last time we queried?  If so,
         * then the network is not ready to query until it is propogated */
        private bool m_readyToQuery;

        //Until we find a use for this it remains commented out.
        //   public JTree()
        //   {
        //     m_jTreeNodes = new HashSet();
        //     m_jTreeEdges = new HashSet();
        //     m_initialized = false;
        //     m_idMap = new HashMap();
        //   }

        /// <summary>
        /// Construct the clique tree based on the underlying undirected graph.
        /// For convenience, we include the mapping from the ids to the potential nodes.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="edges"></param>
        /// <param name="idMap"></param>
        /// <param name="flatBN"></param>
        //public JTree(HashSet<JTreeNode> nodes, HashSet<JTreeEdge> edges, Dictionary<int, PotentialNode> idMap, List<PotentialNode> original)
        public JTree(Set.HashSet<JTreeNode> nodes, Set.HashSet<JTreeEdge> edges, Dictionary<int, PotentialNode> idMap)
        {
            m_jTreeNodes = new Set.HashSet<JTreeNode>(nodes);
            m_jTreeEdges = new Set.HashSet<JTreeEdge>(edges);
            m_idMap = new Dictionary<int, PotentialNode>(idMap);
            m_nodeAssignments = new Dictionary<int, JTreeNode>();
            m_root = null;
            m_initialized = false;
            initialize();// (original);
            m_readyToQuery = false; // if no evidence has been entered, maybe we're not ready to query
        }

        public Set.HashSet<JTreeNode> Nodes
        {
            get
            {
                return m_jTreeNodes;
            }
        }

        public Set.HashSet<JTreeEdge> Edges
        {
            get
            {
                return m_jTreeEdges;
            }
        }

        public JTreeNode setRoot()
        {
            if (m_jTreeNodes.Count == 0)
            {
                System.Console.WriteLine("There are no nodes in the JTree.  Cannot set root");
            }
            // Just choose any node to be the root
            foreach (JTreeNode jtn in m_jTreeNodes)
            {
                m_root = jtn;
                return m_root; // just use the first one in the enumerator
            }
            return null;
        }

        private void initialize()//(List<PotentialNode> original)
        {
            m_initialized = true;

            // First assign all of the underlying Undirected nodes to a group
            // in the JTree.
            Set.HashSet<PotentialNode> assigned = new Set.HashSet<PotentialNode>();
            foreach (JTreeNode jtn in m_jTreeNodes) // for each JTNode
            {
                Set.HashSet<PotentialNode> underlyingNodes = jtn.Nodes;
                foreach (PotentialNode un in underlyingNodes) // for each underlying PotNode
                {
                    bool fits = true;
                    // if this node is already in a group, skip it.
                    if (!assigned.Contains(un))
                    {
                        // Get the corresponding node's neighbors, see which ones are actually
                        // in the potential table/varlist.  Then, see if all of *those* are in
                        // the JTNode.  If so, assign it.

                        Set.HashSet<PotentialNode> neighbors = un.getNeighbors();

                        //string ssdebug = "init: node " + un.getId() + " pot:";
                        //
                        //System.Console.WriteLine(ssdebug);
                        //un.getProbTable().print();
                        //System.Console.WriteLine();

                        //foreach (PotentialNode nbr in neighbors)
                        //    if (un.getProbTable().getVariables().Contains(nbr.getId())) ssdebug += (nbr.getId() + " ");
                        //if (DEBUG)
                        //    System.Console.WriteLine(ssdebug);

                        foreach (PotentialNode nbr in neighbors)
                        {
                            // if nbr is represented in the node's potential table
                            if (un.getProbTable().Variables.Contains(nbr.getId()))
                            {
                                if (!underlyingNodes.Contains(nbr)) // then if it's NOT in the JTN
                                {
                                    fits = false;
                                    break;
                                }
                            }
                        }
                        if (fits)
                        {
                            if (m_nodeAssignments == null)
                            {
                                System.Console.WriteLine("node assignments is null");
                            }
                            m_nodeAssignments.Add(un.getId(), jtn);
                            jtn.addToGroup(un);
                            assigned.Add(un);
                        }
                    }
                }
            }
        }

        public void resetEvidence()
        {
            foreach (JTreeNode jtn in m_jTreeNodes)
                jtn.resetEvidence();
        }

        /// <summary>
        /// Takes a dictionary of nodes to observed values and enters the evidence
        /// into the junction tree's nodes
        /// </summary>
        /// <param name="observed"></param>
        public void addEvidence(Dictionary<int, int> observed)
        {
            // Note that we have made a change and are not ready to query until
            // we pass the messages that were caused by this change.
            m_readyToQuery = false;

            // Insert this evidence into the nodes with the BayesNetIds
            foreach (int id in observed.Keys)
            {
                JTreeNode jtn = (JTreeNode)m_nodeAssignments[id];

                jtn.addEvidence(id, observed[id]);
            }
        }

        public ArrayList query(int queryNodeId)
        {
            //if (DEBUG) System.Console.WriteLine("Querying the JTree.  There are " + m_jTreeNodes.Count +
            //       " nodes in the tree");

            // System.Console.WriteLine();
            // if we haven't propogated the evidence since we added it, do that
            // now
            if (!m_readyToQuery)
            {
                if (DEBUG) System.Console.WriteLine("Resetting the JTree");

                // Reset all the nodes and then propogate
                foreach (JTreeNode jtn in m_jTreeNodes)
                {
                    jtn.initialize();
                    if (DEBUG) System.Console.WriteLine( "Potential of " + jtn + " after initialization is " );
                    if (DEBUG) jtn.Potential.print();
                }
                foreach (JTreeEdge jte in m_jTreeEdges)
                {
                    jte.initialize();
                }

                propogate();
            }

            // Find the universe that this node has been assigned to
            JTreeNode thisJtn = (JTreeNode)m_nodeAssignments[queryNodeId];
            return thisJtn.getValues(queryNodeId);
        }

        // Version of query that takes a list of node IDs.  The result of marginalization
        // might change the variable order, so we have to return a Potential object to contain it.
        public Potential query(List<int> queryNodeIds)
        {
            //if (DEBUG) System.Console.WriteLine("Querying the JTree.  There are " + m_jTreeNodes.Count +
            //       " nodes in the tree");

            // System.Console.WriteLine();
            // if we haven't propogated the evidence since we added it, do that
            // now
            if (!m_readyToQuery)
            {
                if (DEBUG) System.Console.WriteLine("Resetting the JTree");

                // Reset all the nodes and then propogate
                foreach (JTreeNode jtn in m_jTreeNodes)
                {
                    jtn.initialize();
                    if (DEBUG) System.Console.WriteLine("Potential of " + jtn + " after initialization is ");
                    if (DEBUG) jtn.Potential.print();
                }
                foreach (JTreeEdge jte in m_jTreeEdges)
                {
                    jte.initialize();
                }

                propogate();
            }

            
            // Find a universe that contains all of these nodes.  If there is none, then return null.
            JTreeNode parent = null;
            foreach (JTreeNode thisJtn in m_jTreeNodes)
            {
                bool containsAll = true;
                foreach (int id in queryNodeIds)
                {
                    if (!thisJtn.containsNode(m_idMap[id]))
                    {
                        containsAll = false;
                        break;
                    }
                }
                if (containsAll)
                {
                    parent = thisJtn;
                    break;
                }
            }
            if (parent != null)
                return parent.getValues(queryNodeIds);
            else
                return null;
        }

        private void propogate()
        {
            if (DEBUG) System.Console.WriteLine("===Beginning to propogate evidence.===");
            m_readyToQuery = true;

            // start at the root and run the propogation algorithm.
            collectEvidence(m_root, null);
            distributeEvidence(m_root, null);

            //LOG.debug( "After evidence is passed the potentials in the JTree are " );
            // Iterator it = m_jTreeNodes.iterator();
            //     while ( it.hasNext() ) {
            //       JTreeNode n = (JTreeNode)it.next();
            //       LOG.debug( n + " has potential " + n.getPotential() );
            //     }

        }

        /// <summary>
        /// Collect nodes from all the directions except the one we just came from.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="entrance"></param>
        /// <returns></returns>
        private JTreeNode collectEvidence(JTreeNode node, JTreeEdge entrance)
        {
            if (node == null)
            {
                throw new Exception("***Can't collect evidence from a null node.***");
                return node;
            }
            else if (DEBUG)
            {
                System.Console.WriteLine("      Collecting evidence from " + node.Id);
            }

            foreach (JTreeEdge e in node.Edges)
            {
                if (! (e == entrance) )
                {
                    update(node, collectEvidence(e.getOtherNode(node), e), e);
                }
            }
            return node;
        }

        private void distributeEvidence(JTreeNode node, JTreeEdge entrance)
        {
            if (node == null)
            {
                throw new Exception("***Can't distribute evidence from a null node!***");
                return;
            }
            else if (DEBUG)
            {
                System.Console.WriteLine("      Distributing evidence from " + node.Id);
            }


            foreach (JTreeEdge e in node.Edges)
            {
                if (! (e == entrance) )
                {
                    JTreeNode child = e.getOtherNode(node);
                    update(child, node, e);
                    distributeEvidence(child, e);
                }
            }
        }

        /// <summary>
        /// Update node1 according to the information in node2.
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        /// <param name="e">The edge between node1 and node2</param>
        private void update(JTreeNode node1, JTreeNode node2, JTreeEdge e)
        {
            // First get the message ready to send to the separtor.
            node2.sendMessage(e);
            e.sendMessage(node1);
        }

        public void print()
        {
            System.Console.WriteLine("Printing Junction Tree");
            System.Console.WriteLine("Edges: ");
            foreach (JTreeEdge e in m_jTreeEdges)
            {
                System.Console.WriteLine(e.ToString());
            }
            System.Console.WriteLine("Nodes: ");
            foreach (JTreeNode n in m_jTreeNodes)
            {
                System.Console.WriteLine(n.ToString());
            }
            System.Console.WriteLine("Done Printing JTree");
        }

        public void printGM() // print for GraphMagics debugging (indexing starts at 1)
        {
            System.Console.WriteLine("Printing Junction Tree for GraphMagics (indexes +1)");
            System.Console.WriteLine("Edges: ");
            foreach (JTreeEdge e in m_jTreeEdges)
            {
                System.Console.WriteLine(e.ToGMString());
            }
            System.Console.WriteLine("Nodes: ");
            foreach (JTreeNode n in m_jTreeNodes)
            {
                System.Console.WriteLine(n.ToGMString());
            }
            System.Console.WriteLine("Done Printing JTree");
        }

    }// JTree

}
