@echo off
TITLE Install of AutoBUS Worker service

set mypath=%~dp0
sc create AutoBUSWorker start= auto binPath= "%mypath%bin\AutoBUS.Worker.exe" displayname= "AutoBUS Worker"
sc description AutoBUSWorker "AutoBUS Worker service, all in one SaaS"
sc failure AutoBUSWorker reset= 86400 actions= restart/1000/restart/1000/restart/1000
