@echo off
TITLE Install of AutoBUS Worker service

set mypath=%~dp0

mkdir "%ProgramFiles%\AutoBUS\"
mkdir "%ProgramFiles%\AutoBUS\Worker\"
xcopy %mypath%Start.cmd "%ProgramFiles%\AutoBUS\Worker\" /B/V/Y/E
xcopy %mypath%Stop.cmd "%ProgramFiles%\AutoBUS\Worker\" /B/V/Y/E
xcopy %mypath%Uninstall.cmd "%ProgramFiles%\AutoBUS\Worker\" /B/V/Y/E
xcopy %mypath%bin\* "%ProgramFiles%\AutoBUS\Worker\bin\" /B/V/Y/E

sc create AutoBUSWorker start= auto binPath= "%ProgramFiles%\AutoBUS\Worker\bin\AutoBUS.Worker.exe" displayname= "AutoBUS Worker"
sc description AutoBUSWorker "AutoBUS Worker service, all in one SaaS"
sc failure AutoBUSWorker reset= 86400 actions= restart/1000/restart/1000/restart/1000

sc start AutoBUSWorker
