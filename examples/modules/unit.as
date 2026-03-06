@module(UNIT_LIB);


var node = {
    A: 1,
    B: 2,
    C: 3,
    D: 4,
    E: "Hello",
    F: () => { console.log("reset"); }
};

node = Object.assign(node, { 你好: 'Hello' } );

