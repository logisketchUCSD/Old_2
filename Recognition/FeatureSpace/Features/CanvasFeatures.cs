using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Drawing;

namespace FeatureSpace
{
    /// <summary>
    /// This feature indicates the minimum distance from the stroke to
    /// either the left or right edge of the drawing canvas.
    /// </summary>
    [Serializable]
    public class DistanceToLREdge : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="points">Points in the Stroke</param>
        public DistanceToLREdge(RectangleF sketchBox, RectangleF strokeBox)
            : base("Distance To Left or Right Edge", Scope.Multiple_Dynamic)
        {
            m_Normalizer = 1.0;

            float distL = strokeBox.Left - sketchBox.Left;
            float distR = sketchBox.Right - strokeBox.Right;

            if (distL < distR)
                m_Value = (double)distL / (double)sketchBox.Width;
            else
                m_Value = (double)(sketchBox.Width - distR) / (double)(sketchBox.Width);
        }
    }

    /// <summary>
    /// This feature indicates the minimum distance from the stroke to
    /// either the top or bottom edge of the drawing canvas.
    /// </summary>
    [Serializable]
    public class DistanceToTBEdge : Feature
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="points">Points in the Stroke</param>
        public DistanceToTBEdge(RectangleF sketchBox, RectangleF strokeBox)
            : base("Distance To Top or Bottom Edge", Scope.Multiple_Dynamic)
        {
            m_Normalizer = 1.0;

            float distT = strokeBox.Top - sketchBox.Top;
            float distB = sketchBox.Bottom - strokeBox.Bottom;

            if (distT < distB)
                m_Value = (double)distT / (double)sketchBox.Height;
            else
                m_Value = (double)(sketchBox.Height - distB) / (double)(sketchBox.Height);
        }
    }
}
