// AuroraScript Core Library Declarations
// This file provides definitions for built-in objects and functions for the VS Code extension.




// ==========================================================================================
// =================================== Console ==============================================
// ==========================================================================================

/**
* Standard console output Object.
* Provides basic logging and timing facilities.
*/
export declare console;

/**
* Prints to stdout with a newline.
* @param args The items to be logged.
*/
export declare console.log(...args: Object[]): void;

/**
* Prints to stderr with a newline.
* @param args The items to be logged.
*/
export declare console.error(...args: Object[]): void;

/**
* Starts a timer you can use to track how long an operation takes.
* @param label The label to identify the timer.
*/
export declare console.time(label: String): void;

/**
* Stops a timer that was previously started by calling `console.time()`.
* @param label The label of the timer to stop.
*/
export declare console.timeEnd(label: String): void;


// ==========================================================================================
// =================================== HotPatch ==============================================
// ==========================================================================================

/**
* HotPatch Object.
* Provides basic hot patch facilities.
*/
export declare HotPatch;

/**
* Apply replacement patches to the module.
* @param modulepath The path of the file to be fixed.
* @param script The script of the module to be fixed.
* @param ignoreDepends .If the 'import' module already exists, ignore the compilation, default: false
*/
export declare HotPatch.replace(modulepath: String, script: String, ignoreDepends: Boolean | null): void;

/**
* Apply incremental patches to the module.
* @param modulepath The path of the file to be fixed.
* @param script The script of the module to be fixed.
* @param ignoreDepends .If the 'import' module already exists, ignore the compilation, default: false
*/
export declare HotPatch.incremental(modulepath: String, script: String, ignoreDepends: Boolean | null): void;


// ==========================================================================================
// ==================================== Math ================================================
// ==========================================================================================

/**
* Basic mathematics functionality and constants.
*/
export declare Math;

/**
* The ratio of the circumference of a circle to its diameter, approximately 3.14159.
*/
export declare Math.PI: Number;

/**
* The base of natural logarithms, e, approximately 2.718.
*/
export declare Math.E: Number;

/**
* The ratio of the circumference of a circle to its radius, approximately 6.28318.
*/
export declare Math.Tau: Number;

/**
* Conversion factor from radians to degrees.
*/
export declare Math.DEG_PER_RAD: Number;

/**
* Returns the absolute value of a Number.
* @param x A numeric expression.
* @returns The absolute value of x.
*/
export declare Math.abs(x: Number): Number;

/**
* Returns the largest of zero or more numbers.
* @param values Numeric expressions to be compared.
* @returns The largest of the given numbers.
*/
export declare Math.max(...values: Number[]): Number;

/**
* Returns the smallest of zero or more numbers.
* @param values Numeric expressions to be compared.
* @returns The smallest of the given numbers.
*/
export declare Math.min(...values: Number[]): Number;

/**
* Returns a pseudo-random Number between 0 and 1.
* @returns A pseudo-random Number.
*/
export declare Math.random(): Number;

/**
* Returns the natural logarithm (base e) of a Number.
* @param x A numeric expression.
* @returns The natural logarithm of x.
*/
export declare Math.log(x: Number): Number;

/**
* Returns base to the exponent power.
* @param base The base value of the expression.
* @param exponent The exponent value of the expression.
* @returns The result of raising base to the exponent power.
*/
export declare Math.pow(base: Number, exponent: Number): Number;

/**
* Returns e^x, where x is the argument, and e is Euler's constant.
* @param x A numeric expression.
* @returns The result of e raised to the power of x.
*/
export declare Math.exp(x: Number): Number;

/**
* Returns the cosine of a Number.
* @param x A numeric expression that contains an angle measured in radians.
* @returns The cosine of x.
*/
export declare Math.cos(x: Number): Number;

/**
* Returns the sine of a Number.
* @param x A numeric expression that contains an angle measured in radians.
* @returns The sine of x.
*/
export declare Math.sin(x: Number): Number;

/**
* Returns the tangent of a Number.
* @param x A numeric expression that contains an angle measured in radians.
* @returns The tangent of x.
*/
export declare Math.tan(x: Number): Number;

/**
* Returns the arccosine of a Number.
* @param x A numeric expression.
* @returns The arccosine of x, in radians.
*/
export declare Math.acos(x: Number): Number;

/**
* Returns the arcsine of a Number.
* @param x A numeric expression.
* @returns The arcsine of x, in radians.
*/
export declare Math.asin(x: Number): Number;

/**
* Returns the arctangent of a Number.
* @param x A numeric expression.
* @returns The arctangent of x, in radians.
*/
export declare Math.atan(x: Number): Number;

