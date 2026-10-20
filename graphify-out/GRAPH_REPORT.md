# Graph Report - Aplicativo celular  (2026-10-20)

## Corpus Check
- 134 files · ~59,421 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 8 file(s) not represented in the graph (top: (none) 3, .h264 2, .css 1)

## Summary
- 1497 nodes · 4425 edges · 67 communities (52 shown, 15 thin omitted)
- Extraction: 80% EXTRACTED · 20% INFERRED · 0% AMBIGUOUS · INFERRED: 899 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `084f426c`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .RunAsync
- LinkMessageType
- DimState
- CancellationToken
- FakeUsbmuxd
- VideoLayerView
- Open-Meteo Weather
- VirtualMonitorFeed
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
- project.yml (XcodeGen project definition)
- RestScreenView
- Clock Page & Battery Ring
- Design spec: iPhone second screen + Modo Descanso
- Hotkey
- LinkStatsMeter
- Prototype Static Server
- EncoderProfile
- ios-test workflow (swift test + XCUITest + screenshots)
- BatteryMonitor
- Swift Package Manifest
- FakeProcessRunner
- LinkSessionState
- TimeSpan
- TelaDoisCore
- IReadOnlyList
- VirtualDisplay
- VideoConfig
- RestScreenUITests
- LinkFrame
- MonitorMode
- LinkProtocol
- LinkSessionOptions
- IVirtualMonitor
- TelaDois.Core.Video
- LinkProtocol.swift
- DriverPipe
- LinkMessageType
- Foundation
- XCTestCase
- EncoderProbe
- TrayContext
- IVideoFeed
- DisplayCatalog.cs
- .ReadAll
- LinkSession
- FakeVirtualMonitor
- Etapa 5c — o 3º monitor some quando desligado + tela preta no iPhone — Especificação de Design
- Win32DisplayApi
- TelaDois app (iPhone second screen + standby)
- TraySettings
- VideoConfig
- ScheduledTaskSwitch
- Fact
- WeatherProvider
- TunnelState

## God Nodes (most connected - your core abstractions)
1. `FakeVirtualMonitor` - 54 edges
2. `VirtualDisplay` - 51 edges
3. `FakeProcess` - 47 edges
4. `VideoFeedFactory` - 44 edges
5. `FakeProcessRunner` - 43 edges
6. `MonitorMode` - 41 edges
7. `VideoConfig` - 38 edges
8. `IVideoFeed` - 38 edges
9. `LinkSessionTests` - 37 edges
10. `TelaDois.Core.Video` - 36 edges

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

## Communities (67 total, 15 thin omitted)

### Community 0 - ".RunAsync"
Cohesion: 0.22
Nodes (3): LinkFrameReader, FakePhone, LinkSessionTests

### Community 1 - "LinkMessageType"
Cohesion: 0.17
Nodes (12): .messageType, LinkMessageType, bye, config, hello, keyframeRequest, ping, pong (+4 more)

### Community 2 - "DimState"
Cohesion: 0.28
Nodes (5): DimState, TouchResult, passThrough, wokeUp, DimStateTests

### Community 4 - "FakeUsbmuxd"
Cohesion: 0.06
Nodes (25): HelloInfo, TunnelStatusText, Plist, UsbmuxClient, DefaultEndpoint, UsbmuxException, Code, DeviceAttached (+17 more)

### Community 6 - "Open-Meteo Weather"
Cohesion: 0.13
Nodes (7): CodingKeys, temperature, Current, MissingTemperature, OpenMeteo, Response, OpenMeteoTests

### Community 7 - "VirtualMonitorFeed"
Cohesion: 0.19
Nodes (10): VirtualMonitorFeed, Config, Kind, Meters, FailingDisposeFeed, Config, VirtualMonitorFeedTests, WaitingFeed (+2 more)

