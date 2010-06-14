@ECHO OFF

"%~1\SimisFile.exe" /TEST "Microsoft Train Simulator" /J > simisfile_data.txt
FOR /F "tokens=6,7,8" %%a IN ('TYPE simisfile_data.txt ^| FINDSTR /B "(Total"') DO (
	> simisfile_total.properties ECHO YVALUE=%%a
	> simisfile_read.properties  ECHO YVALUE=%%b
	> simisfile_write.properties ECHO YVALUE=%%c
)
