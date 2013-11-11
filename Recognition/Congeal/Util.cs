using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using Adrian.PhotoX.Lib;
using System.Runtime.Serialization.Formatters.Binary;

namespace Congeal
{
    /// <summary>
    /// This class contains useful helper methods, mostly for IO
    /// </summary>
    public class Util
    {
        public static string getDir()
        {
            return Directory.GetCurrentDirectory();
        }

        public static Bitmap substrokesToBitmap(int width, int height, List<Sketch.Substroke> ss)
        {
            return new GaussianBlur(1).ProcessImage(
                new SymbolRec.Image.Image(width, height, new SymbolRec.Substrokes(ss)).getThisAsBitmap());
        }

        public static Bitmap substrokesToBitmap(int width, int height, Sketch.Substroke[] ss)
        {
            return new GaussianBlur(1).ProcessImage(
                new SymbolRec.Image.Image(width, height, new SymbolRec.Substrokes(ss)).getThisAsBitmap());
        }

        public static Bitmap sketchToBitmap(int width, int height, Sketch.Sketch sketch)
        {
            SymbolRec.Substrokes substrokes = new SymbolRec.Substrokes(sketch.Substrokes);
            SymbolRec.Image.Image im = new SymbolRec.Image.Image(width, height, substrokes);
            Bitmap bm = im.getThisAsBitmap();
            //Bitmap bm2 = new Bitmap(bm,new Size(width,height));
            GaussianBlur GB = new GaussianBlur(1);
            return GB.ProcessImage(bm);
            //return bm2;
        }

        public static List<Bitmap> sketchToBitmap(int width, int height, List<Sketch.Sketch> sketches)
        {
            List<Bitmap> returnVals = new List<Bitmap>(sketches.Count);
            foreach (Sketch.Sketch s in sketches)
            {
                returnVals.Add(sketchToBitmap(width,height,s));
            }
            return returnVals;
        }

        public static List<Bitmap> getBitmaps(string dir, string pattern)
        {
            List<string> files = getFiles(dir, pattern);
            List<Bitmap> imgList = new List<Bitmap>(files.Count);

            foreach (string filename in files)
            {
                Bitmap bm = new Bitmap(filename);
                imgList.Add(bm);
            }
            return imgList;
        }

        /// <summary>
        /// Gets files in directory dir (and all subdirectories) where the filename matches pattern
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static List<string> getFiles(string dir, string pattern)
        {
            Files.Files fileClass = new Files.Files(new string[] { dir, pattern, "-r" });
            List<string> filenames = fileClass.GetFiles;
            return filenames;
        }

        public static void applyMask(string file1, string file2)
        {
            Bitmap avgImg1 = new Bitmap(file1);
            Bitmap avgImg2 = new Bitmap(file2);
            
            Bitmap mask = new Bitmap(avgImg1.Width, avgImg1.Height);

            for (int i = 0; i < avgImg1.Height; i++)
            {
                for (int j = 0; j < avgImg1.Width; j++)
                {
                    Color p1 = avgImg1.GetPixel(i, j);
                    Color p2 = avgImg2.GetPixel(i, j);
                    int red = Math.Abs(p1.R - p2.R);
                    int blue = Math.Abs(p1.B - p2.B);
                    int green = Math.Abs(p1.G - p2.G);

                    double rprob = Math.Pow(((double)red) / 255, 1);
                    double gprob = Math.Pow((double)green / 255, 1);
                    double bprob = Math.Pow((double)blue / 255, 1);

                    red = (int)(255 * rprob);
                    green = (int)(255 * gprob);
                    blue = (int)(255 * bprob);
                    Color c = Color.FromArgb(255-red, 255-green, 255-blue);
                    mask.SetPixel(i, j, c);

                    red = (int)(rprob * (255-p1.R));
                    green = (int)(gprob * (255-p1.G));
                    blue = (int)(bprob * (255-p1.B));
                    c = Color.FromArgb(255-red, 255-green, 255-blue);
                    avgImg1.SetPixel(i, j, c);

                    red = (int)(rprob * (255-p2.R));
                    green = (int)(gprob * (255-p2.G));
                    blue = (int)(bprob * (255-p2.B));
                    c = Color.FromArgb(255-red, 255-green, 255-blue);
                    avgImg2.SetPixel(i, j, c);
                }
            }
            mask.Save("mask.bmp");
            avgImg1.Save("masked1.bmp");
            avgImg2.Save("masked2.bmp");
        }

        public static void writeText(string path, string text)
        {
            TextWriter tw = new StreamWriter(path);
            // write a line of text (present date/time) to the file
            tw.WriteLine(DateTime.Now);
            // write the rest of the text lines
            tw.Write(text);
            // close the stream
            tw.Close();
        }



    }
}
