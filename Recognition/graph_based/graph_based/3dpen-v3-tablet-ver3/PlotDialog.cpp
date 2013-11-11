/***********************************************

	Copyright (C) 2002 - 2005
	UC Riverside Smart Tools Lab and Thomas Stahovich 
		

***********************************************/


// PlotDialog.cpp : implementation file
//

#include "stdafx.h"
#include "OpenGL View Class.h"
#include "OpenGL View ClassDoc.h"
#include "COpenGLView.h"

#include "PlotDialog.h"
#include "ink.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif




/////////////////////////////////////////////////////////////////////////////
// CPlotDialog dialog


// for modeless use
CPlotDialog::CPlotDialog(COpenGLView *pview)
{
	my_view = pview;
}

CPlotDialog::CPlotDialog(CWnd* pParent /*=NULL*/)
	: CDialog(CPlotDialog::IDD, pParent)
{
	my_view = NULL;
	//{{AFX_DATA_INIT(CPlotDialog)
		// NOTE: the ClassWizard will add member initialization here
	//}}AFX_DATA_INIT
}


void CPlotDialog::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CPlotDialog)
		// NOTE: the ClassWizard will add DDX and DDV calls here
	//}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CPlotDialog, CDialog)
	ON_EN_CHANGE(ID_DYN_EDIT, OnApplyChange)
	ON_BN_CLICKED(ID_DYN_BUTTON0, OnApplyChange)

	//{{AFX_MSG_MAP(CPlotDialog)
	ON_WM_CREATE()
	ON_BN_CLICKED(IDC_APPLY_PROC_OPS, OnApplyProcOps)
	ON_WM_PAINT()
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CPlotDialog message handlers

int CPlotDialog::OnCreate(LPCREATESTRUCT lpCreateStruct) 
{
	if (CDialog::OnCreate(lpCreateStruct) == -1)
		return -1;
	
	// TODO: Add your specialized creation code here
	return 0;
}

BOOL CPlotDialog::OnInitDialog() 
{
	CDialog::OnInitDialog();
	

	
	// TODO: Add extra initialization here
	int ii;
	CRect rect2;
	CString tmpstr;
	for(ii=0;ii<DAT_MAX && ii<MAX_CONTROL;ii++) {
		
		//CRect rect1(20, 40+30*ii, 100, 60+30*ii);

		//mystatic[ii].Create(DataLabels[ii], WS_CHILD | WS_VISIBLE | WS_TABSTOP |
		//		 SS_LEFT | WS_BORDER, rect1, this, ID_DYN_STATIC);


		// Create Check Boxes
		rect2 = CRect(20, 60+30*ii, 140, 80+30*ii);
		
	//	Show[ii].Create(DataLabels[ii], WS_CHILD | WS_VISIBLE | WS_TABSTOP |
	//			 BS_CHECKBOX | BS_AUTOCHECKBOX     | WS_BORDER, rect2, this, ID_DYN_BUTTON0+ii);
		Show[ii].Create(DataLabels[ii], WS_CHILD | WS_VISIBLE | WS_TABSTOP |
				 BS_CHECKBOX | BS_AUTOCHECKBOX     | WS_BORDER, rect2, this, ID_DYN_BUTTON0);

		Show[ii].SetCheck(DataVisible[ii]);
		//Show[ii].SetCheck(1);


		// Data Scaling factors:  negative values cause the data to be normalized so that the max val is 1
		rect2 = CRect(150, 60+30*ii, 200, 80+30*ii);
		
		Scale[ii].Create(WS_CHILD | WS_VISIBLE | WS_TABSTOP | ES_RIGHT   
				     | WS_BORDER, rect2, this, ID_DYN_EDIT);
		tmpstr.Format("%6.4lf",	DataScale[ii]);
		Scale[ii].SetWindowText(tmpstr);



		// Color Selection
		rect2 = CRect(210, 60+30*ii, 260, 80+30*ii);

		Red[ii].Create(WS_CHILD | WS_VISIBLE | WS_TABSTOP | ES_RIGHT   
				     | WS_BORDER, rect2, this, ID_DYN_EDIT);
		tmpstr.Format("%6.4lf",	DataClr[ii][0]);
		Red[ii].SetWindowText(tmpstr);


		rect2 = CRect(270, 60+30*ii, 320, 80+30*ii);

		Green[ii].Create(WS_CHILD | WS_VISIBLE | WS_TABSTOP | ES_RIGHT   
				     | WS_BORDER, rect2, this, ID_DYN_EDIT);
		tmpstr.Format("%6.4lf",	DataClr[ii][1]);
		Green[ii].SetWindowText(tmpstr);

		rect2 = CRect(330, 60+30*ii, 380, 80+30*ii);

		Blue[ii].Create(WS_CHILD | WS_VISIBLE | WS_TABSTOP | ES_RIGHT   
				     | WS_BORDER, rect2, this, ID_DYN_EDIT);
		tmpstr.Format("%6.4lf",	DataClr[ii][2]);
		Blue[ii].SetWindowText(tmpstr);

		// Line Type Selection

		rect2 = CRect(390, 60+30*ii, 440, 80+30*ii);

		LineType[ii].Create(WS_CHILD | WS_VISIBLE | WS_TABSTOP | ES_RIGHT   
				     | WS_BORDER, rect2, this, ID_DYN_EDIT);
		tmpstr.Format("%d",	DataLineType[ii]);
		LineType[ii].SetWindowText(tmpstr);



	}

	Show[0].SetFocus();
	

	return TRUE;  // return TRUE unless you set the focus to a control
	              // EXCEPTION: OCX Property Pages should return FALSE
}




