/***********************************************

	Copyright (C) 2002 - 2005
	UC Riverside Smart Tools Lab and Thomas Stahovich 
		

***********************************************/

#include "stdafx.h"
#include "ink.h"

// Include the OpenGL headers
#include "gl\gl.h"
#include "gl\glu.h"
#include "gl\glaux.h"
#include <math.h>


//bool debug = false;
extern class Stroke *stroke_data;
extern Stroke *edit_stroke;
extern PenState pstate;
extern int DoingEdit;
extern int StartNewStroke;
extern int DoingErase;
extern int StartNewErase;
extern int EraseSeg;
extern int EraseStroke;



extern int print_circ;
extern int show_ink;
extern int show_seg;
extern int show_seg_point;
extern int show_seg_ends;

extern CString StatusText;

#define PICTURE_WIDTH 2 //3


//#define SCREEN_CAPTURE 1



	
CString DataLabels[DAT_MAX] = {
"ARC LENGTH",
"X",
"Y",
"V MAG",
"VX",
"VY",
"A MAG",
"AX",
"AY",
"AN",
"AT",
"ALIGN",
"RELALIGN",
"ALIGN NORM",
"ALIGN TANG",
"TILT",
"PRESSURE",
"RAD CUR",
"Cur SIGN",
"TANG DIR",
"TANG RATE",
"LS TANG RATE",
"TR SIGN"};


int DataVisible[DAT_MAX] =
{ 
	0, 0, 0, 1, 0,
	0, 0, 0, 0, 0,
	0, 0, 0, 0, 0,
	0, 0, 0, 0, 0,
	0, 1, 1
};



double DataScale[DAT_MAX] = 
{
	0, 0, 0, 0, 0,
	0, 0, 0, 0, 0,
	0, 0, 0, 0, 0,
	0, 0, 0, 0, -360,
	0, 0, 0
};

double DataClr[DAT_MAX][3] =
{ 
	0.5, 0.0, 0.0,
	0.0, 0.5, 0.0,
	0.0, 0.0, 0.5,

	1.0, 0.0, 0.0,
	1.0, 0.5, 0.0,
	1.0, 0.0, 0.5,
	
	0.4, 0.1, 0.0,
	0.4, 0.4, 0.1,
	0.4, 0.1, 0.4,
	0.0, 0.8, 0.6,
	0.6, 0.8, 0.0,

	0.0, 1.0, 0.0,
	1.0, 1.0, 0.0,
	0.0, 1.0, 1.0,

	0.0, 0.0, 1.0,

	1.0, 0.0, 1.0,
	0.0, 1.0, 1.0,
	0.2, 0.3, 0.4,

	1.0, 0.0, 0.0,
	0.0, 0.0, 1.0,
	0.5, 0.5, 0.1,
	0.0, 0.0, 1.0,
	1.0, 1.0, 0.0
};


int DataLineType[DAT_MAX] =
{
	1, 1, 1, 0, 0,
	1, 1, 1, 1, 1,
	1, 1, 1, 1, 1,
	1, 1, 0, 0, 0,
	1, 0, 0
};



double  InkData::GetAn(void)
{
	vect norm;
	norm = Tang;
	norm.rotcw90();
	norm = norm*NormSign;

	return(A*norm);
}

double InkData::GetAt(void)
{
	return(A*Tang);
}


double InkData::GetAmag(void)
{
	return(sqrt(A*A));
}


double InkData::GetVmag(void)
{
	return (sspeed);
	//return(sqrt(V*V));
}



Stroke::Stroke()
{
	Ink = NULL;
	SegmentPoints = NULL;
	SegmentPointType = NULL;
	MaxData=0;
	EndData=0;
	Processed = 0;
	Selected = -1;
	LastSegment = 0;
}




Stroke::Stroke(int max)
{
	Ink = new InkData[max];
	if(!Ink) {
		cout << "Our of memory\n";
		return;
	}
	SegmentPoints = new int[STYPES * max];
	if(!SegmentPoints) {
		cout << "Our of memory\n";
		return;
	}
	SegmentPointType = new int[STYPES * max];
	if(!SegmentPointType) {
		cout << "Our of memory\n";
		return;
	}
	MaxData = max;
	EndData = 0;
	Processed = 0;
	Selected = -1;
	LastSegment = 0;
}
Stroke::~Stroke()
{
	if (Ink) delete[] Ink;
	if (SegmentPoints) delete[] SegmentPoints;
	if (SegmentPointType) delete[] SegmentPointType;
}


void Stroke::AddPoint(int X, int Y, int Pressure, double Tilt, double Dir, DWORD TimeStamp)
{
	if(EndData < MaxData-1 && Ink != NULL) {
		Ink[EndData].P.x = X;
		Ink[EndData].P.y = Y;
		Ink[EndData].Pressure = Pressure;
		Ink[EndData].Tilt = Tilt;
		Ink[EndData].Dir = Dir;
		Ink[EndData].TimeStamp = TimeStamp;
		Ink[EndData].RadCur = 0.0;
		Ink[EndData].CSign = 0;
		Ink[EndData].RelAlign = 3;
		Ink[EndData].Align = 0.0;

		EndData++;
	}
	Processed = 0;
	LastSegment = 0;
}


int DrawPen(PenState pstate)
{

	class GLUquadric *quad;
	if(!pstate.ShowPen) return(0);




	glNormal3f(0.0, 0.0, 1.0);
	glColor3f(0.0,0.0,0.0);


	glPushMatrix();

	glTranslated(pstate.PenX / INK_SCALE, pstate.PenY / INK_SCALE, 0.0);

	glBegin(GL_POLYGON);
	glVertex3f(1.0,1.0,0.0);
	glVertex3f(-1.0,1.0,0.0);
	glVertex3f(-1.0,-1.0,0.0);
	glVertex3f(1.0,-1.0,0.0);
	glEnd();


	glPopMatrix();

	return 1;

	quad = gluNewQuadric();

	glRotated(pstate.PenDir - 90.0, 0.0, 0.0, 1.0);
	if(pstate.PenTilt < 0.0) {

		glRotated(pstate.PenTilt+90, 1.0, 0.0, 0.0);
		gluQuadricNormals(quad,GLU_SMOOTH);
		glColor3f(1.0, 0.0, 0.0);
		gluCylinder(quad, 0.001,0.2 ,0.5, 20, 20);
		glTranslatef(0.0,0.0,.5);
		/*
		if(EraseSeg ) {
			glColor3f(0.0,1.0,1.0);
		} else if (EraseStroke) {
			glColor3f(1.0,0.0,0.0);
		} else {
			glColor3f(1.0,1.0,0.0);
		}
		*/
		glColor3f(1.0,1.0,0.0);
		gluCylinder(quad, 0.2,0.2 ,5.0,20,20);
		glTranslatef(0.0,0.0,5.0);
		glColor3f(0.0, 1.0, 0.0);
		gluCylinder(quad, 0.2,0.001,0.5,20,20);
	} else {
		glRotated(pstate.PenTilt - 90.0, 1.0, 0.0, 0.0);
		gluQuadricNormals(quad,GLU_SMOOTH);
		glColor3f(0.0, 1.0, 0.0);
		gluCylinder(quad, 0.001,0.2,0.5,20,20);

		glTranslatef(0.0,0.0,0.5);
		//glColor3f(0.0, 0.0, 1.0);
		//gluCylinder(quad, 0.2,0.2,5.0,20,20);
		glColor3f(1.0, 0.0, 1.0);
		gluCylinder(quad, 0.2,0.2,5.0 * pstate.PenPressure / 1024.0,20,20);
		glPushMatrix();
		glTranslatef(0.0,0.0, (float)(5.0 * pstate.PenPressure / 1024.0));
		glColor3f(0.0,0.0,1.0);
		gluCylinder(quad, 0.2,0.2,5.0 * (1024.0 - pstate.PenPressure) / 1024.0,20,20);
		glPopMatrix();
		glTranslatef(0.0,0.0,5.0);
		glColor3f(1.0, 0.0, 0.0);
		gluSphere(quad, 0.2,20,20);
	}
	gluDeleteQuadric(quad);
	glPopMatrix();

	return 1;
}


#define MARKERSIZE 5

void Stroke::Draw(void)
{
	float xx, yy;

	int ii;

	glNormal3f(0.0, 0.0, 1.0);

	//if(show_ink || pstate.TipDown) {
	if(show_ink || 
		//(pstate.ShowPen && this == edit_stroke) ||
		//(pstate.ShowPen  && this == stroke_data && (DoingEdit || !StartNewStroke || pstate.PenTilt < 0.0) )) {
		(pstate.ShowPen && this == edit_stroke) ||
		(pstate.ShowPen && this == stroke_data && !StartNewStroke ) ||
		(pstate.ShowPen  && (DoingEdit || ((pstate.Button1 || pstate.Button2) && StartNewStroke) ) )  ) {
		//(pstate.ShowPen  && this == stroke_data && (DoingEdit || !StartNewStroke || pstate.Button1 || pstate.Button2) )) {
		// draw the ink 
		
#ifdef SCREEN_CAPTURE
		glColor3f(0.0,0.0,0.0);
#else 
		glColor3f(0.0,1.0,0.0);
#endif
		//glColor3f(1.0,1.0,1.0);


		if(this == edit_stroke) {
			glColor3f(0.0,1.0,1.0);
		}


		glLineWidth(PICTURE_WIDTH);
		if(EndData > 0) {
			glBegin(GL_LINE_STRIP);
			glNormal3f(0.0,0.0,1.0);
			for(int pt_it =0; pt_it<EndData; pt_it++) {
			
			/*	if(Ink[pt_it].CSign == 1) {
					glColor3f(1.0,0.0,0.0);
				} else if(Ink[pt_it].CSign == -1) {
					glColor3f(0.0,0.0,1.0);
				} else {
					glColor3f(0.0,1.0,0.0);
				}
			*/
				
				xx = (float) Ink[pt_it].P.x/INK_SCALE;
				yy = (float) Ink[pt_it].P.y/INK_SCALE;
				glVertex3f(xx, yy, (float) 0.05);

			}
			glEnd();
		}


		if(Selected >= 0 && Processed) {
			glNormal3f(0.0,0.0,1.0);
			glColor3f(0.0,0.0,1.0);
			glBegin(GL_LINE_STRIP);
				glVertex3d(Ink[Selected].P.x/INK_SCALE, Ink[Selected].P.y /INK_SCALE, 0.06);
				glVertex3d((Ink[Selected].P.x +100.0 * Ink[Selected].Tang.x)/INK_SCALE, 
					(Ink[Selected].P.y +100.0 * Ink[Selected].Tang.y)/INK_SCALE, 0.06);
			glEnd();
		}



		glLineWidth(1.0);


	} 


	if(show_seg) {
		seglist.Draw();
	}


	if(show_seg_point) {
		// draw segment points
		class GLUquadric *quad;
		quad = gluNewQuadric();
		gluQuadricNormals(quad,GLU_SMOOTH);

		gluQuadricNormals(quad,GLU_SMOOTH);


		for(ii=0; ii<LastSegment; ii++) {
			if(SegmentPointType[ii] == CSPIKE) {
				glColor3f(0.0, 0.0, 1.0);
			} else if(SegmentPointType[ii] == SPEED) {
				glColor3f(1.0, 0.0, 0.0);
			} else if (SegmentPointType[ii] == CSIGN) {
				glColor3f(0.0, 1.0, 1.0);
			}

			//if(SegmentPointType[ii] == CSIGN) {
			xx = (float) Ink[SegmentPoints[ii]].P.x/INK_SCALE;
			yy = (float) Ink[SegmentPoints[ii]].P.y/INK_SCALE;

			glPushMatrix();
			glTranslatef(xx, yy, (float)0.07);


#ifdef SCREEN_CAPTURE

			glColor3f(0.0, 0.0, 0.0);
			if(SegmentPointType[ii] == CSPIKE) {

				glBegin(GL_POLYGON);
				glVertex2f(-MARKERSIZE, -MARKERSIZE);
				glVertex2f(+MARKERSIZE, -MARKERSIZE);
				glVertex2f(+MARKERSIZE, +MARKERSIZE);
				glVertex2f(-MARKERSIZE, +MARKERSIZE);
				glEnd();
/*
				glBegin(GL_POLYGON);
				glVertex2f(-0.03, -0.03);
				glVertex2f(+0.03, -0.03);
				glVertex2f(+0.03, +0.03);
				glVertex2f(-0.03, +0.03);
				glEnd();
*/
			} else if(SegmentPointType[ii] == SPEED) {
				gluDisk(quad, 0.0, 1.5*MARKERSIZE, 20, 20);
				//gluDisk(quad, 0.0, 0.05, 20, 20);
			} else if (SegmentPointType[ii] == CSIGN) {
				glBegin(GL_POLYGON);
				glVertex2f(-MARKERSIZE, -MARKERSIZE);
				glVertex2f(+MARKERSIZE, -MARKERSIZE);
				glVertex2f(0, +MARKERSIZE);
				glEnd();
/*
				glBegin(GL_POLYGON);
				glVertex2f(-0.04, -0.04);
				glVertex2f(+0.04, -0.04);
				glVertex2f(0, +0.04);
				glEnd();
*/
			}
#else
			// august 3 changes
			gluDisk(quad, 0.0, 5.0, 20, 20);

#endif

			glPopMatrix();
			//}
		}
		gluDeleteQuadric(quad);  // new on July 15 2002
	}

}




