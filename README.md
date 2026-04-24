# Meridian Voice Concierge

Meridian Voice Concierge is a local implementation of a luxury hotel voice support assistant for The Meridian Casino & Resort.

It includes:
- a `.NET 10` backend API for knowledge retrieval and unanswered-question logging
- a `TypeScript` LiveKit voice agent
- a `React` admin panel
- a `PostgreSQL` database
- a `Docker Compose` setup for local evaluation

The current implementation supports:
- FAQ search with hybrid retrieval
- unanswered-question tracking
- seeded Meridian property data
- a LiveKit-powered voice playground flow
- an admin panel for reviewing FAQ items, unanswered questions, voice settings, and an embedded test-mode playground

## Architecture

The system is split into three layers:

- `src/api/VoiceConcierge`
  - owns the knowledge base
  - stores raw property data, retrieval items, embeddings, and unanswered questions
  - exposes backend endpoints for search, concierge responses, and admin data

- `src/agent`
  - owns the guest-facing voice experience
  - connects to LiveKit
  - calls the backend API for grounded answers
  - turns those results into concierge-style spoken responses

- `src/admin`
  - owns the admin experience
  - currently includes FAQ review, unanswered questions review, voice settings, and an embedded playground

## Project Structure

- `src/api/VoiceConcierge` - backend API
- `src/agent` - LiveKit voice agent
- `src/admin` - React admin panel
- `docs/technical-decisions.md` - technical design summary
- `docker-compose.yml` - local orchestration for database, API, agent, and admin UI

## Tech Notes

- `Entity Framework Core` is used for database access, schema management, and migrations
- `PostgreSQL` is the application database
- `DBeaver` can be used to inspect the local database tables during development

## Prerequisites

Install these before running the project:

- `Docker Desktop`
- `.NET SDK 10`
- `Node.js 20+`
- `npm`
- `DBeaver` (optional, for database inspection)

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
POSTGRES_DB=concierge
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_PORT=5432

# API
API_PORT=8080
CONNECTION_STRING=Host=db;Port=5432;Database=concierge;Username=postgres;Password=postgres
API_BASE_URL=http://localhost:8080
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
- `API_BASE_URL=http://localhost:8080` matches the Docker Compose API exposure
- use the same database name consistently across the project

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

## Running The Full Stack


This is the main local run path:

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
- `admin`

Available local URLs:

