# open-chat-agents-dotnet

Porta em **.NET** do POC [open-chat-agents](../Chat%20Bot%20Com%20Rag%20e%20Front) — o mesmo chatbot com agentes configuráveis, streaming e persistência, agora com **ASP.NET Core** e o **Microsoft Agent Framework** no lugar de FastAPI + LangChain/LangGraph. Também adiciona o que o projeto original nunca teve de verdade apesar do nome: **RAG** (bases de conhecimento com upload de documentos, chunking, embeddings e retrieval), autenticação com login/senha e um worker orientado a eventos para o pipeline de ingestão.

---

## Tech Stack

| Camada | Tecnologia |
|--------|-----------|
| Frontend | Next.js 15, React 19, **MUI (Material-UI)** como design system (`@mui/material-nextjs` para SSR no App Router; Tailwind ainda instalado mas não é mais o principal) |
| Backend / Worker | ASP.NET Core 10 + Worker Service, C# — 5 projetos em camadas DDD (Domain → Application → Infrastructure → Api/Worker) |
| Orquestração de LLM | [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) (`Microsoft.Agents.AI`) |
| LLM / Embeddings | Ollama (`OllamaSharp`) ou AWS Bedrock (`AWSSDK.Extensions.Bedrock.MEAI`) |
| Banco relacional | PostgreSQL 16 + Entity Framework Core 10 |
| Banco vetorial | Weaviate, via `Microsoft.Extensions.VectorData` + `Weaviate.Client.VectorData` |
| Armazenamento de arquivos | MinIO |
| Mensageria / eventos | RabbitMQ (alimentado pelas *bucket notifications* nativas do MinIO) |
| Hash de senha | Argon2id (`Konscious.Security.Cryptography.Argon2`) |
| Extração de texto | PdfPig (PDF), `DocumentFormat.OpenXml` (DOCX), leitura direta (texto/markdown) |
| Observabilidade | OpenTelemetry (traces) → **Langfuse** self-hosted via OTLP/HTTP (spans de agente, chamada ao modelo com tokens/latência, retrieval de KB, tags de sessão/usuário) |

---

## Arquitetura

O backend segue uma separação em camadas ao estilo DDD — `Domain` no centro, sem dependências externas; `Application` orquestra os casos de uso contra abstrações do `Domain`; `Infrastructure` fornece as implementações concretas (EF Core, Weaviate, MinIO, RabbitMQ, Argon2, Ollama/Bedrock); `Api` e `Worker` são as camadas de apresentação/host que compõem tudo via DI:

```
backend/src/
├── OpenChatAgents.Domain/            Sem dependência de infra — só abstrações e regras puras
│   ├── Models/         Entidades (Agent, Session, Message, User, KnowledgeBase,
│   │                   KbDocument, KbChunkRef, AgentKnowledgeBase)
│   ├── Options/          Configuração fortemente tipada (AppOptions e afins)
│   ├── Repositories/      Interfaces de repositório (IAgentRepository, ISessionRepository...)
│   ├── Abstractions/       Portas para infraestrutura (IChatAgentFactory, IEmbeddingClientFactory,
│   │                       IObjectStore, IPasswordHasher, ITextExtractor)
│   ├── VectorStore/         IKbVectorStore + records de transporte (KbChunkRecord, KbSearchResult)
│   ├── Services/             Regras de domínio puras (ModerationService, ChunkingService)
│   └── Telemetry/              AppActivitySource (ActivitySource compartilhado)
├── OpenChatAgents.Application/     Casos de uso — depende só do Domain
│   ├── Dtos/            Contratos de entrada/saída (Create/Update/Response, snake_case no wire)
│   ├── Exceptions/        ApiException (mapeada para status HTTP pelo middleware da Api)
│   └── Services/            AgentService, SessionService, ChatService, AuthService, UserService,
│                            KnowledgeBaseService, KbRetrievalService, KbIngestionService
├── OpenChatAgents.Infrastructure/  Implementações concretas das portas do Domain
│   ├── Data/            AppDbContext + Migrations
│   ├── Persistence/       Repositórios EF Core (implementam as interfaces do Domain)
│   ├── Agents/             ChatAgentFactory, EmbeddingClientFactory, BedrockModelCatalog
│   ├── Security/             Argon2PasswordHasher
│   ├── Storage/                MinioObjectStore
│   ├── Messaging/                RabbitMqConnectionFactory, evento do MinIO
│   ├── VectorStore/                 KbVectorStore (coleção Weaviate dinâmica por KB)
│   ├── Ingestion/                     TextExtractor
│   └── Telemetry/                       TelemetryExtensions (wiring do OpenTelemetry/Langfuse)
├── OpenChatAgents.Api/       Controllers + Program.cs (composition root)
└── OpenChatAgents.Worker/    BackgroundService (consome a fila) + Program.cs
```

