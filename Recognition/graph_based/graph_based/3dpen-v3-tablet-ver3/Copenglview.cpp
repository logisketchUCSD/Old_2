/***********************************************

	Copyright (C) 2002 - 2005
	UC Riverside Smart Tools Lab and Thomas Stahovich 
		

***********************************************/

// COpenGLView.cpp : implementation of the COpenGLView class
//

#include "stdafx.h"
#include "OpenGL View Class.h"

#include "OpenGL View ClassDoc.h"
#include "COpenGLView.h"
#include "gl_view.h"
#include "ink.h"
#include "plotDialog.h"
#include "ProcessDialog.h"
#include <time.h>
#include "psrender.h"
// global variables used to update the status bar
PenState pstate;
CString StatusText;
int print_circ = 0;
int show_ink = 1;
int show_seg_point = 1;
int show_seg_ends = 1;
int show_seg = 1;
int show_pen = 1;
int show_data = 0;

long TabMaxX, TabMaxY;

//double ParamSpeedThresh = 0.25;
// weesan: changed speed threshold to 0.55 for better recognition rate
double ParamSpeedThresh = 0.55;
int ParamCleanEnds = 1;
int ParamMergeSimilar1 =1 ;
int ParamMergeSimilar2 =1;
int ParamMergeShort1 = 1;
int ParamMergeShort2 =1;
int ParamSplit1 =1;
int ParamSplit2 =1;







// Storage for the sketch data
SketchList Sketches;
class Stroke *stroke_data=NULL;
class RawSketch *CurrentSketch;
int StartNewStroke = 1;
int StartNewEdit = 1;
int DoingEdit = 0;
int DoingErase = 0;
int StartNewErase = 1;
int EraseSeg = 0;
int EraseStroke = 0;


// storage for editing gestures
Stroke *edit_stroke = new Stroke(MAX_PTS);





//#include <windows.h>
#ifdef HIGH_RES
#define PACKETDATA      (PK_X | PK_Y | PK_BUTTONS | PK_TIME | PK_CURSOR | PK_STATUS)
#else
#define PACKETDATA      (PK_X | PK_Y | PK_BUTTONS | PK_TIME | PK_NORMAL_PRESSURE | PK_ORIENTATION | PK_CURSOR | PK_STATUS)
#endif

#define PACKETMODE      0
#include <math.h>
#include <string.h>
//#include "tablet.h"
#include "pktdef.h"


/* converts FIX32 to double */
#define FIX_DOUBLE(x)   ((double)(INT(x))+((double)FRAC(x)/65536))
#define pi 3.14159265359





#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif



const char* const COpenGLView::_ErrorStrings[]= {
				{"No Error"},					// 0
				{"Unable to get a DC"},			// 1
				{"ChoosePixelFormat failed"},	// 2
				{"SelectPixelFormat failed"},	// 3
				{"wglCreateContext failed"},	// 4
				{"wglMakeCurrent failed"},		// 5
				{"wglDeleteContext failed"},	// 6
				{"SwapBuffers failed"},			// 7

		};

/////////////////////////////////////////////////////////////////////////////
// COpenGLView

IMPLEMENT_DYNCREATE(COpenGLView, CView)

BEGIN_MESSAGE_MAP(COpenGLView, CView)
	ON_MESSAGE(WT_PACKET, OnWTPacket)
	ON_MESSAGE(WM_DONESETTINGS, OnDoneSettings)
	//{{AFX_MSG_MAP(COpenGLView)
	ON_WM_CREATE()
	ON_WM_DESTROY()
	ON_WM_ERASEBKGND()
	ON_WM_SIZE()
	ON_WM_LBUTTONDOWN()
	ON_WM_LBUTTONUP()
	ON_WM_MOUSEMOVE()
	ON_WM_MOUSEWHEEL()
	ON_WM_RBUTTONUP()
	ON_WM_RBUTTONDOWN()
	ON_COMMAND(IDM_WriteData, OnWriteData)
	ON_COMMAND(IDM_PLOT_DATA, OnPlotData)
	ON_COMMAND(ID_PLOTSETTINGS, OnPlotsettings)
	ON_WM_KEYUP()
	ON_WM_TIMER()
	ON_COMMAND(ID_FILE_OPEN, OnFileOpen)
	ON_COMMAND(ID_FILE_SAVE, OnFileSave)
	ON_COMMAND(ID_FILE_SAVE_AS, OnFileSaveAs)
	ON_COMMAND(IDM_SEGMENT_SETTINGS, OnSegmentSettings)
	ON_COMMAND(ID_FILE_NEW, OnFileNew)
	ON_COMMAND(ID_SKETCH_NEW, OnSketchNew)
	ON_COMMAND(ID_SKETCH_SHOWENDS, OnSketchShowends)
	ON_COMMAND(ID_SKETCH_SHOWINK, OnSketchShowink)
	ON_COMMAND(ID_SKETCH_SHOWPTS, OnSketchShowpts)
	ON_COMMAND(ID_SKETCH_SHOWSEGSS, OnSketchShowsegss)
	ON_COMMAND(ID_SKETCH_TOGGLE, OnSketchToggle)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// COpenGLView construction/destruction

COpenGLView::COpenGLView() :
	 m_hRC(0), m_pDC(0), m_ErrorString(_ErrorStrings[0])

{
	// TODO: add construction code here

		 plotsettings_dialog = new CPlotDialog(this);
}

COpenGLView::~COpenGLView()
{
	delete plotsettings_dialog;
}

BOOL COpenGLView::PreCreateWindow(CREATESTRUCT& cs) 
{
	// TODO: Add your specialized code here and/or call the base class

	// An OpenGL window must be created with the following flags and must not
    // include CS_PARENTDC for the class style. 
    cs.style |= WS_CLIPSIBLINGS | WS_CLIPCHILDREN;
  	

	return CView::PreCreateWindow(cs);
}


