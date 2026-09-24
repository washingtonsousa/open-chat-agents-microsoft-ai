# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET port of the Python **open-chat-agents** project (`../Chat Bot Com Rag e Front`), rebuilt on **ASP.NET Core 10** and the **Microsoft Agent Framework** instead of FastAPI/LangChain, and extended with things the original project never had: a real RAG pipeline (Knowledge Bases, event-driven ingestion into Weaviate), JWT/Argon2id auth, MCP tool servers (external **and built-in**: filesystem/datetime/skill-creator) attachable to agents, reusable markdown Skills, multi-agent orchestration (agents calling other agents as tools), vision/image attachments in chat, a client-credentials flow for external apps to consume agents remotely, a home dashboard, and OpenTelemetry/Langfuse observability. See [spec.md](spec.md) for the full architecture/data-model/API reference and [README.md](README.md) for the quickstart/config table — don't duplicate either here.

## Commands

### Backend / Worker (.NET)

```bash
cd backend
dotnet build                                    # whole solution (Infrastructure + Api + Worker)
dotnet run --project src/OpenChatAgents.Api      # API on http://localhost:8090
dotnet run --project src/OpenChatAgents.Worker   # KB ingestion worker (needs RabbitMQ/MinIO/Weaviate running)
```

EF Core migrations live in `OpenChatAgents.Infrastructure`; the startup project for `dotnet ef` is `OpenChatAgents.Api`:

```bash
dotnet ef migrations add <Name> \
  --project src/OpenChatAgents.Infrastructure/OpenChatAgents.Infrastructure.csproj \
  --startup-project src/OpenChatAgents.Api/OpenChatAgents.Api.csproj \
  --output-dir Migrations
```

Migrations apply automatically on startup (`dbContext.Database.MigrateAsync()` in both `Api` and `Worker` `Program.cs`) — no manual `dotnet ef database update` needed in normal dev.

No test projects exist yet.

### Frontend (Next.js)

```bash
cd frontend
npm run dev      # http://localhost:3000
npm run build    # also type-checks; run this after touching src/services/api.ts or src/types
npm run lint
```

### Full stack

```bash
docker-compose up --build
```