`Domain` não referencia nenhum pacote de infraestrutura (Weaviate/MinIO/RabbitMQ/EF Core/Ollama/Bedrock) — só `Microsoft.Extensions.AI.Abstractions` para os tipos de mensagem/embedding, que são puramente abstrações. `Application` idem, mais `Microsoft.Extensions.Options`/`Logging.Abstractions` e o necessário para emitir JWT. Só `Infrastructure`, `Api` e `Worker` conhecem os SDKs concretos.

### Chat com retrieval (RAG)

```
Browser → POST /api/v1/chat/stream → ChatController → ChatService (Application)
                                          ├── ModerationService     (Domain — guarda de profanidade)
                                          ├── KbRetrievalService     (Application — embed da pergunta + busca vetorial por KB do agente)
                                          └── IChatAgentFactory        (Infrastructure — Microsoft Agent Framework + Ollama/Bedrock)
                                                  ↓ SSE stream (event: user_message | chunk | done)
```

Se o agente da sessão tiver bases de conhecimento associadas, o `KbRetrievalService` gera o embedding da mensagem do usuário, busca os chunks mais próximos em cada coleção Weaviate da KB e injeta o contexto recuperado como mensagem de sistema antes do histórico.

### Ingestão de KB (orientada a eventos)

```
Usuário → POST /knowledge-bases/{id}/documents (upload)
              → cria KbDocument (status=uploaded) e grava o arquivo no MinIO
                     ↓ MinIO dispara bucket notification (AMQP) — sem o backend publicar nada
              RabbitMQ (exchange "minio-events") → fila "kb-ingestion"
                     ↓
OpenChatAgents.Worker: KbIngestionBackgroundService (host) delega para
                        KbIngestionService (Application), que extrai texto (Infrastructure/TextExtractor),
                        faz chunking (Domain/ChunkingService), gera embeddings e grava no Weaviate
                        (uma coleção por KB) → status=completed/failed
```

O frontend acompanha o status de cada documento (`uploaded → processing → completed/failed`) via polling na tela de Bases de Conhecimento.

**Formatos suportados no upload:** `.pdf`, `.docx`, `.txt`, `.md`. Qualquer outra extensão é lida como texto puro (`TextExtractor.cs`) — arquivos binários de outros formatos (`.doc`, `.xlsx`, imagens etc.) não têm extrator dedicado e resultam em texto ilegível sendo indexado.

---

## Requisitos

