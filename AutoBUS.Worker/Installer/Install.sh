#!/bin/bash
echo Install of AutoBUS Worker service

/usr/sbin/AutoBUS/Worker/Uninstall.sh

mkdir /usr/sbin/AutoBUS/
mkdir /usr/sbin/AutoBUS/Worker/
mkdir /usr/sbin/AutoBUS/Worker/bin/

cp ./Start.sh /usr/sbin/AutoBUS/Worker/
cp ./Stop.sh /usr/sbin/AutoBUS/Worker/
cp ./Uninstall.sh /usr/sbin/AutoBUS/Worker/
cp -r ./bin/* /usr/sbin/AutoBUS/Worker/bin/
sudo chmod +x /usr/sbin/AutoBUS/Worker/*.sh

sudo chmod +x /usr/sbin/AutoBUS/Worker/bin/AutoBUS.Worker

systemctl stop AutoBUS.Worker.service
cp AutoBUS.Worker.service /etc/systemd/system/AutoBUS.Worker.service
systemctl daemon-reload
systemctl enable AutoBUS.Worker.service
systemctl start AutoBUS.Worker.service

