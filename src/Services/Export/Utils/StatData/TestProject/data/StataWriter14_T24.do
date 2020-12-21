
// do not edit this file with Notepad.exe

clear all
use "StataWriter14_T24.dta", clear

assert c(N)==4
assert c(k)==2

// check variable names
quietly ds
assert r(varlist)=="x y"

// check variable types
confirm double variable x
confirm double variable y


// check the values are missing where appropriate
assert x[1]==1
assert x[2]==3.14159265
assert x[3]==-0.00010203
assert x[4]==.a

assert y[1]==2
assert y[2]==5
assert y[3]==-5
assert y[4]==.a

assert `"`:value label x'"'==""
assert `"`:value label y'"'=="y"


// test is now completed successfully, create the marker
display filewrite("StataWriter14_T24.txt","ok")

exit, STATA clear