int Stroke::Compress()
{
	
	int newpts, ii;
	int loss, dup;
	InkData *newink, tink;
	newink = new InkData[MaxData];
	newink[0]=Ink[0];
	newpts=1;
	ii=1;


	while(ii<EndData) {
		if(Ink[ii].P.x != Ink[ii-1].P.x || Ink[ii].P.y != Ink[ii-1].P.y) {
			newink[newpts] = Ink[ii];
			newpts++;
			ii++;
		} else { 
			newpts--;
			tink.P = Ink[ii-1].P;
			tink.Pressure = Ink[ii-1].Pressure;
			tink.Tilt = Ink[ii-1].Tilt;
			tink.Dir = Ink[ii-1].Dir;
			dup = 1;
			while (Ink[ii].P.x == Ink[ii-1].P.x && Ink[ii].P.y == Ink[ii-1].P.y && ii<EndData) {
				tink.Pressure += Ink[ii].Pressure;
				tink.Tilt += Ink[ii].Tilt;
				tink.Dir += Ink[ii].Dir;
				ii++;
				dup++;
			}
			tink.P = Ink[ii-1].P;
			tink.Pressure /= dup;
			tink.Tilt /= (double) dup;
			tink.Dir /= (double) dup;
			tink.TimeStamp = Ink[ii-1].TimeStamp;
			newink[newpts] = tink;
			newpts++;
		}
	}

	delete[] Ink;
	Ink = newink;
	loss = EndData - newpts;
	EndData = newpts;

	return loss;
}




double sqr(double x)
{ 
	return(x*x);
}

double determinate(double m[3][3])
{
	return (
	m[0][0]  * (m[1][1]*m[2][2] - m[1][2]*m[2][1]) 
  - m[0][1]  * (m[1][0]*m[2][2] - m[1][2]*m[2][0]) 
  + m[0][2]  * (m[1][0]*m[2][1] - m[1][1]*m[2][0]) 
	);
}



int SVDC_SolveAXD(double aa[3][3], double xx[3], double dd[3]);


int SolveAXD(double aa[3][3], double xx[3], double dd[3])
{
	int ii, jj;
	double a[3][3], d[3];

	// make a copy to preserve the arguments;
	for(ii=0;ii<3;ii++) {
		for(jj=0;jj<3;jj++) {
			a[ii][jj] = aa[ii][jj];
			d[ii] = dd[ii];
		}
	}

	double tmp1, tmp2;
	tmp1 = a[1][0]/a[0][0];
	tmp2 = a[2][0]/a[0][0];
	for(ii=0;ii<3;ii++) {
		a[1][ii] -=a[0][ii]*tmp1;
		a[2][ii] -=a[0][ii]*tmp2;
	}
	d[1] -=d[0]*tmp1;
	d[2] -=d[0]*tmp2;
	
	tmp1 = a[2][1]/a[1][1];
	for(ii=1;ii<3;ii++) {
		a[2][ii] -=a[1][ii]*tmp1;
	}
	d[2] -= d[1]*tmp1;

	xx[2] = d[2]/a[2][2];
	xx[1] = (d[1] - a[1][2]*xx[2])/a[1][1];
	xx[0] = (d[0] - a[0][1]*xx[1] - a[0][2]*xx[2])/a[0][0];

	double e[3];
	double error;

	error = 0.0;
	for(ii=0;ii<3;ii++) {
		e[ii] = - dd[ii];
		for(jj=0;jj<3;jj++) {
			e[ii] += aa[ii][jj] * xx[jj];
		}
		error += fabs(e[ii]);
	}
	if(error > 1.0e-5) { 
		return(0);
	}
	
	return(1);
}


double fit_line(InkData fl_ink[], int low, int high, double *A, double *B) 
{
	double yave;
	int ii;
	int num;

	num = high - low;

	yave = 0.0;
	for(ii=low;ii<high;ii++) {
		yave += fl_ink[ii].P.y;
	}
	yave = yave / (double) num;


	double sqr_stdev=0.0;
	double S, SX, SXY, SY, SXX;

	sqr_stdev = 0.0;
	for(ii=low;ii<high;ii++) {
		sqr_stdev += sqr(fl_ink[ii].P.y - yave);
	}

	sqr_stdev = sqr_stdev / ((double) num - 1.0);
	if(sqr_stdev < 1.0e-5) {
		*A = yave;
		*B = 0.0;
		return (0.0);
	}

	S = SX = SXY = SY = SXX = 0.0;

	for(ii=low;ii<high;ii++) {
		S += 1.0 / sqr_stdev;
		SX += fl_ink[ii].P.x / sqr_stdev;
		SY += fl_ink[ii].P.y / sqr_stdev;
		
		SXX += sqr(fl_ink[ii].P.x) / sqr_stdev;
		SXY += fl_ink[ii].P.x * fl_ink[ii].P.y / sqr_stdev;
	}

	double deter;
	deter = S*SXX -sqr(SX);
	*A = (SXX*SY - SX*SXY) / deter;
	*B = (S*SXY - SX*SY) / deter;

	double err;
	err = 0;
	for(ii=low;ii<high;ii++) {
		err += 	fabs(fl_ink[ii].P.y - (*B)*fl_ink[ii].P.x - (*A));
	}
	return(err / (double) (high - low));

}

double Stroke::CalcArcLength(void)
{
	int rr;
	Ink[0].S = 0;
	double dels;
	vect delP;
	for(rr=1; rr<EndData; rr++) {
		delP = Ink[rr].P - Ink[rr-1].P;
		dels = sqrt( delP * delP);
		Ink[rr].S = Ink[rr-1].S + dels;
	}
	return (Ink[EndData-1].S - Ink[0].S);
}


double Stroke::DensifyData(InkData TmpInk[], int low, int hi, 
						   int pt, int den, int *thept, int *tii)
{
	// make the data more dense and equally spaced
	double dl = Ink[hi-1].S - Ink[low].S;
	dl = dl / (double) den;
	*tii = 0;
	int jj;
	double len;
	vect dir, npt;
	TmpInk[*tii]=Ink[low];
	(*tii)++;
	(*thept) = 0; // the first data point may be the point of interest 
	for(jj=low+1;jj<hi;jj++) {
		len = Ink[jj].S - Ink[jj-1].S;
		dir = Ink[jj].P - Ink[jj-1].P;
		dir.normalize();

		npt = Ink[jj-1].P;
		while(len > dl) {
			npt = npt + dl*dir;
			TmpInk[*tii].P=npt;
			len = len - dl;
			(*tii)++;
		}
		if(jj == pt) *thept = *tii;     // save the new ink point corresponding to "pt"
		TmpInk[(*tii)]=Ink[jj];
		(*tii)++;
	}
	return (dl);
}



#define LINE_FIT_TOL 0.1   // 0.04

int Stroke::CalcCircle(double *rad, vect *cen, int low, int hi, int pt, vect *tangent, int *NormSign)
{


	InkData TmpInk[200];
	int ii;
	int tii;
	double len;
	double A, B;

	int thept;
	int skip_line;

	double a1, b1, a2, b2, db;
	vect nn;
	double err;



	// make sure the input data is valid
	if ((hi - low < 3) || (hi < 0) || (low < 0) || (hi > EndData) 
		|| (pt < low) || (pt >= hi)) {
		cout << "************************\n";
		cout << "Invalid data in Stroke::CalcCircle\n";
		cout << "Hi = " << hi << endl;
		cout << "low = " << low << endl;
		cout << "pt = " << pt << endl;

		return 0;
	}


	/*
	// test code to write densified data to file
	if(print_circ) {
		ofstream dataout("circ_dat.csv");
		int dat;

		for(dat=0; dat<tii; dat++) {
			dataout << TmpInk[dat].P.x << "\t";
		}
		dataout << endl;
		for(dat=0; dat<tii; dat++) {
			dataout << TmpInk[dat].P.y << "\t";
		}
		dataout << endl;
		dataout.close();
	}

*/

	
	CalcArcLength();
	len = Ink[hi-1].S - Ink[low].S;

	//DensifyData(TmpInk, low, hi, pt, 100, &thept, &tii);
	DensifyData(TmpInk, low, hi, pt, 1, &thept, &tii);


	// check for a vertical line
	double xx = -1000000.0, xn = 1000000.0;
	for(ii=0;ii<tii;ii++) {
		if(TmpInk[ii].P.x < xn) xn = TmpInk[ii].P.x;
		if(TmpInk[ii].P.x > xx) xx = TmpInk[ii].P.x;

	}


	skip_line = 0; // if we try a vertical line fit and fail, go straight to circle fit

	if(xx - xn < 0.05 * len) {

		for(ii=0;ii<tii;ii++) {  //rotate coord 90 deg so line appears horizontal
			double tempx = TmpInk[ii].P.x;
			TmpInk[ii].P.x = -TmpInk[ii].P.y;
			TmpInk[ii].P.y = tempx;
		}

		err = fit_line(&TmpInk[0], 0, tii, &A, &B);
		
		// FIX: Error was being normalized twice
		//err = err / (double) tii;
		
		

		if(err < LINE_FIT_TOL*len) {
			if(pt > 0) {
				*NormSign = Ink[pt-1].NormSign;
			} else {
				*NormSign = 1;
			}
			tangent->x = -B;
			tangent->y = 1.0;

			tangent->normalize();

			vect check_vect = Ink[hi-1].P - Ink[low].P;
			check_vect.normalize();
			if( (*tangent) * check_vect < 0.0) {
				*tangent = -1.0 * (*tangent);
			}

			fit_line(&TmpInk[0], 0, thept, &a1, &b1);		
			fit_line(&TmpInk[0], thept+1, tii, &a2, &b2);

			db = fabs(b2 - b1);
			if(db > 1.0e-6) {
				*rad = fabs(Ink[hi-1].S - Ink[pt].S) / db;
			} else {
				*rad = 1.0e6;
			}
			nn = *tangent;
			nn.rotcw90();
			if(*NormSign == -1) {
				nn = -1.0*nn;
			}
			cen->x = (*rad) * nn.x + Ink[pt].P.x;
			cen->y = (*rad) * nn.y + Ink[pt].P.y;
			return 0;
		} else {
			skip_line = 1;  // we are going to fit a circle
			for(ii=0;ii<tii;ii++) {   // rotate coord back to original
				double tempx = TmpInk[ii].P.x;
				TmpInk[ii].P.x = TmpInk[ii].P.y;
				TmpInk[ii].P.y = -tempx;
			}

		}
	}



	
	
	// solve least squares line fit
	if(!skip_line) {  // skip if line was almost vertical
		err = fit_line(&TmpInk[0], 0, tii, &A, &B);
		
		// FIX: Error was being normalized twice
		// err = err / (double) tii;


		if(err < LINE_FIT_TOL*len) {

			if(pt > 0) {
				*NormSign = Ink[pt-1].NormSign;
			} else {
				*NormSign = 1;
			}
			tangent->x = 1.0;
			tangent->y = B;
			tangent->normalize();

			if(Ink[low].P.x > Ink[hi-2].P.x) {
				*tangent = -1.0 * (*tangent);
			}


			fit_line(&TmpInk[0], 0, thept, &a1, &b1);		
			fit_line(&TmpInk[0], thept+1, tii, &a2, &b2);

			db = fabs(b2 - b1);
			if(db > 1.0e-6) {
				*rad = fabs(Ink[hi-1].S - Ink[pt].S) / db;
			} else {
				*rad = 1.0e6;
			}
			nn = *tangent;
			nn.rotcw90();
			if(*NormSign == -1) {
				nn = -1.0*nn;
			}
			cen->x = (*rad) * nn.x + Ink[pt].P.x;
			cen->y = (*rad) * nn.y + Ink[pt].P.y;
			//cout << "line " << err << endl;
			return 0;
		}
	}


	// solve least squares circle fit

	double sang, eang;  // unused arguments
	// Begin Burak's code
	double real_sang = 0.0, real_eang = 0.0;
	int dir;
	// End Burak's code

	fitarc(&TmpInk[0], 0, tii, &(cen->x), &(cen->y), rad, &sang, &eang,
		// Begin Burak's code
		&dir, &real_sang, &real_eang);
		// End Burak's code
	
	// calculate normal and tangent
	vect norm, tang, approx_tang;
	norm = *cen - Ink[pt].P;
	norm.normalize();
	tang = norm;

	int tail, head;
	head = pt+2;
	if(head >= EndData) head = EndData-1;
	tail = pt-2;
	if(tail < 0) tail = 0; 
	approx_tang = Ink[head].P - Ink[tail].P;
	approx_tang.normalize();

	tang.rotCcw90();
	if(tang * approx_tang < 0.0) {
		*NormSign = -1;
		tang.rotCcw90();
		tang.rotCcw90();
	} else {
		*NormSign = 1;
	}
	
	*tangent = tang;

	return(*NormSign);
}