/////////////////////////////////////////////////////////////////////////////
// COpenGLView drawing



extern vect intersectpoint;

void COpenGLView::OnDraw(CDC* pDC)
{
	COpenGLViewClassDoc* pDoc = GetDocument();
	ASSERT_VALID(pDoc);


	glClear (GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);


	 CRect rect; 
	 GetClientRect(&rect); 
	
	m_WinWidth = rect.Width();
	m_WinHeight = rect.Height();
	CPoint cp;
	cp.x = 0;
	cp.y = 0;
	ClientToScreen( & cp);
	m_ClientLeft = cp.x;
	m_ClientBottom = cp.y;
	




	cp.x = m_WinWidth;
	cp.y = m_WinHeight;
	ClientToScreen( & cp);
	m_ClientRight = cp.x;
	m_ClientTop = cp.y;


	int tt, bb;
#ifdef _VISTA
	tt = 768 - m_ClientTop-20;     // changed for vista
	bb = 768 - m_ClientBottom-20;
#else
	tt = 768 - m_ClientTop-10;
	bb = 768 - m_ClientBottom+10;
#endif
	m_ClientBottom = tt;
	m_ClientTop = bb;



	glViewport(0, 0, m_WinWidth, m_WinHeight);
	glMatrixMode(GL_PROJECTION);

	glLoadIdentity();
	//glOrtho(0.0, m_WinWidth, 0.0, m_WinHeight,  1.0, -1.0);


// Commented by weesan@cs.ucr.edu
//    cout << "Left " << m_ClientLeft << "  Right " << m_ClientRight
//		<< " Bottom " << m_ClientBottom << " Top " << m_ClientTop << endl;
	glOrtho(m_ClientLeft, m_ClientRight, m_ClientBottom, m_ClientTop,  1.0, -1.0);
	glMatrixMode(GL_MODELVIEW);
	glLoadIdentity();




	// draw pen 
	if(show_pen) DrawPen(pstate);

	// if pen up, process ink
	if(!pstate.TipDown) {
		if(!stroke_data->Processed) {
			stroke_data->ProcessInk();
		}
	}

	// draw ink, seg points, and segments
	CurrentSketch->Draw();

	edit_stroke->Draw();
	
	
	::glFinish();

	if ( FALSE == ::SwapBuffers( m_pDC->GetSafeHdc() ) )
		{
		SetError(7);
		}


}


/////////////////////////////////////////////////////////////////////////////
// COpenGLView diagnostics

#ifdef _DEBUG
void COpenGLView::AssertValid() const
{
	CView::AssertValid();
}

void COpenGLView::Dump(CDumpContext& dc) const
{
	CView::Dump(dc);
}

COpenGLViewClassDoc* COpenGLView::GetDocument() // non-debug version is inline
{
	ASSERT(m_pDocument->IsKindOf(RUNTIME_CLASS(COpenGLViewClassDoc)));
	return (COpenGLViewClassDoc*)m_pDocument;
}
#endif //_DEBUG

/////////////////////////////////////////////////////////////////////////////
// COpenGLView message handlers

int COpenGLView::OnCreate(LPCREATESTRUCT lpCreateStruct) 
{
	if (CView::OnCreate(lpCreateStruct) == -1)
		return -1;
	
	// TODO: Add your specialized creation code here



	StartNewSketch();



	InitializeOpenGL();
	InitDraw();
	hTab = TabletInit();
	if (!hTab) {
		MessageBox(" Could Not Open Tablet Context.", "WinTab", 
		   MB_OK | MB_ICONHAND);
			//SendMessage(m_hWnd, WM_DESTROY, 0, 0L);
	}
	SetTimer(1,100,NULL);
	return 0;
}


/////////////////////////////////////////////////////////////////////////////
// GL helper functions

void COpenGLView::SetError( int e )
{
	// if there was no previous error,
	// then save this one
	if ( _ErrorStrings[0] == m_ErrorString ) 
		{
		m_ErrorString = _ErrorStrings[e];
		}
}


BOOL COpenGLView::InitializeOpenGL()
{
	// Can we put this in the constructor?
    m_pDC = new CClientDC(this);

    if ( NULL == m_pDC ) // failure to get DC
		{
		SetError(1);
		return FALSE;
		}

	if (!SetupPixelFormat())
		{
        return FALSE;
		}

    //n = ::GetPixelFormat(m_pDC->GetSafeHdc());
    //::DescribePixelFormat(m_pDC->GetSafeHdc(), n, sizeof(pfd), &pfd);

  //  CreateRGBPalette();

    if ( 0 == (m_hRC = ::wglCreateContext( m_pDC->GetSafeHdc() ) ) )
		{
		SetError(4);
		return FALSE;
		}

    if ( FALSE == ::wglMakeCurrent( m_pDC->GetSafeHdc(), m_hRC ) )
		{
		SetError(5);
		return FALSE;
		}	

#ifdef NO_TABLET
    ::glClearColor(1.0f, 1.0f, 1.0f, 1.0f);
#else
	// specify black as clear color
    ::glClearColor(0.0f, 0.0f, 0.0f, 0.0f);
#endif
	
	// specify the back of the buffer as clear depth
    ::glClearDepth(1.0f);
	// enable depth testing
    ::glEnable(GL_DEPTH_TEST);
	
	makeRasterFont();


	return TRUE;
}


