/***********************************************

	Copyright (C) 2002 - 2005
	UC Riverside Smart Tools Lab and Thomas Stahovich 
		

***********************************************/


#if !defined(AFX_PLOTDIALOG_H__A7E512B8_95E6_41BF_969E_92427EA31AE9__INCLUDED_)
#define AFX_PLOTDIALOG_H__A7E512B8_95E6_41BF_969E_92427EA31AE9__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000
// PlotDialog.h : header file
//




#define MAX_CONTROL 100
#define WM_DONESETTINGS WM_USER + 5

/////////////////////////////////////////////////////////////////////////////
// CPlotDialog dialog

class CPlotDialog : public CDialog
{
// Construction
public:
	//CEdit myedit[MAX_CONTROL];
	CButton Show[MAX_CONTROL];
	//CStatic mystatic[MAX_CONTROL];
	CEdit Red[MAX_CONTROL];
	CEdit Green[MAX_CONTROL];
	CEdit Blue[MAX_CONTROL];
	CEdit Scale[MAX_CONTROL];
	CEdit LineType[MAX_CONTROL];

// for modeless use
private:
	COpenGLView *my_view;


public:
	CPlotDialog::CPlotDialog(COpenGLView *pview);
	BOOL Create();


public:
	CPlotDialog(CWnd* pParent = NULL);   // standard constructor

// Dialog Data
	//{{AFX_DATA(CPlotDialog)
	enum { IDD = IDD_PLOT_DIALOG };
		// NOTE: the ClassWizard will add data members here
	//}}AFX_DATA


// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CPlotDialog)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation
protected:
	afx_msg void OnApplyChange();
	// Generated message map functions
	//{{AFX_MSG(CPlotDialog)
	afx_msg int OnCreate(LPCREATESTRUCT lpCreateStruct);
	virtual BOOL OnInitDialog();
	virtual void OnOK();
	afx_msg void OnApplyProcOps();
	afx_msg void OnPaint();
	virtual void OnCancel();
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_PLOTDIALOG_H__A7E512B8_95E6_41BF_969E_92427EA31AE9__INCLUDED_)
