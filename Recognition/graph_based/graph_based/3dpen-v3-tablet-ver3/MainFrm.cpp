/***********************************************

	Copyright (C) 2002 - 2005
	UC Riverside Smart Tools Lab and Thomas Stahovich 
		

***********************************************/

// MainFrm.cpp : implementation of the CMainFrame class
//

#include "stdafx.h"
#include "OpenGL View Class.h"
#include "ink.h"

#include "MainFrm.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CMainFrame

IMPLEMENT_DYNCREATE(CMainFrame, CFrameWnd)

BEGIN_MESSAGE_MAP(CMainFrame, CFrameWnd)
//	ON_UPDATE_COMMAND_UI(ID_INDICATOR_PEN, OnUpdatePenStatus)
	ON_UPDATE_COMMAND_UI(ID_INDICATOR_TEXT, OnUpdateStatusText)
	ON_UPDATE_COMMAND_UI(ID_INDICATOR_PEN_TILT, OnUpdatePenTilt)
	ON_UPDATE_COMMAND_UI(ID_INDICATOR_PEN_DIR, OnUpdatePenDir)
	ON_UPDATE_COMMAND_UI(ID_INDICATOR_PEN_PRESSURE, OnUpdatePenPressure)
	ON_UPDATE_COMMAND_UI(ID_INDICATOR_PEN_X, OnUpdatePenX)
	ON_UPDATE_COMMAND_UI(ID_INDICATOR_PEN_Y, OnUpdatePenY)
	ON_UPDATE_COMMAND_UI(ID_INDICATOR_PEN_TIP, OnUpdatePenTip)
	ON_UPDATE_COMMAND_UI(ID_INDICATOR_PEN_BUTTON1, OnUpdatePenButton1)
	ON_UPDATE_COMMAND_UI(ID_INDICATOR_PEN_BUTTON2, OnUpdatePenButton2)
	ON_UPDATE_COMMAND_UI(ID_INDICATOR_TEXT, OnUpdateStatusText)


	//{{AFX_MSG_MAP(CMainFrame)
	ON_WM_CREATE()
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CMainFrame construction/destruction

CMainFrame::CMainFrame()
{
	// TODO: add member initialization code here
}

CMainFrame::~CMainFrame()
{
}

BOOL CMainFrame::PreCreateWindow(CREATESTRUCT& cs)
{
	// TODO: Modify the Window class or styles here by modifying
	//  the CREATESTRUCT cs

	/*
    cs.cy = ::GetSystemMetrics(SM_CYSCREEN) / 3; 
    cs.cx = ::GetSystemMetrics(SM_CXSCREEN) / 3; 
    cs.y = ((cs.cy * 3) - cs.cy) / 2; 
    cs.x = ((cs.cx * 3) - cs.cx) / 2;
	*/

    cs.cx = (2*1024)/3;
    cs.cy = (2*640)/3;
    cs.x = 100;
    cs.y = 100;


	return CFrameWnd::PreCreateWindow(cs);
}

/////////////////////////////////////////////////////////////////////////////
// CMainFrame diagnostics

#ifdef _DEBUG
void CMainFrame::AssertValid() const
{
	CFrameWnd::AssertValid();
}

void CMainFrame::Dump(CDumpContext& dc) const
{
	CFrameWnd::Dump(dc);
}

#endif //_DEBUG
 

static UINT indicators[] =
{
	//ID_SEPARATOR,           // status line indicator
	ID_INDICATOR_TEXT,
	ID_INDICATOR_PEN_TILT,
	ID_INDICATOR_PEN_DIR,
	ID_INDICATOR_PEN_PRESSURE,
	ID_INDICATOR_PEN_X,
	ID_INDICATOR_PEN_Y,
	ID_INDICATOR_PEN_TIP,
	ID_INDICATOR_PEN_BUTTON1,
	ID_INDICATOR_PEN_BUTTON2

	//ID_INDICATOR_CAPS,
	//ID_INDICATOR_NUM,
	//ID_INDICATOR_SCRL
	
};



/////////////////////////////////////////////////////////////////////////////
// CMainFrame message handlers



