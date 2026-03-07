@module(UNIT_LIB);

import md5 from 'md5';

var node = {
    A: 1,
    B: 2,
    C: 3,
    D: 4,
    E: "Hello",
    F: () => { console.log("reset"); }
};

node = Object.assign(node, { 你好: 'Hello' });

const _boolean = true;
const _string = 'Hanks';
const _date = Date.now();
const _obj =  {v:11};
const _number = 1 + 5;
const _array = [1,2,3,4,5,6,7,8,9,0];



export func testEmpty() {
    return null;
}


export func testMD5() {
    md5.MD5("12345");
}

export function testMD5_100() {
    for (var i = 0; i < 100; i++) {
        md5.MD5("12345");
    }
}

export func testFor1E(count = 100000000) {
    for (var o = 0; o < count; o++) {
        // .....
    }
}

export func testClrType() {
    var s = TestObject();
    s.Title = "hello boy.";
    s.Name = "Hanks";
    s.Number = Math2.Log10(5);
    // $state is UserState in ExecuteOptions
    $state.Test(123.45,'abc');
}

export func testDatetime(){
    var now = Date.now();
    var utcNow = Date.utcNow();
    // =============
    var s1 = now.toString();
    var s2 = now.toString("yyyy-MM-dd HH:mm:ss fff");
    // =============
    var s3 = utcNow.toString();
    var s4 = utcNow.toString("yyyy-MM-dd HH:mm:ss fff");
}


export func testGetModule() {
    return global.modules["MD5_LIB"];
}


export func testJson() {
    var node = {
        A: 1,
        B: 2,
        C: 3,
        D: 4,
        E: "Hello",
        F: () => { console.log("reset"); }
    };
    var json = JSON.stringify(node, true);
    var obj = JSON.parse(json);
}

export func testDeConstruct(){
    var a = [4,5,6];
    var b = [1,2,3, ...a,7,8,9];
    var c = {d:4,e:5,f:6};
    var d = {a:1,b:2,c:3,...c,g:7,h:8,...b};
}

function replacer(match, p1, p2, p3, offset, string) {
  // p1 是非数字，p2 是数字，且 p3 非字母数字
  return [p1, p2, p3].join(" - ");
}


export func testRegex(){
    var regex = /(?<animal>fox|cat) jumps over/;
    var paragraph = "The quick brown fox jumps over the lazy dog. It barked.";
    const found1 = paragraph.match(regex);

    const paragraph2 = "The quick brown fox jumps over the lazy dog. It barked.";
    const regex2 = /[A-Z]/g;
    const found2 = paragraph2.match(regex2);

    const str = "For more information, see Chapter 3.4.5.1";
    const re = /see (chapter \d+(\.\d)*)/i;
    const found3 = str.match(re);

    var array = "test1test2".matchAll(/t(e)(st(\d?))/g);
    const newString = "abc12345#$*%".replace(/([^\d]*)(\d*)([^\w]*)/, replacer);

}


export function testArrays(iterations = 10000) {
    var arr = [];
    for (var i = 0; i < iterations; i++) {
        arr.push(i);
    }
    var sum = 0;
    var len = arr.length;
    for (var j = 0; j < len; j++) {
        sum = sum + arr[j];
    }
    return sum;
}

export function testClosure(iterations = 10000) {
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


export function testObjects(iterations = 10000) {
    var total = 0;
    for (var i = 0; i < iterations; i++) {
        var obj = { index: i, value: i & 7 };
        obj.sum = obj.index + obj.value;
        total = total + obj.sum;
    }
    return total;
}

export function testStrings(iterations = 10000) {
    var buffer = "";
    for (var i = 0; i < iterations; i++) {
        buffer = buffer + "a";
        if (buffer.length > 32) {
            buffer = buffer.substring(buffer.length - 16);
        }
    }
    var ddd;
    for (i = 0; i < iterations; i++) {
        ddd = "Combined multiplication and subtraction" + "sss" ;
    }
    return buffer.length;
}





export function testIterator() {
    for (var key in node) {
        var prop = node[key];
    }
}


export function testClone() {
    var node2 = Object.clone(node);
}



export func testTypeOf(){
    var j = (typeof _date);
}


func testStringBuffer() {
    var buffer = StringBuffer('\n');
    var i = 0; 
    var j = 0;
    var n = 21;
    var r = Math.round(n / 2);
    var l = r;
    var k = 1;
    for (i = 0; i < n; i++)
    {
        for (j = 0; j <= l; j++)
        {
            buffer.append("*");
        }
        while (j < r)
        {
            buffer.append(" ");
            j++;
        }
        while (j < n)
        {
            buffer.append("*");
            j++;
        }
            buffer.appendLine();
        if (l == 0) k = -k;
        l -= k;
        r += k;
    }
    return buffer.toString();
}















func testSetVar() {
   var a = 15;
}


func testGetVar() {
    var a = 10;
    var b = a;
}


func testGetProperty() {
    var a = node.A;
}


func testSetProperty() {
    node.A = 15;
}



func testGetElement() {
   var temp = _array[5];
}


func testSetElement() {
   _array[5] = 5;
}


func testIncerment() {
    var temp = 100;
    temp++;
}

func testDecerment() {
    var temp = 100;
    temp--;
}


func testAddSI() {
    var temp = 100 + "abc";
}

func testAddVar() {
    var temp = 100 + 25;
}

func testSubVar() {
    var temp = 100 - 25;
}

func testMulVar() {
    var temp = 100 * 25;
}

func testDivVar() {
    var temp = 100 / 25;
}

func testAndVar() {
    var temp = 100 && 200;
}

func testOrVar() {
    var temp = 100 || 100;
}

func testBitXor() {
    var temp = 384 ^ 128;
}

func testBitOr() {
    var temp = 256 | 64;
}


func testBitNot() {
    var temp = ~15;
}


func testNegate() {
    var temp = -15;
}






func testNot() {
    var temp = !true;
}


func testEqual() {
    var temp = 100 == 100;
}

func testNotEqual() {
    var temp = 100 == 100;
}

func testGreaterThan() {
    var temp = 100 > 100;
}

func testGreaterEqual() {
    var temp = 100 >= 100;
}

func testLessThan() {
    var temp = 100 < 100;
}

func testLessEqual() {
    var temp = 100 <= 100;
}

func testIfTrue() {
    if(true) { 
    
    }
}
func testIfFalse() {
    if(false) { 
        
    }
}

func testMod() {
    var temp = 256 % 255;
}


 func testDraw() {
        var buffer = new StringBuffer('\n');
        var i = 0;
        var j = 0;
        var n = 21;
        var r = Math.round(n / 2);
        var l = r;
        var k = 1;
        for (i = 0; i < n; i++)
        {
            for (j = 0; j <= l; j++)
            {
                buffer.append("*");
            }
            while (j < r)
            {
                buffer.append(" ");
                j++;
            }
            while (j < n)
            {
                buffer.append("*");
                j++;
            }
            buffer.appendLine();
            if (l == 0) k = -k;
            l -= k;
            r += k;
        }
        buffer.stringAndRelease();
    }