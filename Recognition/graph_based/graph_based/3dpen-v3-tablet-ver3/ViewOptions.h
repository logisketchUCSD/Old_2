/***********************************************

	Copyright (C) 2002 - 2005
	UC Riverside Smart Tools Lab and Thomas Stahovich 
		

***********************************************/

#if !defined(AFX_VIEWOPTIONS_H__D797D362_6BE5_11D2_9B2A_006008592FCF__INCLUDED_)
#define AFX_VIEWOPTIONS_H__D797D362_6BE5_11D2_9B2A_006008592FCF__INCLUDED_

#if _MSC_VER >= 1000
#pragma once
#endif // _MSC_VER >= 1000
// ViewOptions.h : header file
//

/////////////////////////////////////////////////////////////////////////////
// CViewOptions dialog

class CViewOptions : public CDialog
{
// Construction
public:
	CViewOptions(CWnd* pParent = NULL);   // standard constructor

// Dialog Data
	//{{AFX_DATA(CViewOptions)
	enum { IDD = IDD_DIALOG1 };
	BOOL	m_repeat;
	BOOL	m_stop_on_collision;
	BOOL	m_two_sided;
	BOOL	m_lighting_on;
	BOOL	m_workpiece;
	BOOL	m_cutter;
	//}}AFX_DATA


// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CViewOptions)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation
protected:

	// Generated message map functions
	//{{AFX_MSG(CViewOptions)
		// NOTE: the ClassWizard will add member functions here
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Developer Studio will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_VIEWOPTIONS_H__D797D362_6BE5_11D2_9B2A_006008592FCF__INCLUDED_)