int dir_point_intersect(vect pa, vect pb, vect na, vect nb, vect *p)
{
	vect t;
	double del = -na.x*nb.y + na.y*nb.x;
	vect c(pa.x-pb.x, pa.y-pb.y);
	vect r1(nb.y, -nb.x);
	vect r2(na.y,-na.x);
	if(fabs(del) < 1.0e-6) return 0;
	t.x = (r1*c) / del;
	t.y = (r2*c) / del;

	*p = pa + na*t.x;

	
	if(t.x < 0.) return -1;
	return 1;
}


int ll_intersect(vect sa, vect ea, vect sb, vect eb, vect *p)
{
	vect t, na, nb;
	na = ea - sa;
	nb = eb - sb;


	double del = -na.x*nb.y + na.y*nb.x;
	vect c(sa.x-sb.x, sa.y-sb.y);
	vect r1(nb.y, -nb.x);
	vect r2(na.y,-na.x);
	if(fabs(del) < 1.0e-6) return 0;
	t.x = (r1*c) / del;
	t.y = (r2*c) / del;

	*p = sa + na*t.x;

	if(t.x < 0.0 || t.x > 1.0 || t.y < 0.0 || t.y > 1.0 ) {
		*p = vect(10000.0, 10000.0); // for debugging
		return 0;
	}


	return 1;
}
	

#define ITOL .01
int lc_intersect(vect pa, vect pb, vect c, double rad, double sang, double eang, vect *p)
{
	vect t, l, tmp1, tmp2, foot, perp;
	double ang, tang, len;

	t = c - pa;
	l = pb - pa;
	len = l.length();
	l.normalize();
	foot = pa+ (t * l) * l; // foot of perpendicular
	perp = foot - c;  // vector: a perpendicular from the center to the foot
	


	// no intersection
	if (perp.length() > (1+ITOL)*rad) return 0; 


	// one point intersection
	if (perp.length() < (1+ITOL)*rad && perp.length() > (1-ITOL)*rad) {
		ang = atan2(perp.y, perp.x);
		if(ang < sang) ang+= 2*PI;  // eang > sang, so we must map to the right range of angles

		if(ang <= eang) {
			*p = foot;
			return 1;
		} else {
			return 0;
		}
	}

	// two point intersection
	tang = acos(perp.length() / rad);
	*p = foot + (rad*sin(tang))*l;
	ang = atan2(p->y - c.y, p->x - c.x);
	if(ang < sang) ang+= 2*PI;
	tmp1 = *p - pa;
	tmp2 = *p - pb;
	if(ang <= eang && tmp1.length() <= len && tmp2.length() <= len ) 
		return 1;

	*p = foot - (rad*sin(tang))*l;
	ang = atan2(p->y - c.y, p->x - c.x);
	if(ang < sang) ang+= 2*PI;
	tmp1 = *p - pa;
	tmp2 = *p - pb;
	if(ang <= eang && tmp1.length() <= len && tmp2.length() <= len ) 
		return 1;

	*p = vect(1000000.0, 100000.0); // for debug purposes
	return 0;
}


double curvature(vect p0, vect p1, vect p2, vect *tangent, int *NormSign)
{
	vect na, nb, p, tang;
	double curv;

	na = p1 - p0;
	na.normalize();
	na.rotcw90();

	nb = p2 - p1;
	nb.normalize();
	nb.rotcw90();

	int id = dir_point_intersect((p0+p1)/2, (p1+p2)/2, na, nb, &p); 
	if(id)
	{
		p = p - p1;
		curv = (double) id * 1.0/sqrt(p*p);
		tang = p;
		tang.normalize();
		tang.rotCcw90();
		tang = tang*(double) id;
		*NormSign = id;
	} else {
		curv = 0.0;
		tang = p2 - p0;
		tang.normalize();
		*NormSign = 1;
	}
	*tangent = tang;


	return curv;
}
		

#define TANGWIN 5
//#define TRWIN 7
#define TRWIN 5
#define TRS_WIN 5
int Stroke::CalcCurv()
{
	if(EndData < 3) return 0;
	int ii;
	int low, hi;
	double rad;
	class vect cen;

	for(ii=0;ii<EndData;ii++) {
		low = ii-TANGWIN;
		hi = ii+TANGWIN;
		if(low < 0) {
			low = 0;
			hi = 2*TANGWIN+1;
		}
		if(hi > EndData) {
			hi = EndData;
			low = EndData - (2*TANGWIN+1);
			if(low < 0) low = 0;
		}



		Ink[ii].CSign = CalcCircle(&rad, &cen, low, hi, ii, &Ink[ii].Tang, &Ink[ii].NormSign);	

		
		//Ink[ii].RadCur = rad * (double) Ink[ii].NormSign;
		Ink[ii].RadCur = 1.0/ rad;

		double ang = atan2(Ink[ii].Tang.y, Ink[ii].Tang.x);
		ang = 180.0 * ang / PI;
		if(ang < 0.0) ang += 360.0;
		
		Ink[ii].TDir = ang;
	}

	int done = 0;
	for(ii=1;ii<EndData;ii++) {
		done = 0;
		int count  = 0;
		while(!done && count < 10) {
			if(  fabs(Ink[ii].TDir - Ink[ii-1].TDir) > fabs((Ink[ii].TDir + 360.0) - Ink[ii-1].TDir) ){
				Ink[ii].TDir += 360.0;
			} else {
				done = 1;
			}
			count++;
		}
			
		count = 0;
		done = 0;
		while(!done && count < 10) {
			if(  fabs(Ink[ii].TDir - Ink[ii-1].TDir) > fabs((Ink[ii].TDir - 360.0) - Ink[ii-1].TDir) ) {
				Ink[ii].TDir -= 360.0;
			} else {
				done = 1;
			}
			count++;
		}
			
	}


	trave = 0;
	for(ii=0;ii<EndData;ii++) {
		low = ii-TRWIN;
		hi = ii+TRWIN;
		if(low < 0) {
			low = 0;
			hi = 2*TRWIN+1;
		}
		if(hi > EndData) {
			hi = EndData;
			low = EndData - (2*TRWIN+1);
			if(low < 0) low = 0;
		}

		Ink[ii].LS_TRate = CalcLSTRate(low, hi, ii);
	}


	for(ii=3;ii<EndData-3;ii++) {
		trave += Ink[ii].LS_TRate;
	}
	trave /= (double) EndData - 6.0;

	for(ii=0;ii<EndData;ii++) {
		low = ii-TRS_WIN;
		hi = ii+TRS_WIN;
		if(low < 0) {
			low = 0;
			hi = 2*TRS_WIN+1;
		}
		if(hi > EndData) {
			hi = EndData;
			low = EndData - (2*TRS_WIN+1);
			if(low < 0) low = 0;
		}

		Ink[ii].LS_TR_Sign = CalcLSTRateSign(low, hi, ii);
	}



	cout.flush();



	return(1);
}


int Stroke::CalcPenAlignment()
{
	vect pdir, norm;
	double tdot, ndot;

	for(int ii=0;ii<EndData;ii++) {
		pdir = vect(Ink[ii].Dir);

		norm = Ink[ii].Tang;
		norm.rotcw90();
		norm = norm*Ink[ii].NormSign;

		ndot = pdir*norm;
		tdot = pdir*Ink[ii].Tang;

		
		/*
			RelAlign is a bit field with 2 bits used:
			0 = 00b = <trailing the drawing direction> and <opposite the normal>
			1 = 01b = <trailing the drawing direction> and <with the normal>
			2 = 10b = <leading the drawing direction> and <opposite the normal>
			3 = 11b = <leading the drawing direction> and <with the normal>
		*/

		Ink[ii].RelAlign = 0;
		if(ndot >= 0) {
			Ink[ii].RelAlign += 1;
		}

		if(tdot >= 0) {
			Ink[ii].RelAlign += 2;
		}
		
		Ink[ii].Align = acos(ndot) * 180.0 / PI;


		// consider the smallest angle between the pen and a line perpendicular to the curve
		// if the pen direction points opposite the normal, we are effectively flipping the normal
		if(Ink[ii].Align > 90.0) {
			Ink[ii].Align = 180.0 - Ink[ii].Align;
		}

		// if the pen direction points opposite the tangent, the alignment angle is negative
		if (tdot < 0.) {
			Ink[ii].Align *= -1.0;
		}

	}
	return 1;
}