BOOL COpenGLView::SetupPixelFormat()
{
  static PIXELFORMATDESCRIPTOR pfd = 
	{
        sizeof(PIXELFORMATDESCRIPTOR),  // size of this pfd
        1,                              // version number
        PFD_DRAW_TO_WINDOW |            // support window
          PFD_SUPPORT_OPENGL |          // support OpenGL
          PFD_DOUBLEBUFFER,             // double buffered
        PFD_TYPE_RGBA,                  // RGBA type
        24,                             // 24-bit color depth
        0, 0, 0, 0, 0, 0,               // color bits ignored
        0,                              // no alpha buffer
        0,                              // shift bit ignored
        0,                              // no accumulation buffer
        0, 0, 0, 0,                     // accum bits ignored
//        32,                             // 32-bit z-buffer
		16, // NOTE: 16-bit is faster than a 32-bit z-buffer
        0,                              // no stencil buffer
        0,                              // no auxiliary buffer
        PFD_MAIN_PLANE,                 // main layer
        0,                              // reserved
        0, 0, 0                         // layer masks ignored
    };
    int pixelformat;

    if ( 0 == (pixelformat = ::ChoosePixelFormat(m_pDC->GetSafeHdc(), &pfd)) )
	    {
		SetError(2);
        return FALSE;
		}

    if ( FALSE == ::SetPixelFormat(m_pDC->GetSafeHdc(), pixelformat, &pfd) )
	    {
       	SetError(3);
        return FALSE;
		}

    return TRUE;
}

void COpenGLView::OnDestroy() 
{
	CView::OnDestroy();  // bug shouldn't this be last
	
	// TODO: Add your message handler code here

    if ( FALSE == ::wglDeleteContext( m_hRC ) )
		{
		SetError(6);
 		}

    if ( m_pDC )
		{
        delete m_pDC;
		}
	if (hTab)
		WTClose(hTab);

}


BOOL COpenGLView::OnEraseBkgnd(CDC* pDC) 
{
	// TODO: Add your message handler code here and/or call default
	
	//	return CView::OnEraseBkgnd(pDC);
	return TRUE; // tell Windows not to erase the background
}

void COpenGLView::OnSize(UINT nType, int cx, int cy) 
{
	CView::OnSize(nType, cx, cy);
	
	CPoint cp;
	cp.x = 0;
	cp.y = 0;
	ClientToScreen( & cp);
	m_ClientLeft = cp.x;
	m_ClientBottom = cp.y;

	cp.x = cx;
	cp.y = cy;
	ClientToScreen( & cp);
	m_ClientRight = cp.x;
	m_ClientTop = cp.y;

		
	// TODO: Add your message handler code here
	GLdouble aspect_ratio; // width/height ratio
	
	if ( 0 >= cx || 0 >= cy )
		{
		return;
		}

	m_WinWidth  = cx;
	m_WinHeight = cy;

	//SetupViewport( cx, cy );
    ::glViewport(0, cy/2+1, cx, cy/2);


	// select the projection matrix and clear it
    ::glMatrixMode(GL_PROJECTION);
    ::glLoadIdentity();

	// compute the aspect ratio
	// this will keep all dimension scales equal
	aspect_ratio = 2.0*(GLdouble)cx/(GLdouble)cy;

	// select the viewing volumn
	SetupViewingFrustum( aspect_ratio );
	
	// switch back to the modelview matrix
    // ::glMatrixMode(GL_MODELVIEW);
    // ::glLoadIdentity();

	// now perform any viewing transformations
	// SetupViewingTransform();
}

 /////////////////////////////////////////////////////////////////////////////
// COpenGLView helper functions

BOOL COpenGLView::SetupViewport( int cx, int cy )
{
	// select the full client area
    ::glViewport(0, 0, cx, cy);

	return TRUE;
}

BOOL COpenGLView::SetupViewingFrustum( GLdouble aspect_ratio )
{
	// select a default viewing volumn
    ::gluPerspective(40.0f, aspect_ratio, .5f, 60.0f);
	// ::gluPerspective(40.0f, aspect_ratio, .1f, 20.0f);
	return TRUE;
}


BOOL COpenGLView::SetupViewingTransform()
{
	// select a default viewing transformation
	// of a 20 degree rotation about the X axis
	// then a -5 unit transformation along Z
	::glTranslatef( 0.0f, 0.0f, -5.0f );
	::glRotatef( 20.0f, 1.0f, 0.0f, 0.0f );
    return TRUE;
}


BOOL COpenGLView::RenderScene()
{
	// draw a red wire sphere inside a
	// light blue cube

	// rotate the wire sphere so it's vertically
	// oriented
	::glRotatef( 90.0f, 1.0f, 0.0f, 0.0f );
	::glColor3f( 1.0f, 0.0f, 0.0f );
	::auxWireSphere( .5 );
	::glColor3f( 0.5f, 0.5f, 1.0f );
	::auxWireCube( 1.0 );
    return TRUE;
}	

// Draw a square surface that looks like a
// black and white checkerboard
void COpenGLView::RenderStockScene()
{
	// define all vertices   X     Y     Z
	GLfloat v0[3], v1[3], v2[3], v3[3], delta;
	int color = 0;

	delta = 0.5f;

	// define the two colors
	GLfloat color1[3] = { 0.9f, 0.9f, 0.9f };
 	GLfloat color2[3] = { 0.05f, 0.05f, 0.05f };

	v0[1] = v1[1] = v2[1] = v3[1] = 0.0f;

	::glBegin( GL_QUADS );

	for ( int x = -5 ; x <= 5 ; x++ )
		{
		for ( int z = -5 ; z <= 5 ; z++ )
			{
			::glColor3fv( (color++)%2 ? color1 : color2 );
		
			v0[0] = 0.0f+delta*z;
			v0[2] = 0.0f+delta*x;

			v1[0] = v0[0]+delta;
			v1[2] = v0[2];

			v2[0] = v0[0]+delta;
			v2[2] = v0[2]+delta;

			v3[0] = v0[0];
			v3[2] = v0[2]+delta;

			::glVertex3fv( v0 );
			::glVertex3fv( v1 );
			::glVertex3fv( v2 );
			::glVertex3fv( v3 );
			}
		}
	::glEnd();	
	
}



