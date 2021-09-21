#!/bin/bash
echo Uninstall of AutoBUS Worker service

systemctl stop AutoBUS.Worker.service
systemctl disable AutoBUS.Worker.service
systemctl revert AutoBUS.Worker.service
systemctl reset-failed
rm -f /etc/systemd/system/AutoBUS.Worker.service
systemctl daemon-reload

rm -rf /usr/sbin/AutoBUS/Worker/*

