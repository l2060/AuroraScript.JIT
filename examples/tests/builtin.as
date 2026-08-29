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


	var v = Stats.PI;
	var h = Stats.mean(c,d);
	var h2 = Stats.mean(c,b);



	var vec = new Vec2(1000,2000);
	var vx = vec.x;
	vec.x++;
	vec.x+=65;
	vec.x =10086;
	return vec;
}


func testNativeScript(){
	var n = 1000000;
	n++;
	n--;
	n+= 15;
	n-=15;
	++n;
	--n;
	var k = n % 5;
	var v = n << 5;
	var m = n >> 5;
}