@echo off
TITLE Install of AutoBUS Main service

set mypath=%~dp0
sc create AutoBUSMain start= auto binPath= "%mypath%bin\AutoBUS.Main.exe" displayname= "AutoBUS Main"
sc description AutoBUSMain "AutoBUS Main service, all in one SaaS"
sc failure AutoBUSMain reset= 86400 actions= restart/1000/restart/1000/restart/1000
