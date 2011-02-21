#!/bin/bash

pushd ../../source/extract/
lazbuild -B --os=linux --ws=gtk2 --cpu=i386 extract.lpr
popd
strip ../../output/extract
cp ../../output/extract .

zip -r extract-linux-i386-gtk2.zip extract
mv *.zip ../../output
rm -rf extract
