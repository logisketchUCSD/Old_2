# Microsoft Developer Studio Generated NMAKE File, Format Version 4.10
# ** DO NOT EDIT **

# TARGTYPE "Win32 (x86) Application" 0x0101

!IF "$(CFG)" == ""
CFG=OpenGL View Class - Win32 Debug
!MESSAGE No configuration specified.  Defaulting to OpenGL View Class - Win32\
 Debug.
!ENDIF 

!IF "$(CFG)" != "OpenGL View Class - Win32 Release" && "$(CFG)" !=\
 "OpenGL View Class - Win32 Debug"
!MESSAGE Invalid configuration "$(CFG)" specified.
!MESSAGE You can specify a configuration when running NMAKE on this makefile
!MESSAGE by defining the macro CFG on the command line.  For example:
!MESSAGE 
!MESSAGE NMAKE /f "OpenGL View Class.mak" CFG="OpenGL View Class - Win32 Debug"
!MESSAGE 
!MESSAGE Possible choices for configuration are:
!MESSAGE 
!MESSAGE "OpenGL View Class - Win32 Release" (based on\
 "Win32 (x86) Application")
!MESSAGE "OpenGL View Class - Win32 Debug" (based on "Win32 (x86) Application")
!MESSAGE 
!ERROR An invalid configuration is specified.
!ENDIF 

!IF "$(OS)" == "Windows_NT"
NULL=
!ELSE 
NULL=nul
!ENDIF 
################################################################################
# Begin Project
# PROP Target_Last_Scanned "OpenGL View Class - Win32 Debug"
CPP=cl.exe
RSC=rc.exe
MTL=mktyplib.exe

!IF  "$(CFG)" == "OpenGL View Class - Win32 Release"

# PROP BASE Use_MFC 6
# PROP BASE Use_Debug_Libraries 0
# PROP BASE Output_Dir "Release"
# PROP BASE Intermediate_Dir "Release"
# PROP BASE Target_Dir ""
# PROP Use_MFC 5
# PROP Use_Debug_Libraries 0
# PROP Output_Dir "Release"
# PROP Intermediate_Dir "Release"
# PROP Target_Dir ""
OUTDIR=.\Release
INTDIR=.\Release

ALL : "$(OUTDIR)\OpenGL View Class.exe"

CLEAN : 
	-@erase "$(INTDIR)\COpenGLView.obj"
	-@erase "$(INTDIR)\MainFrm.obj"
	-@erase "$(INTDIR)\OpenGL View Class.obj"
	-@erase "$(INTDIR)\OpenGL View Class.pch"
	-@erase "$(INTDIR)\OpenGL View Class.res"
	-@erase "$(INTDIR)\OpenGL View ClassDoc.obj"
	-@erase "$(INTDIR)\StdAfx.obj"
	-@erase "$(OUTDIR)\OpenGL View Class.exe"

"$(OUTDIR)" :
    if not exist "$(OUTDIR)/$(NULL)" mkdir "$(OUTDIR)"

# ADD BASE CPP /nologo /MD /W3 /GX /O2 /D "WIN32" /D "NDEBUG" /D "_WINDOWS" /D "_AFXDLL" /D "_MBCS" /Yu"stdafx.h" /c
# ADD CPP /nologo /MT /W3 /GX /O2 /D "WIN32" /D "NDEBUG" /D "_WINDOWS" /D "_MBCS" /Yu"stdafx.h" /c
CPP_PROJ=/nologo /MT /W3 /GX /O2 /D "WIN32" /D "NDEBUG" /D "_WINDOWS" /D\
 "_MBCS" /Fp"$(INTDIR)/OpenGL View Class.pch" /Yu"stdafx.h" /Fo"$(INTDIR)/" /c 