/**
* Returns the largest integer less than or equal to a Number.
* @param x A numeric expression.
* @returns The greatest integer less than or equal to x.
*/
export declare Math.floor(x: Number): Number;

/**
* Returns the value of a Number rounded to the nearest integer.
* @param x A numeric expression.
* @returns Rounded value of x.
*/
export declare Math.round(x: Number): Number;


// ==========================================================================================
// ===================================== JSON ===============================================
// ==========================================================================================



/**
* An intrinsic Object that provides functions to convert to and from the Object Notation (JSON) format.
*/
export declare JSON;

/**
* Converts a Object Notation (JSON) String into an Object.
* @param text A valid JSON string.
* @returns An Object parsed from the given JSON string.
*/
export declare JSON.parse(text: String): Object;

/**
* Converts a value to a Object Notation (JSON) String.
* @param value A value, usually an object or array, to be converted.
* @param indented Whether to indent the output string for readability.
* @returns A JSON string representation of the value.
*/
export declare JSON.stringify(value: Object, indented: Boolean | null): String;


// ==========================================================================================
// ==================================== StringBuffer ========================================
// ==========================================================================================


/**
* A mutable String buffer class for efficient string manipulation.
* Comparable to `StringBuilder` in C#.
*/
export declare StringBuffer;

/**
* Creates a new StringBuffer instance.
* @param initialValue Optional initial string content.
*/
export declare StringBuffer(initialValue: String | null): StringBuffer;

/**
* Returns the String content currently stored in the buffer.
* @returns The complete string content.
*/
export declare StringBuffer.prototype.toString(): String;

/**
* Appends the String representation of items to the end of the buffer.
* @param items The items to append.
*/
export declare StringBuffer.prototype.append(...items: Object[]): void;

/**
* Inserts a String at the specified index.
* @param index The zero-based index at which to insert.
* @param str The string to insert.
*/
export declare StringBuffer.prototype.insert(index: Number, str: String): void;

/**
* Appends items followed by a newline character.
* @param items The items to append.
*/
export declare StringBuffer.prototype.appendLine(...items: Object[]): void;

/**
* Clears the buffer content, resetting it to an empty state.
*/
export declare StringBuffer.prototype.clear(): void;


// ==========================================================================================
// ===================================== Object ===============================================
// ==========================================================================================


/**
* The fundamental Object constructor.
*/
export declare Object;

/**
* Creates a new Object or converts a value to an Object.
* @param value The value to wrap or the object to initialize.
*/
export declare Object(value: Object | null): Object;

/**
* Strict equality comparison (identity check).
* @returns True if both objects identify as the same instance.
*/
export declare Object.equal$(a: Object, b: Object): Boolean;

/**
* Value equality comparison.
* @returns True if both objects have equivalent values.
*/
export declare Object.equal(a: Object, b: Object): Boolean;

/**
* Deep recursive equality comparison.
* @returns True if both objects are deeply equal.
*/
export declare Object.deepEqual(a: Object, b: Object): Boolean;

/**
* Copy the values of all enumerable own properties from source objects to a target object.
* @param target The target object to copy to.
* @param sources One or more source objects to copy from.
* @returns The modified target object.
*/
export declare Object.assign(target: Object, ...sources: Object[]): Object;

/**
* Returns the names of the enumerable String properties of an Object.
* @param o The object to extract keys from.
* @returns An array of property names.
*/
export declare Object.keys(o: Object): String[];

/**
* Creates a shallow copy of the Object.
* @param o The object to clone.
* @returns A new object with the same top-level properties.
*/
export declare Object.clone(o: Object): Object;

/**
* Creates a deep recursive copy of the Object.
* @param o The object to clone.
* @returns A new object with all nested properties cloned.
*/
export declare Object.deepClone(o: Object): Object;

/**
* Returns a String representation of the Object.
* @returns A string representing the object.
*/
export declare Object.prototype.toString(): String;


// ==========================================================================================
// ===================================== Boolean ==============================================
// ==========================================================================================


/**
* The Boolean constructor.
*/
export declare Boolean;

/**
* Creates a new Boolean object.
* @param value The initial value.
*/
export declare Boolean(value: Object | null): Boolean;

/**
* The primitive true value.
*/
export declare Boolean.true: Boolean;

/**
* The primitive false value.
*/
export declare Boolean.false: Boolean;

/**
* Returns the primitive Boolean value of a numeric, string, or boolean input.
* @param val The value to convert.
* @returns The resulting Boolean value.
*/
export declare Boolean.valueOf(val: Number | String | Boolean): Boolean;

