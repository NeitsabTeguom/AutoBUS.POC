@echo off
TITLE Uninstall of AutoBUS Worker service

sc stop AutoBUSWorker

:loop
sc query AutoBUSWorker | find "STOPPED"
if errorlevel 1 (
  timeout 1
  goto loop
)

sc delete AutoBUSWorker

timeout 5

rmdir .\bin\ /s /q
del *.cmd
