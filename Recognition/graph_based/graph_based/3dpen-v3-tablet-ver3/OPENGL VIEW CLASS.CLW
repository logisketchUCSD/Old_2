; CLW file contains information for the MFC ClassWizard

[General Info]
Version=1
LastClass=COpenGLView
LastTemplate=CDialog
NewFileInclude1=#include "stdafx.h"
NewFileInclude2=#include "OpenGL View Class.h"
LastPage=0

ClassCount=9
Class1=COpenGLViewClassApp
Class2=COpenGLViewClassDoc
Class3=COpenGLView
Class4=CMainFrame

ResourceCount=7
Resource1=IDR_MAINFRAME
Resource2=IDD_PLOT_DIALOG
Class5=CAboutDlg
Class6=CViewOptions
Resource3=IDD_ABOUTBOX
Resource4=IDD_PROCESSING_DIALOG
Class7=CProcessingDialog
Resource5=IDD_DIALOG1
Class8=CPlotDialog
Resource6=IDD_PROCESS_DIALOG
Class9=CProcessDialog
Resource7=IDR_TOOLBAR1

[CLS:COpenGLViewClassApp]
Type=0
HeaderFile=OpenGL View Class.h
ImplementationFile=OpenGL View Class.cpp
Filter=N
LastObject=COpenGLViewClassApp

[CLS:COpenGLViewClassDoc]
Type=0
HeaderFile=OpenGL View ClassDoc.h
ImplementationFile=OpenGL View ClassDoc.cpp
Filter=N
LastObject=COpenGLViewClassDoc

[CLS:COpenGLView]
Type=0
HeaderFile=COpenGLView.h
ImplementationFile=COpenGLView.cpp
Filter=C
BaseClass=CView
VirtualFilter=VWC
LastObject=ID_SKETCH_SHOWINK

[CLS:CMainFrame]
Type=0
HeaderFile=MainFrm.h
ImplementationFile=MainFrm.cpp
Filter=T
BaseClass=CFrameWnd
VirtualFilter=fWC
LastObject=ID_SKETCH_SHOWSEGSS



[CLS:CAboutDlg]
Type=0
HeaderFile=OpenGL View Class.cpp
ImplementationFile=OpenGL View Class.cpp
Filter=D

[DLG:IDD_ABOUTBOX]
Type=1
Class=CAboutDlg
ControlCount=4
Control1=IDC_STATIC,static,1342177283
Control2=IDC_STATIC,static,1342308352
Control3=IDOK,button,1342373889
Control4=IDC_STATIC,static,1342308352

[MNU:IDR_MAINFRAME]
Type=1
Class=CMainFrame
Command1=ID_FILE_NEW
Command2=ID_FILE_OPEN
Command3=ID_FILE_SAVE
Command4=ID_FILE_SAVE_AS
Command5=ID_FILE_MRU_FILE1
Command6=ID_APP_EXIT
Command7=ID_EDIT_UNDO
Command8=ID_EDIT_CUT
Command9=ID_EDIT_COPY
Command10=ID_EDIT_PASTE
Command11=ID_APP_ABOUT
Command12=IDM_SEGMENT_SETTINGS
Command13=IDM_WriteData
Command14=IDM_PLOT_DATA
Command15=ID_PLOTSETTINGS
Command16=ID_SKETCH_NEW
Command17=ID_SKETCH_SHOWSEGSS
Command18=ID_SKETCH_SHOWPTS
Command19=ID_SKETCH_SHOWINK
Command20=ID_SKETCH_SHOWENDS
Command21=ID_SKETCH_TOGGLE
CommandCount=21

[ACL:IDR_MAINFRAME]
Type=1
Class=CMainFrame
Command1=ID_EDIT_COPY
Command2=ID_FILE_NEW
Command3=ID_FILE_OPEN
Command4=ID_FILE_SAVE
Command5=ID_EDIT_PASTE
Command6=ID_EDIT_UNDO
Command7=ID_EDIT_CUT
Command8=ID_NEXT_PANE
Command9=ID_PREV_PANE
Command10=ID_PLOTSETTINGS
Command11=ID_EDIT_COPY
Command12=ID_EDIT_PASTE
Command13=ID_EDIT_CUT
Command14=ID_EDIT_UNDO
CommandCount=14