/**
* Returns a String representation of the Boolean Object ("true" or "false").
* @returns The string representation.
*/
export declare Boolean.prototype.toString(): String;


// ==========================================================================================
// ===================================== String ===============================================
// ==========================================================================================


/**
* The String constructor and global methods.
*/
export declare String;

/**
* Creates a new String object.
* @param value The value to convert to a string.
*/
export declare String(value: Object | null): String;

/**
* Returns a String created from the specified sequence of UTF-16 code units.
* @param charCode A sequence of numbers that are UTF-16 code units.
* @returns The generated string.
*/
export declare String.fromCharCode(charCode: Number): String;

/**
* Returns the string representation of a value.
* @param codePoint The value to convert.
* @returns The string representation.
*/
export declare String.valueOf(codePoint: Object): String;

/**
* Compares two strings lexicographically.
* @returns 0 if equal, < 0 if a < b, > 0 if a > b.
*/
export declare String.compare(a: String, b: String): Number;


/**
* The length of the String in characters.
*/
export declare String.prototype.length: Number;

/**
* Returns true if the String contains the specified substring.
* @param str The substring to search for.
*/
export declare String.prototype.contains(str: String): Boolean;

/**
* Returns the position of the first occurrence of a substring.
* @param str The substring to search for.
* @returns The index of the first occurrence, or -1 if not found.
*/
export declare String.prototype.indexOf(str: String): Number;

/**
* Returns the last occurrence of a substring in the String.
* @param str The substring to search for.
* @returns The index of the last occurrence, or -1 if not found.
*/
export declare String.prototype.lastIndexOf(str: String): Number;

/**
* Returns true if the String starts with the specified prefix.
* @param str The prefix to check.
*/
export declare String.prototype.startsWith(str: String): Boolean;

/**
* Returns true if the String ends with the specified suffix.
* @param str The suffix to check.
*/
export declare String.prototype.endsWith(str: String): Boolean;

/**
* Returns the substring at the specified location within a String Object.
* @param start The starting index.
* @param end The ending index (exclusive). If null, extracts to the end.
* @returns The extracted substring.
*/
export declare String.prototype.substring(start: Number, end : Number | null): String;

/**
* Split a String into substrings using the specified separator.
* @param separator A string or regular expression used to split.
* @returns An array of strings.
*/
export declare String.prototype.split(separator: String | RegExp): String[];

/**
* Matches a String with a regular expression.
* @param matcher The regular expression or search string.
* @returns An array containing the results of the search, or null.
*/
export declare String.prototype.match(matcher: RegExp | String): Object[] | Object | null;

/**
* Matches a String with a regular expression, and returns an iterable of matches.
* @param regexp The regular expression.
* @returns An iterable object containing match results.
*/
export declare String.prototype.matchAll(regexp: RegExp): Object;

/**
* Replaces text in a String, using a regular expression or search String.
* @param searchValue The pattern to look for.
* @param replaceValue The string to replace with.
* @returns The resulting string.
*/
export declare String.prototype.replace(searchValue: String | RegExp, replaceValue: String): String;

/**
* Removes the leading and trailing white space and line terminator characters from a String.
* @returns The trimmed string.
*/
export declare String.prototype.trim(): String;

/**
* Removes the leading white space.
* @returns The trimmed string.
*/
export declare String.prototype.trimLeft(): String;

/**
* Removes the trailing white space.
* @returns The trimmed string.
*/
export declare String.prototype.trimRight(): String;

/**
* Extracts a section of a string and returns it as a new string.
* @param start The zero-based index at which to begin extraction.
* @param end The zero-based index before which to end extraction.
*/
export declare String.prototype.slice(start: Number, end: Number): String;

/**
* Returns the string itself.
* @returns The primitive string value.
*/
export declare String.prototype.toString(): String;

/**
* Returns the Unicode value of the character at the specified index.
* @param char The index of the character.
* @returns The numeric Unicode value.
*/
export declare String.prototype.charCodeAt(char: Number): Number;

/**
* Converts all the alphabetic characters in a String to lowercase.
* @returns The lowercase string.
*/
export declare String.prototype.toLowerCase(): String;

/**
* Converts all the alphabetic characters in a String to uppercase.
* @returns The uppercase string.
*/
export declare String.prototype.toUpperCase(): String;



// ==========================================================================================
// ===================================== Number =============================================
// ==========================================================================================


/**
* The Number constructor.
*/
export declare Number;

/**
* Creates a new Number object.
* @param value The numeric value.
*/
export declare Number(value: Number | String): Number;

