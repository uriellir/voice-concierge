# Technical Decisions

## Initial stack choice
- .NET for backend API
- TypeScript for LiveKit voice agent
- PostgreSQL for persistence
- Docker Compose for local orchestration

## Planned architecture
- API owns knowledge retrieval and unanswered-question logging
- Agent handles voice orchestration, prompting, and speech response
- Database stores knowledge items, embeddings, and unanswered questions