- **.NET SDK 10**
- **Node.js 20+**
- **Docker** (Postgres, Weaviate, MinIO, RabbitMQ)
- **Ollama** — [instale aqui](https://ollama.com) (para chat e embeddings locais, ex. `nomic-embed-text`)

---

## Como rodar

A forma mais simples é via Docker Compose (sobe Postgres, Weaviate, MinIO, RabbitMQ, backend, worker e frontend):

```bash
docker-compose up --build
```

O backend aplica as migrações do EF Core automaticamente na inicialização, cria o usuário `admin` com senha `1234` (se ainda não existir nenhum usuário) e o `minio-init` configura o bucket e a notificação AMQP do MinIO para o RabbitMQ. Ollama deve estar rodando no host (`host.docker.internal:11434`).

> **Notas de porta:** a `8090` (backend), `8085` (Weaviate REST) e outras foram escolhidas para não colidir com serviços já rodando nesta máquina (ex. Portainer na `8000`, WordPress na `8080`) — ajuste no `docker-compose.yml` se necessário.

### Rodando sem Docker (dev local)

```bash
# 1. Suba Postgres, Weaviate, MinIO e RabbitMQ (docker-compose up postgres weaviate minio minio-init rabbitmq)

# 2. Backend
cd backend/src/OpenChatAgents.Api
dotnet run

# 3. Worker (em outro terminal)
cd backend/src/OpenChatAgents.Worker
dotnet run

# 4. Frontend
cd frontend
cp .env.local.example .env.local
npm install
npm run dev
```

Backend em `http://localhost:8090`, frontend em `http://localhost:3000`.

---

## Login

Primeiro acesso: usuário **admin**, senha **1234** — o sistema obriga a troca de senha no primeiro login. Só o admin pode cadastrar novos usuários (`/admin/users`); uma vez logado, qualquer usuário pode criar agentes e bases de conhecimento, que são públicos e visíveis para todos.

---

## Variáveis de configuração

Todas em `AppSettings` (backend e worker), com variáveis de ambiente usando `__` como separador em Docker (ex.: `AppSettings__Minio__Endpoint`):

| Seção | Chaves | Descrição |
|-------|--------|-----------|
| `ConnectionStrings:Default` | — | String de conexão Npgsql |
| `AppSettings:OllamaBaseUrl` / `LlmModel` | — | Ollama padrão |
| `AppSettings:Aws` | `Region`, `AccessKeyId`, `SecretAccessKey` | Bedrock (vazio = cadeia padrão AWS) |
| `AppSettings:Jwt` | `Secret`, `Issuer`, `Audience`, `ExpiryMinutes` | Emissão de token JWT — **troque o `Secret` em produção** |
| `AppSettings:Minio` | `Endpoint`, `AccessKey`, `SecretKey`, `Bucket` | Armazenamento dos arquivos enviados às KBs |
| `AppSettings:RabbitMq` | `HostName`, `ExchangeName`, `QueueName` | Fila de ingestão |
| `AppSettings:Weaviate` | `RestEndpoint`, `RestPort`, `GrpcEndpoint`, `GrpcPort` | Banco vetorial |
| `AppSettings:Argon2` | `MemoryKb`, `Iterations`, `Parallelism` | Custo do hash de senha |

### Frontend — `frontend/.env.local`

| Variável | Padrão |
|----------|--------|
| `NEXT_PUBLIC_API_URL` | `http://127.0.0.1:8090` |

---

## API

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `POST` | `/api/v1/auth/login` | Login, retorna JWT |
| `POST` | `/api/v1/auth/change-password` | Troca a própria senha |
| `GET` | `/api/v1/auth/me` | Usuário autenticado |
| `POST`/`GET`/`DELETE` | `/api/v1/users` | Gestão de usuários (admin) |
| `POST`/`GET`/`PUT`/`DELETE` | `/api/v1/agents` | Agentes (provider, modelo, temperatura, prompt, KBs associadas) |
| `POST`/`GET`/`DELETE` | `/api/v1/sessions` | Sessões de conversa |
| `POST` | `/api/v1/chat/stream` | Envia mensagem (streaming via SSE, com RAG se o agente tiver KBs) |
| `GET` | `/api/v1/chat/{sessionId}/history` | Histórico da sessão |
| `GET` | `/api/v1/models/ollama` \| `/bedrock` | Modelos disponíveis |
| `POST`/`GET`/`DELETE` | `/api/v1/knowledge-bases` | Bases de conhecimento |
| `POST` | `/api/v1/knowledge-bases/{id}/documents` | Upload de documento (multipart) |
| `GET` | `/api/v1/knowledge-bases/{id}/documents` | Status dos documentos da KB |

Todos os endpoints exigem `Authorization: Bearer <token>`, exceto o login.

---

## License

MIT
