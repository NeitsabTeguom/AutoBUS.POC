#!/bin/bash

if [ -z "$1" ]
then
	echo "Veuillez saisir le paramètre : Description"
else
	git fetch
	git pull
	git status
	git add .
	git commit -am "$1"
	git merge --no-ff
	git fetch
	git pull
	git push
fi