CPP_OBJS=.\Release/
CPP_SBRS=.\.
# ADD BASE MTL /nologo /D "NDEBUG" /win32
# ADD MTL /nologo /D "NDEBUG" /win32
MTL_PROJ=/nologo /D "NDEBUG" /win32 
# ADD BASE RSC /l 0x409 /d "NDEBUG" /d "_AFXDLL"
# ADD RSC /l 0x409 /d "NDEBUG"
RSC_PROJ=/l 0x409 /fo"$(INTDIR)/OpenGL View Class.res" /d "NDEBUG" 
BSC32=bscmake.exe
# ADD BASE BSC32 /nologo
# ADD BSC32 /nologo
BSC32_FLAGS=/nologo /o"$(OUTDIR)/OpenGL View Class.bsc" 
BSC32_SBRS= \
	
LINK32=link.exe
# ADD BASE LINK32 /nologo /subsystem:windows /machine:I386
# ADD LINK32 opengl32.lib glu32.lib glaux.lib /nologo /subsystem:windows /machine:I386
LINK32_FLAGS=opengl32.lib glu32.lib glaux.lib /nologo /subsystem:windows\
 /incremental:no /pdb:"$(OUTDIR)/OpenGL View Class.pdb" /machine:I386\
 /out:"$(OUTDIR)/OpenGL View Class.exe" 
LINK32_OBJS= \
	"$(INTDIR)\COpenGLView.obj" \
	"$(INTDIR)\MainFrm.obj" \
	"$(INTDIR)\OpenGL View Class.obj" \
	"$(INTDIR)\OpenGL View Class.res" \
	"$(INTDIR)\OpenGL View ClassDoc.obj" \
	"$(INTDIR)\StdAfx.obj"

"$(OUTDIR)\OpenGL View Class.exe" : "$(OUTDIR)" $(DEF_FILE) $(LINK32_OBJS)
    $(LINK32) @<<
  $(LINK32_FLAGS) $(LINK32_OBJS)
<<

!ELSEIF  "$(CFG)" == "OpenGL View Class - Win32 Debug"

# PROP BASE Use_MFC 6
# PROP BASE Use_Debug_Libraries 1
# PROP BASE Output_Dir "Debug"
# PROP BASE Intermediate_Dir "Debug"
# PROP BASE Target_Dir ""
# PROP Use_MFC 6
# PROP Use_Debug_Libraries 1
# PROP Output_Dir "Debug"
# PROP Intermediate_Dir "Debug"
# PROP Target_Dir ""
OUTDIR=.\Debug
INTDIR=.\Debug

ALL : "$(OUTDIR)\OpenGL View Class.exe"

CLEAN : 
	-@erase "$(INTDIR)\COpenGLView.obj"
	-@erase "$(INTDIR)\MainFrm.obj"
	-@erase "$(INTDIR)\OpenGL View Class.obj"
	-@erase "$(INTDIR)\OpenGL View Class.pch"
	-@erase "$(INTDIR)\OpenGL View Class.res"
	-@erase "$(INTDIR)\OpenGL View ClassDoc.obj"
	-@erase "$(INTDIR)\StdAfx.obj"
	-@erase "$(INTDIR)\vc40.idb"
	-@erase "$(INTDIR)\vc40.pdb"
	-@erase "$(OUTDIR)\OpenGL View Class.exe"
	-@erase "$(OUTDIR)\OpenGL View Class.ilk"
	-@erase "$(OUTDIR)\OpenGL View Class.pdb"

"$(OUTDIR)" :
    if not exist "$(OUTDIR)/$(NULL)" mkdir "$(OUTDIR)"

# ADD BASE CPP /nologo /MDd /W3 /Gm /GX /Zi /Od /D "WIN32" /D "_DEBUG" /D "_WINDOWS" /D "_AFXDLL" /D "_MBCS" /Yu"stdafx.h" /c
# ADD CPP /nologo /MDd /W3 /Gm /GX /Zi /Od /D "WIN32" /D "_DEBUG" /D "_WINDOWS" /D "_AFXDLL" /D "_MBCS" /Yu"stdafx.h" /c
CPP_PROJ=/nologo /MDd /W3 /Gm /GX /Zi /Od /D "WIN32" /D "_DEBUG" /D "_WINDOWS"\
 /D "_AFXDLL" /D "_MBCS" /Fp"$(INTDIR)/OpenGL View Class.pch" /Yu"stdafx.h"\
 /Fo"$(INTDIR)/" /Fd"$(INTDIR)/" /c 
