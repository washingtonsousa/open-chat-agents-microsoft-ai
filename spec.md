# Especificação técnica — Open Chat Agents (.NET)

> Documento de referência completo do projeto: objetivo, stack, arquitetura, estrutura de pastas, modelo de dados, API, convenções e infraestrutura. Para instruções operacionais rápidas (comandos, gotchas) veja [CLAUDE.md](CLAUDE.md); para quickstart e visão geral orientada a quem chega no repo, veja [README.md](README.md).

---

## 1. Objetivo

Uma plataforma de chat com agentes de IA configuráveis, com RAG (Retrieval-Augmented Generation) de verdade, ferramentas externas via MCP (Model Context Protocol), observabilidade de LLM de ponta a ponta, e um modelo de acesso que cobre tanto usuários humanos (login/senha) quanto integrações programáticas (client_id/client_secret).

É um **porte .NET** do POC Python `open-chat-agents` (`../Chat Bot Com Rag e Front`), reescrito sobre **ASP.NET Core 10** + **Microsoft Agent Framework** no lugar de FastAPI + LangChain/LangGraph, e estendido com capacidades que o projeto original nunca teve:

- Bases de conhecimento (RAG) com upload de documentos, chunking configurável, embeddings e busca vetorial.
- Autenticação com login/senha (JWT + Argon2id) e uma segunda via de acesso para aplicações externas (OAuth2 client-credentials).
- Um worker dedicado, orientado a eventos, para o pipeline de ingestão de documentos.
- Servidores MCP cadastráveis e anexáveis a agentes como ferramentas.
- Um painel inicial (dashboard) com visão geral e ações rápidas.
- Observabilidade de agente/LLM (tracing OpenTelemetry → Langfuse self-hosted).
- Frontend em Material Design (MUI) no lugar de Tailwind puro.

---

## 2. Stack tecnológica

| Camada | Tecnologia | Versão |
|---|---|---|
| Frontend | Next.js (App Router) | 15.1.0 |
| Frontend | React | 19 |
| Frontend | MUI (Material-UI) | 9.4.0 (`@mui/material`, `@mui/icons-material`, `@mui/material-nextjs`) |
| Frontend | Emotion (motor de estilo do MUI) | 11.14.x |
| Frontend | Tailwind CSS | 3.4.1 (instalado, não é mais o principal) |
| Frontend | react-markdown + remark-gfm + react-syntax-highlighter | render de markdown/código nas respostas do chat |
| Backend / Worker | ASP.NET Core / .NET | 10 |
| Orquestração de LLM | Microsoft Agent Framework (`Microsoft.Agents.AI`) | 1.15.0 |
| Abstrações de IA | `Microsoft.Extensions.AI.Abstractions` | 10.8.0 |
| LLM / Embeddings | Ollama, via `OllamaSharp` | 5.4.27 |
| LLM / Embeddings | AWS Bedrock, via `AWSSDK.Extensions.Bedrock.MEAI` | 4.0.101.6 |
| Ferramentas externas | MCP (Model Context Protocol), via `ModelContextProtocol.Core` | 2.2.0 |
| Banco relacional | PostgreSQL | 16 (app), 17 (Langfuse) |
| ORM | Entity Framework Core + Npgsql | 10.0.10 / 10.0.3 |
| Banco vetorial | Weaviate, via `Microsoft.Extensions.VectorData` + `Weaviate.Client.VectorData` | Weaviate 1.39.2, client 1.2.0, abstractions 10.0.1 |
| Armazenamento de arquivos | MinIO | — |
| Mensageria / eventos | RabbitMQ (via *bucket notifications* nativas do MinIO) | `RabbitMQ.Client` 7.2.2 |
| Hash de senha / client secret | Argon2id, via `Konscious.Security.Cryptography.Argon2` | 1.3.1 |
| Criptografia de segredos MCP | ASP.NET Core Data Protection API | — |
| Auth | JWT bearer (`Microsoft.AspNetCore.Authentication.JwtBearer` + `System.IdentityModel.Tokens.Jwt`) | 10.0.11 / 8.19.2 |
| Extração de texto | PdfPig (PDF), `DocumentFormat.OpenXml` (DOCX), leitura direta (texto/markdown) | 0.1.16 / 3.5.1 |
| Observabilidade | OpenTelemetry .NET SDK → OTLP/HTTP → Langfuse self-hosted | 1.18.x |
| Infra local | Docker Compose | — |

---

