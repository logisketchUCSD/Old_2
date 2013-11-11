using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using MathNet.Numerics.LinearAlgebra;

namespace Congeal
{
    /// <summary>
    /// We represent each example symbol internally as an ImageTransform.
    /// This class contains an array representing where the pixels were in the bitmap,
    /// together with the affine transform that is applied to the image during congealing.
    /// </summary>
    public class ImageTransform
    {
        //Color m_backgroundColor;

        #region Internals
		
        private SymbolRec.Image.Image originalImage;
		private SymbolRec.Image.Image latentImage;

        private string m_id;

        private int m_height;
        private int m_width;

		private CongealParameters p;
		private CongealParameters oldP;

		private bool last_was_applied = false;
		private CongealParameters lastAppliedCP = null;

		private Warp warp;


        //private Sketch.Sketch m_sketch;
        //private SymbolRec.Image.Image m_image;
        #endregion

        #region Constructors
        public ImageTransform(int width, int height, Sketch.Sketch sketch)
        {
            m_height = height;
            m_width = width;
            Bitmap bm = Util.sketchToBitmap(width, height,sketch);
            initFromBitmap(bm);
			p = new CongealParameters(height, width);
			oldP = new CongealParameters(p);
        }

        /// <summary>
        /// The constructor converts the bitmap into a 2D array of booleans (to indicate 'on' or 'off' pixels).
        /// It also initializes an inital, identity warp for this image.
        /// </summary>
        /// <param name="bm"></param>
        public ImageTransform(Bitmap bm)
        {
            //m_backgroundColor = new Color();
            m_width = bm.Width;
            m_height = bm.Height;
            initFromBitmap(bm);
			p = new CongealParameters(m_height, m_width);
			oldP = new CongealParameters(p);
        }

        public ImageTransform(Bitmap bm, string id):this(bm)
        {
            m_id = id;
        }

        #endregion

        private void initFromBitmap(Bitmap bm)
        {
			if (bm.Width == 0 || bm.Height == 0)
				throw new Exception("Bitmap must have non-zero dimensions");

			Adrian.PhotoX.Lib.GaussianBlur blur = new Adrian.PhotoX.Lib.GaussianBlur(3);
			blur.ProcessImage(bm);

            m_height = bm.Height;
            m_width = bm.Width;

			originalImage = new SymbolRec.Image.Image(m_height, m_width);
            for (int row = 0; row < m_height; ++row)
            {
                for (int col = 0; col < m_width; ++col)
                {
                    if (isPixelColored(col, row, bm))
                    {
						originalImage[col, row] = Convert.ToDouble(true);
                    }
                }
            }
			warp = new Warp();
            latentImage = originalImage.Clone();
        }


        #region Properties

        public int Width{ get { return m_width; } }
        public int Height { get { return m_height; } }

		/// <summary>
		/// The current parameters acting on this ImageTransform
		/// </summary>
		internal CongealParameters CPs
		{
			get
			{
				return p;
			}
			set
			{
				p = new CongealParameters(value);
				latentImage = warp.warpImage(originalImage);
			}
		}

        public SymbolRec.Image.Image LatentImage
        {
            get
            {
                return latentImage;
            }
        }

		public Warp Warp
		{
			get
			{
				return p.getWarp(Centroid[0], Centroid[1]);
			}
		}

        public string Id
        {
            get { return m_id; }
            set { m_id = value; }
        }

		/// <summary>
		/// Returns {centroid_x, centroid_y}, an array of the positions
		/// of the centroids of this image (calculated by weighted average of the pixels)
		/// </summary>
		public double[] Centroid
		{
			get
			{
				double centroid_x = 0.0;
				double centroid_y = 0.0; 
				int count = 0;
				foreach (int[] bp in latentImage.BlackPoints)
				{
					centroid_x += bp[0];
					centroid_y += bp[1];
					++count;
				}
				if (count == 0)
					return new double[] { m_width / 2.0, m_height / 2.0 };
				return new double[] { centroid_x / count, centroid_y / count };
			}
		}

		/// <summary>
		/// The last set of CongealParameters that somebody tried to apply to this IT
		/// </summary>
		public CongealParameters LastCP
		{
			get
			{
				if (lastAppliedCP == null)
					return new CongealParameters(m_height, m_width);
				return lastAppliedCP;
			}
		}

		/// <summary>
		/// Whether or not we kept the last CP we tried to apply
		/// </summary>
		public bool LastCPWasApplied
		{
			get
			{
				return last_was_applied;
			}
		}

        #endregion

        #region Public Methods

