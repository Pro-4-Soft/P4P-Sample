// P4P-Packing API — JavaScript (Node.js) sample.
//
// Reads a packing request from input.json, POSTs it to /api/pack, and prints a
// readable summary of how the items were packed into containers.
//
// Runs key-free out of the box. Keyless calls are rate-limited to 1 request per
// minute per IP; set an API key below for metered, unlimited use.
//
// Learn more: https://p4p.pro4soft.com

import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

// ── Config ───────────────────────────────────────────────────────────────────
const baseUrl = 'https://p4p.pro4soft.com/api';
const apiKey = '<API_KEY>';   // fill in your own key for metered use; leave as-is to run keyless
const inputFile = 'input.json';
// ─────────────────────────────────────────────────────────────────────────────

console.log('P4P-Packing API — JavaScript sample');
console.log();

const inputPath = join(dirname(fileURLToPath(import.meta.url)), inputFile);

let requestBody = null;
try {
  requestBody = await readFile(inputPath, 'utf8');
} catch {
  // leave requestBody null — handled below
}

if (requestBody === null) {
  console.log(`Could not find '${inputFile}'. Make sure it sits next to the program and try again.`);
} else {
  const headers = { 'Content-Type': 'application/json' };

  // Send the key only when it has been changed from the placeholder, so the
  // committed sample runs keyless as-is.
  if (apiKey !== '<API_KEY>')
    headers['X-Api-Key'] = apiKey;

  console.log(`Packing… (POST ${baseUrl}/pack)`);

  try {
    const response = await fetch(`${baseUrl}/pack`, { method: 'POST', headers, body: requestBody });
    const responseBody = await response.text();
    console.log();

    if (!response.ok) {
      // Error bodies are plain text (or a ProblemDetails JSON on malformed input);
      // either way, surfacing the body gives the clearest message.
      if (response.status === 429) {
        // The server reports how long the window has left; don't assume a duration.
        const retryAfter = response.headers.get('Retry-After');
        console.log(`Rate limited: keyless calls are capped per IP. Retry ${retryAfter ? `in ${retryAfter}s` : 'shortly'}, ` +
                    'or set an API key to remove the limit.');
      } else
        console.log(`Request failed (${response.status} ${response.statusText}): ${responseBody}`);
    } else {
      printResult(responseBody);
    }
  } catch (error) {
    console.log(`Could not reach the API. Network error: ${error.message}`);
  }
}

function printResult(responseBody) {
  const result = JSON.parse(responseBody);
  console.log(`Packed into ${result.containers.length} container(s). Result id: ${result.id}`);

  for (const container of result.containers) {
    console.log();
    console.log(`${container.name}  (${container.length} x ${container.width} x ${container.height} ${container.unit}, ${container.loadingMode})`);
    console.log(`  ${container.utilization}% full, ${container.totalWeight} ${container.weightUnit}`);

    // Each placed entry is a grid block of nx*ny*nz identical units, so the total
    // count of an item type is the sum of that product across all of its blocks.
    const counts = new Map();
    for (const block of container.items) {
      const units = block.nx * block.ny * block.nz;
      counts.set(block.name, (counts.get(block.name) ?? 0) + units);
    }

    for (const [name, count] of [...counts].sort((a, b) => b[1] - a[1]))
      console.log(`  ${String(count).padStart(4)} x ${name}`);
  }

  console.log();
  if (result.unpackedItems?.length) {
    console.log('Did not fit:');
    for (const item of result.unpackedItems)
      console.log(`  ${String(item.quantity).padStart(4)} x ${item.name}`);
  } else {
    console.log('All items packed.');
  }
}
