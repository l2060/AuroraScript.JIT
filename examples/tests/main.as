@module(MAIN);

var _testCases = [];

func main() {
	var cases = _testCases as Array;
	var modules = global.modules;
	var moduleNames = Object.keys(modules);
	console.log('===============================================================');
	console.log('found modules: ', moduleNames.join(', '));
	console.log('===============================================================');
	for (var moduleName in moduleNames) {
		if (typeof modules[moduleName] == 'object' && modules[moduleName] != this) {
			var module = modules[moduleName];
			for (var propName in Object.keys(module)) {
				if (typeof module[propName] == 'function' && propName.startsWith('test')) {
					cases.push({ name: propName, method: module[propName] });
				}
			}
		}
	}
	console.log(cases);
	for (var _case in cases) {
		console.log("start Test Case", _case.name);
		_case.method();
	}
}