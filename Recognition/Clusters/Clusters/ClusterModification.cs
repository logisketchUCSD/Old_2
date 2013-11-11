using System;
using System.Collections.Generic;
using System.Text;
using Sketch;

namespace Clusters
{
    [Serializable]
    public class ClusterModification
    {
        public enum Mod { Add, Remove, None };

        Substroke m_Stroke;

        Mod m_Modification;

        public ClusterModification(Substroke stroke, Mod modification)
        {
            m_Stroke = stroke;
            m_Modification = modification;
        }

        public Substroke Stroke
        {
            get { return m_Stroke; }
        }

        public Mod ModToMake
        {
            get { return m_Modification; }
        }
    }
}
