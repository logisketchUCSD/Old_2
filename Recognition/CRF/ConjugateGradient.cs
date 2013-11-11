/**
 * File: ConjugateGradient.cs
 * 
 * Authors: Aaron Wolin, Devin Smith, Jason Fennell, and Max Pflueger (Sketchers 2006).
 *          Code expanded by Anton Bakalov (Sketchers 2007).
 *          Harvey Mudd College, Claremont, CA 91711.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */

using System;

namespace CRF
{
	public class ConjugateGradient
	{
		public delegate double myFuncDel(double[] u);
		public delegate double[] gradDel(double[] u);

		/*public static void Main(string[] args)
		{
			function myFunc = new function();
			myFuncDel f = new myFuncDel(myFunc.f);
			gradDel g = new gradDel(myFunc.gradf);
			double errorTol = Math.Pow(.1, 5);
			double paramTol = .01;

			double[] x = new double[2];
			x[0] = 0;
			x[1] = 0;

			double[] result = conjGradDescent(f,x,g,errorTol,paramTol);

			for(int i = 0; i < result.Length; i++)
			{
				Console.WriteLine("The {0}th component of the result is {1}",i,result[i]);
			}

			int j = 0;
		}*/

		/// <summary>
		/// Finds the cricical points of a function using the Method of Conjugate Gradients.  Ported from the
		/// netlab implementation of Scaled Conjugate Gradient which was done in Matlab
		/// 
		/// CRF - Matlab\Foreign\netlab\netlab3.3\scg.m
		/// </summary>
		/// <param name="f">Function for which the critical points will be found</param>
		/// <param name="x">The inital point at which the function will be evaluated</param>
		/// <param name="gradf">The gradient of the function</param>
		/// <param name="errorTol">Error tolerance condition for convergence.
		/// This is a measure of the absolute precision required for the value of X at the solution.  
		/// If the absolute difference between the values of X between two successive steps is less 
		/// than the error tolerance,then this condition is satisfied.
		///  </param>
		/// <param name="paramTol">Parameter toleracne condition for convergence.
		/// This is a measure of the precision required of the objective function at the solution.  
		/// If the absolute difference between the objective function values between two successive steps is less than
		/// the parameter tolerance, then this condition is satisfied. Both this and the
		/// previous condition must be satisfied for termination.
		///  </param>
		/// <returns>The critical point of the function that was converged to</returns>
		public static double[] conjGradDescent(myFuncDel f, double[] x, gradDel gradf, double errorTol, double paramTol)
		{
			Console.WriteLine("Starting conjugate gradient descent");
			int maxIter = 200; // Used to be 100; this caused cutoffs and, I assume, worse tcrfs
			double sigma0 = .0001;  // Inital function value
			//double sigma0 = 1; //something stupid big

			double gradTol = 0.01;  // if the gradient is less than this, call it 0
			
			Console.WriteLine("Calculating f(x)"); 
			double fold = f(x);
			double fnow = fold;
			Console.WriteLine("First f(x) = {0}", fold);
			
			Console.WriteLine("Calculating gradf(x)");
			double[] gradnew = gradf(x);
			double[] d = scalMult(-1, gradnew);
			
			int success = 1;		// Force calculation of directional deriviatives
			int nsuccess = 0;		

			double beta = 1.0;		// Inital scale parameter
			double betamin = Math.Pow(0.1,15);  // Lower bound on scale (1.0e-15)
			double betamax = Math.Pow(10,100);  // Upper bound on scale (1.0e100)

			Console.WriteLine("Starting iteration");
			double mu = 0.0;
			double kappa = 0.0;
			double sigma = 0.0;
			double theta = 0.0;
			double alpha = 0.0;
			double[] xplus = new double[x.Length];
			double[] xold = new double[x.Length];
			double[] gplus = new double[x.Length];
			double[] gradold = new double[x.Length];

			// Main optimization loop
			for (int i = 0; i < maxIter; i++)
			{
				//Console.WriteLine("{0} iteration of cg", i);

				// Calculate first and second directional derivatives
				if (success == 1)
				{
					mu = dot(d, gradnew);
					
					if (mu >= 0)
					{
						d = scalMult(-1, gradnew);
						mu = dot(d, gradnew);
					}
					
					kappa = dot(d, d);  
					sigma = sigma0 / Math.Sqrt(kappa);
					xplus = add(x, scalMult(sigma, d));
					gplus = gradf(xplus);

					double[] subgrad = subtract(gplus, gradnew);
					double dotProd = dot(d, subgrad);
					theta = dotProd / sigma;
				}

				// Increase effective curvature and evalute step size alpha
				double delta = theta + (beta * kappa);
				if (delta <= 0)
				{
					delta = beta * kappa;
					beta = beta - (theta / kappa);
				}
				alpha = -1 * (mu / delta);  //this -1 makes alpha positive
				//alpha = -1 * (mu/theta);

				// Calculate the comparison ratio
				double[] xnew = add(x, scalMult(alpha, d));

				//for(int j=0; j < d.Length; j++)
				//{
				//	Console.WriteLine("{0}", gradnew[j]);
				//}

				double fnew = f(xnew);
				while (fnew < 0.0 || double.IsNaN(fnew))
				{
					// Shake the parameters
					Console.WriteLine("Shake It!");
					double norm = Math.Sqrt(dot(xnew, xnew));
					
					for (int k = 0; k < xnew.Length; ++k)
					{
						if (xnew[k] != 0.0)
						{
							//xnew[k] = xnew[k] + CreateGraph.randGaussian()/2*norm; //add a random value
							
							//or another way
							// I don't think this is the best way to do this, probably better to use the code above
							xnew[k] = xnew[k] * (CRF.NextGaussianDouble() + 0.7);
						}
					}

					fnew = f(xnew);
				}

				double Delta = 2 * (fnew - fold)/(alpha * mu);
				xold = x;   // DELETEME
				
				if (Delta >= 0.0)
				{
					success = 1;
					nsuccess++;
					x = xnew;
					fnow = fnew;  // DELETEME
					Console.WriteLine("Success!");
				}
				else
				{
					success = 0;
					fnow = fold;  // DELETEME
				}

				// DELETE ME
				Console.WriteLine("Cycle {0,3}	Error {1,20}	Scale {2,20}  Attempted f val {3,20}", i, fnow, beta, fnew);
				#region DEBUG PRINT
//				Console.WriteLine("****************************************************************************************************************");
//				// DELETE ME
//				// Print out the step size you would have taken
//				double[] asdfasdf = scalMult(alpha, d);
//				for(int p = 0; p < x.Length; p++)
//				{
//					if(p == 0)
//					{
//						Console.WriteLine();
//						Console.WriteLine("Site feature, label 0");
//					}
//					else if (p == 8)
//					{
//						Console.WriteLine();
//						Console.WriteLine("Site feature, label 1");
//					}
//					else if (p == 16)
//					{
//						Console.WriteLine();
//						Console.WriteLine("Interaction feature, label 0-0");
//					}
//					else if (p == 34)
//					{
//						Console.WriteLine();
//						Console.WriteLine("Interaction feature, label 0-1");
//					}
//					else if(p == 52)
//					{
//						Console.WriteLine();
//						Console.WriteLine("Interaction feature, label 1-0");
//					}
//					else if(p == 70)
//					{
//						Console.WriteLine();
//						Console.WriteLine("Interaction feature, label 1-1");
//					}
//					Console.WriteLine("Component {0,3}	Old x {1,20}	New x {2,20}	Stepsize {3,20}", p, xold[p], xnew[p], asdfasdf[p]);
//				}
//				Console.WriteLine();
				#endregion

				if (success == 1)
				{
					// Test for termination.  
					// Can see above that scalMult(alpha,d) is the amount by which the x vector changes
					// between steps.
					if (absMaxComp(scalMult(alpha, d)) < errorTol && Math.Abs(fnew - fold) < paramTol)
					{
						Console.WriteLine("WOOHOO CONVERGENCE by getting under tolerance");
						return x;  //commenting this line is an interesting experiment
					}
					
					// Update variables for new position
					fold =  fnew;
					gradold = gradnew;
					gradnew = gradf(x);

					Console.WriteLine("gradient is " + dot(gradnew, gradnew));
					// If gradient is zero, then we are done
					// For our use, this is where we should always get, but numerical error is 
					// sometimes an issue.
					if (dot(gradnew, gradnew) < gradTol)
					{
						Console.WriteLine("WOOHOO CONVERGENCE by zero gradient");
						return x;
					}
				}

				// Adjust beta according to comparison ratio
				Console.WriteLine("Delta = {0}", Delta);
				if (Delta < 0.25)
                //if((Delta < 0.0)|| (Delta > 0.9))
				{
					beta =  Math.Min(4.0 * beta, betamax);
				}
				
				if (Delta > 0.75)
				//if((Delta > 0.0) && (Delta < 0.5))
				{
					beta = Math.Max(0.5 * beta, betamin);
				}

				// Update search direction using Polak-Ribiere formula, or re-start
				// in direction of negative gradient after nparams steps
				if (nsuccess == x.Length)
				{
					d = scalMult(-1,gradnew);
					nsuccess = 0;
					//beta = 1.0; // DOES THIS HELP?
				}
				else 
				{
					if (success == 1)
					{
						double gamma = dot(subtract(gradold, gradnew), gradnew) / mu;
						d = subtract(scalMult(gamma, d), gradnew);
					}
				}
			}

			// If we get here, it means that we didn't converge in maxIter iterations
			// this x is the best guess so far
			return x;
		}

