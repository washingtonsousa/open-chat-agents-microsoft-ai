# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET port of the Python **open-chat-agents** project (`../Chat Bot Com Rag e Front`), rebuilt on **ASP.NET Core 10** and the **Microsoft Agent Framework** instead of FastAPI/LangChain, and extended with a real RAG pipeline (Knowledge Bases, event-driven ingestion into Weaviate) and JWT/Argon2id auth that the original project never had. See [README.md](README.md) for the full feature list, API table, and configuration reference — don't duplicate that here.

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
│   ├── Models/          Agent, Session, Message, User, KnowledgeBase, KbDocument, KbChunkRef, AgentKnowledgeBase
│   ├── Options/           AppOptions and its nested sections (Jwt, Minio, RabbitMq, Weaviate, Argon2, Aws, Telemetry)
│   ├── Repositories/        IAgentRepository, ISessionRepository, IMessageRepository, IUserRepository,
│   │                        IKnowledgeBaseRepository, IKbDocumentRepository
│   ├── Abstractions/          IChatAgentFactory, IEmbeddingClientFactory, IObjectStore, IPasswordHasher, ITextExtractor
│   ├── VectorStore/             IKbVectorStore + KbChunkRecord/KbSearchResult
│   ├── Services/                  ModerationService, ChunkingService (pure, no external deps)
│   └── Telemetry/                   AppActivitySource
├── OpenChatAgents.Application/    use-case services — depends only on Domain
│   ├── Dtos/             wire contracts (Create/Update/Response types, snake_case over the wire)
│   ├── Exceptions/         ApiException
│   └── Services/             AgentService, SessionService, ChatService, AuthService, UserService,
│                             KnowledgeBaseService, KbRetrievalService, KbIngestionService
├── OpenChatAgents.Infrastructure/  concrete implementations of Domain interfaces
│   ├── Data/             AppDbContext + Migrations
│   ├── Persistence/        EF Core repositories (AgentRepository, SessionRepository, ...)
│   ├── Agents/              ChatAgentFactory, EmbeddingClientFactory, BedrockModelCatalog
│   ├── Security/              Argon2PasswordHasher
│   ├── Storage/                  MinioObjectStore
│   ├── Messaging/                  RabbitMqConnectionFactory, MinioEventNotification (S3-style event DTO)
│   ├── VectorStore/                   KbVectorStore
│   ├── Ingestion/                       TextExtractor
│   └── Telemetry/                         TelemetryExtensions (OTel/Langfuse wiring)
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

### Weaviate client quirks (read before touching `KbVectorStore.cs`)

- `Weaviate.Client.VectorData` is pinned to `Microsoft.Extensions.VectorData.Abstractions` **10.0.1** in `Infrastructure.csproj` — bumping the Abstractions package alone compiles fine but breaks at runtime (`MissingMethodException` on `VectorSearchOptions.OldFilter`). Keep them in lockstep.
- Use `VectorStore.GetCollection<Guid, Dictionary<string, object?>>(name, definition)`, not `GetDynamicCollection` (that one forces `TKey=object`, which the Weaviate provider rejects).
- Weaviate lowercases the first letter of every property server-side, and the client's search-result dictionary doesn't reliably map back to the declared casing — `KbVectorStore.GetValue` does a case-insensitive lookup on `result.Record` for this reason. Don't name a property `Id` (collides with Weaviate's reserved `id`); this codebase uses `ChunkKey`.
- GraphQL integers come back as `Int64`; use `Convert.ToInt32(...)`, not a direct `(int)` cast.

### Docker/Weaviate gotcha

Weaviate ≥1.29 needs `CLUSTER_HOSTNAME` set in `docker-compose.yml` (single-node Raft bootstrap) or it hangs indefinitely on leader election. Never reuse the `weaviate_data` volume across an image version bump — a stale Raft log can wedge startup even with `CLUSTER_HOSTNAME` correct; drop the volume and let it re-bootstrap.

### Auth

JWT bearer, issued by `AuthService` (`Application/Services`), validated globally via `app.MapControllers().RequireAuthorization()` in `Program.cs` — `[AllowAnonymous]` only on `AuthController.Login`. Admin bootstrap (`admin`/`1234`, `MustChangePassword=true`) happens once at startup if the `users` table is empty. KBs, agents, and sessions are public to any authenticated user; only `UsersController` is `[Authorize(Roles = "Admin")]`.
