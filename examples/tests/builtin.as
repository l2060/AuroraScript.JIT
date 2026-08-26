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


	console.log(Stats.chat(a, b));
	console.log(Stats.chat(a, c));
	console.log(Stats.chat(a, d));
	console.log(Stats.chat(b, c));
	// console.log(Stats.chat(e, e));
}