@module(BUILTIN);

import fs from 'fs';
import http from 'http';

func testFileSystem() {
	var text = (fs.readText('config.tdoc'));
	console.log(text);
}

func testHttpGet() {

	http.getAsync("https://www.baidu.com", null, (error, res) => {
			console.log(error, res.status);
		});
}

func testNative() {
	var a = "Hello";
	var b = 123;
	var c = 123.45;
	var d = "123.45";
	var e = {a: 1, b: 2};


	var v = Math.PI;
	var h = Math.pow(c,d);
	var h2 = Math.pow(c,b);
}