using System;
using System.Collections.Generic;
using System.Text;
using Sketch;
using Utilities;

namespace Clusters
{
    [Serializable]
    public class Cluster
    {
        #region Member Variables

        Guid m_Id;

        System.Drawing.RectangleF m_BoundingBox;

        List<Substroke> m_Strokes;

        SortedList<double, Substroke> m_DistanceToClosestStrokes;

        Dictionary<Substroke, List<SubstrokeDistance>> m_AllDistances;

        string m_Class;

        ClusterScore m_Score;

        List<ClusterModification> m_Modifications;

        bool m_IsParent = true;

        Cluster m_ParentCluster;

        List<Cluster> m_Children;

        #endregion

        #region Constructors

        public Cluster(Substroke s, string className, Dictionary<Substroke, List<SubstrokeDistance>> distances)
        {
            m_Id = Guid.NewGuid();
            m_Strokes = new List<Substroke>();
            m_Strokes.Add(s);
            m_BoundingBox = Compute.BoundingBox(m_Strokes);
            m_Class = className;
            m_AllDistances = distances;
            m_DistanceToClosestStrokes = FindClosestStrokes(m_Strokes, m_AllDistances);
            m_Modifications = new List<ClusterModification>();
            m_Children = new List<Cluster>();
        }

        public Cluster(Substroke s1, Substroke s2, string className, Dictionary<Substroke, List<SubstrokeDistance>> distances)
        {
            m_Id = Guid.NewGuid();
            m_Strokes = new List<Substroke>();
            m_Strokes.Add(s1);
            m_Strokes.Add(s2);
            m_BoundingBox = Compute.BoundingBox(m_Strokes);
            m_Class = className;
            m_AllDistances = distances;
            m_DistanceToClosestStrokes = FindClosestStrokes(m_Strokes, m_AllDistances);
            m_Modifications = new List<ClusterModification>();
            m_Children = new List<Cluster>();
        }

        public Cluster(List<Substroke> strokes, string className, Dictionary<Substroke, List<SubstrokeDistance>> distances)
        {
            m_Id = Guid.NewGuid();
            m_Strokes = new List<Substroke>();
            foreach (Substroke s in strokes)
                m_Strokes.Add(s);
            m_BoundingBox = Compute.BoundingBox(m_Strokes);
            m_Class = className;
            m_AllDistances = distances;
            m_DistanceToClosestStrokes = FindClosestStrokes(m_Strokes, m_AllDistances);
            m_Modifications = new List<ClusterModification>();
            m_Children = new List<Cluster>();
        }

        public Cluster(List<Substroke> strokes, string className, Dictionary<Substroke, List<SubstrokeDistance>> allDistances, 
            List<ClusterModification> allMods, ClusterModification mod)
        {
            m_Id = Guid.NewGuid();
            m_Strokes = new List<Substroke>();
            foreach (Substroke s in strokes)
                m_Strokes.Add(s);
            
            m_Class = className;
            m_AllDistances = allDistances;
            m_DistanceToClosestStrokes = FindClosestStrokes(m_Strokes, m_AllDistances);
            m_Modifications = new List<ClusterModification>();
            foreach (ClusterModification m in allMods)
                m_Modifications.Add(m);

            // Make Modification
            if (mod.ModToMake == ClusterModification.Mod.Add && !m_Strokes.Contains(mod.Stroke))
            {
                m_Strokes.Add(mod.Stroke);
                m_Modifications.Remove(mod);
            }
            else if (mod.ModToMake == ClusterModification.Mod.Remove && m_Strokes.Contains(mod.Stroke))
            {
                m_Strokes.Remove(mod.Stroke);
                m_Modifications.Remove(mod);
            }
            m_BoundingBox = Compute.BoundingBox(m_Strokes);
        }

        public Cluster(List<Substroke> strokes, string className, Dictionary<Substroke, List<SubstrokeDistance>> allDistances,
            List<ClusterModification> allMods, ClusterModification mod, Cluster parent)
        {
            m_Id = Guid.NewGuid();
            m_Strokes = new List<Substroke>();
            foreach (Substroke s in strokes)
                m_Strokes.Add(s);

            m_Class = className;
            m_AllDistances = allDistances;
            m_DistanceToClosestStrokes = FindClosestStrokes(m_Strokes, m_AllDistances);
            m_Modifications = new List<ClusterModification>();
            foreach (ClusterModification m in allMods)
                m_Modifications.Add(m);

            // Make Modification
            if (mod.ModToMake == ClusterModification.Mod.Add && !m_Strokes.Contains(mod.Stroke))
            {
                m_Strokes.Add(mod.Stroke);
                m_Modifications.Remove(mod);
            }
            else if (mod.ModToMake == ClusterModification.Mod.Remove && m_Strokes.Contains(mod.Stroke))
            {
                m_Strokes.Remove(mod.Stroke);
                m_Modifications.Remove(mod);
            }
            m_BoundingBox = Compute.BoundingBox(m_Strokes);
            m_IsParent = false;
            m_ParentCluster = parent;
        }

