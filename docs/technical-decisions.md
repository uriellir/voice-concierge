# Technical Decisions

## Overview

This project is designed as a grounded voice concierge for The Meridian Casino & Resort.

The key architectural choice was to separate:

- knowledge retrieval and persistence in the backend API
- voice orchestration and concierge-style response generation in the LiveKit agent
- admin workflows in a dedicated React admin panel

This keeps factual lookup deterministic while allowing the guest experience layer to stay flexible and conversational, and leaves room for staff-facing workflows without mixing them into the guest voice runtime.

## Stack Choices

### Backend API: .NET 10
### Voice Agent: TypeScript + LiveKit
### Admin Panel: React

### Database: PostgreSQL

PostgreSQL was selected for:

- simple local setup
- solid support with EF Core
- structured storage for knowledge records and unanswered-question analytics

Entity Framework Core was used for:

- schema management through migrations
- database access from the API layer
- keeping the persistence model simple and explicit for a small project

## Architecture

The project is split into three runtime services:

### 1. Backend API

Responsibilities:

- store seeded Meridian property information
- transform raw rows into retrieval-friendly knowledge items
- generate and store embeddings
- perform hybrid search
- log unanswered questions and their frequency

### 2. Voice Agent

Responsibilities:

- receive guest speech through LiveKit
- invoke the backend for grounded answers
- produce concierge-style spoken responses
- gracefully handle no-answer cases

### 3. Admin Panel

Responsibilities:

- provide a staff-facing interface for bonus workflows
- surface FAQ items from the knowledge base
- surface unanswered guest questions and their frequency
- allow staff to view and change the active concierge voice
- create a clear foundation for future FAQ and voice-management modules

The current admin implementation focuses on:

- FAQ list and search
- unanswered-questions review workflow
- voice selection and current active voice display
- an embedded browser-based test mode playground

This separation makes it easier to explain, test, and evolve each concern independently.

## Why Hybrid Retrieval Instead of Embeddings Alone

An early version relied more heavily on embeddings. In practice, that produced false positives for structured hospitality data.

Example problem:

- semantically similar but wrong answers could score unexpectedly well
- a query like an out-of-domain guest request could still match a room or suite result

To improve reliability, the search now combines:

- embedding similarity for semantic recall
- keyword/token overlap for grounding
- acceptance thresholds and fallback rules

### Current retrieval strategy

The backend:

- expands the user query slightly for hospitality phrasing
- compares it with stored embedding vectors
- measures overlap against title, category, subtitle, and details
- combines those signals into a final confidence score
- rejects weak vector-only matches

## Why Unanswered Questions Are Stored

Capturing unanswered questions is a required product feature and also an important operational feedback loop.

The system stores:

- normalized question text
- original text
- first seen timestamp
- last seen timestamp
- frequency count

This enables future FAQ improvement and helps prioritize what staff should add next.

That same data is now also surfaced in the admin panel so the feedback loop is visible, not just stored.

## Why the Agent Uses a Response Composer

Raw retrieval output is not enough to match the example conversations.

The examples require:

- a warm luxury-hospitality tone
- combining multiple relevant knowledge items for recommendation questions
- graceful fallback wording for unknown questions

To support this, the agent includes a response-composition layer that:

- takes grounded backend results
- uses an LLM when available to rewrite them into a polished spoken response
- falls back to templates when the LLM is unavailable

Important constraint:

The LLM is not used as the source of truth. It is used only to rephrase retrieved facts, not to invent facts.

## Why LiveKit Playground Was Used

A playground interface for testing voice interactions.

LiveKit Agents Playground is the fastest path to the required capabilities:

- microphone input
- spoken output
- connection/session state
- start/end interaction flow

This allowed me to focus implementation effort on:

- retrieval quality
- voice-agent behavior
- backend correctness

instead of building a custom browser UI from scratch.

## Why A Separate React Admin Panel Was Added

The bonus requirements call for a staff-facing admin experience, which is a different interaction model from the guest voice flow.

A separate React admin panel was added so that:

- staff workflows stay isolated from the guest-facing voice path
- unanswered-question review can be demonstrated visually
- future FAQ CRUD and voice settings can be added without changing the core voice architecture

The first implemented admin slice focuses on:

- viewing FAQ items from the live knowledge base
- searching FAQ items from the admin UI
- viewing unanswered questions
- viewing frequency counts for repeated unanswered questions
- viewing the available voice options
- selecting the active voice for new conversations
- testing concierge conversations inside the admin UI

The FAQ view is intentionally read-only at this stage so the first admin increment can validate the data flow and layout before adding create, edit, and delete workflows.

## Why Voice Configuration Is Stored In The Backend

The active voice is stored in the backend rather than only in the frontend so the setting becomes part of the system state, not just the current browser session.

This makes it possible to:

- keep the selected voice consistent across admin sessions
- let the voice agent read the latest setting for each new conversation
- apply voice changes without restarting the running agent

The current implementation applies the selected voice to new conversations by having the agent fetch the active voice from the API when a new session starts.

## Why The Integrated Playground Uses Browser Speech APIs

The admin panel includes an embedded `Test Mode` playground so staff can test the concierge experience without leaving the admin UI.

For this stage, the integrated playground uses browser speech recognition and speech synthesis rather than a full in-browser LiveKit client.

Why:

- it provides the required embedded admin-side conversation flow quickly
- it reuses the existing `/api/concierge/ask` backend path
- it keeps the integrated playground lightweight while the core LiveKit agent remains unchanged

Tradeoff:

- the embedded admin playground is a practical browser test mode, not yet a full browser LiveKit session
- spoken output depends on browser-available voices, even though the selected active voice is still reflected in the admin experience

## Seeded CSV Data

- load CSV into raw records
- transform raw records into knowledge items
- generate embeddings from those knowledge items

This keeps the data pipeline simple and reproducible for local evaluation.

## Docker Compose Decision

The project includes `Docker Compose` so anyone can launch:

- database
- API
- agent
- admin panel

with one command.

## Known Tradeoffs

### Vector storage

Embeddings are currently stored as JSON in PostgreSQL rather than using `pgvector`.

Why:

- the dataset is small and fixed

Future improvement:

- move to `pgvector` or another vector index if the corpus grows

### Response composition

The agent uses retrieved facts plus an LLM rewriter for hospitality polish.

Why:

- it produces more natural voice output
- it better matches the example conversations

Risk:

- any generative layer introduces some risk of drift

Mitigation:

- keep the LLM constrained to retrieved facts only
- retain explicit no-match fallback behavior
