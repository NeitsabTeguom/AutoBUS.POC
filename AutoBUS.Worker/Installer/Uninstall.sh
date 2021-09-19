#!/bin/bash
echo Uninstall of AutoBUS Worker service

systemctl stop AutoBUS.Worker # stop service to release any file locks which could conflict with dotnet publish
systemctl disable AutoBUS.Worker
systemctl revert AutoBUS.Worker
rm /etc/systemd/system/AutoBUS.Worker.service
systemctl daemon-reload
systemctl reset-failed

rm -rf /usr/sbin/AutoBUS/Worker/*