CPP_OBJS=.\Debug/
CPP_SBRS=.\.
# ADD BASE MTL /nologo /D "_DEBUG" /win32
# ADD MTL /nologo /D "_DEBUG" /win32
MTL_PROJ=/nologo /D "_DEBUG" /win32 
# ADD BASE RSC /l 0x409 /d "_DEBUG" /d "_AFXDLL"
# ADD RSC /l 0x409 /d "_DEBUG" /d "_AFXDLL"
RSC_PROJ=/l 0x409 /fo"$(INTDIR)/OpenGL View Class.res" /d "_DEBUG" /d "_AFXDLL"\
 
BSC32=bscmake.exe
# ADD BASE BSC32 /nologo
# ADD BSC32 /nologo
BSC32_FLAGS=/nologo /o"$(OUTDIR)/OpenGL View Class.bsc" 
BSC32_SBRS= \
	
LINK32=link.exe
# ADD BASE LINK32 /nologo /subsystem:windows /debug /machine:I386
# ADD LINK32 opengl32.lib glu32.lib glaux.lib /nologo /subsystem:windows /debug /machine:I386
LINK32_FLAGS=opengl32.lib glu32.lib glaux.lib /nologo /subsystem:windows\
 /incremental:yes /pdb:"$(OUTDIR)/OpenGL View Class.pdb" /debug /machine:I386\
 /out:"$(OUTDIR)/OpenGL View Class.exe" 
LINK32_OBJS= \
	"$(INTDIR)\COpenGLView.obj" \
	"$(INTDIR)\MainFrm.obj" \
	"$(INTDIR)\OpenGL View Class.obj" \
	"$(INTDIR)\OpenGL View Class.res" \
	"$(INTDIR)\OpenGL View ClassDoc.obj" \
	"$(INTDIR)\StdAfx.obj"

"$(OUTDIR)\OpenGL View Class.exe" : "$(OUTDIR)" $(DEF_FILE) $(LINK32_OBJS)
    $(LINK32) @<<
  $(LINK32_FLAGS) $(LINK32_OBJS)
<<

!ENDIF 

.c{$(CPP_OBJS)}.obj:
   $(CPP) $(CPP_PROJ) $<  

.cpp{$(CPP_OBJS)}.obj:
   $(CPP) $(CPP_PROJ) $<  

.cxx{$(CPP_OBJS)}.obj:
   $(CPP) $(CPP_PROJ) $<  

.c{$(CPP_SBRS)}.sbr:
   $(CPP) $(CPP_PROJ) $<  

.cpp{$(CPP_SBRS)}.sbr:
   $(CPP) $(CPP_PROJ) $<  

.cxx{$(CPP_SBRS)}.sbr:
   $(CPP) $(CPP_PROJ) $<  

################################################################################
# Begin Target

# Name "OpenGL View Class - Win32 Release"
# Name "OpenGL View Class - Win32 Debug"

!IF  "$(CFG)" == "OpenGL View Class - Win32 Release"

!ELSEIF  "$(CFG)" == "OpenGL View Class - Win32 Debug"

!ENDIF 

################################################################################
# Begin Source File

SOURCE=.\ReadMe.txt

!IF  "$(CFG)" == "OpenGL View Class - Win32 Release"

!ELSEIF  "$(CFG)" == "OpenGL View Class - Win32 Debug"

!ENDIF 

# End Source File
################################################################################
# Begin Source File

SOURCE=".\OpenGL View Class.cpp"
DEP_CPP_OPENG=\
	".\COpenGLView.h"\
	".\MainFrm.h"\
	".\OpenGL View Class.h"\
	".\OpenGL View ClassDoc.h"\
	".\StdAfx.h"\
	

"$(INTDIR)\OpenGL View Class.obj" : $(SOURCE) $(DEP_CPP_OPENG) "$(INTDIR)"\
 "$(INTDIR)\OpenGL View Class.pch"


