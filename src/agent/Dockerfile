FROM node:22-bookworm-slim AS build
WORKDIR /app

COPY src/agent/package*.json ./
RUN npm ci

COPY src/agent/ ./
RUN npm run build
RUN node dist/main.js download-files

FROM node:22-bookworm-slim AS final
WORKDIR /app

COPY --from=build /app/package*.json ./
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/dist ./dist
COPY --from=build /root/.cache /root/.cache

CMD ["node", "dist/main.js", "start"]
