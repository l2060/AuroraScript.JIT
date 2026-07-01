@module(ASTAR1000);

const WALKABLE = 0;
const BLOCKED = 1;

const ORTH = 10;
const DIAG = 14;

// ------------------------------------------------------------
// Basic utils
// ------------------------------------------------------------

func absn(v) {
    if (v < 0) {
        return -v;
    }

    return v;
}

func idOf(x, y, w) {
    return y * w + x;
}

// Octile distance, suitable for 8-direction grid movement.
@directCall
func heuristic(x, y, gx, gy) {
    var dx = x - gx;
    if (dx < 0) {
        dx = -dx;
    }

    var dy = y - gy;
    if (dy < 0) {
        dy = -dy;
    }

    if (dx < dy) {
        return ORTH * dy + (DIAG - ORTH) * dx;
    }

    return ORTH * dx + (DIAG - ORTH) * dy;
}

// ------------------------------------------------------------
// Deterministic pseudo random generator
// ------------------------------------------------------------

func rand01(rng) {
    var x = rng.seed;

    x = x ^ (x << 13);
    x = x ^ (x >> 17);
    x = x ^ (x << 5);

    rng.seed = x;

    if (x < 0) {
        x = -x;
    }

    return (x % 1000000) / 1000000;
}

// ------------------------------------------------------------
// Generate 1000 * 1000 map data
//
// 0 = walkable
// 1 = blocked
// ------------------------------------------------------------

export func makeMap(w, h, blockRate, seed) {
    var n = w * h;
    var map = Array.withCapacity(n);
    var rng = { seed: seed };

    for (var i = 0; i < n; i++) {
        if (rand01(rng) < blockRate) {
            map.push(BLOCKED);
        } else {
            map.push(WALKABLE);
        }
    }

    // Ensure there is always a valid path:
    // clear top row and right side column.
    for (var x = 0; x < w; x++) {
        map[idOf(x, 0, w)] = WALKABLE;

        if (h > 1) {
            map[idOf(x, 1, w)] = WALKABLE;
        }
    }

    for (var y = 0; y < h; y++) {
        map[idOf(w - 1, y, w)] = WALKABLE;

        if (w > 1) {
            map[idOf(w - 2, y, w)] = WALKABLE;
        }
    }

    map[0] = WALKABLE;
    map[n - 1] = WALKABLE;

    return map;
}

// ------------------------------------------------------------
// Binary min heap.
// Search uses lazy duplicate entries instead of decrease-key, so no heap position
// array is needed on the hot path.
// ------------------------------------------------------------

@directCall
func heapPush(heap, f, heapSize, node) {
    var i = heapSize;
    var score = f[node];

    heapSize = heapSize + 1;

    while (i > 0) {
        var parent = i - 1;
        parent = (parent - (parent % 2)) / 2;

        var parentNode = heap[parent];
        if (f[parentNode] <= score) {
            break;
        }

        heap[i] = parentNode;
        i = parent;
    }

    heap[i] = node;

    return heapSize;
}

// ------------------------------------------------------------
// Pathfinder object factory
// ------------------------------------------------------------

export func newFinder(w, h, map) {
    var n = w * h;

    return {
        w: w,
        h: h,
        map: map,

        g: Array.withCapacity(n),
        f: Array.withCapacity(n),
        parent: Array.withCapacity(n),

        open: Array.withCapacity(n),
        closed: Array.withCapacity(n),

        heap: Array.withCapacity(n),

        heapSize: 0,
        sid: 1
    };
}

@directCall
func beginSearch(pf) {
    pf.heapSize = 0;
    pf.sid = pf.sid + 1;
}

// ------------------------------------------------------------
// Neighbor expansion
// ------------------------------------------------------------
// Inlined in findPath. Keeping neighbor checks local avoids object property
// reads and function calls for every expanded node.

// ------------------------------------------------------------
// Reconstruct path
// ------------------------------------------------------------

func buildPath(pf, goal) {
    var rev = [];
    var cur = goal;

    while (cur != -1) {
        rev.push(cur);
        cur = pf.parent[cur];
    }

    var path = Array.withCapacity(rev.length);

    for (var i = rev.length - 1; i >= 0; i--) {
        path.push(rev[i]);
    }

    return path;
}

// ------------------------------------------------------------
// A* search
// ------------------------------------------------------------