## 3. Arquitetura

### 3.1 Backend — camadas DDD-ish

Cinco projetos .NET, com dependências apontando sempre para dentro: `Api`/`Worker` → `Infrastructure` + `Application` → `Domain`. `Infrastructure` e `Application` nunca se referenciam entre si — as duas só dependem de `Domain`, que não tem nenhuma dependência de infraestrutura concreta (só `Microsoft.Extensions.AI.Abstractions`, que é puramente tipos/interfaces).

- **`OpenChatAgents.Domain`** — o núcleo. Entidades, POCOs de configuração (`AppOptions`), interfaces de repositório, interfaces de infraestrutura ("portas": `IChatAgentFactory`, `IEmbeddingClientFactory`, `IObjectStore`, `IPasswordHasher`, `ITextExtractor`, `ISecretProtector`, `IMcpToolFactory`, `IKbVectorStore`), e serviços de domínio puros (`ModerationService`, `ChunkingService`) sem nenhuma dependência externa.
- **`OpenChatAgents.Application`** — os casos de uso. Um serviço por área de negócio (`AgentService`, `SessionService`, `ChatService`, `AuthService`, `UserService`, `KnowledgeBaseService`, `KbRetrievalService`, `KbIngestionService`, `McpServerService`, `ConsumerApplicationService`), os DTOs de contrato da API (`Dtos/`, sempre com o trio `*Create`/`*Update`/`*Response` + `*ListResponse`), e `ApiException` (erro de negócio com status HTTP associado). Depende só de `Domain`.
- **`OpenChatAgents.Infrastructure`** — as implementações concretas de cada porta do `Domain`: EF Core (`Data/`, `Persistence/`), Weaviate (`VectorStore/`), MinIO (`Storage/`), RabbitMQ (`Messaging/`), Argon2 (`Security/Argon2PasswordHasher`), Data Protection (`Security/DataProtectionSecretProtector`), Ollama/Bedrock (`Agents/`), MCP (`Mcp/McpToolFactory`), extração de texto (`Ingestion/TextExtractor`), e o wiring do OpenTelemetry (`Telemetry/TelemetryExtensions`).
- **`OpenChatAgents.Api`** — Controllers + `Program.cs`. É o *composition root*: o único lugar que liga interface do `Domain` → implementação do `Infrastructure` via injeção de dependência.
- **`OpenChatAgents.Worker`** — um único `BackgroundService` (`KbIngestionBackgroundService`) que consome RabbitMQ e delega para `KbIngestionService` (Application); mais seu próprio `Program.cs`/DI.

Regra prática: mexer numa entidade, interface de repositório ou porta de infra → sempre em `Domain`. Mexer em como algo é persistido ou em qual SDK é chamado → `Infrastructure`. Mexer na orquestração de um caso de uso → `Application`. Os cinco projetos precisam ser rebuildados/redeployados juntos sempre que qualquer um muda — não há versionamento independente entre camadas.

### 3.2 Frontend

Next.js App Router, cada página monta sua própria composição (`AuthGuard` + `Drawer`/nav + conteúdo) — não existe um layout compartilhado único; o padrão se repete por página em vez de uma abstração de layout genérica.

```
frontend/src/
├── app/
│   ├── page.tsx                          Dashboard (tela inicial, "/")
│   ├── chat/page.tsx                     Chat (sidebar de agentes/conversas + janela de chat)
│   ├── login/page.tsx
│   ├── knowledge-bases/page.tsx          CRUD de KBs + upload/status de documentos
│   ├── mcp-servers/page.tsx              CRUD de servidores MCP
│   ├── admin/users/page.tsx              CRUD de usuários (admin only)
│   ├── admin/consumer-applications/page.tsx  CRUD de aplicações consumidoras (admin only)
│   └── layout.tsx                        AppRouterCacheProvider + ThemeProvider + CssBaseline
├── components/
│   ├── agent/          AgentForm, AgentModal
│   ├── auth/            AuthGuard (contexto de usuário logado + guard de rota), ChangePasswordModal
│   ├── chat/              ChatWindow, ChatInput, MessageBubble, SessionSidebar
│   ├── knowledge-base/     KnowledgeBaseForm/Modal, KbMultiSelect, KbDetailPanel
│   ├── mcp-server/           McpServerForm/Modal, McpServerMultiSelect
│   └── session/                 NewSessionModal
├── services/api.ts        cliente HTTP único: um objeto por recurso (`agentApi`, `knowledgeBaseApi`,
│                           `mcpServerApi`, `consumerApplicationApi`, `sessionApi`, `chatApi`, `userApi`, `authApi`)
├── types/index.ts          tipos TS espelhando o contrato JSON (snake_case) do backend
└── theme.ts                 tema MUI (Material Design 3)
```

