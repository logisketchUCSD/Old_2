/***********************************************

	Copyright (C) 2002 - 2005
	UC Riverside Smart Tools Lab and Thomas Stahovich 
		

***********************************************/

// OpenGL View ClassDoc.cpp : implementation of the COpenGLViewClassDoc class
//

#include "stdafx.h"
#include "OpenGL View Class.h"

#include "OpenGL View ClassDoc.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// COpenGLViewClassDoc

IMPLEMENT_DYNCREATE(COpenGLViewClassDoc, CDocument)

BEGIN_MESSAGE_MAP(COpenGLViewClassDoc, CDocument)
	//{{AFX_MSG_MAP(COpenGLViewClassDoc)
		// NOTE - the ClassWizard will add and remove mapping macros here.
		//    DO NOT EDIT what you see in these blocks of generated code!
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// COpenGLViewClassDoc construction/destruction

COpenGLViewClassDoc::COpenGLViewClassDoc()
{
	// TODO: add one-time construction code here

}

COpenGLViewClassDoc::~COpenGLViewClassDoc()
{
}

BOOL COpenGLViewClassDoc::OnNewDocument()
{
	if (!CDocument::OnNewDocument())
		return FALSE;

	// TODO: add reinitialization code here
	// (SDI documents will reuse this document)

	return TRUE;
}

/////////////////////////////////////////////////////////////////////////////
// COpenGLViewClassDoc serialization

void COpenGLViewClassDoc::Serialize(CArchive& ar)
{
	if (ar.IsStoring())
	{
		// TODO: add storing code here
	}
	else
	{
		// TODO: add loading code here
	}
}

/////////////////////////////////////////////////////////////////////////////
// COpenGLViewClassDoc diagnostics

#ifdef _DEBUG
void COpenGLViewClassDoc::AssertValid() const
{
	CDocument::AssertValid();
}

void COpenGLViewClassDoc::Dump(CDumpContext& dc) const
{
	CDocument::Dump(dc);
}
#endif //_DEBUG

/////////////////////////////////////////////////////////////////////////////
// COpenGLViewClassDoc commands
