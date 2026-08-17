@module(RUNTIME_BENCH);

import helper from "helper";

func localAdd(a, b, c) {
    return a + b + c;
}

export func emptyCall() {
    return 1;
}

export func functionCallLoop(iterations = 1000) {
    var sum = 0;
    for (var i = 0; i < iterations; i++) {
        sum = sum + localAdd(i, 1, 2);
    }
    return sum;
}

export func moduleCallLoop(iterations = 1000) {
    var sum = 0;
    for (var i = 0; i < iterations; i++) {
        sum = sum + helper.add3(i, 1, 2);
    }
    return sum;
}

export func numericLoop(iterations = 1000) {
    var sum = 0;
    for (var i = 0; i < iterations; i++) {
        sum = sum + ((i * 3) - (i / 2));
    }
    return sum;
}

export func objectCreateSetGet(iterations = 1000) {
    var sum = 0;
    for (var i = 0; i < iterations; i++) {
        var item = { id: i, name: "Aurora", value: i + 1 };
        item.extra = item.id + item.value;
        sum = sum + item.extra;
    }
    return sum;
}

export func objectForIn(iterations = 1000) {
    var item = { a: 1, b: 2, c: 3, d: 4, e: 5, f: 6 };
    var sum = 0;
    for (var i = 0; i < iterations; i++) {
        for (var key in item) {
            sum = sum + item[key];
        }
    }
    return sum;
}

export func arrayPushIndex(iterations = 1000) {
    var values = [];
    for (var i = 0; i < iterations; i++) {
        values.push(i);
    }
    var sum = 0;
    for (var j = 0; j < iterations; j++) {
        sum = sum + values[j];
    }
    return sum;
}

export func arrayLiteralIndex(iterations = 1000) {
    var sum = 0;
    for (var i = 0; i < iterations; i++) {
        var values = [i, i + 1, i + 2, i + 3];
        sum = sum + values[0] + values[3];
    }
    return sum;
}

export func arrayFixedIndex(iterations = 1000) {
    var values = new Array(iterations);
    for (var i = 0; i < iterations; i++) {
        values[i] = i;
    }
    var sum = 0;
    for (var j = 0; j < iterations; j++) {
        sum = sum + values[j];
    }
    return sum;
}

export func int32ArrayIndex(iterations = 1000) {
    var values = new Int32Array(iterations);
    for (var i = 0; i < iterations; i++) {
        values[i] = i;
    }
    var sum = 0;
    for (var j = 0; j < iterations; j++) {
        sum = sum + values[j];
    }
    return sum;
}

export func float64ArrayIndex(iterations = 1000) {
    var values = new Float64Array(iterations);
    for (var i = 0; i < iterations; i++) {
        values[i] = i + 0.25;
    }
    var sum = 0;
    for (var j = 0; j < iterations; j++) {
        sum = sum + values[j];
    }
    return sum;
}

export func int32ArrayObjectBoundary(iterations = 1000) {
    var holder = { values: new Int32Array(iterations) };
    var values = holder.values;
    for (var i = 0; i < iterations; i++) {
        values[i] = i;
    }
    var sum = 0;
    for (var j = 0; j < iterations; j++) {
        sum = sum + values[j];
    }
    return sum;
}

export func int8AndBooleanArrayIndex(iterations = 1000) {
    var values = new Int8Array(iterations);
    var flags = new BooleanArray(iterations);
    for (var i = 0; i < iterations; i++) {
        values[i] = i;
        flags[i] = (i % 2) == 0;
    }
    var sum = 0;
    for (var j = 0; j < iterations; j++) {
        if (flags[j]) sum = sum + values[j];
    }
    return sum;
}

@directCall
func int32PrngCore(iterations, seed) {
    var state = seed;
    var checksum = 0;
    for (var i = 0; i < iterations; i++) {
        state = state ^ (state << 13);
        state = state ^ (state >> 17);
        state = state ^ (state << 5);
        checksum = checksum ^ state;
    }
    return checksum;
}

export func int32PrngKernel(iterations = 1000) {
    return int32PrngCore(iterations | 0, 123456789);
}

@directCall
func packedChecksumCore(iterations) {
    var values = new Int32Array(iterations);
    var state = 246353424;
    for (var i = 0; i < iterations; i++) {
        state = state ^ (state << 13);
        state = state ^ (state >> 17);
        state = state ^ (state << 5);
        values[i] = state;
    }

    var checksum = 0;
    for (var j = 0; j < iterations; j++) {
        checksum = checksum ^ values[j];
    }
    return checksum;
}