        #endregion

        #region Methods

        public void FillModificationsListRadius(double radius)
        {
            foreach (Substroke s in m_Strokes)
                m_Modifications.Add(new ClusterModification(s, ClusterModification.Mod.Remove));

            foreach (KeyValuePair<double, Substroke> pair in m_DistanceToClosestStrokes)
            {
                if (pair.Key < radius)
                    m_Modifications.Add(new ClusterModification(pair.Value, ClusterModification.Mod.Add));
            }
        }

        public void FillModificationsListNClosest(int nClosest)
        {
            foreach (Substroke s in m_Strokes)
                m_Modifications.Add(new ClusterModification(s, ClusterModification.Mod.Remove));

            IEnumerator<KeyValuePair<double, Substroke>> counter = m_DistanceToClosestStrokes.GetEnumerator();
            for (int i = 0; i < nClosest && i < m_DistanceToClosestStrokes.Count; i++)
            {
                counter.MoveNext();
                KeyValuePair<double, Substroke> current = counter.Current;
                Substroke stroke = current.Value;
                if (!m_Strokes.Contains(stroke))
                    m_Modifications.Add(new ClusterModification(stroke, ClusterModification.Mod.Add));
            }
        }

        public Dictionary<int, Cluster> GetChildren(Cluster parent, int toDepth, int currentDepth)
        {
            Dictionary<int, Cluster> allChildren = new Dictionary<int,Cluster>(this.m_Modifications.Count * this.m_Modifications.Count);
            //allChildren.Add(this.HashForImageReco, this);
            if (currentDepth < toDepth)
            {
                
                currentDepth++;
                foreach (ClusterModification mod in this.m_Modifications)
                {
                    Cluster child = new Cluster(this.Strokes, this.ClassName, this.m_AllDistances, this.m_Modifications, mod, parent);
                    if (child.Strokes.Count > 0)
                    {
                        int childHash = child.HashForImageReco;
                        if (!allChildren.ContainsKey(childHash))
                            allChildren.Add(childHash, child);

                        Dictionary<int, Cluster> grandChildren = child.GetChildren(parent, toDepth, currentDepth);

                        foreach (KeyValuePair<int, Cluster> entry in grandChildren)
                        {
                            int hash = entry.Key;
                            if (!allChildren.ContainsKey(hash))
                                allChildren.Add(hash, entry.Value);
                        }
                    }
                }
            }

            return allChildren;
        }

        public List<Cluster> GetChildren2(Cluster parent, int toDepth, int currentDepth)
        {
            List<Cluster> children = new List<Cluster>(this.m_Modifications.Count * this.m_Modifications.Count);

            if (currentDepth >= toDepth)
                return children;

            
            currentDepth++;
            foreach (ClusterModification mod in this.m_Modifications)
                children.Add(new Cluster(this.Strokes, this.ClassName, this.m_AllDistances, this.m_Modifications, mod, parent));

              

            children.Add(parent);
            return children;
        }

        public void AddStroke(Substroke stroke)
        {
            m_Strokes.Add(stroke);
            
            // I am removing the stroke from the list, so that I don't attempt to 
            // add the same stroke later
            if (m_DistanceToClosestStrokes.ContainsValue(stroke))
            {
                int index = m_DistanceToClosestStrokes.IndexOfValue(stroke);
                m_DistanceToClosestStrokes.RemoveAt(index);
            }

            // Should I update the list of closest strokes?? No...for now
            // I don't think I should update the list, because I want to expand
            // the cluster based on the original closest strokes. 
        }

        public void RemoveStroke(Substroke stroke)
        {
            if (m_Strokes.Contains(stroke))
                m_Strokes.Remove(stroke);
        }