---

## 4. Estrutura de pastas (árvore completa)

```
open-chat-agents-dotnet/
├── README.md                 quickstart, tabela de stack, referência de API, variáveis de ambiente
├── CLAUDE.md                  guia operacional para trabalhar no repo (comandos, gotchas)
├── spec.md                     este documento
├── docker-compose.yml           orquestração completa (app + Langfuse)
├── backend/
│   ├── Dockerfile                imagem da Api
│   ├── Dockerfile.worker          imagem do Worker
│   ├── OpenChatAgents.slnx
│   └── src/
│       ├── OpenChatAgents.Domain/
│       │   ├── Models/            Agent, AgentKnowledgeBase, AgentMcpServer, ConsumerApplication,
│       │   │                      KbChunkRef, KbDocument, KbDocumentStatus, KnowledgeBase, LlmProvider,
│       │   │                      McpAuthType, McpServer, Message, MessageRole, Session, User
│       │   ├── Options/           AppOptions (+ Jwt/Minio/RabbitMq/Weaviate/Argon2/Aws/Telemetry)
│       │   ├── Repositories/      IAgentRepository, IConsumerApplicationRepository, IKbDocumentRepository,
│       │   │                      IKnowledgeBaseRepository, IMcpServerRepository, IMessageRepository,
│       │   │                      ISessionRepository, IUserRepository
│       │   ├── Abstractions/      IChatAgentFactory, IEmbeddingClientFactory, IMcpToolFactory,
│       │   │                      IObjectStore, IPasswordHasher, ISecretProtector, ITextExtractor
│       │   ├── VectorStore/       IKbVectorStore (+ KbChunkRecord/KbSearchResult)
│       │   ├── Services/          ChunkingService, ModerationService
│       │   └── Telemetry/         AppActivitySource
│       ├── OpenChatAgents.Application/
│       │   ├── Dtos/              AgentDtos, AuthDtos, ChatDtos, ConsumerApplicationDtos,
│       │   │                      KnowledgeBaseDtos, McpServerDtos, SessionDtos, UserDtos
│       │   ├── Exceptions/        ApiException
│       │   └── Services/          AgentService, AuthService, ChatService, ConsumerApplicationService,
│       │                          KbIngestionService, KbRetrievalService, KnowledgeBaseService,
│       │                          McpServerService, SessionService, UserService
│       ├── OpenChatAgents.Infrastructure/
│       │   ├── Data/              AppDbContext
│       │   ├── Migrations/        InitialCreate, AddKbAndAuth, AddMcpServersAndConsumerApplications
│       │   ├── Persistence/       AgentRepository, ConsumerApplicationRepository, KbDocumentRepository,
│       │   │                      KnowledgeBaseRepository, McpServerRepository, MessageRepository,
│       │   │                      SessionRepository, UserRepository
│       │   ├── Agents/            BedrockModelCatalog, ChatAgentFactory, EmbeddingClientFactory
│       │   ├── Mcp/               McpToolFactory
│       │   ├── Security/          Argon2PasswordHasher, DataProtectionSecretProtector
│       │   ├── Storage/           MinioObjectStore
│       │   ├── Messaging/         MinioEventNotification, RabbitMqConnectionFactory
│       │   ├── VectorStore/       KbVectorStore
│       │   ├── Ingestion/         TextExtractor
│       │   └── Telemetry/         TelemetryExtensions
│       ├── OpenChatAgents.Api/
│       │   ├── Controllers/       AgentsController, AuthController, ChatController,
│       │   │                      ConsumerApplicationsController, KnowledgeBasesController,
│       │   │                      McpServersController, ModelsController, SessionsController,
│       │   │                      UsersController
│       │   └── Program.cs
│       └── OpenChatAgents.Worker/
│           ├── KbIngestionBackgroundService.cs
│           └── Program.cs
└── frontend/
    ├── Dockerfile
    └── src/                    ver árvore na seção 3.2
```

---

## 5. Modelo de dados

Tabelas Postgres (snake_case), todas com `Id`/PK `Guid`, timestamps em `timestamp with time zone`:

| Tabela | Campos principais | Relacionamentos |
|---|---|---|
| `users` | `Username` (único), `PasswordHash` (Argon2id), `IsAdmin`, `MustChangePassword`, `CreatedByUserId` | auto-referência (quem cadastrou) |
| `agents` | `Name` (único), `Provider` (`ollama`/`bedrock`), `LlmModel`, `Temperature`, `MaxTokens`, `SystemPrompt`, `CreatedByUserId` | N:N com `knowledge_bases` (via `agent_knowledge_bases`) e com `mcp_servers` (via `agent_mcp_servers`); 1:N com `sessions` |
| `sessions` | `Title`, `AgentId` (nulável) | N:1 com `agents`; 1:N com `messages`. **Sem dono** — sessões são públicas para qualquer usuário autenticado |
| `messages` | `SessionId`, `Role` (`user`/`assistant`), `Content` | N:1 com `sessions`, cascade delete |
| `knowledge_bases` | `Name` (único), `Description`, `ChunkSize`, `ChunkOverlap`, `EmbeddingProvider`, `EmbeddingModel`, `EmbeddingDimensions`, `CreatedByUserId` | N:N com `agents`; 1:N com `kb_documents` |
| `kb_documents` | `KnowledgeBaseId`, `FileName`, `ContentType`, `SizeBytes`, `ObjectKey` (caminho no MinIO), `Status` (`uploaded`→`processing`→`completed`/`failed`), `ErrorMessage`, `ChunkCount`, `ProcessedAt` | N:1 com `knowledge_bases`, cascade; 1:N com `kb_chunk_refs` |
| `kb_chunk_refs` | `KbDocumentId`, `ChunkIndex`, `CharStart`, `CharEnd` | referência aos vetores gravados no Weaviate (o conteúdo do chunk em si vive só no Weaviate, não aqui) |
| `agent_knowledge_bases` | PK composta `(AgentId, KnowledgeBaseId)` | tabela de junção, cascade nos dois lados |
| `mcp_servers` | `Name` (único), `Url`, `AuthType` (`none`/`bearer-token`/`header`), `AuthHeaderName`, `Secret` (**ciphertext**, Data Protection), `CreatedByUserId` | N:N com `agents` |
| `agent_mcp_servers` | PK composta `(AgentId, McpServerId)` | tabela de junção, cascade nos dois lados |
| `consumer_applications` | `Name`, `ClientId` (único, prefixo `oca_`), `ClientSecretHash` (Argon2id), `IsActive`, `CreatedByUserId` | sem tabela de junção — client credentials dão acesso geral, igual um usuário |

Cada Knowledge Base tem sua **própria coleção Weaviate**, nomeada `Kb_{kbId:N}`, criada dinamicamente porque cada KB pode usar um modelo de embedding diferente (dimensionalidade diferente).

---

## 6. API — referência de endpoints

Base: `/api/v1`. Todos exigem `Authorization: Bearer <token>` exceto os marcados **público**.

| Método | Rota | Descrição | Acesso |
|---|---|---|---|
| `POST` | `/auth/login` | Login usuário/senha → JWT | público |
| `POST` | `/auth/token` | Client-credentials (`client_id`+`client_secret`) → JWT | público |
| `POST` | `/auth/change-password` | Troca a própria senha | autenticado |
| `GET` | `/auth/me` | Usuário autenticado atual | autenticado |
| `POST GET PUT DELETE` | `/agents` | CRUD de agentes (inclui `knowledge_base_ids`/`mcp_server_ids`) | autenticado |
| `POST GET DELETE` | `/sessions` | CRUD de sessões de conversa | autenticado |
| `POST` | `/chat/stream` | Envia mensagem (SSE: `user_message`\|`chunk`\|`done`) | autenticado |
| `GET` | `/chat/{sessionId}/history` | Histórico da sessão | autenticado |
| `GET` | `/models/ollama` \| `/models/bedrock` | Modelos disponíveis por provider | autenticado |
| `POST GET DELETE` | `/knowledge-bases` | CRUD de KBs | autenticado |
| `POST` | `/knowledge-bases/{id}/documents` | Upload de documento (multipart) | autenticado |
| `GET` | `/knowledge-bases/{id}/documents` | Status dos documentos da KB | autenticado |
| `POST GET PUT DELETE` | `/mcp-servers` | CRUD de servidores MCP | autenticado |
| `POST GET DELETE` | `/users` | CRUD de usuários | **admin** |
| `POST GET DELETE` | `/consumer-applications` | CRUD de aplicações consumidoras (secret só aparece na criação) | **admin** |
| `GET` | `/health` | Healthcheck | público |

