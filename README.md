# open-chat-agents-dotnet

Porta em **.NET** do POC [open-chat-agents](../Chat%20Bot%20Com%20Rag%20e%20Front) — o mesmo chatbot com agentes configuráveis, streaming e persistência, agora com **ASP.NET Core** e o **Microsoft Agent Framework** no lugar de FastAPI + LangChain/LangGraph. O frontend Next.js é reaproveitado sem alterações: o contrato HTTP/SSE é idêntico ao do projeto original.

---

## Tech Stack

| Camada | Tecnologia |
|--------|-----------|
| Frontend | Next.js 15, React 19, Tailwind CSS (reaproveitado do projeto original) |
| Backend | ASP.NET Core 10, C# |
| Orquestração de LLM | [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) (`Microsoft.Agents.AI`) |
| LLM | Qualquer modelo via Ollama (`OllamaSharp`) ou AWS Bedrock (`AWSSDK.Extensions.Bedrock.MEAI`) |
| Banco de dados | PostgreSQL 16 |
| ORM | Entity Framework Core 10 (`Npgsql.EntityFrameworkCore.PostgreSQL`) |
| Migrações | EF Core Migrations |

---

## Arquitetura

```
frontend/                          backend/src/OpenChatAgents.Api/
├── src/                           ├── Controllers/     HTTP layer (Agents, Sessions, Chat, Models)
│   ├── app/          Next.js      ├── Services/        Regras de negócio
│   ├── components/   UI           ├── Repositories/    Acesso a dados (EF Core)
│   ├── services/     API calls    ├── Models/          Entidades EF Core
│   └── types/        Contratos    ├── Dtos/            Contratos HTTP (request/response)
│                                  ├── Agents/           ChatAgentFactory (Microsoft Agent Framework)
│                                  ├── Data/             AppDbContext + Migrations
│                                  └── Options/          Configuração fortemente tipada
```

**Fluxo da requisição de chat:**

```
Browser → Next.js → POST /api/v1/chat/stream → ChatController
                                                    ├── ModerationService   (guarda de profanidade)
                                                    ├── MessageRepository   (persiste)
                                                    └── ChatAgentFactory    (Microsoft Agent Framework + Ollama/Bedrock)
                                                            ↓ SSE stream (event: user_message | chunk | done)
Browser ← chunks chegando palavra por palavra ←──────────────────────────
```

O `ChatAgentFactory` (`backend/src/OpenChatAgents.Api/Agents/ChatAgentFactory.cs`) monta um `IChatClient` (Ollama ou Bedrock) a partir da configuração do `Agent` e cria um `AIAgent` via `chatClient.AsAIAgent(...)` do Microsoft Agent Framework, fazendo streaming token a token com `RunStreamingAsync`.

---

## Requisitos

- **.NET SDK 10**
- **Node.js 20+**
- **Docker** (para PostgreSQL)
- **Ollama** — [instale aqui](https://ollama.com)

---

## Como rodar

### 1. Escolha e inicie seu LLM

```bash
ollama pull gemma3
ollama serve
```

### 2. Suba o PostgreSQL

```bash
docker run -d --name open-chat-agents-dotnet-pg \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=chatbot \
  -p 5432:5432 \
  postgres:16-alpine
```

### 3. Backend

```bash
cd backend/src/OpenChatAgents.Api
dotnet restore

# aplica as migrações (ou deixe o app aplicar sozinho no startup)
dotnet tool install --global dotnet-ef   # se ainda não tiver
dotnet ef database update

dotnet run
```

A API sobe em `http://localhost:8090`. Documentação OpenAPI em `http://localhost:8090/openapi/v1.json` (ambiente Development).

> **Nota de porta:** o projeto Python original usa a porta `8000`; este usa `8090` para evitar conflito caso ambos rodem ao mesmo tempo na mesma máquina.

### 4. Frontend

```bash
cd frontend
cp .env.local.example .env.local   # já aponta para http://127.0.0.1:8090
npm install
npm run dev
```

Frontend em `http://localhost:3000`.

---

## Variáveis de configuração

### Backend — `backend/src/OpenChatAgents.Api/appsettings.json` (ou variáveis de ambiente equivalentes)

| Chave | Padrão | Descrição |
|-------|--------|-----------|
| `ConnectionStrings:Default` | — | String de conexão Npgsql |
| `AppSettings:OllamaBaseUrl` | `http://localhost:11434` | URL do servidor Ollama |
| `AppSettings:LlmModel` | `gemma3` | Modelo padrão quando a sessão não tem agente |
| `AppSettings:CorsOrigins` | `["http://localhost:3000"]` | Origens permitidas |
| `AppSettings:Aws:Region` | `us-east-1` | Região do Bedrock |
| `AppSettings:Aws:AccessKeyId` / `SecretAccessKey` | vazio | Deixe em branco para usar a cadeia padrão de credenciais AWS |

Em Docker Compose, essas chaves são passadas como variáveis de ambiente usando `__` como separador (ex.: `AppSettings__OllamaBaseUrl`).

### Frontend — `frontend/.env.local`

| Variável | Padrão |
|----------|--------|
| `NEXT_PUBLIC_API_URL` | `http://127.0.0.1:8090` |

---

## Rodando com Docker Compose

```bash
docker-compose up --build
```

O backend aplica as migrações do EF Core automaticamente na inicialização. Ollama deve estar rodando no host (`host.docker.internal:11434`).

---

## API

Mesmos endpoints e mesmo contrato JSON (snake_case) do projeto original:

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `POST` | `/api/v1/agents` | Cria um agente configurável (provider, modelo, temperatura, prompt) |
| `GET` | `/api/v1/agents` | Lista agentes |
| `GET`/`PUT`/`DELETE` | `/api/v1/agents/{id}` | Detalhe / atualiza / remove |
| `POST` | `/api/v1/sessions` | Cria sessão de conversa |
| `GET` | `/api/v1/sessions` | Lista sessões |
| `GET`/`DELETE` | `/api/v1/sessions/{id}` | Detalhe / remove |
| `POST` | `/api/v1/chat/stream` | Envia mensagem (streaming via SSE) |
| `GET` | `/api/v1/chat/{sessionId}/history` | Histórico da sessão |
| `GET` | `/api/v1/models/ollama` | Modelos disponíveis no Ollama |
| `GET` | `/api/v1/models/bedrock` | Modelos disponíveis no Bedrock |

---

## License

MIT
