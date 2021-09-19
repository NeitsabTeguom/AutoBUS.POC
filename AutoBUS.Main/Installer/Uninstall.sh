#!/bin/bash
echo Uninstall of AutoBUS Main service

systemctl stop AutoBUS.Main # stop service to release any file locks which could conflict with dotnet publish
systemctl disable AutoBUS.Main
systemctl revert AutoBUS.Main
rm /etc/systemd/system/AutoBUS.Main.service
systemctl daemon-reload
systemctl reset-failed

rm -rf /usr/sbin/AutoBUS/Main/*
