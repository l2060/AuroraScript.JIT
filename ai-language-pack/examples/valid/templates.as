@module(TEMPLATES);

export func run(name) {
    var small = `hello ${name}`;
    var large = `[ ${0 + 10} - ${1 + 10} - ${2 + 10} - ${3 + 10} - ${4 + 10} - ${5 + 10} ]`;
    return [small, large];
}

