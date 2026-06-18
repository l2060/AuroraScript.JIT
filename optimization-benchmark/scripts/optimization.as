@module(OPT_BENCH);

export func empty() {
    return null;
}

export func callNoArgs(iterations = 10000) {
    function noop() {
        return 1;
    }
    var sum = 0;
    for (var i = 0; i < iterations; i++) {
        sum = sum + noop();
    }
    return sum;
}

export func callOneArg(iterations = 10000) {
    function inc(value) {
        return value + 1;
    }
    var sum = 0;
    for (var i = 0; i < iterations; i++) {
        sum = sum + inc(i);
    }
    return sum;
}

export func callTwoArgs(iterations = 10000) {
    function add(a, b) {
        return a + b;
    }
    var sum = 0;
    for (var i = 0; i < iterations; i++) {
        sum = sum + add(i, 1);
    }
    return sum;
}

export func callThreeArgs(iterations = 10000) {
    function add3(a, b, c) {
        return a + b + c;
    }
    var sum = 0;
    for (var i = 0; i < iterations; i++) {
        sum = sum + add3(i, 1, 2);
    }
    return sum;
}

export func callFourArgs(iterations = 10000) {
    function add4(a, b, c, d) {
        return a + b + c + d;
    }
    var sum = 0;
    for (var i = 0; i < iterations; i++) {
        sum = sum + add4(i, 1, 2, 3);
    }
    return sum;
}

export func callFiveArgs(iterations = 10000) {
    function add5(a, b, c, d, e) {
        return a + b + c + d + e;
    }
    var sum = 0;
    for (var i = 0; i < iterations; i++) {
        sum = sum + add5(i, 1, 2, 3, 4);
    }
    return sum;
}

export func callSevenArgs(iterations = 10000) {
    function add7(a, b, c, d, e, f, g) {
        return a + b + c + d + e + f + g;
    }
    var sum = 0;
    for (var i = 0; i < iterations; i++) {
        sum = sum + add7(i, 1, 2, 3, 4, 5, 6);
    }
    return sum;
}

export func propertyCallTwoArgs(iterations = 10000) {
    var ops = {
        add2: (a, b) => {
            return a + b;
        }
    };
    var sum = 0;
    for (var i = 0; i < iterations; i++) {
        sum = sum + ops.add2(i, 1);
    }
    return sum;
}

export func propertyCallThreeArgs(iterations = 10000) {
    var ops = {
        add3: (a, b, c) => {
            return a + b + c;
        }
    };
    var sum = 0;
    for (var i = 0; i < iterations; i++) {
        sum = sum + ops.add3(i, 1, 2);
    }
    return sum;
}

export func propertyCallFourArgs(iterations = 10000) {
    var ops = {
        add4: (a, b, c, d) => {
            return a + b + c + d;
        }
    };
    var sum = 0;
    for (var i = 0; i < iterations; i++) {
        sum = sum + ops.add4(i, 1, 2, 3);
    }
    return sum;
}

export func propertyCallSevenArgs(iterations = 10000) {
    var ops = {
        add7: (a, b, c, d, e, f, g) => {
            return a + b + c + d + e + f + g;
        }
    };
    var sum = 0;
    for (var i = 0; i < iterations; i++) {
        sum = sum + ops.add7(i, 1, 2, 3, 4, 5, 6);
    }
    return sum;
}

export func numericLoop(iterations = 100000) {
    var acc = 0;
    for (var i = 0; i < iterations; i++) {
        acc = (acc + i) % 97;
    }
    return acc;
}

export func objectCreateSetGet(iterations = 10000) {
    var total = 0;
    for (var i = 0; i < iterations; i++) {
        var obj = {
            a: i,
            b: i + 1,
            c: i + 2
        };
        obj.d = obj.a + obj.b + obj.c;
        total = total + obj.d;
    }
    return total;
}

export func objectEnumerate(iterations = 10000) {
    var total = 0;
    var obj = {
        a: 1,
        b: 2,
        c: 3,
        d: 4,
        e: 5,
        f: 6
    };
    for (var i = 0; i < iterations; i++) {
        for (var key in obj) {
            total = total + obj[key];
        }
    }
    return total;
}

export func arrayLiteral(iterations = 10000) {
    var total = 0;
    for (var i = 0; i < iterations; i++) {
        var arr = [i, i + 1, i + 2, i + 3];
        total = total + arr[0] + arr[1] + arr[2] + arr[3];
    }
    return total;
}

export func arrayPushIndex(iterations = 10000) {
    var arr = [];
    for (var i = 0; i < iterations; i++) {
        arr.push(i);
    }
    var total = 0;
    for (var j = 0; j < iterations; j++) {
        total = total + arr[j];
    }
    return total;
}

export func stringConcat(iterations = 1000) {
    var text = "";
    for (var i = 0; i < iterations; i++) {
        text = text + "x" + i;
    }
    return text.length;
}

export func closureInvoke(iterations = 10000) {
    function makeCounter() {
        var count = 0;
        return () => {
            count = count + 1;
            return count;
        };
    }
    var counter = makeCounter();
    var last = 0;
    for (var i = 0; i < iterations; i++) {
        last = counter();
    }
    return last;
}
