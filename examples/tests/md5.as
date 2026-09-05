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

native function RotateLeft(uint32 lValue, int32 iShiftBits) uint32 {
	return(lValue << iShiftBits) | (lValue >>> (32 - iShiftBits));
}

native function AddUnsigned(uint32 lX, uint32 lY) uint32 {
	return lX + lY;
}

native function F(uint32 x, uint32 y, uint32 z) uint32 { return(x & y) | ((~x) & z); }
native function G(uint32 x, uint32 y, uint32 z) uint32 { return(x & z) | (y & (~z)); }
native function H(uint32 x, uint32 y, uint32 z) uint32 { return(x ^ y ^ z); }
native function I(uint32 x, uint32 y, uint32 z) uint32 { return(y ^ (x | (~z))); }

native function FF(uint32 a, uint32 b, uint32 c, uint32 d, uint32 x, int32 s, uint32 ac) uint32 {
	a = AddUnsigned(a, AddUnsigned(AddUnsigned(F(b, c, d), x), ac));
	return AddUnsigned(RotateLeft(a, s), b);
};

native function GG(uint32 a, uint32 b, uint32 c, uint32 d, uint32 x, int32 s, uint32 ac) uint32 {
	a = AddUnsigned(a, AddUnsigned(AddUnsigned(G(b, c, d), x), ac));
	return AddUnsigned(RotateLeft(a, s), b);
};

native function HH(uint32 a, uint32 b, uint32 c, uint32 d, uint32 x, int32 s, uint32 ac) uint32 {
	a = AddUnsigned(a, AddUnsigned(AddUnsigned(H(b, c, d), x), ac));
	return AddUnsigned(RotateLeft(a, s), b);
};

native function II(uint32 a, uint32 b, uint32 c, uint32 d, uint32 x, int32 s, uint32 ac) uint32 {
	a = AddUnsigned(a, AddUnsigned(AddUnsigned(I(b, c, d), x), ac));
	return AddUnsigned(RotateLeft(a, s), b);
};

native function WordToHex(uint32 lValue) String {
	var WordToHexValue = '';
	var WordToHexValue_temp = '';
	var lByte = 0;
	var lCount = 0;
	for (lCount = 0; lCount <= 3; lCount++) {
		lByte = (lValue >> (lCount * 8)) & 255;
		WordToHexValue_temp = "0" + lByte.toString(16);
		WordToHexValue += (WordToHexValue_temp.substring(WordToHexValue_temp.length - 2, 2));
	}
	return WordToHexValue;
};