export func packedChecksumKernel(iterations = 1000) {
    return packedChecksumCore(iterations | 0);
}

@directCall
func integerHeapKernelCore(iterations) {
    var heap = new Int32Array(iterations);
    var scores = new Int32Array(iterations);
    var checksum = 0;

    for (var node = 0; node < iterations; node++) {
        var score = node ^ (node << 13);
        score = score ^ (score >> 17);
        score = score ^ (score << 5);
        scores[node] = score;

        var pos = node;
        while (pos > 0) {
            var parent = (pos - 1) >> 1;
            var parentNode = heap[parent];
            if (scores[parentNode] <= score) break;
            heap[pos] = parentNode;
            pos = parent;
        }
        heap[pos] = node;
        checksum = checksum ^ heap[0];
    }
    return checksum;
}

export func integerHeapKernel(iterations = 1000) {
    return integerHeapKernelCore(iterations | 0);
}

export func hashMapSetGet(iterations = 1000) {
    var map = new HashMap(iterations);
    for (var i = 0; i < iterations; i++) {
        map.set("k" + i, i);
    }
    var sum = 0;
    for (var j = 0; j < iterations; j++) {
        sum = sum + map.get("k" + j);
    }
    return sum + map.size;
}

export func stringConcat(iterations = 1000) {
    var value = "";
    for (var i = 0; i < iterations; i++) {
        value = value + "x" + i;
    }
    return value.length;
}

export func templateSmall(iterations = 1000) {
    var value = "";
    for (var i = 0; i < iterations; i++) {
        value = `${i}:${i + 1}`;
    }
    return value.length;
}

export func templateLarge(iterations = 1000) {
    var value = "";
    for (var i = 0; i < iterations; i++) {
        value = `a${i}b${i + 1}c${i + 2}d${i + 3}e`;
    }
    return value.length;
}

export func stringBufferAppend(iterations = 1000) {
    var buffer = new StringBuffer("");
    for (var i = 0; i < iterations; i++) {
        buffer.append("x");
        buffer.append(i);
    }
    return buffer.stringAndRelease().length;
}

export func jsonStringify(iterations = 1000) {
    var value = { id: 1, name: "Aurora", flags: [true, false, null], nested: { a: 1, b: "x" } };
    var json = "";
    for (var i = 0; i < iterations; i++) {
        json = JSON.stringify(value);
    }
    return json.length;
}

export func jsonParse(iterations = 1000) {
    var json = '{"id":1,"name":"Aurora","flags":[true,false,null],"nested":{"a":1,"b":"x"}}';
    var value = null;
    for (var i = 0; i < iterations; i++) {
        value = JSON.parse(json);
    }
    return value.nested.a;
}

export func jsonRoundTrip(iterations = 1000) {
    var value = { id: 1, name: "Aurora", flags: [true, false, null], nested: { a: 1, b: "x" } };
    var parsed = null;
    for (var i = 0; i < iterations; i++) {
        parsed = JSON.parse(JSON.stringify(value));
    }
    return parsed.id;
}

export func regexMatchAll(iterations = 1000) {
    var text = "AuroraScript 123 test456 Aurora789";
    var total = 0;
    for (var i = 0; i < iterations; i++) {
        var matches = text.matchAll(/([A-Za-z]+)(\d*)/g);
        total = total + matches.length;
    }
    return total;
}

export func closureInvoke(iterations = 1000) {
    var seed = 1;
    var next = (value) => {
        seed = seed + value;
        return seed;
    };
    var sum = 0;
    for (var i = 0; i < iterations; i++) {
        sum = sum + next(1);
    }
    return sum;
}

export func clrPropertyGetSet(iterations = 1000) {
    for (var i = 0; i < iterations; i++) {
        host.Name = "Aurora";
        host.Count = i;
    }
    return host.Count;
}

export func clrInstanceMethod(iterations = 1000) {
    for (var i = 0; i < iterations; i++) {
        host.Say(i, "Aurora");
    }
    return host.Count;
}

export func clrStaticMethod(iterations = 1000) {
    var value = "";
    for (var i = 0; i < iterations; i++) {
        value = HostObject.Cat("[", "-", "]");
    }
    return value.length;
}

export func clrArrayArgument(iterations = 1000) {
    var value = "";
    for (var i = 0; i < iterations; i++) {
        value = HostObject.CatArray(["[", "-", "]"]);
    }
    return value.length;
}
