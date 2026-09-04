@module(MD5_LIB);

/**
*
*  MD5 (Message-Digest Algorithm)
*  http://www.webtoolkit.info/
*
**/

function throwMethod() {
	console.log("Start testError");
	var ax = xxxx.c(1);
	console.log("End testError");
}

native function RotateLeft(Number lValue, Number iShiftBits) Number {
	return(lValue << iShiftBits) | (lValue >>> (32 - iShiftBits));
}

native function AddUnsigned(Number lX, Number lY) Number {
	// Convert the exact sum back to a signed 32-bit word. This is addition
	// modulo 2^32 without the branch-heavy sign-bit reconstruction.
	return (lX + lY) | 0;
}

native function F(Number x, Number y, Number z) Number { return(x & y) | ((~x) & z); }
native function G(Number x, Number y, Number z) Number { return(x & z) | (y & (~z)); }
native function H(Number x, Number y, Number z) Number { return(x ^ y ^ z); }
native function I(Number x, Number y, Number z) Number { return(y ^ (x | (~z))); }

native function FF(Number a, Number b, Number c, Number d, Number x, Number s, Number ac) Number {
	a = AddUnsigned(a, AddUnsigned(AddUnsigned(F(b, c, d), x), ac));
	return AddUnsigned(RotateLeft(a, s), b);
};

native function GG(Number a, Number b, Number c, Number d, Number x, Number s, Number ac) Number {
	a = AddUnsigned(a, AddUnsigned(AddUnsigned(G(b, c, d), x), ac));
	return AddUnsigned(RotateLeft(a, s), b);
};

native function HH(Number a, Number b, Number c, Number d, Number x, Number s, Number ac) Number {
	a = AddUnsigned(a, AddUnsigned(AddUnsigned(H(b, c, d), x), ac));
	return AddUnsigned(RotateLeft(a, s), b);
};

native function II(Number a, Number b, Number c, Number d, Number x, Number s, Number ac) Number {
	a = AddUnsigned(a, AddUnsigned(AddUnsigned(I(b, c, d), x), ac));
	return AddUnsigned(RotateLeft(a, s), b);
};

native function WordToHex(Number lValue) String {
	var WordToHexValue = '';
	var WordToHexValue_temp = '';
	var lByte;
	var lCount;
	for (lCount = 0; lCount <= 3; lCount++) {
		lByte = (lValue >> (lCount * 8)) & 255;
		WordToHexValue_temp = "0" + lByte.toString(16);
		WordToHexValue += (WordToHexValue_temp.substring(WordToHexValue_temp.length - 2, 2));
	}
	return WordToHexValue;
};

