
func testStr2() {
	const a = "Hello";
	const h = "Hello";
	var c = a + " " + "Wrold";
	var d = "Hello" + "Wrold";
	console.log(a, c, d, h);
}






func testInput() {
	INPUT_NUMBER('��������', '����һ��0-99��ֵ', 'number', input_change);

	INPUT_NUMBER('��������', '����һ��0-99��ֵ', 'number', (value) => {
			GIVE("esd", value);
			console.log(`����ֵ=${ value }`);
		});
	console.log(testTextTemplate());
}

export func testTextTemplate() {
	return `[ ${ 0+10 } - ${ 1+10 } - ${ 2+10 } - ${ 3+10 } - ${ 4+10 } - ${ 5+10 } ]`;
}

export func testTextTemplate2(n) {
	var a = 123;
	{
		func a(b) {
			console.log(b);
		}
		a("--");
	}
	return `Template: ${n}`;
}


func throwTest() {
	throw new Error("test");
}




func testCatch() {
	try { throwTest(); } finally {}


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