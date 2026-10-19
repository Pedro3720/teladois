# Graph Report - Aplicativo celular  (2026-10-19)

## Corpus Check
- 93 files · ~28,373 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 7 file(s) not represented in the graph (top: (none) 3, .h264 2, .css 1)

## Summary
- 869 nodes · 2012 edges · 39 communities (29 shown, 10 thin omitted)
- Extraction: 86% EXTRACTED · 14% INFERRED · 0% AMBIGUOUS · INFERRED: 282 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `1fbb54a8`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- PC Link Session Core
- Swift Link Protocol
- TrayContext
- C# Namespaces & Usings
- Usbmux Plist Codec
- PC<->iPhone protocol (HELLO, CONFIG, VIDEO, KEYFRAME_REQ, PING/PONG, BYE)
- Open-Meteo Weather
- Fake PC Test Harness
- Design Tokens & Icon
- Clock Formatting
- Encerrar Session Routine
- UsbLink
- Prototype Package Config
- Task
- .NET Project Setup
- FakeReadStream
- Prototype Rest Mode JS
- Install & Tunnel Decisions
- RestScreen (Clock and Music pages)
- ios-test workflow (swift test + XCUITest + screenshots)
- Clock Page & Battery Ring
- Roadmap & Core Decisions
- FakeUsbmuxd
- SwiftUI Theme Views
- Prototype Static Server
- UsbTunnelService
- H264NALUnit
- Swift Package Manifest
- IReadOnlyList
- Dictionary
- CancellationToken
- TelaDoisCore
- UsbLink.swift
- DisplayCatalog.cs
- Foundation
- XCTestCase

## God Nodes (most connected - your core abstractions)
1. `FakeUsbmuxd` - 30 edges
2. `LinkFrame` - 24 edges
3. `FfmpegCapture` - 24 edges
4. `FakeProcess` - 23 edges
5. `FakeProcessRunner` - 22 edges
6. `LinkFrame` - 21 edges
7. `UsbLink` - 20 edges
8. `TelaDois.Core.Video` - 19 edges
9. `FrameQueue` - 17 edges
10. `EncoderProbe` - 16 edges

## Surprising Connections (you probably didn't know these)
- `3.2 iPhone (Swift)` --references--> `H264Parser`  [INFERRED]
  docs/superpowers/specs/2026-10-19-etapa-5a-video-pelo-cabo-design.md → ios/Packages/TelaDoisCore/Sources/TelaDoisCore/H264Parser.swift
- `5. Testes` --references--> `H264Parser`  [INFERRED]
  docs/superpowers/specs/2026-10-19-etapa-5a-video-pelo-cabo-design.md → ios/Packages/TelaDoisCore/Sources/TelaDoisCore/H264Parser.swift
- `3.2 iPhone (Swift)` --references--> `UsbLink`  [INFERRED]
  docs/superpowers/specs/2026-10-19-etapa-5a-video-pelo-cabo-design.md → ios/TelaDois/UsbLink.swift
- `3.1 PC (C#, `windows/src/TelaDois.Core`)` --references--> `LinkSession`  [INFERRED]
  docs/superpowers/specs/2026-10-19-etapa-5a-video-pelo-cabo-design.md → windows/src/TelaDois.Core/Link/LinkSession.cs
- `5. Testes` --references--> `LinkSession`  [INFERRED]
  docs/superpowers/specs/2026-10-19-etapa-5a-video-pelo-cabo-design.md → windows/src/TelaDois.Core/Link/LinkSession.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **GitHub Actions CI workflows gating the TelaDois branches** — _github_workflows_ios_test, _github_workflows_ios_build, _github_workflows_windows_test, _github_workflows_prototype_test, _github_workflows_pages [INFERRED 0.85]
- **PC <-> iPhone USB link: usbmuxd transport, protocol and endpoints** — docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_usbmuxd_transport, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_protocol, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_usblink, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_devicewatcher, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_streamsender, claude_rule_protocol_parity [INFERRED 0.85]
- **Unsigned .ipa delivery: CI build, inspection, xtool/Sideloadly install** — _github_workflows_ios_build, _github_workflows_ios_build_ipa_artifact, claude_rule_ipa_inspection, claude_decision_unsigned_ipa, docs_instalacao_xtool_method, docs_instalacao_sideloadly_method [INFERRED 0.85]

## Communities (39 total, 10 thin omitted)

### Community 0 - "PC Link Session Core"
Cohesion: 0.07
Nodes (20): LinkFrame, MessageType, LinkFrameDecoder, LinkFrameReader, HelloInfo, LinkMessages, VideoConfig, LinkEndReason (+12 more)

### Community 1 - "Swift Link Protocol"
Cohesion: 0.06
Nodes (25): 3.2 iPhone (Swift), bigEndianBytes(), LinkDecodeError, payloadTooLarge, LinkFrame, .messageType, LinkFrameDecoder, LinkHello (+17 more)

