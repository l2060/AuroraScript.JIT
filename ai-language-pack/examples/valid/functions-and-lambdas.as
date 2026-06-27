@module(FUNCTIONS_AND_LAMBDAS);

@directCall
func add(a, b = 1) {
    return a + b;
}

export func run() {
    var inc = x => x + 1;
    var sum = (a, b) => { return a + b; };
    return [add(1), inc(2), sum(3, 4)];
}