# End Source File
################################################################################
# Begin Source File

SOURCE=.\StdAfx.cpp
DEP_CPP_STDAF=\
	".\StdAfx.h"\
	

!IF  "$(CFG)" == "OpenGL View Class - Win32 Release"

# ADD CPP /Yc"stdafx.h"

BuildCmds= \
	$(CPP) /nologo /MT /W3 /GX /O2 /D "WIN32" /D "NDEBUG" /D "_WINDOWS" /D "_MBCS"\
 /Fp"$(INTDIR)/OpenGL View Class.pch" /Yc"stdafx.h" /Fo"$(INTDIR)/" /c $(SOURCE) \
	

"$(INTDIR)\StdAfx.obj" : $(SOURCE) $(DEP_CPP_STDAF) "$(INTDIR)"
   $(BuildCmds)

"$(INTDIR)\OpenGL View Class.pch" : $(SOURCE) $(DEP_CPP_STDAF) "$(INTDIR)"
   $(BuildCmds)

!ELSEIF  "$(CFG)" == "OpenGL View Class - Win32 Debug"

# ADD CPP /Yc"stdafx.h"

BuildCmds= \
	$(CPP) /nologo /MDd /W3 /Gm /GX /Zi /Od /D "WIN32" /D "_DEBUG" /D "_WINDOWS"\
 /D "_AFXDLL" /D "_MBCS" /Fp"$(INTDIR)/OpenGL View Class.pch" /Yc"stdafx.h"\
 /Fo"$(INTDIR)/" /Fd"$(INTDIR)/" /c $(SOURCE) \
	

"$(INTDIR)\StdAfx.obj" : $(SOURCE) $(DEP_CPP_STDAF) "$(INTDIR)"
   $(BuildCmds)

"$(INTDIR)\OpenGL View Class.pch" : $(SOURCE) $(DEP_CPP_STDAF) "$(INTDIR)"
   $(BuildCmds)

!ENDIF 

# End Source File
################################################################################
# Begin Source File

SOURCE=.\MainFrm.cpp
DEP_CPP_MAINF=\
	".\MainFrm.h"\
	".\OpenGL View Class.h"\
	".\StdAfx.h"\
	

"$(INTDIR)\MainFrm.obj" : $(SOURCE) $(DEP_CPP_MAINF) "$(INTDIR)"\
 "$(INTDIR)\OpenGL View Class.pch"


# End Source File
################################################################################
# Begin Source File

SOURCE=".\OpenGL View ClassDoc.cpp"
DEP_CPP_OPENGL=\
	".\OpenGL View Class.h"\
	".\OpenGL View ClassDoc.h"\
	".\StdAfx.h"\
	

"$(INTDIR)\OpenGL View ClassDoc.obj" : $(SOURCE) $(DEP_CPP_OPENGL) "$(INTDIR)"\
 "$(INTDIR)\OpenGL View Class.pch"


# End Source File
################################################################################
# Begin Source File

SOURCE=.\COpenGLView.cpp
DEP_CPP_COPEN=\
	".\COpenGLView.h"\
	".\OpenGL View Class.h"\
	".\OpenGL View ClassDoc.h"\
	".\StdAfx.h"\
	

"$(INTDIR)\COpenGLView.obj" : $(SOURCE) $(DEP_CPP_COPEN) "$(INTDIR)"\
 "$(INTDIR)\OpenGL View Class.pch"


# End Source File
################################################################################
# Begin Source File

SOURCE=".\OpenGL View Class.rc"
DEP_RSC_OPENGL_=\
	".\res\OpenGL View Class.ico"\
	".\res\OpenGL View Class.rc2"\
	".\res\OpenGL View ClassDoc.ico"\
	

"$(INTDIR)\OpenGL View Class.res" : $(SOURCE) $(DEP_RSC_OPENGL_) "$(INTDIR)"
   $(RSC) $(RSC_PROJ) $(SOURCE)


# End Source File
# End Target
# End Project
################################################################################
