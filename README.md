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

## First-Time Setup From Git Clone

If someone downloads the repo fresh from Git, these are the recommended steps to run the full LiveKit path.

### 1. Clone the repository

```powershell
git clone <your-repo-url>
cd VoiceConcierge
```

### 2. Create the root `.env`

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
```

Notes:

- `LLM_API_KEY` is used in two places:
  - backend embeddings
  - agent response composition
- `API_BASE_URL=http://localhost:5282` is correct for local non-Docker agent runs
- in Docker Compose, the agent uses `http://api:8080` internally

Create [`.env`](C:/Dev/VoiceConcierge/.env) using the template above and fill in:

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

## Running the Full LiveKit Flow

This is the main evaluation path for the project.

### 1. Docker Compose Stack

From the repo root:

```powershell
cd C:\Dev\VoiceConcierge
docker compose up -d --build
```

This starts:

- `db`
- `api`
- `agent`

The Dockerized API is exposed at:
- `http://localhost:8080`

### 2. Generate embeddings

After the API is running, call (can do also from swagger):

```http
POST http://localhost:5282/api/knowledge/reindex
Content-Type: application/json

{
  "force": true
}
```

This creates fresh embeddings for the seeded knowledge items.

### 3. Using the LiveKit Agent Playground

Once the backend and agent are running, this is the quickest way to test the full voice experience.

### Playground steps

1. Open [https://agents-playground.livekit.io](https://agents-playground.livekit.io)
2. Connect it to the same LiveKit project used by the agent
3. Use the same values from [`.env`](C:/Dev/VoiceConcierge/.env):
   - `LIVEKIT_URL`
   - `LIVEKIT_API_KEY`
   - `LIVEKIT_API_SECRET`
4. Select or target the registered agent:
   - `meridian-concierge`
5. Join a session or room
6. Allow microphone access
7. Start speaking to the concierge

### Good first test questions

- `What time is check-in?`
- `Is the poker room open right now?`
- `What's your best restaurant?`
- `Can I bring my dog to the hotel?`

### What should happen

- your speech is transcribed by the LiveKit voice pipeline
- the agent calls the backend `/api/concierge/ask`
- the backend returns a grounded result or fallback
- the agent responds with spoken concierge-style audio

### If the playground does not see the agent

Check:

- the agent worker is still running
- the agent is registered in your LiveKit dashboard as `meridian-concierge`
- the playground is using the same LiveKit project credentials as the running worker
- you do not have an old local worker process using different credentials

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

## Developer Helpers Without LiveKit

These are optional local development helpers

### `VoiceConcierge.http`

[VoiceConcierge.http](C:/Dev/VoiceConcierge/src/api/VoiceConcierge/VoiceConcierge.http) is a simple HTTP scratch file for manual backend testing from editors that support `.http` requests.

It is useful for quickly calling endpoints like:

- `/api/knowledge/search`
- `/api/knowledge/reindex`
- `/api/concierge/ask`
- `/api/unanswered`

### Console-only agent mode

[index.ts](C:/Dev/VoiceConcierge/src/agent/src/index.ts) is a console-only agent entrypoint for local development without LiveKit.

It is useful if you want to validate:

- backend connectivity
- response composition
- LLM-based concierge phrasing

Run it with:

```powershell
cd C:\Dev\VoiceConcierge\src\agent
npm run dev:console
```

That mode lets you type guest questions directly into the terminal and inspect replies before testing the full voice path.

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