        private SortedList<double, Substroke> FindClosestStrokes(List<Substroke> strokesInCluster, Dictionary<Substroke, List<SubstrokeDistance>> distances)
        {
            SortedList<double, Substroke> closeStrokes = new SortedList<double, Substroke>();

            foreach (Substroke stroke in strokesInCluster)
            {
                try
                {
                    if (distances.ContainsKey(stroke))
                    {
                        foreach (SubstrokeDistance d in distances[stroke])
                        {
                            Substroke otherStroke;
                            if (stroke == d.StrokeA)
                                otherStroke = d.StrokeB;
                            else if (stroke == d.StrokeB)
                                otherStroke = d.StrokeA;
                            else
                            {
                                otherStroke = d.StrokeA;
                                Console.WriteLine("You shouldn't get here!! FindClosestStrokes fcn");
                            }

                            if (!m_Strokes.Contains(otherStroke))
                            {
                                double min = d.Min;
                                while (closeStrokes.ContainsKey(min))
                                    min += 0.00000000001;

                                closeStrokes.Add(min, otherStroke);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Cluster FindClosestStrokes: " + e.Message);
                }
            }

            SortedList<double, Substroke> noDuplicates = new SortedList<double, Substroke>();
            foreach (KeyValuePair<double, Substroke> distance in closeStrokes)
                if (!noDuplicates.ContainsValue(distance.Value))
                    noDuplicates.Add(distance.Key, distance.Value);


            return noDuplicates;
        }

        

        #endregion

        #region Getters / Queries

        public bool Contains(Substroke s)
        {
            return m_Strokes.Contains(s);
        }

        public List<Substroke> Strokes
        {
            get { return m_Strokes; }
        }

        public System.Drawing.RectangleF BoundingBox
        {
            get { return m_BoundingBox; }
        }

        public Guid Id
        {
            get { return m_Id; }
        }

        public bool IsParent
        {
            get { return m_IsParent; }
        }

        public Cluster ParentCluster
        {
            get { return m_ParentCluster; }
        }

        public List<Cluster> Children
        {
            get { return m_Children; }
            set { m_Children = value; }
        }

        public ClusterScore Score
        {
            get { return m_Score; }
            set { m_Score = value; }
        }

        public bool HasBeenScored
        {
            get { return m_Score != null; }
        }

        public string ClassName
        {
            get { return m_Class; }
        }

        public int HashSubstrokes
        {
            get
            {
                int result = 0;
                for (int i = 0; i < m_Strokes.Count; i++)
                    result = result ^ m_Strokes[i].GetHashCode();

                return result;
            }
        }

        public int HashModifications
        {
            get
            {
                int result = 0;
                for (int i = 0; i < m_Modifications.Count; i++)
                    result = result ^ m_Modifications[i].GetHashCode();

                return result;
            }
        }

        public int HashClassName
        {
            get { return m_Class.GetHashCode(); }
        }

        public int HashForImageReco
        {
            get
            {
                int hash1 = HashSubstrokes;
                int hash2 = HashClassName;

                return hash1 ^ hash2;
            }
        }

        #endregion

        #region Static Functions

        public static Cluster MergeClusters(Cluster c1, Cluster c2)
        {
            List<Substroke> strokes = new List<Substroke>();
            foreach (Substroke s in c1.Strokes)
                if (!strokes.Contains(s))
                    strokes.Add(s);
            foreach (Substroke s in c2.Strokes)
                if (!strokes.Contains(s))
                    strokes.Add(s);

            return new Cluster(strokes, c1.ClassName, c1.m_AllDistances);
        }

        public static bool ListContainsEquivalentCluster(List<Cluster> clusters, Cluster c)
        {
            foreach (Cluster current in clusters)
                if (General.ListsHaveSameSubstrokes(current.Strokes, c.Strokes))
                    if (ListOfModificationsIsSame(current.m_Modifications, c.m_Modifications))
                        return true;

            return false;
        }

        private static bool ListOfModificationsIsSame(List<ClusterModification> mods1, List<ClusterModification> mods2)
        {
            if (mods1.Count != mods2.Count)
                return false;

            foreach (ClusterModification mod in mods1)
                if (!mods2.Contains(mod))
                    return false;

            return true;
        }

        public static bool ListContainsClusterWithSameStrokes(List<Cluster> list, Cluster cluster)
        {
            if (list.Contains(cluster))
                return true;

            foreach (Cluster c in list)
                if (General.ListsHaveSameSubstrokes(cluster.Strokes, c.Strokes))
                    return true;

            return false;
        }

        #endregion

    }
}
