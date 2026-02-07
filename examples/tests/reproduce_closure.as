var fns = [];

function setup() {
    console.log("1");
    var v = 3;
    var x = v + 5;
    var y = x * 3;
    // Closure 1: increments by 1
    fns.push(() => {
        x = x + 1;
        return x;
    });
    // Closure 2: increments by 10
    fns.push(() => {
        y = y + 10;
        return y;
    });
    if (console && console.log) {
        console.log("1");
    }
    var aff = { y: 1, a: 2, b: 3 };
    aff.y++;
    aff.c = 123;
    aff.b = () => { console.log(fns); };
    var ar = [0, 0, 0];
    var ss = [1, 2, 3, 4, 5, ...ar];
    var { a, e } = aff;
    console.log("destructuring test passed!", a, e);
    testFor(1, 2, 3, ...aff);

    var [a1, ...a2, a3] = ss;
    console.log("destructuring test passed!", a1, a2, a3);

    for (var l in aff) {
        console.log(l);
    }
    for (var k in ss) {
        console.log(k);
    }
}


func testProxy() {
    console.log("Testing Proxy");

    var proxy = new Proxy({}, {
        get: (target, prop, receiver) => {
            console.log('Getting', prop);
            return target[prop];
        },
        set: (target, prop, value, receiver) => {
            console.log('Setting', prop , value);
            target[prop] = value;
            // return Reflect.set(target, prop, value, receiver);
        }
    });

    proxy.string = "Hello, Proxy!";
    console.log(proxy.string);
    console.log(proxy);
}


func testFor() {
    console.log($args);
    for (var i = 0; i < 1000000; i++) {
    }
}


func test() {
    func makeCounter() {
        var count = 0;
        return () => {
            count = count + 1;
            return count;
        };
    }
    if (makeCounter) {
        console.log("1");
    }else {
        console.log("2");
    }


    makeCounter();
    var counter = makeCounter();
    var last = 0;
    for (var i = 0; i < 100; i++) {
        last = counter();
    }
    var s = last++;
    return last;
}

func closure1() {
    var title = '123';
    var count = 0;
    function makeCounter1() {
        var slot1 = 10;
        return () => {
            slot1++;
            title = 'ABC';
            count = count + 1;
            return { title, count };
        };
    }
    function makeCounter2() {
        var slot2 = 20;
        func ddd() {
            slot2++;
            return slot2;
        }
        return () => {
            slot2++;
            title = 'XYZ';
            count = count + 1;
            return { title, ss: ddd() };
        };
    }
    return { a: makeCounter1() , b: makeCounter2() };
}

func empty(a = 1, b = 2, c) {
    //debugger;
    console.log(a, b, c);
}



empty(3);

closure1();
test();
closure1();
setup();
testFor();


console.log("First call (x+1): " + fns[0]());
console.log("Second call (x+10): " + fns[1]());
console.log("Third call (x+1): " + fns[0]());
testProxy();