/* JTreeEdge.cs
 * 
 * Eric Doi
 * Adapted from MIT class JTreeEdge.
 * ==========
 * Created:   Thu Oct 24 14:04:21 2002<br>
 * Copyright: Copyright (C) 2001 by MIT.  All rights reserved.<br>
 * 
 * @author <a href="mailto:calvarad@fracas.ai.mit.edu">christine alvarado</a>
 * @version $Id: JTreeEdge.java,v 1.6 2005/01/27 22:14:57 hammond Exp $
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Set;

namespace CRF
{
    public class JTreeEdge : IComparable<JTreeEdge>
    {
        private JTreeNode m_node1;
        private JTreeNode m_node2;
        private int m_weight;
        private Set.HashSet<PotentialNode> m_overlap;

        private Potential m_currentValue;
        private Potential m_updatedValue;

        private int m_id;
        public static int s_current_id = 0;

        public JTreeEdge(JTreeNode node1, JTreeNode node2)
        {
            m_id = s_current_id;
            s_current_id++;
            m_node1 = node1;
            m_node2 = node2;
            m_weight = 0;
            m_overlap = new Set.HashSet<PotentialNode>();
            foreach (PotentialNode un in node1.Nodes)
            {
                if (node2.containsNode(un))
                {
                    m_overlap.Add(un);
                    m_weight++;
                }
            }
            m_currentValue = new Potential();
            m_updatedValue = m_currentValue;
        }

        public Potential CurrentPotential
        {
            get
            {
                return m_currentValue;
            }
        }

        public Potential UpdatedPotential
        {
            get
            {
                return m_updatedValue;
            }
        }

        public int Weight
        {
            get
            {
                return m_weight;
            }
        }

        public JTreeNode getOtherNode(JTreeNode n)
        {
            if (n == m_node1)
            {
                return m_node2;
            }
            else
            {
                return m_node1;
            }
        }

        private class backwardSorter : IComparer<JTreeEdge>
        {
            public int Compare(JTreeEdge a, JTreeEdge b)
            {
                if (a.Weight > b.Weight)
                    return -1;
                else if (a.Weight == b.Weight)
                    return 0;
                else
                    return 1;
            }
        }

        public static IComparer<JTreeEdge> sortBackward()
        {
            return new backwardSorter();
        }

        public int CompareTo(JTreeEdge jte)
        {
            if (jte.Weight < this.Weight)
            {
                return 1;
            }
            else if (jte.Weight == this.Weight)
            {
                return 0;
            }
            else return -1;
        }

        public JTreeNode Node1
        {
            get
            {
                return m_node1;
            }
        }

        public JTreeNode Node2
        {
            get
            {
                return m_node2;
            }
        }

        public Set.HashSet<PotentialNode> SeparatorNodes
        {
            get
            {
                return new Set.HashSet<PotentialNode>(m_overlap);
            }
        }

        public void receiveMessage(Potential p)
        {
            m_currentValue = m_updatedValue;
            m_updatedValue = p;
        }

        /// <summary>
        /// Send a messsage to the node passed in.
        /// </summary>
        /// <param name="node"></param>
        public void sendMessage(JTreeNode node)
        {
            // System.Console.WriteLine( "Sending a message from " + this + " to " + node );
            // First prepare the message to sent.
            // System.Console.WriteLine( "Before division potentials are: " + m_updatedValue
            //                         + " and " + m_currentValue );
            Potential p = m_updatedValue.divideBy(m_currentValue);
            // System.Console.WriteLine( "After division potentials are: " + m_updatedValue
            //                         + " and " + m_currentValue );
            // System.Console.WriteLine( "New value is " + p );
            node.receiveMessage(p);
        }

        public void initialize()
        {
            m_currentValue = new Potential();
            m_updatedValue = new Potential();
        }
        
        public override String ToString()
        {
            String ret = "JTreeEdge between: [" + m_node1.Id + "] and [" + m_node2.Id + "]";
            return ret;
        }

        public String ToGMString() // just for Graph Magics debugging
        {
            String ret = "JTreeEdge between: [" + (m_node1.Id+1) + "] and [" + (m_node2.Id+1) + "]";
            return ret;
        }
    }// JTreeEdge

}