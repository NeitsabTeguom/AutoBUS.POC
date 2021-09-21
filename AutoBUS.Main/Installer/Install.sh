#!/bin/bash
echo Install of AutoBUS Main service

/usr/sbin/AutoBUS/Main/Uninstall.sh

mkdir /usr/sbin/AutoBUS/
mkdir /usr/sbin/AutoBUS/Main/
mkdir /usr/sbin/AutoBUS/Main/bin/

cp ./Start.sh /usr/sbin/AutoBUS/Main/
cp ./Stop.sh /usr/sbin/AutoBUS/Main/
cp ./Uninstall.sh /usr/sbin/AutoBUS/Main/
cp -r ./bin/* /usr/sbin/AutoBUS/Main/bin/
sudo chmod +x /usr/sbin/AutoBUS/Main/*.sh

sudo chmod +x /usr/sbin/AutoBUS/Main/bin/AutoBUS.Main

systemctl stop AutoBUS.Main.service
cp AutoBUS.Main.service /etc/systemd/system/AutoBUS.Main.service
systemctl daemon-reload
systemctl enable AutoBUS.Main.service
systemctl start AutoBUS.Main.service

