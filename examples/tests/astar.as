@module(ASTAR);

export const ASTAR_SQRT2 = 1.4142135623730951;
export const ASTAR_MAX_SEARCH_ID = 2147483646;

@directCall
func astarHeuristic(x, y, goalX, goalY, allowDiagonal, minCost) {
	var dx = x - goalX;
	if (dx < 0) {
		dx = -dx;
	}

	var dy = y - goalY;
	if (dy < 0) {
		dy = -dy;
	}

	if (allowDiagonal) {
		if (dx > dy) {
			return((dx - dy) + ASTAR_SQRT2 * dy) * minCost;
		}

		return((dy - dx) + ASTAR_SQRT2 * dx) * minCost;
	}

	return(dx + dy) * minCost;
}

@directCall
func astarHeapPush(heapNodes, heapScores, heapTies, heapLength, node, score, tie) {
	var i = heapLength;
	heapLength++;

	while (i > 0) {
		var parent = i - 1;
		parent = (parent - (parent % 2)) / 2;

		var parentScore = heapScores[parent];
		var parentTie = heapTies[parent];
		var parentNode = heapNodes[parent];
		var shouldMoveParent = false;

		if (score < parentScore) {
			shouldMoveParent = true;
		} else {
			if (score == parentScore) {
				if (tie < parentTie) {
					shouldMoveParent = true;
				} else {
					if (tie == parentTie && node < parentNode) {
						shouldMoveParent = true;
					}
				}
			}
		}

		if (!shouldMoveParent) {
			break;
		}

		heapNodes[i] = parentNode;
		heapScores[i] = parentScore;
		heapTies[i] = parentTie;
		i = parent;
	}

	heapNodes[i] = node;
	heapScores[i] = score;
	heapTies[i] = tie;
	return heapLength;
}

@directCall
func astarClearSearchState(astar) {
	var opened = tdoc Int32Array $(astar.opened);
	var closed = tdoc Int32Array $(astar.closed);
	var size = astar.size;

	for (var i = 0; i < size; i++) {
		opened[i] = 0;
		closed[i] = 0;
	}
}

export func createAStar(width, height, walkable, costs) {
	if (width <= 0 || height <= 0) {
		throw "width and height must be positive";
	}

	var size = width * height;
	var walk = new Int8Array(size);
	var moveCosts = new Float64Array(size);
	var gScore = new Float64Array(size);
	var parents = new Int32Array(size);
	var opened = new Int32Array(size);
	var closed = new Int32Array(size);
	var heapNodes = Array.withCapacity(size);
	var heapScores = Array.withCapacity(size);
	var heapTies = Array.withCapacity(size);
	var uniformCost = costs == null;
	var minCost = 1;
	if (uniformCost) {
		moveCosts.fill(1);
	} else {
		minCost = Number.POSITIVE_INFINITY;
	}

	for (var i = 0; i < size; i++) {
		if (walkable == null) {
			walk[i] = 1;
		} else {
			if (walkable[i]) {
				walk[i] = 1;
			} else {
				walk[i] = 0;
			}
		}

		if (!uniformCost) {
			var cellCost = costs[i];
			if (cellCost <= 0) {
				cellCost = 1;
			}
			moveCosts[i] = cellCost;
			if (cellCost < minCost) {
				minCost = cellCost;
			}
		}
	}

	return {
		width: width,
		height: height,
		size: size,
		minCost: minCost,
		uniformCost: uniformCost,
		searchId: 0,
		expanded: 0,
		walkable: walk,
		costs: moveCosts,
		gScore: gScore,
		parents: parents,
		opened: opened,
		closed: closed,
		heapNodes: heapNodes,
		heapScores: heapScores,
		heapTies: heapTies
	};
}

export func toIndex(astar, x, y) {
	if (astar == null) {
		return -1;
	}

	if (x < 0 || y < 0 || x >= astar.width || y >= astar.height) {
		return -1;
	}

	return y * astar.width + x;
}

export func indexX(astar, index) {
	return index % astar.width;
}

export func indexY(astar, index) {
	var x = index % astar.width;
	return(index - x) / astar.width;
}