int Stroke::CountSpeedSegs(void)
{

	if(EndData < 3) return 0;

	/*
	Processed = 1;

	double dtp, dtm, dels;
	vect delP;
	int ii;
	double ts;

	Ink[0].S = 0;
	for(ii=1; ii<EndData-1; ii++) {
		dtp = Ink[ii+1].TimeStamp - Ink[ii].TimeStamp;
		dtm = Ink[ii].TimeStamp - Ink[ii-1].TimeStamp;
		
		Ink[ii].V = (Ink[ii+1].P - Ink[ii-1].P) / (dtp + dtm);

		delP = Ink[ii].P - Ink[ii-1].P;
		dels = sqrt( delP * delP);
		Ink[ii].S = Ink[ii-1].S + dels;
	}

	Ink[0].V = Ink[1].V;
	Ink[EndData-1].V = Ink[EndData-2].V;


	LastSegment = 0;
	CalcSegPoints_Speed();
	return(LastSegment);
	*/

	ProcessInk();
	int ii, count;
	count = 0;
	for(ii=0; ii<LastSegment; ii++) {
		if(SegmentPointType[ii] == SPEED) count++;
	}
	return(count);
}





int Stroke::CalcKinematics()
{

	if(EndData < 3) return 0;
	
	double dtp, dtm, dels;
	vect delP;
	int ii;
	double ds_min = 1000000.0;
	double ds_ave;
	double ts;

	Ink[0].S = 0;
	for(ii=1; ii<EndData-1; ii++) {
		dtp = Ink[ii+1].TimeStamp - Ink[ii].TimeStamp;
		dtm = Ink[ii].TimeStamp - Ink[ii-1].TimeStamp;
		
		Ink[ii].V = (Ink[ii+1].P - Ink[ii-1].P) / (dtp + dtm);

		Ink[ii].A = ( ((Ink[ii+1].P - Ink[ii].P) / dtp) 
						- ((Ink[ii].P - Ink[ii-1].P) / dtm ) ) / (0.5*(dtp + dtm));
		delP = Ink[ii].P - Ink[ii-1].P;
		dels = sqrt( delP * delP);
		if(dels < ds_min) ds_min = dels;
		Ink[ii].S = Ink[ii-1].S + dels;
		Ink[ii].TRate = (Ink[ii].GetTDir() - Ink[ii-1].GetTDir())/ dels;
	}

	Ink[0].V = Ink[1].V;
	Ink[0].A = Ink[1].A;
	Ink[0].TRate = Ink[1].TRate;

	Ink[EndData-1].V = Ink[EndData-2].V;
	Ink[EndData-1].A= Ink[EndData-2].A;

	ii = EndData - 1;
	delP = Ink[ii].P - Ink[ii-1].P;
	dels = sqrt( delP * delP);
	Ink[ii].S = Ink[ii-1].S + dels;
	Ink[ii].TRate = (Ink[ii].GetTDir() - Ink[ii-1].GetTDir())/ dels;

	ds_ave = Ink[EndData-1].S / (double) EndData;
	ts = (Ink[EndData-1].TimeStamp - Ink[0].TimeStamp)/ (double) EndData;

	/*
	// This code is smoothing the tangent rate data.
	// 
	double *tr;
	tr = new double[EndData];

	for(ii=0; ii<EndData; ii++) {
		tr[ii] = fabs(Ink[ii].TRate);
	}
	


	double w[]= {0.05, 0.1, 0.2, 0.3, 0.4, 1.0, 0.4, 0.3, 0.2, 0.1, 0.05};
	double sw=0.0;
	for(jj=0;jj<11;jj++) sw += w[jj];

	for(ii=5; ii<EndData-5; ii++) {
		Ink[ii].TRate =  0.0;
		// I don't think this code works 
		// On aug 16, I changed the indices of the loop
		for(jj=-5;jj<6;jj++) {
			Ink[ii].TRate += w[jj+5]*tr[ii+jj]/sw;
		}
	}

	for(ii=0; ii<EndData; ii++) {
		Ink[ii].TRate = tr[ii];
	}

	delete[] tr;

    */


	double *spd;
	spd = new double[EndData];
	for(ii=0; ii<EndData; ii++) {
		spd[ii] = sqrt(Ink[ii].V * Ink[ii].V);
	}

	for(ii=2; ii<EndData-2; ii++) {
		Ink[ii].sspeed = (spd[ii-2] + spd[ii-1] + spd[ii] + spd[ii+1] + spd[ii+2]) / 5.0;
	}
	Ink[0].sspeed = Ink[1].sspeed = Ink[2].sspeed;
	Ink[EndData-1].sspeed = Ink[EndData-2].sspeed = Ink[EndData-3].sspeed;


	
	delete[] spd;


	//StatusText.Format("Ave DT = %lf    Min DS = %lf   ave DS = %lf", ts, ds_min, ds_ave);

	return(1);

}





double InkData::GetTDir(void)
{
	return TDir;
	/*
	double ang = atan2(Tang.y, Tang.x);
	ang = 180.0 * ang / PI;
	if(ang < 0.0) ang += 360.0;
	return(ang);
	*/
}


double InkData::GetData(int ii)
{
	switch (ii)
	{
	case DAT_ARCLEN:
		return S;
	case DAT_X:
		return P.x;
	case DAT_Y:
		return P.y;
	case DAT_VMAG: 
		return GetVmag();
	case DAT_VX:
		return V.x;
	case DAT_VY:
		return V.y;
	case DAT_AMAG:
		return GetAmag();
	case DAT_AX:
		return A.x;
	case DAT_AY:
		return A.y;
	case DAT_AN:
		return GetAn();
	case DAT_AT:
		return GetAt();
	case DAT_ALIGN:
		return Align;
	case DAT_RELALIGN:
		return RelAlign;
	case DAT_ALIGNNORM:
		return RelAlign & 1;
	case DAT_ALIGNTANG:
		return RelAlign & 2;
	case DAT_TILT:
		return Tilt;
	case DAT_PRESSURE:
		return Pressure;
	case DAT_RADCUR:
		return RadCur;
	case DAT_CSIGN:
		return CSign;
	case DAT_TDIR:
		return GetTDir();
	case DAT_TRATE:
		return TRate;
	case DAT_LS_TRATE:
		return LS_TRate;
	case DAT_TR_SIGN:
		return LS_TR_Sign;

	}
	return -1;
}

int StrokeList::Write(ofstream &ofs) {
	int res, s=0, ii;
	for(ii=0; ii<GetSize(); ii++) {
		// weesan
		//if( ((class Stroke *) stroke_list[ii])->EndData > 0) 
		if( ((class Stroke *) stroke_list[ii])->EndData > 1) 
			s++;
	}

	ofs << s << endl; 
	
	for(ii=0; ii<GetSize(); ii++) {
		// weesan
		//if ( ((class Stroke *) stroke_list[ii])->EndData > 0)  {
		if ( ((class Stroke *) stroke_list[ii])->EndData > 1)  {
			res = ((class Stroke *) stroke_list[ii])->Write(ofs);
			if(!res) return 0;
		}	
	}
	return 1;
}

int StrokeList::Read(ifstream &ifs) {
	int res;
	int len = 0;
	Stroke *news;
	ifs >> len;
	for(int ii=0; ii<len; ii++) {
		news = new Stroke(MAX_PTS);
		if (!news) return 0;
		InsertEnd(news);
		res = news->Read(ifs);
		if(!res) return 0;
		news->ProcessInk();
	}
	return 1;
}


int Stroke::Write(ofstream &ofs)
{

	ofs << EndData << endl;

	int ii;
	for(ii=0; ii<EndData; ii++) {
		ofs << Ink[ii].P.x << '\t' << Ink[ii].P.y << '\t' << Ink[ii].Pressure << '\t' 
			<< Ink[ii].Tilt  << '\t' << Ink[ii].Dir << '\t' << Ink[ii].TimeStamp << endl;
	}
	return 1;
}



int Stroke::Read(ifstream &ifs)
{

	int Pressure;
	double Tilt, Dir;
	DWORD TimeStamp;
	class vect P;
	int pts;

	ifs >> pts;
	int ii;
	for(ii=0; ii<pts; ii++) {
		ifs >> P.x >> P.y >> Pressure >> Tilt  >> Dir >> TimeStamp;
		AddPoint((int)P.x, (int)P.y,Pressure,Tilt,Dir, TimeStamp);
	}
	return 1;
}


int Stroke::WriteData(CString filename)
{

	if (EndData < 3) return 0;

	ofstream dataout(filename);


	if(!Processed) {
		ProcessInk();
	} 

	double max;
	int ii, dat;

	for(dat=0; dat<DAT_MAX; dat++) {
		max = -1;
		for(ii=0; ii<EndData; ii++) {
			if(fabs(Ink[ii].GetData(dat)) > max) 
				max = fabs(Ink[ii].GetData(dat));
		}
		if(max < 1.0e-5) max = 1.0;
		if(dat == DAT_ARCLEN) max = 1.0;


		// turn off scaling
		max = 1.0;


		dataout << (LPCTSTR)DataLabels[dat] << "\t";
		for(ii=0; ii<EndData; ii++) {
			dataout << Ink[ii].GetData(dat)/max << "\t";
		}
		dataout << endl;
	}

	dataout.close();
	return 1;
}



int Stroke::PlotData()
{
	if (EndData < 3) return 0;

	if(!Processed) {
		ProcessInk();
	} 




	double max;
	int ii, dat;

	
	// draw the background
		glColor3f(1.0, 1.0, 1.0);
		glBegin(GL_POLYGON);
			glVertex3d(0.0,-1.1,-0.1);
			glVertex3d(Ink[EndData-1].GetData(DAT_ARCLEN), -1.1, -0.1);
			glVertex3d(Ink[EndData-1].GetData(DAT_ARCLEN), 1.1, -0.1);
			glVertex3d(0.0,1.1,-0.1);
		glEnd();

		glColor3f(0.0, 0.0, 0.0);
		glBegin(GL_LINES);
			glVertex3d(0.0,0.0,0.0);
			glVertex3d(Ink[EndData-1].GetData(DAT_ARCLEN), 0.0, 0.0);
		glEnd();


	// draw the data traces
	for(dat=0; dat<DAT_MAX; dat++) {
		if(DataVisible[dat]) {

			glEnable(GL_LINE_STIPPLE);
			switch(DataLineType[dat])
			{
				case 1:
					glLineStipple(3, 0xCCCC);
					break;
				case 2:
					glLineStipple(3, 0x00FF);
					break;
				case 3:
					glLineStipple(3, 0x18FF);
					break;
				case 4:
					glLineStipple(3, 0x667E);
					break;
				default:
					glDisable(GL_LINE_STIPPLE);
					break;
			}

			max = -1;
			for(ii=0; ii<EndData; ii++) {
				if(fabs(Ink[ii].GetData(dat)) > max) 
					max = fabs(Ink[ii].GetData(dat));
			}
			glColor3dv(DataClr[dat]);
			if(max < 1.0e-5) max = 1.0;
			if(DataScale[dat] > 0.0001) max *= DataScale[dat];
			if(DataScale[dat] < -0.0001) max = -DataScale[dat];

			if(dat == DAT_VMAG) {
				glColor3dv(DataClr[dat]);
				glBegin(GL_LINE_STRIP);
					glVertex3d(0.0,vave/max,0.05);
					glVertex3d(Ink[EndData-1].S, vave/max,0.05);
				glEnd();
			}


			if( dat == DAT_LS_TRATE) {
				double tr = 3.0;
				glColor3dv(DataClr[dat]);
				glBegin(GL_LINE_STRIP);
					glVertex3d(0.0,trave/max,0.05);
					//cout << "Tangent Rate LS Approach, TRAVE =" << trave << endl;
					glVertex3d(Ink[EndData-1].S, trave/max,0.05);
				glEnd();
			}



			glBegin(GL_LINE_STRIP);
				for(ii=0; ii<EndData; ii++) {
					glVertex3d(Ink[ii].GetData(DAT_ARCLEN), Ink[ii].GetData(dat)/max, 0.0);
				}
			glEnd();
		}
	}
	glDisable(GL_LINE_STIPPLE);

	// if a point has been selected, draw a vertical line to mark the spot on the traces
	if(Selected>=0) {
		glLineWidth(2.0);
		glColor3f(1.0,0.0,1.0);
		glBegin(GL_LINE_STRIP);
			glVertex3d(Ink[Selected].GetData(DAT_ARCLEN), -1.0, 0.0);
			glVertex3d(Ink[Selected].GetData(DAT_ARCLEN), 1.0, 0.0);
		glEnd();
		glLineWidth(1.0);
	}



	return 1;
}




