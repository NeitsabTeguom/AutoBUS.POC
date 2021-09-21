#!/bin/bash
echo Stop of AutoBUS Worker service

systemctl stop AutoBUS.Worker # stop service to release any file locks which could conflict with dotnet publish
