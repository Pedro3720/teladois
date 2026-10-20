# Graph Report - Aplicativo celular  (2026-10-20)

## Corpus Check
- 121 files · ~48,343 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 8 file(s) not represented in the graph (top: (none) 3, .h264 2, .css 1)

## Summary
- 1251 nodes · 3531 edges · 52 communities (36 shown, 16 thin omitted)
- Extraction: 81% EXTRACTED · 19% INFERRED · 0% AMBIGUOUS · INFERRED: 681 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `f7266c4f`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- PC Link Session Core
- LinkFrame
- DimState
- TelaDois.Core.Link
- Dictionary
- VideoLayerView
- Open-Meteo Weather
- H264NALUnit
- Design Tokens & Icon
- Clock Formatting
- Encerrar Session Routine
- UsbLink
- Prototype Package Config
- Fact
- .NET Project Setup
- FakeReadStream
- Prototype Rest Mode JS
- Install & Tunnel Decisions
- project.yml (XcodeGen project definition)
- RestScreenView
- Clock Page & Battery Ring
- Etapa 4: USB tunnel (done, tested on iPhone)
- TrayContext
- TelaDois app (iPhone second screen + standby)
- Prototype Static Server
- ios-build workflow (unsigned Release .ipa)
- ios-test workflow (swift test + XCUITest + screenshots)
- BatteryMonitor
- Swift Package Manifest
- IReadOnlyList
- LinkSessionState
- CancellationToken
- TelaDoisCore
- RestScreenView
- VirtualDisplay
- Foundation
- RestScreenUITests
- LinkProtocolTests
- MonitorMode
- LinkProtocol
- LinkSession.cs
- TelaDois.Core.Display
- TelaDois.Core.Video
- TelaDois.Core.Tests.Fakes
- DriverPipe
- LinkMessageType
- FakePC
- XCTestCase

## God Nodes (most connected - your core abstractions)
1. `MonitorMode` - 41 edges
2. `FakeVirtualMonitor` - 38 edges
3. `VideoConfig` - 37 edges
4. `VideoFeedFactory` - 37 edges
5. `LinkSessionTests` - 35 edges
6. `IVideoFeed` - 34 edges
7. `FakeProcessRunner` - 34 edges
8. `FakeUsbmuxd` - 34 edges
9. `FakeProcess` - 33 edges
10. `VirtualDisplay` - 32 edges

## Surprising Connections (you probably didn't know these)
- `3.2 iPhone (Swift)` --references--> `H264Parser`  [INFERRED]
  docs/superpowers/specs/2026-10-19-etapa-5a-video-pelo-cabo-design.md → ios/Packages/TelaDoisCore/Sources/TelaDoisCore/H264Parser.swift
- `5. Testes` --references--> `H264Parser`  [INFERRED]
  docs/superpowers/specs/2026-10-19-etapa-5a-video-pelo-cabo-design.md → ios/Packages/TelaDoisCore/Sources/TelaDoisCore/H264Parser.swift
- `3.2 iPhone (Swift)` --references--> `LinkProtocol`  [INFERRED]
  docs/superpowers/specs/2026-10-19-etapa-5b-monitor-virtual-design.md → ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkProtocol.swift
- `3.2 iPhone (Swift)` --references--> `LinkSessionState`  [INFERRED]
  docs/superpowers/specs/2026-10-19-etapa-5b-monitor-virtual-design.md → ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkSessionState.swift
- `5. Testes` --references--> `LinkSessionState`  [INFERRED]
  docs/superpowers/specs/2026-10-19-etapa-5b-monitor-virtual-design.md → ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkSessionState.swift

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **GitHub Actions CI workflows gating the TelaDois branches** — _github_workflows_ios_test, _github_workflows_ios_build, _github_workflows_windows_test, _github_workflows_prototype_test, _github_workflows_pages [INFERRED 0.85]
- **PC <-> iPhone USB link: usbmuxd transport, protocol and endpoints** — docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_usbmuxd_transport, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_protocol, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_usblink, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_devicewatcher, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_streamsender, claude_rule_protocol_parity [INFERRED 0.85]
- **Unsigned .ipa delivery: CI build, inspection, xtool/Sideloadly install** — _github_workflows_ios_build, _github_workflows_ios_build_ipa_artifact, claude_rule_ipa_inspection, claude_decision_unsigned_ipa, docs_instalacao_xtool_method, docs_instalacao_sideloadly_method [INFERRED 0.85]

