# Graph Report - Aplicativo celular  (2026-10-20)

## Corpus Check
- 129 files · ~54,183 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 8 file(s) not represented in the graph (top: (none) 3, .h264 2, .css 1)

## Summary
- 1384 nodes · 3990 edges · 69 communities (51 shown, 18 thin omitted)
- Extraction: 80% EXTRACTED · 20% INFERRED · 0% AMBIGUOUS · INFERRED: 813 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `308d1fd3`
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
- LinkSession.cs
- TelaDois.Core.Display
- TelaDois.Core.Video
- TelaDois.Core.Tests.Fakes
- DriverPipe
- LinkMessageType
- FakePC
- XCTestCase
- EncoderProbe
- TrayContext
- GatedFeed
- TrayContext.cs
- .ReadAll
- LinkSession
- .Detach
- PlistTests.cs
- FakeDisplayApi
- RestScreen (Clock and Music pages)
- Foundation
- VideoRenderer
- SlowToDisposeFeed
- Fact
- DisplayModeInfo
- ToggleResult
- HelloInfo
- EndlessFeed

## God Nodes (most connected - your core abstractions)
1. `MonitorMode` - 41 edges
2. `FakeProcessRunner` - 41 edges
3. `FakeProcess` - 40 edges
4. `FakeVirtualMonitor` - 39 edges
5. `VideoConfig` - 38 edges
6. `IVideoFeed` - 38 edges
7. `VideoFeedFactory` - 38 edges
8. `LinkSessionTests` - 37 edges
9. `FfmpegCapture` - 36 edges
10. `TelaDois.Core.Video` - 34 edges

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

## Communities (69 total, 18 thin omitted)

### Community 0 - ".RunAsync"
Cohesion: 0.19
Nodes (12): LinkFrameReader, IVideoFeed, Config, Kind, Meters, FixedFeed, Config, Disposed (+4 more)

### Community 1 - "LinkMessageType"
Cohesion: 0.17
Nodes (11): .messageType, LinkMessageType, bye, config, hello, keyframeRequest, ping, pong (+3 more)

### Community 2 - "DimState"
Cohesion: 0.23
Nodes (6): DimState, TouchResult, passThrough, wokeUp, DimStateTests, .body

### Community 4 - "FakeUsbmuxd"
Cohesion: 0.06
Nodes (24): LinkSessionOptions, Default, FirstFrameTimeout, StatsInterval, Plist, UsbmuxClient, DefaultEndpoint, UsbmuxException (+16 more)

### Community 6 - "Open-Meteo Weather"
Cohesion: 0.10
Nodes (8): CodingKeys, temperature, Current, MissingTemperature, OpenMeteo, Response, OpenMeteoTests, WeatherProvider

### Community 7 - "H264NALUnit"
Cohesion: 0.22
Nodes (5): H264NALType, H264NALUnit, .isVCL, H264Parser, H264ParserTests

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

### Community 13 - "Fact"
Cohesion: 0.06
Nodes (31): IVirtualMonitor, DisplayOutput, AccessUnit, SystemProcess, Output, RecentErrors, VideoFeedFactory, OutputWait (+23 more)

### Community 14 - ".NET Project Setup"
Cohesion: 0.14
Nodes (10): net10.0-windows, coverlet.collector (6.0.4), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), net10.0, Microsoft.NET.Sdk, Microsoft.NET.Sdk (+2 more)

### Community 15 - "FakeReadStream"
Cohesion: 0.08
Nodes (13): BlockingStream, IsDisposed, ReadStarted, FailingStream, FakeReadStream, CanRead, CanSeek, CanWrite (+5 more)

### Community 16 - "Prototype Rest Mode JS"
Cohesion: 0.23
Nodes (12): armDimTimer(), changeTrack(), fetchTemp(), keepScreenOn(), renderClock(), renderMusic(), showTemp(), start() (+4 more)

### Community 17 - "Install & Tunnel Decisions"
Cohesion: 0.19
Nodes (10): TelaDois-ipa artifact (TelaDois.ipa + BUILD_INFO.txt), usbmuxd USB tunnel (127.0.0.1:7100), Install on iPhone via xtool in WSL (bash -lc, USBMUXD_SOCKET_ADDRESS), INSTALACAO.md (install TelaDois on iPhone without paid account), iTunes/iCloud web installers (not Microsoft Store) for Apple Mobile Device Support, iPhone Developer Mode and trusting the Apple ID, Sideloadly install method (alternative), WSL mirrored networking so Ubuntu reaches Windows usbmuxd 127.0.0.1:27015 (+2 more)

### Community 18 - "project.yml (XcodeGen project definition)"
Cohesion: 0.18
Nodes (8): Open-Meteo weather API, WeatherService (Open-Meteo + CoreLocation), project.yml (XcodeGen project definition), TelaDoisCore local package dependency, Location when-in-use permission string (for weather), Swift 5 mode with SWIFT_STRICT_CONCURRENCY complete, TelaDois application target, TelaDoisUITests target