/**
* The largest positive representable Number.
*/
export declare Number.MAX_VALUE: Number;

/**
* The smallest positive representable Number (closest to zero).
*/
export declare Number.MIN_VALUE: Number;

/**
* The maximum safe integer in AuroraScript.
*/
export declare Number.MAX_SAFE_INTEGER: Number;

/**
* The minimum safe integer in AuroraScript.
*/
export declare Number.MIN_SAFE_INTEGER: Number;


/**
* A constant representing "Not-a-Number".
*/
export declare Number.NaN: Number;

/**
* Positive infinity value.
*/
export declare Number.POSITIVE_INFINITY: Number;

/**
* Negative infinity value.
*/
export declare Number.NEGATIVE_INFINITY: Number;

/**
* Returns true if the value is NaN.
* @param number The value to test.
*/
export declare Number.isNaN(number: Number): Boolean;

/**
* Returns true if the value is an integer.
* @param number The value to test.
*/
export declare Number.isInteger(number: Number): Boolean;

/**
* Returns true if the value is infinity (positive or negative).
* @param number The value to test.
*/
export declare Number.isInfinity(number: Number): Boolean;

/**
* Parses a String argument and returns a floating point Number.
* @param string The string to parse.
*/
export declare Number.parseFloat(string: String): Number;

/**
* Parses a String argument and returns an integer of the specified radix.
* @param string The string to parse.
* @param radix An integer between 2 and 36 that represents the radix.
*/
export declare Number.parseInt(string: String, radix : Number | null): Number;

/**
* Returns a String representing the specified Number Object.
* @param radix An optional radix to use in the conversion.
* @returns The string representation.
*/
export declare Number.prototype.toString(radix : Number | null): String;

/**
* Returns a String representing the Number in fixed-point notation.
* @param fractionDigits Number of digits after the decimal point.
* @returns The formatted string.
*/
export declare Number.prototype.toFixed(fractionDigits : Number | null): String;



// ==========================================================================================
// ===================================== Array ==============================================
// ==========================================================================================



/**
* The Array constructor.
*/
export declare Array;

/**
* Creates a new Array with the specified initial capacity.
* @param capacity The initial size of the array.
*/
export declare Array(capacity: Number): Array;

/**
* Creates an array from an array-like Object.
* @param iterable An array-like or iterable object.
* @param iterable item callback.
* @returns A new array.
*/
export declare Array.from(iterable: Object, callback: Function | null): Object[];

/**
* Returns true if the variable is an Array.
* @param arg The value to check.
*/
export declare Array.isArray(arg: Object): Boolean;

/**
* Creates a new Array instance with a variable Number of arguments.
* @param items The elements to populate the array with.
* @returns A new array.
*/
export declare Array.of(...items: Object[]): Object[];

/**
* The number of elements in the array.
*/
export declare Array.prototype.length: Number;

/**
* Appends new elements to the end of an array, and returns the new length.
* @param items New elements of the array.
* @returns The new length of the array.
*/
export declare Array.prototype.push(...items: Object[]): Number;

/**
* Removes the last element from an array and returns it.
* @returns The removed element, or undefined if the array is empty.
*/
export declare Array.prototype.pop(): Object;

/**
* Sorts an array in place.
* @param compareFn Function used to determine the order of the elements.
* @returns The sorted array.
*/
export declare Array.prototype.sort(compareFn : Function | null): Array;

/**
* Adds all the elements of an array into a String, separated by the specified separator.
* @param separator A string used to separate one element of the array from the next.
* @returns The joined string.
*/
export declare Array.prototype.join(separator : String | null): String;

/**
* Returns a section of an array.
* @param start The beginning of the specified portion of the array.
* @param end The end of the specified portion of the array.
* @returns A new array containing the sliced section.
*/
export declare Array.prototype.slice(start: Number | null , end : Number | null): Array;

/**
* Reverses the elements in an array in place.
* @returns The reversed array.
*/
export declare Array.prototype.reverse(): Array;

/**
* Inserts new elements at the start of an array.
* @param items Elements to insert at the start of the array.
* @returns The new length of the array.
*/
export declare Array.prototype.unshift(...items: Object[]): Number;

/**
* Removes the first element from an array and returns it.
* @returns The removed element.
*/
export declare Array.prototype.shift(): Object;

/**
* Combines two or more arrays.
* @param items Additional items or arrays to concatenate.
* @returns A new combined array.
*/
export declare Array.prototype.concat(...items: Object[]): Array;

/**
* Returns a String representing the specified array and its elements.
* @returns The string representation.
*/
export declare Array.prototype.toString(): String;






