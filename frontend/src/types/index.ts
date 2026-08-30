export type LLMProvider = "ollama" | "bedrock";

export interface User {
  id: string;
  username: string;
  is_admin: boolean;
  must_change_password: boolean;
  created_at: string;
}

export interface UserCreate {
  username: string;
  password: string;
  is_admin: boolean;
}

export interface UserListResponse {
  users: User[];
  total: number;
}

export interface LoginResponse {
  access_token: string;
  must_change_password: boolean;
  user: User;
}

export type KnowledgeBaseStatus = "empty" | "processing" | "ready" | "failed";

export interface KnowledgeBase {
  id: string;
  name: string;
  description: string;
  chunk_size: number;
  chunk_overlap: number;
  embedding_provider: LLMProvider;
  embedding_model: string;
  embedding_dimensions: number;
  status: KnowledgeBaseStatus;
  document_count: number;
  created_by: User | null;
  created_at: string;
  updated_at: string;
}

export interface KnowledgeBaseCreate {
  name: string;
  description: string;
  chunk_size: number;
  chunk_overlap: number;
  embedding_provider: LLMProvider;
  embedding_model: string;
}

export interface KnowledgeBaseListResponse {
  knowledge_bases: KnowledgeBase[];
  total: number;
}

export type KbDocumentStatus = "uploaded" | "processing" | "completed" | "failed";

export interface KbDocument {
  id: string;
  knowledge_base_id: string;
  file_name: string;
  content_type: string;
  size_bytes: number;
  status: KbDocumentStatus;
  error_message: string | null;
  chunk_count: number;
  created_at: string;
  processed_at: string | null;
}

export interface KbDocumentListResponse {
  documents: KbDocument[];
}

export interface AgentKnowledgeBaseSummary {
  id: string;
  name: string;
}

export interface Agent {
  id: string;
  name: string;
  provider: LLMProvider;
  llm_model: string;
  temperature: number;
  max_tokens: number | null;
  system_prompt: string;
  created_by: User | null;
  knowledge_bases: AgentKnowledgeBaseSummary[];
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
  knowledge_base_ids: string[];
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
