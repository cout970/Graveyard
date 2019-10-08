#!/bin/sh

NAME='Graveyard-1.1.4'

mkdir tmp
cp -r assets tmp/assets
cp modicon.png tmp/modicon.png
cp modinfo.json tmp/modinfo.json
cp obj/Debug/Graveyard.dll tmp/Graveyard.dll

cd tmp
zip -r "${NAME}.zip" *

cd ..
mv "tmp/${NAME}.zip" "obj/${NAME}.zip"

rm -r tmp
