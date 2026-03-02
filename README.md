# TemperatureAnalysisService

Overview
--------
TemperatureAnalysisService is a background worker that consumes temperature readings from RabbitMQ, analyzes them, and publishes analysis results back to RabbitMQ. This project is part of a demo illustrating use of mocks for unit and integration tests.

Contents
--------
- `src/Program.cs` — application entry point
- `src/Worker.cs` — background worker wiring consumer/publisher and analyzer
- `src/Processing/TemperatureAnalyzer.cs` — core analysis logic
- `src/Messaging/RabbitMqConnection.cs` — RabbitMQ connection helper
- `src/Messaging/RabbitMqConsumer.cs` — consumes readings
- `src/Messaging/RabbitMqPublisher.cs` — publishes results
- `src/Contracts/*` — shared DTOs: `TemperatureReading`, `TemperatureAnalysisResult`, `TemperatureStatus`

Prerequisites
-------------
- .NET 10 SDK (or matching SDK for the project)
- Docker & Docker Compose (optional, for running with RabbitMQ container)

Configuration
-------------
Configuration is provided via `appsettings.json` / `appsettings.Development.json` in the `src` folder. RabbitMQ connection settings are configured there. Update the host, username/password, and queue names as needed.

Running locally
----------------
1. From the `src` folder, restore/build/run:

```bash
cd TemperatureAnalysisService/src
dotnet restore
dotnet build
dotnet run
```

2. The worker reads from the configured input queue (e.g. `temperature.readings`) and publishes to the output queue (e.g. `temperature.results`).

Running with Docker Compose
--------------------------
This folder includes a `docker-compose.yml` that can bring up a RabbitMQ instance for local testing. Example:

```bash
cd TemperatureAnalysisService
docker compose up -d
```

Then run the app (locally or in a container) pointed at the RabbitMQ service.

How it works (high-level)
-------------------------
1. `RabbitMqConsumer` receives messages containing `TemperatureReading` DTOs.
2. `TemperatureAnalyzer` evaluates readings and produces a `TemperatureAnalysisResult` with a `TemperatureStatus` (OK, Warning, Critical).
3. `RabbitMqPublisher` sends the result to the configured results queue.

Testing and mocks
-----------------
This demo is focused on showing how to use mocks for unit and integration tests. Suggested approaches:

- Unit tests: Extract dependencies behind small interfaces (for example, publisher/consumer wrappers or a `IRabbitMqClient`), then mock those in unit tests to exercise `TemperatureAnalyzer` and `Worker` behavior without real RabbitMQ.
- Integration tests: Use a test RabbitMQ instance (Docker Compose) and a lightweight test harness that sends sample readings and asserts results. Alternatively, use in-memory fakes for quicker CI-friendly tests.

Where to add tests
------------------
Create a separate test project (xUnit / NUnit) next to `src` (e.g. `tests/TemperatureAnalysisService.Tests`). Add unit tests for `TemperatureAnalyzer` and higher-level behavior where RabbitMQ dependencies are mocked.

Notes
-----
- Example appsettings are present in the `bin/Debug/net10.0` output; use them as a reference.
- Logs and worker lifecycle are handled by the default `BackgroundService` patterns in `Worker.cs`.

License
-------
- This project is licensed under the MIT license. See the [LICENSE](LICENSE) for details.