export native function MD5(String input) String {

	var a = 0x67452301u;
	var b = 0xEFCDAB89u;
	var c = 0x98BADCFEu;
	var d = 0x10325476u;
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
	var x = new UInt32Array(wordCount);
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

		a = FF(a, b, c, d, x[k + 0], S11, 0xD76AA478u);
		d = FF(d, a, b, c, x[k + 1], S12, 0xE8C7B756u);
		c = FF(c, d, a, b, x[k + 2], S13, 0x242070DBu);
		b = FF(b, c, d, a, x[k + 3], S14, 0xC1BDCEEEu);

		a = FF(a, b, c, d, x[k + 4], S11, 0xF57C0FAFu);
		d = FF(d, a, b, c, x[k + 5], S12, 0x4787C62Au);
		c = FF(c, d, a, b, x[k + 6], S13, 0xA8304613u);
		b = FF(b, c, d, a, x[k + 7], S14, 0xFD469501u);

		a = FF(a, b, c, d, x[k + 8], S11, 0x698098D8u);
		d = FF(d, a, b, c, x[k + 9], S12, 0x8B44F7AFu);
		c = FF(c, d, a, b, x[k + 10], S13, 0xFFFF5BB1u);
		b = FF(b, c, d, a, x[k + 11], S14, 0x895CD7BEu);

		a = FF(a, b, c, d, x[k + 12], S11, 0x6B901122u);
		d = FF(d, a, b, c, x[k + 13], S12, 0xFD987193u);
		c = FF(c, d, a, b, x[k + 14], S13, 0xA679438Eu);
		b = FF(b, c, d, a, x[k + 15], S14, 0x49B40821u);

		a = GG(a, b, c, d, x[k + 1], S21, 0xF61E2562u);
		d = GG(d, a, b, c, x[k + 6], S22, 0xC040B340u);
		c = GG(c, d, a, b, x[k + 11], S23, 0x265E5A51u);
		b = GG(b, c, d, a, x[k + 0], S24, 0xE9B6C7AAu);

		a = GG(a, b, c, d, x[k + 5], S21, 0xD62F105Du);
		d = GG(d, a, b, c, x[k + 10], S22, 0x2441453u);
		c = GG(c, d, a, b, x[k + 15], S23, 0xD8A1E681u);
		b = GG(b, c, d, a, x[k + 4], S24, 0xE7D3FBC8u);

		a = GG(a, b, c, d, x[k + 9], S21, 0x21E1CDE6u);
		d = GG(d, a, b, c, x[k + 14], S22, 0xC33707D6u);
		c = GG(c, d, a, b, x[k + 3], S23, 0xF4D50D87u);
		b = GG(b, c, d, a, x[k + 8], S24, 0x455A14EDu);

		a = GG(a, b, c, d, x[k + 13], S21, 0xA9E3E905u);
		d = GG(d, a, b, c, x[k + 2], S22, 0xFCEFA3F8u);
		c = GG(c, d, a, b, x[k + 7], S23, 0x676F02D9u);
		b = GG(b, c, d, a, x[k + 12], S24, 0x8D2A4C8Au);

		a = HH(a, b, c, d, x[k + 5], S31, 0xFFFA3942u);
		d = HH(d, a, b, c, x[k + 8], S32, 0x8771F681u);
		c = HH(c, d, a, b, x[k + 11], S33, 0x6D9D6122u);
		b = HH(b, c, d, a, x[k + 14], S34, 0xFDE5380Cu);

		a = HH(a, b, c, d, x[k + 1], S31, 0xA4BEEA44u);
		d = HH(d, a, b, c, x[k + 4], S32, 0x4BDECFA9u);
		c = HH(c, d, a, b, x[k + 7], S33, 0xF6BB4B60u);
		b = HH(b, c, d, a, x[k + 10], S34, 0xBEBFBC70u);

		a = HH(a, b, c, d, x[k + 13], S31, 0x289B7EC6u);
		d = HH(d, a, b, c, x[k + 0], S32, 0xEAA127FAu);
		c = HH(c, d, a, b, x[k + 3], S33, 0xD4EF3085u);
		b = HH(b, c, d, a, x[k + 6], S34, 0x4881D05u);

		a = HH(a, b, c, d, x[k + 9], S31, 0xD9D4D039u);
		d = HH(d, a, b, c, x[k + 12], S32, 0xE6DB99E5u);
		c = HH(c, d, a, b, x[k + 15], S33, 0x1FA27CF8u);
		b = HH(b, c, d, a, x[k + 2], S34, 0xC4AC5665u);

		a = II(a, b, c, d, x[k + 0], S41, 0xF4292244u);
		d = II(d, a, b, c, x[k + 7], S42, 0x432AFF97u);
		c = II(c, d, a, b, x[k + 14], S43, 0xAB9423A7u);
		b = II(b, c, d, a, x[k + 5], S44, 0xFC93A039u);

		a = II(a, b, c, d, x[k + 12], S41, 0x655B59C3u);
		d = II(d, a, b, c, x[k + 3], S42, 0x8F0CCC92u);
		c = II(c, d, a, b, x[k + 10], S43, 0xFFEFF47Du);
		b = II(b, c, d, a, x[k + 1], S44, 0x85845DD1u);

		a = II(a, b, c, d, x[k + 8], S41, 0x6FA87E4Fu);
		d = II(d, a, b, c, x[k + 15], S42, 0xFE2CE6E0u);
		c = II(c, d, a, b, x[k + 6], S43, 0xA3014314u);
		b = II(b, c, d, a, x[k + 13], S44, 0x4E0811A1u);

		a = II(a, b, c, d, x[k + 4], S41, 0xF7537E82u);
		d = II(d, a, b, c, x[k + 11], S42, 0xBD3AF235u);
		c = II(c, d, a, b, x[k + 2], S43, 0x2AD7D2BBu);
		b = II(b, c, d, a, x[k + 9], S44, 0xEB86D391u);

		a = AddUnsigned(a, AA);
		b = AddUnsigned(b, BB);
		c = AddUnsigned(c, CC);
		d = AddUnsigned(d, DD);
	}

	var temp = WordToHex(a) + WordToHex(b) + WordToHex(c) + WordToHex(d);
	return temp.toLowerCase();
}
