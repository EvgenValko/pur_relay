FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app .

# Expose the default relay port (UDP) and health check port (HTTP)
EXPOSE 9050/udp
EXPOSE 8080/tcp

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "PurrNetRelayServer.dll"]
CMD ["--port", "9050", "--max-rooms", "1000", "--tick-rate", "30", "--http-port", "8080"]

