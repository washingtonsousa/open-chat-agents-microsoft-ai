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

export interface AgentMcpServerSummary {
  id: string;
  name: string;
  kind: McpServerKind;
}

export interface AgentSubAgentSummary {
  id: string;
  name: string;
}

export interface AgentSkillSummary {
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
  mcp_servers: AgentMcpServerSummary[];
  sub_agents: AgentSubAgentSummary[];
  skills: AgentSkillSummary[];
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
  mcp_server_ids: string[];
  sub_agent_ids: string[];
  skill_ids: string[];
}

export type McpAuthType = "none" | "bearer-token" | "header";
export type McpServerKind = "external" | "built-in";

export interface McpServer {
  id: string;
  name: string;
  description: string;
  kind: McpServerKind;
  url: string | null;
  auth_type: McpAuthType;
  auth_header_name: string | null;
  has_secret: boolean;
  built_in_key: string | null;
  created_by: User | null;
  created_at: string;
  updated_at: string;
}

export interface McpServerCreate {
  name: string;
  description?: string;
  url: string;
  auth_type: McpAuthType;
  auth_header_name?: string | null;
  secret?: string | null;
}

export interface McpServerListResponse {
  mcp_servers: McpServer[];
  total: number;
}

export interface Skill {
  id: string;
  name: string;
  description: string;
  content: string;
  created_by: User | null;
  created_at: string;
  updated_at: string;
}

export interface SkillCreate {
  name: string;
  description: string;
  content: string;
}

export interface SkillListResponse {
  skills: Skill[];
  total: number;
}

export interface ConsumerApplication {
  id: string;
  name: string;
  client_id: string;
  is_active: boolean;
  created_by: User | null;
  created_at: string;
}

export interface ConsumerApplicationCreate {
  name: string;
}

export interface ConsumerApplicationCreated {
  id: string;
  name: string;
  client_id: string;
  client_secret: string;
  created_at: string;
}

export interface ConsumerApplicationListResponse {
  consumer_applications: ConsumerApplication[];
  total: number;
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
  image_url: string | null;
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