int CMainFrame::OnCreate(LPCREATESTRUCT lpCreateStruct) 
{
	if (CFrameWnd::OnCreate(lpCreateStruct) == -1)
		return -1;

	//if (!m_wndToolBar.Create(this) )
	//{
	//	TRACE0("Failed to create toolbar\n");
	//	return -1;      // fail to create
	//}

	if (!m_wndStatusBar.Create(this) ||
		!m_wndStatusBar.SetIndicators(indicators,
		  sizeof(indicators)/sizeof(UINT)))
	{
		TRACE0("Failed to create status bar\n");
		return -1;      // fail to create
	}

	


	/*
	// TODO: Remove this if you don't want tool tips or a resizeable toolbar
	m_wndToolBar.SetBarStyle(m_wndToolBar.GetBarStyle() |
		CBRS_TOOLTIPS | CBRS_FLYBY | CBRS_SIZE_DYNAMIC);

	// TODO: Delete these three lines if you don't want the toolbar to
	//  be dockable
	m_wndToolBar.EnableDocking(CBRS_ALIGN_ANY);
	EnableDocking(CBRS_ALIGN_ANY);
	DockControlBar(&m_wndToolBar);
*/
	
    CString ptext;

	SetWindowText("Hi Tom");
    
    m_wndStatusBar.SetPaneText(1, "hi tom", TRUE);


	// TODO: Add your specialized creation code here
	
	return 0;
}


extern class PenState pstate;
extern CString StatusText;


/*
void CMainFrame::OnUpdatePenStatus(CCmdUI *pCmdUI) 
{
    pCmdUI->Enable(); 
    CString strPage;
	
    strPage.Format( "%4.1lf", PenAngle ); 
    pCmdUI->SetText( strPage ); 
	
}
*/

void CMainFrame::OnUpdateStatusText(CCmdUI *pCmdUI) 
{
    pCmdUI->Enable(); 
    CString strPage;
	
    strPage.Format( "%s", StatusText ); 
    pCmdUI->SetText( strPage ); 

	
}

void CMainFrame::OnUpdatePenTilt(CCmdUI *pCmdUI) 
{
    pCmdUI->Enable(); 
    CString strPage;
	
    strPage.Format( "T: %4.1lf", pstate.PenTilt ); 
    pCmdUI->SetText( strPage ); 
	
}
void CMainFrame::OnUpdatePenDir(CCmdUI *pCmdUI) 
{
    pCmdUI->Enable(); 
    CString strPage;
	
    strPage.Format( "D: %5.1lf", pstate.PenDir ); 
    pCmdUI->SetText( strPage ); 
	
}
void CMainFrame::OnUpdatePenPressure(CCmdUI *pCmdUI) 
{
    pCmdUI->Enable(); 
    CString strPage;
	
    strPage.Format( "P: %6.1lf", pstate.PenPressure ); 
    pCmdUI->SetText( strPage ); 
	
}
void CMainFrame::OnUpdatePenX(CCmdUI *pCmdUI) 
{
    pCmdUI->Enable(); 
    CString strPage;
	
    strPage.Format( "X: %4d", pstate.PenX ); 
    pCmdUI->SetText( strPage ); 
	
}
void CMainFrame::OnUpdatePenY(CCmdUI *pCmdUI) 
{
    pCmdUI->Enable(); 
    CString strPage;
	
    strPage.Format( "Y: %4d", pstate.PenY ); 
    pCmdUI->SetText( strPage ); 
	
}

void CMainFrame::OnUpdatePenTip(CCmdUI *pCmdUI) 
{
    pCmdUI->Enable(); 
    CString strPage;
	
    strPage.Format( "C: %1d", pstate.TipDown ); 
    pCmdUI->SetText( strPage ); 
	
}
void CMainFrame::OnUpdatePenButton1(CCmdUI *pCmdUI) 
{
    pCmdUI->Enable(); 
    CString strPage;
	
    strPage.Format( "B1: %1d", pstate.Button1 ); 
    pCmdUI->SetText( strPage ); 
	
}
void CMainFrame::OnUpdatePenButton2(CCmdUI *pCmdUI) 
{
    pCmdUI->Enable(); 
    CString strPage;
	
    strPage.Format( "B2: %d", pstate.Button2 ); 
    pCmdUI->SetText( strPage ); 
	
}