export func setWalkable(astar, x, y, canWalk) {
	var index = toIndex(astar, x, y);
	if (index < 0) {
		return false;
	}

	if (canWalk) {
		astar.walkable[index] = 1;
	} else {
		astar.walkable[index] = 0;
	}

	return true;
}

export func setCost(astar, x, y, cost) {
	var index = toIndex(astar, x, y);
	if (index < 0 || cost <= 0) {
		return false;
	}

	astar.costs[index] = cost;
	astar.uniformCost = false;
	if (cost < astar.minCost) {
		astar.minCost = cost;
	}

	return true;
}

export func newPathBuffer(astar) {
	return Array.withCapacity(astar.width + astar.height);
}

export func findPathInto(astar, startX, startY, goalX, goalY, outPath, allowDiagonal = true, avoidCornerCut = true) {
	if (astar == null) {
		throw "astar is required";
	}

	if (outPath == null) {
		throw "outPath is required";
	}

	outPath.length = 0;

	var width = astar.width;
	var height = astar.height;

	if (startX < 0 || startY < 0 || goalX < 0 || goalY < 0) {
		astar.expanded = 0;
		return 0;
	}

	if (startX >= width || goalX >= width || startY >= height || goalY >= height) {
		astar.expanded = 0;
		return 0;
	}

	var start = startY * width + startX;
	var goal = goalY * width + goalX;
	var walkable = tdoc Int8Array $(astar.walkable);

	if (!walkable[start] || !walkable[goal]) {
		astar.expanded = 0;
		return 0;
	}

	if (start == goal) {
		outPath[0] = start;
		astar.expanded = 0;
		return 1;
	}

	var searchId = astar.searchId + 1;
	if (searchId > ASTAR_MAX_SEARCH_ID) {
		astarClearSearchState(astar);
		searchId = 1;
	}
	astar.searchId = searchId;

	var costs = tdoc Float64Array $(astar.costs);
	var gScore = tdoc Float64Array $(astar.gScore);
	var parents = tdoc Int32Array $(astar.parents);
	var opened = tdoc Int32Array $(astar.opened);
	var closed = tdoc Int32Array $(astar.closed);
	var heapNodes = tdoc Array $(astar.heapNodes);
	var heapScores = tdoc Array $(astar.heapScores);
	var heapTies = tdoc Array $(astar.heapTies);
	var minCost = astar.minCost;
	var uniformCost = astar.uniformCost;
	var uniformDiagonalCost = minCost * ASTAR_SQRT2;
	var lastX = width - 1;
	var lastY = height - 1;
	var heapLength = 0;
	var expanded = 0;
	var startH = astarHeuristic(startX, startY, goalX, goalY, allowDiagonal, minCost);

	gScore[start] = 0;
	parents[start] = -1;
	opened[start] = searchId;
	heapNodes[0] = start;
	heapScores[0] = startH;
	heapTies[0] = startH;
	heapLength = 1;

	while (heapLength > 0) {
		var current = heapNodes[0];
		var lastHeapIndex = heapLength - 1;
		var lastNode = heapNodes[lastHeapIndex];
		var lastScore = heapScores[lastHeapIndex];
		var lastTie = heapTies[lastHeapIndex];
		heapLength = lastHeapIndex;

		if (heapLength > 0) {
			var sift = 0;
			while (true) {
				var left = sift * 2 + 1;
				if (left >= heapLength) {
					break;
				}

				var best = left;
				var right = left + 1;

				if (right < heapLength) {
					var leftScore = heapScores[left];
					var leftTie = heapTies[left];
					var leftNode = heapNodes[left];
					var rightScore = heapScores[right];
					var rightTie = heapTies[right];
					var rightNode = heapNodes[right];
					var rightIsBetter = false;

					if (rightScore < leftScore) {
						rightIsBetter = true;
					} else {
						if (rightScore == leftScore) {
							if (rightTie < leftTie) {
								rightIsBetter = true;
							} else {
								if (rightTie == leftTie && rightNode < leftNode) {
									rightIsBetter = true;
								}
							}
						}
					}

					if (rightIsBetter) {
						best = right;
					}
				}

				var childScore = heapScores[best];
				var childTie = heapTies[best];
				var childNode = heapNodes[best];
				var childIsBetter = false;

				if (childScore < lastScore) {
					childIsBetter = true;
				} else {
					if (childScore == lastScore) {
						if (childTie < lastTie) {
							childIsBetter = true;
						} else {
							if (childTie == lastTie && childNode < lastNode) {
								childIsBetter = true;
							}
						}
					}
				}

				if (!childIsBetter) {
					break;
				}

				heapNodes[sift] = childNode;
				heapScores[sift] = childScore;
				heapTies[sift] = childTie;
				sift = best;
			}

			heapNodes[sift] = lastNode;
			heapScores[sift] = lastScore;
			heapTies[sift] = lastTie;
		}

		if (closed[current] == searchId) {
			continue;
		}

		closed[current] = searchId;
		expanded++;

		if (current == goal) {
			var count = 0;
			var pathNode = goal;

			while (pathNode >= 0) {
				outPath[count] = pathNode;
				count++;
				pathNode = parents[pathNode];
			}

			var swapLeft = 0;
			var swapRight = count - 1;
			while (swapLeft < swapRight) {
				var tmp = outPath[swapLeft];
				outPath[swapLeft] = outPath[swapRight];
				outPath[swapRight] = tmp;
				swapLeft++;
				swapRight--;
			}

			astar.expanded = expanded;
			return count;
		}

		var currentX = current % width;
		var currentY = (current - currentX) / width;
		var baseG = gScore[current];

		if (currentX > 0) {
			var leftNeighbor = current - 1;
			if (walkable[leftNeighbor] && closed[leftNeighbor] != searchId) {
				var leftG = baseG + minCost;
				if (!uniformCost) {
					leftG = baseG + costs[leftNeighbor];
				}
				if (opened[leftNeighbor] != searchId || leftG < gScore[leftNeighbor]) {
					var leftH = astarHeuristic(currentX - 1, currentY, goalX, goalY, allowDiagonal, minCost);
					opened[leftNeighbor] = searchId;
					gScore[leftNeighbor] = leftG;
					parents[leftNeighbor] = current;
					heapLength = astarHeapPush(heapNodes, heapScores, heapTies, heapLength, leftNeighbor, leftG + leftH, leftH);
				}
			}
		}

		if (currentX < lastX) {
			var rightNeighbor = current + 1;
			if (walkable[rightNeighbor] && closed[rightNeighbor] != searchId) {
				var rightG = baseG + minCost;
				if (!uniformCost) {
					rightG = baseG + costs[rightNeighbor];
				}
				if (opened[rightNeighbor] != searchId || rightG < gScore[rightNeighbor]) {
					var rightH = astarHeuristic(currentX + 1, currentY, goalX, goalY, allowDiagonal, minCost);
					opened[rightNeighbor] = searchId;
					gScore[rightNeighbor] = rightG;
					parents[rightNeighbor] = current;
					heapLength = astarHeapPush(heapNodes, heapScores, heapTies, heapLength, rightNeighbor, rightG + rightH, rightH);
				}
			}
		}

		if (currentY > 0) {
			var upNeighbor = current - width;
			if (walkable[upNeighbor] && closed[upNeighbor] != searchId) {
				var upG = baseG + minCost;
				if (!uniformCost) {
					upG = baseG + costs[upNeighbor];
				}
				if (opened[upNeighbor] != searchId || upG < gScore[upNeighbor]) {
					var upH = astarHeuristic(currentX, currentY - 1, goalX, goalY, allowDiagonal, minCost);
					opened[upNeighbor] = searchId;
					gScore[upNeighbor] = upG;
					parents[upNeighbor] = current;
					heapLength = astarHeapPush(heapNodes, heapScores, heapTies, heapLength, upNeighbor, upG + upH, upH);
				}
			}
		}

		if (currentY < lastY) {
			var downNeighbor = current + width;
			if (walkable[downNeighbor] && closed[downNeighbor] != searchId) {
				var downG = baseG + minCost;
				if (!uniformCost) {
					downG = baseG + costs[downNeighbor];
				}
				if (opened[downNeighbor] != searchId || downG < gScore[downNeighbor]) {
					var downH = astarHeuristic(currentX, currentY + 1, goalX, goalY, allowDiagonal, minCost);
					opened[downNeighbor] = searchId;
					gScore[downNeighbor] = downG;
					parents[downNeighbor] = current;
					heapLength = astarHeapPush(heapNodes, heapScores, heapTies, heapLength, downNeighbor, downG + downH, downH);
				}
			}
		}

		if (allowDiagonal) {
			if (currentY > 0) {
				var baseUp = current - width;

				if (currentX > 0) {
					var upLeft = baseUp - 1;
					var canUpLeft = true;
					if (avoidCornerCut) {
						if (!walkable[current - 1] || !walkable[baseUp]) {
							canUpLeft = false;
						}
					}

					if (canUpLeft && walkable[upLeft] && closed[upLeft] != searchId) {
						var upLeftG = baseG + uniformDiagonalCost;
						if (!uniformCost) {
							upLeftG = baseG + costs[upLeft] * ASTAR_SQRT2;
						}
						if (opened[upLeft] != searchId || upLeftG < gScore[upLeft]) {
							var upLeftH = astarHeuristic(currentX - 1, currentY - 1, goalX, goalY, allowDiagonal, minCost);
							opened[upLeft] = searchId;
							gScore[upLeft] = upLeftG;
							parents[upLeft] = current;
							heapLength = astarHeapPush(heapNodes, heapScores, heapTies, heapLength, upLeft, upLeftG + upLeftH, upLeftH);
						}
					}
				}

				if (currentX < lastX) {
					var upRight = baseUp + 1;
					var canUpRight = true;
					if (avoidCornerCut) {
						if (!walkable[current + 1] || !walkable[baseUp]) {
							canUpRight = false;
						}
					}

					if (canUpRight && walkable[upRight] && closed[upRight] != searchId) {
						var upRightG = baseG + uniformDiagonalCost;
						if (!uniformCost) {
							upRightG = baseG + costs[upRight] * ASTAR_SQRT2;
						}
						if (opened[upRight] != searchId || upRightG < gScore[upRight]) {
							var upRightH = astarHeuristic(currentX + 1, currentY - 1, goalX, goalY, allowDiagonal, minCost);
							opened[upRight] = searchId;
							gScore[upRight] = upRightG;
							parents[upRight] = current;
							heapLength = astarHeapPush(heapNodes, heapScores, heapTies, heapLength, upRight, upRightG + upRightH, upRightH);
						}
					}
				}
			}

			if (currentY < lastY) {
				var baseDown = current + width;

				if (currentX > 0) {
					var downLeft = baseDown - 1;
					var canDownLeft = true;
					if (avoidCornerCut) {
						if (!walkable[current - 1] || !walkable[baseDown]) {
							canDownLeft = false;
						}
					}

					if (canDownLeft && walkable[downLeft] && closed[downLeft] != searchId) {
						var downLeftG = baseG + uniformDiagonalCost;
						if (!uniformCost) {
							downLeftG = baseG + costs[downLeft] * ASTAR_SQRT2;
						}
						if (opened[downLeft] != searchId || downLeftG < gScore[downLeft]) {
							var downLeftH = astarHeuristic(currentX - 1, currentY + 1, goalX, goalY, allowDiagonal, minCost);
							opened[downLeft] = searchId;
							gScore[downLeft] = downLeftG;
							parents[downLeft] = current;
							heapLength = astarHeapPush(heapNodes, heapScores, heapTies, heapLength, downLeft, downLeftG + downLeftH, downLeftH);
						}
					}
				}

				if (currentX < lastX) {
					var downRight = baseDown + 1;
					var canDownRight = true;
					if (avoidCornerCut) {
						if (!walkable[current + 1] || !walkable[baseDown]) {
							canDownRight = false;
						}
					}

					if (canDownRight && walkable[downRight] && closed[downRight] != searchId) {
						var downRightG = baseG + uniformDiagonalCost;
						if (!uniformCost) {
							downRightG = baseG + costs[downRight] * ASTAR_SQRT2;
						}
						if (opened[downRight] != searchId || downRightG < gScore[downRight]) {
							var downRightH = astarHeuristic(currentX + 1, currentY + 1, goalX, goalY, allowDiagonal, minCost);
							opened[downRight] = searchId;
							gScore[downRight] = downRightG;
							parents[downRight] = current;
							heapLength = astarHeapPush(heapNodes, heapScores, heapTies, heapLength, downRight, downRightG + downRightH, downRightH);
						}
					}
				}
			}
		}
	}

	astar.expanded = expanded;
	return 0;
}

