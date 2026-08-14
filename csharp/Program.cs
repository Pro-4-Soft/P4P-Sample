// P4P-Packing API — C# sample.
//
// Reads a packing request from input.json, POSTs it to /api/pack, and prints a
// readable summary of how the items were packed into containers.
//
// Runs key-free out of the box. Keyless calls are rate-limited to 1 request per
// minute per IP; set an API key below for metered, unlimited use.

using System.Net;
using System.Text;
using System.Text.Json;

// ── Config ───────────────────────────────────────────────────────────────────
// BaseUrl ends at "/api"; endpoints are appended (e.g. "/pack").
const string baseUrl = "https://p4p.pro4soft.com/api";

// Fill in your own key for metered use. Leave it as the placeholder to run keyless.
const string apiKey = "<API_KEY>";

const string inputFile = "input.json";
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine("P4P-Packing API — C# sample");
Console.WriteLine();

var inputPath = Path.Combine(AppContext.BaseDirectory, inputFile);

if (!File.Exists(inputPath))
{
    Console.WriteLine($"Could not find '{inputFile}'. Make sure it sits next to the program and try again.");
}
else
{
    var requestBody = await File.ReadAllTextAsync(inputPath);

    using var http = new HttpClient();

    // Send the key only when it has been changed from the placeholder, so the
    // committed sample runs keyless as-is.
    if (apiKey != "<API_KEY>")
        http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

    using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

    Console.WriteLine($"Packing… (POST {baseUrl}/pack)");

    try
    {
        var response = await http.PostAsync($"{baseUrl}/pack", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine();

        if (!response.IsSuccessStatusCode)
        {
            // Error bodies are plain text (or a ProblemDetails JSON on malformed
            // input); either way, surfacing the body gives the clearest message.
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                // The server reports how long the window has left; don't assume a duration.
                var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds;
                var wait = retryAfter is null ? "shortly" : $"in {retryAfter:0}s";
                Console.WriteLine($"Rate limited: keyless calls are capped per IP. Retry {wait}, " +
                                  "or set an API key to remove the limit.");
            }
            else
                Console.WriteLine($"Request failed ({(int)response.StatusCode} {response.StatusCode}): {responseBody}");
        }
        else
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<PackResponse>(responseBody, options);

            if (result is null)
            {
                Console.WriteLine("The server returned an empty or unreadable response.");
            }
            else
            {
                Console.WriteLine($"Packed into {result.Containers.Count} container(s). Result id: {result.Id}");

                foreach (var container in result.Containers)
                {
                    Console.WriteLine();
                    Console.WriteLine($"{container.Name}  ({container.Length} x {container.Width} x {container.Height} {container.Unit}, {container.LoadingMode})");
                    Console.WriteLine($"  {container.Utilization}% full, {container.TotalWeight} {container.WeightUnit}");

                    // Each placed entry is a grid block of nx*ny*nz identical units,
                    // so the total count of an item type is the sum of that product
                    // across all of its blocks in this container.
                    var counts = container.Items
                        .GroupBy(item => item.Name)
                        .Select(group => new { Name = group.Key, Count = group.Sum(b => b.Nx * b.Ny * b.Nz) })
                        .OrderByDescending(x => x.Count);

                    foreach (var entry in counts)
                        Console.WriteLine($"  {entry.Count,4} x {entry.Name}");
                }

                Console.WriteLine();
                if (result.UnpackedItems is { Count: > 0 })
                {
                    Console.WriteLine("Did not fit:");
                    foreach (var item in result.UnpackedItems)
                        Console.WriteLine($"  {item.Quantity,4} x {item.Name}");
                }
                else
                {
                    Console.WriteLine("All items packed.");
                }
            }
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Could not reach the API. Network error: {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine("Done. Press Enter to exit.");
Console.In.ReadLine();

// ── Response models ──────────────────────────────────────────────────────────
// Enums (unit, weightUnit, loadingMode) arrive as strings on the wire and are
// kept as strings here — the sample only displays them.

record PackResponse(
    string Id,
    DateTime CreatedAt,
    List<PackedContainer> Containers,
    List<UnpackedItem>? UnpackedItems);

record PackedContainer(
    string Id,
    string Name,
    decimal Length,
    decimal Width,
    decimal Height,
    string Unit,
    string LoadingMode,
    List<PackedBlock> Items,
    string WeightUnit,
    decimal TotalWeight,
    decimal Utilization,
    decimal Cost);

// A block of nx*ny*nz identical units, each dx*dy*dz, with its corner at (x, y, z).
record PackedBlock(
    string Name,
    string? Color,
    decimal X, decimal Y, decimal Z,
    decimal Dx, decimal Dy, decimal Dz,
    int Nx, int Ny, int Nz);

record UnpackedItem(string Name, int Quantity);