int Stroke::ProcessInk(void)
{
	if(EndData == 0) return 0;

	// added Oct 30, 2003
	// to accomodate change in Wacom driver
	if(EndData > 2 && Ink[0].TimeStamp > Ink[1].TimeStamp) {
		Ink[0].TimeStamp = Ink[1].TimeStamp-1;
	}

	Compress();
	CalcCurv();
	CalcPenAlignment();
	CalcKinematics();
	Processed = 1;
	LastSegment = 0;

	
	CalcSegPoints_Speed();
	CalcSegPoints_TR();
	CalcSegPoints_TR_Sign();
	CalcPrims();
	
	int ii;
	//cout << "\ninitial Prims\n" ;
	for(ii=0; ii<seglist.GetSize(); ii++) {
		//cout << seglist[ii]->fit_error << '\t' << seglist[ii]->type << '\t' << seglist[ii]->start_point << '\t' << seglist[ii]->end_point  << endl;
	}

	//cout << endl << endl << "clean and merge\n";
	if(ParamCleanEnds)  {
		CleanFirstLastSegs();
		//cout << "\nClean ends\n" ;
		for(ii=0; ii<seglist.GetSize(); ii++) {
		//cout << seglist[ii]->fit_error << '\t' << seglist[ii]->type << '\t' << seglist[ii]->start_point << '\t' << seglist[ii]->end_point  << endl;
		}
	}


	
	if(ParamMergeShort1) {
		MergeShortSegs();
		//cout << "\nMergeShort1\n" ;
		for(ii=0; ii<seglist.GetSize(); ii++) {
		//cout << seglist[ii]->fit_error << '\t' << seglist[ii]->type << '\t' << seglist[ii]->start_point << '\t' << seglist[ii]->end_point  << endl;
		}
	}

	if(ParamMergeSimilar1) {
		MergeSimilarSegs();
		//cout << "\nMergeSimilar1\n";
		for(ii=0; ii<seglist.GetSize(); ii++) {
		//cout << seglist[ii]->fit_error << '\t' << seglist[ii]->type << '\t' << seglist[ii]->start_point << '\t' << seglist[ii]->end_point  << endl;
		}
	}

	if(ParamSplit1) {
		SplitSeg();
		//cout << "\nSplit1\n";
		for(ii=0; ii<seglist.GetSize(); ii++) {
		//cout << seglist[ii]->fit_error << '\t' << seglist[ii]->type << '\t' << seglist[ii]->start_point << '\t' << seglist[ii]->end_point  << endl;
		}
	}
	


	if(ParamSplit2) {
		SplitSeg();
		//cout << "\nSplit2\n";
		for(ii=0; ii<seglist.GetSize(); ii++) {
		//cout << seglist[ii]->fit_error << '\t' << seglist[ii]->type << '\t' << seglist[ii]->start_point << '\t' << seglist[ii]->end_point  << endl;
		}
	}


	if(ParamMergeShort2) {
		MergeShortSegs();
		//cout << "\nMergeShort2\n";
		for(ii=0; ii<seglist.GetSize(); ii++) {
		//cout << seglist[ii]->fit_error << '\t' << seglist[ii]->type << '\t' << seglist[ii]->start_point << '\t' << seglist[ii]->end_point  << endl;
		}
	}
	

	if(ParamMergeSimilar2) {
		MergeSimilarSegs();
		//cout << "\nMergeSimilar2\n";
		for(ii=0; ii<seglist.GetSize(); ii++) {
		//cout << seglist[ii]->fit_error << '\t' << seglist[ii]->type << '\t' << seglist[ii]->start_point << '\t' << seglist[ii]->end_point  << endl;
		}
	}
	


	
	return 1;
}







void Stroke::CalcSegPoints_Speed()
{
	double ave;
	int ii;
	
	if(!Processed) 
		ProcessInk();

	ave = 0;
	for(ii=0; ii<EndData; ii++) {
		ave += Ink[ii].GetVmag();
	}
	ave /= (double) EndData;

	ave = (Ink[EndData-1].S - Ink[0].S) / (Ink[EndData-1].TimeStamp - Ink[0].TimeStamp); 


	vave = ave;
	/*
#ifdef HIGH_RES
	ave *= 1.00;
#else
	ave *=.25;
#endif
	//ave *=.75;
*/
	ave *= ParamSpeedThresh;
	
	double max, min;
	int ride_up_valley;
	

	min = Ink[0].GetVmag();
	ride_up_valley=0;
	for(ii=1; ii<EndData; ii++) {
		if(!ride_up_valley) {
			if(Ink[ii].GetVmag() < min) {
				min = Ink[ii].GetVmag();
			} else {
				if(min < ave ) {
					if(LastSegment < MaxData * STYPES) {
						SegmentPoints[LastSegment] = ii-1;
						SegmentPointType[LastSegment] = SPEED;
						LastSegment++;
					}
				}
				ride_up_valley=1;
				max = Ink[ii].GetVmag(); 
				min = 10000.0;
			}

		} else {
			if(Ink[ii].GetVmag() > max) {
				max = Ink[ii].GetVmag();
			} else {
				ride_up_valley = 0;
			}
		}
	}
}


void Stroke::CalcSegPoints_TR()
{
	double max, min;
	int ii;
	int ride_down_peak;
	
	if(!Processed) 
		ProcessInk();

	max= fabs(Ink[0].LS_TRate);
	ride_down_peak=0;
	for(ii=1; ii<EndData; ii++) {
		if(!ride_down_peak) {
			if(fabs(Ink[ii].LS_TRate) > max) {
				max = fabs(Ink[ii].LS_TRate);
			} else {
				if(max > 0.75 && Ink[ii-1].GetVmag() < 0.8*vave ) {
					if(LastSegment < MaxData * STYPES) {
						SegmentPoints[LastSegment] = ii-1;
						SegmentPointType[LastSegment] = CSPIKE;
						LastSegment++;
					}
				}
				ride_down_peak=1;
				min = fabs(Ink[ii].LS_TRate); 
				max = 0.0;
			}
		} else {
			if(fabs(Ink[ii].LS_TRate) < min) {
				min = fabs(Ink[ii].LS_TRate);
			} else {
				ride_down_peak = 0;
			}
		}
	}
}



void Stroke::CalcSegPoints_TR_Sign()
{
	int ii;
	if(!Processed) 
		ProcessInk();

	for(ii=1; ii<EndData; ii++) {
	// LS_TR_Sign is thresholded to give values that are -1, 0, or 1
	// here we are looking for a change in ABSOLUTE VALUE from 1 to 0, or vice versa
	// this constant has no special significance
#define STHRESH 0.9
		if( (fabs(Ink[ii-1].LS_TR_Sign) < STHRESH && fabs(Ink[ii].LS_TR_Sign) > STHRESH) ||
			(fabs(Ink[ii-1].LS_TR_Sign) > STHRESH && fabs(Ink[ii].LS_TR_Sign) < STHRESH) ){

			if(LastSegment < MaxData * STYPES) {
				SegmentPoints[LastSegment] = ii;
				SegmentPointType[LastSegment] = CSIGN;
				LastSegment++;
			}
		}
	}
}




double Stroke::CalcLSTRate(int low, int hi, int pt)
{


	InkData TmpInk[200];

	// make sure the input data is valid
	if(hi - low < 3) return 0;
	if (hi < 0) return 0;
	if (low < 0) return 0;
	if (hi > EndData) return 0;
	if (pt < low || pt >= hi) return 0;

	int tii = 0;
	int jj;

	tii=0;
	for(jj=low;jj<hi;jj++) {
		TmpInk[tii]=Ink[jj];
		TmpInk[tii].P.x = TmpInk[tii].S;
		TmpInk[tii].P.y = TmpInk[tii].TDir;
		tii++;
	}

	double err, A, B;
	err = fit_line(&TmpInk[0], 0, tii, &A, &B);
	//return (fabs(B));
	return B;

}




double Stroke::CalcLSTRateSign(int low, int hi, int pt)
{
	double ave = 0;



	// make sure the input data is valid
	if(hi - low < 3) return 0;
	if (hi < 0) return 0;
	if (low < 0) return 0;
	if (hi > EndData) return 0;
	if (pt < low || pt >= hi) return 0;

	int jj;

	for(jj=low;jj<hi;jj++) {
		ave += Ink[jj].LS_TRate;
	}

	ave /= (double) (hi - low) ;
	//cout << "Ave curvature " << ave << endl;
		

	
#define STRS 0.1
	if(ave > STRS) return 1.0;
	if(ave < -STRS) return -1.0;
	return 0.0;

}





void Stroke::CalcPrims(void)
{

	if(EndData < 10) return;
	int *spts;
	spts = new int[MaxData];
	if(!spts) {
		cout << "Our of memory!!!!!! in calcprims\n";
		return;
	}
	

	int ii, jj, start, end;

	spts[0] = 1;
	for(ii=0; ii<EndData; ii++) {
		spts[ii] = 0;
		for(jj=0; jj<LastSegment; jj++) {
			if(SegmentPoints[jj] == ii && SegmentPointType[jj] != CSIGN) {
				spts[ii] = 1;
				break;
			}
		}
	}
	spts[EndData-1] = 1;
// purge segment points that are clustered


#define POINT_MERGE_TOL 7
//For leslie's code
//#define POINT_MERGE_TOL 5
	int pa, pb;
	pa = 0;
	pb = 1;
	while(pb<EndData) {
		while(pb<EndData && !spts[pb]) pb++;
		if(pb - pa < POINT_MERGE_TOL) {
			spts[pb]=0;
		}
		pa = pb;
		pb = pb + 1;
	}
	// hack on dec 21 2006
	if(spts[EndData-2] || spts[EndData-3] || spts[EndData-4]) {
		spts[EndData-2] = spts[EndData-3] = spts[EndData-4] = 0;
	}



/*
#define LMERGETOL .07
	int pa, pb, pc;
	int done = 0;
	while(!done) {
		done = 1;
		pa = 0;
		pb = 1;
		while(pb<EndData && !spts[pb]) pb++;
		pc = pb;
		while(pc<EndData) {
			pc++;
			while(pc<EndData && !spts[pc]) pc++;
			if((Ink[pb].S - Ink[pa].S) < LMERGETOL*(Ink[pc].S - Ink[pb].S) || 
				LMERGETOL*(Ink[pb].S - Ink[pa].S) > (Ink[pc].S - Ink[pb].S)) {
				spts[pb]=0;
				pb = pc;
				pc = pb + 1;
				done = 0;
			} else { 
				pa = pb;
				pb = pc;
				pc = pb + 1;
			}
		}
	}
*/
	int i = 0;
	start = 0;
	end = 1;
	while(end < EndData) {
		while(spts[end] != 1 && end < EndData-1) {
			end++;
		}

		//seglist.InsertEnd(FitSegment(start, end));
		/*
		if (i++ == 4) {
			debug = true;
		} else {
			debug = false;
		}
		*/
		Segment *s = FitSegment(start, end);
		seglist.InsertEnd(s);

		start = end;
		end++;
	}

	delete[] spts;
}







