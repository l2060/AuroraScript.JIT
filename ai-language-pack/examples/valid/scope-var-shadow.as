@module(SCOPE_VAR_SHADOW);

export func run() {
    var a = 123;
    var inner = 0;
    {
        var a = 123456;
        console.log(a);
        inner = a;
    }
    return [a, inner];
}

