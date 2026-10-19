# Graph Report - Aplicativo celular  (2026-10-19)

## Corpus Check
- Corpus is ~19,746 words - fits in a single context window. You may not need a graph.

## Summary
- 633 nodes · 1285 edges · 31 communities (23 shown, 8 thin omitted)
- Extraction: 88% EXTRACTED · 12% INFERRED · 0% AMBIGUOUS · INFERRED: 159 edges (avg confidence: 0.85)
- Token cost: 144,564 input · 0 output

## Community Hubs (Navigation)
- PC Link Session Core
- Swift Link Protocol
- Tray App Shell
- C# Namespaces & Usings
- Usbmux Plist Codec
- Second Screen Architecture
- Open-Meteo Weather
- Fake PC Test Harness
- Design Tokens & Icon
- Clock Formatting
- Encerrar Session Routine
- iPhone UsbLink Listener
- Prototype Package Config
- App Settings & Root View
- .NET Project Setup
- Dim State Machine
- Prototype Rest Mode JS
- Install & Tunnel Decisions
- Build & Distribution Decisions
- iOS CI Workflows
- Clock Page & Battery Ring
- Roadmap & Core Decisions
- Screen Brightness Control
- SwiftUI Theme Views
- Prototype Static Server
- Battery Monitor
- Link End Reasons
- Swift Package Manifest

## God Nodes (most connected - your core abstractions)
1. `FakeUsbmuxd` - 30 edges
2. `LinkFrame` - 19 edges
3. `UsbLink` - 19 edges
4. `LinkFrame` - 16 edges
5. `CLAUDE.md (TelaDois project guide)` - 16 edges
6. `DimState` - 15 edges
7. `LinkSessionState` - 15 edges
8. `UsbTunnelService` - 15 edges
9. `RestScreenView` - 14 edges
10. `TrayContext` - 14 edges

## Surprising Connections (you probably didn't know these)
- `xtool install method (primary)` --semantically_similar_to--> `Install on iPhone via xtool in WSL (bash -lc, USBMUXD_SOCKET_ADDRESS)`  [INFERRED] [semantically similar]
  docs/INSTALACAO.md → CLAUDE.md
- `Clock page (clock digits, date, temp, battery ring)` --semantically_similar_to--> `RestScreen (Clock and Music pages)`  [INFERRED] [semantically similar]
  prototype/index.html → docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md
- `Dim overlay (black screen after inactivity)` --semantically_similar_to--> `DimController (60 s inactivity, black screen, brightness 0)`  [INFERRED] [semantically similar]
  prototype/index.html → docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md
- `Rotate hint overlay (portrait message)` --semantically_similar_to--> `Landscape-only, full-screen, hidden status bar, iPhone-only settings`  [INFERRED] [semantically similar]
  prototype/index.html → ios/project.yml
- `Step 2: verify Claude Code environment for real` --references--> `Install on iPhone via xtool in WSL (bash -lc, USBMUXD_SOCKET_ADDRESS)`  [INFERRED]
  .claude/skills/encerrar/SKILL.md → CLAUDE.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **GitHub Actions CI workflows gating the TelaDois branches** — _github_workflows_ios_test, _github_workflows_ios_build, _github_workflows_windows_test, _github_workflows_prototype_test, _github_workflows_pages [INFERRED 0.85]
- **PC <-> iPhone USB link: usbmuxd transport, protocol and endpoints** — docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_usbmuxd_transport, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_protocol, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_usblink, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_devicewatcher, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_streamsender, claude_rule_protocol_parity [INFERRED 0.85]
- **Unsigned .ipa delivery: CI build, inspection, xtool/Sideloadly install** — _github_workflows_ios_build, _github_workflows_ios_build_ipa_artifact, claude_rule_ipa_inspection, claude_decision_unsigned_ipa, docs_instalacao_xtool_method, docs_instalacao_sideloadly_method [INFERRED 0.85]

## Communities (31 total, 8 thin omitted)

### Community 0 - "PC Link Session Core"
Cohesion: 0.07
Nodes (21): LinkFrame, MessageType, LinkFrameDecoder, LinkFrameReader, HelloInfo, LinkMessages, LinkSession, LinkSessionOptions (+13 more)