### Community 8 - "Design Tokens & Icon"
Cohesion: 0.12
Nodes (19): App icon check step (Assets.car, CFBundleIconName=AppIcon), Etapa 3: Spotify (deferred), Design tokens (Etapa 1), Behavior tokens (60 s dim with 1.2 s fade, wake tap does not press buttons), Color tokens (black, clock #7FA9F2, ink, muted, battery, track, bars), App icon (icone-1024.png, Canva), Landscape layout (clock left, date/temp/battery right, mini player), Typography tokens (Sofia Sans Extra Condensed stretched 1.5x, rounded system font) (+11 more)

### Community 9 - "Clock Formatting"
Cohesion: 0.21
Nodes (3): ClockFormatter, ClockText, ClockFormatterTests

### Community 10 - "Encerrar Session Routine"
Cohesion: 0.15
Nodes (16): /encerrar skill (end-of-session routine), Step 3: handoff between computers, Step 4: pre-commit secret/security check, Step 6: summary for the user, screenshots artifact (renamed simulator prints), pages workflow (publish prototype to GitHub Pages), prototype-test workflow (Playwright WebKit), windows-test workflow (dotnet test on windows-2025) (+8 more)

### Community 12 - "Prototype Package Config"
Cohesion: 0.12
Nodes (11): devDependencies, @playwright/test, name, private, scripts, serve, test, type (+3 more)

### Community 13 - "Task"
Cohesion: 0.19
Nodes (4): DisplayOutput, VideoFeedFactory, OutputWait, VideoFeedFactoryTests

### Community 14 - ".NET Project Setup"
Cohesion: 0.14
Nodes (10): net10.0-windows, coverlet.collector (6.0.4), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), net10.0, Microsoft.NET.Sdk, Microsoft.NET.Sdk (+2 more)

### Community 15 - "FakeReadStream"
Cohesion: 0.08
Nodes (10): FailingStream, FakeReadStream, CanRead, CanSeek, CanWrite, Length, Position, GatedStream (+2 more)

### Community 16 - "Prototype Rest Mode JS"
Cohesion: 0.23
Nodes (12): armDimTimer(), changeTrack(), fetchTemp(), keepScreenOn(), renderClock(), renderMusic(), showTemp(), start() (+4 more)

### Community 17 - "Install & Tunnel Decisions"
Cohesion: 0.16
Nodes (11): Step 2: verify Claude Code environment for real, TelaDois-ipa artifact (TelaDois.ipa + BUILD_INFO.txt), graphify setup (graphifyy, python -m graphify, blocked exe), Install on iPhone via xtool in WSL (bash -lc, USBMUXD_SOCKET_ADDRESS), INSTALACAO.md (install TelaDois on iPhone without paid account), iTunes/iCloud web installers (not Microsoft Store) for Apple Mobile Device Support, iPhone Developer Mode and trusting the Apple ID, Sideloadly install method (alternative) (+3 more)

### Community 18 - "project.yml (XcodeGen project definition)"
Cohesion: 0.19
Nodes (8): Open-Meteo weather API, WeatherService (Open-Meteo + CoreLocation), project.yml (XcodeGen project definition), TelaDoisCore local package dependency, Location when-in-use permission string (for weather), Swift 5 mode with SWIFT_STRICT_CONCURRENCY complete, TelaDois application target, TelaDoisUITests target

### Community 19 - "RestScreenView"
Cohesion: 0.10
Nodes (7): AppSettings, RestScreenView, .body, ScreenBrightness, .screen, TelaDoisApp, .body

### Community 20 - "Clock Page & Battery Ring"
Cohesion: 0.11
Nodes (7): BatteryRingView, .body, .percent, ClockPageView, .body, Theme, SwiftUI

### Community 21 - "Design spec: iPhone second screen + Modo Descanso"
Cohesion: 0.14
Nodes (14): Step 1: update CLAUDE.md, Etapa 4: USB tunnel (done, tested on iPhone), Etapa 5: Segunda Tela (next: virtual monitor + FFmpeg + video), usbmuxd USB tunnel (127.0.0.1:7100), Virtual Display Driver (pipe MTTVirtualDisplayPipe), WSL mirrored networking so Ubuntu reaches Windows usbmuxd 127.0.0.1:27015, Design spec: iPhone second screen + Modo Descanso, PC <-> iPhone architecture over USB (usbmuxd) (+6 more)