export func findPathIndexes(astar, startX, startY, goalX, goalY, allowDiagonal = true, avoidCornerCut = true) {
	var path = newPathBuffer(astar);
	findPathInto(astar, startX, startY, goalX, goalY, path, allowDiagonal, avoidCornerCut);
	return path;
}

export func findPath(astar, startX, startY, goalX, goalY, allowDiagonal = true, avoidCornerCut = true) {
	var indexes = findPathIndexes(astar, startX, startY, goalX, goalY, allowDiagonal, avoidCornerCut);
	var count = indexes.length;
	var result = Array.withCapacity(count);
	var width = astar.width;

	for (var i = 0; i < count; i++) {
		var index = indexes[i];
		var x = index % width;
		var y = (index - x) / width;
		result.push({ x: x, y: y });
	}

	return result;
}

// examples
// 
const ASTAR_WALKABLE = 1;
const ASTAR_BLOCKED = 0;

const width = 1000;
const height = 1000;
const blockRate = 0.28;
const seed = 20250701;

var map = [];
var astar = null;
var pathBuffer = null;
var startX = 0;
var startY = 0;
var goalX = width - 1;
var goalY = height - 1;

func astarRand01(rng) {
	var x = rng.seed;

	x = x ^ (x << 13);
	x = x ^ (x >> 17);
	x = x ^ (x << 5);

	rng.seed = x;

	if (x < 0) {
		x = -x;
	}

	return(x % 1000000) / 1000000;
}