---

## 7. Funcionalidades em detalhe

### 7.1 Chat com RAG

`ChatController.Stream` → `ChatService.StreamMessageAsync` (Application): persiste a mensagem do usuário → roda `ModerationService` (filtro de profanidade, Domain) → se o agente tem KBs associadas, `KbRetrievalService` gera embedding da pergunta e busca os chunks mais relevantes em cada coleção Weaviate, injetando o resultado como mensagem de sistema → `IChatAgentFactory.StreamAsync` (Microsoft Agent Framework) faz o streaming via SSE, persistindo a resposta completa no fim.

### 7.2 Ingestão de KB (orientada a eventos)

Upload só grava o arquivo no MinIO e cria o registro `KbDocument` (status `uploaded`) — não publica nada. O MinIO dispara uma *bucket notification* AMQP nativa direto pro RabbitMQ. O `KbIngestionBackgroundService` (Worker) é um adaptador fino: só decodifica a notificação e delega pro `KbIngestionService` (Application), que extrai texto → faz chunking → gera embeddings → grava no Weaviate → atualiza status.

### 7.3 Ferramentas MCP nos agentes

Um `McpServer` tem URL + tipo de autenticação (`none`, `bearer-token` ou `header` customizado) + segredo (criptografado em repouso via ASP.NET Core Data Protection — precisa ser recuperável, ao contrário de senha). A conexão MCP **não é cacheada como singleton**: a cada turno de chat, `ChatAgentFactory.StreamAsync` monta uma sessão MCP nova (`IMcpToolFactory.CreateSessionAsync`) para os servidores do agente, resolve as tools (`McpClientTool`, que já herda de `AIFunction`/`AITool`), injeta em `ChatOptions.Tools`, e só descarta a conexão depois do streaming terminar (a invocação de uma tool acontece durante o streaming, então a conexão precisa ficar viva até lá). Falha em um servidor específico não derruba o chat — só aquele servidor fica sem tools naquela conversa (log de warning).

### 7.4 Consumo remoto via API (client credentials)

Um admin cadastra uma `ConsumerApplication` (só o nome) e recebe, uma única vez, um `client_id`/`client_secret` gerados aleatoriamente (o secret é hasheado com Argon2id e nunca mais retornado). A aplicação externa troca essas credenciais em `POST /auth/token` por um JWT (mesma chave de assinatura, mesmas claims `sub`/`token_use`, expiração igual à de usuário) e usa esse token exatamente como um usuário logado usaria — os endpoints de chat/sessão não precisaram de nenhuma mudança de autorização, porque agentes/KBs/sessões já eram públicos para qualquer principal autenticado.

### 7.5 Dashboard inicial

`/` é a tela inicial: mensagem de boas-vindas, contagem de agentes/KBs/conversas/servidores MCP, ações rápidas (nova conversa, novo agente, nova KB, novo servidor MCP — os três últimos abrem os modais existentes direto na própria tela), e lista das conversas recentes (que levam para `/chat?session=<id>`). O chat em si vive em `/chat`.

### 7.6 Observabilidade

OpenTelemetry .NET SDK, span raiz automático via `AddAspNetCoreInstrumentation` (só na Api) + `AddHttpClientInstrumentation`, mais um `ActivitySource` manual (`AppActivitySource`, Domain) para amarrar o pipeline de negócio (moderação, retrieval, ingestão) em torno das chamadas de modelo já auto-instrumentadas pelo Microsoft Agent Framework (`UseOpenTelemetry` em cima do `AIAgent`/`IEmbeddingGenerator`). Exportado via OTLP/HTTP para um Langfuse self-hosted completo (Postgres + ClickHouse + Redis + MinIO dedicados). `AppSettings:Telemetry:CaptureSensitiveContent` (padrão `false`) controla se o conteúdo real de prompts/respostas aparece no trace.

---

## 8. Convenções

