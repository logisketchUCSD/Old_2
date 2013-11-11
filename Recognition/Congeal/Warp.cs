using System;
using System.Collections.Generic;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

namespace Congeal
{
    /// <summary>
    /// Our wrapper class for the built in System.Drawing.Drawing2D.Matrix.
    /// It encodes an affine transform.
    /// </summary>
	[Serializable]
    public class Warp
    {
        private Matrix warp;
		[NonSerialized] private Matrix last_warp = Matrix.Identity(3, 3);
        public string Name;


        /// <summary>
        /// Construct an identity warp.
        /// </summary>
        public Warp()
        {
			warp = Matrix.Identity(3, 3);
        }

        /// <summary>
        /// Construct a warp to scale an image by 'scale' in both x and y directions
        /// </summary>
        /// <param name="scale"></param>
        public Warp(float scale)
        {
			warp = Matrix.Identity(3, 3);
			warp[0, 2] = scale;
			warp[1, 2] = scale;
        }
		
		/// <summary>
		/// Construct a translation warp
		/// </summary>
		/// <param name="t_x">X-translation</param>
		/// <param name="t_y">Y-translation</param>
		public Warp(double t_x, double t_y)
		{
			warp = Matrix.Identity(3, 3);
			warp[0,2] = t_x;
			warp[1,2] = t_y;
		}

        /// <summary>
        /// Construct an arbitrary warp
        /// </summary>
        /// <param name="x_scale">The amount this warp will scale along the x-axis (log).</param>
        /// <param name="y_scale">The amount this warp will scale along the y-axis (log).</param>
        /// <param name="x_skew">The amount this warp will skew along the x-axis.</param>
        /// <param name="y_skew">The amount this warp will skew along the y-axis.</param>
        /// <param name="t_x">The amount this warp will translate along the x-axis.</param>
        /// <param name="t_y">The amount this warp will translate along the y-axis.</param>
		public Warp(double x_scale, double y_scale, double x_skew, double y_skew, double t_x, double t_y)
        {
			x_scale = Math.Exp(x_scale);
			y_scale = Math.Exp(y_scale);
            // See http://www.senocular.com/flash/tutorials/transformmatrix/ for an
            // explanation of affine transformations.
			Matrix transformWarp = Matrix.Identity(3, 3);
			transformWarp[0, 2] = t_x;
			transformWarp[1, 2] = t_y;
			Matrix scaleWarp = Matrix.Identity(3, 3);
			scaleWarp[0, 0] = x_scale;
			scaleWarp[1, 1] = y_scale;
			Matrix xShearWarp = Matrix.Identity(3, 3);
			xShearWarp[0, 1] = x_skew;
			Matrix yShearWarp = Matrix.Identity(3, 3);
			yShearWarp[1, 0] = y_skew;
			warp = transformWarp * scaleWarp * xShearWarp * yShearWarp;
        }

		/// <summary>
		/// Creates a new warp with the given parameters
		/// </summary>
		/// <param name="t_x">X translation</param>
		/// <param name="t_y">Y translation</param>
		/// <param name="theta">Rotation(radians)</param>
		/// <param name="s_x">X scale (log)</param>
		/// <param name="s_y">Y scale (log)</param>
		/// <param name="h_x">X shear</param>
		/// <param name="h_y">Y shear</param>
		/// <param name="centroid_x">X-coordinate to transform around</param>
		/// <param name="centroid_y">Y-coordinate to transform around</param>
		public Warp(double t_x, double t_y, double theta, double s_x, double s_y, double h_x, double h_y,
			double centroid_x, double centroid_y)
		{
			s_x = Math.Exp(s_x);
			s_y = Math.Exp(s_y);
			warp = Matrix.Identity(3, 3);
			if (t_x != 0 || t_y != 0)
			{
				Matrix transformWarp = Matrix.Identity(3, 3);
				transformWarp[0, 2] = t_x;
				transformWarp[1, 2] = t_y;
				warp *= transformWarp;
			}
			if (theta != 0)
			{
				Matrix rotationWarp = Matrix.Identity(3, 3);
				rotationWarp[0, 0] = Math.Cos(theta);
				rotationWarp[0, 1] = -Math.Sin(theta);
				rotationWarp[0, 2] = centroid_x - centroid_x * Math.Cos(theta) + centroid_y * Math.Sin(theta);
				rotationWarp[1, 0] = Math.Sin(theta);
				rotationWarp[1, 1] = Math.Cos(theta);
				rotationWarp[1, 2] = centroid_y - centroid_y * Math.Cos(theta) - centroid_x * Math.Sin(theta);
				warp *= rotationWarp;
			}
			if (s_x != 1.0 || s_y != 1.0)
			{
				Matrix scaleWarp = Matrix.Identity(3, 3);
				scaleWarp[0, 0] = s_x;
				scaleWarp[0, 2] = centroid_x - centroid_x * s_x;
				scaleWarp[1, 1] = s_y;
				scaleWarp[1, 2] = centroid_y - centroid_y * s_y;
				warp *= scaleWarp;
			}
			if (h_x != 0)
			{
				Matrix xShearWarp = Matrix.Identity(3, 3);
				xShearWarp[0, 1] = h_x;
				xShearWarp[0, 2] = -centroid_y * h_x;
				warp *= xShearWarp;
			}
			if (h_y != 0)
			{
				Matrix yShearWarp = Matrix.Identity(3, 3);
				yShearWarp[1, 0] = h_y;
				yShearWarp[1, 2] = -centroid_x * h_y;
				warp *= yShearWarp;
			}
		}

