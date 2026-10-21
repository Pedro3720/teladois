# Graph Report - Teladois  (2026-10-21)

## Corpus Check
- 135 files · ~64,012 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 8 file(s) not represented in the graph (top: (none) 3, .h264 2, .css 1)

## Summary
- 1556 nodes · 4765 edges · 72 communities (52 shown, 20 thin omitted)
- Extraction: 79% EXTRACTED · 21% INFERRED · 0% AMBIGUOUS · INFERRED: 1013 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `f8aaf2ba`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .RunAsync
- LinkFrame
- DimState
- TelaDois.Core.Video
- FakeUsbmuxd
- VideoLayerView
- Open-Meteo Weather
- H264NALUnit
- Design Tokens & Icon
- Clock Formatting
- Encerrar Session Routine
- UsbLink
- Prototype Package Config
- FakeProcess
- .NET Project Setup
- FakeReadStream
- Prototype Rest Mode JS
- Install & Tunnel Decisions
- RestScreen (Clock and Music pages)
- TelaDoisApp
- Clock Page & Battery Ring
- Etapa 4: USB tunnel (done, tested on iPhone)
- TrayContext
- LinkStatsMeter
- Prototype Static Server
- ios-test workflow (swift test + XCUITest + screenshots)
- rename-screenshots.py
- BatteryMonitor
- Swift Package Manifest
- IReadOnlyList
- LinkSessionState
- FrameQueue
- TelaDoisCore
- TimeSpan
- Etapa 5b — Monitor estendido (monitor virtual) + atalho — Especificação de Design
- Foundation
- LinkProtocol
- LinkProtocolTests
- VideoRenderer
- LinkFrame
- LinkSession.cs
- IVirtualMonitor
- TelaDois.Core.Link
- TelaDois.Core.Video
- DriverPipe
- LinkMessageType
- FakePC
- XCTestCase
- FakeProcess
- TrayContext
- GatedFeed
- H264NALUnit
- List
- Etapa 5b — Monitor estendido (monitor virtual) + atalho — Especificação de Design
- FakeVirtualMonitor
- Etapa 5c — o 3º monitor some quando desligado + tela preta no iPhone — Especificação de Design
- Win32DisplayApi
- RestScreen (Clock and Music pages)
- TraySettings
- Sendable
- TelaDois.Core.Display
- .Create
- VideoRenderer
- TelaDois.Core.Tests.Fakes
- SystemProcess
- MonitorMode.cs
- Foundation
- IScreenSwitch
- IVideoSwitch

## God Nodes (most connected - your core abstractions)
1. `MonitorMode` - 65 edges
2. `FakeVirtualMonitor` - 62 edges
3. `VirtualDisplay` - 54 edges
4. `VideoFeedFactory` - 50 edges
5. `FakeProcess` - 47 edges
6. `VideoConfig` - 44 edges
7. `FakeProcessRunner` - 43 edges
8. `LinkSessionTests` - 43 edges
9. `IVideoFeed` - 41 edges
10. `VirtualDisplayTests` - 40 edges

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

## Communities (72 total, 20 thin omitted)

### Community 0 - ".RunAsync"
Cohesion: 0.12
Nodes (26): LinkFrameReader, VideoConfig, LinkSessionOptions, Default, FirstFrameTimeout, StatsInterval, LinkStatus, IVideoFeed (+18 more)

### Community 1 - "LinkFrame"
Cohesion: 0.15
Nodes (12): .messageType, LinkMessageType, bye, config, hello, keyframeRequest, ping, pong (+4 more)

### Community 2 - "DimState"
Cohesion: 0.28
Nodes (5): DimState, TouchResult, passThrough, wokeUp, DimStateTests

### Community 4 - "FakeUsbmuxd"
Cohesion: 0.05
Nodes (29): TunnelStatusText, Plist, UsbmuxClient, DefaultEndpoint, UsbmuxException, Code, DeviceAttached, DeviceDetached (+21 more)

### Community 6 - "Open-Meteo Weather"
Cohesion: 0.09
Nodes (8): CodingKeys, temperature, Current, MissingTemperature, OpenMeteo, Response, OpenMeteoTests, WeatherProvider

### Community 7 - "H264NALUnit"
Cohesion: 0.26
Nodes (8): VirtualMonitorFeed, Config, Kind, Meters, VirtualMonitorFeedTests, WaitingFeed, Config, Disposed

