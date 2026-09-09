

export func arrayWork(Array array) void {
	for (var i = 0; i <= 10;i++) array.push(i,1,2);
	var len = array.length;
	for (var n = 0 ; n < len; n++) {
		array[n] = n;
		var item = array[n];
		console.log(item);
	}
	var d = Date.parse(12580);
}




export func testArray()  {
	var array = [1, 2, 3, 4];
	arrayWork(array);
}
