"""P4P-Packing API — Python sample.

Reads a packing request from input.json, POSTs it to /api/pack, and prints a
readable summary of how the items were packed into containers.

Runs key-free out of the box. Keyless calls are rate-limited to 1 request per
minute per IP; set an API key below for metered, unlimited use.

Learn more: https://p4p.pro4soft.com
"""

import json
import os
import urllib.error
import urllib.request

# ── Config ───────────────────────────────────────────────────────────────────
BASE_URL = "https://p4p.pro4soft.com/api"
API_KEY = "<API_KEY>"   # fill in your own key for metered use; leave as-is to run keyless
INPUT_FILE = "input.json"
# ─────────────────────────────────────────────────────────────────────────────


def main() -> None:
    print("P4P-Packing API — Python sample")
    print()

    input_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), INPUT_FILE)

    if not os.path.exists(input_path):
        print(f"Could not find '{INPUT_FILE}'. Make sure it sits next to the program and try again.")
    else:
        with open(input_path, "rb") as file:
            request_body = file.read()

        headers = {"Content-Type": "application/json"}

        # Send the key only when it has been changed from the placeholder, so the
        # committed sample runs keyless as-is.
        if API_KEY != "<API_KEY>":
            headers["X-Api-Key"] = API_KEY

        print(f"Packing… (POST {BASE_URL}/pack)")
        request = urllib.request.Request(f"{BASE_URL}/pack", data=request_body, headers=headers, method="POST")

        try:
            with urllib.request.urlopen(request) as response:
                response_body = response.read().decode("utf-8")
            print()
            print_result(response_body)
        except urllib.error.HTTPError as error:
            # Error bodies are plain text (or a ProblemDetails JSON on malformed
            # input); either way, surfacing the body gives the clearest message.
            body = error.read().decode("utf-8", errors="replace")
            print()
            if error.code == 429:
                # The server reports how long the window has left; don't assume a duration.
                retry_after = error.headers.get("Retry-After")
                wait = f"in {retry_after}s" if retry_after else "shortly"
                print(f"Rate limited: keyless calls are capped per IP. Retry {wait}, "
                      "or set an API key to remove the limit.")
            else:
                print(f"Request failed ({error.code} {error.reason}): {body}")
        except urllib.error.URLError as error:
            print(f"Could not reach the API. Network error: {error.reason}")


def print_result(response_body: str) -> None:
    result = json.loads(response_body)
    containers = result["containers"]
    print(f"Packed into {len(containers)} container(s). Result id: {result['id']}")

    for container in containers:
        print()
        print(f"{container['name']}  ({container['length']} x {container['width']} x "
              f"{container['height']} {container['unit']}, {container['loadingMode']})")
        print(f"  {container['utilization']}% full, {container['totalWeight']} {container['weightUnit']}")

        # Each placed entry is a grid block of nx*ny*nz identical units, so the
        # total count of an item type is the sum of that product across its blocks.
        counts: dict[str, int] = {}
        for block in container["items"]:
            units = block["nx"] * block["ny"] * block["nz"]
            counts[block["name"]] = counts.get(block["name"], 0) + units

        for name, count in sorted(counts.items(), key=lambda item: item[1], reverse=True):
            print(f"  {count:>4} x {name}")

    print()
    unpacked = result.get("unpackedItems")
    if unpacked:
        print("Did not fit:")
        for item in unpacked:
            print(f"  {item['quantity']:>4} x {item['name']}")
    else:
        print("All items packed.")


if __name__ == "__main__":
    main()
