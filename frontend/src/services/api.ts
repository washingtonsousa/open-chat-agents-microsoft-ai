import type {
  Agent,
  AgentCreate,
  AgentListResponse,
  ChatHistoryResponse,
  ConsumerApplicationCreate,
  ConsumerApplicationCreated,
  ConsumerApplicationListResponse,
  KbDocument,
  KbDocumentListResponse,
  KnowledgeBase,
  KnowledgeBaseCreate,
  KnowledgeBaseListResponse,
  LoginResponse,
  McpServer,
  McpServerCreate,
  McpServerListResponse,
  Message,
  ModelsResponse,
  Session,
  SessionListResponse,
  User,
  UserCreate,
  UserListResponse,
} from "@/types";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://127.0.0.1:8090";
const API = `${BASE_URL}/api/v1`;
const TOKEN_KEY = "open_chat_agents_token";

export const authStorage = {
  getToken: () => (typeof window === "undefined" ? null : localStorage.getItem(TOKEN_KEY)),
  setToken: (token: string) => localStorage.setItem(TOKEN_KEY, token),
  clearToken: () => localStorage.removeItem(TOKEN_KEY),
};

function authHeaders(): Record<string, string> {
  const token = authStorage.getToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API}${path}`, {
    headers: { "Content-Type": "application/json", ...authHeaders() },
    ...init,
  });

  if (res.status === 401) {
    authStorage.clearToken();
    if (typeof window !== "undefined") window.location.href = "/login";
    throw new Error("Sessão expirada. Faça login novamente.");
  }

  if (!res.ok) {
    const error = await res.json().catch(() => ({ detail: res.statusText }));
    throw new Error(error.detail ?? "Erro na requisição");
  }
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export const authApi = {
  login: (username: string, password: string) =>
    request<LoginResponse>("/auth/login", {
      method: "POST",
      body: JSON.stringify({ username, password }),
    }),

  changePassword: (currentPassword: string, newPassword: string) =>
    request<void>("/auth/change-password", {
      method: "POST",
      body: JSON.stringify({ current_password: currentPassword, new_password: newPassword }),
    }),

  me: () => request<User>("/auth/me"),
};

export const userApi = {
  create: (payload: UserCreate) =>
    request<User>("/users", { method: "POST", body: JSON.stringify(payload) }),

  list: () => request<UserListResponse>("/users"),

  delete: (id: string) => request<void>(`/users/${id}`, { method: "DELETE" }),
};

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

  update: (id: string, payload: Partial<AgentCreate>) =>
    request<Agent>(`/agents/${id}`, {
      method: "PUT",
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

export const knowledgeBaseApi = {
  create: (payload: KnowledgeBaseCreate) =>
    request<KnowledgeBase>("/knowledge-bases", {
      method: "POST",
      body: JSON.stringify(payload),
    }),

  list: () => request<KnowledgeBaseListResponse>("/knowledge-bases"),

  get: (id: string) => request<KnowledgeBase>(`/knowledge-bases/${id}`),

  delete: (id: string) => request<void>(`/knowledge-bases/${id}`, { method: "DELETE" }),

  uploadDocument: async (id: string, file: File): Promise<KbDocument> => {
    const form = new FormData();
    form.append("file", file);
    const res = await fetch(`${API}/knowledge-bases/${id}/documents`, {
      method: "POST",
      headers: { ...authHeaders() },
      body: form,
    });
    if (!res.ok) {
      const error = await res.json().catch(() => ({ detail: res.statusText }));
      throw new Error(error.detail ?? "Erro no upload");
    }
    return res.json() as Promise<KbDocument>;
  },

  listDocuments: (id: string) =>
    request<KbDocumentListResponse>(`/knowledge-bases/${id}/documents`),
};

export const mcpServerApi = {
  create: (payload: McpServerCreate) =>
    request<McpServer>("/mcp-servers", { method: "POST", body: JSON.stringify(payload) }),

  update: (id: string, payload: Partial<McpServerCreate>) =>
    request<McpServer>(`/mcp-servers/${id}`, { method: "PUT", body: JSON.stringify(payload) }),

  list: () => request<McpServerListResponse>("/mcp-servers"),

  get: (id: string) => request<McpServer>(`/mcp-servers/${id}`),

  delete: (id: string) => request<void>(`/mcp-servers/${id}`, { method: "DELETE" }),
};

export const consumerApplicationApi = {
  create: (payload: ConsumerApplicationCreate) =>
    request<ConsumerApplicationCreated>("/consumer-applications", {
      method: "POST",
      body: JSON.stringify(payload),
    }),

  list: () => request<ConsumerApplicationListResponse>("/consumer-applications"),

  delete: (id: string) => request<void>(`/consumer-applications/${id}`, { method: "DELETE" }),
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
      headers: { "Content-Type": "application/json", ...authHeaders() },
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