double fitline(InkData fl_ink[], int low, int hi, double *sx,
			   double *sy, double *ex, double *ey)
{
	double A, B;	
	double err;


	InkData TmpInk[1000];
	int ii, jj;

	/*
	// make sure the input data is valid
	if ((hi - low < 3) || (hi < 0) || (low < 0) || (hi > EndData)) {
		cout << "************************\n";
		cout << "Invalid data in fitline\n";
		return 1000000.0;
	}
*/


	
	// check for a vertical line
	double xx = -1000000.0, xn = 1000000.0;
	for(ii=low;ii<hi;ii++) {
		if(fl_ink[ii].P.x < xn) xn = fl_ink[ii].P.x;
		if(fl_ink[ii].P.x > xx) xx = fl_ink[ii].P.x;

	}

	double yx = -1000000.0, yn = 1000000.0;
	for(ii=low;ii<hi;ii++) {
		if(fl_ink[ii].P.y < yn) yn = fl_ink[ii].P.y;
		if(fl_ink[ii].P.y > yx) yx = fl_ink[ii].P.y;

	}
	
	vect n,o;
	vect pt;
	//if(xx - xn < .05*(fl_ink[hi-1].S - fl_ink[low].S)) {
	if(xx - xn < yx - yn) {
		jj=0;
		for(ii=low;ii<hi;ii++) {  //rotate coord 90 deg so line appears horizontal
			TmpInk[jj].P.x = -fl_ink[ii].P.y;
			TmpInk[jj].P.y = fl_ink[ii].P.x;
			jj++;
		}
		err = fit_line(&TmpInk[0], 0, jj, &A, &B);
		o = vect(A, 0.0);
		n = vect(-B, 1.0);
		n.normalize();
		pt = fl_ink[low].P - o;
		pt = (pt*n) * n + o;
		*sx = pt.x;
		*sy = pt.y;
		pt =  fl_ink[hi-1].P - o;
		pt = (pt*n) * n + o;
		*ex = pt.x;
		*ey = pt.y;
	} else {
		err = fit_line(fl_ink, low, hi, &A, &B);
		o = vect(0.0, A);
		n = vect(1.0, B);
		n.normalize();
		pt = fl_ink[low].P - o;
		pt = (pt*n) * n + o;
		*sx = pt.x;
		*sy = pt.y;
		pt =  fl_ink[hi-1].P - o;
		pt = (pt*n) * n + o;
		*ex = pt.x;
		*ey = pt.y;
	}
	return err;
}

double fitarc(InkData fl_ink[], int low, int hi, double *cx, double *cy,
			  double *rad, double *start_ang, double *end_ang,
			  // Begin Burak's code
			  int *dir, double *real_sang, double *real_eang)
			  // End Burak's code
{
	int ii, jj;
	double a[3][3], d[3], x[3];
	double A, B, C;
	double minX = 100000, minY = 100000, maxX = 0, maxY = 0;

	// solve least squares circle fit

	// construct the matrix
	for(ii=0;ii<3;ii++) {
		d[ii] = 0.0;
		for(jj=0;jj<3;jj++) {
			a[ii][jj] = 0.0;
		}
	}

	for(ii=low;ii<hi;ii++) {
		minX = min(minX, fl_ink[ii].P.x);
		maxX = max(maxX, fl_ink[ii].P.x);
		minY = min(minY, fl_ink[ii].P.y);
		maxY = max(maxY, fl_ink[ii].P.y);
		a[0][0] += 2.0*sqr(fl_ink[ii].P.x);
		a[0][1] += 2.0*fl_ink[ii].P.x*fl_ink[ii].P.y;
		a[0][2]  += fl_ink[ii].P.x;
		d[0] += -(sqr(fl_ink[ii].P.x) + sqr(fl_ink[ii].P.y))*fl_ink[ii].P.x;
	
		
		a[1][1] += 2.0*sqr(fl_ink[ii].P.y);
		a[1][2]  += fl_ink[ii].P.y;
		d[1] += -(sqr(fl_ink[ii].P.x) + sqr(fl_ink[ii].P.y))*fl_ink[ii].P.y;

		a[2][2]  += 1.0;
		d[2] += -(sqr(fl_ink[ii].P.x) + sqr(fl_ink[ii].P.y));

	}
	a[1][0] = a[0][1];
	a[2][0] = 2*a[0][2];
	a[2][1] = 2*a[1][2];



	int res;
	res = SVDC_SolveAXD(a, x, d);

	/*
	if (debug) {
		double xxx[3];
		int r2, iii;
		r2 = SolveAXD(a, xxx, d);
		printf("%f %f %f\n", xxx[0], xxx[1], xxx[2]);
		//for(int i=0; i<3; i++) {
		//	if(fabs(xxx[i] - x[i]) > 0.1) {
		//		printf("Whoops: %f, %f\n", xxx[i], x[i]);
		//	}
		//}
	}
	*/

	A = x[0];
	B = x[1];
	C = x[2];
	/*
	if (debug) {
		printf("%f %f %f\n", A, B, C);
	}
	*/

	*rad = sqrt(sqr(A) + sqr(B) - C);
	// XXX
	//printf("ink = %.4f, ink' = %.4f, xdiff = %.4f, ydiff = %.4f\n",
	//	   *rad, *rad * 1.20, (maxX - minX) / 2, (maxY - minY) / 2);
/*
	*rad = max(*rad, (maxX - minX) / 2);
	*rad = max(*rad, (maxY - minY) / 2);
	*rad += 1;
*/
	*cx = -A;
	*cy = -B;


	vect cord, tst;
	double tmp;
	*start_ang = atan2(fl_ink[low].P.y - *cy, fl_ink[low].P.x - *cx);
	*end_ang = atan2(fl_ink[hi-1].P.y - *cy, fl_ink[hi-1].P.x - *cx);

	// Begin Burak's code
	*real_sang = *start_ang;
	*real_eang = *end_ang;
	if (*real_sang < 0.0) *real_sang += 2.0 * PI;
	if (*real_eang < 0.0) *real_eang += 2.0 * PI;
	*dir = 1;
	// End Burak's code

	cord = fl_ink[hi-1].P - fl_ink[low].P;
	tst = fl_ink[(hi+low)/2].P - vect(*cx, *cy);
	cord.normalize();
	cord.rotCcw90();
	if(cord*tst > 0.0 ) 
	{
		tmp = *start_ang;
		*start_ang = *end_ang;
		*end_ang = tmp;
		*dir = -1;
	}

	if(*start_ang < 0.0) *start_ang += 2.0*PI;

	while(*end_ang < *start_ang) *end_ang += 2.0*PI;

	// check for full circles
	if(fl_ink[hi-1].S - fl_ink[low].S > 2.0*PI*(*rad) ) {
		*end_ang = *start_ang + 2.0 * PI;
		// Begin Burak's code
		*real_sang = 0.0;
		*real_eang = 2.0 * PI - 1.0 * PI / 180.0;
		*dir = 1;
		// End Burak's code
	}


	double cerr;
	cerr = 0;
//	for(ii=low;ii<hi;ii++) {
//		cerr += sqrt(fabs( sqr(fl_ink[ii].P.x) + 2.0*A*fl_ink[ii].P.x + 2.0*B*fl_ink[ii].P.y + sqr(fl_ink[ii].P.y) +C ));
//	}


	cerr = 0.0;
	for(ii=low;ii<hi;ii++) {
		cerr += fabs(sqrt(sqr(fl_ink[ii].P.x - *cx) + sqr(fl_ink[ii].P.y - *cy)) - *rad);
	}

	cerr /= (double) (hi - low);
	return(cerr);

}









void SegmentList::Draw(void)
{
	int ii;
	glNormal3f(0.0,0.0,1.0);
	for(ii=0; ii<GetSize(); ii++) {
		if(ii % 2) {
			glColor3f(0.0, 0.0, 1.0);
		} else {
			glColor3f(1.0, 0.0, 0.0);
		}
#ifdef SCREEN_CAPTURE
		glColor3f(0.0, 0.0, 0.0);
#endif
		GetAt(ii)->Draw();
	}
}

void Segment::Draw(void)
{

	double a, xx, yy;
	int jj;

	//cout << "length is :" << FitLength() << endl;
	glNormal3f(0.0, 0.0, 1.0);

	glLineWidth(PICTURE_WIDTH);

	if(type == LINE_SEG) {

		glBegin(GL_LINE_STRIP);

		xx = (float) line.sx/INK_SCALE;
		yy = (float) line.sy/INK_SCALE;
		glVertex3f((float)xx, (float)yy, (float) 0.07);

		xx = (float) line.ex/INK_SCALE;
		yy = (float) line.ey/INK_SCALE;
		glVertex3f((float)xx, (float)yy, (float) 0.07);

		glEnd();

#ifdef SCREEN_CAPTURE
		if(!show_seg_point  && show_seg_ends  ) {
			class GLUquadric *quad;
			quad = gluNewQuadric();
			gluQuadricNormals(quad,GLU_SMOOTH);
			
			glPushMatrix();
			xx = (float) line.sx/INK_SCALE;
			yy = (float) line.sy/INK_SCALE;
			glTranslatef(xx, yy, 0.07);
			gluDisk(quad, 0.0, 0.06, 20, 20);
			glPopMatrix();

			glPushMatrix();
			xx = (float) line.ex/INK_SCALE;
			yy = (float) line.ey/INK_SCALE;
			glTranslatef(xx, yy, 0.07);
			gluDisk(quad, 0.0, 0.06, 20, 20);
			glPopMatrix();
			gluDeleteQuadric(quad);
		}

#endif

	} else {
		glBegin(GL_LINE_STRIP);

		for(jj=0;jj<=25;jj++) {
			a = (arc.eang -arc.sang) * (double) jj / 25.0 + arc.sang;
			xx = (float)(arc.cx + arc.rad*cos(a)) /INK_SCALE;
			yy = (float)(arc.cy + arc.rad*sin(a)) /INK_SCALE;
			glVertex3f((float)xx, (float)yy, (float)0.07);

		}
		glEnd();


#ifdef SCREEN_CAPTURE
		if(!show_seg_point && show_seg_ends) {
			class GLUquadric *quad2;
			quad2 = gluNewQuadric();
			gluQuadricNormals(quad2,GLU_SMOOTH);
			
			glPushMatrix();
			a = arc.sang;
			xx = (float)(arc.cx + arc.rad*cos(a)) /INK_SCALE;
			yy = (float)(arc.cy + arc.rad*sin(a)) /INK_SCALE;
			glTranslatef(xx, yy, 0.07);
			gluDisk(quad2, 0.0, 0.06, 20, 20);
			glPopMatrix();

			glPushMatrix();
			a = arc.eang;
			xx = (float)(arc.cx + arc.rad*cos(a)) /INK_SCALE;
			yy = (float)(arc.cy + arc.rad*sin(a)) /INK_SCALE;
			glTranslatef(xx, yy, 0.07);
			gluDisk(quad2, 0.0, 0.06, 20, 20);
			glPopMatrix();
			gluDeleteQuadric(quad2);
		}

#endif




	}

	glLineWidth(1.0);
}



#define SEG_MERGE_LENGTH_TOL 0.2
#define SEG_MERGE_ERROR_TOL 1.1


