# Meridian Voice Concierge

Meridian Voice Concierge is a local interview-project implementation of a luxury hotel voice support assistant for The Meridian Casino & Resort.

It includes:
- a `.NET 10` backend API for knowledge retrieval and unanswered-question logging
- a `TypeScript` LiveKit voice agent
- a `PostgreSQL` database
- a `Docker Compose` setup for local evaluation

The current implementation supports:
- FAQ search with hybrid retrieval
- unanswered-question tracking
- seeded Meridian property data
- a LiveKit-powered voice playground flow

## Architecture

The system is split into two layers:

- `src/api/VoiceConcierge`
  - owns the knowledge base
  - stores raw property data, retrieval items, embeddings, and unanswered questions
  - exposes backend endpoints for search and question logging

- `src/agent`
  - owns the guest-facing voice experience
  - connects to LiveKit
  - calls the backend API for grounded answers
  - turns those results into concierge-style spoken responses

## Project Structure

- `src/api/VoiceConcierge` - backend API
- `src/agent` - LiveKit voice agent
- `docs/technical-decisions.md` - technical design summary
- `docker-compose.yml` - local orchestration for database, API, and agent

## Prerequisites

Install these before running the project:

- `Docker Desktop`
- `.NET SDK 10`
- `Node.js 20+`
- `npm`

You also need external credentials for:

- `LiveKit`
- `OpenAI API`

## Environment Setup

Create a file named [`.env`](C:/Dev/VoiceConcierge/.env) at the repository root.

Use this template:

```env
# PostgreSQL
POSTGRES_DB=meridian
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_PORT=5432

# API
API_PORT=8080
CONNECTION_STRING=Host=db;Port=5432;Database=meridian;Username=postgres;Password=postgres
API_BASE_URL=http://localhost:5282
CONCIERGE_TOP_K=5
CONCIERGE_MIN_CONFIDENCE=0.55
CONCIERGE_AGENT_NAME=Meridian Voice Concierge

# LiveKit
LIVEKIT_URL=wss://your-project.livekit.cloud
LIVEKIT_API_KEY=your-livekit-api-key
LIVEKIT_API_SECRET=your-livekit-api-secret

# LLM
LLM_API_KEY=your-openai-api-key
LLM_MODEL=gpt-4o-mini
LLM_BASE_URL=https://api.openai.com/v1

Notes:

- `LLM_API_KEY` is used in two places:
  - backend embeddings
  - agent response composition
- `API_BASE_URL=http://localhost:5282` is correct for local non-Docker agent runs
- in Docker Compose, the agent uses `http://api:8080` internally

## First-Time Setup From Git Clone

If someone downloads the repo fresh from Git, these are the exact steps to run it.

### 1. Clone the repository

```powershell
git clone <your-repo-url>
cd VoiceConcierge
```

### 2. Create the root `.env`

Create [`.env`](C:/Dev/VoiceConcierge/.env) and fill in:

- LiveKit URL/key/secret
- OpenAI API key

### 3. Install agent dependencies

```powershell
cd C:\Dev\VoiceConcierge\src\agent
npm install
```

### 4. Download LiveKit model files once

```powershell
npm run build
node dist/main.js download-files
```

This downloads the local turn-detection files needed by the LiveKit worker.

### 5. Start PostgreSQL

From the repo root:

```powershell
cd C:\Dev\VoiceConcierge
docker compose up db -d
```

### 6. Start the API

In a new terminal:

```powershell
cd C:\Dev\VoiceConcierge\src\api\VoiceConcierge
dotnet run
```

The backend will:

- run migrations
- seed the Meridian data
- start on `http://localhost:5282`

### 7. Generate embeddings

After the API is running, call:

```http
POST http://localhost:5282/api/knowledge/reindex
Content-Type: application/json

{
  "force": true
}
```

This creates fresh embeddings for the seeded knowledge items.

### 8. Start the LiveKit agent

In another terminal:

```powershell
cd C:\Dev\VoiceConcierge\src\agent
node dist/main.js dev
```

### 9. Open the LiveKit Agents Playground

Use:

[https://agents-playground.livekit.io](https://agents-playground.livekit.io)

Connect it to the same LiveKit project and select the registered agent:

- `meridian-concierge`

Then:

- allow microphone access
- join a session
- begin speaking to the agent

## Local Run Options

### Option A: Recommended local dev flow

Use separate terminals:

```powershell
# terminal 1
cd C:\Dev\VoiceConcierge
docker compose up db -d

# terminal 2
cd C:\Dev\VoiceConcierge\src\api\VoiceConcierge
dotnet run

# terminal 3
cd C:\Dev\VoiceConcierge\src\agent
npm run build
node dist/main.js dev
```

### Local developer helpers

There are two useful local-only helpers in the repo:

- [VoiceConcierge.http](C:/Dev/VoiceConcierge/src/api/VoiceConcierge/VoiceConcierge.http)
  - a simple HTTP scratch file for manual backend testing from editors that support `.http` requests
  - useful for quickly calling endpoints like:
    - `/api/knowledge/search`
    - `/api/knowledge/reindex`
    - `/api/concierge/ask`
    - `/api/unanswered`

- [index.ts](C:/Dev/VoiceConcierge/src/agent/src/index.ts)
  - a console-only agent entrypoint for local development without LiveKit
  - useful if you want to validate:
    - backend connectivity
    - response composition
    - LLM-based concierge phrasing
  - run it with:

```powershell
cd C:\Dev\VoiceConcierge\src\agent
npm run dev:console
```

That mode lets you type guest questions directly into the terminal and inspect replies before testing the full voice path.

### Option B: Docker Compose stack

From the repo root:

```powershell
cd C:\Dev\VoiceConcierge
docker compose up -d --build
```

This starts:

- `db`
- `api`
- `agent`

Useful commands:

```powershell
docker compose ps
docker compose logs -f api
docker compose logs -f agent
docker compose down
```

## Development Notes

For day-to-day development, the easiest workflow is usually:

1. run Postgres with Docker
2. run the API locally with `dotnet run`
3. run either:
   - `node dist/main.js dev` for the LiveKit worker
   - `npm run dev:console` for text-only debugging

That gives faster iteration than rebuilding the full Docker stack every time.

The Dockerized API is exposed at:

- `http://localhost:8080`

## Important Endpoints

### Search the knowledge base

```http
POST http://localhost:5282/api/knowledge/search
Content-Type: application/json

{
  "query": "What time is check-in?",
  "topK": 5,
  "minimumConfidence": 0.55
}
```

### Concierge-ready response

```http
POST http://localhost:5282/api/concierge/ask
Content-Type: application/json

{
  "query": "What's your best restaurant?"
}
```

### Record an unanswered question manually

```http
POST http://localhost:5282/api/unanswered
Content-Type: application/json

{
  "question": "Can I bring my dog to the hotel?"
}
```

### Rebuild embeddings

```http
POST http://localhost:5282/api/knowledge/reindex
Content-Type: application/json

{
  "force": true
}
```

## Suggested Demo Questions

Use these in the playground:

- `Is the poker room open right now?`
- `What time is check-in?`
- `What's your best restaurant?`
- `Are there any good restaurants nearby you'd recommend?`
- `Can I bring my dog to the hotel?`

## Current Core vs Bonus Status

### Core implemented

- backend knowledge API
- seeded Meridian property data
- hybrid FAQ retrieval
- unanswered-question tracking
- LiveKit voice-agent integration
- local playground testing path
- Docker Compose stack

### Bonus not fully implemented yet

- admin panel for managing FAQ content
- unanswered-to-FAQ review workflow
- voice personality management UI
- embedded playground inside an admin interface

## Troubleshooting

### LiveKit agent starts but errors about turn detector files

Run once from [src/agent](C:/Dev/VoiceConcierge/src/agent):

```powershell
npm run build
node dist/main.js download-files
```

### Playground connects but no answers come back

Check:

- API is running
- agent is running
- `.env` has the correct LiveKit credentials
- `LLM_API_KEY` is present
- `meridian-concierge` appears in your LiveKit dashboard

### Docker stack runs but the playground still uses the old local worker

Stop any local `node dist/main.js dev` process so only the containerized agent is active.

## Security Note

Do not commit [`.env`](C:/Dev/VoiceConcierge/.env).

If any API keys were exposed during development, rotate them before sharing or submitting the project.
