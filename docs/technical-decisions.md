# Technical Decisions

## Overview

This project is designed as a grounded voice concierge for The Meridian Casino & Resort.

The key architectural choice was to separate:

- knowledge retrieval and persistence in the backend API
- voice orchestration and concierge-style response generation in the LiveKit agent

This keeps factual lookup deterministic while allowing the guest experience layer to stay flexible and conversational.

## Stack Choices

### Backend API: .NET 10
### Voice Agent: TypeScript + LiveKit

### Database: PostgreSQL

PostgreSQL was selected for:

- simple local setup
- solid support with EF Core
- structured storage for knowledge records and unanswered-question analytics

## Architecture

The project is split into two runtime services:

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
