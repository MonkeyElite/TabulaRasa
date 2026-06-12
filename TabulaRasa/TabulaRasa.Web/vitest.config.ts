import { defineConfig } from "vitest/config";
import path from "node:path";

export default defineConfig({
  root: __dirname,
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "src")
    }
  },
  test: {
    environment: "jsdom",
    globals: true
  }
});
