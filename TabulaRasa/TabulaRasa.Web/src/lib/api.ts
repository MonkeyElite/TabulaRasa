import type {
  SimulationConfig,
  SimulationDraft,
  SimulationDraftSchema,
  SimulationCheckpointSummary,
  SimulationResourceLimits,
  SimulationRunPage,
  SimulationSnapshot,
  SimulationStatus,
  SimulationSummary,
  SaveSimulationResponse,
  ScenarioExport,
  RetentionResult
} from "@/types/simulation";

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

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export const simulationApi = {
  list: () => request<SimulationSummary[]>("/simulations"),
  runs: (offset = 0, limit = 50) => request<SimulationRunPage>(`/simulations/runs?offset=${offset}&limit=${limit}`),
  checkpoints: (runId: string) => request<SimulationCheckpointSummary[]>(`/simulations/runs/${runId}/checkpoints`),
  loadRun: (runId: string) =>
    request<SimulationSummary>(`/simulations/runs/${runId}/load`, { method: "POST" }),
  forkRun: (runId: string, requestBody: { name?: string; sourceTick?: number } = {}) =>
    request<SimulationSummary>(`/simulations/runs/${runId}/fork`, {
      method: "POST",
      body: JSON.stringify(requestBody)
    }),
  applyRetention: () =>
    request<RetentionResult>("/simulations/storage/retention/apply", { method: "POST" }),
  resourceLimits: () => request<SimulationResourceLimits>("/simulations/resource-limits"),
  create: (requestBody: { name?: string; config?: SimulationConfig }) =>
    request<SimulationSummary>("/simulations", {
      method: "POST",
      body: JSON.stringify(requestBody)
    }),
  clone: (simulationId: string, requestBody: { name?: string; sourceTick?: number } = {}) =>
    request<SimulationSummary>(`/simulations/${simulationId}/clone`, {
      method: "POST",
      body: JSON.stringify(requestBody)
    }),
  delete: (simulationId: string) =>
    request<void>(`/simulations/${simulationId}`, { method: "DELETE" }),
  status: (simulationId: string) => request<SimulationStatus>(`/simulations/${simulationId}/status`),
  current: (simulationId: string) => request<SimulationSnapshot>(`/simulations/${simulationId}/current`),
  tick: (simulationId: string, tick: number) => request<SimulationSnapshot>(`/simulations/${simulationId}/ticks/${tick}`),
  step: (simulationId: string) => request<SimulationSnapshot>(`/simulations/${simulationId}/step`, { method: "POST" }),
  save: (simulationId: string) =>
    request<SaveSimulationResponse>(`/simulations/${simulationId}/save`, { method: "POST" }),
  exportScenario: (simulationId: string) =>
    request<ScenarioExport>(`/simulations/${simulationId}/export-scenario`),
  importScenario: (requestBody: { name?: string; scenario: SimulationDraft }) =>
    request<SimulationSummary>("/simulations/import-scenario", {
      method: "POST",
      body: JSON.stringify(requestBody)
    }),
  run: (simulationId: string, intervalMilliseconds: number) =>
    request<SimulationStatus>(`/simulations/${simulationId}/run`, {
      method: "POST",
      body: JSON.stringify({ intervalMilliseconds })
    }),
  pause: (simulationId: string) => request<SimulationStatus>(`/simulations/${simulationId}/pause`, { method: "POST" }),
  stop: (simulationId: string) => request<SimulationStatus>(`/simulations/${simulationId}/stop`, { method: "POST" }),
  reset: (simulationId: string, config?: SimulationConfig) =>
    request<SimulationSnapshot>(`/simulations/${simulationId}/reset`, {
      method: "POST",
      body: JSON.stringify({ config })
    }),
  updateConfig: (simulationId: string, config: SimulationConfig) =>
    request<SimulationStatus>(`/simulations/${simulationId}/config`, {
      method: "POST",
      body: JSON.stringify({ config })
    }),
  draft: (simulationId: string) => request<SimulationDraft>(`/simulations/${simulationId}/draft`),
  draftSchema: (simulationId: string) => request<SimulationDraftSchema>(`/simulations/${simulationId}/draft-schema`),
  restartFromDraft: (simulationId: string, draft: SimulationDraft) =>
    request<SimulationSnapshot>(`/simulations/${simulationId}/restart-from-draft`, {
      method: "POST",
      body: JSON.stringify(draft)
    })
};
