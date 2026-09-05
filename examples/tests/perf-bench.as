@module(PERF_BENCH);

// 宿主提供 Stopwatch 单调时钟：相对起点的毫秒数，保留小数精度。
native func nowMs() Number {
	return Env.elapsedMs();
}

// 样本通常只有几十个；复制时插入排序，只分配一个数组，不改变原始采样顺序。
func sortedCopy(Float64Array a) {
	var n = a.length;
	var b = new Float64Array(n);

	for (var i = 0; i < n; i++) {
		var value = a[i];
		var j = i;
		while (j > 0 && b[j - 1] > value) {
			b[j] = b[j - 1];
			j--;
		}
		b[j] = value;
	}

	return b;
}

func percentile(Float64Array sorted, Number p) Number {
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
export func calcStats(Float64Array samples) Object {
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

	var sorted = sortedCopy(samples);

	var sum = 0.0;

	for (var i = 0; i < n; i++) {
		sum = sum + sorted[i];
	}

	var mean = sum / n;
	var varianceSum = 0.0;

	for (var j = 0; j < n; j++) {
		var d = sorted[j] - mean;
		varianceSum = varianceSum + d * d;
	}

	var variance = varianceSum / n;
	var stddev = Math.pow(variance, 0.5);

	var cv = 0.0;

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

func runLoops(Number loopCount, work) Number {
	var guard = 0.0;

	for (var i = 0; i < loopCount; i++) {
		var r = work();

		if (r != null) {
			guard = guard + r;
		}
	}

	return guard;
}

export func benchmark(String name, Number warmups, Number samples, Number innerLoops, work) Object {
	for (var w = 0; w < warmups; w++) {
		runLoops(innerLoops, work);
	}

	var times = new Float64Array(samples);
	var guard = 0.0;

	for (var s = 0; s < samples; s++) {
		var t0 = nowMs();

		guard = guard + runLoops(innerLoops, work);

		var t1 = nowMs();

		var elapsedMs = t1 - t0;
		times[s] = (elapsedMs / innerLoops);
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

export func autoBenchmark(String name, work, Number warmups, Number samples, Number minSampleMs, Number maxInnerLoops) Object {
	// 先预热几次，避免首次运行冷启动影响自动校准
	for (var w = 0; w < warmups; w++) {
		runLoops(1, work);
	}

	var loops = 1;
	var lastMs = 0.0;

	while (loops < maxInnerLoops) {
		var t0 = nowMs();

		runLoops(loops, work);

		var t1 = nowMs();

		lastMs = t1 - t0;

		if (lastMs >= minSampleMs) {
			break;
		}

		loops = loops * 2;
	}

	return benchmark(name, warmups, samples, loops, work);
}

// 示例：测试一个简单数组求和函数
export func run() {
	var data = new Int64Array(10000);

	for (var i = 0; i < 10000; i++) {
		data[i] = i;
	}

	var work = () => {
		var s = 0L;
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
