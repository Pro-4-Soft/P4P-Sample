# P4P-Packing API — PHP Sample

[![License: MIT](https://img.shields.io/badge/License-MIT-44883e.svg)](../LICENSE)
[![PHP 8+](https://img.shields.io/badge/PHP-8%2B-777BB4.svg)](https://www.php.net)

PHP client for the [P4P-Packing API](https://p4p.pro4soft.com) (REST 3D bin-packing). Reads a
request from `input.json`, calls `POST /api/pack`, and prints the result. Uses the standard `cURL`
and `json` extensions — no Composer, no dependencies.

The bundled `input.json` packs five grocery-case types onto two GMA pallets.

## Requirements

- [PHP 8.0+](https://www.php.net) with the cURL extension (bundled with most builds)

## Run

```bash
cd php
php main.php
```

Runs keyless. Keyless `POST /api/pack` is rate-limited per IP (currently 1 request/minute); on
`429` the sample reports the `Retry-After` wait and exits.

## Edit the request

`input.json` is sent verbatim as the request body — change items, containers, weights, or
`optimizeBy` without touching code. Field reference: [root README](../README.md#the-packing-request).

## API key (optional)

Removes the rate limit and enables metered use ($0.03/request). In `main.php`:

```php
$apiKey = 'your-key-here';
```

The `X-Api-Key` header is sent only when the key differs from the `'<API_KEY>'` placeholder, so the
committed code stays keyless.

## Output

```text
P4P-Packing API — PHP sample

Packing… (POST https://p4p.pro4soft.com/api/pack)

Packed into 2 container(s). Result id: 665e1f2a9c1d4b00123abcd0

GMA Pallet 60"  (40 x 48 x 60 in, topDown)
  86.4% full, 3120 lb
    40 x Case of Water
    40 x Case of Soda
    30 x Case of Beer

GMA Pallet 48"  (40 x 48 x 48 in, topDown)
  79.1% full, 1620 lb
    30 x Detergent Case
    24 x Produce Crate

All items packed.
```

Numbers vary with the input. The `Result id` works with `GET /api/pack/{id}` and
`GET /api/container/{id}/svg`.

## Links

- [API docs](https://p4p.pro4soft.com)
- [Samples in other languages](../README.md) — C#, JavaScript, Python, Java, PHP
