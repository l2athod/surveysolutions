
// do not edit this file with Notepad.exe

clear all
use "TestStata14Missings.dta", clear

assert c(N)==5
assert c(k)==4

// check variable names
quietly ds
assert r(varlist)=="V0 V1 V2 V3"

// check variable types
confirm double variable V0
confirm int variable V1
confirm double variable V2
confirm str1 variable V3

// check the values are missing where appropriate
assert V0[3]==.a
assert V1[3]==.a
assert V2[4]==.a
assert V2[5]==.b

// test is now completed successfully, create the marker
display filewrite("MarkerMissings.txt","ok")

exit, STATA clear