## Communities (52 total, 16 thin omitted)

### Community 0 - "PC Link Session Core"
Cohesion: 0.06
Nodes (38): LinkFrame, MessageType, LinkFrameDecoder, LinkFrameReader, LinkMessages, VideoConfig, LinkSessionOptions, Default (+30 more)

### Community 1 - "LinkFrame"
Cohesion: 0.15
Nodes (12): LinkFrame, .messageType, LinkMessageType, bye, config, hello, keyframeRequest, ping (+4 more)

### Community 2 - "DimState"
Cohesion: 0.28
Nodes (5): DimState, TouchResult, passThrough, wokeUp, DimStateTests

### Community 3 - "TelaDois.Core.Link"
Cohesion: 0.17
Nodes (4): TelaDois.Core, TelaDois.Core.Link, TelaDois.Core.Tests, TelaDois.Core.Usbmux

### Community 4 - "Dictionary"
Cohesion: 0.05
Nodes (30): HelloInfo, TunnelStatusText, Plist, UsbmuxClient, DefaultEndpoint, UsbmuxException, Code, DeviceAttached (+22 more)

### Community 6 - "Open-Meteo Weather"
Cohesion: 0.10
Nodes (8): CodingKeys, temperature, Current, MissingTemperature, OpenMeteo, Response, OpenMeteoTests, WeatherProvider

### Community 7 - "H264NALUnit"
Cohesion: 0.15
Nodes (6): H264NALType, H264NALUnit, .isVCL, H264Parser, H264ParserTests, VideoRenderer

### Community 8 - "Design Tokens & Icon"
Cohesion: 0.13
Nodes (19): Etapa 3: Spotify (deferred), Design tokens (Etapa 1), Behavior tokens (60 s dim with 1.2 s fade, wake tap does not press buttons), Color tokens (black, clock #7FA9F2, ink, muted, battery, track, bars), Landscape layout (clock left, date/temp/battery right, mini player), Typography tokens (Sofia Sans Extra Condensed stretched 1.5x, rounded system font), BatteryService (UIDevice), DimController (60 s inactivity, black screen, brightness 0) (+11 more)

### Community 9 - "Clock Formatting"
Cohesion: 0.21
Nodes (3): ClockFormatter, ClockText, ClockFormatterTests

### Community 10 - "Encerrar Session Routine"
Cohesion: 0.13
Nodes (19): /encerrar skill (end-of-session routine), Step 5: commit and push (never force, no merge to main), Step 3: handoff between computers, Step 4: pre-commit secret/security check, Step 6: summary for the user, Step 1: update CLAUDE.md, Step 2: verify Claude Code environment for real, pages workflow (publish prototype to GitHub Pages) (+11 more)

### Community 12 - "Prototype Package Config"
Cohesion: 0.12
Nodes (11): devDependencies, @playwright/test, name, private, scripts, serve, test, type (+3 more)

### Community 13 - "Fact"
Cohesion: 0.05
Nodes (38): IVirtualMonitor, DisplayOutput, EncoderProbe, ProbeTimeout, FfmpegCapture, Config, Dropped, ProcessStopper (+30 more)

### Community 14 - ".NET Project Setup"
Cohesion: 0.14
Nodes (10): net10.0-windows, coverlet.collector (6.0.4), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), net10.0, Microsoft.NET.Sdk, Microsoft.NET.Sdk (+2 more)

### Community 15 - "FakeReadStream"
Cohesion: 0.06
Nodes (16): BlockingStream, IsDisposed, ReadStarted, FailingStream, FakeReadStream, CanRead, CanSeek, CanWrite (+8 more)

