/***********************************************

	Copyright (C) 2002 - 2005
	UC Riverside Smart Tools Lab and Thomas Stahovich 
		

***********************************************/

#ifndef INK_H
#define INK_H
#include <math.h>
#include <iostream>
#include <fstream>

using namespace std;

#define MAX_PTS 6000  // 10/3/02 increased from 2000

#ifndef PI
#define PI 3.14159
#endif
//#define HIGH_RES 1



#ifdef HIGH_RES
#define INK_SCALE (float)120.0
#else
//#define INK_SCALE (float)60.0
#define INK_SCALE (float)1.0
#endif

#define LINE_SEG  1
#define ARC_SEG  2

#define  DAT_ARCLEN		0
#define  DAT_X			1
#define  DAT_Y			2
#define  DAT_VMAG		3
#define  DAT_VX			4
#define  DAT_VY			5
#define  DAT_AMAG		6
#define  DAT_AX			7
#define  DAT_AY			8
#define  DAT_AN			9
#define  DAT_AT			10
#define  DAT_ALIGN		11
#define  DAT_RELALIGN	12
#define  DAT_ALIGNNORM	13
#define  DAT_ALIGNTANG	14
#define  DAT_TILT		15	
#define  DAT_PRESSURE	16
#define  DAT_RADCUR		17
#define  DAT_CSIGN		18
#define  DAT_TDIR		19
#define  DAT_TRATE		20
#define  DAT_LS_TRATE	21
#define  DAT_TR_SIGN	22
#define  DAT_MAX		23


#define STYPES 5
#define SPEED 0
#define CSPIKE 1
#define CSIGN 2

extern CString DataLabels[];
extern int DataVisible[];
extern double DataScale[];
extern double DataClr[][3];
extern int DataLineType[];

class PenState
{
public:
	double PenTilt;
	double PenDir; 
	int PenX;
	int PenY;
	double PenPressure;
	int TipDown;
	int Button1;
	int Button2;
	CTime LastPenMove;
	int ShowPen;
	PenState()
	{
		ShowPen = 1;
		TipDown = 0;
	};
};



class vect
{
public:
	double x, y;
	vect()
	{
		x=y=0.0;
	}
	vect(double xx, double yy) 
	{
		x = xx;
		y = yy;
	}
	vect(double ang)
	{
		x = cos(PI*ang/180.0);
		y = sin(PI*ang/180.0);
	}

	/*
	vect operator+(vect a)
	{
		vect c;
		c.x = a.x;
		c.y = a.y;
		return c;
	}
	vect operator-(vect a)
	{
		vect c;
		c.x = -a.x;
		c.y = -a.y;
		return c;
	}
*/

	void normalize(void)
	{
		double len = x*x + y*y;
		len = sqrt(len);
		if(fabs(len) < 1.0e-6) return;
		x = x/len;
		y = y/len;
	}
	double length(void)
	{
		return sqrt(x*x + y*y);
	}
	void rotcw90()
	{
		double tmp = x;
		x = y;
		y = -tmp;
	}

	void rotCcw90()
	{
		double tmp = x;
		x = -y;
		y = tmp;
	}

};

class InkData
{
public:
	int Pressure, CSign, NormSign;
	class vect Tang;
	double Tilt, Dir;
	DWORD TimeStamp;
	class vect A, V, P;
	double RadCur;
	double Align;
	int RelAlign;
	double S, TRate, LS_TRate;
	double TDir;
	double LS_TR_Sign;
	double sspeed;
	InkData() :
		// Added by weesan@cs.ucr.edu
		Pressure(0),
		CSign(0),
		NormSign(0),
		Tilt(0),
		Dir(0),
		TimeStamp(0),
		RadCur(0),
		Align(0),
		RelAlign(0),
		S(0),
		TRate(0),
		LS_TRate(0),
		TDir(0),
		LS_TR_Sign(0),
		sspeed(0) {
		// Commented out by weesan@cs.ucr.edu
		/*
		#pragma warning( disable : 4244 )
		Tilt=Pressure=CSign=NormSign=Dir=TimeStamp=RadCur=Align=RelAlign=S=TRate=0;
		#pragma warning( default : 4244 )
		*/
	}
	double GetAn(void);
	double GetAt(void);
	double GetAmag(void);
	double GetVmag(void);
	double GetTDir(void);
	double GetData(int ii);



};