void CPlotDialog::OnOK() 
{
	OnApplyProcOps();	
	if(my_view != NULL) {
		UpdateData(TRUE);
		my_view->PostMessage(WM_DONESETTINGS, IDOK);
	} else {
		CDialog::OnOK();
	}
}

void CPlotDialog::OnCancel() 
{
	// TODO: Add extra cleanup here

	if(my_view != NULL) {
		my_view->PostMessage(WM_DONESETTINGS, IDCANCEL);
	} else {
		CDialog::OnCancel();	
	}
}



BOOL CPlotDialog::Create()
{	
	return(CDialog::Create(CPlotDialog::IDD));
}

void CPlotDialog::OnApplyChange() 
{
	this->Invalidate();

}


void CPlotDialog::OnApplyProcOps() 
{
	CString txt;
	double tmp;
	int ii, jj;


	if(my_view) {
		my_view->CauseRedraw();
	}

	for(ii=0;ii<DAT_MAX && ii<MAX_CONTROL;ii++) {
		
		DataVisible[ii] = Show[ii].GetState();

		Scale[ii].GetWindowText(txt);
		sscanf(txt, "%lf", &tmp);
		DataScale[ii] = tmp;

		Red[ii].GetWindowText(txt);
		sscanf(txt, "%lf", &tmp);
		DataClr[ii][0] = tmp;
	
		Green[ii].GetWindowText(txt);
		sscanf(txt, "%lf", &tmp);
		DataClr[ii][1] = tmp;

		Blue[ii].GetWindowText(txt);
		sscanf(txt, "%lf", &tmp);
		DataClr[ii][2] = tmp;

		LineType[ii].GetWindowText(txt);
		sscanf(txt, "%d", &jj);
		DataLineType[ii] = jj;

	}

}

void CPlotDialog::OnPaint() 
{
	CPaintDC dc(this); // device context for painting
	
	// TODO: Add your message handler code here
	
	int ii, R, G, B;

	CPen *mypen;;
	CPen *oldpen;
	oldpen = dc.GetCurrentPen();

	// get all of the current values
	OnApplyProcOps();


	for(ii=0;ii<DAT_MAX && ii<MAX_CONTROL;ii++) {
		if(DataVisible[ii]) {
			R = DataClr[ii][0] * 255;
			G = DataClr[ii][1] * 255;
			B = DataClr[ii][2] * 255;
			switch(DataLineType[ii])
			{
				case 1:
					mypen = new CPen(PS_DOT, 1, RGB(R, G, B));
					break;
				case 2:
					mypen = new CPen(PS_DASH, 1, RGB(R, G, B));
					break;
				case 3:
					mypen = new CPen(PS_DASHDOT, 1, RGB(R, G, B));
					break;
				case 4:
					mypen = new CPen(PS_DASHDOTDOT, 1, RGB(R, G, B));
					break;
				default:
					mypen = new CPen(PS_SOLID, 1, RGB(R, G, B));
					break;
			}
			dc.SelectObject(mypen);
			dc.MoveTo(450, 75+30*ii);
			dc.LineTo(550, 75+30*ii);
			dc.SelectObject(oldpen);
			delete mypen;
			//mypen->DelectObject();
		}

	}


	// Do not call CDialog::OnPaint() for painting messages
}