void COpenGLView::OnLButtonDown(UINT nFlags, CPoint point) 
{
	// TODO: Add your message handler code here and/or call default

	pan = 1;
	if(nFlags & MK_CONTROL) pan = 0;

	leftdown = 1;
	startx = point.x;
	starty = point.y;

	InvalidateRect(0,FALSE);
	GetParent()->PostMessage(WM_PAINT);

	CView::OnLButtonDown(nFlags, point);
}

void COpenGLView::OnLButtonUp(UINT nFlags, CPoint point) 
{
	// TODO: Add your message handler code here and/or call default

	leftdown = 0;

	InvalidateRect(0,FALSE);
	GetParent()->PostMessage(WM_PAINT);
	
	CView::OnLButtonUp(nFlags, point);
}

void COpenGLView::OnMouseMove(UINT nFlags, CPoint point) 
{
	// TODO: Add your message handler code here and/or call default
	/*
	if(leftdown) {
		pan = 1;
		if(nFlags & MK_CONTROL) pan = 0;
	} else if (rightdown) {
		rotmode = 1;
		if(nFlags & MK_CONTROL) rotmode = 0;
	}


	if (leftdown && pan) {
		trans_x =  + ((float) (point.x - startx))/PANINC;
		trans_y =  + ((float) (starty - point.y))/PANINC;
	} else if (leftdown && !pan) {
		zoom  -= (point.x - startx)/ZOOMINC;
	} else  if (rightdown && rotmode) {
		spinx =  + (point.x - startx)/ANGINC;
		spiny =  + (point.y - starty)/ANGINC;
	} else  if (rightdown && !rotmode) {
		spinz =  + (point.x - startx)/ANGINC;
	}

	startx = point.x;
	starty = point.y;	
	
	if(leftdown || rightdown) {
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
	}
*/
	CView::OnMouseMove(nFlags, point);
}

BOOL COpenGLView::OnMouseWheel(UINT nFlags, short zDelta, CPoint pt) 
{
	// TODO: Add your message handler code here and/or call default
	
	if( !(nFlags & MK_CONTROL)) {
		zoom  -= ( (float) zDelta)/200.0;
	} else {
		spinz += ( (float) zDelta)/50.0; 
	}

	InvalidateRect(0,FALSE);
	GetParent()->PostMessage(WM_PAINT);


	return CView::OnMouseWheel(nFlags, zDelta, pt);
}

void COpenGLView::OnRButtonUp(UINT nFlags, CPoint point) 
{
	// TODO: Add your message handler code here and/or call default
	
	rightdown = 0;

	InvalidateRect(0,FALSE);
	GetParent()->PostMessage(WM_PAINT);


	CView::OnRButtonUp(nFlags, point);
}

void COpenGLView::OnRButtonDown(UINT nFlags, CPoint point) 
{
	// TODO: Add your message handler code here and/or call default
	
	rotmode = 1;
	if(nFlags & MK_CONTROL) rotmode = 0;
	rightdown = 1;

	InvalidateRect(0,FALSE);
	GetParent()->PostMessage(WM_PAINT);

	CView::OnRButtonDown(nFlags, point);
}




void COpenGLView::OnActivateView(BOOL bActivate, CView* pActivateView, CView* pDeactiveView) 
{
	// TODO: Add your specialized code here and/or call the base class
	
	CView::OnActivateView(bActivate, pActivateView, pDeactiveView);
}




HCTX COpenGLView::TabletInit(void)
{
	LOGCONTEXT      lcMine;           /* The context of the tablet */
	AXIS            TabletX, TabletY; /* The maximum tablet size */

	/* get default region */
	WTInfo(WTI_DEFCONTEXT, 0, &lcMine);

	/* modify the digitizing region */
	wsprintf(lcMine.lcName, "TiltTest Digitizing");
	lcMine.lcOptions |= CXO_MESSAGES;
	lcMine.lcPktData = PACKETDATA;
	lcMine.lcPktMode = PACKETMODE;
	lcMine.lcMoveMask = PACKETDATA;
	lcMine.lcBtnUpMask = lcMine.lcBtnDnMask;

    /* Set the entire tablet as active */
	WTInfo(WTI_DEVICES,DVC_X,&TabletX);
	WTInfo(WTI_DEVICES,DVC_Y,&TabletY);
	lcMine.lcInOrgX = 0;
	lcMine.lcInOrgY = 0;
	lcMine.lcInExtX = TabletX.axMax;
	lcMine.lcInExtY = TabletY.axMax;

	TabMaxX = TabletX.axMax;
	TabMaxY = TabletY.axMax;

	//cout << TabMaxX << '\t' << TabMaxY << endl;
	
	

    /* output the data in screen coords */
	//lcMine.lcOutOrgX = lcMine.lcOutOrgY = 0;
	//lcMine.lcOutExtX = GetSystemMetrics(SM_CXSCREEN);
    
	lcMine.lcOutOrgX = lcMine.lcOutOrgY = 0;
#ifdef HIGH_RES
	lcMine.lcOutExtX = 2048;
#else
	lcMine.lcOutExtX = 1024;
	//lcMine.lcOutExtX = TabletX.axMax;

#endif
	
	
	/* move origin to upper left */
	//lcMine.lcOutExtY = -GetSystemMetrics(SM_CYSCREEN);

#ifdef HIGH_RES
	lcMine.lcOutExtY = 2* (-768);
#else
	lcMine.lcOutExtY = 768;
	//lcMine.lcOutExtY = TabletY.axMax;

#endif

	// Added by weesan@cs.ucr.edu to override whatever is done above
    /* Set the entire tablet as active */
	WTInfo(WTI_DEVICES,DVC_X,&TabletX);
	WTInfo(WTI_DEVICES,DVC_Y,&TabletY);
	lcMine.lcInOrgX = 0;
	lcMine.lcInOrgY = 0;
	lcMine.lcInExtX = TabletX.axMax;
	lcMine.lcInExtY = TabletY.axMax;

    /* output the data in screen coords */
	lcMine.lcOutOrgX = lcMine.lcOutOrgY = 0;
	lcMine.lcOutExtX = GetSystemMetrics(SM_CXSCREEN);
    /* move origin to upper left */
	lcMine.lcOutExtY = GetSystemMetrics(SM_CYSCREEN);
	// End here

	/* open the region */
	return WTOpen(m_hWnd, &lcMine, TRUE);

/*
	//LOGCONTEXT      lcMine;           // The context of the tablet

	//TABLET: get current settings as a starting point for this context of the tablet.
	//WTInfo(WTI_DEFCONTEXT, 0, &lcMine);	// default settings may be different to current settings
	WTInfo(WTI_DEFSYSCTX, 0, &lcMine);	// current settings as set in control panel

	lcMine.lcOptions |= CXO_MESSAGES;	// keep existing options and make sure message handling is on for this context
	//TABLET: PACKETDATA must be defined FIRST before including pktdef.h. See the header file of this class for more details
	lcMine.lcPktData = PACKETDATA;	// these settings MUST be defined in the pktdef.h file, see notes
	lcMine.lcPktMode = PACKETMODE;
	lcMine.lcMoveMask = PACKETDATA;

	return WTOpen(m_hWnd, &lcMine, TRUE);
*/
}



