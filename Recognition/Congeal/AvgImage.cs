/// Jason Fennell
/// March 19, 2008
/// 


using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using MathNet.Numerics.LinearAlgebra;

namespace Congeal
{
    /// <summary>
    /// This class is a 'contstruct-as-needed' class that will be used to calculate entropy and liklihoods of ImageTransforms
    /// during training and classification (respectively) for the Congealing algorithm.  Essentially what needs to be done is a bunch
    /// of n by n images (represented as an n by n array of booleans, as pixels can be either on or off) need to be amalgamated into
    /// one 'average' representation that holds the probability of each pixel being on or off.  An instance of the average image class
    /// will be constructed at each instant in time that this information is needed (it will be constantly changing through training
    /// and classifiction) and then can be used to calculate either entropy or the liklihood of an image with respect to the
    /// model given by the average image.
    /// </summary>
    [Serializable]
    public class AvgImage
    {
        private int height;
        private int width;
        private int m_numImgs;
		private Matrix m_avgImg;
		private Matrix cached_distances = null;
        private double entropy = double.PositiveInfinity;

        /// <summary>
        /// Construct an instance of the AvgImage class by passing in the dimension of all the images
        /// and a list of images.  It will calculate the average image upon construction, then will be able to
        /// calculate the entropy of this image or the likelihood of a future ImageTransform.
        /// 
        /// This method assumes that every image is the height and width of the first image passed to it.
        /// </summary>
        /// <param name="imgs"></param>
        public AvgImage(List<ImageTransform> imgs)
        {
            m_numImgs = imgs.Count;
			height = imgs[0].LatentImage.Height;
			width = imgs[0].LatentImage.Width;
			m_avgImg = new Matrix(height, width, 0.0);

			double numImages = (double)imgs.Count;
			if (numImages == 0)
				throw new ArgumentException("imgs must be a non-empty List");
			double incr = 1.0 / numImages;

            foreach (ImageTransform img in imgs)
            {
                for (int row = 0; row < height; ++row)
                {
                    for (int col = 0; col < width; ++col)
                    {
                        if (img.LatentImage[col, row] > 0)
                        {
							m_avgImg[row, col] += incr;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The entropy of the average image is the summed binary entropy of the bernoulli random variables that
        /// represent the probability of each pixel being black (or white).
        /// </summary>
        public double Entropy
        {
            get
            {
                // If we have not calculated entropy before then calculate it, otherwise return the stored value
                if (double.IsInfinity(entropy)) 
                {
                    entropy = 0.0;
                    for (int row = 0; row < height; ++row)
                    {
                        for (int col = 0; col < width; ++col)
                        {
                            double p = m_avgImg[row,col];
                            if ((p == 0.0) || (p == 1.0))
                            {
                                continue; // Binary entropy is 0 for probabilities of 0 and 1
                            }
                            else
                            {
                                entropy += -(p * Math.Log(p, 2)) - ((1 - p) * Math.Log(1 - p, 2));
                            }
                        }
                    }
                }

                return entropy;
            }
        }


        public int Height { get { return height; } }
        public int Width { get { return width; } }

		public SymbolRec.Image.Image Image
		{
			get
			{
				return new SymbolRec.Image.Image(m_avgImg);
			}
		}

        /// <summary>
        /// This is the likelihood of an image I given a model (average image) A, P(I|A).
        /// This can be broken down into the individual pixels,
        ///     P(I_1, I_2, ..., I_n | A_1, ..., A_n)       which are independent of each other given a model for their individual probabilities
        ///   = P(I_1 | A_1, ..., A_n)...P(I_n | A_1, ..., A_n)     and only the model for the pixel in question is relevant
        ///   = P(I_1 | A_1)...P(I_n | A_n)          (***)
        /// 
        /// Since A_i is the probability that an individual pixel is black (or "true" or "on"), this gives us
        ///     P(I_i = Black | A_i) = A_i           (%%%)
        ///     P(I_i = White | A_i) = 1 - A_i       (&&&)
        /// 
        /// The calculation of the likelihood function is the evaluation of (***) using the rules (%%%) and (&&&).
        /// 
        /// PROBLEM : If a single pixel appears white/black where it was ALWAYS black/white in the training data then liklihood is 0...
        /// Can I change this to a sum???
        /// 
        /// TODO: Verify me!!!
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        //public double likelihood(ImageTransform img)
        //{
        //    double likelihood = 1.0;

        //    for (int row = 0; row < height; ++row)
        //    {
        //        for (int col = 0; col < width; ++col)
        //        {
        //            if (img.LatentImage[row][col]) // If the pixel is "on" or "black"
        //            {
        //                //likelihood *= m_avgImg[row][col];
        //                likelihood += m_avgImg[row][col];
        //            }
        //            else // pixel is "off" or "white"
        //            {
        //                //likelihood *= (1 - m_avgImg[row][col]);
        //                likelihood += (1 - m_avgImg[row][col]);
        //            }
        //        }
        //    }

        //    return likelihood;
        //}

        public override string ToString()
        {
            /*string result = "";
            for (int row = 0; row < height; ++row)
            {
                for (int col = 0; col < width; ++col)
                {
                    //result += String.Format("{0:00e+0} ", m_avgImg[row][col]);
                    result += String.Format("{0:0.0} ", m_avgImg[row,col]);
                }
                result += "\n";
            }
            return result;*/
			return m_avgImg.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>A shallow copy of the double[][] representing the average image</returns>
        public double[][] getPixels() { return (double[][])m_avgImg.Clone(); }

        /// <summary>
        /// A testing method that lets us output a grayscale representation of the average image to a file.
        /// 
        /// Change to private or delete in final version
        /// </summary>
        /// <param name="filename">The filename to write to</param>
        public void writeImage(string filename)
        {
			if (!System.IO.Directory.Exists(filename.Substring(0, filename.LastIndexOf("\\"))))
				throw new System.IO.DirectoryNotFoundException("file directory did not exist");
            Bitmap b = new Bitmap(width, height);
            Bitmap stretchedBM = new Bitmap(64, 64);
            for (int i = 0; i < height; ++i)
            {
                for (int j = 0; j < width; ++j)
                {
                    double avgValue = m_avgImg[i,j];

                    // Convert to grayscale
                    avgValue = 1 - avgValue; // Flips white and black
                    avgValue *= 255;
                    int grayValue = (int)avgValue;
                    b.SetPixel(j, i, Color.FromArgb(grayValue, grayValue, grayValue));
                    
                    stretchedBM = new Bitmap(b, stretchedBM.Size);
                }
            }

            stretchedBM.Save(filename, System.Drawing.Imaging.ImageFormat.Png);

            //Used for testing of Gaussian blur:
            //Adrian.PhotoX.Lib.GaussianBlur gb = new Adrian.PhotoX.Lib.GaussianBlur(5);
            //gb.ProcessImage(stretchedBM).Save(filename.Split('.')[0] + "BLUR.bmp");       
            
        }

        /// <summary>
        /// Creates a bitmap of an AvgImage together with the ImageTransform. 
        /// The ImageTransform pixels are colored red.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="t"></param>
        internal void writeImage(string filename, ImageTransform t)
        {
            Bitmap b = new Bitmap(width, height);
            Bitmap stretchedBM = new Bitmap(64, 64);
            for (int i = 0; i < height; ++i)
            {
                for (int j = 0; j < width; ++j)
                {
                    double avgValue = m_avgImg[i,j];
					bool tColored = (t.LatentImage[j, i] > 0.0);
                    double tVal=avgValue;
                    if (tColored)
                    {
                        tVal -= .2;
                        avgValue += .1;
                    }
                    if ( tVal < 0)
                        tVal = 0;
                    if (avgValue > 1)
                        avgValue = 1;

                    tVal = 1 - tVal;
                    tVal *= 255;



                    // Convert to grayscale
                    avgValue = 1 - avgValue; // Flips white and black
                    avgValue *= 255;
                    int grayValue = (int)avgValue;
                    b.SetPixel(j, i, Color.FromArgb((int)tVal, grayValue, grayValue));

                    stretchedBM = new Bitmap(b, stretchedBM.Size);
                    stretchedBM.Save(filename);

                }
            }
        }

		/// <summary>
		/// Calculates the entropy of an ImageTransform
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		internal double getTrainingEntropy(ImageTransform t)
		{
			return getTrainingEntropy(t.LatentImage);
		}

		/// <summary>
		/// Calculates the entropy of a ShapeTransform
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		internal double getTrainingEntropy(ShapeTransform t)
		{
			return getTrainingEntropy(t.LatentImage);
		}

        /// <summary>
        /// Calculates entropy of an ImageTransfom
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        internal double getTrainingEntropy(SymbolRec.Image.Image t)
        {
            double entropy = 0.0;
            for (int row = 0; row < height; ++row)
            {
                for (int col = 0; col < width; ++col)
                {
                    double p = adjustedP(t[col, row], m_avgImg[row,col]);
                    if ((p == 0.0) || (p == 1.0))
                    {
                        continue; // Binary entropy is 0 for probabilities of 0 and 1
                    }
                    else
                    {
                        entropy += -(p * Math.Log(p)) - ((1 - p) * Math.Log(1 - p));
                    }
                }
            }
            return entropy;
        }

		/// <summary>
		/// Calculate the Hausdorff distance between an Image and this AvgImage using cached values
		/// </summary>
		/// <param name="i">The image to calculate the distance to</param>
		/// <returns>The maxmin distance between the images</returns>
		public double HausdorffDistance(SymbolRec.Image.Image i)
		{
			if (cached_distances == null)
			{
				calculate_cached_distances();
			}
			double h = 0.0;
			double distance = 0.0;
			foreach (int[] vec in i.BlackPoints)
			{
				int x = vec[0];
				int y = vec[1];
				distance = cached_distances[y, x];
				if (distance > h)
					h = distance;
			}
			return h;
		}

		/// <summary>
		/// Recalculate the calculated Hausdorff distances
		/// </summary>
		private void calculate_cached_distances()
		{
			cached_distances = new Matrix(height, width);
			for (int y = 0; y < height; ++y)
			{
				for (int x = 0; x < width; ++x)
				{
					double shortest = Double.PositiveInfinity;
					for (int yB = 0; yB < height; ++yB)
					{
						for (int xB = 0; xB < width; ++xB)
						{
							// If the pixel is "on"
							if (m_avgImg[yB, xB] > 0.5)
							{
								double d = Math.Sqrt(Math.Pow(x - xB, 2) + Math.Pow(y - yB, 2));
								if (d < shortest)
									shortest = d;
							}
						}
					}
					cached_distances[y, x] = shortest;
				}
			}
		}

		/// <summary>
		/// Helper method, used for getting probabilities in AvgImage taking the
		/// ImageTransform into account.
		/// </summary>
		/// <param name="b"></param>
		/// <param name="p"></param>
		/// <returns></returns>
		private double adjustedP(double b, double p)
        {
            double db = b;
            double n = (double)m_numImgs;

            return p * (n / (n + 1)) + db / (n + 1);
            
        }


    }
}
