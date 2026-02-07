import x from '../tests/l123';

func main() {
    var count = 0;
    func inc() {
        count = count + 1;
        return count;
    }

    console.log("Count 1:", inc());
    console.log("Count 2:", inc());

    var title = "Hello";
    func makeGreeter(prefix) {
        return (name) => {
            return prefix + " " + name + " " + title;
        };
    }

    var greeter = makeGreeter("Greetings");
    console.log(greeter("Aurora"));

    title = "Hi";
    console.log(greeter("World"));
}
