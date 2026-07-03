FROM node:24-alpine AS build
WORKDIR /app
COPY apps/validatedscale/package*.json ./
RUN npm ci
COPY apps/validatedscale/ ./
RUN npm run build

FROM node:24-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV HOST=0.0.0.0
ENV PORT=3000
EXPOSE 3000
COPY --from=build /app/package*.json ./
RUN npm ci --omit=dev --ignore-scripts
COPY --from=build /app/build ./build
CMD ["node", "build"]