void Stroke::MergeShortSegs(void)
{
	int ii;
	int done = 0;
	Segment *segp;
	int merged = 0;

	CalcArcLength();
	for(ii=0;ii<seglist.GetSize();ii++) {
		seglist[ii]->prune = 0;
	}

	while(!done) {
		done = 1;
		for(ii=0;ii<seglist.GetSize();ii++) {
			if(seglist[ii]->prune) {
				delete seglist[ii];
				seglist.RemoveAt(ii);
			}
		}

		for(ii=1;ii<seglist.GetSize();ii++) {
			if(seglist[ii-1]->length(this) < SEG_MERGE_LENGTH_TOL * seglist[ii]->length(this)) {
				segp = FitSegment(seglist[ii-1]->start_point, seglist[ii]->end_point, seglist[ii]->type);
				if(segp->fit_error < SEG_MERGE_ERROR_TOL* ( seglist[ii-1]->fit_error + seglist[ii]->fit_error)) {
					segp->merged = seglist[ii]->merged + 1;
					delete seglist[ii];
					seglist[ii] = segp;
					seglist[ii-1]->prune = 1;
					done = 0;
					merged++;
				} else {
					delete segp;
				}
			} else if(seglist[ii]->length(this) < SEG_MERGE_LENGTH_TOL * seglist[ii-1]->length(this)) {
				segp = FitSegment(seglist[ii-1]->start_point, seglist[ii]->end_point, seglist[ii-1]->type);
				if(segp->fit_error < SEG_MERGE_ERROR_TOL* ( seglist[ii-1]->fit_error + seglist[ii]->fit_error)) {
					segp->merged = seglist[ii-1]->merged + 1;
					delete seglist[ii-1];
					seglist[ii-1] = segp;
					seglist[ii]->prune = 1;
					done = 0;
					merged++;
				} else {
					delete segp;
				}
			}
		}
	}
	//cout << "Merged " << merged << " short segs with long ones\n";
}



void Stroke::MergeSimilarSegs(void)
{

	int ii;

	int done = 0;
	vect v1, v2;
	Segment *segp;
	int merged = 0;



	for(ii=0;ii<seglist.GetSize();ii++) {
		seglist[ii]->prune = 0;
	}

	while(!done) {
		done = 1;
		for(ii=0;ii<seglist.GetSize();ii++) {
			if(seglist[ii]->prune) {
				delete seglist[ii];
				seglist.RemoveAt(ii);
			}
		}

		for(ii=1;ii<seglist.GetSize();ii++) {
			if(seglist[ii-1]->type == seglist[ii]->type) {
				//cout << "Considering similar-seg merge \n";
				cout.flush();
				if(seglist[ii]->type == LINE_SEG) {
					 v1 = Ink[seglist[ii-1]->end_point].P - Ink[seglist[ii-1]->start_point].P;
					 v2 = Ink[seglist[ii]->end_point].P - Ink[seglist[ii]->start_point].P;
					 v1.normalize();
					 v2.normalize();
					 if(v1*v2 > .75) {
						segp = FitSegment(seglist[ii-1]->start_point, seglist[ii]->end_point, LINE_SEG);
		 				if(segp->fit_error < SEG_MERGE_ERROR_TOL* ( seglist[ii-1]->fit_error + seglist[ii]->fit_error)) {
							segp->merged = max(seglist[ii-1]->merged,seglist[ii]->merged) + 1;
							delete seglist[ii-1];
							seglist[ii-1] = segp;
							seglist[ii]->prune = 1;
							done = 0;
							merged++;
						} else {
							delete segp;
						}
					}
				} else {  // dealing with an arc
					if( (seglist[ii-1]->arc.sang < seglist[ii]->arc.sang && seglist[ii]->arc.sang < seglist[ii]->arc.eang) ||
						(seglist[ii-1]->arc.sang > seglist[ii]->arc.sang && seglist[ii]->arc.sang > seglist[ii]->arc.eang) ) {
						segp = FitSegment(seglist[ii-1]->start_point, seglist[ii]->end_point, ARC_SEG);
						if(segp->fit_error < SEG_MERGE_ERROR_TOL* ( seglist[ii-1]->fit_error + seglist[ii]->fit_error)) {
							double t1, t2, t3;
							t1 = seglist[ii-1]->fit_error ;
							t2 = seglist[ii]->fit_error ;
							t3 = segp->fit_error;
							segp->merged = max(seglist[ii-1]->merged,seglist[ii]->merged) + 1;
							delete seglist[ii-1];
							seglist[ii-1] = segp;
							seglist[ii]->prune = 1;
							done = 0;
							merged++;
						} else {
							delete segp;
						}
					}
				}
			}
		}
	}
	//cout << "Merged " << merged << " similar segs\n";
}


#define END_LEN_AVE_NUM 3
#define END_LEN_TOL 0.1
#define MIN_END_LEN 15.0
int Stroke::CleanFirstLastSegs(void)
{

	int ii, last, first;
	double avelen = 0.0;
	int removed = 0;

	if(seglist.GetSize() <  2) return 0;

	first = 1;
	last = END_LEN_AVE_NUM;
	if (last > seglist.GetSize()) last = seglist.GetSize();
	
	
	avelen = 0.0;
	for(ii=first; ii<last; ii++) {
		avelen += seglist[ii]->length(this);
	} 
	avelen /= (double) (last-first+1);

	if(seglist[0]->length(this)  < END_LEN_TOL*avelen || seglist[0]->FitLength() < MIN_END_LEN) {
		delete seglist[0];
		seglist.RemoveAt(0);
		removed++;
		//cout << "Deleted first seg\n";
	}


	last = seglist.GetSize() -1;
	first = seglist.GetSize() - END_LEN_AVE_NUM;
	if (first < 0) first = 0;
	if (last < 0) last = 0;

	
	avelen = 0.0;
	for(ii=first; ii<last; ii++) {
		avelen += seglist[ii]->length(this);
	} 
	avelen /= (double) (last-first+1);

	
	if(seglist[last]->length(this)  < END_LEN_TOL*avelen || seglist[last]->FitLength() < MIN_END_LEN) {
		delete seglist[last];
		seglist.RemoveAt(last);
		removed++;
		//cout << "Deleted last seg\n";
	}
	return removed;
}




#define SPLIT_ERROR_LIMIT 7.0
#define SPLIT_IMPROVEMENT_MIN 0.65
void Stroke::SplitSeg(void)
{

	int ii, jj;


	int *spts;
	spts = new int[MaxData];
	if(!spts) {
		cout << "Our of memory!!!!!! in SplitSeg\n";
		return;
	}

	// gather up all of the CSIGN seg points
	// do this for all segments
	for(ii=0; ii<EndData; ii++) {
		spts[ii] = 0;
		for(jj=0; jj<LastSegment; jj++) {
			if(SegmentPoints[jj] == ii && SegmentPointType[jj] == CSIGN) {
				spts[ii] = 1;
				break;
			}
		}
	}


	Segment *sega, *segb;

	



	for(ii=0;ii<seglist.GetSize();ii++) {
		int best = -1;
		double best_error = 10.0 * seglist[ii]->fit_error;  // make the initial value large

		if(seglist[ii]->fit_error > SPLIT_ERROR_LIMIT) {
			for(jj=seglist[ii]->start_point+1; jj<seglist[ii]->end_point; jj++) {
				if(spts[jj]) {
					sega = FitSegment(seglist[ii]->start_point, jj);
					segb = FitSegment(jj, seglist[ii]->end_point);
					if(sega->fit_error + segb->fit_error < best_error) {
						best = jj;
						best_error = sega->fit_error + segb->fit_error;
					}
					delete sega;
					delete segb;
				}
				if(best_error < SPLIT_IMPROVEMENT_MIN * seglist[ii]->fit_error && best > -1) {
					// cout << "Best error " << best_error << "  original error " << seglist[ii]->fit_error << endl;
					sega = FitSegment(seglist[ii]->start_point, best);
					segb = FitSegment(best, seglist[ii]->end_point);
					int num_splits;
					num_splits = seglist[ii]->split + 1;
					delete seglist[ii];
					seglist[ii] = sega;
					sega->split = num_splits;
					seglist.InsertAt(ii+1, segb);
					segb->split = num_splits;
					ii++;   /// don't immediatly try splitting again
				}
			}
		}
	}

	delete []spts;
	cout.flush();
}



#define MIN_ARC_TOL 3.14149/5.0

Segment * Stroke::FitSegment(int start, int end, int segtype)
{
	double lerr, aerr;
	class Segment *segp;
	double sx, sy, ex, ey;
	double cx, cy, rad;
	double sang, eang;
	// Begin Burak's code
	// In the case of a line, those 3 values are undefined,
	// zero seems to be a good default value.
	double real_sang = 0.0, real_eang = 0.0;
	int dir = 0;
	// End Burak's code

	segp = new Segment;

	sy=sx=0;  // added 
	if(!segtype) {  // find best fit
		lerr = fitline(&Ink[0], start, end, &sx, &sy, &ex, &ey);
		aerr = fitarc(&Ink[0], start, end, &cx, &cy, &rad, &sang, &eang,
			// Begin Burak's code
			&dir, &real_sang, &real_eang);
			// End Burak's code
		/*
		if (debug) {
			printf("lerr = %f, aerr = %f\n", lerr, aerr);
		}
		*/
	} else if(segtype == LINE_SEG) {   // fit to a line
		lerr = fitline(&Ink[0], start, end, &sx, &sy, &ex, &ey);
		aerr = 100000.0;
	} else {  // fit to an arc
		lerr = 10000.0;
		aerr = fitarc(&Ink[0], start, end, &cx, &cy, &rad, &sang, &eang,
			// Begin Burak's code
			&dir, &real_sang, &real_eang);
			// End Burak's code
	}

	if(segtype == LINE_SEG || lerr < aerr || (eang - sang) <  MIN_ARC_TOL) {
		segp->line.sx = sx;
		segp->line.sy = sy;
		segp->line.ex = ex;
		segp->line.ey = ey;
		segp->type = LINE_SEG;
		segp->start_point = start;
		segp->end_point = end;
		segp->fit_error = lerr;
	} else {
		segp->arc.cx = cx;
		segp->arc.cy = cy;
		segp->arc.rad = rad;
		segp->arc.sang = sang;
		segp->arc.eang = eang;
		// Begin Burak's code
		segp->arc.real_sang = real_sang;
		segp->arc.real_eang = real_eang;
		segp->arc.dir = dir;
		// End Burak's code
		segp->type = ARC_SEG;
		segp->start_point = start;
		segp->end_point = end;
		segp->fit_error = aerr;
	}
	segp->merged = 0;
	segp->prune = 0;

	return segp;
}



				   
double Segment::length(class Stroke *stroke) {
	double len;
	len = stroke->Ink[end_point].S - stroke->Ink[start_point].S;
	return(len);
}


double Segment::FitLength(void) {
	double len;
	
	if(type == LINE_SEG)
	{
		len = sqrt(sqr(line.sx - line.ex) + sqr(line.sy - line.ey));
	} else {
		len = (arc.eang - arc.sang) * arc.rad;
	} 
	return(len);
}


void RawSketch::Draw(void) {
	Strokes.Draw();
}


int RawSketch::Write(ofstream &ofs)
{
	return Strokes.Write(ofs);
}
	
int RawSketch::Read(ifstream &ifs)
{
	return Strokes.Read(ifs);
}
	


