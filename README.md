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

FFBE JP CPK format (per-file encryption)
========================================
The Japanese version of FFBE stores every file in its CPKs encrypted with a key derived from the file's own name
(see `LibCPK/AssetCipher.cs` for the algorithm). Use the *FFBE JP format* option to handle these archives:

* CLI: add `-jp` / `--ffbejp` to `extract_all` (decrypts the files) and to `replace` (encrypts the patch files).
* GUI: tick `Options > FFBE JP format (per-file encryption)` before extracting, or the checkbox in the *Patch CPK* window.

```
CriPakTools.exe extract_all -p gallery1.cpk -o extracted --ffbejp
CriPakTools.exe replace -p gallery1.cpk -i extracted -o rebuilt.cpk --ffbejp
```

Compiling
=========
Install Visual Stuidio 2019 & Visual C++ V142.

Change directory to where the `CriPakTools.sln` file is located, 

Output file should be in `CriPakGUI/bin/CriPakGUI.exe`. Otherwise, just open the `CriPakTools.sln` file in Visual Studio 2019 and build.

