@echo off
TITLE Install of AutoBUS Main service

set mypath=%~dp0

mkdir "%ProgramFiles%\AutoBUS\"
mkdir "%ProgramFiles%\AutoBUS\Main\"
xcopy %mypath%Start.cmd "%ProgramFiles%\AutoBUS\Main\" /B/V/Y/E
xcopy %mypath%Stop.cmd "%ProgramFiles%\AutoBUS\Main\" /B/V/Y/E
xcopy %mypath%Uninstall.cmd "%ProgramFiles%\AutoBUS\Main\" /B/V/Y/E
xcopy %mypath%bin\* "%ProgramFiles%\AutoBUS\Main\bin\" /B/V/Y/E

sc create AutoBUSMain start= auto binPath= "%ProgramFiles%\AutoBUS\Main\bin\AutoBUS.Main.exe" displayname= "AutoBUS Main"
sc description AutoBUSMain "AutoBUS Main service, all in one SaaS"
sc failure AutoBUSMain reset= 86400 actions= restart/1000/restart/1000/restart/1000

sc start AutoBUSMain
