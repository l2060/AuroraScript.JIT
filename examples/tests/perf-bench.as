@module(PERF_BENCH);

const TICKS_PER_MS = 10000;

@directCall
func nowTicks() {
	return Date.utcNow().ticks;
}

@directCall
func ticksToMs(ticks) {
	return ticks / TICKS_PER_MS;
}

func copyArray(a) {
	var n = a.length;
	var b = Array.withCapacity(n);

	for (var i = 0; i < n; i++) {
		b.push(a[i]);
	}

	return b;
}

func percentile(sorted, p) {
    var n = sorted.length;
    if (n == 0) {
        return 0;
    }
    if (p <= 0) {
        return sorted[0];
    }
    if (p >= 1) {
        return sorted[n - 1];
    }
    var idx = Math.ceil(p * n) - 1;
    if (idx < 0) {
        idx = 0;
    }
    if (idx >= n) {
        idx = n - 1;
    }
    return sorted[idx];
}
export func calcStats(samples) {
	var n = samples.length;

	if (n == 0) {
		return {
			count: 0,
			minMs: 0,
			maxMs: 0,
			meanMs: 0,
			medianMs: 0,
			p90Ms: 0,
			p95Ms: 0,
			p99Ms: 0,
			stddevMs: 0,
			cv: 0
		};
	}

	var sorted = copyArray(samples);
	sorted.sort();

	var sum = 0;

	for (var i = 0; i < n; i++) {
		sum = sum + sorted[i];
	}

	var mean = sum / n;
	var varianceSum = 0;

	for (var j = 0; j < n; j++) {
		var d = sorted[j] - mean;
		varianceSum = varianceSum + d * d;
	}

	var variance = varianceSum / n;
	var stddev = Math.pow(variance, 0.5);

	var cv = 0;

	if (mean != 0) {
		cv = stddev / mean;
	}

	return {
		count: n,
		minMs: sorted[0],
		maxMs: sorted[n - 1],
		meanMs: mean,
		medianMs: percentile(sorted, 0.50),
		p90Ms: percentile(sorted, 0.90),
		p95Ms: percentile(sorted, 0.95),
		p99Ms: percentile(sorted, 0.99),
		stddevMs: stddev,
		cv: cv
	};
}

func runLoops(loopCount, work) {
	var guard = 0;

	for (var i = 0; i < loopCount; i++) {
		var r = work();

		if (r != null) {
			guard = guard + r;
		}
	}

	return guard;
}

export func benchmark(name, warmups, samples, innerLoops, work) {
	for (var w = 0; w < warmups; w++) {
		runLoops(innerLoops, work);
	}

	var times = Array.withCapacity(samples);
	var guard = 0;

	for (var s = 0; s < samples; s++) {
		var t0 = nowTicks();

		guard = guard + runLoops(innerLoops, work);

		var t1 = nowTicks();

		var elapsedMs = ticksToMs(t1 - t0);
		times.push(elapsedMs / innerLoops);
	}

	var stat = calcStats(times);

	return {
		name: name,
		warmups: warmups,
		samples: samples,
		innerLoops: innerLoops,
		unit: "ms/op",
		rawMsPerOp: times,
		stats: stat,
		guard: guard
	};
}

export func autoBenchmark(name, work, warmups, samples, minSampleMs, maxInnerLoops) {
	// 先预热几次，避免首次运行冷启动影响自动校准
	for (var w = 0; w < warmups; w++) {
		runLoops(1, work);
	}

	var loops = 1;
	var lastMs = 0;

	while (loops < maxInnerLoops) {
		var t0 = nowTicks();

		runLoops(loops, work);

		var t1 = nowTicks();

		lastMs = ticksToMs(t1 - t0);

		if (lastMs >= minSampleMs) {
			break;
		}

		loops = loops * 2;
	}

	return benchmark(name, warmups, samples, loops, work);
}

// 示例：测试一个简单数组求和函数
export func run() {
	var data = Array.withCapacity(10000);

	for (var i = 0; i < 10000; i++) {
		data.push(i);
	}

	var work = () => {
		var s = 0;
		var n = data.length;

		for (var i = 0; i < n; i++) {
			s = s + data[i];
		}

		return s;
	};

	var result = autoBenchmark("sum-10000", work, 10, 50, 100, 4096);
	console.log(result);
	return result;
}