@module(DEBUG_TEST);
include 'debug_inc';

export function main() {
    console.log("Main start");
    callNested();
}

function callNested() {
    console.log("In callNested, calling throwError");
    throwError();
}
export func run(){

    console.log('DEBUG_TEST run();');
}