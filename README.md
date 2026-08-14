# P4P-Packing API — Samples

[![License: MIT](https://img.shields.io/badge/License-MIT-44883e.svg)](LICENSE)
[![API](https://img.shields.io/badge/API-p4p.pro4soft.com-086AD8.svg)](https://p4p.pro4soft.com)

Runnable client samples for the [P4P-Packing API](https://p4p.pro4soft.com) — a REST 3D
bin-packing service. Each sample reads a request from `input.json`, calls `POST /api/pack`, and
prints the result. Five languages, identical behavior, so you can compare implementations and copy
the one you need.

## What the API does

Given a set of items (dimensions, quantity, weight, constraints) and candidate containers, the
service computes where each item goes and returns the full plan: per-container fill, total weight,
exact placement coordinates, loading order, and any items that didn't fit. One endpoint
(`POST /api/pack`) covers cartonization, palletization, container loading, and truck loading — the
difference is just the container dimensions and `loadingMode` you pass.

What's relevant when integrating against it:

- **Optimization objective is selectable** — `optimizeBy: "volume"` maximizes utilization;
  `"cost"` minimizes total container cost across a heterogeneous container set.
- **Constraints are honored per item** — orientation lock (`uprightOnly`), stacking strength
  (`crashability`), per-container weight caps (`maxContainerWeight`), and single-SKU containers.
- **Bounded, predictable latency** — each request runs under a hard **5 s** compute budget; on
  overrun the call returns `408` rather than hanging.
- **Scales per request** — up to **100** item types, **20** container types, **10,000** total
  units, **1200 in** (100 ft) max per side.
- **Compact response** — placements are returned as grid blocks (`nx × ny × nz` identical units at
  a corner `(x, y, z)`), not one entry per unit, so a 10,000-unit pack stays a small payload. The
  samples expand a block to a count with `nx * ny * nz`.
- **Results are addressable** — each pack gets an `id`; retrieve it later (`GET /api/pack/{id}`) or
  render any container as an isometric SVG (`GET /api/container/{id}/svg`).

## Samples

Each folder is a self-contained CLI program with its own README and an editable `input.json`.

| Language | Folder | Run | HTTP / JSON |
|---|---|---|---|
| C# | [`csharp/`](csharp/) | `dotnet run` | `HttpClient` + `System.Text.Json`, no packages |
| JavaScript (Node.js 18+) | [`javascript/`](javascript/) | `node index.js` | native `fetch`, no packages |
| Python 3.9+ | [`python/`](python/) | `python main.py` | `urllib` + `json`, stdlib only |
| Java 17+ | [`java/`](java/) | `mvn -q compile exec:java` | `java.net.http` + Gson |
| PHP 8+ | [`php/`](php/) | `php main.php` | `cURL` + `json`, stdlib only |

## Quick start

```bash
cd python        # or csharp, javascript, java, php
python main.py
```

Samples run keyless against the live API. Keyless `POST /api/pack` is rate-limited per IP
(currently **1 request per minute**); on overrun the call returns `429` with a `Retry-After` header,
which each sample reads and reports before exiting cleanly. `GET /api/pack/limits` publishes the
current window if you want to pace requests up front. Set an API key in the config block at the top
of the program to remove the limit.

## The packing request

Each sample sends `input.json` verbatim as the request body, so you can change the scenario without
touching code. JSON is **camelCase**; enums are **strings**.

**Item** (`items[]`)

| Field | Type | Notes |
|---|---|---|
| `length`, `width`, `height` | number | Required, `> 0`, in `unit` |
| `quantity` | int | Count of this item type |
| `name` | string | Echoed back in the result |
| `weight` | number | Per-unit weight, in `weightUnit` |
| `unit` | string | `"in"` or `"cm"` (items reject `"ft"`) |
| `uprightOnly` | bool | Keep height vertical (no tipping) |
| `crashability` | int | Stacking strength; higher bears more load on top, `0` = unconstrained |
| `color`, `payload` | string | Opaque, echoed back (e.g. hex color, SKU) |

**Container** (`containers[]`)

| Field | Type | Notes |
|---|---|---|
| `length`, `width`, `height` | number | Required, `> 0`, in `unit` |
| `name` | string | Echoed back |
| `unit` | string | `"in"`, `"cm"`, or `"ft"` |
| `loadingMode` | string | `"topDown"`, `"frontLoad"`, or `"sideLoad"` |
| `cost` | number | Per-container cost; required `> 0` when `optimizeBy` is `"cost"` |
| `payload` | string | Opaque, echoed back (e.g. your own container id) |

**Top-level**

| Field | Type | Default | Notes |
|---|---|---|---|
| `weightUnit` | string | `"lb"` | `"lb"` or `"kg"` |
| `optimizeBy` | string | `"volume"` | `"volume"` or `"cost"`; `"cost"` requires a `cost` on every container |
| `singleSkuPerContainer` | bool | `false` | One item type per container |
| `maxContainerWeight` | number | `0` | Weight cap per container, `0` = none |
| `heavyOnBottom` | bool | `false` | Derive stacking order from `weight`, heaviest lowest; overrides `crashability` for items that have a weight |

The response lists each used container with `utilization`, `totalWeight`, and its packed blocks,
plus `unpackedItems` for anything that didn't fit.

## Endpoints

| Method | Path | Returns |
|---|---|---|
| `POST` | `/api/pack` | Packing plan (containers + placements) |
| `GET` | `/api/pack/{id}` | A previously saved result |
| `GET` | `/api/container/{id}/svg` | Isometric SVG of one container |
| `GET` | `/api/pack/sample` | A sample request payload |
| `GET` | `/api/pack/limits` | Current keyless rate limit (`windowSeconds`, `permitLimit`) |

Base URL: `https://p4p.pro4soft.com/api`.

## Errors

Status code first; bodies are plain text (or a `ProblemDetails` JSON on malformed input).

| Status | Meaning |
|---|---|
| `400` | Validation error (bad dimensions, over a limit, item using `"ft"`, or `optimizeBy: "cost"` with a container that has no `cost`) |
| `406` | An item fits no container in any allowed orientation |
| `408` | Compute budget (5 s) exceeded |
| `429` | Keyless rate limit exceeded; carries `Retry-After` (seconds) — absent when an API key is sent |
| `401` / `402` | Invalid key / insufficient balance (only when a key is sent) |

## Pricing

Free keyless (1 req/min). With an `X-Api-Key`: $0.03/request, no rate limit. Details at
[p4p.pro4soft.com](https://p4p.pro4soft.com).

## License

[MIT](LICENSE) © Pro4Soft.
