/***********************************************

	Copyright (C) 2002 - 2005
	UC Riverside Smart Tools Lab and Thomas Stahovich 
		

***********************************************/

#define ANGINC 1.0
#define PANINC 90.0
#define ZOOMINC 75.0
//#define INITZOOM 26.0
#define INITZOOM 16.0


#define NO_TABLET 1


void InitDraw(void);
void DrawView();
void ViewControl(void);
void DrawAxes(void);

double dot_prod(double a[], double b[]);
double *cross_prod(double a[], double b[], double c[]);


extern int pan;
extern int rotmode;
extern int light;
extern int two_sided;
extern int leftdown;
extern int rightdown;


extern GLfloat trans_x;
extern GLfloat trans_y;
extern GLfloat spinx;
extern GLfloat spiny;
extern GLfloat spinz;
extern GLfloat zoom;
extern int startx;
extern int starty;


void printString(char *s);
void makeRasterFont(void);
