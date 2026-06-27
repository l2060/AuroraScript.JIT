@module(CONST_SHADOW_IN_CHILD_BLOCK);

export func run() {
    const a = 123;
    {
        var a = 456;
    }
    return a;
}

