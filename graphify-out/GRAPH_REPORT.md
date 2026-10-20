# Graph Report - Aplicativo celular  (2026-10-20)

## Corpus Check
- 122 files · ~50,918 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 8 file(s) not represented in the graph (top: (none) 3, .h264 2, .css 1)

## Summary
- 1286 nodes · 3691 edges · 70 communities (49 shown, 21 thin omitted)
- Extraction: 80% EXTRACTED · 20% INFERRED · 0% AMBIGUOUS · INFERRED: 738 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `415c6a9b`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .RunAsync
- LinkMessageType
- DimState
- TelaDois.Core.Link
- FakeUsbmuxd
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
- Hotkey
- TelaDois app (iPhone second screen + standby)
- Prototype Static Server
- ios-build workflow (unsigned Release .ipa)
- ios-test workflow (swift test + XCUITest + screenshots)
- BatteryMonitor
- Swift Package Manifest
- FakeProcessRunner
- LinkSessionState
- FrameQueue
- TelaDoisCore
- RestScreenView
- VirtualDisplay
- VideoConfig
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
- TrayContext
- GatedFeed
- .ListenAsync
- .ReadAll
- LinkSession
- TunnelStatus
- Dictionary
- FakeDisplayApi
- RestScreen (Clock and Music pages)
- UsbmuxClient
- VideoRenderer
- Win32DisplayApi
- Fact
- DisplayModeInfo
- UsbTunnelService
- Stream
- .HandleAsync
- .IsEnabled

## God Nodes (most connected - your core abstractions)
1. `MonitorMode` - 41 edges
2. `FakeProcessRunner` - 40 edges
3. `FakeProcess` - 39 edges
4. `FakeVirtualMonitor` - 39 edges
5. `VideoFeedFactory` - 38 edges
6. `VideoConfig` - 37 edges
7. `IVideoFeed` - 35 edges
8. `LinkSessionTests` - 35 edges
9. `FakeUsbmuxd` - 34 edges
10. `VirtualDisplay` - 33 edges

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

## Communities (70 total, 21 thin omitted)

### Community 0 - ".RunAsync"
Cohesion: 0.18
Nodes (14): LinkFrameReader, IVideoFeed, Config, Kind, EndlessFeed, Config, Disposed, FixedFeed (+6 more)

### Community 1 - "LinkMessageType"
Cohesion: 0.18
Nodes (10): .messageType, LinkMessageType, bye, config, hello, keyframeRequest, ping, pong (+2 more)

### Community 2 - "DimState"
Cohesion: 0.28
Nodes (5): DimState, TouchResult, passThrough, wokeUp, DimStateTests

### Community 3 - "TelaDois.Core.Link"
Cohesion: 0.14
Nodes (5): TelaDois.Core, TelaDois.Core.Link, TelaDois.Tray, TelaDois.Core.Tests, TelaDois.Core.Usbmux

### Community 4 - "FakeUsbmuxd"
Cohesion: 0.28
Nodes (5): FakeUsbmuxd, Endpoint, EventsAfterListen, Requests, UsbTunnelServiceTests

### Community 6 - "Open-Meteo Weather"
Cohesion: 0.09
Nodes (8): CodingKeys, temperature, Current, MissingTemperature, OpenMeteo, Response, OpenMeteoTests, WeatherProvider

### Community 7 - "H264NALUnit"
Cohesion: 0.22
Nodes (5): H264NALType, H264NALUnit, .isVCL, H264Parser, H264ParserTests