export native function MD5(String input) String {

	var a = 0x67452301;
	var b = 0xEFCDAB89;
	var c = 0x98BADCFE;
	var d = 0x10325476;
	var S11 = 7; var S12 = 12; var S13 = 17; var S14 = 22;
	var S21 = 5; var S22 = 9; var S23 = 14; var S24 = 20;
	var S31 = 4; var S32 = 11; var S33 = 16; var S34 = 23;
	var S41 = 6; var S42 = 10; var S43 = 15; var S44 = 21;

	// Count UTF-8 bytes without allocating an intermediate encoded string.
	// CRLF is normalized to LF to preserve the previous implementation.
	var sourceLength = input.length;
	var byteLength = 0i;
	for (var n = 0; n < sourceLength; n++) {
		var code = input.charCodeAt(n);
		if (code == 13 && n + 1 < sourceLength && input.charCodeAt(n + 1) == 10) {
			n++;
			code = 10;
		}
		if (code < 128) {
			byteLength++;
		} else if (code < 2048) {
			byteLength += 2;
		} else {
			byteLength += 3;
		}
	}

	var paddedLength = byteLength + 8;
	var blockCount = (paddedLength - (paddedLength % 64)) / 64 + 1;
	var wordCount = blockCount * 16;
	var x = new Int32Array(wordCount);
	var wordIndex = 0;
	var byteShift = 0;

	// Encode directly into the packed 32-bit message buffer.
	for (var p = 0; p < sourceLength; p++) {
		var value = input.charCodeAt(p);
		if (value == 13 && p + 1 < sourceLength && input.charCodeAt(p + 1) == 10) {
			p++;
			value = 10;
		}

		var first;
		var second = -1;
		var third = -1;
		if (value < 128) {
			first = value;
		} else if (value < 2048) {
			first = (value >> 6) | 192;
			second = (value & 63) | 128;
		} else {
			first = (value >> 12) | 224;
			second = ((value >> 6) & 63) | 128;
			third = (value & 63) | 128;
		}

		x[wordIndex] = x[wordIndex] | (first << byteShift);
		byteShift += 8;
		if (byteShift == 32) {
			byteShift = 0;
			wordIndex++;
		}

		if (second >= 0) {
			x[wordIndex] = x[wordIndex] | (second << byteShift);
			byteShift += 8;
			if (byteShift == 32) {
				byteShift = 0;
				wordIndex++;
			}
		}
		if (third >= 0) {
			x[wordIndex] = x[wordIndex] | (third << byteShift);
			byteShift += 8;
			if (byteShift == 32) {
				byteShift = 0;
				wordIndex++;
			}
		}
	}

	x[wordIndex] = x[wordIndex] | (0x80 << byteShift);
	x[wordCount - 2] = byteLength << 3;
	x[wordCount - 1] = byteLength >> 29;

	var xLen = x.length;
	for (var k = 0; k < xLen; k += 16) {
		var AA = a;
		var BB = b;
		var CC = c;
		var DD = d;

		a = FF(a, b, c, d, x[k + 0], S11, 0xD76AA478);
		d = FF(d, a, b, c, x[k + 1], S12, 0xE8C7B756);
		c = FF(c, d, a, b, x[k + 2], S13, 0x242070DB);
		b = FF(b, c, d, a, x[k + 3], S14, 0xC1BDCEEE);

		a = FF(a, b, c, d, x[k + 4], S11, 0xF57C0FAF);
		d = FF(d, a, b, c, x[k + 5], S12, 0x4787C62A);
		c = FF(c, d, a, b, x[k + 6], S13, 0xA8304613);
		b = FF(b, c, d, a, x[k + 7], S14, 0xFD469501);

		a = FF(a, b, c, d, x[k + 8], S11, 0x698098D8);
		d = FF(d, a, b, c, x[k + 9], S12, 0x8B44F7AF);
		c = FF(c, d, a, b, x[k + 10], S13, 0xFFFF5BB1);
		b = FF(b, c, d, a, x[k + 11], S14, 0x895CD7BE);

		a = FF(a, b, c, d, x[k + 12], S11, 0x6B901122);
		d = FF(d, a, b, c, x[k + 13], S12, 0xFD987193);
		c = FF(c, d, a, b, x[k + 14], S13, 0xA679438E);
		b = FF(b, c, d, a, x[k + 15], S14, 0x49B40821);

		a = GG(a, b, c, d, x[k + 1], S21, 0xF61E2562);
		d = GG(d, a, b, c, x[k + 6], S22, 0xC040B340);
		c = GG(c, d, a, b, x[k + 11], S23, 0x265E5A51);
		b = GG(b, c, d, a, x[k + 0], S24, 0xE9B6C7AA);

		a = GG(a, b, c, d, x[k + 5], S21, 0xD62F105D);
		d = GG(d, a, b, c, x[k + 10], S22, 0x2441453);
		c = GG(c, d, a, b, x[k + 15], S23, 0xD8A1E681);
		b = GG(b, c, d, a, x[k + 4], S24, 0xE7D3FBC8);

		a = GG(a, b, c, d, x[k + 9], S21, 0x21E1CDE6);
		d = GG(d, a, b, c, x[k + 14], S22, 0xC33707D6);
		c = GG(c, d, a, b, x[k + 3], S23, 0xF4D50D87);
		b = GG(b, c, d, a, x[k + 8], S24, 0x455A14ED);

		a = GG(a, b, c, d, x[k + 13], S21, 0xA9E3E905);
		d = GG(d, a, b, c, x[k + 2], S22, 0xFCEFA3F8);
		c = GG(c, d, a, b, x[k + 7], S23, 0x676F02D9);
		b = GG(b, c, d, a, x[k + 12], S24, 0x8D2A4C8A);

		a = HH(a, b, c, d, x[k + 5], S31, 0xFFFA3942);
		d = HH(d, a, b, c, x[k + 8], S32, 0x8771F681);
		c = HH(c, d, a, b, x[k + 11], S33, 0x6D9D6122);
		b = HH(b, c, d, a, x[k + 14], S34, 0xFDE5380C);

		a = HH(a, b, c, d, x[k + 1], S31, 0xA4BEEA44);
		d = HH(d, a, b, c, x[k + 4], S32, 0x4BDECFA9);
		c = HH(c, d, a, b, x[k + 7], S33, 0xF6BB4B60);
		b = HH(b, c, d, a, x[k + 10], S34, 0xBEBFBC70);

		a = HH(a, b, c, d, x[k + 13], S31, 0x289B7EC6);
		d = HH(d, a, b, c, x[k + 0], S32, 0xEAA127FA);
		c = HH(c, d, a, b, x[k + 3], S33, 0xD4EF3085);
		b = HH(b, c, d, a, x[k + 6], S34, 0x4881D05);

		a = HH(a, b, c, d, x[k + 9], S31, 0xD9D4D039);
		d = HH(d, a, b, c, x[k + 12], S32, 0xE6DB99E5);
		c = HH(c, d, a, b, x[k + 15], S33, 0x1FA27CF8);
		b = HH(b, c, d, a, x[k + 2], S34, 0xC4AC5665);

		a = II(a, b, c, d, x[k + 0], S41, 0xF4292244);
		d = II(d, a, b, c, x[k + 7], S42, 0x432AFF97);
		c = II(c, d, a, b, x[k + 14], S43, 0xAB9423A7);
		b = II(b, c, d, a, x[k + 5], S44, 0xFC93A039);

		a = II(a, b, c, d, x[k + 12], S41, 0x655B59C3);
		d = II(d, a, b, c, x[k + 3], S42, 0x8F0CCC92);
		c = II(c, d, a, b, x[k + 10], S43, 0xFFEFF47D);
		b = II(b, c, d, a, x[k + 1], S44, 0x85845DD1);

		a = II(a, b, c, d, x[k + 8], S41, 0x6FA87E4F);
		d = II(d, a, b, c, x[k + 15], S42, 0xFE2CE6E0);
		c = II(c, d, a, b, x[k + 6], S43, 0xA3014314);
		b = II(b, c, d, a, x[k + 13], S44, 0x4E0811A1);

		a = II(a, b, c, d, x[k + 4], S41, 0xF7537E82);
		d = II(d, a, b, c, x[k + 11], S42, 0xBD3AF235);
		c = II(c, d, a, b, x[k + 2], S43, 0x2AD7D2BB);
		b = II(b, c, d, a, x[k + 9], S44, 0xEB86D391);

		a = AddUnsigned(a, AA);
		b = AddUnsigned(b, BB);
		c = AddUnsigned(c, CC);
		d = AddUnsigned(d, DD);
	}

	var temp = WordToHex(a) + WordToHex(b) + WordToHex(c) + WordToHex(d);
	return temp.toLowerCase();
}
