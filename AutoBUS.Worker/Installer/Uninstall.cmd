@echo off
TITLE Uninstall of AutoBUS Worker service

sc stop AutoBUSWorker
sc delete AutoBUSWorker