### Community 2 - "TrayContext"
Cohesion: 0.12
Nodes (6): TelaDois.Core, TelaDois.Tray, TunnelStatusText, FileLog, Program, TrayContext

### Community 3 - "C# Namespaces & Usings"
Cohesion: 0.05
Nodes (19): TelaDois.Core.Link, TelaDois.Core.Tests.Fakes, TelaDois.Core.Tests, TelaDois.Core.Tests.Video, TelaDois.Core.Video, TelaDois.Core.Usbmux, LinkMessageType, Bye (+11 more)

### Community 4 - "Usbmux Plist Codec"
Cohesion: 0.20
Nodes (6): DeviceAttached, DeviceDetached, ListenStarted, UsbmuxEvent, UsbmuxMessages, UsbmuxTests

### Community 5 - "PC<->iPhone protocol (HELLO, CONFIG, VIDEO, KEYFRAME_REQ, PING/PONG, BYE)"
Cohesion: 0.24
Nodes (9): AppState (rest <-> secondScreen state machine), PC <-> iPhone architecture over USB (usbmuxd), CaptureEncoder (FFmpeg ddagrab + hardware H.264, libx264 fallback), DeviceWatcher (PC usbmuxd client, attach/detach), PC<->iPhone protocol (HELLO, CONFIG, VIDEO, KEYFRAME_REQ, PING/PONG, BYE), StreamPlayer (H.264 decode via VideoToolbox), StreamSender (opens tunnel, sends messages, measures latency), UsbLink (iPhone listener on port 7100) (+1 more)

### Community 6 - "Open-Meteo Weather"
Cohesion: 0.09
Nodes (8): CodingKeys, temperature, Current, MissingTemperature, OpenMeteo, Response, OpenMeteoTests, WeatherProvider

### Community 7 - "Fake PC Test Harness"
Cohesion: 0.14
Nodes (3): close, FakePC, RestScreenUITests

