@module(TIMER_LIB);

declare function debug(msg);

declare function CREATE_TIMER(timer);
declare function START_TIMER(timer);
declare function STOP_TIMER(timer);
declare function DELETE_TIMER(timer);



export var resetCount = 0;
var timeCount = 0;
export var timers = [0,, 1, 2, 3, 4, 5];

// timers[10] = 10;
// var sdss = timers[10];

function time_proc(timer, arrays, clrdata) {
	for (var item in timers) {
		timeCount++;
	}
	console.log(timer, arrays, clrdata, $state);

}

function testCallback() {
	CREATE_TIMER(time_proc);
}



export function createTimer(callback, interval = 521) {

	var timer = {
		timeId: timeCount++,
		callback,
		interval,
		cancel,
		numbers: [1, 2, 3, 4],
		strings: ['a', 'b', 'c', 'd'],
		datas: { v1: 123, v2: 'hello', v3: true },
		count: 50,
		reset: () => {
			timer.count = 0;
			log("reset");
		}
	};
	function log(text) {
		console.log("Timer:" + timer.timeId + " [" + text /*.toUpperCase()*/ + "]");
	}
	function cancel() {
		log("canceled");
		timer.cancel = null;
		timeCount--;
		timer.timeId = null;
		timer.callback = null;
		timer.interval = null;
		timer.reset = null;
		timer.count = null;
		timer.abc = "abc";
		return true;
	}

	log(
		|> 111
		|> 1. 这是一个特殊的字符串模板
		|> 2. 支持多行文本\n 123
		|> 3. 它会让代码看起来更舒服
		|> 4. <Buy/@Buy> <Close/@Close>
		|> 5. <Buys/@Buys:input-number>
	);
	resetCount++;
	timeCount++;
	timers.push(timer);
	return Object(timer);
}

func Buy(count = 1) {

}

func Close() {

}