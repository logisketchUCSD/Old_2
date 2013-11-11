/***********************************************

	Copyright (C) 2002 - 2005
	UC Riverside Smart Tools Lab and Thomas Stahovich 
		

***********************************************/


#if !defined(AFX_PROCESSDIALOG_H__A6EEE432_6C1D_459D_BD34_00B0872645B6__INCLUDED_)
#define AFX_PROCESSDIALOG_H__A6EEE432_6C1D_459D_BD34_00B0872645B6__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000
// ProcessDialog.h : header file
//

/////////////////////////////////////////////////////////////////////////////
// CProcessDialog dialog

class CProcessDialog : public CDialog
{
// Construction
public:
	CProcessDialog(CWnd* pParent = NULL);   // standard constructor

// Dialog Data
	//{{AFX_DATA(CProcessDialog)
	enum { IDD = IDD_PROCESS_DIALOG };
	BOOL	m_clean_ends;
	BOOL	m_merge_short_1;
	BOOL	m_merge_short_2;
	BOOL	m_merge_similar_1;
	BOOL	m_merge_similar_2;
	double	m_speed_thresh;
	BOOL	m_split_1;
	BOOL	m_split_2;
	//}}AFX_DATA


// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CProcessDialog)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation
protected:

	// Generated message map functions
	//{{AFX_MSG(CProcessDialog)
		// NOTE: the ClassWizard will add member functions here
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_PROCESSDIALOG_H__A6EEE432_6C1D_459D_BD34_00B0872645B6__INCLUDED_)