export func findPath(pf, sx, sy, gx, gy) {
    var w = pf.w;
    var h = pf.h;

    if (sx < 0 || sx >= w || sy < 0 || sy >= h) {
        return [];
    }

    if (gx < 0 || gx >= w || gy < 0 || gy >= h) {
        return [];
    }

    var start = sy * w + sx;
    var goal = gy * w + gx;
    var map = pf.map;

    if (map[start] == BLOCKED || map[goal] == BLOCKED) {
        return [];
    }

    beginSearch(pf);

    var g = pf.g;
    var f = pf.f;
    var parent = pf.parent;
    var open = pf.open;
    var closed = pf.closed;
    var heap = pf.heap;
    var sid = pf.sid;
    var heapSize = 0;
    var lastX = w - 1;
    var lastY = h - 1;

    g[start] = 0;
    f[start] = heuristic(sx, sy, gx, gy);
    parent[start] = -1;
    open[start] = sid;
    heapSize = heapPush(heap, f, heapSize, start);

    while (heapSize > 0) {
        var cur = heap[0];
        heapSize = heapSize - 1;

        if (heapSize > 0) {
            var last = heap[heapSize];
            var lastScore = f[last];
            var sift = 0;

            while (true) {
                var left = sift * 2 + 1;
                if (left >= heapSize) {
                    break;
                }

                var best = left;
                var bestNode = heap[left];
                var bestScore = f[bestNode];
                var right = left + 1;

                if (right < heapSize) {
                    var rightNode = heap[right];
                    var rightScore = f[rightNode];

                    if (rightScore < bestScore) {
                        best = right;
                        bestNode = rightNode;
                        bestScore = rightScore;
                    }
                }

                if (bestScore >= lastScore) {
                    break;
                }

                heap[sift] = bestNode;
                sift = best;
            }

            heap[sift] = last;
        }

        if (closed[cur] == sid) {
            continue;
        }

        if (cur == goal) {
            pf.heapSize = heapSize;
            return buildPath(pf, goal);
        }

        closed[cur] = sid;

        var cx = cur % w;
        var cy = (cur - cx) / w;
        var baseG = g[cur];

        if (cx < lastX) {
            var rightId = cur + 1;
            if (map[rightId] != BLOCKED && closed[rightId] != sid) {
                var rightG = baseG + ORTH;
                if (open[rightId] != sid || rightG < g[rightId]) {
                    open[rightId] = sid;
                    g[rightId] = rightG;
                    f[rightId] = rightG + heuristic(cx + 1, cy, gx, gy);
                    parent[rightId] = cur;
                    heapSize = heapPush(heap, f, heapSize, rightId);
                }
            }
        }

        if (cx > 0) {
            var leftId = cur - 1;
            if (map[leftId] != BLOCKED && closed[leftId] != sid) {
                var leftG = baseG + ORTH;
                if (open[leftId] != sid || leftG < g[leftId]) {
                    open[leftId] = sid;
                    g[leftId] = leftG;
                    f[leftId] = leftG + heuristic(cx - 1, cy, gx, gy);
                    parent[leftId] = cur;
                    heapSize = heapPush(heap, f, heapSize, leftId);
                }
            }
        }

        if (cy < lastY) {
            var downId = cur + w;
            if (map[downId] != BLOCKED && closed[downId] != sid) {
                var downG = baseG + ORTH;
                if (open[downId] != sid || downG < g[downId]) {
                    open[downId] = sid;
                    g[downId] = downG;
                    f[downId] = downG + heuristic(cx, cy + 1, gx, gy);
                    parent[downId] = cur;
                    heapSize = heapPush(heap, f, heapSize, downId);
                }
            }
        }

        if (cy > 0) {
            var upId = cur - w;
            if (map[upId] != BLOCKED && closed[upId] != sid) {
                var upG = baseG + ORTH;
                if (open[upId] != sid || upG < g[upId]) {
                    open[upId] = sid;
                    g[upId] = upG;
                    f[upId] = upG + heuristic(cx, cy - 1, gx, gy);
                    parent[upId] = cur;
                    heapSize = heapPush(heap, f, heapSize, upId);
                }
            }
        }

        if (cy < lastY) {
            var baseDown = cur + w;

            if (cx < lastX) {
                var downRight = baseDown + 1;
                if (map[cur + 1] != BLOCKED && map[baseDown] != BLOCKED && map[downRight] != BLOCKED && closed[downRight] != sid) {
                    var downRightG = baseG + DIAG;
                    if (open[downRight] != sid || downRightG < g[downRight]) {
                        open[downRight] = sid;
                        g[downRight] = downRightG;
                        f[downRight] = downRightG + heuristic(cx + 1, cy + 1, gx, gy);
                        parent[downRight] = cur;
                        heapSize = heapPush(heap, f, heapSize, downRight);
                    }
                }
            }

            if (cx > 0) {
                var downLeft = baseDown - 1;
                if (map[cur - 1] != BLOCKED && map[baseDown] != BLOCKED && map[downLeft] != BLOCKED && closed[downLeft] != sid) {
                    var downLeftG = baseG + DIAG;
                    if (open[downLeft] != sid || downLeftG < g[downLeft]) {
                        open[downLeft] = sid;
                        g[downLeft] = downLeftG;
                        f[downLeft] = downLeftG + heuristic(cx - 1, cy + 1, gx, gy);
                        parent[downLeft] = cur;
                        heapSize = heapPush(heap, f, heapSize, downLeft);
                    }
                }
            }
        }

        if (cy > 0) {
            var baseUp = cur - w;

            if (cx < lastX) {
                var upRight = baseUp + 1;
                if (map[cur + 1] != BLOCKED && map[baseUp] != BLOCKED && map[upRight] != BLOCKED && closed[upRight] != sid) {
                    var upRightG = baseG + DIAG;
                    if (open[upRight] != sid || upRightG < g[upRight]) {
                        open[upRight] = sid;
                        g[upRight] = upRightG;
                        f[upRight] = upRightG + heuristic(cx + 1, cy - 1, gx, gy);
                        parent[upRight] = cur;
                        heapSize = heapPush(heap, f, heapSize, upRight);
                    }
                }
            }

            if (cx > 0) {
                var upLeft = baseUp - 1;
                if (map[cur - 1] != BLOCKED && map[baseUp] != BLOCKED && map[upLeft] != BLOCKED && closed[upLeft] != sid) {
                    var upLeftG = baseG + DIAG;
                    if (open[upLeft] != sid || upLeftG < g[upLeft]) {
                        open[upLeft] = sid;
                        g[upLeft] = upLeftG;
                        f[upLeft] = upLeftG + heuristic(cx - 1, cy - 1, gx, gy);
                        parent[upLeft] = cur;
                        heapSize = heapPush(heap, f, heapSize, upLeft);
                    }
                }
            }
        }
    }

    pf.heapSize = 0;
    return [];
}

