@module(TEST);
import util from './util';
export func run(value) {
    const total = value + 1;
    if (total > 0) {
        return Math.abs(-total);
    } else {
        return 0;
    }
}