		/// <summary>
		/// Find the aboslute value of maximum component of a vector of doubles
		/// </summary>
		/// <param name="u">Vector to find max component of</param>
		/// <returns>Absolute value of max component of vector</returns>
		public static double absMaxComp(double[] u)
		{
			double maxComp = 0.0;

			for(int i = 0; i < u.Length; i++)
			{
				if( Math.Abs(u[i]) > maxComp)
				{
					maxComp = Math.Abs(u[i]);
				}
			}

			return maxComp;
		}

		/// <summary>
		/// Vector addition
		/// </summary>
		/// <param name="u">Vector of doubles to be added</param>
		/// <param name="v">Vector of doubles to be added</param>
		/// <returns>The sum of two vectors</returns>
		public static double[] add(double[] u, double[] v)
		{
			double[] w = new double[u.Length];

			for(int i = 0; i < u.Length; i++)
			{
				w[i] = u[i] + v[i];
			}

			return w;
		}

		/// <summary>
		/// Vector subtraction
		/// </summary>
		/// <param name="u">Vector of doubles to be subtracted from</param>
		/// <param name="v">Vector of doubles to be subtracted by</param>
		/// <returns>The difference of two vectors, u-v</returns>
		public static double[] subtract(double[] u, double[] v)
		{
			double[] w = new double[u.Length];

			for (int i = 0; i < u.Length; i++)
			{
				w[i] = u[i] - v[i];
			}

			return w;
		}

		/// <summary>
		/// Scalar multiplication
		/// </summary>
		/// <param name="c">Scalar double to be multiplied by vector</param>
		/// <param name="u">Vector of doubles to be scaled</param>
		/// <returns>Scaled vector of doubles</returns>
		public static double[] scalMult(double c, double[] u)
		{
			double[] v = new double[u.Length];

			for(int i = 0; i < u.Length; i++)
			{
				v[i] = u[i] * c;
			}

			return v;
		}

		/// <summary>
		/// Finds the dot product of two vectors
		/// </summary>
		/// <param name="u">Vector of doubles to be dotted</param>
		/// <param name="v">Vector of double to be dotted</param>
		/// <returns>Scalar double value of dot product</returns>
		public static double dot(double[] u, double[] v)
		{
			double result = 0.0;

			for (int i = 0; i < u.Length; i++)
			{
				result += (u[i] * v[i]);
			}

			return result;
		}
	}
}
