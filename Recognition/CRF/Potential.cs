/* Potential.cs 
 *
 * Eric Doi
 * Adapted from MIT code.
 * ==========
 * This is meant to represent the potential that exists within
 * a junction tree.  After message passing these potentials will
 * be the joint probability of the variables included in them.
 * In general, they may be anything.
 *
 * Created:   Wed Oct 30 17:59:51 2002<br>
 * Copyright: Copyright (C) 2001 by MIT.  All rights reserved.<br>
 * 
 * @author <a href="mailto:calvarad@fracas.ai.mit.edu">christine alvarado</a>
 * @version $Id: Potential.java,v 1.7 2005/01/27 22:14:58 hammond Exp $
 **/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CRF
{
    public class Potential
    {
        public bool DEBUG = false;

        /**  basically a nested table for each variable in terms of the others? */
        private ArrayList m_table;

        /** What level is each variable stored at in the Table */
        private List<int> m_variableLevel;

        private Hashtable m_evidence;

        public Potential()
        {
            m_variableLevel = new List<int>();
            m_evidence = new Hashtable();
            m_table = new ArrayList();
        }

        public Potential(ArrayList values, List<int> variables)
        {
            m_table = values;
            m_variableLevel = new List<int>(variables);
            m_evidence = new Hashtable();
        }

        public Potential multiply(Potential pot)
        {
            // If this is the first thing we are multiplying, then the value just
            // becomes the same.
            // This makes the tables point to the same object.  This just means that
            // we can't modify the values in the actual tables, but should copy
            // them if we change anything.  Is this an OK assumption?
            if (m_variableLevel.Count == 0)
            {
                ArrayList table = pot.Table;
                List<int> levels = new List<int>(pot.Variables);
                return new Potential(table, levels);
            }
            if (pot.Variables.Count == 0)
            {
                return new Potential(m_table, m_variableLevel);
            }
            else
            {
                // Match up any variables appearing in both tables, and
                // expand the table to include any new variables.
                //LOG.debug( "Multiplying " + this + " by " + pot + " at top level" );
                return multiplyHelper(m_table, pot.Table, m_variableLevel,
                              pot.Variables);
            }
        }

        public Potential divideBy(Potential pot)
        {
            if (m_variableLevel.Count == 0)
            {
                // Assume the potential is filled with ones and
                // just divide everything by one.
                return new Potential(invertTable(pot.Table), pot.Variables);
            }
            else if (pot.Variables.Count == 0)
            {
                return this;
            }
            else
            {
                // Make sure both tables are the same size, i.e. the same variables.
                List<int> other_vars = pot.Variables;
                List<int> missing1 = new List<int>(other_vars);
                List<int> missing2 = new List<int>(m_variableLevel);

                foreach (int i in new List<int>(missing1)) // remove all vars in m_variableLevel from missing1
                    if (m_variableLevel.Contains(i)) missing1.Remove(i);
                foreach (int i in new List<int>(missing2)) // etc
                    if (other_vars.Contains(i)) missing2.Remove(i);

                if ( !(missing1.Count == 0) || !(missing2.Count == 0) )
                {
                    if (DEBUG) System.Console.Out.WriteLine("Adjusting the size of potentials to do division.");
                }

                ArrayList newTable1 = m_table;
                ArrayList newTable2 = pot.Table;
                ArrayList other_table = pot.Table;
                ArrayList tempTable;
                List<int> newVariables1 = new List<int>(m_variableLevel);
                List<int> newVariables2 = new List<int>(pot.Variables);
                foreach (int id in missing1)
                {
                    // See how big the table is at the depth we need.
                    int index = other_vars.IndexOf(id);
                    ArrayList list = (ArrayList)other_table[0];

                    /* *** HUH?  I don't get this code here... ***
				    while (index > 0)
				    {
					    list = (ArrayList)other_table[0];
					    index--;
				    }
                    */

                    int length = list.Count;
                    // now expand the table by duplicating it this many times.
                    tempTable = newTable1;
                    newTable1 = new ArrayList(length);
                    for (int i = 0; i < length; i++)
                    {
                        newTable1.Add(deepListCopy(tempTable));
                    }
                    newVariables1.Insert(0, id);
                }

                foreach (int id in missing2)
                {
                    // See how big the table is at the depth we need.
                    int index = m_variableLevel.IndexOf(id);
                    ArrayList list = (ArrayList)m_table[0];

                    /* ???
                    while (index > 0)
                    {
                        list = (ArrayList)m_table.get(0);
                        index--;
                    }
                    */
                    int length = list.Count;
                    // now expand the table by duplicating it this many times.
                    tempTable = newTable2;
                    newTable2 = new ArrayList(length);
                    for (int i = 0; i < length; i++)
                    {
                        newTable2.Add(deepListCopy(tempTable));
                    }
                    newVariables2.Insert(0, id);
                }

                Potential p = divideHelper(newTable1, newTable2, newVariables1,
                            newVariables2);
                return p;
            }
        }

        private ArrayList deepListCopy(IList l)
        {
            ArrayList ret = new ArrayList();
            Object obj = l[0];
            if (obj is IList)
            {
                foreach (IList al in l)
                {
                    ret.Add(deepListCopy(al));
                }
            }
            else
            {
                foreach (object o in l)
                {
                    ret.Add(o);
                }
            }
            return ret;
        }

        private ArrayList invertTable(ArrayList l)
        {
            ArrayList ret = new ArrayList();
            foreach (object o in l)
            {
                if (o is ArrayList)
                {
                    ArrayList nextl = (ArrayList)o;
                    ret.Add(invertTable(nextl));
                }
                else
                {
                    Double d = (Double)o;
                    Double n = (1.0 / d);
                    ret.Add(n);
                    if ((d != 1.0) && (n == 1.0))
                    {
                        if (DEBUG) System.Console.Out.WriteLine("WARNING: precision lost due to rounding error");
                    }
                }
            }
            return ret;
        }

        private Potential divideHelper(ArrayList al1, ArrayList al2, List<int> vars1,
                          List<int> vars2)
        {
            // as long as the two lists don't contain the exact same elements.
            if (!vars1.TrueForAll(vars2.Contains) || !vars2.TrueForAll(vars1.Contains))
            {
                if (DEBUG)
                {
                    System.Console.Out.WriteLine("To divide all the same variables must be in both potentials");
                    System.Console.Out.WriteLine(vars1.ToString());
                    System.Console.Out.WriteLine(vars2.ToString());
                }
                return null;
            }
            // get the first variable off the first list.
            // If there's only one left, then we are in the base case
            if (vars1.Count == 1)
            {
                ArrayList ret = new ArrayList(al1.Count);
                for (int i = 0; i < al1.Count; ++i)
                {
                    double d1 = (Double)al1[i];
                    double d2 = (Double)al2[i];
                    if (d2 == 0)
                    {
                        if (d1 == 0)
                        {
                            ret.Add(0.0);
                        }
                        else if (DEBUG) System.Console.Out.WriteLine("We can't divide a non zero number by 0.  d1 = {0}, d2 = {1}", d1, d2);
                    }
                    else
                    {
                        double nd = d1 / d2;
                        if ((nd == 0.0) && (d1 != 0.0))
                        {
                            if (DEBUG) System.Console.Out.WriteLine("WARNING: precision lost due to rounding error");
                        }
                        if ((nd == 1.0) && (d1 != d2))
                        {
                            if (DEBUG) System.Console.Out.WriteLine("WARNING: precision lost due to rounding error");
                        }
                        ret.Add(nd);
                    }
                }
                Potential ret_p = new Potential(ret, new List<int>(vars1));
                return ret_p;
            }
            else
            {
                List<int> nextVars1 = new List<int>(vars1);
                int id1 = nextVars1[0];
                nextVars1.RemoveAt(0);
                int index = vars2.IndexOf(id1);

                Potential ret;

                // Then the element is a part of table2 so take a slice of it.
                List<int> nextVars2 = new List<int>(vars2);
                nextVars2.RemoveAt(index);

                int valindex = 0;
                Potential nextp = new Potential();
                ArrayList retTable = new ArrayList(al1.Count);

                foreach (ArrayList nexta1 in al1)
                {
                    // Now the tricky part... reduce al2 by the correct dimensions
                    ArrayList nexta2 = reduceMatrix(al2, index, valindex);
                    nextp = divideHelper(nexta1, nexta2, nextVars1, nextVars2);
                    retTable.Add(nextp.Table);

                    valindex++;
                }

                List<int> retVars = new List<int>();

                retVars.Add(id1);
                retVars.AddRange(nextp.Variables);

                ret = new Potential(retTable, retVars);

                return ret;
            }
        }

        /// <summary>
        /// Sum out all the variables except those in the variable list
        /// </summary>
        /// <param name="variables"></param>
        /// <returns></returns>
        public Potential marginalize(List<int> variables)
        {
            List<int> getrid = new List<int>(m_variableLevel);
            List<int> varlist_copy = new List<int>(m_variableLevel);
            List<int> varlist_copy2 = new List<int>(m_variableLevel);
            List<int> extras = new List<int>(variables);

            // Get rid of any variables from the list that aren't really in this
            // potential.  We iterate through copies of the lists in order to prevent
            // complaining about modification of the collection while looping
            foreach (int id in new List<int>(extras)) if (m_variableLevel.Contains(id)) extras.Remove(id);
            foreach (int id in new List<int>(variables)) if (extras.Contains(id)) variables.Remove(id);
            foreach (int id in new List<int>(getrid)) if (variables.Contains(id)) getrid.Remove(id);
            foreach (int id in new List<int>(varlist_copy2)) if (getrid.Contains(id)) varlist_copy2.Remove(id);

            ArrayList newTable = m_table;
            // Sum out over all the remaining variables
            foreach (int id in getrid)
            {
                int index = varlist_copy.IndexOf(id);
                if (index < 0)
                {
                    if (DEBUG) System.Console.Out.WriteLine(id + " is not in the potential to be summed out ");
                    if (DEBUG) System.Console.Out.WriteLine("the valid IDs are " + m_variableLevel);
                }

                newTable = sumOut(newTable, index);
                varlist_copy.RemoveAt(index);

            }

            return new Potential(newTable, varlist_copy2);
        }

        private ArrayList sumOut(ArrayList table, int depth)
        {
            ArrayList ret = new ArrayList(table.Count);

            if (depth == 0)
            {
                return sumTables(table, 0);
            }
            foreach (ArrayList next in table)
            {
                ArrayList summed = sumOut(next, depth - 1);
                if (summed.Count == 1) 
                {
                    ret.AddRange(summed);
                }
                else
                {
                    ret.Add(summed);
                }
            }
            return ret;
        }

        private ArrayList sumTables(ArrayList tables, int index)
	    {
		    ArrayList ret = new ArrayList();
		    double total = 0;

		    if (index < tables.Count)
		    {
			    Object o = tables[index];
			    if (o is ArrayList)
			    {
				    ret = sumTwoTables((ArrayList)o, sumTables(tables, index + 1));
			    }
			    else
			    {
				    foreach (double d in tables)
				    {
                        total += d;
				    }
				    ret.Add(total);
			    }

		    }

		    return ret;
	    }

        private ArrayList sumTwoTables(ArrayList first, ArrayList second)
	    {
		    ArrayList ret = new ArrayList();
		    if (second.Count == 0)
		    {
			    return first;
		    }
		    else
		    {
                for (int i = 0; (i < first.Count && i < second.Count); ++i)
			    {
				    Object o1 = first[i];
				    Object o2 = second[i];
				    if (o1 is ArrayList)
				    {
					    ret.Add(sumTwoTables((ArrayList)o1, (ArrayList)o2));
				    }
				    else
				    {
					    double d1 = (Double)o1;
					    double d2 = (Double)o2;
					    ret.Add(d1 + d2);
				    }
			    }
			    return ret;
		    }
	    }



        /** Zeros out all the entries *not* in the list of values
       *  This is not the most efficient way to do things, but I have
       * GOT to get something working and quick!  This was the easiest to
       * understand.
       * XXX: I am not doing it this way, because I think that just multiplying
       * by an evidence vector does the same thing.
      */
        //   private ArrayList zeroOut( ArrayList table, int index, List values )
        //   {
        //     ArrayList ret = new ArrayList( table );
        //     if ( index == 0 ) {
        //       Iterator it = table.iterator();
        //       Integer pos = new Integer( 0 );
        //       while ( it.hasNext() ) {
        // 	Object o = it.next();
        // 	if ( o instanceof ArrayList ) {
        // 	  if ( values.contains( pos ) ) {
        // 	    ret.add( o );
        // 	  }
        // 	  else {
        // 	    ret.add( zeroAll( (ArrayList)o ) );
        // 	  }
        // 	}
        // 	else {
        // 	  if ( values.contains( pos ) ) {
        // 	    ret.add( o );
        // 	  }
        // 	  else {
        // 	    ret.add( new Double( 0 ) );
        // 	  }
        // 	}
        // 	pos = new Integer( pos.intValue() + 1 );
        //       }
        //     }
        //     else {
        //       Iterator it = table.iterator();
        //       while ( it.hasNext() ) {
        // 	ret.add( zeroOut( (ArrayList)it.next(), index - 1, values ) );
        //       }
        //     }
        //     return ret;
        //   }

        /// <summary>
        /// Return a table the same size as the one passed in but with all zeros.
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        private ArrayList zeroAll(ArrayList l)
	    {
			ArrayList ret = new ArrayList();
            foreach (Object o in l)
		    {
				if (o is ArrayList)
			    {
				    ret.Add(zeroAll((ArrayList)o));
			    }
			    else
			    {
				    ret.Add(0.0);
			    }
		    }
		    return ret;
	    }

        /// <summary>
        /// Return a new slice of this probability table according to the
        /// evidence we have received.
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Potential enterEvidence(int variable, int value)
        {
            // Figure out how deep in the table this variable is
            int level = m_variableLevel.IndexOf(variable);
            if (level < 0)
            {
                // if the variable is not in the table, then there is no change.
                return this;
            }
            else
            {
                List<int> levels = new List<int>(m_variableLevel);
                levels.RemoveAt(level);

                /* debug
                if (m_variableLevel.Count == levels.Count)
                {
                    System.Console.WriteLine("======================================== THERE WILL BE TROUBLE");
                    System.Console.Write("m_variableLevel, before removal of {0}th var: ", level);
                    foreach (int blah in m_variableLevel)
                        System.Console.Write("{0}, ", blah);
                    System.Console.WriteLine();
                    System.Console.Write("m_variableLevel, after removal: ");
                    foreach (int blah in levels)
                        System.Console.Write("{0}, ", blah);
                }*/
                return new Potential(reduceMatrix(m_table, level, value), levels);
            }
        }

        /// <summary>
        /// Returns a single value of this probability table according to a full
        /// map from variables to labels.  If the evidence map given is not complete,
        /// it will print an error and return -1.0
        /// </summary>
        /// <param name="evidence">A map from variables to labels</param>
        /// <returns></returns>
        public double enterFullEvidence(Dictionary<int, int> evidence)
        {
            Potential reduced = new Potential(this.Table, this.Variables);
            foreach (int var in m_variableLevel)
            {
                if (!evidence.ContainsKey(var))
                {
                    System.Console.Error.WriteLine("Problem getting single probability from JTree: No evidence for node {0}.", var);
                    return -1.0;
                }
                reduced = reduced.enterEvidence(var, evidence[var]);
            }

            if ((reduced.Table[0] is double) && (reduced.Table.Count == 1))
            {
                return (double)(reduced.Table[0]);
            }
            else
            {
                System.Console.Error.WriteLine("Problem getting single probability from JTree: potential table has not been reduced to a single value.");
                return -1.0;
            }
        }

        public Potential normalize()
        {
            ArrayList values = this.Table;

            double normal = normalizeAdder(values);
            ArrayList normalizedValues = normalizeHelper(values, normal);
            Potential ret = new Potential(normalizedValues, this.Variables);

            return ret;
        }

        // just a recursive helper function for the normalize function
        private ArrayList normalizeHelper(ArrayList table, double normal)
        {
            ArrayList ret = new ArrayList();

            // assuming that if the first element is a double, so are the rest
            if (table[0] is double)
            {
                foreach (double val in table)
                {
                    ret.Add(val / normal);
                }
            }
            else // otherwise, recurse
            {
                foreach (object o in table)
                {
                    ret.Add(normalizeHelper((ArrayList)o, normal));
                }
            }
            return ret;
        }

        // Recursively adds of all values in the given table.
        // Just another recursive helper function
        private double normalizeAdder(ArrayList table)
        {
            double total = 0.0;
            // assuming that if the first element is a double, so are the rest
            if (table[0] is double)
            {
                foreach (double val in table)
                {
                    total += val;
                }
                return total;
            }
            else // otherwise, recurse
            {
                foreach (object o in table)
                {
                    total += normalizeAdder((ArrayList)o);
                }
            }
            return total;
        }


        /// <summary>
        /// Get the list of variables in the order in which they appear in this potenetial's multiTable
        /// </summary>
        /// <returns></returns>
        public List<int> Variables
        {
            get
            {
                return new List<int>(m_variableLevel);
            }
        }

        public ArrayList Table
        {
            get
            {
                return m_table;
            }
        }

        private Potential multiplyHelper(ArrayList al1, ArrayList al2, List<int> vars1, List<int> vars2)
        {
            // get the first variable off the first list.
            // If there's only one left, then we are in the base case
            if (vars1.Count == 1 || vars2.Count == 1)
            {
                //LOG.debug( "one of the lists is down to one element" );
                // Figure out which one is down to only one element
                ArrayList end;
                ArrayList other;
                List<int> endVars;
                List<int> otherVars;
                if (vars1.Count == 1)
                {
                    end = al1;
                    other = al2;
                    endVars = vars1;
                    otherVars = vars2;
                }
                else
                {
                    end = al2;
                    other = al1;
                    endVars = vars2;
                    otherVars = vars1;
                }

                int id = endVars[0];
                int index = otherVars.IndexOf(id);
                if (index >= 0)
                {
                    ArrayList ret_list = new ArrayList();
                    int v = 0;
                    foreach (Object o in end)
                    {
                        double val = (Double)o;
                        if (otherVars.Count == 1)
                        {
                            Double d1 = (Double)other[v];
                            Double n = d1 * val;
                            if ((n == 0) &&
                                 !((d1 == 0.0) || (val == 0.0)))
                            {
                                if (DEBUG) System.Console.Out.WriteLine("WARNING: precision lost due to rounding error.");
                            }
                            ret_list.Add(n);
                        }
                        else
                        {
                            ArrayList reduced = reduceMatrix(other, index, v);
                            ret_list.Add(numTimesMatrix(val, reduced));
                        }

                        v++;
                    }
                    List<int> ret_vars = new List<int>(otherVars);
                    int temp = ret_vars[index];
                    ret_vars.RemoveAt(index);
                    ret_vars.Insert(0, temp);
                    return new Potential(ret_list, ret_vars);
                }
                else
                {
                    ArrayList ret_list = new ArrayList();
                    foreach (Object o in end)
                    {
                        Double val = (Double)o;
                        ret_list.Add(numTimesMatrix(val, other));
                    }
                    List<int> ret_vars = new List<int>(endVars);
                    ret_vars.AddRange(otherVars);
                    return new Potential(ret_list, ret_vars);
                }

            }
            else
            {
                List<int> nextVars1 = new List<int>(vars1);
                int id1 = (int)nextVars1[0];
                nextVars1.RemoveAt(0);
                int index = vars2.IndexOf(id1);

                Potential ret;

                if (index < 0)
                {
                    // Then this variable is not in table2 so we need to expand
                    // the table by the values of this variable.
                    ArrayList retTable = new ArrayList(al1.Count);
                    List<int> retVars = new List<int>();
                    Potential nextp = new Potential();
                    foreach (ArrayList nextal1 in al1)
                    {
                        nextp = multiplyHelper(nextal1, al2, nextVars1, vars2);
                        retTable.Add(nextp.Table);
                    }

                    retVars.Add(id1);
                    retVars.AddRange(nextp.Variables);

                    ret = new Potential(retTable, retVars);
                }
                else
                {
                    // Then the element is a part of table2 so take a slice of it.
                    List<int> nextVars2 = new List<int>(vars2);
                    nextVars2.RemoveAt(index);
                    int valindex = 0;
                    Potential nextp = new Potential();
                    ArrayList retTable = new ArrayList(al1.Count);
                    List<int> retVars = new List<int>();

                    foreach (ArrayList nexta1 in al1)
                    {
                        // Now the tricky part... reduce al2 by the correct dimensions
                        //LOG.debug( "Reducing matrix " + al2 + " by dimension" + index );
                        ArrayList nexta2 = reduceMatrix(al2, index, valindex);
                        nextp = multiplyHelper(nexta1, nexta2, nextVars1, nextVars2);
                        retTable.Add(nextp.Table);

                        valindex++;
                    }

                    retVars.Add(id1);
                    retVars.AddRange(nextp.Variables);

                    ret = new Potential(retTable, retVars);
                }

                return ret;
            }
        }

        private ArrayList numTimesMatrix(double num, ArrayList matrix)
	    {
		    ArrayList ret = new ArrayList(matrix.Count);
		    foreach (Object o in matrix)
		    {
			    if (o is ArrayList)
			    {
				    ret.Add(numTimesMatrix(num, (ArrayList)o));
			    }
			    else
			    {
				    Double d = (Double)o;
				    Double n = num * d;
				    if ((n == 0) &&
					     !((d == 0.0) || (num == 0.0)))
				    {
					    if (DEBUG) System.Console.Out.WriteLine("WARNING: precision lost due to rounding error.");
				    }
				    ret.Add(n);
			    }
		    }

		    return ret;
	    }


        private ArrayList reduceMatrix(ArrayList a, int depth, int val)
	    {
            //debug!
            //System.Console.WriteLine("reduce matrix given:");
            //printArrayList(a, 0);
            //System.Console.WriteLine("at depth {0} to value {1}", depth, val);
		    //    LOG.debug( "REDUCEMATRIX: " + a + " by dim " + depth );
		    if (a.Count == 0)
		    {
			    if (DEBUG) System.Console.Out.WriteLine("The list we are reducing is empty");
		    }

		    ArrayList ret = new ArrayList();
		    if (depth > 0)
		    {
			    // The recursive step
			    foreach (ArrayList next in a)
			    {
				    int newdepth = depth - 1;
				    ArrayList returned = reduceMatrix(next, newdepth, val);
				    if (returned.Count == 1)
				    {
					    ret.AddRange(returned);
				    }
				    else
				    {
					    ret.Add(returned);
				    }
			    }
		    }
		    else
		    {
			    // The base case.  We are at the correct depth, now just choose the
			    // correct value
			    Object anew = a[val];
			    if (anew is ArrayList)
			    {
				    ret.AddRange((ArrayList)anew);
			    }
			    else
			    {
				    ret.Add(anew);
			    }
		    }
            //debug!
            //System.Console.WriteLine("reduced! Now:");
            //printArrayList(ret, 0);
		    return ret;
	    }

        public void print()
        {
            string s = "Potential with variable order: ";
            s += "[";
            foreach (int var in m_variableLevel)
                s += (var + " ");
            s += "]";
            s += " and table:";
            System.Console.WriteLine(s);

            printArrayList(m_table, 0);
        }

        public void printArrayList(ArrayList plist, int nestLevel)
        {
            bool isDoublelist = false;
            string start = new String(" ".ToCharArray()[0], nestLevel * 3);

            System.Console.Write(start + "[");

            string line = "";
            foreach (object o in plist)
            {
                if (o is ArrayList)
                {
                    System.Console.WriteLine("");
                    printArrayList((ArrayList)o, nestLevel + 1);
                }
                else
                {
                    isDoublelist = true;
                    line += (double)o;
                    line += " ";
                }
            }
            if (isDoublelist)
            {
                line += "]";
                System.Console.WriteLine(line);
            }
            else
            {
                System.Console.WriteLine(start + "]");
            }
            
        }

    } //Potential

}
