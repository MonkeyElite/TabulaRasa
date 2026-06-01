import { afterEach, describe, expect, it, vi } from "vitest";
import { apiBaseUrl, simulationApi } from "./api";

describe("api client", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("uses the local API default", () => {
    expect(apiBaseUrl).toBe("/api");
  });

  it("calls the stop endpoint", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse({ status: "Stopped" }));

    await simulationApi.stop();

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/simulation/stop",
      expect.objectContaining({ method: "POST" })
    );
  });

  it("sends config when resetting", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(jsonResponse({ tick: 0 }));

    await simulationApi.reset({
      seed: 42,
      eventHistoryLimit: 2,
      tickIntervalMilliseconds: 250
    });

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/simulation/reset",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({
          config: {
            seed: 42,
            eventHistoryLimit: 2,
            tickIntervalMilliseconds: 250
          }
        })
      })
    );
  });
});

function jsonResponse(body: unknown) {
  return {
    ok: true,
    json: () => Promise.resolve(body)
  } as Response;
}