LRESULT COpenGLView::OnWTPacket(WPARAM wSerial, LPARAM hCtx)
{
	// Read the packet



	PACKET pkt;
//	int erasesegs;
	if(WTPacket( (HCTX)hCtx, wSerial, &pkt)) {

		// pstate.PenX = (int)(pkt.pkX * 1024) / TabMaxX;
		pstate.PenX = (int)pkt.pkX;

#ifdef HIGH_RES
		pstate.PenY = 2*768 - (int)pkt.pkY;
#else
		//pstate.PenY = (int)(pkt.pkY * 768) / TabMaxY ;
		pstate.PenY = (int)(pkt.pkY);
#endif

#ifdef HIGH_RES
		pstate.PenPressure = 0.0;
		pstate.PenTilt = 60.0;
		pstate.PenDir = -45.0;
#else
		pstate.PenPressure = (double) pkt.pkNormalPressure;
		pstate.PenTilt = ((double)pkt.pkOrientation.orAltitude) / 10.0;
		pstate.PenDir = 90.0 - (double) pkt.pkOrientation.orAzimuth / 10.0; 
		if(pstate.PenDir < 0.0) pstate.PenDir += 360.0;

#endif	
		pstate.TipDown = pkt.pkButtons & 1;
		pstate.Button1 = (pkt.pkButtons & 2)/2;
		pstate.Button2 = (pkt.pkButtons & 4)/4;

		pstate.LastPenMove = GetCurrentTime();
		pstate.ShowPen = 1;

	/*
		int EditingTip;
		int EditingEraser;
		int Drawing;

		if(!StartNewStroke) {
			Drawing = 1;
		} else {
			Drawing = 0;
		}

		if(

*/

		// can only start editing if not in the middle of a pen stroke
		// if StartNewStroke == 1, not in the middle of a stroke
		if(StartNewStroke && (pstate.Button1 || pstate.Button2 || DoingEdit) && pstate.TipDown ) { // holding button down
			if(StartNewEdit) {
				StartNewEdit = 0;
				edit_stroke->EndData = 0;
				DoingEdit = 1;
			}
			edit_stroke->AddPoint(pstate.PenX,pstate.PenY,pstate.PenPressure,
				pstate.PenTilt,pstate.PenDir, pkt.pkTime);

		// if DoingEdit and pen is now up, edit is complete
		} else if (DoingEdit) {
			DoingEdit = 0;
			StartNewEdit = 1;
			ImplementEdit(edit_stroke, CurrentSketch);
			edit_stroke->EndData = 0;

		// if pen is upside down, then using eraser
		} else if(pstate.PenTilt < 0.0 && pstate.TipDown) {  // using eraser
			if(StartNewErase) {
				StartNewErase = 0;
				edit_stroke->EndData = 0;
				DoingErase = 1;
				StartNewStroke = 1; // make sure we will start a new stroke the next time the tip is downy
			}
			edit_stroke->AddPoint(pstate.PenX,pstate.PenY,pstate.PenPressure,
				pstate.PenTilt,pstate.PenDir, pkt.pkTime);
			/*
			erasesegs = edit_stroke->CountSpeedSegs();
			cout << erasesegs << endl;
			if(erasesegs > 20) {
				EraseSeg = 0;
				EraseStroke = 1;
			} else if(erasesegs > 7) {
				EraseSeg = 1;
				EraseStroke = 0;
			} else {
				EraseSeg = 0;
				EraseStroke = 0;
			}

			//edit_stroke->Processed = 0;

			//cout << edit_stroke->EndData  << endl;
			*/
		} else if (DoingErase) {
			DoingErase = 0;
			StartNewErase = 1;
			/*
			if(EraseSeg || EraseStroke) {
				edit_stroke->Processed = 0;
				ImplementErase(edit_stroke, CurrentSketch);
				cout << "last erase point is " << edit_stroke->EndData << endl;
			}
			*/
			ImplementErase(edit_stroke, CurrentSketch);
			edit_stroke->EndData = 0;
			
		} else if(pstate.PenTilt > 0.0) {  // using pen tip
			if(pstate.TipDown) stroke_data->Selected = -1;

			if(pstate.TipDown && StartNewStroke==1) 
			{
				StartNewStroke  = 0;
				//cout << "Starting new stroke\n";
				CurrentSketch->Strokes.InsertEnd(new Stroke(MAX_PTS));
				stroke_data = CurrentSketch->Strokes[CurrentSketch->Strokes.GetSize()-1];
				if(!stroke_data) {
					cout << "Out of memory\n";
					return false;
				}
				stroke_data->EndData=0;
				stroke_data->seglist.Clear();
			}
			if(!StartNewStroke) {
				stroke_data->AddPoint(pstate.PenX,pstate.PenY,pstate.PenPressure,
					pstate.PenTilt,pstate.PenDir, pkt.pkTime);
			}

			if(!pstate.TipDown) {
				StartNewStroke = 1;
			}





			// code for picking a point on the ink
			/*
			vect dp;
			int nearest_pt;
			double dist, min_dist = 10000.0;
			for(int ii=0;ii<stroke_data->EndData;ii++)  {
				dp.x = pstate.PenX; 
				dp.y = pstate.PenY;
				dp = stroke_data->Ink[ii].P - dp;
				dist = sqrt(dp*dp);
				if(dist < min_dist) {
					min_dist = dist;
					nearest_pt = ii;
				}
			}
			if(min_dist < 8.0) {
				stroke_data->Selected = nearest_pt;
				CString tmps;
				//tmps.Format("selected = %d", stroke_data->EndData - nearest_pt);
				//DisplayStatusText(tmps);
			}
			*/
		}
	}
	

	// Process packets in order, one at a time
	//CSingleLock lock( pWTMutex, TRUE );

	InvalidateRect(0,FALSE);
	GetParent()->PostMessage(WM_PAINT);
	
	return TRUE;
}


