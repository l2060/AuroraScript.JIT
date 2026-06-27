@module(STRING_BUFFER_EXAMPLE);

export func run(count) {
    var buffer = new StringBuffer("");
    for (var i = 0; i < count; i++) {
        buffer.append(i);
        if (i + 1 < count) {
            buffer.append(",");
        }
    }
    return buffer.stringAndRelease();
}

