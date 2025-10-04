#!/bin/bash

# PurrNet Relay Server Startup Script
# Usage: ./start-server.sh [port] [max-rooms] [tick-rate]

PORT=${1:-9050}
MAX_ROOMS=${2:-1000}
TICK_RATE=${3:-30}

echo "Starting PurrNet Relay Server..."
echo "Port: $PORT"
echo "Max Rooms: $MAX_ROOMS"
echo "Tick Rate: $TICK_RATE Hz"
echo ""

dotnet run -- --port $PORT --max-rooms $MAX_ROOMS --tick-rate $TICK_RATE

