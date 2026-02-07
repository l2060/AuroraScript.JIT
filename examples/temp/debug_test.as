@module(DEBUG_TEST);
include 'debug_inc';

function main() {
    console.log("Main start");
    callNested();
}

function callNested() {
    console.log("In callNested, calling throwError");
    throwError();
}