### Community 22 - "Hotkey"
Cohesion: 0.16
Nodes (6): 3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`), KeyboardLayout, CreateDXGIFactory1(), Hotkey, Registered, KeyboardLayoutLocalTests

### Community 23 - "LinkStatsMeter"
Cohesion: 0.06
Nodes (14): Dispatch, H264NALType, H264NALUnit, .isVCL, H264Parser, DurationTally, .average, LinkStatsMeter (+6 more)

### Community 25 - "EncoderProfile"
Cohesion: 0.10
Nodes (6): EncoderProfile, EncoderProfiles, Candidates, FfmpegArguments, LocalDriverGateTests, FfmpegArgumentsTests

### Community 26 - "ios-test workflow (swift test + XCUITest + screenshots)"
Cohesion: 0.14
Nodes (5): Step 5: commit and push (never force, no merge to main), ios-build workflow (unsigned Release .ipa), ios-test workflow (swift test + XCUITest + screenshots), install-xcodegen.sh script, pick-simulator.sh script

### Community 31 - "FakeProcessRunner"
Cohesion: 0.20
Nodes (8): FfmpegCapture, Config, Dropped, Meters, BlockingStream, IsDisposed, ReadStarted, FfmpegCaptureTests

### Community 32 - "LinkSessionState"
Cohesion: 0.20
Nodes (5): Action, close, send, LinkSessionState, LinkSessionStateTests

### Community 33 - "TimeSpan"
Cohesion: 0.08
Nodes (20): 1. Objetivo, 2. Ambiente de referência, 3.1 PC (C#, `windows/src/TelaDois.Core`), 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, 4. Tratamento de erros, 5. Testes, 6. Riscos (+12 more)

### Community 34 - "TelaDoisCore"
Cohesion: 0.16
Nodes (7): AVFoundation, CoreLocation, Network, Observation, TelaDoisCore, UIKit, XCTest

### Community 35 - "IReadOnlyList"
Cohesion: 0.11
Nodes (10): ProcessStopper, IProcessRunner, IRunningProcess, Output, RecentErrors, SystemProcess, Output, RecentErrors (+2 more)

### Community 36 - "VirtualDisplay"
Cohesion: 0.06
Nodes (25): IDeviceSwitch, DisplayBounds, DisplayChangeException, Code, DisplayDeviceInfo, DisplayModeInfo, IDisplayApi, VirtualDisplay (+17 more)

### Community 37 - "VideoConfig"
Cohesion: 0.10
Nodes (6): LinkFrame, MessageType, LinkFrameDecoder, LinkMessages, LinkStats, LinkProtocolTests

### Community 39 - "LinkFrame"
Cohesion: 0.13
Nodes (4): bigEndianBytes(), LinkStats, .fields, LinkProtocolTests

### Community 40 - "MonitorMode"
Cohesion: 0.14
Nodes (10): MonitorMode, IsConnected, WantsVideo, ToggleResult, NoPhone, TurnedOff, TurnedOn, IVideoSwitch (+2 more)

### Community 41 - "LinkProtocol"
Cohesion: 0.17
Nodes (6): 3.2 iPhone (Swift), LinkFrame, LinkFrameDecoder, LinkProtocol, LinkVideoFrame, readBigEndian()

### Community 42 - "LinkSessionOptions"
Cohesion: 0.15
Nodes (10): LinkEndReason, Bye, Cancelled, Closed, Timeout, LinkSession, LinkSessionOptions, Default (+2 more)

### Community 44 - "TelaDois.Core.Video"
Cohesion: 0.06
Nodes (13): TelaDois.Core, TelaDois.Core.Tests.Display, TelaDois.Core.Link, TelaDois.Core.Tests.Fakes, TelaDois.Core.Tests, TelaDois.Core.Tests.Video, TelaDois.Core.Video, TelaDois.Core.Display (+5 more)

### Community 45 - "LinkProtocol.swift"
Cohesion: 0.19
Nodes (10): AppState (rest <-> secondScreen state machine), CaptureEncoder (FFmpeg ddagrab + hardware H.264, libx264 fallback), PC<->iPhone protocol (HELLO, CONFIG, VIDEO, KEYFRAME_REQ, PING/PONG, BYE), StreamPlayer (H.264 decode via VideoToolbox), StreamSender (opens tunnel, sends messages, measures latency), UsbLink (iPhone listener on port 7100), LinkDecodeError, payloadTooLarge (+2 more)

### Community 46 - "DriverPipe"
Cohesion: 0.37
Nodes (3): DriverPipe, Timeout, DriverPipeTests

### Community 47 - "LinkMessageType"
Cohesion: 0.18
Nodes (11): LinkMessageType, Bye, Config, Hello, KeyframeRequest, Ping, Pong, Screen (+3 more)

### Community 48 - "Foundation"
Cohesion: 0.21
Nodes (3): Darwin, Foundation, FakePC

### Community 50 - "EncoderProbe"
Cohesion: 0.24
Nodes (8): EncoderProbe, ProbeTimeout, FakeProcess, Output, RecentErrors, FakeProcessRunner, Started, EncoderProbeTests

### Community 52 - "TrayContext"
Cohesion: 0.18
Nodes (3): FileLog, TrayContext, FileLogTests

### Community 53 - "IVideoFeed"
Cohesion: 0.08
Nodes (25): LinkStatus, IVideoFeed, Config, Kind, Meters, VideoKind, Extend, Mirror (+17 more)

### Community 55 - ".ReadAll"
Cohesion: 0.18
Nodes (4): AccessUnitReader, H264, AccessUnitReaderTests, TrickleStream

### Community 56 - "LinkSession"
Cohesion: 0.18
Nodes (10): 1. Objetivo, 2. O driver (lido no código-fonte da release 25.7.23), 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, 5. Testes, 6. Riscos, Comportamento (Parte 1, aprovada), Etapa 5b — Monitor estendido (monitor virtual) + atalho — Especificação de Design (+2 more)

### Community 57 - "FakeVirtualMonitor"
Cohesion: 0.12
Nodes (16): FakeVirtualMonitor, Attached, AttachError, AttachTakes, Calls, CanPowerOff, Detached, Device (+8 more)

### Community 58 - "Etapa 5c — o 3º monitor some quando desligado + tela preta no iPhone — Especificação de Design"
Cohesion: 0.14
Nodes (13): 1. Objetivos, 2.1 Medições que sustentam a decisão (driver 25.7.23, PC Intel, 2026-10-20), 2.2 Instalador (`tools/vdd/instalar-vdd.ps1`, administrador, uma vez por PC), 2.3 TelaDois (sem administrador), 2.4 Riscos a validar no 1º passo do plano (driver real), 2. Parte A — o monitor só existe enquanto está em uso, 3.1 Protocolo (Swift e C#, bytes de referência nos dois testes), 3.2 PC (+5 more)

### Community 59 - "Win32DisplayApi"
Cohesion: 0.21
Nodes (5): 4. Tratamento de erros (Parte 3, aprovada), DevMode, DisplayDevice, Win32DisplayApi, Persist

### Community 60 - "TelaDois app (iPhone second screen + standby)"
Cohesion: 0.33
Nodes (4): Etapa 2: native Modo Descanso (done), Modo Descanso (StandBy-style rest mode), Segunda Tela (PC second monitor over USB), TelaDois app (iPhone second screen + standby)

### Community 61 - "TraySettings"
Cohesion: 0.17
Nodes (5): TelaDois.Tray, Program, TraySettings, FilePath, Monitor

### Community 62 - "VideoConfig"
Cohesion: 0.27
Nodes (4): VideoConfig, FixedFeed, Config, Disposed

### Community 64 - "Fact"
Cohesion: 0.16
Nodes (4): FileRecorder, Files, ScheduledTaskSwitchTests, DisplayCatalogTests

### Community 66 - "TunnelState"
Cohesion: 0.40
Nodes (5): TunnelState, Connected, ServiceMissing, WaitingForApp, WaitingForIPhone

## Knowledge Gaps
- **164 isolated node(s):** `PackageDescription`, `passThrough`, `wokeUp`, `.isVCL`, `hello` (+159 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 320 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **15 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `LinkSessionState` connect `LinkSessionState` to `UsbLink`, `Foundation`, `LinkStatsMeter`, `LinkSession`, `Etapa 5c — o 3º monitor some quando desligado + tela preta no iPhone — Especificação de Design`?**
  _High betweenness centrality (0.213) - this node is a cross-community bridge._
- **Are the 24 inferred relationships involving `FakeVirtualMonitor` (e.g. with `.TurningOnAgainWhileTheOldFeedDetachesKeepsTheSameVirtualMonitorOn()` and `.AttachFailureFallsBackToMirror()`) actually correct?**
  _`FakeVirtualMonitor` has 24 INFERRED edges - model-reasoned connections that need verification._
- **What connects `PackageDescription`, `passThrough`, `wokeUp` to the rest of the system?**
  _164 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `FakeUsbmuxd` be split into smaller, more focused modules?**
  _Cohesion score 0.05605124685426676 - nodes in this community are weakly interconnected._
- **Why does `5. Testes` connect `LinkSession` to `LinkSessionState`, `VirtualDisplay`, `MonitorMode`, `LinkSessionOptions`, `.ReadAll`?**
  _High betweenness centrality (0.200) - this node is a cross-community bridge._
- **Are the 18 inferred relationships involving `VirtualDisplay` (e.g. with `3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`)` and `5. Testes`) actually correct?**
  _`VirtualDisplay` has 18 INFERRED edges - model-reasoned connections that need verification._
- **Should `Open-Meteo Weather` be split into smaller, more focused modules?**
  _Cohesion score 0.13438735177865613 - nodes in this community are weakly interconnected._