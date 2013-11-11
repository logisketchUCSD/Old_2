/***********************************************

	Copyright (C) 2002 - 2005
	UC Riverside Smart Tools Lab and Thomas Stahovich 
		

***********************************************/

// ProcessDialog.cpp : implementation file
//

#include "stdafx.h"
#include "OpenGL View Class.h"
#include "ProcessDialog.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CProcessDialog dialog


CProcessDialog::CProcessDialog(CWnd* pParent /*=NULL*/)
	: CDialog(CProcessDialog::IDD, pParent)
{
	//{{AFX_DATA_INIT(CProcessDialog)
	m_clean_ends = FALSE;
	m_merge_short_1 = FALSE;
	m_merge_short_2 = FALSE;
	m_merge_similar_1 = FALSE;
	m_merge_similar_2 = FALSE;
	m_speed_thresh = 0.0;
	m_split_1 = FALSE;
	m_split_2 = FALSE;
	//}}AFX_DATA_INIT
}


void CProcessDialog::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CProcessDialog)
	DDX_Check(pDX, IDC_CLEAN_ENDS, m_clean_ends);
	DDX_Check(pDX, IDC_MERGE_SHORT_1, m_merge_short_1);
	DDX_Check(pDX, IDC_MERGE_SHORT_2, m_merge_short_2);
	DDX_Check(pDX, IDC_MERGE_SIMIILAR_1, m_merge_similar_1);
	DDX_Check(pDX, IDC_MERGE_SIMIILAR_2, m_merge_similar_2);
	DDX_Text(pDX, IDC_SPEED_THRESH, m_speed_thresh);
	DDX_Check(pDX, IDC_SPLIT_1, m_split_1);
	DDX_Check(pDX, IDC_SPLIT_2, m_split_2);
	//}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CProcessDialog, CDialog)
	//{{AFX_MSG_MAP(CProcessDialog)
		// NOTE: the ClassWizard will add message map macros here
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CProcessDialog message handlers