void COpenGLView::DisplayStatusText(CString txt)
{
	StatusText.Format("%s",txt);
	// cout << "txt" << endl;
}

void COpenGLView::OnWriteData() 
{
	int res = stroke_data->WriteData("data.txt");
	if(res = 0) {
		DisplayStatusText("Write Failed");
	} else {
		DisplayStatusText("Write Succeeded");
	}
	// TODO: Add your command handler code here
	
}

void COpenGLView::OnPlotData() 
{
    //glMatrixMode(GL_PROJECTION);
	//glLoadIdentity();
	//glViewport(0, 0, m_WinWidth, m_WinHeight);
	InvalidateRect(0,FALSE);
	GetParent()->PostMessage(WM_PAINT);


}

void COpenGLView::OnPlotsettings() 
{
	//CPlotDialog dialog;
	//dialog.DoModal();
	//InvalidateRect(0,FALSE);
	//GetParent()->PostMessage(WM_PAINT);
	plotsettings_dialog->Create();
	
}

	

LRESULT COpenGLView::OnDoneSettings(WPARAM wParam, LPARAM lParam)
{
	
	plotsettings_dialog->DestroyWindow();
	return 0L;
}


void COpenGLView::CauseRedraw(void)
{
	InvalidateRect(0,FALSE);
	//GetParent()->PostMessage(WM_PAINT);
	GetParent()->SendMessage(WM_PAINT);
}