### Community 8 - "Design Tokens & Icon"
Cohesion: 0.19
Nodes (13): Design tokens (Etapa 1), Color tokens (black, clock #7FA9F2, ink, muted, battery, track, bars), Landscape layout (clock left, date/temp/battery right, mini player), Typography tokens (Sofia Sans Extra Condensed stretched 1.5x, rounded system font), Landscape-only, full-screen, hidden status bar, iPhone-only settings, index.html (Modo Descanso web prototype), Battery ring SVG (battery-level circle), Dim overlay (black screen after inactivity) (+5 more)

### Community 9 - "Clock Formatting"
Cohesion: 0.21
Nodes (3): ClockFormatter, ClockText, ClockFormatterTests

### Community 10 - "Encerrar Session Routine"
Cohesion: 0.12
Nodes (21): /encerrar skill (end-of-session routine), Step 5: commit and push (never force, no merge to main), Step 3: handoff between computers, Step 4: pre-commit secret/security check, Step 6: summary for the user, Step 1: update CLAUDE.md, Step 2: verify Claude Code environment for real, pages workflow (publish prototype to GitHub Pages) (+13 more)

### Community 12 - "Prototype Package Config"
Cohesion: 0.12
Nodes (11): devDependencies, @playwright/test, name, private, scripts, serve, test, type (+3 more)

### Community 13 - "Fact"
Cohesion: 0.06
Nodes (26): IVirtualMonitor, DisplayOutput, AccessUnit, VideoFeedFactory, OutputWait, VirtualMonitorFeed, Config, Kind (+18 more)

### Community 14 - ".NET Project Setup"
Cohesion: 0.14
Nodes (10): net10.0-windows, coverlet.collector (6.0.4), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), net10.0, Microsoft.NET.Sdk, Microsoft.NET.Sdk (+2 more)

### Community 15 - "FakeReadStream"
Cohesion: 0.08
Nodes (9): FakeReadStream, CanRead, CanSeek, CanWrite, Length, Position, GatedStream, AtGate (+1 more)

### Community 16 - "Prototype Rest Mode JS"
Cohesion: 0.23
Nodes (12): armDimTimer(), changeTrack(), fetchTemp(), keepScreenOn(), renderClock(), renderMusic(), showTemp(), start() (+4 more)

### Community 17 - "Install & Tunnel Decisions"
Cohesion: 0.23
Nodes (9): usbmuxd USB tunnel (127.0.0.1:7100), Install on iPhone via xtool in WSL (bash -lc, USBMUXD_SOCKET_ADDRESS), INSTALACAO.md (install TelaDois on iPhone without paid account), iTunes/iCloud web installers (not Microsoft Store) for Apple Mobile Device Support, iPhone Developer Mode and trusting the Apple ID, Sideloadly install method (alternative), WSL mirrored networking so Ubuntu reaches Windows usbmuxd 127.0.0.1:27015, xtool install method (primary) (+1 more)

### Community 18 - "project.yml (XcodeGen project definition)"
Cohesion: 0.38
Nodes (5): project.yml (XcodeGen project definition), TelaDoisCore local package dependency, Swift 5 mode with SWIFT_STRICT_CONCURRENCY complete, TelaDois application target, TelaDoisUITests target

### Community 19 - "RestScreenView"
Cohesion: 0.14
Nodes (4): AppSettings, RestScreenView, TelaDoisApp, .body

### Community 20 - "Clock Page & Battery Ring"
Cohesion: 0.11
Nodes (7): BatteryRingView, .body, .percent, ClockPageView, .body, Theme, SwiftUI

### Community 21 - "Etapa 4: USB tunnel (done, tested on iPhone)"
Cohesion: 0.24
Nodes (9): AppState (rest <-> secondScreen state machine), PC <-> iPhone architecture over USB (usbmuxd), CaptureEncoder (FFmpeg ddagrab + hardware H.264, libx264 fallback), DeviceWatcher (PC usbmuxd client, attach/detach), PC<->iPhone protocol (HELLO, CONFIG, VIDEO, KEYFRAME_REQ, PING/PONG, BYE), StreamPlayer (H.264 decode via VideoToolbox), StreamSender (opens tunnel, sends messages, measures latency), UsbLink (iPhone listener on port 7100) (+1 more)