// ==========================================================================================
// ==================================== HashMap =============================================
// ==========================================================================================
/**
* The HashMap constructor.
*/
export declare HashMap;

/**
* Creates a new HashMap instance.
* @param value Optional initial key-value pairs array in [[Key, Value]] format to initialize the HashMap
* @returns A new HashMap instance (Note: The original Date return type may be a typo, adjusted to match the constructor semantics)
*/
export declare HashMap(): HashMap;

/**
* Checks whether the specified key exists in the HashMap
* @param key The key to check (can be any Object type)
* @returns Boolean value indicating if the key exists (true if exists, false otherwise)
*/
export declare HashMap.prototype.has(key: Object): Boolean;

/**
* Adds or updates a key-value pair in the HashMap
* @param key The key to set (can be any Object type)
* @param value The value to associate with the key (can be any Object type)
* @returns No return value
*/
export declare HashMap.prototype.set(key: Object, value: Object): void;

/**
* Retrieves the value associated with the specified key from the HashMap
* @param key The key to retrieve the value for (can be any Object type)
* @returns The value associated with the key, or undefined if the key does not exist
*/
export declare HashMap.prototype.get(key: Object): Object;

/**
* Retrieves the value for the specified key, or inserts a new value if the key does not exist
* @param key The key to retrieve or insert (can be any Object type)
* @param value The value to insert if the key does not exist, or a function that returns the value to insert
* @returns The existing value if the key exists, or the newly inserted value if the key does not exist
*/
export declare HashMap.prototype.getOrInsert(key: Object, value: Object | Function): Object;

/**
* Removes all key-value pairs from the HashMap
* @returns No return value
*/
export declare HashMap.prototype.clear(): void;

/**
* Removes the specified key and its associated value from the HashMap
* @param key The key to delete (can be any Object type)
* @returns No return value
*/
export declare HashMap.prototype.delete(key: Object): void;

/**
* Gets an array containing all keys in the HashMap
* @returns Array of all keys in the HashMap (each element is an Object type)
*/
export declare HashMap.prototype.keys: Array<Object>;

/**
* Gets an array containing all values in the HashMap
* @returns Array of all values in the HashMap (each element is an Object type)
*/
export declare HashMap.prototype.values: Array<Object>;

/**
* Gets the number of key-value pairs in the HashMap
* @returns Numeric value representing the count of key-value pairs
*/
export declare HashMap.prototype.size: Number;



// ==========================================================================================
// ===================================== Date ===============================================
// ==========================================================================================



/**
* The Date constructor.
*/
export declare Date;

/**
* Creates a new Date instance.
* @param value A timestamp or date string to initialize.
*/
export declare Date(value: Number | String): Date;

/**
* Returns a Date Object representing the current time.
*/
export declare Date.now(): Date;

/**
* Returns a Date Object representing the current UTC time.
*/
export declare Date.utcNow(): Date;

/**
* Parses a String representation of a date, and returns a Date Object.
* @param s A date string.
* @returns The parsed Date object.
*/
export declare Date.parse(s: String): Date;

/**
* Returns a String representation of the date.
* @param format An optional format string.
* @returns The formatted date string.
*/
export declare Date.prototype.toString(format: String | null): String;


// ==========================================================================================
// ===================================== Regex ==============================================
// ==========================================================================================




/**
* The Regex constructor for regular expression patterns.
*/
export declare Regex;

/**
* Creates a new Regex instance.
* @param pattern The pattern string or another Regex object.
* @param flags Optional flags for the regular expression.
*/
export declare Regex(pattern: String | Regex, flags : String | null): Regex;

/**
* Returns true if the pattern exists in the searched string.
* @param string The string to search.
*/
export declare Regex.prototype.test(string: String): Boolean;




// ==========================================================================================
// ===================================== Proxy ==============================================
// ==========================================================================================


/**
* The Proxy Object enables you to create a proxy for another Object.
*/
export declare Proxy;

/**
* Creates a new Proxy instance.
* @param target The original object to be proxied.
* @param handler An object whose properties define the behavior of the proxy.
*/
export declare Proxy(target: Object, handler: Object): Proxy;



// ==========================================================================================
// ===================================== Global =============================================
// ==========================================================================================


/**
* The global Object of the current script domain.
*/
export declare global: Object;

/**
* Access to modules loaded within the current script domain.
*/
export declare global.modules: Object;

/**
* Context state passed by the user during script invocation.
* Usually contains persistent state information.
*/
export declare $state: Object;

/**
* Argument list passed to the current method call.
*/
export declare $args: Object[];

