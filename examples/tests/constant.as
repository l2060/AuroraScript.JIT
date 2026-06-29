

export enum EnumTest {
	Alpha = 1,
	Beta = 2,
	Gamma = 3
}

export const NUM = 3.141592678987654321;
export const STR = 'this is string';
export const BOOL = true;
export const BASE = 10;
export const COMPLEX = BASE * NUM + 5;
export const TAG = BASE + '_' + 1;
export const TEMPLATE = STR + BASE + '_' + TAG;


export func log() {
	console.log(...$args);
}


export declare func GIVE(item, count);

export declare func INPUT_NUMBER(title, label, type, callback);