class TomLine
{
public:
	double sx, ex, sy, ey;
	// Added by weesan@cs.ucr.edu
	TomLine(void) : sx(0), ex(0), sy(0), ey(0) {
	}
};

class TomArc
{
public:
	double cx, cy, rad;
	double sang, eang;
	// Begin Burak's code
	double real_sang, real_eang;
	// End Burak's code
	int dir;
	// Added by weesan@cs.ucr.edu
	TomArc(void) :
		cx(0), cy(0), rad(0), sang(0), eang(0),
		real_sang(0), real_eang(0), dir(0) {
	}
};

class Segment
{
public:
	int type;
	int start_point, end_point;
	class TomArc arc;
	class TomLine line;
	int split;
	int merged;
	int prune;
	double fit_error;
	Segment()
	{
		split = merged = 0;
		type = 0;
	}
	void Draw(void);
	double length(class Stroke *stroke);
	double FitLength(void);
	int intersect(class Segment *otherseg);
	
};

class SegmentList
{
public:
	CPtrArray slist;

	~SegmentList(void) {
		Clear();
	}
	class Segment*& operator[](int i)
	{
		return( (class Segment *&) slist[i] );
	}
	class Segment*& GetAt(int i)
	{
		return( (class Segment *&) slist[i] );
	}
	int GetSize(void)
	{
		return(slist.GetSize());
	}
	int RemoveAt(int i)
	{
		if(i >= GetSize()) return 0;
		slist.RemoveAt(i);
		return(1);
	}
	void Clear(void)
	{
		for(int ii=0;ii<GetSize();ii++) {
			delete (Segment *)slist[ii];
		}
		slist.RemoveAll();
	}
	void InsertAt(int i, class Segment *seg)
	{
		slist.InsertAt(i, (CObject *) seg);
	}
	void InsertFront(class Segment *seg)
	{
		slist.InsertAt(0, (CObject *) seg);
	}
	void InsertEnd(class Segment *seg)
	{
		slist.InsertAt(slist.GetSize(), (CObject *) seg);
	}

	void Draw(void);

};


#define MAX_PRIMS 500

class Stroke
{
public:
	InkData *Ink;
	int MaxData, EndData;
	int *SegmentPoints;
	int *SegmentPointType;
	int LastSegment;
	int Processed;
	int Selected;
	double vave, trave;
	int ProcessInk(void);
	SegmentList seglist;

	Stroke();
	Stroke(int max);
	~Stroke();
	void AddPoint(int X, int Y, int Pressure, double Tilt, double Dir, DWORD TimeStamp);
	void Draw(void);
	int Compress();
	int CalcCurv();
	int CalcPenAlignment();
	int CalcKinematics();
	int WriteData(CString filename);
	int PlotData(void);
	int CalcCircle(double *rad, vect *cen, int low, int hi, int pt, vect *tangent, int *NormSign);
	void CalcSegPoints_Speed();
	void CalcSegPoints_TR();
	void CalcSegPoints_TR_Sign();
	double CalcLSTRate(int low, int hi, int pt);
	double CalcLSTRateSign(int low, int hi, int pt);
	double Stroke::CalcArcLength(void);
	double Stroke::DensifyData(InkData TmpInk[], int low, int hi, 
		int pt, int den, int *thept, int *tii);
	void CalcPrims(void);
	void MergeShortSegs(void);
	void MergeSimilarSegs(void);
	Segment* FitSegment(int start, int end, int segtype=0);
	int CleanFirstLastSegs(void);
	void SplitSeg(void);
	Stroke* Split(int seg);
	int CountSpeedSegs(void);
	int Write(ofstream &ofs);
	int Read(ifstream &ifs);

};


class StrokeList {
public:
	CPtrArray stroke_list;

