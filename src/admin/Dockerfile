FROM node:20-alpine AS build
WORKDIR /app

COPY src/admin/package.json src/admin/package-lock.json ./
RUN npm ci

COPY src/admin/ ./

ARG API_BASE_URL=http://localhost:8080
ENV API_BASE_URL=${API_BASE_URL}

RUN npm run build

EXPOSE 4173

CMD ["npm", "run", "preview"]
