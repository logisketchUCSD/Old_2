/***********************************************

	Copyright (C) 2002 - 2005
	UC Riverside Smart Tools Lab and Thomas Stahovich 
		

***********************************************/

// OpenGL View ClassDoc.cpp : implementation of the COpenGLViewClassDoc class
//

#include "stdafx.h"

#include <GL/gl.h>
#include <GL/glu.h>

#include <GL/glaux.h>
#include <stdlib.h>
#include <math.h>
#include "gl_view.h"
#include <string.h>


#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif




/*  global variable: Matrix to store accumulated view
    modifications */
GLdouble view_mat[] = { 1.0, 0.0, 0.0, 0.0,
			0.0, 1.0, 0.0, 0.0,
			0.0, 0.0, 1.0, 0.0,
			0.0, 0.0, 0.0, 1.0};


/*	global variables used to store changes in view
	of the scene (pan, zoom, rotate) until they can
	be included in the viewing matrix by ControlView()
*/

GLfloat trans_x = 0.0, trans_y=0.0;
GLfloat spinx=0.0, spiny=0.0, spinz=0.0;
GLfloat zoom = INITZOOM;
int startx = 0;
int starty = 0;

/*  global variables that indicate the current function of 
	the mouse (i.e., panning, zooming, rotating x-y, rotating z)
	and the status of the mouse buttons.
*/

int pan=1;
int rotmode=1;
int light=1;
int two_sided = 0;
int leftdown;
int rightdown;

/* This function initializes the screen and sets the background to black */
void InitDraw(void)
{
  GLfloat light_diffuse[] = {1.0, 1.0, 1.0, 1.0};  /* white diffuse light. */
  GLfloat light_specular[] = {1.0, 1.0, 1.0, 1.0};  
  GLfloat light_ambient[] = {1.0, 1.0, 1.0, 1.0};  
  GLfloat light_position[] = {0.0, 0.0, 1.0, 0.0};  /* Infinite light location. */

#ifdef NO_TABLET
    glClearColor(1.0f, 1.0f, 1.0f, 1.0f);
#else
	// specify black as clear color
    glClearColor (0.0f,0.0f,0.0f,0.0f);
#endif

  
  glShadeModel (GL_SMOOTH);

  /* turn on lighting */
  //glLightfv(GL_LIGHT0, GL_DIFFUSE, light_diffuse);
  //glLightfv(GL_LIGHT0, GL_POSITION, light_position);
  //glEnable(GL_LIGHT0);
  //glEnable(GL_LIGHTING);
  //glEnable(GL_COLOR_MATERIAL);
  //glColorMaterial(GL_FRONT, GL_DIFFUSE);

  /* turn on hidden surface removal */
  glEnable(GL_DEPTH_TEST);

  /* shade the front but make the back of each polygon transparent (i.e., wireframe) */
  glPolygonMode(GL_BACK, GL_LINE);
  glPolygonMode(GL_FRONT, GL_FILL);


  glMatrixMode(GL_MODELVIEW);
  glLoadIdentity ();
  glTranslatef(-8.5,-6.5,0.0);
  glGetDoublev(GL_MODELVIEW_MATRIX, view_mat);


}



