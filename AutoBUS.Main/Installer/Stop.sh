#!/bin/bash
echo Stop of AutoBUS Main service

systemctl stop AutoBUS.Main # stop service to release any file locks which could conflict with dotnet publish
