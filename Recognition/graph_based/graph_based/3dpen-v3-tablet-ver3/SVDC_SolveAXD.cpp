#include "stdafx.h"

#include <string>
#include <fstream>
#include <iostream>
#include <iomanip>
#include "nr.h"
using namespace std;

// Driver for routine svbksb, which calls routine svdcmp

int SVDC_SolveAXD(double aa[3][3], double xx[3], double dd[3]) 
{
    int k,l,n = 3;

        DP wmax,wmin, cutoff;
	Vec_DP w(n),x(n),c(n);
	Mat_DP u(n,n),v(n,n);

	for (k=0;k<n;k++)
	  for (l=0;l<n;l++) 
	    u[k][l] = aa[k][l];

	for (k=0;k<n;k++) 
	  c[k] = dd[k];


	// decompose matrix a
	NR::svdcmp(u,w,v);

	// find maximum singular value
	wmax=0.0;
	wmin=1.0e10;
	for (k=0;k<n;k++)
	  if (w[k] > wmax) wmax=w[k];
	for (k=0;k<n;k++)
	  if (w[k] < wmin) wmin=w[k];
	// cout << "Condition number: " << wmax/wmin << endl;
	//if (wmax/wmin > 1.0e12) return 0;


	// define "small"
	cutoff=wmax*(1.0e-6);
	// zero the "small" singular values
	//for (k=0;k<n;k++)
	  //if (w[k] < cutoff) w[k]=0.0;
	
	// backsubstitute for each right-hand side vector
	NR::svbksb(u,w,v,c,x);
	
	  
	for (k=0;k<n;k++) 
	  xx[k] = x[k];
	
	return 1;
}
