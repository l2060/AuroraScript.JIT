
func testStr2() {
    const a = "Hello";
    const h = "Hello";
    var c = a + " " + "Wrold";
    var d = "Hello" + "Wrold";
    console.log(a, c, d, h);
}






function testInput() {
    INPUT_NUMBER('��������', '����һ��0-99��ֵ', 'number', input_change);

    INPUT_NUMBER('��������', '����һ��0-99��ֵ', 'number', (value) => {
        GIVE("esd", value);
        console.log(`����ֵ=${ value }`);
    });

}



func testInlude() {
    var obj = { a: 1, b: 2, c: '' };
    console.log('a' in obj);
    console.log('e' in 'hello');
    if("A" in ["A", "B"] && ) {
        console.log("111");
    }
}



func throwTest() {
    throw new Error("test");
}




func testCatch() {

    try { throwTest(); }

    try
    {
        throwTest();
    }
    catch (ex)
    {
        console.log(ex);
    }
    finally
    {
        console.log("finally");
    }
}