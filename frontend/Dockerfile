# Multi-stage: Node builds the Angular bundle, nginx serves it and proxies /api
# to the backend, so browser and API share one origin and no CORS is needed.
FROM node:22-alpine AS build
WORKDIR /app

# Dependencies first, so a source-only change reuses the cached npm layer.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:1.28-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/repair-shop-web/browser /usr/share/nginx/html
EXPOSE 80
