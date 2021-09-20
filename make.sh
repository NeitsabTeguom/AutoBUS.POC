#!/bin/bash
echo Install of AutoBUS Main service

mypath=`pwd`
make=$mypath/make

logfile=$make/makeErrors.log

pkill dotnet

mkdir $make/
rm -rf $make/

# Main

mkdir $make/AutoBUS.Main/
rm -rf $mypath/AutoBUS.Main/bin/
rm -rf $mypath/AutoBUS.Main/obj/
echo Publication of Main
echo
cd $mypath/AutoBUS.Main/
dotnet publish -c Release -o "$make/AutoBUS.Main/bin/" | grep "error CS">$logfile
if [ $? -eq 0 ]
then
 echo ----------------------------------------------------
 echo ERREUR DE PUBLICATION - AutoBUS.Main
 echo ----------------------------------------------------
 cat $logfile
 del $logfile
 rm -rf $make\
 exit 1
fi

cd $mypath/

echo
echo Copie Main Installer
cp $mypath/AutoBUS.Main/Installer/*.cmd $make/AutoBUS.Main/
cp $mypath/AutoBUS.Main/Installer/*.sh $make/AutoBUS.Main/


# Worker

mkdir $make/AutoBUS.Worker/
rm -rf $mypath/AutoBUS.Worker/bin/
rm -rf $mypath/AutoBUS.Worker/obj/
echo Publication of Worker
echo
cd $mypath/AutoBUS.Worker/
dotnet publish -c Release -o "$make/AutoBUS.Worker/bin/" | grep "error CS">$logfile
if [ $? -eq 0 ]
then
 echo ----------------------------------------------------
 echo ERREUR DE PUBLICATION - AutoBUS.Worker
 echo ----------------------------------------------------
 cat $logfile
 del $logfile
 rm -rf $make\
 exit 1
fi

cd $mypath/

echo
echo Copie Worker Installer
cp $mypath/AutoBUS.Worker/Installer/*.cmd $make/AutoBUS.Worker/
cp $mypath/AutoBUS.Worker/Installer/*.sh $make/AutoBUS.Worker/


cp $mypath/README.md $make/
