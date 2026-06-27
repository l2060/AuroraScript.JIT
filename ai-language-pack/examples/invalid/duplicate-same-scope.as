@module(DUPLICATE_SAME_SCOPE);

export func run() {
    const a = 123;
    var a = 456;
    return a;
}