// ------------------------------------------------------------
// Validate path
// ------------------------------------------------------------

export func validate(map, w, h, path) {
    if (path.length == 0) {
        return false;
    }

    for (var i = 0; i < path.length; i++) {
        var id = path[i];

        if (id < 0 || id >= w * h) {
            return false;
        }

        if (map[id] == BLOCKED) {
            return false;
        }

        if (i > 0) {
            var p = path[i - 1];

            var dx = absn((id % w) - (p % w));
            var dy = absn(Math.floor(id / w) - Math.floor(p / w));

            if (dx > 1 || dy > 1) {
                return false;
            }
        }
    }

    return true;
}

// ------------------------------------------------------------
// Optional helper: convert path id to coordinate objects
// ------------------------------------------------------------

export func pathToPoints(path, w) {
    var points = Array.withCapacity(path.length);

    for (var i = 0; i < path.length; i++) {
        var id = path[i];

        points.push({
            x: id % w,
            y: Math.floor(id / w)
        });
    }

    return points;
}

// ------------------------------------------------------------
// Main test: generates 1000 * 1000 map and runs A*
// ------------------------------------------------------------

export func run() {
    var w = 1000;
    var h = 1000;
    var cells = w * h;

    console.log("generate map", w, h, "cells", cells);

    console.time("make 1000x1000 map");
    var map = makeMap(w, h, 0.28, 20250701);
    console.timeEnd("make 1000x1000 map");

    console.log("map generated, length =", map.length);

    console.time("create finder");
    var pf = newFinder(w, h, map);
    console.timeEnd("create finder");

    console.time("astar");
    var path = findPath(pf, 0, 0, w - 1, h - 1);
    console.timeEnd("astar");

    var ok = validate(map, w, h, path);

    console.log("path length =", path.length);
    console.log("valid =", ok);

    var first = null;
    var last = null;

    if (path.length > 0) {
        first = path[0];
        last = path[path.length - 1];

        console.log("first node =", first);
        console.log("last node =", last);
    }

    return {
        width: w,
        height: h,
        cells: cells,
        mapLength: map.length,
        pathLength: path.length,
        valid: ok,
        firstNode: first,
        lastNode: last
    };
}
