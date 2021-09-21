#!/bin/bash
echo Uninstall of AutoBUS Main service

systemctl stop AutoBUS.Main.service
systemctl disable AutoBUS.Main.service
systemctl revert AutoBUS.Main.service
systemctl reset-failed
rm -f /etc/systemd/system/AutoBUS.Main.service
systemctl daemon-reload

rm -rf /usr/sbin/AutoBUS/Main/*

