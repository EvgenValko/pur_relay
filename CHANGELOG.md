# Changelog

All notable changes to PurrNet Relay Server will be documented in this file.

## [1.0.3] - 2025-10-04

### Added
- **HTTP health check server** for deployment platform compatibility
  - Runs on port 8080 (configurable with `--http-port`)
  - Endpoints: `/health`, `/status`, `/ping`
  - Essential for platforms like Railway, Render, Kubernetes
  - Dockerfile now includes HEALTHCHECK directive
- `HealthCheckServer.cs` - Lightweight HTTP server for monitoring
- `DEPLOYMENT.md` - Complete deployment guide for various platforms
- Statistics API at `/status` endpoint

### Fixed
- Deployment failures due to missing HTTP health checks
- Docker health check configuration

## [1.0.2] - 2025-10-04

### Fixed
- **Critical**: Fixed data packet parsing that caused players not to spawn
  - Was using incorrect offset (`reader.Position`) to extract game data
  - Now uses `reader.GetRemainingBytesSegment()` to properly extract remaining data after headers
  - This ensures game data (spawn packets, etc.) is correctly forwarded between host and clients

## [1.0.1] - 2025-10-04

### Fixed
- **Critical**: Fixed string encoding error when Unity clients connect to relay server
  - Unity was using `PurrNet.Packing.Packer` format for strings
  - Relay server expected `LiteNetLib.NetDataWriter` format
  - Solution: Updated `RelayTransport.cs` to use `NetDataWriter` for all relay communication
  - Error message was: "Unable to translate bytes at index X from specified code page to Unicode"

### Changed
- `RelayTransport.cs` now uses `LiteNetLib.Utils.NetDataWriter` instead of `BitPacker` for:
  - CreateRoom requests
  - JoinRoom requests  
  - LeaveRoom requests
  - Data packet forwarding
  - All communication with relay server
- Data extraction now uses `GetRemainingBytesSegment()` for correct offset handling

### Added
- `TROUBLESHOOTING.md` - Comprehensive troubleshooting guide
- Better error handling and logging in relay server

## [1.0.0] - 2025-10-04

### Added
- Initial release of PurrNet Relay Server
- Standalone .NET relay server using LiteNetLib
- Unity `RelayTransport` component for PurrNet
- Room-based matchmaking system
- Automatic room cleanup
- Command-line configuration options
- Docker support
- Startup scripts for Windows and Linux
- Comprehensive documentation:
  - README.md
  - QUICKSTART.md
  - IMPLEMENTATION_SUMMARY.md
  - TROUBLESHOOTING.md

### Features
- UDP-based relay using LiteNetLib 1.2.0
- Configurable max rooms (default: 1000)
- Configurable max clients per room (default: 100)
- Configurable tick rate (default: 30 Hz)
- Connection timeout handling (default: 10 seconds)
- Thread-safe room management
- Statistics tracking (connections, rooms, uptime)
- Graceful shutdown on Ctrl+C
- Cross-platform support (Windows, Linux, macOS)

### Protocol
- Binary protocol over UDP
- 8 packet types for room management and data forwarding
- Efficient packet forwarding between host and clients
- Low latency overhead (5-50ms depending on relay location)

### Unity Integration
- `RelayTransport` component compatible with PurrNet
- Supports all PurrNet channel types
- Automatic room code generation
- Configurable settings in Unity Inspector
- Full state management (Connecting, Connected, etc.)

### Deployment Options
- Direct .NET execution
- Docker container
- systemd service (Linux)
- Windows Service (with NSSM)

### Documentation
- Complete API documentation
- Quick start guide
- Deployment guides for multiple platforms
- Performance tips and benchmarks
- Troubleshooting guide

## Future Enhancements (Planned)

### Security
- [ ] Authentication/authorization system
- [ ] Room passwords
- [ ] Rate limiting per connection
- [ ] DDoS protection

### Features
- [ ] Room metadata (game type, map, player count)
- [ ] Room browser/listing API
- [ ] Master server integration
- [ ] Statistics API endpoint
- [ ] Health check endpoint
- [ ] Admin API for room management

### Performance
- [ ] Bandwidth limiting per room/client
- [ ] Connection pooling optimizations
- [ ] Metrics export (Prometheus/Grafana)
- [ ] Load balancing support
- [ ] Regional server selection

### Quality of Life
- [ ] Web-based admin dashboard
- [ ] Automatic reconnection handling
- [ ] Room persistence on relay restart
- [ ] Spectator mode support
- [ ] Voice chat relay (optional)

---

## Version History

| Version | Date | Notes |
|---------|------|-------|
| 1.0.3 | 2025-10-04 | Added HTTP health checks for deployment |
| 1.0.2 | 2025-10-04 | Fixed data packet parsing (player spawn) |
| 1.0.1 | 2025-10-04 | Fixed string encoding bug |
| 1.0.0 | 2025-10-04 | Initial release |

## Upgrade Guide

### From 1.0.1 to 1.0.2

No breaking changes. Simply update `RelayTransport.cs` in your Unity project:

1. Copy new `RelayTransport.cs` to your project
2. Rebuild your project
3. Players should now spawn correctly

The relay server itself has no changes and is fully compatible.

### From 1.0.0 to 1.0.1

No breaking changes. Simply update `RelayTransport.cs` in your Unity project:

1. Copy new `RelayTransport.cs` to your project
2. Rebuild your project
3. Test connection to relay server

The relay server itself has no changes and is fully compatible.

## Contributing

When submitting changes, please:
1. Update this CHANGELOG
2. Increment version in project file
3. Update documentation if needed
4. Add tests for new features

## Semantic Versioning

This project follows [Semantic Versioning](https://semver.org/):
- MAJOR version for incompatible API changes
- MINOR version for backwards-compatible functionality additions
- PATCH version for backwards-compatible bug fixes