[DLG:IDD_DIALOG1]
Type=1
Class=CViewOptions
ControlCount=11
Control1=IDOK,button,1342242817
Control2=IDCANCEL,button,1342242816
Control3=IDC_STATIC,button,1342177287
Control4=IDC_LIGHTING_ON,button,1342242819
Control5=IDC_TWO_SIDED,button,1342242819
Control6=IDC_STATIC,button,1342177287
Control7=IDC_REPEAT,button,1342242819
Control8=IDC_STOP_ON_COLLISION,button,1342242819
Control9=IDC_STATIC,button,1342177287
Control10=IDC_CUTTER,button,1342242819
Control11=IDC_WORKPIECE,button,1342242819

[CLS:CViewOptions]
Type=0
HeaderFile=ViewOptions.h
ImplementationFile=ViewOptions.cpp
BaseClass=CDialog
Filter=D
VirtualFilter=dWC
LastObject=CViewOptions

[TB:IDR_TOOLBAR1]
Type=1
Class=?
Command1=ID_BUTTON32775
Command2=ID_BUTTON32776
CommandCount=2

[DLG:IDD_PROCESSING_DIALOG]
Type=1
Class=CProcessingDialog
ControlCount=20
Control1=IDOK,button,1342242817
Control2=IDCANCEL,button,1342242816
Control3=IDC_APPLY,button,1342242816
Control4=IDC_SPEED_THRESH,button,1342177289
Control5=IDC_ALIGN,button,1342177289
Control6=IDC_CURVATURE_THRESHOLD,button,1342177289
Control7=IDC_CURVATURE_MAG,button,1342177289
Control8=IDC_ACCEL_MAG,button,1342177289
Control9=IDC_NORMAL_ACCEL,button,1342177289
Control10=IDC_TANGENTIAL_ACCEL,button,1342177289
Control11=IDC_STATIC,button,1342177287
Control12=IDC_EDIT_SPEED_THRESH,edit,1350631552
Control13=IDC_EDIT_CURV_THRESH,edit,1350631552
Control14=IDC_EDIT_CURV_SCALE,edit,1350631552
Control15=IDC_EDIT_ACCEL_SCALE,edit,1350631552
Control16=IDC_EDIT_NORM_ACCEL_SCALE,edit,1350631552
Control17=IDC_EDIT_TANG_ACCEL_SCALE,edit,1350631552
Control18=IDC_STATIC,static,1342308352
Control19=IDC_STATIC,static,1342308352
Control20=IDC_STATIC,button,1342177287

[CLS:CProcessingDialog]
Type=0
HeaderFile=ProcessingDialog.h
ImplementationFile=ProcessingDialog.cpp
BaseClass=CDialog
Filter=D
VirtualFilter=dWC
LastObject=IDC_EDIT_ACCEL_SCALE

[DLG:IDD_PLOT_DIALOG]
Type=1
Class=CPlotDialog
ControlCount=3
Control1=IDOK,button,1342242817
Control2=IDCANCEL,button,1342242816
Control3=IDC_APPLY_PROC_OPS,button,1342242816

[CLS:CPlotDialog]
Type=0
HeaderFile=PlotDialog.h
ImplementationFile=PlotDialog.cpp
BaseClass=CDialog
Filter=D
LastObject=CPlotDialog
VirtualFilter=dWC

[DLG:IDD_PROCESS_DIALOG]
Type=1
Class=CProcessDialog
ControlCount=19
Control1=IDOK,button,1342242817
Control2=IDCANCEL,button,1342242816
Control3=IDC_CLEAN_ENDS,button,1342242819
Control4=IDC_MERGE_SIMIILAR_1,button,1342242819
Control5=IDC_SPLIT_1,button,1342242819
Control6=IDC_SPLIT_2,button,1342242819
Control7=IDC_SPEED_THRESH,edit,1350631552
Control8=IDC_STATIC,static,1342308352
Control9=IDC_STATIC,static,1342308354
Control10=IDC_STATIC,static,1342308354
Control11=IDC_STATIC,static,1342308354
Control12=IDC_STATIC,static,1342308354
Control13=IDC_STATIC,static,1342308354
Control14=IDC_STATIC,static,1342308354
Control15=IDC_STATIC,static,1342308354
Control16=IDC_MERGE_SHORT_1,button,1342242819
Control17=IDC_MERGE_SHORT_2,button,1342242819
Control18=IDC_MERGE_SIMIILAR_2,button,1342242819
Control19=IDC_STATIC,button,1342177287

[CLS:CProcessDialog]
Type=0
HeaderFile=ProcessDialog.h
ImplementationFile=ProcessDialog.cpp
BaseClass=CDialog
Filter=D
VirtualFilter=dWC
LastObject=CProcessDialog

