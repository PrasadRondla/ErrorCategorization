@echo off

   rem ******************************************************************************
   rem This batch file executes the ErrorCategorizationTool.exe
   rem *******************************************************************************
  
set batchpath=%~dp0
set CUR_YYYY=%date:~10,4%
set CUR_MM=%date:~4,2%
set CUR_DD=%date:~7,2%
set SUBFILENAME=%CUR_YYYY%%CUR_MM%%CUR_DD%
IF [%~1]==[] (GOTO LASTBUILD) ELSE (GOTO BUILD_JOB)
:BUILD_JOB
SET buildNumber=%1
SET buildURL= "http://10.222.128.33:8080/job/RMS/job/ADXE10/"%buildNumber%"/consoleText"
SET buildDetails=Build_%buildNumber%
GOTO NEXTSTEPS
:LASTBUILD
SET buildURL= "http://10.222.128.33:8080/job/RMS/job/ADXE10/lastBuild/consoleText"
SET buildDetails=Build_%SUBFILENAME%
GOTO NEXTSTEPS

:NEXTSTEPS
echo %buildURL%

curl -o %buildDetails% --user "Infosys":"CMchange11!" %buildURL%
powershell -Command "(gc %buildDetails%) | Out-File -encoding UTF8 %buildDetails%"
powershell -Command "(gc %buildDetails%) -replace ' X ', 'FAILED: ' | Out-File -encoding UTF8 %buildDetails%"
powershell -Command "(gc %buildDetails%) -replace ' [^\u0000-\u007F] ', 'PASSED: ' | Out-File -encoding UTF8 %buildDetails%"
echo %batchpath%\ErrorCategorizationTool\Jenkins\Builds\%buildDetails%
MOVE %buildDetails% %batchpath%\ErrorCategorizationTool\Jenkins\Builds\%buildDetails%
cd "ErrorCategorizationTool"
start /W/B %batchpath%\ErrorCategorizationTool\ErrorCategorizationTool.exe
echo %ERRORLEVEL%
cd..
REM powershell -Command  "Get-ChildItem -Filter %buildDetails% -Recurse | Foreach-Object { foreach ($word in @('PASSED:', 'FAILED:')) {$_ | Select-String -Pattern $word |Select-Object Line, Pattern, LineNumber,@{ Label='INDEX';e={$_.Matches[0].INDEX}}}}|Sort-Object LineNumber,INDEX |Export-Csv -NoTypeInformation -Path %buildDetails%Results.csv -Encoding UTF8"
REM start Excel.exe %batchpath%\%buildDetails%Results.csv
REM MOVE %batchpath%\ErrorCategorizationTool\Jenkins\Jenkins_Failures %batchpath%\ErrorCategorizationTool\Jenkins\Jenkins_Failures_%SUBFILENAME%
REM powershell -Command "Copy-Item -Path %batchpath%\ErrorCategorizationTool\Jenkins\Jenkins_Failures -Destination %batchpath%\ErrorCategorizationTool\Jenkins\Jenkins_Failures_%SUBFILENAME% -Force"
start Excel.exe %batchpath%\ErrorCategorizationTool\Jenkins\Jenkins_Failures
