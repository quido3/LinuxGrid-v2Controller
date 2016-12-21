# LinuxGrid-v2Controller
A c# terminal program to control the NZXT Grid+ V2 fan controller.

This is a C# program that automates the fan speed of a NZXT Grid+ V2 controller.

This program is made for my personal use, and in no way is it meant for commercial distribution nor do I provide support for it. Any harm that might have been caused by using this software is in the users own responsibility.

I have not done excessive testing on this software, but am running it myself.

This software relies heavily on other software present in the system.

Requirements:
-You need a gridfan bash script made by CapitalF: https://github.com/CapitalF/gridfan
-You should be able to get GPU info with 'nvidia-smi -q -d temperature' command
-You should be able to get CPU temperature info with 'sensors'