### Community 1 - "Swift Link Protocol"
Cohesion: 0.07
Nodes (22): bigEndianBytes(), LinkDecodeError, payloadTooLarge, LinkFrame, .messageType, LinkFrameDecoder, LinkHello, LinkMessageType (+14 more)

### Community 2 - "Tray App Shell"
Cohesion: 0.08
Nodes (14): TelaDois.Core, TelaDois.Tray, TunnelStatusText, TunnelState, Connected, ServiceMissing, WaitingForApp, WaitingForIPhone (+6 more)

### Community 3 - "C# Namespaces & Usings"
Cohesion: 0.08
Nodes (15): TelaDois.Core.Link, TelaDois.Core.Tests.Fakes, TelaDois.Core.Tests, TelaDois.Core.Usbmux, LinkMessageType, Bye, Config, Hello (+7 more)

### Community 4 - "Usbmux Plist Codec"
Cohesion: 0.11
Nodes (9): Plist, DeviceAttached, DeviceDetached, ListenStarted, UsbmuxEvent, UsbmuxMessages, UsbmuxPacket, PlistTests (+1 more)

### Community 5 - "Second Screen Architecture"
Cohesion: 0.09
Nodes (17): CoreLocation, Darwin, AppState (rest <-> secondScreen state machine), PC <-> iPhone architecture over USB (usbmuxd), CaptureEncoder (FFmpeg ddagrab + hardware H.264, libx264 fallback), DeviceWatcher (PC usbmuxd client, attach/detach), PC<->iPhone protocol (HELLO, CONFIG, VIDEO, KEYFRAME_REQ, PING/PONG, BYE), StreamPlayer (H.264 decode via VideoToolbox) (+9 more)

### Community 6 - "Open-Meteo Weather"
Cohesion: 0.09
Nodes (8): CodingKeys, temperature, Current, MissingTemperature, OpenMeteo, Response, OpenMeteoTests, WeatherProvider

### Community 7 - "Fake PC Test Harness"
Cohesion: 0.14
Nodes (3): close, FakePC, RestScreenUITests

### Community 8 - "Design Tokens & Icon"
Cohesion: 0.12
Nodes (21): App icon check step (Assets.car, CFBundleIconName=AppIcon), Etapa 3: Spotify (deferred), Design tokens (Etapa 1), Behavior tokens (60 s dim with 1.2 s fade, wake tap does not press buttons), Color tokens (black, clock #7FA9F2, ink, muted, battery, track, bars), App icon (icone-1024.png, Canva), Landscape layout (clock left, date/temp/battery right, mini player), Typography tokens (Sofia Sans Extra Condensed stretched 1.5x, rounded system font) (+13 more)

### Community 9 - "Clock Formatting"
Cohesion: 0.15
Nodes (4): ClockFormatter, ClockText, ClockFormatterTests, XCTestCase

### Community 10 - "Encerrar Session Routine"
Cohesion: 0.15
Nodes (18): /encerrar skill (end-of-session routine), Step 3: handoff between computers, Step 4: pre-commit secret/security check, Step 6: summary for the user, Step 1: update CLAUDE.md, Step 2: verify Claude Code environment for real, pages workflow (publish prototype to GitHub Pages), prototype-test workflow (Playwright WebKit) (+10 more)

### Community 12 - "Prototype Package Config"
Cohesion: 0.12
Nodes (11): devDependencies, @playwright/test, name, private, scripts, serve, test, type (+3 more)

### Community 13 - "App Settings & Root View"
Cohesion: 0.14
Nodes (4): AppSettings, RestScreenView, TelaDoisApp, .body

### Community 14 - ".NET Project Setup"
Cohesion: 0.14
Nodes (10): net10.0-windows, coverlet.collector (6.0.4), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), net10.0, Microsoft.NET.Sdk, Microsoft.NET.Sdk (+2 more)

### Community 15 - "Dim State Machine"
Cohesion: 0.28
Nodes (5): DimState, TouchResult, passThrough, wokeUp, DimStateTests

### Community 16 - "Prototype Rest Mode JS"
Cohesion: 0.23
Nodes (12): armDimTimer(), changeTrack(), fetchTemp(), keepScreenOn(), renderClock(), renderMusic(), showTemp(), start() (+4 more)

