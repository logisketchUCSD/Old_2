/***********************************************

	Copyright (C) 2002 - 2005
	UC Riverside Smart Tools Lab and Thomas Stahovich 
		

***********************************************/


// ProcessingDialog.cpp : implementation file
//

#include "stdafx.h"
#include "OpenGL View Class.h"
#include "ProcessingDialog.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CProcessingDialog dialog


CProcessingDialog::CProcessingDialog(CWnd* pParent /*=NULL*/)
	: CDialog(CProcessingDialog::IDD, pParent)
{
	//{{AFX_DATA_INIT(CProcessingDialog)
		// NOTE: the ClassWizard will add member initialization here
	//}}AFX_DATA_INIT
}


void CProcessingDialog::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CProcessingDialog)
	DDX_Control(pDX, IDC_APPLY, m_Apply);
	//}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CProcessingDialog, CDialog)
	//{{AFX_MSG_MAP(CProcessingDialog)
	ON_BN_CLICKED(IDC_APPLY, OnApply)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CProcessingDialog message handlers

void CProcessingDialog::OnApply() 
{


	// TODO: Add your control notification handler code here
	
}