void DrawView(void)
{
  glClear (GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
  glMatrixMode(GL_MODELVIEW);
  ViewControl();  /* apply rotation, translation, and zoom to get desired view */
  //DrawAxes(); /* draw coordinate axes */

}



    


void DrawAxes(void)
{
  glColor3f (1.0, 0.0, 0.0);
  glBegin(GL_LINES);
  glVertex3f(0.0, 0.0, 0.0);
  glVertex3f(1.0, 0.0, 0.0);
  glEnd();
  glColor3f (0.0, 1.0, 0.0);
  glBegin(GL_LINES);
  glVertex3f(0.0, 0.0, 0.0);
  glVertex3f(0.0, 1.0, 0.0);
  glEnd();
  glColor3f (0.0, 0.0, 1.0);
  glBegin(GL_LINES);
  glVertex3f(0.0, 0.0, 0.0);
  glVertex3f(0.0, 0.0, 1.0);
  glEnd();
}



/* this view control routine always rotates and translate relative the global
fixed coordinate system */
void ViewControl(void)
{
  /* start with the identity matrix */
  glLoadIdentity ();
  /* mult by the incremtal tranlations and rotations */
  glTranslatef(trans_x,trans_y,0.0);
  glRotatef(spiny, 1.0, 0.0, 0.0);
  glRotatef(spinx, 0.0, 1.0, 0.0);
  glRotatef(spinz, 0.0, 0.0, 1.0);
  /* RIGHT multiply by the preview view matrix */
  glMultMatrixd(view_mat);
  glGetDoublev(GL_MODELVIEW_MATRIX, view_mat);
  /* save as the current view matrix */

  /* postion the camera */
  glLoadIdentity ();
  gluLookAt (0.0, 0.0, zoom, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0);
  /* RIGHT multiply by the current view */
  glMultMatrixd(view_mat);  
  trans_x = trans_y = spinx = spiny = spinz = 0.0;
}


double *cross_prod(double a[], double b[], double c[])
{
  c[0] = a[1]*b[2]-a[2]*b[1];
  c[1] = -a[0]*b[2]+a[2]*b[0];
  c[2] = a[0]*b[1]-a[1]*b[0];
  return(c);
}


double dot_prod(double a[], double b[])
{
  return (a[0]*b[0]+a[1]*b[1]+a[2]*b[2]);
}









/// putting fonts on the screen

GLubyte space[] =
    {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
GLubyte letters[][13] = {
    {0x00, 0x00, 0xc3, 0xc3, 0xc3, 0xc3, 0xff, 0xc3, 0xc3, 0xc3, 0x66, 0x3c, 0x18},
    {0x00, 0x00, 0xfe, 0xc7, 0xc3, 0xc3, 0xc7, 0xfe, 0xc7, 0xc3, 0xc3, 0xc7, 0xfe},
    {0x00, 0x00, 0x7e, 0xe7, 0xc0, 0xc0, 0xc0, 0xc0, 0xc0, 0xc0, 0xc0, 0xe7, 0x7e},
    {0x00, 0x00, 0xfc, 0xce, 0xc7, 0xc3, 0xc3, 0xc3, 0xc3, 0xc3, 0xc7, 0xce, 0xfc},
    {0x00, 0x00, 0xff, 0xc0, 0xc0, 0xc0, 0xc0, 0xfc, 0xc0, 0xc0, 0xc0, 0xc0, 0xff},
    {0x00, 0x00, 0xc0, 0xc0, 0xc0, 0xc0, 0xc0, 0xc0, 0xfc, 0xc0, 0xc0, 0xc0, 0xff},
    {0x00, 0x00, 0x7e, 0xe7, 0xc3, 0xc3, 0xcf, 0xc0, 0xc0, 0xc0, 0xc0, 0xe7, 0x7e},
    {0x00, 0x00, 0xc3, 0xc3, 0xc3, 0xc3, 0xc3, 0xff, 0xc3, 0xc3, 0xc3, 0xc3, 0xc3},
    {0x00, 0x00, 0x7e, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x7e},
    {0x00, 0x00, 0x7c, 0xee, 0xc6, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06},
    {0x00, 0x00, 0xc3, 0xc6, 0xcc, 0xd8, 0xf0, 0xe0, 0xf0, 0xd8, 0xcc, 0xc6, 0xc3},
    {0x00, 0x00, 0xff, 0xc0, 0xc0, 0xc0, 0xc0, 0xc0, 0xc0, 0xc0, 0xc0, 0xc0, 0xc0},
    {0x00, 0x00, 0xc3, 0xc3, 0xc3, 0xc3, 0xc3, 0xc3, 0xdb, 0xff, 0xff, 0xe7, 0xc3},
    {0x00, 0x00, 0xc7, 0xc7, 0xcf, 0xcf, 0xdf, 0xdb, 0xfb, 0xf3, 0xf3, 0xe3, 0xe3},
    {0x00, 0x00, 0x7e, 0xe7, 0xc3, 0xc3, 0xc3, 0xc3, 0xc3, 0xc3, 0xc3, 0xe7, 0x7e},
    {0x00, 0x00, 0xc0, 0xc0, 0xc0, 0xc0, 0xc0, 0xfe, 0xc7, 0xc3, 0xc3, 0xc7, 0xfe},
    {0x00, 0x00, 0x3f, 0x6e, 0xdf, 0xdb, 0xc3, 0xc3, 0xc3, 0xc3, 0xc3, 0x66, 0x3c},
    {0x00, 0x00, 0xc3, 0xc6, 0xcc, 0xd8, 0xf0, 0xfe, 0xc7, 0xc3, 0xc3, 0xc7, 0xfe},
    {0x00, 0x00, 0x7e, 0xe7, 0x03, 0x03, 0x07, 0x7e, 0xe0, 0xc0, 0xc0, 0xe7, 0x7e},
    {0x00, 0x00, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0xff},
    {0x00, 0x00, 0x7e, 0xe7, 0xc3, 0xc3, 0xc3, 0xc3, 0xc3, 0xc3, 0xc3, 0xc3, 0xc3},
    {0x00, 0x00, 0x18, 0x3c, 0x3c, 0x66, 0x66, 0xc3, 0xc3, 0xc3, 0xc3, 0xc3, 0xc3},
    {0x00, 0x00, 0xc3, 0xe7, 0xff, 0xff, 0xdb, 0xdb, 0xc3, 0xc3, 0xc3, 0xc3, 0xc3},
    {0x00, 0x00, 0xc3, 0x66, 0x66, 0x3c, 0x3c, 0x18, 0x3c, 0x3c, 0x66, 0x66, 0xc3},
    {0x00, 0x00, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x3c, 0x3c, 0x66, 0x66, 0xc3},
    {0x00, 0x00, 0xff, 0xc0, 0xc0, 0x60, 0x30, 0x7e, 0x0c, 0x06, 0x03, 0x03, 0xff}
};

GLuint fontOffset;

void makeRasterFont(void)
{
   GLuint i, j;
   glPixelStorei(GL_UNPACK_ALIGNMENT, 1);

   fontOffset = glGenLists (128);
   for (i = 0,j = 'A'; i < 26; i++,j++) {
      glNewList(fontOffset + j, GL_COMPILE);
      glBitmap(8, 13, 0.0, 2.0, 10.0, 0.0, letters[i]);
      glEndList();
   }
   glNewList(fontOffset + ' ', GL_COMPILE);
   glBitmap(8, 13, 0.0, 2.0, 10.0, 0.0, space);
   glEndList();
}


void printString(char *s)
{
   glPushAttrib (GL_LIST_BIT);
   glListBase(fontOffset);
   glCallLists(strlen(s), GL_UNSIGNED_BYTE, (GLubyte *) s);
   glPopAttrib ();
}


