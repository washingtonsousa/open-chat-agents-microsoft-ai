export type LLMProvider = "ollama" | "bedrock";

export interface Agent {
  id: string;
  name: string;
  provider: LLMProvider;
  llm_model: string;
  temperature: number;
  max_tokens: number | null;
  system_prompt: string;
  created_at: string;
  updated_at: string;
}

export interface AgentCreate {
  name: string;
  provider: LLMProvider;
  llm_model: string;
  temperature: number;
  max_tokens: number | null;
  system_prompt: string;
}

export interface AgentListResponse {
  agents: Agent[];
  total: number;
}

export interface ModelInfo {
  name: string;
  provider: string | null;
  size: number | null;
}

export interface ModelsResponse {
  models: ModelInfo[];
}

export interface Session {
  id: string;
  title: string;
  agent_id: string | null;
  agent: Agent | null;
  created_at: string;
  updated_at: string;
}

export interface Message {
  id: string;
  session_id: string;
  role: "user" | "assistant";
  content: string;
  created_at: string;
}

export interface ChatHistoryResponse {
  session_id: string;
  messages: Message[];
}

export interface SessionListResponse {
  sessions: Session[];
  total: number;
}
