using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace FeatureSpace
{
    /// <summary>
    /// This feature indicates whether the stroke is part of a closed path.
    /// Closed paths can be either a single stroke or multiple strokes.
    /// </summary>
    [Serializable]
    public class PartOfClosedPath : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="points">Points in the Stroke</param>
        public PartOfClosedPath(bool partOfClosedPath)
            : base("Part of a Closed Path", Scope.Multiple_Dynamic)
        {
            m_Normalizer = 1.0;

            if (partOfClosedPath)
                m_Value = 1.0;
            else
                m_Value = 0.0;
        }
    }

    /// <summary>
    /// This feature indicates whether the stroke is inside the bounding
    /// box of a closed path. The stroke cannot be part of the closed path.
    /// Closed paths can be either a single stroke or multiple strokes.
    /// </summary>
    [Serializable]
    public class InsideClosedPath : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="points">Points in the Stroke</param>
        public InsideClosedPath(bool insideClosedPath)
            : base("Inside a Closed Path", Scope.Multiple_Dynamic)
        {
            m_Normalizer = 1.0;

            if (insideClosedPath)
                m_Value = 1.0;
            else
                m_Value = 0.0;
        }
    }
}
