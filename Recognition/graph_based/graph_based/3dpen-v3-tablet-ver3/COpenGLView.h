/***********************************************

	Copyright (C) 2002 - 2005
	UC Riverside Smart Tools Lab and Thomas Stahovich 
		

***********************************************/


// COpenGLView.h : interface of the COpenGLView class
//
/////////////////////////////////////////////////////////////////////////////


// Include the OpenGL headers
#include "gl\gl.h"
#include "gl\glu.h"
#include "gl\glaux.h"

#include "msgpack.h"
#include "wintab.h"


class COpenGLView : public CView
{


private:
	class CPlotDialog *plotsettings_dialog;

private:
	int m_WinWidth, m_WinHeight;
	int m_ClientLeft, m_ClientRight;
	int m_ClientTop, m_ClientBottom;
protected: // create from serialization only
	COpenGLView();
	DECLARE_DYNCREATE(COpenGLView)

// Attributes
public:
	COpenGLViewClassDoc* GetDocument();


public:
	void CauseRedraw(void);

// Operations
public:

// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(COpenGLView)
	public:
	virtual void OnDraw(CDC* pDC);  // overridden to draw this view
	virtual BOOL PreCreateWindow(CREATESTRUCT& cs);
	protected:
	virtual void OnActivateView(BOOL bActivate, CView* pActivateView, CView* pDeactiveView);
	//}}AFX_VIRTUAL

// Implementation
public:
	virtual ~COpenGLView();
#ifdef _DEBUG
	virtual void AssertValid() const;
	virtual void Dump(CDumpContext& dc) const;
#endif

protected:
	// Added by weesan@cs.ucr.edu
	HCTX tabletHandle(void) const {
		return (hTab);
	}

// Generated message map functions
protected:
	afx_msg LRESULT OnDoneSettings(WPARAM wParam, LPARAM lParam);
	//{{AFX_MSG(COpenGLView)
	afx_msg int OnCreate(LPCREATESTRUCT lpCreateStruct);
	afx_msg void OnDestroy();
	afx_msg BOOL OnEraseBkgnd(CDC* pDC);
	afx_msg void OnSize(UINT nType, int cx, int cy);
	afx_msg void OnLButtonDown(UINT nFlags, CPoint point);
	afx_msg void OnLButtonUp(UINT nFlags, CPoint point);
	afx_msg void OnMouseMove(UINT nFlags, CPoint point);
	afx_msg BOOL OnMouseWheel(UINT nFlags, short zDelta, CPoint pt);
	afx_msg void OnRButtonUp(UINT nFlags, CPoint point);
	afx_msg void OnRButtonDown(UINT nFlags, CPoint point);
	afx_msg void OnWriteData();
	afx_msg void OnPlotData();
	afx_msg void OnPlotsettings();
	afx_msg void OnKeyUp(UINT nChar, UINT nRepCnt, UINT nFlags);
	afx_msg void OnTimer(UINT nIDEvent);
	afx_msg void OnFileOpen();
	afx_msg void OnFileSave();
	afx_msg void OnFileSaveAs();
	afx_msg void OnSegmentSettings();
	afx_msg void OnFileNew();
	afx_msg void OnSketchNew();
	afx_msg void OnSketchShowends();
	afx_msg void OnSketchShowink();
	afx_msg void OnSketchShowpts();
	afx_msg void OnSketchShowsegss();
	afx_msg void OnSketchToggle();
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()

	// The following was added

	virtual BOOL SetupPixelFormat( void );
	virtual BOOL SetupViewport( int cx, int cy );
	virtual BOOL SetupViewingFrustum( GLdouble aspect_ratio );
	virtual BOOL SetupViewingTransform( void );
  	virtual BOOL PreRenderScene( void ) { return TRUE; }
 	virtual void RenderStockScene( void );
	virtual BOOL RenderScene( void );

public:
		afx_msg LRESULT OnWTPacket(WPARAM, LPARAM);
		void DisplayStatusText(CString txt);


private:
	BOOL InitializeOpenGL();
	HCTX TabletInit(void);

	void SetError( int e );

	HCTX            hTab;         /* Handle for Tablet Context */
	POINT           ptNew;               /* XY value storage */
	UINT            prsNew;              /* Pressure value storage */
	UINT            curNew;              /* Cursor number storage */
	ORIENTATION     ortNew;              /* Tilt value storage */
	BOOL            tilt_support; /* Is tilt supported */



	HGLRC	m_hRC;
	CDC*	m_pDC;
	
	static const char* const _ErrorStrings[];
	const char* m_ErrorString;

};

#ifndef _DEBUG  // debug version in COpenGLView.cpp
inline COpenGLViewClassDoc* COpenGLView::GetDocument()
   { return (COpenGLViewClassDoc*)m_pDocument; }
#endif


/////////////////////////////////////////////////////////////////////////////
