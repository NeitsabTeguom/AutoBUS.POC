#!/bin/bash
echo Install of AutoBUS Worker service

/usr/sbin/AutoBUS/Worker/Uninstall.sh

mkdir /usr/sbin/AutoBUS/
mkdir /usr/sbin/AutoBUS/Worker/
mkdir /usr/sbin/AutoBUS/Worker/bin/

cp ./Start.sh /usr/sbin/AutoBUS/Worker/
cp ./Stop.sh /usr/sbin/AutoBUS/Worker/
cp ./Uninstall.sh /usr/sbin/AutoBUS/Worker/
cp ./bin/* /usr/sbin/AutoBUS/Worker/bin/

sudo chmod +x /usr/sbin/AutoBUS/Worker/bin/AutoBUS.Worker.exe

systemctl stop AutoBUS.Worker # stop service to release any file locks which could conflict with dotnet publish
cp AutoBUS.Worker.service /etc/systemd/system/AutoBUS.Worker.service
systemctl daemon-reload
systemctl enable AutoBUS.Worker

systemctl start AutoBUS.Worker