	~StrokeList(void) {
		Clear();
	}
	class Stroke*& operator[](int i)
	{
		return( (class Stroke *&) stroke_list[i] );
	}
	class Stroke*& GetAt(int i)
	{
		return( (class Stroke *&) stroke_list[i] );
	}
	int GetSize(void)
	{
		return(stroke_list.GetSize());
	}
	int RemoveAt(int i)
	{
		if(i >= GetSize()) return 0;
		stroke_list.RemoveAt(i);
		return(1);
	}
	void Clear(void)
	{
		for(int ii=0;ii<GetSize();ii++) {
			delete (Stroke *)stroke_list[ii];
		}
		stroke_list.RemoveAll();
	}
	void InsertAt(int i, class Stroke *strk)
	{
		stroke_list.InsertAt(i, (CObject *) strk);
	}
	void InsertFront(class Stroke *strk)
	{
		stroke_list.InsertAt(0, (CObject *) strk);
	}
	void InsertEnd(class Stroke *strk)
	{
		stroke_list.InsertAt(stroke_list.GetSize(), (CObject *) strk);
	}

	void Draw(void) {
		for(int ii=0; ii<GetSize(); ii++) {
			((class Stroke *) stroke_list[ii])->Draw();
		}
	}

	int Write(ofstream &ofs);
	int Read(ifstream &ifs);


};


class RawSketch{
public:
	int id;
	class StrokeList Strokes;
	void Draw(void);
	int Write(ofstream &ofs);
	int Read(ifstream &ofs);

};

class SketchList {
public:
	CPtrArray sketch_list;

	~SketchList(void) {
		Clear();
	}
	class RawSketch*& operator[](int i)
	{
		return( (class RawSketch *&) sketch_list[i] );
	}
	class RawSketch*& GetAt(int i)
	{
		return( (class RawSketch *&) sketch_list[i] );
	}
	int GetSize(void)
	{
		return(sketch_list.GetSize());
	}
	int RemoveAt(int i)
	{
		if(i >= GetSize()) return 0;
		sketch_list.RemoveAt(i);
		return(1);
	}
	void Clear(void)
	{
		for(int ii=0;ii<GetSize();ii++) {
			delete (RawSketch *)sketch_list[ii];
		}
		sketch_list.RemoveAll();
	}
	void InsertAt(int i, class RawSketch *sk)
	{
		sketch_list.InsertAt(i, (CObject *) sk);
	}
	void InsertFront(class RawSketch *sk)
	{
		sketch_list.InsertAt(0, (CObject *) sk);
	}
	void InsertEnd(class RawSketch *sk)
	{
		sketch_list.InsertAt(sketch_list.GetSize(), (CObject *) sk);
	}

	void Draw(void);
};


inline vect operator+(vect a, vect b)
{
	vect c;
	c.x = a.x + b.x;
	c.y = a.y + b.y;
	return c;
}

inline vect operator-(vect a, vect b)
{
	vect c;
	c.x = a.x - b.x;
	c.y = a.y - b.y;
	return c;
}

inline double operator*(vect a, vect b)
{
	return (a.x * b.x + a.y * b.y);
}

inline vect operator*(vect a, double b)
{
	vect c;
	c.x = a.x * b;
	c.y = a.y * b;
	return c;
}

inline vect operator*(double b, vect a)
{
	vect c;
	c.x = a.x * b;
	c.y = a.y * b;
	return c;
}

inline vect operator/(vect a, double b)
{
	vect c;
	c.x = a.x / b;
	c.y = a.y / b;
	return c;
}


int DrawPen(PenState pstate);


int StartNewSketch(void);


int ImplementEdit(Stroke *EditStroke, RawSketch *sketch);
int ImplementErase(Stroke *EditStroke, RawSketch *sketch);


double fitline(InkData fl_ink[], int low, int hi, double *sx,
			   double *sy, double *ex, double *ey);

double fitarc(InkData fl_ink[], int low, int hi, double *cx, double *cy,
			  double *rad, double *start_ang, double *end_ang,
			  // Begin Burak's code
			  int *dir, double *real_sang, double *real_eang);
			  // End Burak's code

int dir_point_intersect(vect pa, vect pb, vect na, vect nb, vect *p);
int ll_intersect(vect sa, vect ea, vect sb, vect eb, vect *p);
int lc_intersect(vect pa, vect pb, vect c, double rad, double sang, double eang, vect *p);



#endif




int ReadSketch(ifstream &ifs);
int WriteSketch(ofstream &ofs);


extern double ParamSpeedThresh;
extern int ParamCleanEnds;
extern int ParamMergeSimilar1;
extern int ParamMergeSimilar2;
extern int ParamMergeShort1;
extern int ParamMergeShort2;
extern int ParamSplit1;
extern int ParamSplit2;
