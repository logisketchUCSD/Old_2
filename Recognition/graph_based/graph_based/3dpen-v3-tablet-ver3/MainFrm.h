/***********************************************

	Copyright (C) 2002 - 2005
	UC Riverside Smart Tools Lab and Thomas Stahovich 
		

***********************************************/


// MainFrm.h : interface of the CMainFrame class
//
/////////////////////////////////////////////////////////////////////////////



class CMainFrame : public CFrameWnd
{
protected: // create from serialization only
	CMainFrame();
	DECLARE_DYNCREATE(CMainFrame)

// Attributes
public:

//	afx_msg void OnUpdatePenStatus(CCmdUI *pCmdUI);
	afx_msg void OnUpdateStatusText(CCmdUI *pCmdUI);
	afx_msg void OnUpdatePenDir(CCmdUI *pCmdUI);
	afx_msg void OnUpdatePenTilt(CCmdUI *pCmdUI);
	afx_msg void OnUpdatePenPressure(CCmdUI *pCmdUI);
	afx_msg void OnUpdatePenX(CCmdUI *pCmdUI);
	afx_msg void OnUpdatePenY(CCmdUI *pCmdUI);
	afx_msg void OnUpdatePenTip(CCmdUI *pCmdUI);
	afx_msg void OnUpdatePenButton1(CCmdUI *pCmdUI);
	afx_msg void OnUpdatePenButton2(CCmdUI *pCmdUI);


// Operations
public:

// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CMainFrame)
	public:
	virtual BOOL PreCreateWindow(CREATESTRUCT& cs);
	//}}AFX_VIRTUAL

// Implementation
public:
	virtual ~CMainFrame();
#ifdef _DEBUG
	virtual void AssertValid() const;
	virtual void Dump(CDumpContext& dc) const;
#endif

	protected:  // control bar embedded members
	CStatusBar  m_wndStatusBar;
	CToolBar    m_wndToolBar;


// Generated message map functions
protected:
	//{{AFX_MSG(CMainFrame)
	afx_msg int OnCreate(LPCREATESTRUCT lpCreateStruct);
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};

/////////////////////////////////////////////////////////////////////////////
