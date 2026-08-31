@global();

declare func debug(msg);
declare func CREATE_TIMER(timer);
declare func START_TIMER( timer) ;
declare func STOP_TIMER(timer) ;
declare func DELETE_TIMER(timer);

declare func GIVE(item, count);
declare func INPUT_NUMBER(title, label, type, callback);

// 当前APP版本，只读
declare const APP_VERSION;

// 在线数量，可 读/写
declare var  ONLINE_TOTAL;

declare type Stats {
    static const Number PI;
    static func mean(Number a, Number b) Number;
}

declare type Vec2 {
    constructor(Number x, Number y);
    Number x;
    Number y;
    func length() Number;
    static const Number DIMENSIONS;
    static func from(Number x, Number y) Vec2;
    static func length(Number x, Number y) Number;
}
