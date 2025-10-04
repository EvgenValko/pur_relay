FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app .

# Expose the default relay port (UDP)
EXPOSE 9050/udp

ENTRYPOINT ["dotnet", "PurrNetRelayServer.dll"]
CMD ["--port", "9050", "--max-rooms", "1000", "--tick-rate", "30"]