- **Contrato JSON**: sempre `snake_case` (via `JsonNamingPolicy.SnakeCaseLower`, configurado uma vez em `Api/Program.cs`) — os tipos TypeScript espelham isso 1:1, sem camada de tradução.
- **DTOs**: trio `*Create` (campos obrigatórios, `[Required]`/`[Range]`/`[RegularExpression]`), `*Update` (todos os campos opcionais — `null` = "não mexe"), `*Response` (com `static FromEntity(...)` de fábrica) + `*ListResponse` (`{ Items, Total }`).
- **Repositórios**: interface em `Domain/Repositories`, implementação EF Core em `Infrastructure/Persistence`. Update parcial via um record `*UpdateFields` com todos os campos nulláveis. Relação N:N é sincronizada por um método privado `Sync*Async` (remove tudo e reinsere) dentro do repositório do lado "dono" (ex.: `AgentRepository.SyncMcpServersAsync`).
- **Tabelas**: nome plural snake_case (`ToTable("agent_mcp_servers")`), índice único via `HasIndex(...).IsUnique()`, timestamps com `HasDefaultValueSql("now()")`, FK para `CreatedByUserId` com `DeleteBehavior.SetNull` (exceto `knowledge_bases`, que usa `Restrict`).
- **Erros de negócio**: `ApiException` (Application) com um `HttpStatusCode` associado (`NotFound`/`Conflict`/`BadGateway`/`Unauthorized`/`Forbidden`), capturado pelo `UseExceptionHandler` global em `Api/Program.cs`.
- **Segredos**: senha e client secret → hash Argon2id (`IPasswordHasher`, one-way). Segredo de servidor MCP → criptografado reversível (`ISecretProtector`/Data Protection), porque precisa ser lido de volta para autenticar no MCP.
- **Frontend**: cada página App Router monta sua própria composição de `AuthGuard` + navegação — não existe layout compartilhado. Multi-select "pill"/chip usa `Autocomplete multiple` do MUI com `renderValue` customizado (`KbMultiSelect`, `McpServerMultiSelect` seguem o mesmo padrão exato). `Paper` com elevação é o "card" idiomático do projeto — `Card`/`CardContent` do MUI não são usados em lugar nenhum.
- **Migrations**: sempre via `dotnet ef migrations add <Name> --project src/OpenChatAgents.Infrastructure/... --startup-project src/OpenChatAgents.Api/...` (comando completo no CLAUDE.md); aplicadas automaticamente no startup, nunca manualmente.

---

## 9. Infraestrutura / Docker Compose

| Serviço | Porta host | Papel |
|---|---|---|
| `postgres` | `5432` | Banco relacional da aplicação |
| `weaviate` | `8085` (REST), `50051` (gRPC) | Banco vetorial |
| `minio` | `9000` (S3), `9001` (console) | Armazenamento de documentos das KBs |
| `rabbitmq` | `5672` (AMQP), `15672` (management) | Fila de ingestão |
| `minio-init` | — | Container one-shot: cria bucket + configura *bucket notification* AMQP |
| `backend` | `8090` | Api (ASP.NET Core); volume `dataprotection_keys:/keys` para persistir a chave de criptografia dos segredos MCP entre restarts |
| `worker` | — | Worker de ingestão de KB |
| `frontend` | `3000` | Next.js |
| `langfuse-postgres` | `5433` | Postgres dedicado do Langfuse |
| `langfuse-clickhouse` | `8123`, `9010` | Analytics store do Langfuse |
| `langfuse-redis` | `6379` | Fila interna do Langfuse |
| `langfuse-minio` | `9090`, `9091` | Blob storage do Langfuse |
| `langfuse-worker` | `3030` | Processamento assíncrono do Langfuse |
| `langfuse-web` | `3001` | UI + API pública do Langfuse |

Ollama roda no **host** (`host.docker.internal:11434`), não em container. Portas foram deliberadamente movidas dos defaults comuns para não colidir com outros serviços já rodando na máquina de desenvolvimento (Portainer em `8000`, WordPress em `8080`).

---

## 10. Limitações / decisões assumidas conhecidas

- Agentes, KBs, sessões e servidores MCP são **públicos** para qualquer principal autenticado (usuário ou aplicação consumidora) — não há escopo por usuário nem por aplicação cliente.
- Aplicações consumidoras têm acesso igual ao de um usuário comum (não-admin); não conseguem gerenciar usuários nem outras aplicações consumidoras.
- A conexão com servidores MCP é refeita a cada turno de chat (sem cache de conexão entre requisições) — favorece correção sobre performance, já que a documentação oficial do SDK MCP não confirma reuso seguro de conexão entre requisições concorrentes.
- Sem projeto de testes automatizados ainda.
- `npm run lint` pede setup interativo de ESLint (lacuna pré-existente do projeto) — `npm run build` já roda type-check + lint internamente e é o que se usa para validar.
