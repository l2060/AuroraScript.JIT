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

