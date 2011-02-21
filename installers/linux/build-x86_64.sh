#!/bin/bash

pushd ../../source/extract/
lazbuild -B --os=linux --ws=gtk2 --cpu=x86_64 --bm="Linux 64" extract.lpr
popd
strip ../../output/extract
cp ../../output/extract .

zip -r extract-linux-x86_64-gtk2.zip extract
mv *.zip ../../output
rm -rf extract
