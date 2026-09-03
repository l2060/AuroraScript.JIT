context bag;
context user as UserState;

export native func player() UserState {
	return user;
}

export func bagName() {
	return bag.name;
}
