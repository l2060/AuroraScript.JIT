

export func main(){


}

func test(){


}



native func sub(Number a, Number b) Number{
	return a - b;
}

export func getFunc(){
	return sub;
}

export func sum(Number a, Number b) Number{
	return sub(a,b);
}