### Community 17 - "Install & Tunnel Decisions"
Cohesion: 0.19
Nodes (10): TelaDois-ipa artifact (TelaDois.ipa + BUILD_INFO.txt), usbmuxd USB tunnel (127.0.0.1:7100), Install on iPhone via xtool in WSL (bash -lc, USBMUXD_SOCKET_ADDRESS), INSTALACAO.md (install TelaDois on iPhone without paid account), iTunes/iCloud web installers (not Microsoft Store) for Apple Mobile Device Support, iPhone Developer Mode and trusting the Apple ID, Sideloadly install method (alternative), WSL mirrored networking so Ubuntu reaches Windows usbmuxd 127.0.0.1:27015 (+2 more)

### Community 18 - "Build & Distribution Decisions"
Cohesion: 0.18
Nodes (9): Etapa 2: native Modo Descanso (done), Open-Meteo weather API, WeatherService (Open-Meteo + CoreLocation), project.yml (XcodeGen project definition), TelaDoisCore local package dependency, Location when-in-use permission string (for weather), Swift 5 mode with SWIFT_STRICT_CONCURRENCY complete, TelaDois application target (+1 more)

### Community 19 - "iOS CI Workflows"
Cohesion: 0.18
Nodes (7): Step 5: commit and push (never force, no merge to main), ios-build workflow (unsigned Release .ipa), ios-test workflow (swift test + XCUITest + screenshots), screenshots artifact (renamed simulator prints), UI-test launch arguments (-TDFixedDate, -TDDimSeconds, ...), install-xcodegen.sh script, pick-simulator.sh script

### Community 20 - "Clock Page & Battery Ring"
Cohesion: 0.17
Nodes (5): BatteryRingView, .body, .percent, ClockPageView, .body

### Community 21 - "Roadmap & Core Decisions"
Cohesion: 0.18
Nodes (9): Etapa 4: USB tunnel (done, tested on iPhone), Etapa 5: Segunda Tela (next: virtual monitor + FFmpeg + video), Modo Descanso (StandBy-style rest mode), Segunda Tela (PC second monitor over USB), TelaDois app (iPhone second screen + standby), Virtual Display Driver (pipe MTTVirtualDisplayPipe), DisplayManager (Virtual Display Driver control, cleanup of leftover monitors), Error handling table (cable removed, app left, encoder missing, decode error) (+1 more)

### Community 22 - "Screen Brightness Control"
Cohesion: 0.24
Nodes (3): .body, ScreenBrightness, .screen

### Community 27 - "Link End Reasons"
Cohesion: 0.40
Nodes (5): LinkEndReason, Bye, Cancelled, Closed, Timeout

## Knowledge Gaps
- **61 isolated node(s):** `PackageDescription`, `passThrough`, `wokeUp`, `hello`, `config` (+56 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 170 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **8 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Etapa 4: USB tunnel (done, tested on iPhone)` connect `Roadmap & Core Decisions` to `Install & Tunnel Decisions`, `Tray App Shell`, `Encerrar Session Routine`, `Second Screen Architecture`?**
  _High betweenness centrality (0.355) - this node is a cross-community bridge._
- **Are the 7 inferred relationships involving `FakeUsbmuxd` (e.g. with `.ConnectIsRefusedWhenAppIsClosed()` and `.ConnectReturnsTunnelToThePhone()`) actually correct?**
  _`FakeUsbmuxd` has 7 INFERRED edges - model-reasoned connections that need verification._
- **What connects `PackageDescription`, `passThrough`, `wokeUp` to the rest of the system?**
  _61 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `PC Link Session Core` be split into smaller, more focused modules?**
  _Cohesion score 0.07004103967168263 - nodes in this community are weakly interconnected._
- **Why does `UsbLink` connect `iPhone UsbLink Listener` to `Swift Link Protocol`, `Second Screen Architecture`, `App Settings & Root View`, `Screen Brightness Control`?**
  _High betweenness centrality (0.154) - this node is a cross-community bridge._
- **Are the 4 inferred relationships involving `LinkFrame` (e.g. with `.testRejectsHelloWithWrongSize()` and `.testUnknownTypeHasNoMessageType()`) actually correct?**
  _`LinkFrame` has 4 INFERRED edges - model-reasoned connections that need verification._
- **Should `Swift Link Protocol` be split into smaller, more focused modules?**
  _Cohesion score 0.06666666666666667 - nodes in this community are weakly interconnected._