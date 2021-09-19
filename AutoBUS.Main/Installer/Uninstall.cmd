@echo off
TITLE Uninstall of AutoBUS Main service

sc stop AutoBUSMain

:loop
sc query AutoBUSMain | find "STOPPED"
if errorlevel 1 (
  timeout 1
  goto loop
)

sc delete AutoBUSMain

timeout 5

rmdir .\bin\ /s /q
del *.cmd