### Community 16 - "Prototype Rest Mode JS"
Cohesion: 0.23
Nodes (12): armDimTimer(), changeTrack(), fetchTemp(), keepScreenOn(), renderClock(), renderMusic(), showTemp(), start() (+4 more)

### Community 17 - "Install & Tunnel Decisions"
Cohesion: 0.25
Nodes (9): usbmuxd USB tunnel (127.0.0.1:7100), Install on iPhone via xtool in WSL (bash -lc, USBMUXD_SOCKET_ADDRESS), INSTALACAO.md (install TelaDois on iPhone without paid account), iTunes/iCloud web installers (not Microsoft Store) for Apple Mobile Device Support, iPhone Developer Mode and trusting the Apple ID, Sideloadly install method (alternative), WSL mirrored networking so Ubuntu reaches Windows usbmuxd 127.0.0.1:27015, xtool install method (primary) (+1 more)

### Community 18 - "project.yml (XcodeGen project definition)"
Cohesion: 0.16
Nodes (9): Etapa 2: native Modo Descanso (done), Open-Meteo weather API, WeatherService (Open-Meteo + CoreLocation), project.yml (XcodeGen project definition), TelaDoisCore local package dependency, Location when-in-use permission string (for weather), Swift 5 mode with SWIFT_STRICT_CONCURRENCY complete, TelaDois application target (+1 more)

### Community 19 - "RestScreenView"
Cohesion: 0.14
Nodes (4): AppSettings, RestScreenView, TelaDoisApp, .body

### Community 20 - "Clock Page & Battery Ring"
Cohesion: 0.11
Nodes (7): BatteryRingView, .body, .percent, ClockPageView, .body, Theme, SwiftUI

### Community 21 - "Etapa 4: USB tunnel (done, tested on iPhone)"
Cohesion: 0.24
Nodes (9): AppState (rest <-> secondScreen state machine), PC <-> iPhone architecture over USB (usbmuxd), CaptureEncoder (FFmpeg ddagrab + hardware H.264, libx264 fallback), DeviceWatcher (PC usbmuxd client, attach/detach), PC<->iPhone protocol (HELLO, CONFIG, VIDEO, KEYFRAME_REQ, PING/PONG, BYE), StreamPlayer (H.264 decode via VideoToolbox), StreamSender (opens tunnel, sends messages, measures latency), UsbLink (iPhone listener on port 7100) (+1 more)

### Community 22 - "TrayContext"
Cohesion: 0.05
Nodes (13): TelaDois.Tray, KeyboardLayout, CreateDXGIFactory1(), OutputDesc, FfmpegLocator, FileLog, Hotkey, Registered (+5 more)

### Community 23 - "TelaDois app (iPhone second screen + standby)"
Cohesion: 0.18
Nodes (9): Etapa 4: USB tunnel (done, tested on iPhone), Etapa 5: Segunda Tela (next: virtual monitor + FFmpeg + video), Modo Descanso (StandBy-style rest mode), Segunda Tela (PC second monitor over USB), TelaDois app (iPhone second screen + standby), Virtual Display Driver (pipe MTTVirtualDisplayPipe), DisplayManager (Virtual Display Driver control, cleanup of leftover monitors), Error handling table (cable removed, app left, encoder missing, decode error) (+1 more)

### Community 25 - "ios-build workflow (unsigned Release .ipa)"
Cohesion: 0.25
Nodes (5): ios-build workflow (unsigned Release .ipa), App icon check step (Assets.car, CFBundleIconName=AppIcon), TelaDois-ipa artifact (TelaDois.ipa + BUILD_INFO.txt), App icon (icone-1024.png, Canva), install-xcodegen.sh script

### Community 26 - "ios-test workflow (swift test + XCUITest + screenshots)"
Cohesion: 0.17
Nodes (4): ios-test workflow (swift test + XCUITest + screenshots), screenshots artifact (renamed simulator prints), UI-test launch arguments (-TDFixedDate, -TDDimSeconds, ...), pick-simulator.sh script

### Community 31 - "IReadOnlyList"
Cohesion: 0.10
Nodes (14): EncoderProfile, EncoderProfiles, Candidates, FfmpegArguments, IProcessRunner, IRunningProcess, Output, RecentErrors (+6 more)