export func makeMap(w, h, rate, rngSeed) {
	var n = w * h;
	var _map = new Int8Array(n);
	var rng = { seed: rngSeed };

	for (var i = 0; i < n; i++) {
		if (astarRand01(rng) < rate) {
			_map[i] = ASTAR_BLOCKED;
		} else {
			_map[i] = ASTAR_WALKABLE;
		}
	}

	// Ensure there is always a valid path:
	// clear top row and right side column.
	for (var x = 0; x < w; x++) {
		_map[x] = ASTAR_WALKABLE;

		if (h > 1) {
			_map[w + x] = ASTAR_WALKABLE;
		}
	}

	for (var y = 0; y < h; y++) {
		_map[y * w + (w - 1)] = ASTAR_WALKABLE;

		if (w > 1) {
			_map[y * w + (w - 2)] = ASTAR_WALKABLE;
		}
	}

	_map[0] = ASTAR_WALKABLE;
	_map[n - 1] = ASTAR_WALKABLE;

	return _map;
}



func init() {
	console.log("generate map", width, height, "cells", width * height);

	console.time("make 1000x1000 map");
	map = makeMap(width, height, blockRate, seed);
	console.timeEnd("make 1000x1000 map");

	console.log("map generated, length =", map.length);

	// fs.writeText('map.tdoc',TDoc.stringify(map,false));

	console.time("create astar");
	astar = createAStar(width, height, map, null);
	pathBuffer = newPathBuffer(astar);
	console.timeEnd("create astar");
}

init();

export func runExample() {

	console.time("astar");
	var pathLength = findPathInto(astar, startX, startY, goalX, goalY, pathBuffer, true, true);
	console.timeEnd("astar");

	console.log("expanded =", astar.expanded);
	console.log("path length =", pathLength);


	var first = null;
	var last = null;

	if (pathLength > 0) {
		first = pathBuffer[0];
		last = pathBuffer[pathLength - 1];

		console.log("first node =", first);
		console.log("last node =", last);
	}

	return {
		width: width,
		height: height,
		cells: width * height,
		mapLength: map.length,
		pathLength: pathLength,
		valid: ok,
		expanded: astar.expanded,
		firstNode: first,
		lastNode: last,
		startX: startX,
		startY: startY,
		goalX: goalX,
		goalY: goalY
	};
}