- API: [http://localhost:8080](http://localhost:8080)
- Admin UI: [http://localhost:5173](http://localhost:5173)

## Admin Panel

The admin panel is available at:

- [http://localhost:5173](http://localhost:5173)

The current admin implementation includes:

- a page title and admin shell
- a left navigation menu
- a right content panel based on the selected module
- an FAQ list view
- client-side FAQ search
- an unanswered questions queue
- frequency counts for each unanswered question
- a voice settings view
- active voice selection
- current active voice display
- an embedded playground labeled as `Test Mode`
- start and end conversation controls
- browser microphone input and spoken output
- transcript and response display inside the admin UI

This currently covers:

- View all FAQ items
- View unanswered questions queue
- See frequency count for each unanswered question
- Select active voice
- Display current active voice
- Embedded admin playground in test mode

### 2. Generate Embeddings

After the API is running, call:

```http
POST http://localhost:8080/api/knowledge/reindex
Content-Type: application/json

{
  "force": true
}
```

This creates fresh embeddings for the seeded knowledge items.

### 3. Testing The Voice Concierge

Use the LiveKit Agents Playground:

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

### Good first test questions:

- `What time is check-in?`
- `Is the poker room open right now?`
- `What's your best restaurant?`
- `Can I bring my dog to the hotel?`

### What should happen

- your speech is transcribed by the LiveKit voice pipeline
- the agent calls the backend `/api/concierge/ask`
- the backend returns a grounded result or fallback
- the agent responds with spoken concierge-style audio

### 4. Testing In The Admin Panel

The admin UI also includes an embedded `Test Mode` playground.

Open:

- [http://localhost:5173](http://localhost:5173)

Then:

1. open the `Playground` tab
2. click `Start conversation`
3. allow microphone access
4. ask a concierge question

The embedded playground:

- sends the recognized question to `/api/concierge/ask`
- uses the live FAQ knowledge base
- reflects the current active voice selection in the UI
- speaks the concierge response back in the browser

## Important Endpoints

### Search the knowledge base

```http
POST http://localhost:8080/api/knowledge/search
Content-Type: application/json

{
  "query": "What time is check-in?",
  "topK": 5,
  "minimumConfidence": 0.55
}
```

### Concierge-ready response

```http
POST http://localhost:8080/api/concierge/ask
Content-Type: application/json

{
  "query": "What's your best restaurant?"
}
```

### Record an unanswered question manually

```http
POST http://localhost:8080/api/unanswered
Content-Type: application/json

{
  "question": "Can I bring my dog to the hotel?"
}
```

### Read unanswered questions for the admin panel

```http
GET http://localhost:8080/api/admin/unanswered
```

### Read FAQ items for the admin panel

```http
GET http://localhost:8080/api/admin/faqs
```

### Read voice options for the admin panel

```http
GET http://localhost:8080/api/admin/voice-options
```

### Set the active voice

```http
PUT http://localhost:8080/api/admin/voice-options/active
Content-Type: application/json

{
  "voiceId": "james"
}
```

### Ask the concierge from the embedded playground

```http
POST http://localhost:8080/api/concierge/ask
Content-Type: application/json

{
  "query": "Is the poker room open right now?"
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

## Database Access

If you want to inspect the database directly:

- PostgreSQL runs on `localhost:5432`
- database name should match `POSTGRES_DB` in the root [`.env`](C:/Dev/VoiceConcierge/.env)
- you can use `DBeaver` to inspect tables like:
  - `knowledge_items`
  - `knowledge_embeddings`
  - `raw_property_records`
  - `unanswered_questions`

`Entity Framework Core` migrations are located under:

- [src/api/VoiceConcierge/Migrations](C:/Dev/VoiceConcierge/src/api/VoiceConcierge/Migrations)

## Developer Helpers

### `VoiceConcierge.http`

[VoiceConcierge.http](C:/Dev/VoiceConcierge/src/api/VoiceConcierge/VoiceConcierge.http) is a simple HTTP scratch file for manual backend testing from editors that support `.http` requests.

It is useful for quickly calling endpoints like:

- `/api/knowledge/search`
- `/api/knowledge/reindex`
- `/api/concierge/ask`
- `/api/unanswered`
- `/api/admin/faqs`
- `/api/admin/unanswered`
- `/api/admin/voice-options`

### Console-only agent mode

[index.ts](C:/Dev/VoiceConcierge/src/agent/src/index.ts) is a console-only agent entrypoint for local development without LiveKit.

Run it with:

```powershell
cd C:\Dev\VoiceConcierge\src\agent
npm run dev:console
```

## Troubleshooting

### The admin panel loads but shows no unanswered questions

Check:

- the admin UI is calling the same API instance you expect
- the API is using the same PostgreSQL database you expect
- the database name is consistent across local config and Docker config

### Playground connects but no answers come back

Check:

- API is running
- agent is running
- `.env` has the correct LiveKit credentials
- `LLM_API_KEY` is present
- `meridian-concierge` appears in your LiveKit dashboard

### The embedded admin playground does not start listening

Check:

- you are using a browser with speech-recognition support
- microphone permissions were granted
- the API is reachable on `http://localhost:8080`
- the browser supports speech synthesis for spoken replies

### LiveKit agent starts but errors about turn detector files

Run once from [src/agent](C:/Dev/VoiceConcierge/src/agent):

```powershell
npm run build
node dist/main.js download-files
```

## Security Note

Do not commit [`.env`](C:/Dev/VoiceConcierge/.env).

If any API keys were exposed during development, rotate them before sharing or submitting the project.