### Community 32 - "LinkSessionState"
Cohesion: 0.22
Nodes (4): Action, send, LinkSessionState, LinkSessionStateTests

### Community 33 - "CancellationToken"
Cohesion: 0.07
Nodes (18): 1. Objetivo, 2. Ambiente de referência, 3.1 PC (C#, `windows/src/TelaDois.Core`), 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, 4. Tratamento de erros, 5. Testes, 6. Riscos (+10 more)

### Community 34 - "TelaDoisCore"
Cohesion: 0.16
Nodes (7): AVFoundation, CoreLocation, Network, Observation, TelaDoisCore, UIKit, XCTest

### Community 35 - "RestScreenView"
Cohesion: 0.28
Nodes (3): .body, ScreenBrightness, .screen

### Community 36 - "VirtualDisplay"
Cohesion: 0.06
Nodes (32): 1. Objetivo, 2. O driver (lido no código-fonte da release 25.7.23), 3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`), 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, 4. Tratamento de erros (Parte 3, aprovada), 5. Testes, 6. Riscos (+24 more)

### Community 40 - "MonitorMode"
Cohesion: 0.14
Nodes (10): MonitorMode, IsConnected, WantsVideo, ToggleResult, NoPhone, TurnedOff, TurnedOn, IVideoSwitch (+2 more)

### Community 41 - "LinkProtocol"
Cohesion: 0.16
Nodes (8): 3.2 iPhone (Swift), bigEndianBytes(), LinkDecodeError, payloadTooLarge, LinkHello, LinkProtocol, LinkVideoConfig, readBigEndian()

### Community 42 - "LinkSession.cs"
Cohesion: 0.33
Nodes (5): LinkEndReason, Bye, Cancelled, Closed, Timeout

### Community 43 - "TelaDois.Core.Display"
Cohesion: 0.14
Nodes (4): TelaDois.Core.Tests.Display, TelaDois.Core.Display, KeyboardLayoutLocalTests, VirtualDisplayLocalTests

### Community 46 - "DriverPipe"
Cohesion: 0.41
Nodes (3): DriverPipe, Timeout, DriverPipeTests

### Community 47 - "LinkMessageType"
Cohesion: 0.22
Nodes (9): LinkMessageType, Bye, Config, Hello, KeyframeRequest, Ping, Pong, Video (+1 more)

## Knowledge Gaps
- **135 isolated node(s):** `PackageDescription`, `passThrough`, `wokeUp`, `.isVCL`, `hello` (+130 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 273 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **16 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `LinkSessionState` connect `LinkSessionState` to `LinkFrame`, `UsbLink`, `VirtualDisplay`, `Foundation`?**
  _High betweenness centrality (0.187) - this node is a cross-community bridge._
- **Are the 27 inferred relationships involving `MonitorMode` (e.g. with `3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`)` and `4. Tratamento de erros (Parte 3, aprovada)`) actually correct?**
  _`MonitorMode` has 27 INFERRED edges - model-reasoned connections that need verification._
- **What connects `PackageDescription`, `passThrough`, `wokeUp` to the rest of the system?**
  _135 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `PC Link Session Core` be split into smaller, more focused modules?**
  _Cohesion score 0.06420906888196608 - nodes in this community are weakly interconnected._
- **Why does `5. Testes` connect `VirtualDisplay` to `LinkSessionState`, `CancellationToken`, `MonitorMode`?**
  _High betweenness centrality (0.187) - this node is a cross-community bridge._
- **Are the 18 inferred relationships involving `FakeVirtualMonitor` (e.g. with `.TurningOnAgainWhileTheOldFeedDetachesKeepsTheSameVirtualMonitorOn()` and `.AttachFailureFallsBackToMirror()`) actually correct?**
  _`FakeVirtualMonitor` has 18 INFERRED edges - model-reasoned connections that need verification._
- **Should `Dictionary` be split into smaller, more focused modules?**
  _Cohesion score 0.05280875236692615 - nodes in this community are weakly interconnected._