### Community 8 - "Design Tokens & Icon"
Cohesion: 0.16
Nodes (15): App icon check step (Assets.car, CFBundleIconName=AppIcon), Design tokens (Etapa 1), Color tokens (black, clock #7FA9F2, ink, muted, battery, track, bars), App icon (icone-1024.png, Canva), Landscape layout (clock left, date/temp/battery right, mini player), Typography tokens (Sofia Sans Extra Condensed stretched 1.5x, rounded system font), Landscape-only, full-screen, hidden status bar, iPhone-only settings, index.html (Modo Descanso web prototype) (+7 more)

### Community 9 - "Clock Formatting"
Cohesion: 0.21
Nodes (3): ClockFormatter, ClockText, ClockFormatterTests

### Community 10 - "Encerrar Session Routine"
Cohesion: 0.13
Nodes (20): /encerrar skill (end-of-session routine), Step 3: handoff between computers, Step 4: pre-commit secret/security check, Step 6: summary for the user, Step 1: update CLAUDE.md, Step 2: verify Claude Code environment for real, pages workflow (publish prototype to GitHub Pages), prototype-test workflow (Playwright WebKit) (+12 more)

### Community 12 - "Prototype Package Config"
Cohesion: 0.12
Nodes (11): devDependencies, @playwright/test, name, private, scripts, serve, test, type (+3 more)

### Community 13 - "FakeProcess"
Cohesion: 0.15
Nodes (4): DisplayOutput, VideoFeedFactory, OutputWait, VideoFeedFactoryTests

### Community 14 - ".NET Project Setup"
Cohesion: 0.14
Nodes (10): net10.0-windows, coverlet.collector (6.0.4), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), net10.0, Microsoft.NET.Sdk, Microsoft.NET.Sdk (+2 more)

### Community 15 - "FakeReadStream"
Cohesion: 0.07
Nodes (11): FakeReadStream, CanRead, CanSeek, CanWrite, Length, Position, GatedStream, AtGate (+3 more)

### Community 16 - "Prototype Rest Mode JS"
Cohesion: 0.23
Nodes (12): armDimTimer(), changeTrack(), fetchTemp(), keepScreenOn(), renderClock(), renderMusic(), showTemp(), start() (+4 more)

### Community 17 - "Install & Tunnel Decisions"
Cohesion: 0.19
Nodes (10): TelaDois-ipa artifact (TelaDois.ipa + BUILD_INFO.txt), usbmuxd USB tunnel (127.0.0.1:7100), Install on iPhone via xtool in WSL (bash -lc, USBMUXD_SOCKET_ADDRESS), INSTALACAO.md (install TelaDois on iPhone without paid account), iTunes/iCloud web installers (not Microsoft Store) for Apple Mobile Device Support, iPhone Developer Mode and trusting the Apple ID, Sideloadly install method (alternative), WSL mirrored networking so Ubuntu reaches Windows usbmuxd 127.0.0.1:27015 (+2 more)

### Community 18 - "RestScreen (Clock and Music pages)"
Cohesion: 0.18
Nodes (8): Open-Meteo weather API, WeatherService (Open-Meteo + CoreLocation), project.yml (XcodeGen project definition), TelaDoisCore local package dependency, Location when-in-use permission string (for weather), Swift 5 mode with SWIFT_STRICT_CONCURRENCY complete, TelaDois application target, TelaDoisUITests target

### Community 19 - "TelaDoisApp"
Cohesion: 0.10
Nodes (7): AppSettings, RestScreenView, .body, ScreenBrightness, .screen, TelaDoisApp, .body

### Community 20 - "Clock Page & Battery Ring"
Cohesion: 0.11
Nodes (7): BatteryRingView, .body, .percent, ClockPageView, .body, Theme, SwiftUI

### Community 21 - "Etapa 4: USB tunnel (done, tested on iPhone)"
Cohesion: 0.13
Nodes (15): Etapa 4: USB tunnel (done, tested on iPhone), Etapa 5: Segunda Tela (next: virtual monitor + FFmpeg + video), Segunda Tela (PC second monitor over USB), Virtual Display Driver (pipe MTTVirtualDisplayPipe), AppState (rest <-> secondScreen state machine), PC <-> iPhone architecture over USB (usbmuxd), CaptureEncoder (FFmpeg ddagrab + hardware H.264, libx264 fallback), DeviceWatcher (PC usbmuxd client, attach/detach) (+7 more)

