#!/bin/sh

svn export ../../output/extract.app extract.app

pushd ../../source/extract/
lazbuild -B --os=darwin --ws=carbon --cpu=i386 extract.lpr
popd
strip ../../output/extract
cp ../../output/extract extract.app/Contents/MacOS/extract
cp Info.plist extract.app/Contents

mv extract.app Akeeba\ eXtract\ Wizard.app
zip -r akeeba-extract-wizard-max-os-x.zip Akeeba\ eXtract\ Wizard.app
mv *.zip ../../output
rm -rf *.app