		/// <summary>
		/// Construct an arbitrary warp
		/// </summary>
		/// <param name="w">The 3x3 transform matrix to construct from</param>
		public Warp(Matrix w)
		{
			if (w.ColumnCount != 3 || w.RowCount != 3)
				throw new ArgumentException(String.Format("Invalid matrix dimensions. Expected 3x3, got {0}x{0}", w.RowCount, w.ColumnCount));
			warp = w.Clone();
		}

        /// <summary>
        /// Leaves the original warp unmodified and returns a Matrix.
        /// </summary>
        /// <param name="scalar"></param>
        /// <returns></returns>
        //private Matrix scaleWarp(float scalar)
        //{
        //    Matrix m = this.warp.Clone();
        //    for(int i = 0; i < m.Elements.Length; i++)
        //    {
        //       m.Elements[i] *= scalar;
        //    }
        //    return m;
        //}

		/// <summary>
		/// Apply a warp to the image and return the warped image
		/// </summary>
		/// <param name="pixels"></param>
		/// <returns></returns>
        public SymbolRec.Image.Image warpImage(SymbolRec.Image.Image pixels)
        {
			if (pixels.Width == 0 || pixels.Height == 0)
				throw new Exception("Image to be warped must be non-empty in both dimensions.");

			return pixels.ApplyTransform(warp);
        }

		/// <summary>
		/// Like warpImage, but for a Shape
		/// </summary>
		/// <param name="_s"></param>
		/// <returns></returns>
		public Sketch.Shape warpShape(Sketch.Shape _s)
		{
			Sketch.Shape newS = _s.Clone();
			newS.transform(warp);
			return newS;
		}

        /// <summary>
        /// Compose the current warp with another warp, so that the new warp
        /// will apply the current warp transformation, then the warp transformatoin 
        /// represented by w.
        /// </summary>
        /// <param name="w">Another warp that will be composed onto the current one.</param>
        public void append(Warp w)
        {
            last_warp = warp.Clone();
			warp = w.warp * warp;
        }

        //public void appendLeft(Warp w)
        //{
        //    last_warp = warp.Clone();
        //    w.warp.Multiply(warp,MatrixOrder.Prepend
        //}

		/// <summary>
		/// Append a scaled version of another matrix to this one
		/// </summary>
		/// <param name="w">The matrix to scale and append (not modified)</param>
		/// <param name="scalar">The scalar</param>
        public void appendScaled(Warp w, float scalar)
        {
			append(w * scalar);
        }

		/// <summary>
		/// Append a scaled version of another matrix to this one
		/// </summary>
		/// <param name="w">The matrix to scale and append (not modified)</param>
		/// <param name="scalar">The scalar</param>
        public void appendScaled(Warp w, int scalar)
        {
            appendScaled(w, (float)scalar);
        }

        /// <summary>
        /// Undo exactly one composition.  Note that this is idempotent, so after calling it once
        /// it will not do anything until another append has been called.
        /// </summary>
        public void undoAppend()
        {
            warp = last_warp.Clone();
		}

		#region Accessors

		internal Matrix Matrix
        {
			get
			{
				return warp;
			}
        }


        public Warp Inverse
        {
            get
            {
                Matrix inv = warp.Inverse();
				return new Warp(inv);
            }
		}

		/// <summary>
		/// Determinant of the warp matrix.
		/// </summary>
		public double Det
		{
			get
			{
				return warp.Determinant();
			}
		}

		public bool ContainsNaN
		{
			get
			{
				for (int i = 0; i < warp.RowCount; ++i)
				{
					for (int j = 0; j < warp.ColumnCount; ++j)
					{
						if (Double.IsNaN(warp[i, j]))
							return true;
					}
				}
				return false;
			}
		}

		#endregion

		#region Operators

		/// <summary>
		/// Multiply two warps by multiplying their transform matrices
		/// </summary>
		/// <param name="lhs">The LHS of the multiplication</param>
		/// <param name="rhs">The RHS of the multiplication</param>
		/// <returns>The product</returns>
		public static Warp operator *(Warp lhs, Warp rhs)
		{
			return new Warp(lhs.warp * rhs.warp);
		}

		/// <summary>
		/// Scale the transform matrix of a warp
		/// </summary>
		/// <param name="lhs">The warp</param>
		/// <param name="rhs">The scalar</param>
		/// <returns>The product</returns>
		public static Warp operator *(Warp lhs, float rhs)
		{
			return rhs * lhs;
		}

		/// <summary>
		/// Scale the transform matrix of a warp
		/// </summary>
		/// <param name="lhs">The warp</param>
		/// <param name="rhs">The scalar</param>
		/// <returns>The product</returns>
		public static Warp operator *(float lhs, Warp rhs)
		{
			return new Warp(lhs * rhs.warp);
		}

		public override string ToString()
        {
			return warp.ToString();
		}
		#endregion

        /// <summary>
        /// Finds the distance between two warps, defined by
        ///     K(v, w) = C * exp(-F(a^-1 * b))
        /// where
        ///     F(M) = ||M - I||^2
        /// where I is the identity matrix.
        /// 
        /// I'm not sure if this is a symmetric distance.
        /// </summary>
        /// <param name="a">The LHS of the distance</param>
        /// <param name="b">The RHS of the distance</param>
        /// <returns>The distance</returns>
        public static double distance(Warp a, Warp b)
        {
            double C = 1.0;

            // Get the necessary matrices
			Matrix aminv = a.warp.Inverse();
			Matrix bm = b.warp;
			Matrix product = aminv * bm;

			double norm = (product - Matrix.Identity(product.RowCount, product.ColumnCount)).NormF();

			double F = Math.Pow(norm, 2);

			return C * Math.Exp(-F);
		}

	}
}