### Community 22 - "TrayContext"
Cohesion: 0.15
Nodes (7): 3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`), KeyboardLayout, CreateDXGIFactory1(), OutputDesc, Hotkey, Registered, KeyboardLayoutLocalTests

### Community 23 - "LinkStatsMeter"
Cohesion: 0.19
Nodes (4): DurationTally, .average, LinkStatsMeter, LinkStatsMeterTests

### Community 25 - "ios-test workflow (swift test + XCUITest + screenshots)"
Cohesion: 0.17
Nodes (5): EncoderProfile, EncoderProfiles, Candidates, FfmpegArguments, FfmpegArgumentsTests

### Community 26 - "rename-screenshots.py"
Cohesion: 0.12
Nodes (7): Step 5: commit and push (never force, no merge to main), ios-build workflow (unsigned Release .ipa), ios-test workflow (swift test + XCUITest + screenshots), screenshots artifact (renamed simulator prints), UI-test launch arguments (-TDFixedDate, -TDDimSeconds, ...), install-xcodegen.sh script, pick-simulator.sh script

### Community 31 - "IReadOnlyList"
Cohesion: 0.19
Nodes (11): FfmpegCapture, Config, Dropped, Meters, BlockingStream, IsDisposed, ReadStarted, FailingStream (+3 more)

### Community 32 - "LinkSessionState"
Cohesion: 0.24
Nodes (4): Action, send, LinkSessionState, LinkSessionStateTests

### Community 33 - "FrameQueue"
Cohesion: 0.08
Nodes (18): 1. Objetivo, 2. Ambiente de referência, 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, 4. Tratamento de erros, 6. Riscos, Etapa 5a — Vídeo pelo cabo (monitor existente → iPhone) — Especificação de Design, Fora do escopo da 5a (+10 more)

### Community 34 - "TelaDoisCore"
Cohesion: 0.16
Nodes (7): AVFoundation, CoreLocation, Network, Observation, TelaDoisCore, UIKit, XCTest

### Community 35 - "TimeSpan"
Cohesion: 0.14
Nodes (9): ScheduledTaskSwitch, Timeout, ProcessStopper, IProcessRunner, IRunningProcess, Output, RecentErrors, FileRecorder (+1 more)

### Community 36 - "Etapa 5b — Monitor estendido (monitor virtual) + atalho — Especificação de Design"
Cohesion: 0.06
Nodes (29): 4. Tratamento de erros (Parte 3, aprovada), 5. Testes, IDriverPipe, IDeviceSwitch, DisplayBounds, DisplayChangeException, Code, DisplayDeviceInfo (+21 more)

### Community 37 - "Foundation"
Cohesion: 0.08
Nodes (8): LinkFrame, MessageType, LinkFrameDecoder, HelloInfo, LinkMessages, LinkStats, LinkProtocolTests, DisplayCatalogTests

### Community 40 - "VideoRenderer"
Cohesion: 0.16
Nodes (6): MonitorMode, IsConnected, WantsDark, WantsVideo, State, MonitorModeTests

### Community 41 - "LinkFrame"
Cohesion: 0.12
Nodes (12): 3.2 iPhone (Swift), bigEndianBytes(), LinkDecodeError, payloadTooLarge, LinkFrame, LinkHello, LinkProtocol, LinkStats (+4 more)

### Community 42 - "LinkSession.cs"
Cohesion: 0.25
Nodes (5): LinkEndReason, Bye, Cancelled, Closed, Timeout

### Community 44 - "TelaDois.Core.Link"
Cohesion: 0.12
Nodes (4): TelaDois.Core, TelaDois.Core.Link, TelaDois.Core.Tests, TelaDois.Core.Usbmux

### Community 45 - "TelaDois.Core.Video"
Cohesion: 0.13
Nodes (4): TelaDois.Core.Tests.Video, TelaDois.Core.Video, FfmpegLocator, H264

### Community 46 - "DriverPipe"
Cohesion: 0.37
Nodes (3): DriverPipe, Timeout, DriverPipeTests

### Community 47 - "LinkMessageType"
Cohesion: 0.18
Nodes (11): LinkMessageType, Bye, Config, Hello, KeyframeRequest, Ping, Pong, Screen (+3 more)

### Community 50 - "FakeProcess"
Cohesion: 0.32
Nodes (6): EncoderProbe, ProbeTimeout, FakeProcess, Output, RecentErrors, EncoderProbeTests

### Community 52 - "TrayContext"
Cohesion: 0.14
Nodes (3): FileLog, TrayContext, FileLogTests

### Community 53 - "GatedFeed"
Cohesion: 0.11
Nodes (14): VideoKind, Extend, Mirror, GatedFeed, Config, Disposed, FirstFrameMisses, Kind (+6 more)

### Community 54 - "H264NALUnit"
Cohesion: 0.22
Nodes (5): H264NALType, H264NALUnit, .isVCL, H264Parser, H264ParserTests

### Community 55 - "List"
Cohesion: 0.24
Nodes (4): 3.1 PC (C#, `windows/src/TelaDois.Core`), 5. Testes, LinkSession, AccessUnitReader

### Community 56 - "Etapa 5b — Monitor estendido (monitor virtual) + atalho — Especificação de Design"
Cohesion: 0.20
Nodes (9): 1. Objetivo, 2. O driver (lido no código-fonte da release 25.7.23), 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, 6. Riscos, Comportamento (Parte 1, aprovada), Etapa 5b — Monitor estendido (monitor virtual) + atalho — Especificação de Design, Fora do escopo da 5b (+1 more)

### Community 57 - "FakeVirtualMonitor"
Cohesion: 0.10
Nodes (19): FakeVirtualMonitor, Attached, AttachError, AttachTakes, Calls, CanPowerOff, Detached, Device (+11 more)

### Community 58 - "Etapa 5c — o 3º monitor some quando desligado + tela preta no iPhone — Especificação de Design"
Cohesion: 0.14
Nodes (13): 1. Objetivos, 2.1 Medições que sustentam a decisão (driver 25.7.23, PC Intel, 2026-10-20), 2.2 Instalador (`tools/vdd/instalar-vdd.ps1`, administrador, uma vez por PC), 2.3 TelaDois (sem administrador), 2.4 Riscos a validar no 1º passo do plano (driver real), 2. Parte A — o monitor só existe enquanto está em uso, 3.1 Protocolo (Swift e C#, bytes de referência nos dois testes), 3.2 PC (+5 more)

### Community 59 - "Win32DisplayApi"
Cohesion: 0.27
Nodes (4): DevMode, DisplayDevice, Win32DisplayApi, Persist

### Community 60 - "RestScreen (Clock and Music pages)"
Cohesion: 0.22
Nodes (8): Etapa 2: native Modo Descanso (done), Etapa 3: Spotify (deferred), Modo Descanso (StandBy-style rest mode), Behavior tokens (60 s dim with 1.2 s fade, wake tap does not press buttons), BatteryService (UIDevice), DimController (60 s inactivity, black screen, brightness 0), RestScreen (Clock and Music pages), SpotifyService (App Remote SDK)

### Community 61 - "TraySettings"
Cohesion: 0.13
Nodes (5): TelaDois.Tray, Program, TraySettings, FilePath, Monitor

### Community 62 - "Sendable"
Cohesion: 0.20
Nodes (4): Dispatch, MainLagProbe, VideoStats, os

### Community 63 - "TelaDois.Core.Display"
Cohesion: 0.15
Nodes (4): TelaDois.Core.Tests.Display, TelaDois.Core.Display, LocalDriverGate, LocalDriverGateTests

### Community 67 - "SystemProcess"
Cohesion: 0.24
Nodes (5): SystemProcess, Output, RecentErrors, SystemProcessRunner, SystemProcessRunnerTests

### Community 68 - "MonitorMode.cs"
Cohesion: 0.22
Nodes (8): DarkToggleResult, NoVideo, TurnedDark, TurnedLit, ToggleResult, NoPhone, TurnedOff, TurnedOn

## Knowledge Gaps
- **173 isolated node(s):** `PackageDescription`, `passThrough`, `wokeUp`, `.isVCL`, `hello` (+168 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 332 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **20 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `LinkSessionState` connect `LinkSessionState` to `VideoRenderer`, `Etapa 5b — Monitor estendido (monitor virtual) + atalho — Especificação de Design`, `Foundation`, `UsbLink`, `Etapa 5c — o 3º monitor some quando desligado + tela preta no iPhone — Especificação de Design`, `Sendable`?**
  _High betweenness centrality (0.223) - this node is a cross-community bridge._
- **Are the 44 inferred relationships involving `MonitorMode` (e.g. with `3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`)` and `4. Tratamento de erros (Parte 3, aprovada)`) actually correct?**
  _`MonitorMode` has 44 INFERRED edges - model-reasoned connections that need verification._
- **What connects `PackageDescription`, `passThrough`, `wokeUp` to the rest of the system?**
  _173 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.RunAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.11854460093896714 - nodes in this community are weakly interconnected._
- **Why does `5. Testes` connect `Etapa 5b — Monitor estendido (monitor virtual) + atalho — Especificação de Design` to `LinkSessionState`, `VideoRenderer`, `Etapa 5b — Monitor estendido (monitor virtual) + atalho — Especificação de Design`, `List`?**
  _High betweenness centrality (0.212) - this node is a cross-community bridge._
- **Are the 29 inferred relationships involving `FakeVirtualMonitor` (e.g. with `.TurningOnAgainWhileTheOldFeedDetachesKeepsTheSameVirtualMonitorOn()` and `.AttachFailureFallsBackToMirror()`) actually correct?**
  _`FakeVirtualMonitor` has 29 INFERRED edges - model-reasoned connections that need verification._
- **Should `FakeUsbmuxd` be split into smaller, more focused modules?**
  _Cohesion score 0.05263157894736842 - nodes in this community are weakly interconnected._