/***********************************************

	Copyright (C) 2002 - 2005
	UC Riverside Smart Tools Lab and Thomas Stahovich 
		

***********************************************/


/// ViewOptions.cpp : implementation file
//

#include "stdafx.h"
#include "OpenGL View Class.h"
#include "ViewOptions.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CViewOptions dialog


CViewOptions::CViewOptions(CWnd* pParent /*=NULL*/)
	: CDialog(CViewOptions::IDD, pParent)
{
	//{{AFX_DATA_INIT(CViewOptions)
	m_repeat = FALSE;
	m_stop_on_collision = FALSE;
	m_two_sided = FALSE;
	m_lighting_on = FALSE;
	m_workpiece = FALSE;
	m_cutter = FALSE;
	//}}AFX_DATA_INIT
}


void CViewOptions::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CViewOptions)
	DDX_Check(pDX, IDC_REPEAT, m_repeat);
	DDX_Check(pDX, IDC_STOP_ON_COLLISION, m_stop_on_collision);
	DDX_Check(pDX, IDC_TWO_SIDED, m_two_sided);
	DDX_Check(pDX, IDC_LIGHTING_ON, m_lighting_on);
	DDX_Check(pDX, IDC_WORKPIECE, m_workpiece);
	DDX_Check(pDX, IDC_CUTTER, m_cutter);
	//}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CViewOptions, CDialog)
	//{{AFX_MSG_MAP(CViewOptions)
		// NOTE: the ClassWizard will add message map macros here
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CViewOptions message handlers
