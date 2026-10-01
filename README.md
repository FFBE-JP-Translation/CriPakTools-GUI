CriPakTools-mod
===========
This tool is based on code by Falo , Nanashi3 ,esperknight and uyjulian. 

I forked and added more features in the *NEW* GUI Version. 

* Add Shift-JIS support for CPK files.
* Support 2GB+ CPK for PS3.
* Add Batch Mode
* Add compression code(Thanks for KenTse 's CRILAYLA compression method)
* Fix GTOC & ETOC
* Fix CPK header
* Add GUI



===========

Tool to extract/update contents of CRIWARE's CPK archive format. (aka CRI FileMajik)  
This is based on codes uploaded by Falo's code released on the Xentax forums (http://forum.xentax.com/viewtopic.php?f=10&t=10646) which was futher modified by Nanashi3 (http://forums.fuwanovel.org/index.php?/topic/1785-request-for-psp-hackers/page-4),and esperknight (https://github.com/esperknight/CriPakTools).  

New CPK format (per-file encryption)
====================================
Some CPKs (e.g. the Japanese version of FFBE) store every file scrambled with a key derived from the
file's own name (see `LibCPK/AssetCipher.cs` for the algorithm). Use the *new format* option to handle them:

* CLI: add `-nf` / `--new-format` to `extract_all` (decrypts the files) and to `replace` (encrypts the patch files).
* GUI: tick `Options > New format (per-file encryption)` before extracting, or the checkbox in the *Patch CPK* window.

```
CriPakTools.exe extract_all -p gallery1.cpk -o extracted --new-format
CriPakTools.exe replace -p gallery1.cpk -i extracted -o rebuilt.cpk --new-format
```

In new format mode the header, TOC and padding of the original CPK are kept, so re-packing the unmodified
extracted files reproduces the original CPK byte for byte.

Compiling
=========
Install Visual Stuidio 2019 & Visual C++ V142.

Change directory to where the `CriPakTools.sln` file is located, 

Output file should be in `CriPakGUI/bin/CriPakGUI.exe`. Otherwise, just open the `CriPakTools.sln` file in Visual Studio 2019 and build.

