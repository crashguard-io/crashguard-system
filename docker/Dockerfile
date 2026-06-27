# Stage 1: Build React app
FROM node:24-alpine AS app-build
WORKDIR /build
COPY src/crashguard-app/package*.json ./
RUN npm ci
COPY src/crashguard-app/ ./
RUN npm run build

# Stage 2: Build .NET engine
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS engine-build
WORKDIR /build
COPY Crashguard.sln ./
COPY src/crashguard-engine/ src/crashguard-engine/
COPY src/crashguard-common/ src/crashguard-common/
COPY src/crashguard-client/ src/crashguard-client/
RUN dotnet publish src/crashguard-engine/Crashguard.Engine.csproj \
    -c Release \
    -o /app/engine \
    --no-self-contained

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends nginx supervisor \
    && rm -rf /var/lib/apt/lists/*

# React app → nginx
COPY --from=app-build /build/dist /var/www/html
COPY docker/nginx.conf /etc/nginx/nginx.conf

# .NET engine
COPY --from=engine-build /app/engine /app/engine

# supervisord
COPY docker/supervisord.conf /etc/supervisor/conf.d/crashguard.conf

ENV ASPNETCORE_URLS=http://+:5050
ENV ConnectionStrings__DefaultConnection="Data Source=/data/crashguard.db"
VOLUME /data

EXPOSE 80 5050

CMD ["/usr/bin/supervisord", "-n", "-c", "/etc/supervisor/conf.d/crashguard.conf"]
