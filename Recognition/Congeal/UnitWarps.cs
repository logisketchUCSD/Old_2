using System;
using System.Collections.Generic;
using System.Text;

namespace Congeal 
{
    /// <summary>
    /// Container class for the types of warps (tranforms) that we can apply to images.
    /// 
    /// </summary>
    public class UnitWarps
    {
        private static double ROT = (-1) * (Math.PI / 180);

        //The old static values are commented out
        private Warp rotate;// = new Warp((float)Math.Cos(ROT),
                                             //       (float)Math.Cos(ROT),
                                             //       (float)-Math.Sin(ROT),
                                             //      (float)Math.Sin(ROT),
                                             //       0, 0);
        private Warp scaleX;// = new Warp(1, (float)1.1, 0, 0, 0, 0);
        private Warp scaleY;// = new Warp((float)1.1, 1, 0, 0, 0, 0);
        private Warp shearX;// = new Warp(1, 1, 0, (float).1, 0, 0);
        private Warp shearY;// = new Warp(1, 1, (float).1, 0, 0, 0);
        private Warp translateX;// = new Warp(1, 1, 0, 0, 0, 1);
        private Warp translateY;// = new Warp(1, 1, 0, 0, 1, 0);

        public Warp[] warps;

        /// <summary>
        /// Gives default values for scale, shear, translate, and degrees to rotate.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public UnitWarps(int width, int height)
            :this(width, height, 1, (float)1.1, (float).1, 1) 
        {
        }
        
        /// <summary>
        /// Create unit warps with these values.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="translate"></param>
        /// <param name="scale"></param>
        /// <param name="shear"></param>
        /// <param name="degrees"></param>
        public UnitWarps(int width, int height, int translate, float scale, float shear, float degrees)
        {
            //To avoid errors, we must move image to origin, apply the warp, then move it back.
            

            rotate = new Warp();
            //rotate.getMatrix().RotateAt((float)ROT*degrees, new System.Drawing.PointF((float)width/2,(float)height/2));        
            rotate.append(new Warp(1, 1, 0, 0, ((float)-height)/2, ((float)-width)/2));
            rotate.append(new Warp((float)Math.Cos(ROT*degrees),
                                                    (float)Math.Cos(ROT*degrees),
                                                    (float)-Math.Sin(ROT*degrees),
                                                   (float)Math.Sin(ROT*degrees),
                                                    0, 0));
            rotate.append(new Warp(1, 1, 0, 0, ((float)height) / 2, ((float)width) / 2));

            scaleX = new Warp();
            scaleX.append(new Warp(1, 1, 0, 0, ((float)-height)/2, ((float)-width)/2));
            scaleX.append(new Warp(1, (float)scale, 0, 0, 0, 0));
            scaleX.append(new Warp(1, 1, 0, 0, ((float)height) / 2, ((float)width) / 2));

            scaleY = new Warp();
            scaleY.append(new Warp(1, 1, 0, 0, ((float)-height) / 2, ((float)-width) / 2));
            scaleY.append(new Warp((float)scale, 1, 0, 0, 0, 0));
            scaleY.append(new Warp(1, 1, 0, 0, ((float)height) / 2, ((float)width) / 2));

            shearX = new Warp();
            shearX.append(new Warp(1, 1, 0, 0, ((float)-height) / 2, ((float)-width) / 2));
            shearX.append(new Warp(1, 1, 0, (float)shear, 0, 0));
            shearX.append(new Warp(1, 1, 0, 0, ((float)height) / 2, ((float)width) / 2));

            shearY = new Warp();
            shearY.append(new Warp(1, 1, 0, 0, ((float)-height) / 2, ((float)-width) / 2));
            shearY.append(new Warp(1, 1, (float)shear, 0, 0, 0));
            shearY.append(new Warp(1, 1, 0, 0, ((float)height) / 2, ((float)width) / 2));

            //Don't need to center the translations
            translateX = new Warp(1, 1, 0, 0, 0, translate);
            translateY = new Warp(1, 1, 0, 0, translate, 0);

            rotate.Name = "rotate";
            scaleX.Name = "scaleX";
            scaleY.Name = "scaleY";
            shearX.Name = "shearX";
            shearY.Name = "shearY";
            translateX.Name = "translateX";
            translateY.Name = "translateY";

            // Allows for iteration over all of the unit warps
            warps = new Warp[]{ rotate, scaleX, scaleY, shearX, shearY, translateX, translateY };
        }

    }
}