### Community 22 - "Hotkey"
Cohesion: 0.12
Nodes (7): 3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`), KeyboardLayout, CreateDXGIFactory1(), OutputDesc, Hotkey, Registered, KeyboardLayoutLocalTests

### Community 23 - "TelaDois app (iPhone second screen + standby)"
Cohesion: 0.29
Nodes (6): Etapa 4: USB tunnel (done, tested on iPhone), Etapa 5: Segunda Tela (next: virtual monitor + FFmpeg + video), Segunda Tela (PC second monitor over USB), Virtual Display Driver (pipe MTTVirtualDisplayPipe), DisplayManager (Virtual Display Driver control, cleanup of leftover monitors), Error handling table (cable removed, app left, encoder missing, decode error)

### Community 25 - "ios-build workflow (unsigned Release .ipa)"
Cohesion: 0.18
Nodes (5): ios-build workflow (unsigned Release .ipa), App icon check step (Assets.car, CFBundleIconName=AppIcon), TelaDois-ipa artifact (TelaDois.ipa + BUILD_INFO.txt), App icon (icone-1024.png, Canva), install-xcodegen.sh script

### Community 26 - "ios-test workflow (swift test + XCUITest + screenshots)"
Cohesion: 0.17
Nodes (4): ios-test workflow (swift test + XCUITest + screenshots), screenshots artifact (renamed simulator prints), UI-test launch arguments (-TDFixedDate, -TDDimSeconds, ...), pick-simulator.sh script

### Community 31 - "FakeProcessRunner"
Cohesion: 0.05
Nodes (32): EncoderProbe, ProbeTimeout, EncoderProfile, EncoderProfiles, Candidates, FfmpegArguments, FfmpegCapture, Config (+24 more)

### Community 32 - "LinkSessionState"
Cohesion: 0.24
Nodes (4): Action, send, LinkSessionState, LinkSessionStateTests

### Community 33 - "FrameQueue"
Cohesion: 0.17
Nodes (13): 1. Objetivo, 2. Ambiente de referência, 3.1 PC (C#, `windows/src/TelaDois.Core`), 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, 4. Tratamento de erros, 5. Testes, 6. Riscos (+5 more)

### Community 34 - "TelaDoisCore"
Cohesion: 0.16
Nodes (7): AVFoundation, CoreLocation, Network, Observation, TelaDoisCore, UIKit, XCTest

### Community 35 - "RestScreenView"
Cohesion: 0.28
Nodes (3): .body, ScreenBrightness, .screen

### Community 36 - "VirtualDisplay"
Cohesion: 0.22
Nodes (6): IDriverPipe, DisplayChangeException, Code, VirtualDisplay, NoPipe, VirtualDisplayTests

### Community 37 - "VideoConfig"
Cohesion: 0.10
Nodes (8): LinkFrame, MessageType, LinkFrameDecoder, HelloInfo, LinkMessages, VideoConfig, LinkStatus, LinkProtocolTests

### Community 40 - "MonitorMode"
Cohesion: 0.18
Nodes (6): MonitorMode, IsConnected, WantsVideo, IVideoSwitch, WantsVideo, MonitorModeTests

### Community 41 - "LinkProtocol"
Cohesion: 0.14
Nodes (10): 3.2 iPhone (Swift), bigEndianBytes(), LinkDecodeError, payloadTooLarge, LinkFrame, LinkHello, LinkProtocol, LinkVideoConfig (+2 more)

### Community 42 - "LinkSession.cs"
Cohesion: 0.33
Nodes (5): LinkEndReason, Bye, Cancelled, Closed, Timeout

### Community 43 - "TelaDois.Core.Display"
Cohesion: 0.10
Nodes (8): TelaDois.Core.Tests.Display, TelaDois.Core.Display, ToggleResult, NoPhone, TurnedOff, TurnedOn, LocalDriverGateTests, VirtualDisplayLocalTests

### Community 46 - "DriverPipe"
Cohesion: 0.37
Nodes (3): DriverPipe, Timeout, DriverPipeTests

### Community 47 - "LinkMessageType"
Cohesion: 0.22
Nodes (9): LinkMessageType, Bye, Config, Hello, KeyframeRequest, Ping, Pong, Video (+1 more)

### Community 48 - "FakePC"
Cohesion: 0.19
Nodes (4): Darwin, Foundation, close, FakePC

### Community 52 - "TrayContext"
Cohesion: 0.10
Nodes (7): FileLog, Program, TrayContext, TraySettings, FilePath, Monitor, FileLogTests

### Community 53 - "GatedFeed"
Cohesion: 0.11
Nodes (14): VideoKind, Extend, Mirror, GatedFeed, Config, Disposed, FirstFrameMisses, Kind (+6 more)

### Community 54 - ".ListenAsync"
Cohesion: 0.24
Nodes (6): DeviceAttached, DeviceDetached, ListenStarted, UsbmuxEvent, UsbmuxMessages, UsbmuxTests

### Community 55 - ".ReadAll"
Cohesion: 0.20
Nodes (4): AccessUnitReader, H264, AccessUnitReaderTests, TrickleStream

### Community 56 - "LinkSession"
Cohesion: 0.12
Nodes (12): 1. Objetivo, 2. O driver (lido no código-fonte da release 25.7.23), 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, 4. Tratamento de erros (Parte 3, aprovada), 5. Testes, 6. Riscos, Comportamento (Parte 1, aprovada) (+4 more)

### Community 57 - "TunnelStatus"
Cohesion: 0.22
Nodes (8): TunnelStatusText, TunnelState, Connected, ServiceMissing, WaitingForApp, WaitingForIPhone, TunnelStatus, TunnelStatusTextTests

### Community 59 - "FakeDisplayApi"
Cohesion: 0.21
Nodes (9): DisplayBounds, DisplayDeviceInfo, IDisplayApi, FakeDisplayApi, Applied, BoundsList, DeviceList, ModeList (+1 more)

### Community 60 - "RestScreen (Clock and Music pages)"
Cohesion: 0.16
Nodes (11): Etapa 2: native Modo Descanso (done), Etapa 3: Spotify (deferred), Modo Descanso (StandBy-style rest mode), Open-Meteo weather API, Behavior tokens (60 s dim with 1.2 s fade, wake tap does not press buttons), BatteryService (UIDevice), DimController (60 s inactivity, black screen, brightness 0), RestScreen (Clock and Music pages) (+3 more)

### Community 61 - "UsbmuxClient"
Cohesion: 0.19
Nodes (5): UsbmuxClient, DefaultEndpoint, UsbmuxException, Code, UsbmuxPacket

### Community 63 - "Win32DisplayApi"
Cohesion: 0.24
Nodes (4): DevMode, DisplayDevice, Win32DisplayApi, Persist

### Community 66 - "UsbTunnelService"
Cohesion: 0.29
Nodes (5): LinkSessionOptions, Default, FirstFrameTimeout, UsbTunnelService, Retry

## Knowledge Gaps
- **137 isolated node(s):** `PackageDescription`, `passThrough`, `wokeUp`, `.isVCL`, `hello` (+132 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 275 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **21 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `LinkSessionState` connect `LinkSessionState` to `LinkSession`, `FakePC`, `UsbLink`, `VideoRenderer`?**
  _High betweenness centrality (0.187) - this node is a cross-community bridge._
- **Are the 27 inferred relationships involving `MonitorMode` (e.g. with `3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`)` and `4. Tratamento de erros (Parte 3, aprovada)`) actually correct?**
  _`MonitorMode` has 27 INFERRED edges - model-reasoned connections that need verification._
- **What connects `PackageDescription`, `passThrough`, `wokeUp` to the rest of the system?**
  _137 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `TelaDois.Core.Link` be split into smaller, more focused modules?**
  _Cohesion score 0.1380952380952381 - nodes in this community are weakly interconnected._
- **Why does `5. Testes` connect `LinkSession` to `LinkSessionState`, `VirtualDisplay`, `MonitorMode`, `.ReadAll`, `FakeDisplayApi`?**
  _High betweenness centrality (0.187) - this node is a cross-community bridge._
- **Are the 29 inferred relationships involving `FakeProcessRunner` (e.g. with `.AlreadyCancelledTokenStartsNothing()` and `.CancellingDuringWaitDisposesRunningProcess()`) actually correct?**
  _`FakeProcessRunner` has 29 INFERRED edges - model-reasoned connections that need verification._
- **Should `Open-Meteo Weather` be split into smaller, more focused modules?**
  _Cohesion score 0.09462365591397849 - nodes in this community are weakly interconnected._