### Community 19 - "RestScreenView"
Cohesion: 0.10
Nodes (6): AppSettings, RestScreenView, ScreenBrightness, .screen, TelaDoisApp, .body

### Community 20 - "Clock Page & Battery Ring"
Cohesion: 0.11
Nodes (7): BatteryRingView, .body, .percent, ClockPageView, .body, Theme, SwiftUI

### Community 21 - "Etapa 4: USB tunnel (done, tested on iPhone)"
Cohesion: 0.13
Nodes (15): Etapa 4: USB tunnel (done, tested on iPhone), Etapa 5: Segunda Tela (next: virtual monitor + FFmpeg + video), Segunda Tela (PC second monitor over USB), Virtual Display Driver (pipe MTTVirtualDisplayPipe), AppState (rest <-> secondScreen state machine), PC <-> iPhone architecture over USB (usbmuxd), CaptureEncoder (FFmpeg ddagrab + hardware H.264, libx264 fallback), DeviceWatcher (PC usbmuxd client, attach/detach) (+7 more)

### Community 22 - "Hotkey"
Cohesion: 0.12
Nodes (9): 3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`), KeyboardLayout, DevMode, DisplayDevice, Win32DisplayApi, Persist, CreateDXGIFactory1(), Hotkey (+1 more)

### Community 23 - "LinkStatsMeter"
Cohesion: 0.10
Nodes (8): Dispatch, DurationTally, .average, LinkStatsMeter, LinkStatsMeterTests, MainLagProbe, VideoStats, os

### Community 25 - "EncoderProfile"
Cohesion: 0.16
Nodes (5): EncoderProfile, EncoderProfiles, Candidates, FfmpegArguments, FfmpegArgumentsTests

### Community 26 - "ios-test workflow (swift test + XCUITest + screenshots)"
Cohesion: 0.12
Nodes (7): Step 5: commit and push (never force, no merge to main), ios-build workflow (unsigned Release .ipa), ios-test workflow (swift test + XCUITest + screenshots), screenshots artifact (renamed simulator prints), UI-test launch arguments (-TDFixedDate, -TDDimSeconds, ...), install-xcodegen.sh script, pick-simulator.sh script

### Community 31 - "FakeProcessRunner"
Cohesion: 0.23
Nodes (10): FfmpegCapture, Config, Dropped, Meters, FakeProcess, Output, RecentErrors, FakeProcessRunner (+2 more)

### Community 32 - "LinkSessionState"
Cohesion: 0.24
Nodes (4): Action, send, LinkSessionState, LinkSessionStateTests

### Community 33 - "TimeSpan"
Cohesion: 0.09
Nodes (17): 1. Objetivo, 2. Ambiente de referência, 4. Tratamento de erros, 5. Testes, 6. Riscos, Etapa 5a — Vídeo pelo cabo (monitor existente → iPhone) — Especificação de Design, Fora do escopo da 5a, FrameQueue (+9 more)

### Community 34 - "TelaDoisCore"
Cohesion: 0.16
Nodes (7): AVFoundation, CoreLocation, Network, Observation, TelaDoisCore, UIKit, XCTest

### Community 35 - "IReadOnlyList"
Cohesion: 0.25
Nodes (6): ProcessStopper, IProcessRunner, IRunningProcess, Output, RecentErrors, SystemProcessRunner

### Community 36 - "VirtualDisplay"
Cohesion: 0.22
Nodes (6): IDriverPipe, DisplayChangeException, Code, VirtualDisplay, NoPipe, VirtualDisplayTests

### Community 37 - "VideoConfig"
Cohesion: 0.12
Nodes (7): LinkFrame, MessageType, LinkFrameDecoder, LinkMessages, LinkStats, VideoConfig, LinkProtocolTests

### Community 39 - "LinkFrame"
Cohesion: 0.15
Nodes (5): bigEndianBytes(), LinkFrame, LinkStats, .fields, LinkProtocolTests

### Community 40 - "MonitorMode"
Cohesion: 0.17
Nodes (6): MonitorMode, IsConnected, WantsVideo, IVideoSwitch, WantsVideo, MonitorModeTests

### Community 41 - "LinkProtocol"
Cohesion: 0.12
Nodes (9): 3.2 iPhone (Swift), LinkDecodeError, payloadTooLarge, LinkFrameDecoder, LinkHello, LinkProtocol, LinkVideoConfig, LinkVideoFrame (+1 more)

### Community 42 - "LinkSession.cs"
Cohesion: 0.17
Nodes (5): LinkEndReason, Bye, Cancelled, Closed, Timeout

### Community 43 - "TelaDois.Core.Display"
Cohesion: 0.14
Nodes (4): TelaDois.Core.Tests.Display, TelaDois.Core.Display, KeyboardLayoutLocalTests, VirtualDisplayLocalTests

### Community 44 - "TelaDois.Core.Video"
Cohesion: 0.20
Nodes (3): TelaDois.Core.Tests.Video, TelaDois.Core.Video, FfmpegLocator

### Community 45 - "TelaDois.Core.Tests.Fakes"
Cohesion: 0.25
Nodes (3): TelaDois.Core.Tests.Fakes, TelaDois.Core.Usbmux, Loopback

### Community 46 - "DriverPipe"
Cohesion: 0.37
Nodes (3): DriverPipe, Timeout, DriverPipeTests

### Community 47 - "LinkMessageType"
Cohesion: 0.20
Nodes (10): LinkMessageType, Bye, Config, Hello, KeyframeRequest, Ping, Pong, Stats (+2 more)

### Community 50 - "EncoderProbe"
Cohesion: 0.28
Nodes (3): EncoderProbe, ProbeTimeout, EncoderProbeTests

### Community 52 - "TrayContext"
Cohesion: 0.06
Nodes (17): FileLog, TunnelStatusText, TunnelState, Connected, ServiceMissing, WaitingForApp, WaitingForIPhone, TunnelStatus (+9 more)

### Community 53 - "GatedFeed"
Cohesion: 0.20
Nodes (6): GatedFeed, Config, Disposed, FirstFrameMisses, Kind, Release

### Community 54 - "TrayContext.cs"
Cohesion: 0.17
Nodes (3): TelaDois.Core, TelaDois.Tray, OutputDesc

### Community 55 - ".ReadAll"
Cohesion: 0.15
Nodes (7): 3.1 PC (C#, `windows/src/TelaDois.Core`), 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, AccessUnitReader, H264, AccessUnitReaderTests, TrickleStream

### Community 56 - "LinkSession"
Cohesion: 0.14
Nodes (11): 1. Objetivo, 2. O driver (lido no código-fonte da release 25.7.23), 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, 4. Tratamento de erros (Parte 3, aprovada), 5. Testes, 6. Riscos, Comportamento (Parte 1, aprovada) (+3 more)

### Community 59 - "FakeDisplayApi"
Cohesion: 0.21
Nodes (8): DisplayBounds, IDisplayApi, FakeDisplayApi, Applied, BoundsList, DeviceList, ModeList, Result

### Community 60 - "RestScreen (Clock and Music pages)"
Cohesion: 0.22
Nodes (8): Etapa 2: native Modo Descanso (done), Etapa 3: Spotify (deferred), Modo Descanso (StandBy-style rest mode), Behavior tokens (60 s dim with 1.2 s fade, wake tap does not press buttons), BatteryService (UIDevice), DimController (60 s inactivity, black screen, brightness 0), RestScreen (Clock and Music pages), SpotifyService (App Remote SDK)

### Community 63 - "SlowToDisposeFeed"
Cohesion: 0.33
Nodes (5): SlowToDisposeFeed, Config, Disposals, Disposed, Kind

### Community 66 - "ToggleResult"
Cohesion: 0.40
Nodes (4): ToggleResult, NoPhone, TurnedOff, TurnedOn

### Community 67 - "HelloInfo"
Cohesion: 0.14
Nodes (7): HelloInfo, LinkSession, LinkStatus, VideoKind, Extend, Mirror, FakePhone

### Community 68 - "EndlessFeed"
Cohesion: 0.50
Nodes (3): EndlessFeed, Config, Disposed

## Knowledge Gaps
- **147 isolated node(s):** `PackageDescription`, `passThrough`, `wokeUp`, `.isVCL`, `hello` (+142 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 294 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **18 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `LinkSessionState` connect `LinkSessionState` to `LinkProtocol`, `UsbLink`, `LinkSession`, `Foundation`, `VideoRenderer`?**
  _High betweenness centrality (0.206) - this node is a cross-community bridge._
- **Are the 27 inferred relationships involving `MonitorMode` (e.g. with `3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`)` and `4. Tratamento de erros (Parte 3, aprovada)`) actually correct?**
  _`MonitorMode` has 27 INFERRED edges - model-reasoned connections that need verification._
- **What connects `PackageDescription`, `passThrough`, `wokeUp` to the rest of the system?**
  _147 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `FakeUsbmuxd` be split into smaller, more focused modules?**
  _Cohesion score 0.061122538936232734 - nodes in this community are weakly interconnected._
- **Why does `5. Testes` connect `LinkSession` to `LinkSessionState`, `HelloInfo`, `VirtualDisplay`, `MonitorMode`, `.ReadAll`, `FakeDisplayApi`?**
  _High betweenness centrality (0.205) - this node is a cross-community bridge._
- **Are the 30 inferred relationships involving `FakeProcessRunner` (e.g. with `.AlreadyCancelledTokenStartsNothing()` and `.CancellingDuringWaitDisposesRunningProcess()`) actually correct?**
  _`FakeProcessRunner` has 30 INFERRED edges - model-reasoned connections that need verification._
- **Should `Open-Meteo Weather` be split into smaller, more focused modules?**
  _Cohesion score 0.09885057471264368 - nodes in this community are weakly interconnected._