void COpenGLView::OnKeyUp(UINT nChar, UINT nRepCnt, UINT nFlags) 
{
	// TODO: Add your message handler code here and/or call default
	int ii;
	
	switch (nChar) {

		// this is code for examing the ink. These keys select a point on the 
		// ink, causing the tangent to be displayed, and the corresponding point
		// in the data plots to be selected.

		/*
	case 'H':
		stroke_data->Selected=-1;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;
	case 'F':
		stroke_data->Selected++;
		if (stroke_data->Selected > stroke_data->EndData-1) stroke_data->Selected = 0;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;
	case 'B':
		stroke_data->Selected--;
		if (stroke_data->Selected < 0) stroke_data->Selected = stroke_data->EndData-1;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;
	case '0':
		stroke_data->Selected = 0;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;
	case '1':
		stroke_data->Selected = stroke_data->EndData / 4;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;
	case '2':
		stroke_data->Selected = stroke_data->EndData / 2;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;
	case '3':
		stroke_data->Selected = (int) (0.75  * (double) stroke_data->EndData);
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;
	case '4':
		stroke_data->Selected = stroke_data->EndData-1;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;
*/
	
	
	// options for controlling what is displayed
	case 'Z':
		show_ink = 1 - show_ink;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;
	case 'S':
		show_seg_point = 1 - show_seg_point;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;
	case 'X':
		show_seg_ends = 1 - show_seg_ends;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;
	case 'A':
		show_seg = 1 - show_seg;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;

	case 'T':
		show_ink = 1 - show_ink;
		show_seg = 1 - show_ink;
		show_seg_ends = 0;
		show_seg_point = 0;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;

	
	case 'D':
		show_data = 1 - show_data;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		cout << "This is the clock factor\n";
		cout << CLOCKS_PER_SEC  << endl;
		break;
	case 'K':
		zoom -= .5;
		cout << "zoom " << zoom << endl;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;

	case 'L':
		zoom += .5;
		cout << "zoom " << zoom << endl;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;
	
		

	// options for creating new sketches and moving between them
	case 'N':
		StartNewSketch();
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;
	case 219:  // '['
		CurrentSketch = Sketches[0];
		stroke_data = CurrentSketch->Strokes[CurrentSketch->Strokes.GetSize()-1];
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;
	case 221: // ']'
		CurrentSketch = Sketches[Sketches.GetSize()-1];
		stroke_data = CurrentSketch->Strokes[CurrentSketch->Strokes.GetSize()-1];
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;
	case 'O':
		for(ii=0;ii<Sketches.GetSize();ii++) {
			if(Sketches[ii] == CurrentSketch) break;
		}
		ii--;
		if(ii < 0) ii = Sketches.GetSize()-1;
		CurrentSketch = Sketches[ii];
		stroke_data = CurrentSketch->Strokes[CurrentSketch->Strokes.GetSize()-1];
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;
	case 'P':
		for(ii=0;ii<Sketches.GetSize();ii++) {
			if(Sketches[ii] == CurrentSketch) break;
		}
		ii++;
		if(ii >= Sketches.GetSize()) ii = 0;
		CurrentSketch = Sketches[ii];
		stroke_data = CurrentSketch->Strokes[CurrentSketch->Strokes.GetSize()-1];
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;

	case 46:  // DEL key
		for(ii=0;ii<Sketches.GetSize();ii++) {
			if(Sketches[ii] == CurrentSketch) break;
		}
		delete Sketches[ii];
		Sketches.RemoveAt(ii);
		if(Sketches.GetSize() == 0) {  // we deleted the only sketch
			StartNewSketch();
		} else {  // advance to the next existing sketch
			if(ii >= Sketches.GetSize()) ii = 0;
			CurrentSketch = Sketches[ii];
			stroke_data = CurrentSketch->Strokes[CurrentSketch->Strokes.GetSize()-1];
		}
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
		break;


	case 'Y':
		{
		BeginWaitCursor(); 

		 // Get client geometry 
		 CRect rect; 
		 GetClientRect(&rect); 
		 CSize size(rect.Width(),rect.Height()); 
		 //TRACE("  client zone : (%d;%d)\n",size.cx,size.cy); 
		 // Lines have to be 32 bytes aligned, suppose 24 bits per pixel 
		 // I just cropped it 
		 size.cx -= size.cx % 4; 
		 //TRACE("  final client zone : (%d;%d)\n",size.cx,size.cy); 

		 // Create a bitmap and select it in the device context 
		 // Note that this will never be used ;-) but no matter 
		 CBitmap bitmap; 
		 CDC *pDC = GetDC(); 
		 CDC MemDC; 
		 ASSERT(MemDC.CreateCompatibleDC(NULL)); 
		 ASSERT(bitmap.CreateCompatibleBitmap(pDC,size.cx,size.cy)); 
		 MemDC.SelectObject(&bitmap); 

		 // Alloc pixel bytes 
		 int NbBytes = 3 * size.cx * size.cy; 
		 unsigned char *pPixelData = new unsigned char[NbBytes]; 

		 // Copy from OpenGL 
		 ::glReadPixels(0,0,size.cx,size.cy,GL_RGB,GL_UNSIGNED_BYTE,pPixelData); 

		 // Fill header 
		 BITMAPINFOHEADER header; 
		 header.biWidth = size.cx; 
		 header.biHeight = size.cy; 
		 header.biSizeImage = NbBytes; 
		 header.biSize = 40; 
		 header.biPlanes = 1; 
		 header.biBitCount =  3 * 8; // RGB 
		 header.biCompression = 0; 
		 header.biXPelsPerMeter = 0; 
		 header.biYPelsPerMeter = 0; 
		 header.biClrUsed = 0; 
		 header.biClrImportant = 0; 

		 // Generate handle 
		 HANDLE handle = (HANDLE)::GlobalAlloc (GHND,sizeof(BITMAPINFOHEADER) + NbBytes); 
		 if(handle != NULL) 
		 { 
		  // Lock handle 
		  char *pData = (char *) ::GlobalLock((HGLOBAL)handle); 
		  // Copy header and data 
		  memcpy(pData,&header,sizeof(BITMAPINFOHEADER)); 
		  memcpy(pData+sizeof(BITMAPINFOHEADER),pPixelData,NbBytes); 
		  // Unlock 
		  ::GlobalUnlock((HGLOBAL)handle); 

		  // Push DIB in clipboard 
		  OpenClipboard(); 
		  EmptyClipboard(); 
		  SetClipboardData(CF_DIB,handle); 
		  CloseClipboard(); 
		 } 

		 // Cleanup 
		 MemDC.DeleteDC(); 
		 bitmap.DeleteObject(); 
		 delete [] pPixelData; 

		 EndWaitCursor(); 
		} 

		break;

	case 'E':
		{
			const int size = (int)6e6; 
			GLfloat *pFeedbackBuffer = new GLfloat[size]; 
			
			CDC *pDC = GetDC(); 

			glFeedbackBuffer(size,GL_3D_COLOR,pFeedbackBuffer); 
			glRenderMode(GL_FEEDBACK); 

			OnDraw(pDC);
			int NbValues = glRenderMode(GL_RENDER); 

			// The export stuff here 
			// This object encapsulates the code from Mark Kilgard, 
			// and adapted by Frederic Delourme 
			CPsRenderer PsRenderer; 
			PsRenderer.Run("foo.eps",pFeedbackBuffer,NbValues,TRUE); 

			// Cleanup 
			delete [] pFeedbackBuffer; 
			ReleaseDC(pDC);			
			}
			break;


	default:
		cout << "invalid key " << nChar << endl;
	} 

	



	CView::OnKeyUp(nChar, nRepCnt, nFlags);
}

void COpenGLView::OnTimer(UINT nIDEvent) 
{
	// TODO: Add your message handler code here and/or call default

	
	CTime ct = GetCurrentTime();
		
	CTimeSpan ts = ct - pstate.LastPenMove;
	if(ts.GetSeconds() > 1 && !pstate.TipDown && pstate.ShowPen) {
		pstate.ShowPen = 0;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
	}


	CView::OnTimer(nIDEvent);
}

int WriteSketch(ofstream &ofs)
{
	CurrentSketch->Write(ofs);
	return 1;
}

