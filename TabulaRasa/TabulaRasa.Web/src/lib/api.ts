import type { SimulationConfig, SimulationDraft, SimulationDraftSchema, SimulationSnapshot, SimulationStatus } from "@/types/simulation";

const defaultBaseUrl = "/api";

export const apiBaseUrl =
  process.env.NEXT_PUBLIC_TABULARASA_API_URL?.replace(/\/$/, "") ?? defaultBaseUrl;

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...init?.headers
    }
  });

  if (!response.ok) {
    const detail = await response.text();
    throw new Error(detail || `Request failed with ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export const simulationApi = {
  status: () => request<SimulationStatus>("/simulation/status"),
  current: () => request<SimulationSnapshot>("/simulation/current"),
  tick: (tick: number) => request<SimulationSnapshot>(`/simulation/ticks/${tick}`),
  step: () => request<SimulationSnapshot>("/simulation/step", { method: "POST" }),
  run: (intervalMilliseconds: number) =>
    request<SimulationStatus>("/simulation/run", {
      method: "POST",
      body: JSON.stringify({ intervalMilliseconds })
    }),
  pause: () => request<SimulationStatus>("/simulation/pause", { method: "POST" }),
  stop: () => request<SimulationStatus>("/simulation/stop", { method: "POST" }),
  reset: (config?: SimulationConfig) =>
    request<SimulationSnapshot>("/simulation/reset", {
      method: "POST",
      body: JSON.stringify({ config })
    }),
  draft: () => request<SimulationDraft>("/simulation/draft"),
  draftSchema: () => request<SimulationDraftSchema>("/simulation/draft-schema"),
  restartFromDraft: (draft: SimulationDraft) =>
    request<SimulationSnapshot>("/simulation/restart-from-draft", {
      method: "POST",
      body: JSON.stringify(draft)
    })
};