int ImplementEdit(Stroke *EditStroke, RawSketch *sketch)
{
	Stroke *DataStroke;
	Segment *segp, *segnew, *segtemp;
	int ii, jj;
	double dist;
	double tol;
	int ClosestPoint=0;
	double ClosestDist=1000000.0;
	vect spoint, epoint, dir, tv, tmpvect;
	double len;
	double max_x, min_x, min_y, max_y;

	segp = EditStroke->FitSegment(0, EditStroke->EndData-1);
	EditStroke->seglist.Clear();
	EditStroke->seglist.InsertEnd(segp);
	
	for(jj=sketch->Strokes.GetSize()-1;jj>=0; jj--) {
		DataStroke = sketch->Strokes[jj];
		if(segp->type == LINE_SEG || (segp->type==ARC_SEG && segp->arc.eang -segp->arc.sang < PI)) {
			spoint = vect(segp->line.sx, segp->line.sy);
			epoint = vect(segp->line.ex, segp->line.ey);
			dir = epoint-spoint;
			len = sqrt(dir * dir);
			dir = (1.0/len) * dir;

			
			min_x = min_y = 100000.0;
			max_x = max_y = -100000.0;

			for(ii=0; ii<EditStroke->EndData; ii++) {
				if(EditStroke->Ink[ii].P.x < min_x) min_x = EditStroke->Ink[ii].P.x;
				if(EditStroke->Ink[ii].P.x > max_x) max_x = EditStroke->Ink[ii].P.x;
				if(EditStroke->Ink[ii].P.y < min_y) min_y = EditStroke->Ink[ii].P.y;
				if(EditStroke->Ink[ii].P.y > max_y) max_y = EditStroke->Ink[ii].P.y;
			}
			tol = (max_x - min_x) / 20.0;
			min_x -= tol;
			max_x += tol;
			min_y -= tol;
			max_y += tol;

			ClosestDist=1000000.0;
			for(ii=0; ii<DataStroke->EndData; ii++) {
				if(DataStroke->Ink[ii].P.x < max_x && 
					DataStroke->Ink[ii].P.x > min_x &&
					DataStroke->Ink[ii].P.y < max_y && 
					DataStroke->Ink[ii].P.y > min_y) {
						tv = DataStroke->Ink[ii].P  - spoint;
						tv = (tv * dir) * dir;
						tv = tv + spoint;
						tv = DataStroke->Ink[ii].P  - tv;
						dist = sqrt(tv*tv);

						// fix: stop looking once we are close enough
						if(dist < ClosestDist) {
							ClosestDist = dist;
							ClosestPoint = ii;
						}
				}
			}
			//if(ClosestPoint != 0) {
			if(ClosestDist < 20.0) {
				for(ii=0; ii<DataStroke->seglist.GetSize(); ii++) {
					if(ClosestPoint > DataStroke->seglist[ii]->start_point &&
						ClosestPoint <  DataStroke->seglist[ii]->end_point) {
						if(ll_intersect(spoint,epoint,vect(DataStroke->Ink[ClosestPoint-1].P.x, DataStroke->Ink[ClosestPoint-1].P.y),
							vect(DataStroke->Ink[ClosestPoint+1].P.x, DataStroke->Ink[ClosestPoint+1].P.y), &tmpvect)) {

								segtemp = DataStroke->FitSegment(DataStroke->seglist[ii]->start_point, ClosestPoint); 
								segnew = DataStroke->FitSegment(ClosestPoint, DataStroke->seglist[ii]->end_point);
								segtemp->merged = DataStroke->seglist[ii]->merged;
								segnew->merged = DataStroke->seglist[ii]->merged;
								segtemp->split= DataStroke->seglist[ii]->split + 1;
								segnew->split= DataStroke->seglist[ii]->split + 1;
								delete DataStroke->seglist[ii];
								DataStroke->seglist[ii] = segtemp;
								DataStroke->seglist.InsertAt(ii+1, segnew);
								EditStroke->seglist.Clear();
								EditStroke->EndData = 0;
								return(1);
						}
						break;
					}
				}

			}

		} else {
			int foundsome = 0;
			for(ii=1; ii<DataStroke->seglist.GetSize(); ii++) {
				dist = sqrt(sqr(segp->arc.cx - DataStroke->Ink[DataStroke->seglist[ii]->start_point].P.x) +
							sqr(segp->arc.cy - DataStroke->Ink[DataStroke->seglist[ii]->start_point].P.y));
				if(dist < segp->arc.rad) {
					segnew = DataStroke->FitSegment(DataStroke->seglist[ii-1]->start_point, DataStroke->seglist[ii]->end_point);
					segnew->merged = max(DataStroke->seglist[ii-1]->merged, DataStroke->seglist[ii]->merged) + 1;
						delete DataStroke->seglist[ii];
						delete DataStroke->seglist[ii-1];
						DataStroke->seglist[ii-1] = segnew;
						DataStroke->seglist.RemoveAt(ii);
						ii--; // need to see if merged segment is still inside the circle
						foundsome = 1;
				}
			}
			if(foundsome) {
				EditStroke->seglist.Clear();
				EditStroke->EndData = 0;
				return(1);
			}

		}
	}
	EditStroke->seglist.Clear();
	EditStroke->EndData = 0;
	return 0;
}




vect intersectpoint = vect(10000.0,10000.0);
int Segment::intersect(class Segment *otherseg)
{
	
	intersectpoint = vect(10000.0,10000.0);
	if(type == LINE_SEG && otherseg->type == LINE_SEG) {
		return(ll_intersect(vect(line.sx, line.sy), vect(line.ex, line.ey), 
			vect(otherseg->line.sx, otherseg->line.sy), vect(otherseg->line.ex, otherseg->line.ey), &intersectpoint) );

	} else if(type == LINE_SEG && otherseg->type == ARC_SEG) {
		return(lc_intersect(vect(line.sx, line.sy), vect(line.ex, line.ey), vect(otherseg->arc.cx, otherseg->arc.cy),
			otherseg->arc.rad, otherseg->arc.sang, otherseg->arc.eang, &intersectpoint));
		
	} else if(type == ARC_SEG && otherseg->type == LINE_SEG) {
		return(lc_intersect(vect(otherseg->line.sx, otherseg->line.sy), vect(otherseg->line.ex, otherseg->line.ey), 
			vect(arc.cx, arc.cy), arc.rad, arc.sang, arc.eang, &intersectpoint));
	} else {
		// we don't handle the arc arc case
		return 0;
	}
}



int ImplementErase(Stroke *EditStroke, RawSketch *sketch)
{
	Segment *segp;
	int ii;
	int numstrokes;
	
	EditStroke->ProcessInk();
	

	// construct the bouding box
	double xmax= - 10000.0, xmin= 10000.0, ymax= - 10000.0, ymin= 10000.0;
	double x, y;
	for(ii=0; ii<EditStroke->LastSegment; ii++) {
		x = EditStroke->Ink[EditStroke->SegmentPoints[ii]].P.x;
		y = EditStroke->Ink[EditStroke->SegmentPoints[ii]].P.y;
		if(x > xmax) xmax = x;
		if(x < xmin) xmin = x;
		if(y > ymax) ymax = y;
		if(y < ymin) ymin = y;
	}


	// figure out which corners of the bounding box are conneted by the eraser strokes
	double x1, y1, x2, y2, ang;
	ang = 0.0;
	numstrokes = EditStroke->seglist.GetSize();
	for(ii=0; ii<EditStroke->seglist.GetSize(); ii++) {
		if(EditStroke->seglist[ii]->type == LINE_SEG) {
			x1 = EditStroke->seglist[ii]->line.sx;
			y1 = EditStroke->seglist[ii]->line.sy;
			x2 = EditStroke->seglist[ii]->line.ex;
			y2 = EditStroke->seglist[ii]->line.ey;
		} else {
			x1 = EditStroke->seglist[ii]->arc.cx +
				EditStroke->seglist[ii]->arc.rad * cos(EditStroke->seglist[ii]->arc.sang);
			y1 = EditStroke->seglist[ii]->arc.cy +
				EditStroke->seglist[ii]->arc.rad * sin(EditStroke->seglist[ii]->arc.sang);
			x2 = EditStroke->seglist[ii]->arc.cx +
				EditStroke->seglist[ii]->arc.rad * cos(EditStroke->seglist[ii]->arc.eang);
			y2 = EditStroke->seglist[ii]->arc.cy +
				EditStroke->seglist[ii]->arc.rad * sin(EditStroke->seglist[ii]->arc.eang);
		}
		y2 = y2 - y1;
		x2 = x2 - x1;
		if(x2 < 0.0) {
			x2 *= -1.0;
			y2 *= -1.0;
		}
		ang = ang + atan2(y2, x2);
	}
	ang /= ii;
	

	if(ang >= 0.0) {
		x1 = xmin;
		y1 = ymin;
		x2 = xmax;
		y2 = ymax;
	} else {
		x1 = xmin;
		y1 = ymax;
		x2 = xmax;
		y2 = ymin;
	}



	// we really are not going to use the stroke that fits the first and last point
	// this just allocates the object
	// we are going to replace the line in the segment with the
	// average line calculated above
	segp = EditStroke->FitSegment(0, EditStroke->EndData-1, LINE_SEG);
	EditStroke->seglist.Clear();
	EditStroke->seglist.InsertEnd(segp);

	segp->line.sx = x1;
	segp->line.sy = y1;
	segp->line.ex = x2;
	segp->line.ey = y2;



	int found = 0;
	int jj;

	for(ii = sketch->Strokes.GetSize()-1; ii >= 0; ii--) {
		for(jj=0; jj<sketch->Strokes[ii]->seglist.GetSize(); jj++) 
		{
			if(sketch->Strokes[ii]->seglist[jj]->intersect(segp)) {
				if(numstrokes > 10 || (numstrokes > 3 && sketch->Strokes[ii]->seglist.GetSize() == 1)) {
				//if(EraseStroke|| (EraseSeg && sketch->Strokes[ii]->seglist.GetSize() == 1)) {
					delete sketch->Strokes[ii];
					sketch->Strokes.RemoveAt(ii);
				} else if (numstrokes > 3) {
				//} else if (EraseSeg) {
					Stroke *strk;
					strk = sketch->Strokes[ii]->Split(jj);
					if(sketch->Strokes[ii]->seglist.GetSize() == 0) {
						delete sketch->Strokes[ii];
						sketch->Strokes[ii] = strk;
					} else if (strk != NULL) {
						sketch->Strokes.InsertAt(ii+1, strk);
					}
				}
				edit_stroke->seglist.Clear();
				edit_stroke->EndData = 0;
				return 1;
			}
		}
	}
	edit_stroke->seglist.Clear();
	edit_stroke->EndData = 0;

	return 0;
}


Stroke* Stroke::Split(int seg)
{

	int ii, tmp;
	int *sp, *spt;
	int splitpoint;
	int newend;
	Stroke *newstroke = new Stroke(MaxData);
	
	newstroke->Processed = Processed;
	newstroke->vave = vave;
	newstroke->trave = trave;
	newstroke->Selected = 0;

	splitpoint = seglist[seg]->end_point;
	newend= seglist[seg]->start_point;


	sp = new int[LastSegment];
	spt = new int[LastSegment];

	tmp = LastSegment;
	for(ii=0;ii<tmp;ii++) {
		sp[ii] = SegmentPoints[ii];
		spt[ii] = SegmentPointType[ii];
	}

	LastSegment = newstroke->LastSegment = 0;
	for(ii=0;ii<tmp;ii++) {
		if(sp[ii] <= newend) {
			SegmentPoints[LastSegment] = sp[ii];
			SegmentPointType[LastSegment] = spt[ii];
			LastSegment++;
		}
		if(sp[ii] >= splitpoint) {
			newstroke->SegmentPoints[newstroke->LastSegment] = sp[ii] - splitpoint;
			newstroke->SegmentPointType[newstroke->LastSegment] = spt[ii];
			newstroke->LastSegment++;
		}
	}

	for(ii=seglist.GetSize()-1; ii>seg; ii--) {
		seglist[ii]->start_point -= splitpoint;
		seglist[ii]->end_point -= splitpoint;
		newstroke->seglist.InsertFront(seglist[ii]);
		seglist.RemoveAt(ii);
	}

	for(ii=splitpoint; ii<EndData; ii++) 
	{
		newstroke->Ink[ii-splitpoint] = Ink[ii];
	}

	newstroke->EndData = EndData - splitpoint;

	delete seglist[seg];
	seglist.RemoveAt(seg);
	EndData = newend+1;

	delete[] sp;
	delete[] spt;

	Selected = -1;
	newstroke->Selected = -1;

	if(newstroke->EndData == 0) {
		delete newstroke;
		newstroke = NULL;
	}
	return (newstroke);
}


