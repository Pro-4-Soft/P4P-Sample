// P4P-Packing API — Java sample.
//
// Reads a packing request from input.json, POSTs it to /api/pack, and prints a
// readable summary of how the items were packed into containers.
//
// Runs key-free out of the box. Keyless calls are rate-limited to 1 request per
// minute per IP; set an API key below for metered, unlimited use.
//
// Learn more: https://p4p.pro4soft.com

import com.google.gson.Gson;
import com.google.gson.JsonArray;
import com.google.gson.JsonObject;

import java.io.IOException;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.LinkedHashMap;
import java.util.Map;

public class PackingSample {

    // ── Config ───────────────────────────────────────────────────────────────
    static final String BASE_URL = "https://p4p.pro4soft.com/api";
    static final String API_KEY = "<API_KEY>";   // fill in your own key for metered use; leave as-is to run keyless
    static final String INPUT_FILE = "input.json";
    // ──────────────────────────────────────────────────────────────────────────

    public static void main(String[] args) {
        System.out.println("P4P-Packing API — Java sample");
        System.out.println();

        Path inputPath = Path.of(INPUT_FILE);

        if (!Files.exists(inputPath)) {
            System.out.println("Could not find '" + INPUT_FILE + "'. Make sure it sits next to the program and try again.");
        } else {
            try {
                String requestBody = Files.readString(inputPath);

                HttpRequest.Builder request = HttpRequest.newBuilder()
                    .uri(URI.create(BASE_URL + "/pack"))
                    .header("Content-Type", "application/json")
                    .POST(HttpRequest.BodyPublishers.ofString(requestBody));

                // Send the key only when it has been changed from the placeholder,
                // so the committed sample runs keyless as-is.
                if (!API_KEY.equals("<API_KEY>"))
                    request.header("X-Api-Key", API_KEY);

                System.out.println("Packing… (POST " + BASE_URL + "/pack)");

                HttpResponse<String> response = HttpClient.newHttpClient()
                    .send(request.build(), HttpResponse.BodyHandlers.ofString());
                System.out.println();

                if (response.statusCode() < 200 || response.statusCode() >= 300) {
                    // Error bodies are plain text (or a ProblemDetails JSON on
                    // malformed input); surfacing the body gives the clearest message.
                    if (response.statusCode() == 429) {
                        // The server reports how long the window has left; don't assume a duration.
                        String wait = response.headers().firstValue("retry-after")
                            .map(seconds -> "in " + seconds + "s").orElse("shortly");
                        System.out.println("Rate limited: keyless calls are capped per IP. Retry " + wait
                            + ", or set an API key to remove the limit.");
                    } else
                        System.out.println("Request failed (" + response.statusCode() + "): " + response.body());
                } else {
                    printResult(response.body());
                }
            } catch (IOException | InterruptedException error) {
                System.out.println("Could not reach the API. Network error: " + error.getMessage());
            }
        }
    }

    static void printResult(String responseBody) {
        JsonObject result = new Gson().fromJson(responseBody, JsonObject.class);
        JsonArray containers = result.getAsJsonArray("containers");
        System.out.println("Packed into " + containers.size() + " container(s). Result id: " + result.get("id").getAsString());

        for (var element : containers) {
            JsonObject container = element.getAsJsonObject();
            System.out.println();
            System.out.printf("%s  (%s x %s x %s %s, %s)%n",
                container.get("name").getAsString(),
                container.get("length").getAsString(), container.get("width").getAsString(),
                container.get("height").getAsString(), container.get("unit").getAsString(),
                container.get("loadingMode").getAsString());
            System.out.printf("  %s%% full, %s %s%n",
                container.get("utilization").getAsString(),
                container.get("totalWeight").getAsString(),
                container.get("weightUnit").getAsString());

            // Each placed entry is a grid block of nx*ny*nz identical units, so the
            // total count of an item type is the sum of that product across its blocks.
            Map<String, Integer> counts = new LinkedHashMap<>();
            for (var item : container.getAsJsonArray("items")) {
                JsonObject block = item.getAsJsonObject();
                int units = block.get("nx").getAsInt() * block.get("ny").getAsInt() * block.get("nz").getAsInt();
                counts.merge(block.get("name").getAsString(), units, Integer::sum);
            }

            counts.entrySet().stream()
                .sorted((a, b) -> Integer.compare(b.getValue(), a.getValue()))
                .forEach(entry -> System.out.printf("  %4d x %s%n", entry.getValue(), entry.getKey()));
        }

        System.out.println();
        JsonArray unpacked = result.getAsJsonArray("unpackedItems");
        if (unpacked != null && unpacked.size() > 0) {
            System.out.println("Did not fit:");
            for (var item : unpacked) {
                JsonObject block = item.getAsJsonObject();
                System.out.printf("  %4d x %s%n", block.get("quantity").getAsInt(), block.get("name").getAsString());
            }
        } else {
            System.out.println("All items packed.");
        }
    }
}
