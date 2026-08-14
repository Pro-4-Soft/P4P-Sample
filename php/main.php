<?php

// P4P-Packing API — PHP sample.
//
// Reads a packing request from input.json, POSTs it to /api/pack, and prints a
// readable summary of how the items were packed into containers.
//
// Runs key-free out of the box. Keyless calls are rate-limited to 1 request per
// minute per IP; set an API key below for metered, unlimited use.
//
// Learn more: https://p4p.pro4soft.com

// ── Config ───────────────────────────────────────────────────────────────────
$baseUrl = 'https://p4p.pro4soft.com/api';
$apiKey = '<API_KEY>';   // fill in your own key for metered use; leave as-is to run keyless
$inputFile = 'input.json';
// ─────────────────────────────────────────────────────────────────────────────

echo "P4P-Packing API — PHP sample\n\n";

$inputPath = __DIR__ . DIRECTORY_SEPARATOR . $inputFile;

if (!file_exists($inputPath)) {
    echo "Could not find '$inputFile'. Make sure it sits next to the program and try again.\n";
} else {
    $requestBody = file_get_contents($inputPath);

    $headers = ['Content-Type: application/json'];

    // Send the key only when it has been changed from the placeholder, so the
    // committed sample runs keyless as-is.
    if ($apiKey !== '<API_KEY>') {
        $headers[] = "X-Api-Key: $apiKey";
    }

    echo "Packing… (POST $baseUrl/pack)\n";

    $curl = curl_init("$baseUrl/pack");
    curl_setopt($curl, CURLOPT_POST, true);
    curl_setopt($curl, CURLOPT_POSTFIELDS, $requestBody);
    curl_setopt($curl, CURLOPT_HTTPHEADER, $headers);
    curl_setopt($curl, CURLOPT_RETURNTRANSFER, true);

    // Capture Retry-After so a rate-limited run can report the real wait.
    $retryAfter = null;
    curl_setopt($curl, CURLOPT_HEADERFUNCTION, function ($curl, $header) use (&$retryAfter) {
        if (stripos($header, 'Retry-After:') === 0) {
            $retryAfter = trim(substr($header, strlen('Retry-After:')));
        }
        return strlen($header);   // cURL treats any other return value as an error
    });

    $responseBody = curl_exec($curl);
    $status = curl_getinfo($curl, CURLINFO_RESPONSE_CODE);
    $networkError = curl_error($curl);
    curl_close($curl);

    echo "\n";

    if ($responseBody === false) {
        echo "Could not reach the API. Network error: $networkError\n";
    } elseif ($status < 200 || $status >= 300) {
        // Error bodies are plain text (or a ProblemDetails JSON on malformed
        // input); either way, surfacing the body gives the clearest message.
        if ($status === 429) {
            // The server reports how long the window has left; don't assume a duration.
            $wait = $retryAfter !== null ? "in {$retryAfter}s" : 'shortly';
            echo "Rate limited: keyless calls are capped per IP. Retry $wait, "
               . "or set an API key to remove the limit.\n";
        } else {
            echo "Request failed ($status): $responseBody\n";
        }
    } else {
        print_result($responseBody);
    }
}

function print_result(string $responseBody): void
{
    $result = json_decode($responseBody, true);
    $containers = $result['containers'];
    echo 'Packed into ' . count($containers) . " container(s). Result id: {$result['id']}\n";

    foreach ($containers as $container) {
        echo "\n";
        printf("%s  (%s x %s x %s %s, %s)\n",
            $container['name'], $container['length'], $container['width'], $container['height'],
            $container['unit'], $container['loadingMode']);
        printf("  %s%% full, %s %s\n",
            $container['utilization'], $container['totalWeight'], $container['weightUnit']);

        // Each placed entry is a grid block of nx*ny*nz identical units, so the
        // total count of an item type is the sum of that product across its blocks.
        $counts = [];
        foreach ($container['items'] as $block) {
            $units = $block['nx'] * $block['ny'] * $block['nz'];
            $counts[$block['name']] = ($counts[$block['name']] ?? 0) + $units;
        }

        arsort($counts);
        foreach ($counts as $name => $count) {
            printf("  %4d x %s\n", $count, $name);
        }
    }

    echo "\n";
    $unpacked = $result['unpackedItems'] ?? null;
    if (!empty($unpacked)) {
        echo "Did not fit:\n";
        foreach ($unpacked as $item) {
            printf("  %4d x %s\n", $item['quantity'], $item['name']);
        }
    } else {
        echo "All items packed.\n";
    }
}
