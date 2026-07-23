import type {
  Agent,
  AgentCreate,
  AgentListResponse,
  ChatHistoryResponse,
  Message,
  ModelsResponse,
  Session,
  SessionListResponse,
} from "@/types";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://127.0.0.1:8000";
const API = `${BASE_URL}/api/v1`;

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API}${path}`, {
    headers: { "Content-Type": "application/json" },
    ...init,
  });
  if (!res.ok) {
    const error = await res.json().catch(() => ({ detail: res.statusText }));
    throw new Error(error.detail ?? "Erro na requisição");
  }
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export const sessionApi = {
  create: (title = "Nova conversa", agent_id: string | null = null) =>
    request<Session>("/sessions/", {
      method: "POST",
      body: JSON.stringify({ title, agent_id }),
    }),

  list: () => request<SessionListResponse>("/sessions/"),

  delete: (id: string) =>
    request<void>(`/sessions/${id}`, { method: "DELETE" }),
};

export const agentApi = {
  create: (payload: AgentCreate) =>
    request<Agent>("/agents/", {
      method: "POST",
      body: JSON.stringify(payload),
    }),

  list: () => request<AgentListResponse>("/agents/"),

  delete: (id: string) =>
    request<void>(`/agents/${id}`, { method: "DELETE" }),
};

export const modelsApi = {
  listOllama: () => request<ModelsResponse>("/models/ollama"),
  listBedrock: () => request<ModelsResponse>("/models/bedrock"),
};

interface StreamCallbacks {
  onUserMessage: (msg: Message) => void;
  onChunk: (chunk: string) => void;
  onDone: (msg: Message) => void;
}

export const chatApi = {
  stream: async (session_id: string, message: string, callbacks: StreamCallbacks) => {
    const res = await fetch(`${API}/chat/stream`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ session_id, message }),
    });

    if (!res.ok || !res.body) {
      const error = await res.json().catch(() => ({ detail: res.statusText }));
      throw new Error(error.detail ?? "Erro no stream");
    }

    const reader = res.body.getReader();
    const decoder = new TextDecoder();
    let buffer = "";

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });
      const blocks = buffer.split("\n\n");
      buffer = blocks.pop() ?? "";

      for (const block of blocks) {
        const lines = block.split("\n");
        let event = "";
        let data = "";

        for (const line of lines) {
          if (line.startsWith("event: ")) event = line.slice(7).trim();
          else if (line.startsWith("data: ")) data = line.slice(6).trim();
        }

        if (!data) continue;
        const payload = JSON.parse(data) as Record<string, unknown>;

        if (event === "user_message") callbacks.onUserMessage(payload as unknown as Message);
        else if (event === "chunk") callbacks.onChunk(payload.content as string);
        else if (event === "done") callbacks.onDone(payload as unknown as Message);
      }
    }
  },

  history: (session_id: string) =>
    request<ChatHistoryResponse>(`/chat/${session_id}/history`),
};
