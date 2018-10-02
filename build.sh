#!/bin/sh

mkdir tmp
cp -r assets tmp/assets
cp modicon.png tmp/modicon.png
cp modinfo.json tmp/modinfo.json
cp obj/Debug/Graveyard.dll tmp/Graveyard.dll

cd tmp
zip -r Graveyard.zip *

cd ..
mv tmp/Graveyard.zip obj/Graveyard.zip

rm -r tmp