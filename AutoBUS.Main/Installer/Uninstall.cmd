@echo off
TITLE Uninstall of AutoBUS Main service

sc stop AutoBUSMain
sc delete AutoBUSMain
