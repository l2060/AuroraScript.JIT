@module(BUILTIN);

import fs from 'fs';
import http from 'http';
import constant from 'constant';


func testFileSystem() {
	var text = (fs.readText('config.tdoc'));
	console.log(text);
}

func testHttpGet() {

	http.getAsync("https://www.baidu.com", {}, (error, res) => {
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
	var h = Stats.mean(c, d);
	var h2 = Stats.mean(c, b);

	var vh = Vec2.from(1234, 1234);


	var vec = new Vec2(1000, 2000);
	var vx = vec.x;
	vec.x++;
	vec.x += 65L;
	vec.x = 10086L;

	var type = Vec2;
	console.log(typeof type);


	return vec;
}


func testNativeScript() {
	var t = constant.COMPLEX + 666;

	var n = 1000000;
	n++;
	n--;
	n += 15;
	n -= 15;
	++n;
	--n;
	var k = n % 5;
	var v = n << 5;
	var m = n >> 5;
}