### Community 8 - "Design Tokens & Icon"
Cohesion: 0.17
Nodes (15): Design tokens (Etapa 1), Behavior tokens (60 s dim with 1.2 s fade, wake tap does not press buttons), Color tokens (black, clock #7FA9F2, ink, muted, battery, track, bars), Landscape layout (clock left, date/temp/battery right, mini player), Typography tokens (Sofia Sans Extra Condensed stretched 1.5x, rounded system font), DimController (60 s inactivity, black screen, brightness 0), Landscape-only, full-screen, hidden status bar, iPhone-only settings, index.html (Modo Descanso web prototype) (+7 more)

### Community 10 - "Encerrar Session Routine"
Cohesion: 0.13
Nodes (19): /encerrar skill (end-of-session routine), Step 5: commit and push (never force, no merge to main), Step 3: handoff between computers, Step 4: pre-commit secret/security check, Step 6: summary for the user, Step 1: update CLAUDE.md, Step 2: verify Claude Code environment for real, pages workflow (publish prototype to GitHub Pages) (+11 more)

### Community 11 - "UsbLink"
Cohesion: 0.05
Nodes (14): DimState, TouchResult, passThrough, wokeUp, DimStateTests, AppSettings, BatteryMonitor, RestScreenView (+6 more)

### Community 12 - "Prototype Package Config"
Cohesion: 0.12
Nodes (11): devDependencies, @playwright/test, name, private, scripts, serve, test, type (+3 more)

### Community 13 - "Task"
Cohesion: 0.05
Nodes (40): 1. Objetivo, 2. Ambiente de referência, 3.1 PC (C#, `windows/src/TelaDois.Core`), 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, 4. Tratamento de erros, 5. Testes, 6. Riscos (+32 more)

### Community 14 - ".NET Project Setup"
Cohesion: 0.14
Nodes (10): net10.0-windows, coverlet.collector (6.0.4), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), net10.0, Microsoft.NET.Sdk, Microsoft.NET.Sdk (+2 more)

### Community 15 - "FakeReadStream"
Cohesion: 0.08
Nodes (13): BlockingStream, IsDisposed, ReadStarted, FakeReadStream, CanRead, CanSeek, CanWrite, Length (+5 more)

### Community 16 - "Prototype Rest Mode JS"
Cohesion: 0.23
Nodes (12): armDimTimer(), changeTrack(), fetchTemp(), keepScreenOn(), renderClock(), renderMusic(), showTemp(), start() (+4 more)

### Community 17 - "Install & Tunnel Decisions"
Cohesion: 0.23
Nodes (9): usbmuxd USB tunnel (127.0.0.1:7100), Install on iPhone via xtool in WSL (bash -lc, USBMUXD_SOCKET_ADDRESS), INSTALACAO.md (install TelaDois on iPhone without paid account), iTunes/iCloud web installers (not Microsoft Store) for Apple Mobile Device Support, iPhone Developer Mode and trusting the Apple ID, Sideloadly install method (alternative), WSL mirrored networking so Ubuntu reaches Windows usbmuxd 127.0.0.1:27015, xtool install method (primary) (+1 more)

### Community 18 - "RestScreen (Clock and Music pages)"
Cohesion: 0.18
Nodes (8): Etapa 2: native Modo Descanso (done), Etapa 3: Spotify (deferred), Open-Meteo weather API, BatteryService (UIDevice), RestScreen (Clock and Music pages), SpotifyService (App Remote SDK), WeatherService (Open-Meteo + CoreLocation), Location when-in-use permission string (for weather)

### Community 19 - "ios-test workflow (swift test + XCUITest + screenshots)"
Cohesion: 0.12
Nodes (14): ios-build workflow (unsigned Release .ipa), App icon check step (Assets.car, CFBundleIconName=AppIcon), TelaDois-ipa artifact (TelaDois.ipa + BUILD_INFO.txt), ios-test workflow (swift test + XCUITest + screenshots), screenshots artifact (renamed simulator prints), UI-test launch arguments (-TDFixedDate, -TDDimSeconds, ...), App icon (icone-1024.png, Canva), install-xcodegen.sh script (+6 more)

### Community 20 - "Clock Page & Battery Ring"
Cohesion: 0.17
Nodes (5): BatteryRingView, .body, .percent, ClockPageView, .body

### Community 21 - "Roadmap & Core Decisions"
Cohesion: 0.18
Nodes (9): Etapa 4: USB tunnel (done, tested on iPhone), Etapa 5: Segunda Tela (next: virtual monitor + FFmpeg + video), Modo Descanso (StandBy-style rest mode), Segunda Tela (PC second monitor over USB), TelaDois app (iPhone second screen + standby), Virtual Display Driver (pipe MTTVirtualDisplayPipe), DisplayManager (Virtual Display Driver control, cleanup of leftover monitors), Error handling table (cable removed, app left, encoder missing, decode error) (+1 more)

### Community 22 - "FakeUsbmuxd"
Cohesion: 0.17
Nodes (6): FakeUsbmuxd, Endpoint, EventsAfterListen, Phone, Requests, UsbTunnelServiceTests

### Community 26 - "UsbTunnelService"
Cohesion: 0.16
Nodes (8): TunnelState, Connected, ServiceMissing, WaitingForApp, WaitingForIPhone, TunnelStatus, UsbTunnelService, Retry

### Community 27 - "H264NALUnit"
Cohesion: 0.22
Nodes (5): H264NALType, H264NALUnit, .isVCL, H264Parser, H264ParserTests

### Community 31 - "IReadOnlyList"
Cohesion: 0.26
Nodes (5): EncoderProfile, EncoderProfiles, Candidates, FfmpegArguments, FfmpegArgumentsTests

### Community 33 - "CancellationToken"
Cohesion: 0.31
Nodes (4): UsbmuxClient, DefaultEndpoint, UsbmuxException, Code

### Community 35 - "UsbLink.swift"
Cohesion: 0.29
Nodes (4): CoreLocation, Network, Observation, UIKit

### Community 36 - "DisplayCatalog.cs"
Cohesion: 0.29
Nodes (3): CreateDXGIFactory1(), DisplayOutput, OutputDesc

### Community 37 - "Foundation"
Cohesion: 0.29
Nodes (3): Darwin, Foundation, ClockFormatter

## Knowledge Gaps
- **84 isolated node(s):** `PackageDescription`, `passThrough`, `wokeUp`, `.isVCL`, `hello` (+79 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 204 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **10 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Etapa 4: USB tunnel (done, tested on iPhone)` connect `Roadmap & Core Decisions` to `Encerrar Session Routine`, `Install & Tunnel Decisions`, `UsbTunnelService`, `UsbLink.swift`?**
  _High betweenness centrality (0.194) - this node is a cross-community bridge._
- **Are the 7 inferred relationships involving `FakeUsbmuxd` (e.g. with `.ConnectIsRefusedWhenAppIsClosed()` and `.ConnectReturnsTunnelToThePhone()`) actually correct?**
  _`FakeUsbmuxd` has 7 INFERRED edges - model-reasoned connections that need verification._
- **What connects `PackageDescription`, `passThrough`, `wokeUp` to the rest of the system?**
  _84 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `PC Link Session Core` be split into smaller, more focused modules?**
  _Cohesion score 0.07316118935837246 - nodes in this community are weakly interconnected._
- **Why does `UsbLink` connect `UsbLink` to `Swift Link Protocol`, `UsbLink.swift`?**
  _High betweenness centrality (0.145) - this node is a cross-community bridge._
- **Are the 5 inferred relationships involving `LinkFrame` (e.g. with `.testRejectsHelloWithWrongSize()` and `.testRejectsMalformedConfigAndVideo()`) actually correct?**
  _`LinkFrame` has 5 INFERRED edges - model-reasoned connections that need verification._
- **Should `Swift Link Protocol` be split into smaller, more focused modules?**
  _Cohesion score 0.06329113924050633 - nodes in this community are weakly interconnected._