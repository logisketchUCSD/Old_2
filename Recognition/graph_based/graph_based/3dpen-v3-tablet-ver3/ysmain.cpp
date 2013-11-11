/***********************************************

	Copyright (C) 2002 - 2005
	UC Riverside Smart Tools Lab and Thomas Stahovich 
		

***********************************************/

#include "stdafx.h"
#include <windows.h>
#include <string.h>

/* in link options, change subsystem from "windows" to "console" */

//#include "ink.h"
//#include "iostream.h"

int main(int ac,char *av[])
{
	int i;
	char buf[4096];
	buf[0]=0;
	for(i=1; i<ac; i++)
	{
		strcat(buf,"\"");
		strcat(buf,av[i]);
		strcat(buf,"\"");
	}

	HINSTANCE inst;
	inst=(HINSTANCE)GetModuleHandle(NULL);



	return WinMain(inst,NULL,buf,SW_SHOWNORMAL);
	//return WinMain(inst,NULL,buf,SW_SHOWMAXIMIZED);
}
