#!/bin/bash
echo Install of AutoBUS Main service

mkdir /usr/sbin/AutoBUS/
mkdir /usr/sbin/AutoBUS/Main/
mkdir /usr/sbin/AutoBUS/Main/bin/

cp ./Start.sh /usr/sbin/AutoBUS/Main/
cp ./Stop.sh /usr/sbin/AutoBUS/Main/
cp ./Uninstall.sh /usr/sbin/AutoBUS/Main/
cp ./bin/* /usr/sbin/AutoBUS/Main/bin/

sudo chmod +x /usr/sbin/AutoBUS/Main/bin/AutoBUS.Main.exe

systemctl stop AutoBUS.Main # stop service to release any file locks which could conflict with dotnet publish
cp AutoBUS.Main.service /etc/systemd/system/AutoBUS.Main.service
systemctl daemon-reload
systemctl enable AutoBUS.Main

systemctl start AutoBUS.Main