int ReadSketch(ifstream &ifs)
{
	// cout << "Starting new sketch\n";
	class RawSketch *skt;
	skt = new RawSketch;
	if(! skt) {
		cout << "Out of memory\n";
		return 0;
	}

	Sketches.InsertEnd(skt);
	CurrentSketch = skt;

	skt->Read(ifs);

	//CurrentSketch->Strokes.InsertEnd(new Stroke(MAX_PTS));
	stroke_data = CurrentSketch->Strokes[CurrentSketch->Strokes.GetSize()-1];
	//if(!stroke_data) {
	//	cout << "Out of memory\n";
	//	return 0;
	//}
	//stroke_data->EndData=0;
	//stroke_data->seglist.Clear();
	//StartNewStroke = 0;
	StartNewStroke = 1;
	return 1;

}
int StartNewSketch(void)
{
	// cout << "Starting new sketch\n";
	class RawSketch *skt;
	skt = new RawSketch;
	if(! skt) {
		cout << "Out of memory\n";
		return 0;
	}

	Sketches.InsertEnd(skt);
	CurrentSketch = skt;

	CurrentSketch->Strokes.InsertEnd(new Stroke(MAX_PTS));
	stroke_data = CurrentSketch->Strokes[CurrentSketch->Strokes.GetSize()-1];
	if(!stroke_data) {
		cout << "Out of memory\n";
		return 0;
	}
	stroke_data->EndData=0;
	stroke_data->seglist.Clear();
	StartNewStroke = 0;
	return 1;

}



void COpenGLView::OnFileOpen() 
{
	// TODO: Add your command handler code here
	static char BASED_CODE szFilter[] = "Text files (*.txt)|*.txt|All Files (*.*)|*.*||";
 
	CFileDialog m_ldFile(TRUE,NULL,NULL,NULL,szFilter);

	
	if (m_ldFile.DoModal()  == IDOK) {
		ifstream ifs(m_ldFile.GetPathName());
		if(!ifs) {
			MessageBox("Could not open" + m_ldFile.GetPathName());
		} else {
			if(!ReadSketch(ifs)) {
				MessageBox("Failure reading " + m_ldFile.GetPathName());
			} else {
				AfxGetApp()->AddToRecentFileList(m_ldFile.GetPathName());
			}
			ifs.close();
			AfxGetApp()->OpenDocumentFile(m_ldFile.GetPathName());
		}
	
	}
	InvalidateRect(0,FALSE);
	GetParent()->PostMessage(WM_PAINT);

}

void COpenGLView::OnFileSave() 
{

	CFileDialog m_ldFile(FALSE,"txt","trace",OFN_OVERWRITEPROMPT);

	if (m_ldFile.DoModal()  == IDOK) {
		ofstream ofs(m_ldFile.GetPathName());
		if(!ofs) {
			MessageBox("Could not open" + m_ldFile.GetPathName());
		} else {
			WriteSketch(ofs);
			ofs.close();
			AfxGetApp()->AddToRecentFileList(m_ldFile.GetPathName());
		}
	
	}
	
}

void COpenGLView::OnFileSaveAs() 
{
	// TODO: Add your command handler code here
	
	CFileDialog m_ldFile(FALSE,"txt","trace",OFN_OVERWRITEPROMPT);

	if (m_ldFile.DoModal()  == IDOK) {
		ofstream ofs(m_ldFile.GetPathName());
		if(!ofs) {
			MessageBox("Could not open" + m_ldFile.GetPathName());
		} else {
			WriteSketch(ofs);
			ofs.close();
			AfxGetApp()->AddToRecentFileList(m_ldFile.GetPathName());
		}
	
	}
	
}

void COpenGLView::OnSegmentSettings() 
{
	CProcessDialog dialog;
	dialog.m_clean_ends = ParamCleanEnds;
	dialog.m_merge_short_1 = ParamMergeShort1;
	dialog.m_merge_short_2 = ParamMergeShort2;
	dialog.m_merge_similar_1 = ParamMergeSimilar1;
	dialog.m_merge_similar_2 = ParamMergeSimilar2;
	dialog.m_split_1 = ParamSplit1;
	dialog.m_split_2 = ParamSplit2;
	dialog.m_speed_thresh = ParamSpeedThresh;
	
	if (dialog.DoModal() == IDOK) {
		ParamCleanEnds = dialog.m_clean_ends;
		ParamMergeShort1 = dialog.m_merge_short_1;
		ParamMergeShort2 = dialog.m_merge_short_2;
		ParamMergeSimilar1 = dialog.m_merge_similar_1;
		ParamMergeSimilar2 = dialog.m_merge_similar_2;
		ParamSplit1 = dialog.m_split_1;
		ParamSplit2 = dialog.m_split_2;
		ParamSpeedThresh =dialog.m_speed_thresh;

		int nums = Sketches.GetSize();
		for(int ii = 0; ii<nums; ii++) {
			int numt = Sketches[ii]->Strokes.GetSize();
			for(int jj = 0; jj<numt; jj++) {
				Sketches[ii]->Strokes[jj]->seglist.Clear();
				Sketches[ii]->Strokes[jj]->ProcessInk();
			}
		}
			

	}

	InvalidateRect(0,FALSE);
	GetParent()->PostMessage(WM_PAINT);
	
}

void COpenGLView::OnFileNew() 
{
	// TODO: Add your command handler code here
		StartNewSketch();
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
	
}

void COpenGLView::OnSketchNew() 
{
		StartNewSketch();
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
	// TODO: Add your command handler code here
	
}

void COpenGLView::OnSketchShowends() 
{

		show_seg_ends = 1 - show_seg_ends;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);

		

	// TODO: Add your command handler code here
	
}

void COpenGLView::OnSketchShowink() 
{
		show_ink = 1 - show_ink;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
	// TODO: Add your command handler code here
	
}

void COpenGLView::OnSketchShowpts() 
{
		show_seg_point = 1 - show_seg_point;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
	// TODO: Add your command handler code here
	
}

void COpenGLView::OnSketchShowsegss() 
{
		show_seg = 1 - show_seg;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
	// TODO: Add your command handler code here
	
}

void COpenGLView::OnSketchToggle() 
{
		show_ink = 1 - show_ink;
		show_seg = 1 - show_ink;
		show_seg_ends = 0;
		show_seg_point = 0;
		InvalidateRect(0,FALSE);
		GetParent()->PostMessage(WM_PAINT);
	// TODO: Add your command handler code here
	
}
