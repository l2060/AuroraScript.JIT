
/**
* 上下文帮助类，用于封装拿取指定类型的UserState
*/
context bag;
context user as UserState;

// 原生方法拿取 UserState 上下文
export native func player() UserState {
	return user;
}

export func bagName() {
	return bag.name;
}
