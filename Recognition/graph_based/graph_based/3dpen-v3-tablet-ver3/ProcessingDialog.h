/***********************************************

	Copyright (C) 2002 - 2005
	UC Riverside Smart Tools Lab and Thomas Stahovich 
		

***********************************************/


#if !defined(AFX_PROCESSINGDIALOG_H__FCA5916B_52BD_4564_A665_2A281E3AC260__INCLUDED_)
#define AFX_PROCESSINGDIALOG_H__FCA5916B_52BD_4564_A665_2A281E3AC260__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000
// ProcessingDialog.h : header file
//

/////////////////////////////////////////////////////////////////////////////
// CProcessingDialog dialog

class CProcessingDialog : public CDialog
{
// Construction
public:
	CProcessingDialog(CWnd* pParent = NULL);   // standard constructor
	CView *m_pView;


// Dialog Data
	//{{AFX_DATA(CProcessingDialog)
	enum { IDD = IDD_PROCESSING_DIALOG };
	CButton	m_Apply;
	//}}AFX_DATA


// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CProcessingDialog)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation
protected:

	// Generated message map functions
	//{{AFX_MSG(CProcessingDialog)
	afx_msg void OnApply();
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_PROCESSINGDIALOG_H__FCA5916B_52BD_4564_A665_2A281E3AC260__INCLUDED_)