Brings up Postgres, Weaviate, MinIO (+ `minio-init`, a one-shot container that wires MinIO's AMQP bucket notification to RabbitMQ), RabbitMQ, backend, worker, and frontend. Ollama is expected to run on the **host** (`host.docker.internal:11434`), not in Compose. Ports were deliberately moved off common defaults to avoid clashing with other local Docker services (Portainer on 8000, WordPress on 8080) — check `docker ps` before changing them back.

## Architecture

### Solution layout

Five projects, DDD-ish layering. Dependencies point inward: `Api`/`Worker` → `Infrastructure` + `Application` → `Domain`. `Infrastructure` and `Application` never reference each other — both only depend on `Domain`, which has zero infrastructure package references (only `Microsoft.Extensions.AI.Abstractions` for message/embedding types).

```
backend/src/
├── OpenChatAgents.Domain/            entities, options POCOs, repository interfaces, infra "ports"
│   ├── Models/          Agent, Session, Message, User, KnowledgeBase, KbDocument, KbChunkRef,
│   │                    AgentKnowledgeBase, McpServer, McpServerKind, AgentMcpServer, McpAuthType,
│   │                    ConsumerApplication, Skill, AgentSkill, AgentSubAgent
│   ├── Options/           AppOptions and its nested sections (Jwt, Minio, RabbitMq, Weaviate, Argon2, Aws,
│   │                      Telemetry, Filesystem)
│   ├── Repositories/        IAgentRepository, ISessionRepository, IMessageRepository, IUserRepository,
│   │                        IKnowledgeBaseRepository, IKbDocumentRepository, IMcpServerRepository,
│   │                        IConsumerApplicationRepository, ISkillRepository
│   ├── Abstractions/          IChatAgentFactory, IEmbeddingClientFactory, IObjectStore, IPasswordHasher,
│   │                          ITextExtractor, ISecretProtector, IMcpToolFactory, IBuiltInToolProvider
│   ├── VectorStore/             IKbVectorStore + KbChunkRecord/KbSearchResult
│   ├── Services/                  ModerationService, ChunkingService (pure, no external deps)
│   └── Telemetry/                   AppActivitySource
├── OpenChatAgents.Application/    use-case services — depends only on Domain
│   ├── Dtos/             wire contracts (Create/Update/Response types, snake_case over the wire)
│   ├── Exceptions/         ApiException
│   └── Services/             AgentService, SessionService, ChatService, AuthService, UserService,
│                             KnowledgeBaseService, KbRetrievalService, KbIngestionService,
│                             McpServerService, ConsumerApplicationService, SkillService
├── OpenChatAgents.Infrastructure/  concrete implementations of Domain interfaces
│   ├── Data/             AppDbContext + Migrations
│   ├── Persistence/        EF Core repositories (AgentRepository, SessionRepository, McpServerRepository,
│   │                       ConsumerApplicationRepository, SkillRepository, ...)
│   ├── Agents/              ChatAgentFactory, EmbeddingClientFactory, BedrockModelCatalog
│   ├── Mcp/                   McpToolFactory (connects to external MCP servers AND resolves built-in
│   │                           tool providers, discriminated by McpServer.Kind)
│   ├── BuiltInTools/            FilesystemToolProvider, DateTimeToolProvider, SkillCreatorToolProvider
│   │                             (each implements IBuiltInToolProvider)
│   ├── Security/                Argon2PasswordHasher, DataProtectionSecretProtector
│   ├── Storage/                    MinioObjectStore
│   ├── Messaging/                    RabbitMqConnectionFactory, MinioEventNotification (S3-style event DTO)
│   ├── VectorStore/                     KbVectorStore
│   ├── Ingestion/                         TextExtractor
│   └── Telemetry/                           TelemetryExtensions (OTel/Langfuse wiring)
├── OpenChatAgents.Api/       Controllers + Program.cs (composition root — the only place that binds
│                             Domain interfaces to Infrastructure implementations via DI)
└── OpenChatAgents.Worker/    KbIngestionBackgroundService (thin RabbitMQ adapter) + Program.cs
```

`KbIngestionBackgroundService` in Worker does no business logic itself — it just parses the MinIO/RabbitMQ notification and calls `KbIngestionService.ProcessDocumentAsync` (Application), which is where extract→chunk→embed→upsert actually lives. This is the one place the layering changed real behavior, not just namespaces: the ingestion workflow used to be entangled with the RabbitMQ consumer loop and a raw `AppDbContext`; now it's a testable Application service behind `IKbDocumentRepository`.

Changing an entity, a repository interface, or an infra port always means touching `Domain`; changing how something is persisted or which SDK is called means touching `Infrastructure`; changing a use case's orchestration means touching `Application`. All five projects need rebuilding/redeploying together when any of them changes — there's no independent versioning between layers.

### JSON contract

Controllers and the manual SSE writer in `ChatController` both serialize with `JsonNamingPolicy.SnakeCaseLower` (configured once in `Api/Program.cs`, reused as a singleton `JsonSerializerOptions`). This exists specifically so the frontend types (`frontend/src/types/index.ts`) — snake_case fields like `agent_id`, `must_change_password`, `knowledge_base_ids` — don't need translation. If you add a new DTO property, no naming-policy work is needed; it's automatic.

### Chat streaming (SSE) and RAG injection

`ChatController.Stream` (Api) writes raw `event: X\ndata: {...}\n\n` frames itself (not `IAsyncEnumerable` auto-serialization) so the exact wire format stays under control. `ChatService.StreamMessageAsync` (Application):
1. persists the user message, runs `ModerationService` (Domain — regex profanity filter, short-circuits if triggered),
2. builds the message history, and if the session's agent has `KnowledgeBaseLinks`, calls `KbRetrievalService.BuildContextAsync` (Application) to embed the query, search each KB's Weaviate collection via `IKbVectorStore`, and prepend the retrieved text as a `ChatRole.System` message,
3. streams tokens from `IChatAgentFactory.StreamAsync` (Domain interface; `ChatAgentFactory` in Infrastructure implements it via the Microsoft Agent Framework's `AIAgent.RunStreamingAsync`) and persists the assembled assistant message at the end.

### KB ingestion (event-driven, not polled)

Upload (`KnowledgeBasesController` → `KnowledgeBaseService.UploadDocumentAsync`, Application) only creates a `KbDocument` row and PUTs the object to MinIO under `kb/{kbId}/{documentId}/{fileName}` — it does **not** publish anything itself. MinIO's own bucket-notification feature (configured by the `minio-init` container) fires an AMQP event on `s3:ObjectCreated:*` straight into RabbitMQ. `KbIngestionBackgroundService` in `OpenChatAgents.Worker` is the only consumer, but it's just a thin adapter: it parses the object key back into a `KbDocumentId` and calls `KbIngestionService.ProcessDocumentAsync` (Application), which extracts text (`ITextExtractor` → Infrastructure's `TextExtractor`, PdfPig/DOCX/plain-text by extension), chunks it (`ChunkingService`, Domain, size/overlap from the KB), embeds each chunk via `IEmbeddingClientFactory`, and upserts into a **per-KB Weaviate collection** named `Kb_{kbId:N}` via `IKbVectorStore`. Status transitions (`uploaded → processing → completed/failed`) are the only thing the frontend polls for.

Each KB can use a different embedding model/provider (Ollama or Bedrock) with a different vector dimensionality — this is why the Weaviate collection is created dynamically per-KB (`VectorStoreCollectionDefinition` built at runtime) rather than from a single static record type.

### MCP tools on agents

An `Agent` can be linked (many-to-many, `AgentMcpServer`) to one or more `McpServer` rows (`Url` + `AuthType`: `none`/`bearer-token`/`header` + an encrypted `Secret`). `ChatAgentFactory.StreamAsync` (Infrastructure) resolves tools **fresh every chat turn** via `IMcpToolFactory.CreateSessionAsync` — it does *not* cache a connected `McpClient` as a singleton, because the SDK docs don't confirm that's safe under concurrent requests. The returned `IMcpToolSession` must stay alive for the whole `RunStreamingAsync` call (not just the initial `ListToolsAsync`), since a tool invocation during streaming routes back over that same MCP connection — it's disposed only in the `finally` after streaming ends. A server that fails to connect/authenticate is skipped with a logged warning, never throws — one bad MCP server must never break the chat. `McpClientTool` (from `ModelContextProtocol.Core`) already derives from `AIFunction`/`AITool`, so resolved tools go straight into `ChatOptions.Tools` with no conversion step.

`McpServer.Secret` is encrypted at rest via `ISecretProtector` (Infrastructure: `DataProtectionSecretProtector`, ASP.NET Core Data Protection) — not hashed like a password, because it has to be recoverable in plaintext to authenticate against the MCP server. The Data Protection key ring is persisted to the `dataprotection_keys` Docker volume (`/keys` in the `backend` container) specifically so encrypted secrets survive a container recreate; losing that volume makes every stored MCP secret permanently undecryptable.

### Built-in MCP tools

Three native tools ship in the backend and are represented as `McpServer` rows with `Kind=BuiltIn` (seeded idempotently at startup by `McpServerService.EnsureBuiltInServersAsync`, upserted by `BuiltInKey`) — they show up in the same "MCP Servers" screen and agent multi-select as external servers, with a "built-in" badge, but `Update`/`Delete` are rejected (`ApiException.Conflict`) for that `Kind`. They're implemented as in-process `AITool`s (`IBuiltInToolProvider` in `Domain/Abstractions`, one implementation per tool in `Infrastructure/BuiltInTools/`), **not** a real MCP-protocol server — `ModelContextProtocol.AspNetCore` doesn't yet support multiple independent MCP routes in one app, and has an open scoped-DI reliability bug in HTTP hosting mode, so native functions sidestep both:

- `FilesystemToolProvider` (`Key="filesystem"`) — read/list/write/delete inside a single sandboxed root (`AppOptions.Filesystem.RootPath`, the `agent_filesystem` volume). Every path is resolved via `Path.GetFullPath` + a prefix check against the canonicalized root; anything that escapes it (`../../etc/passwd`, an absolute path) is rejected. Read **and** write are both enabled — an explicit user choice, this is not read-only.
- `DateTimeToolProvider` (`Key="datetime"`) — current time by timezone, timezone conversion, timezone listing, via `TimeZoneInfo`; no DI dependencies, stateless.
- `SkillCreatorToolProvider` (`Key="skill-creator"`) — a `CreateSkillAsync(name, description, content)` tool the LLM calls when the user asks (in chat) to create a skill; persists it via `ISkillRepository` with `CreatedByUserId` taken from `BuiltInToolContext(UserId)`, which `ChatAgentFactory` builds fresh per chat turn.

`McpToolFactory.CreateSessionAsync` resolves each `McpServer` by `Kind`: `External` connects over HTTP as before; `BuiltIn` looks up the matching `IEnumerable<IBuiltInToolProvider>` by `Key` and calls `GetTools(context)` synchronously, no network involved.

### Skills

A `Skill` (public entity, like a KB or Agent) holds `Name` (unique), `Description`, and `Content` in markdown, owned by `CreatedByUserId`. Created either through the `/skills` markdown editor (split text/preview using the same `react-markdown`+`remark-gfm` already used for chat rendering) or via chat through the `skill-creator` built-in tool above. A skill attached to an agent (`AgentSkill`, many-to-many) is **injected directly into that agent's `Instructions`/system prompt every turn** — not retrieved on demand, not RAG — a deliberate simplicity choice for short, reusable instructions.

### Multi-agent orchestration

An agent can list other existing agents as **sub-agents** (`AgentSubAgent`, a self-referencing many-to-many on `Agent`). At chat time, `ChatAgentFactory.StreamAsync` uses the Microsoft Agent Framework's own `AIAgentExtensions.AsAIFunction(AIAgent, AIFunctionFactoryOptions?, AgentSession?)` to expose each sub-agent as a callable `AIFunction` for the parent — invoking it runs a full `agent.RunAsync` of the sub-agent under the hood. **Depth is capped at 1 level by construction, not by a counter or cycle guard**: when building a sub-agent's own tools, `ChatAgentFactory` resolves only that child's MCP/built-in tools and never expands the child's own `SubAgentLinks` — only the top-level streaming call for the agent actually talking to the user looks at sub-agents at all.

### Vision (image attachments in chat)

`ChatInput` lets the user attach one image (base64-encoded client-side) alongside a text message; `POST /chat/stream` carries it as `image_base64`/`image_content_type`. `ChatService.StreamMessageAsync` decodes it, uploads to MinIO (`chat/{sessionId}/{messageId}/...`), persists the reference on the `Message` row (`ImageObjectKey`/`ImageContentType`), and builds the current turn's `ChatMessage` as multimodal (`Contents.Add(new DataContent(bytes, contentType))`, `Microsoft.Extensions.AI`). **Only the current turn is sent multimodally to the model** — older images in the session history are never re-sent on later turns, they're display-only via `GET /chat/{sessionId}/messages/{messageId}/image`. Since `<img src>` can't carry an `Authorization` header, the frontend fetches that endpoint manually and renders a blob URL (`AuthenticatedImage` in `MessageBubble.tsx`). There's no capability-detection API, so the UI always offers the attach button regardless of whether the agent's model actually supports vision — confirmed both `OllamaSharp` and the Bedrock adapter forward `DataContent` image content to the provider rather than silently dropping it.

### Remote API access (client credentials)

`ConsumerApplication` (admin-managed via `ConsumerApplicationsController`, `[Authorize(Roles = "Admin")]`) holds a `ClientId` + Argon2id-hashed `ClientSecret`. `POST /api/v1/auth/token` (`AllowAnonymous`) exchanges `client_id`/`client_secret` for a JWT built by the same `AuthService`/signing key as a user login, just with different claims (`sub` = the `ConsumerApplication.Id`, plus `client_id` and `token_use=client`). That token is then usable exactly like a user's — **no change was needed to `ChatController`/`SessionsController`**, since sessions/agents/KBs were already public to any authenticated principal and neither controller does a `Users` table lookup on `sub`. The plaintext secret is generated once at creation (`ConsumerApplicationService.CreateAsync`), returned only in that response (`ConsumerApplicationCreated`), and never retrievable again — only its hash is stored.

### Weaviate client quirks (read before touching `KbVectorStore.cs`)

- `Weaviate.Client.VectorData` is pinned to `Microsoft.Extensions.VectorData.Abstractions` **10.0.1** in `Infrastructure.csproj` — bumping the Abstractions package alone compiles fine but breaks at runtime (`MissingMethodException` on `VectorSearchOptions.OldFilter`). Keep them in lockstep.
- Use `VectorStore.GetCollection<Guid, Dictionary<string, object?>>(name, definition)`, not `GetDynamicCollection` (that one forces `TKey=object`, which the Weaviate provider rejects).
- Weaviate lowercases the first letter of every property server-side, and the client's search-result dictionary doesn't reliably map back to the declared casing — `KbVectorStore.GetValue` does a case-insensitive lookup on `result.Record` for this reason. Don't name a property `Id` (collides with Weaviate's reserved `id`); this codebase uses `ChunkKey`.
- GraphQL integers come back as `Int64`; use `Convert.ToInt32(...)`, not a direct `(int)` cast.

### DI lifetime gotcha (Scoped vs Singleton) — watch this one

`ChatAgentFactory` and `McpToolFactory` **must** be registered `AddScoped`, never `AddSingleton`, even though nothing about them looks scoped at a glance. The trap: they depend on `IEnumerable<IBuiltInToolProvider>`, and one of those providers (`SkillCreatorToolProvider`) is itself `Scoped` because it needs `ISkillRepository` → `AppDbContext`. A singleton that captures a scoped dependency (even transitively, through DI's `IEnumerable<T>` collection resolution) silently pins that first-resolved `AppDbContext` instance forever — no exception at startup, just slow, hard-to-diagnose corruption/staleness later. This was a real bug hit and fixed in this codebase. `FilesystemToolProvider`/`DateTimeToolProvider` have no such dependency and stay `AddSingleton`. Rule of thumb before adding any new `IBuiltInToolProvider` or anything consumed by `ChatAgentFactory`/`McpToolFactory`: if it (or anything it depends on) touches `AppDbContext` or another `Scoped` service, the whole chain from `ChatAgentFactory` down must stay `Scoped`.

### Docker/Weaviate gotcha

Weaviate ≥1.29 needs `CLUSTER_HOSTNAME` set in `docker-compose.yml` (single-node Raft bootstrap) or it hangs indefinitely on leader election. Never reuse the `weaviate_data` volume across an image version bump — a stale Raft log can wedge startup even with `CLUSTER_HOSTNAME` correct; drop the volume and let it re-bootstrap.

### Auth

JWT bearer, issued by `AuthService` (`Application/Services`), validated globally via `app.MapControllers().RequireAuthorization()` in `Program.cs` — `[AllowAnonymous]` only on `AuthController.Login` and `AuthController.Token` (the client-credentials endpoint). Admin bootstrap (`admin`/`1234`, `MustChangePassword=true`) happens once at startup if the `users` table is empty. A single `JwtBearer` scheme validates both user-login tokens and client-credentials tokens — `TokenValidationParameters` only checks signature/issuer/audience/lifetime, never does a `Users` table lookup on `sub`, so both token kinds pass through the same pipeline. KBs, agents, sessions, and MCP servers are public to any authenticated principal (user or consumer application); only `UsersController` and `ConsumerApplicationsController` are `[Authorize(Roles = "Admin")]`.

### Frontend routing

The chat UI lives at `/chat`, not `/` — `/` is the home dashboard (welcome message, entity counts, quick actions, recent sessions). Every page composes its own `AuthGuard` + nav Drawer independently; there's no shared Next.js layout for it. `SessionSidebar` (used only by `/chat`) and the dashboard's own Drawer both list the same nav destinations (Painel, Chat, Bases de conhecimento, Servidores MCP, Skills, Usuários, Aplicações consumidoras) — keep them in sync if you add a new top-level page.
