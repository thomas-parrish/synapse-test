# SignalBooster Assignment

This project demonstrates a refactoring of the original "Signal Booster" utility into a maintainable, testable, and production-ready .NET application using Domain-Driven Design (DDD) with an onion architecture.

---

## Requirements
- .NET 8 SDK
- OpenAI API key (only if using OpenAiNoteExtractor)

## 🛠 Tools Used

- **IDE:** Visual Studio 2022 (you can also use VS Code or Rider)
- **Framework:** .NET 8
- **Packages:**
  - `Microsoft.Extensions.*` (Configuration, Logging, Options, Http)
  - `Newtonsoft.Json` (for legacy formatter)
  - `Polly` + `Microsoft.Extensions.Http.Polly` (for resilient HTTP calls)
  - `xUnit` + `Verify.Xunit` (for testing and snapshot verification)
- **Optional AI:** OpenAI API (via `ILlmClient` + `OpenAiClient`)

---

## 🤖 AI Development Tools

ChatGPT was used as an AI pair-programmer during development for brainstorming, refactoring suggestions, test scaffolding, and documentation.  
No GitHub Copilot, Cursor, or Cody were used in this project.

---

## 📐 Assumptions & Limitations

- Physician notes can be **plain text** or **JSON-wrapped** in the form `{ "data": "..." }`.
- No specification for the External Endpoint API was given, so I made assumptions based on `expected_output1.json` as well as the code in `SignalBooster_Original.cs`._
- Supported device types:
  - CPAP
  - BiPAP
  - Oxygen Tank
  - Wheelchair
- Additional DME devices could be added by extending the `IPrescriptionParser` interface (and/or updating the OpenAi prompt.)
- I have had zero exposure to the DME domain and so I relied entirely on GPT for domain expertise. In a real scneario I would leverage the Product team SMEs for this information.
- AHI qualifier rules are simplified from American Academy of Sleep Medicine (AASM) cutoffs and may not reflect all insurance/billing guidelines.
- For structured extraction via OpenAI, the system prompt enforces JSON-only responses; however, malformed LLM output may still require future hardening.
- No database is included — the app is stateless and reads notes from files.

---

## 🚀 Running the Project

### 1. Restore and build

```bash
dotnet restore
dotnet build
```

### 2. Configure User Secrets

Set your OpenAI API key using .NET user secrets:

```bash
dotnet user-secrets set "OpenAI:ApiKey" "sk-<your key here>"
```

Example secret format:

```json
{
  "OpenAI": {
    "ApiKey": "sk-..."
  }
}
```

### 3. Adjust extraction mode

In `appsettings.json`, set:

```json
{
  "Extraction": {
    "UseOpenAI": true
  }
}
```

- `true` → use `OpenAiNoteExtractor`  
- `false` → use `SimpleNoteExtractor`

### 4. Run the console app

```bash
dotnet run --project SignalBooster.Console
```

The app will prompt you for a filename.

- If you type a fully-qualified path (`C:\files\note.txt`), it loads directly.
- If you type a relative file (`oxygen.txt`), it loads from the application’s base directory.

### 5. Sample files

The following are included and copied to the output directory:

- `cpap.json`
- `oxygen.txt`
- `wheelchair.txt`

Example run:

```
Enter path to physician note file:
oxygen.txt
```

The app will:

1. Extract a `PhysicianNote` domain object
2. Print its JSON representation
3. Send a formatted request to the external API endpoint

---
These outputs show the raw PhysicianNote domain object printed by the console app, not the external request formatter.

- wheelchair.txt
```json
{
  "OrderingPhysician": "Dr. Karen Blake",
  "PatientName": "Michael Andrews",
  "PatientDateOfBirth": "1975-07-15",
  "Diagnosis": "Multiple sclerosis with lower extremity weakness",
  "Prescription": {
    "device": "Wheelchair",
    "Type": "manual",
    "SeatWidthIn": 18,
    "SeatDepthIn": 16,
    "LegRests": "elevating",
    "Cushion": "gel",
    "Justification": null,
    "Device": 4
  }
}
```

- oxygen.txt
```json
{
  "OrderingPhysician": "Dr. Cuddy",
  "PatientName": "Harold Finch",
  "PatientDateOfBirth": "1952-04-12",
  "Diagnosis": "COPD",
  "Prescription": {
    "device": "Oxygen Tank",
    "FlowLitersPerMinute": 2,
    "Usage": 3,
    "Device": 3
  }
}
```

- cpap.json
```json
{
  "OrderingPhysician": "Dr. Foreman",
  "PatientName": "Lisa Turner",
  "PatientDateOfBirth": "1984-09-23",
  "Diagnosis": "Severe sleep apnea",
  "Prescription": {
    "device": "CPAP",
    "MaskType": 1,
    "HeatedHumidifier": true,
    "Ahi": 28,
    "Device": 1
  }
}
```

---

## Code Coverage
![Line Coverage](coverage/badge_linecoverage.svg)
![Branch Coverage](coverage/badge_branchcoverage.svg)

We track test coverage using [Coverlet](https://github.com/coverlet-coverage/coverlet) and [ReportGenerator](https://github.com/danielpalme/ReportGenerator).

### Generate Coverage Report

From the repository root, run:

```bash
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage" -reporttypes:Html
```

After running the above, open:


[Coverage Report (coverage/index.html)](coverage/index.html)

---

## 🔮 Future Improvements

- Add additional DME types (e.g., nebulizers, hospital beds.)
- Improve OpenAI error handling (e.g., retry on schema mismatch.)
- Add richer observability (OpenTelemetry, etc.)
- Build a web front-end or API wrapper instead of console I/O.
- Allow configuration of multiple LLM providers (OpenAI, Azure OpenAI.)
- Finish XML comments.
