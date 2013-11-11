using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace FeatureSpace
{
    [Serializable]
    [DebuggerDisplay("{Scope}, {Name}: {Value}")]
    public class Feature// : ISerializable
    {
        public enum Scope { Single, 
            Pair_Static, Pair_Dynamic, 
            Multiple_Static, Multiple_Dynamic };

        #region Member Variables

        protected string m_Name;
        protected double m_Value;
        protected double m_NormalizedValue;
        protected double m_Normalizer;
        protected Scope m_Scope = Scope.Multiple_Static;
        private Guid m_Id;

        #endregion

        #region Constructors

        public Feature()
        {
            m_Name = "Base";
            m_Value = 0.0;
            m_Id = Guid.NewGuid();
        }

        public Feature(string name, Scope scope)
        {
            m_Name = name;
            m_Scope = scope;
            m_Value = 0.0;
            m_Id = Guid.NewGuid();
        }

        public Feature(string name, double value, Scope scope)
        {
            m_Name = name;
            m_Value = value;
            m_Scope = scope;
            m_Id = Guid.NewGuid();
        }

        #endregion

        #region Getters

        public virtual void Normalize()
        {
            m_NormalizedValue = m_Value / m_Normalizer;
        }

        public virtual void SetNormalizer(double value)
        {
            m_Normalizer = value;
        }

        public virtual string Name
        {
            get { return m_Name; }
        }

        public virtual double Value
        {
            get { return m_Value; }
        }

        public virtual double NormalizedValue
        {
            get 
            {
                Normalize();
                return m_NormalizedValue; 
            }
        }

        public Guid Id
        {
            get { return m_Id; }
        }

        public Scope scope
        {
            get { return m_Scope; }
        }

        public bool IsSingle
        {
            get
            {
                if (m_Scope == Scope.Single)
                    return true;
                else
                    return false;
            }
        }

        public bool IsPair
        {
            get
            {
                if (m_Scope == Scope.Pair_Static)
                    return true;
                else
                    return false;
            }
        }

        public bool IsMultiple
        {
            get
            {
                if (m_Scope == Scope.Multiple_Static)
                    return true;
                else
                    return false;
            }
        }

        #endregion

        #region Serialization
        /*
        public Feature(SerializationInfo info, StreamingContext context)
        {
            m_Id = (Guid)info.GetValue("Id", typeof(Guid));
            m_Name = (string)info.GetValue("Name", typeof(string));
            m_Scope = (Scope)info.GetValue("Scope", typeof(Scope));
            m_Value = (double)info.GetValue("Value", typeof(double));
            m_NormalizedValue = (double)info.GetValue("NormalizedValue", typeof(double));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Id", m_Id);
            info.AddValue("Name", m_Name);
            info.AddValue("Scope", m_Scope);
            info.AddValue("Value", m_Value);
            info.AddValue("NormalizedValue", m_NormalizedValue);
        }*/

        #endregion
    }
}
