@module(DESTRUCTURING_AND_LOOPS);

export func run() {
    var { name, age } = { name: "Aurora", age: 6 };
    var [ first, ...rest ] = [1, 2, 3, 4];
    var total = first;
    for (var value in rest) {
        total += value;
    }
    return `${name}:${age}:${total}`;
}