		/// <summary>
		/// Transform this image with new congeal parameters. By default, these
		/// modify the original parameters instead of replacing them.
		/// </summary>
		/// <param name="p"></param>
		public void append(CongealParameters newp)
		{
			lastAppliedCP = newp;
			oldP = new CongealParameters(p);
			p.append(newp);
			warp = p.getWarp(Centroid[0], Centroid[1]);
			latentImage = warp.warpImage(originalImage);
			last_was_applied = true;
		}

        /// <summary>
        /// Transform this image with a new warp.
        /// </summary>
        /// <param name="w"></param>
        public void append(Warp w)
        {
            warp.append(w);
            latentImage = warp.warpImage(originalImage);
        }

		/// <summary>
		/// Append and do not make undoable. In fact, change the ORIGINAL IMAGE!
		/// </summary>
		/// <param name="bp"></param>
		internal void noundo_append(CongealParameters bp)
		{
			originalImage = bp.getWarp(Centroid[0], Centroid[1]).warpImage(originalImage);
			latentImage = originalImage.Clone();
		}


		/// <summary>
		/// Undo the last CongealParameters append
		/// </summary>
        public void undoCPAppend()
        {
			warp = oldP.getWarp(Centroid[0], Centroid[1]);
            latentImage = warp.warpImage(originalImage);
			p = oldP;
			last_was_applied = false;
        }

		/// <summary>
		/// Undo the last new warp.  This is only 'depth one' undo, so it undoes one warp and then
		/// all future calls to this function (until another append is called) do nothing.
		/// </summary>
		public void undoAppend()
		{
			warp.undoAppend();
			latentImage = warp.warpImage(originalImage);
			p = oldP;
		}

        #endregion


        # region utilities

		/// <summary>
		/// Resets the congealing parameters of this image transform by
		/// subtracting off the provided numbers
		/// </summary>
		/// <param name="t_x">X translation</param>
		/// <param name="t_y">Y translation</param>
		/// <param name="x_scale">X scale</param>
		/// <param name="y_scale">Y scale</param>
		/// <param name="x_skew">X skew</param>
		/// <param name="y_skew">Y skew</param>
		/// <param name="theta">Theta</param>
		internal void normalize(double t_x, double t_y, double x_scale, double y_scale, double x_skew, double y_skew, double theta)
		{
			p.t_x -= t_x;
			p.t_y -= t_y;
			p.x_scale -= x_scale;
			p.y_scale -= y_scale;
			p.x_skew -= x_skew;
			p.y_skew -= y_skew;
			p.theta = p.theta - theta;
			oldP = new CongealParameters(p);
			latentImage = p.getWarp(Centroid[0], Centroid[1]).warpImage(originalImage);
		}

        /// <summary>
        /// Warning! only works for the black and red bitmaps.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static bool isPixelColored(int x, int y, Bitmap bitmap)
        {           
            Color c = bitmap.GetPixel(x, y);
            return (c.R > 0);
         
        }

        public override string ToString()
        {
            string result = "";
            for (int row = 0; row < m_height; ++row)
            {
                for (int col = 0; col < m_width; ++col)
                {
					if (latentImage[col, row] != 0.0) // If the pixel is "on"
                    {
                        result += "1 ";
                    }
                    else // pixel is "off"
                    {
                        result += "0 ";
                    }
                }
				result += Environment.NewLine;
            }

            return result;
        }

        /// <summary>
        /// A testing method that lets us output a black and white bitmap of the latent (transformed) image
        /// 
        /// TODO: Change to private or delete in final version
        /// </summary>
        /// <param name="filename">The file to write to</param>
        public void writeImage(string filename)
        {
            Bitmap b = new Bitmap(m_width, m_height);
            for (int i = 0; i < m_height; ++i)
            {
                for (int j = 0; j < m_width; ++j)
                {
                    if (latentImage[j, i] != 0.0)
                    {
                        b.SetPixel(j, i, Color.Black);
                    }
                }
            }
            b.Save(filename);
        }

		/// <summary>
		/// Returns the minimum Hausdorff distance between this ImageTransform
		/// and a set of AvgImages
		/// </summary>
		/// <param name="models"></param>
		/// <returns></returns>
		public double MinHausdorff(List<AvgImage> models)
		{
			double min = Double.PositiveInfinity;
			foreach (AvgImage im in models)
			{
				double distance = Designation.getDistance(this, im, Metrics.ImageDistance.HAUSDORFF);
				//double distance = Designation.getDistance(this, im, Metrics.ImageDistance.TANIMOTO); // TODO: Make me work!
				if (distance < min)
					min = distance;
			}
			return min;
		}

        # endregion
	}
}
