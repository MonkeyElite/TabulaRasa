import { describe, expect, it } from "vitest";
import { apiBaseUrl } from "./api";

describe("api client", () => {
  it("uses the local API default", () => {
    expect(apiBaseUrl).toBe("/api");
  });
});
