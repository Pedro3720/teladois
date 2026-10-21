# Graph Report - Aplicativo celular  (2026-10-21)

## Corpus Check
- 161 files · ~85,355 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 8 file(s) not represented in the graph (top: (none) 3, .h264 2, .css 1)

## Summary
- 1916 nodes · 5814 edges · 97 communities (70 shown, 27 thin omitted)
- Extraction: 81% EXTRACTED · 19% INFERRED · 0% AMBIGUOUS · INFERRED: 1102 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `d1bdb655`
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
- StillSamplesTests
- Design Tokens & Icon
- Clock Formatting
- Encerrar Session Routine
- UsbLink
- Prototype Package Config
- Task
- TelaDois.LatencyBench.Tests.csproj
- FakeReadStream
- Prototype Rest Mode JS
- Install & Tunnel Decisions
- project.yml (XcodeGen project definition)
- RestScreenView
- Clock Page & Battery Ring
- Design spec: iPhone second screen + Modo Descanso
- DllImport
- LinkStatsMeter
- Prototype Static Server
- EncodeSettings
- ios-test workflow (swift test + XCUITest + screenshots)
- BatteryMonitor
- Swift Package Manifest
- FakeProcessRunner
- LinkSessionState
- TimeSpan
- TelaDoisCore
- TimeSpan
- VirtualDisplayTests
- VideoConfig
- RestScreenUITests
- BenchStatsTests
- MonitorMode
- LinkProtocol
- LinkSessionOptions
- IVirtualMonitor
- TelaDois.Core.Link
- LinkProtocol.swift
- DriverPipe
- LinkMessageType
- Foundation
- XCTestCase
- EncoderProbe
- TrayContext
- SlowToDisposeFeed
- DisplayCatalog.cs
- AccessUnitReader
- LinkSession
- FakeVirtualMonitor
- Win32DisplayApi
- FakeDisplayApi
- List
- UsbTunnelService.cs
- VideoConfig
- ScheduledTaskSwitch
- Fact
- VirtualDisplay
- TunnelState
- IReadOnlyList
- MonitorMode.cs
- BenchScreen
- Arrival
- .Parse
- .Attach
- MarkerCodeTests
- SceneWindows.cs
- TimestampedStream
- .ListenAsync
- TunnelStatus
- PixelRect
- Dictionary
- Sendable
- Stream
- UsbTunnelService
- .IdealTimeIsNeverLaterThanWhenTheReaderYields
- BenchForm
- TelaDois.LatencyBench
- DisplayModeInfo
- .ReadAll
- .HandleAsync
- UsbmuxClient
- system_globalization
- Hotkey
- ScreenBrightness
- .VirtualKeyFor
- EndlessFeed

## God Nodes (most connected - your core abstractions)
1. `MonitorMode` - 65 edges
2. `FakeVirtualMonitor` - 62 edges
3. `VirtualDisplay` - 56 edges
4. `VideoFeedFactory` - 50 edges
5. `FakeProcess` - 47 edges
6. `VideoConfig` - 44 edges
7. `TelaDois.Core.Video` - 43 edges
8. `FakeProcessRunner` - 43 edges
9. `LinkSessionTests` - 43 edges
10. `IVideoFeed` - 41 edges

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

## Communities (97 total, 27 thin omitted)

### Community 0 - ".RunAsync"
Cohesion: 0.15
Nodes (19): LinkFrameReader, VideoConfig, IVideoFeed, Config, Kind, Meters, FixedFeed, Config (+11 more)

### Community 1 - "LinkMessageType"
Cohesion: 0.15
Nodes (12): .messageType, LinkMessageType, bye, config, hello, keyframeRequest, ping, pong (+4 more)

### Community 2 - "DimState"
Cohesion: 0.28
Nodes (5): DimState, TouchResult, passThrough, wokeUp, DimStateTests

### Community 3 - "CancellationToken"
Cohesion: 0.12
Nodes (3): ScheduledTaskSwitch, Timeout, AccessUnit

### Community 4 - "FakeUsbmuxd"
Cohesion: 0.24
Nodes (5): FakeUsbmuxd, Endpoint, EventsAfterListen, Requests, UsbTunnelServiceTests

### Community 6 - "Open-Meteo Weather"
Cohesion: 0.09
Nodes (8): CodingKeys, temperature, Current, MissingTemperature, OpenMeteo, Response, OpenMeteoTests, WeatherProvider

### Community 7 - "StillSamplesTests"
Cohesion: 0.12
Nodes (7): StillSamplesTests, PhaseBucket, StillSample, StillSamples, StillSchedule, StillThresholds, Reference

### Community 8 - "Design Tokens & Icon"
Cohesion: 0.13
Nodes (18): App icon check step (Assets.car, CFBundleIconName=AppIcon), Modo Descanso (StandBy-style rest mode), Design tokens (Etapa 1), Behavior tokens (60 s dim with 1.2 s fade, wake tap does not press buttons), Color tokens (black, clock #7FA9F2, ink, muted, battery, track, bars), App icon (icone-1024.png, Canva), Landscape layout (clock left, date/temp/battery right, mini player), Typography tokens (Sofia Sans Extra Condensed stretched 1.5x, rounded system font) (+10 more)

### Community 9 - "Clock Formatting"
Cohesion: 0.21
Nodes (3): ClockFormatter, ClockText, ClockFormatterTests

### Community 10 - "Encerrar Session Routine"
Cohesion: 0.13
Nodes (20): /encerrar skill (end-of-session routine), Step 3: handoff between computers, Step 4: pre-commit secret/security check, Step 6: summary for the user, Step 1: update CLAUDE.md, Step 2: verify Claude Code environment for real, pages workflow (publish prototype to GitHub Pages), prototype-test workflow (Playwright WebKit) (+12 more)

### Community 11 - "UsbLink"
Cohesion: 0.12
Nodes (4): 3.2 iPhone (Swift), .body, UsbLink, VideoRenderer

### Community 12 - "Prototype Package Config"
Cohesion: 0.12
Nodes (11): devDependencies, @playwright/test, name, private, scripts, serve, test, type (+3 more)

### Community 13 - "Task"
Cohesion: 0.22
Nodes (3): VideoFeedFactory, OutputWait, VideoFeedFactoryTests

### Community 14 - "TelaDois.LatencyBench.Tests.csproj"
Cohesion: 0.09
Nodes (18): net10.0, Microsoft.NET.Sdk, net10.0-windows, Microsoft.NET.Sdk, net10.0, coverlet.collector (6.0.4), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3) (+10 more)

### Community 15 - "FakeReadStream"
Cohesion: 0.08
Nodes (10): FailingStream, FakeReadStream, CanRead, CanSeek, CanWrite, Length, Position, GatedStream (+2 more)

### Community 16 - "Prototype Rest Mode JS"
Cohesion: 0.23
Nodes (12): armDimTimer(), changeTrack(), fetchTemp(), keepScreenOn(), renderClock(), renderMusic(), showTemp(), start() (+4 more)

### Community 17 - "Install & Tunnel Decisions"
Cohesion: 0.19
Nodes (10): TelaDois-ipa artifact (TelaDois.ipa + BUILD_INFO.txt), usbmuxd USB tunnel (127.0.0.1:7100), Install on iPhone via xtool in WSL (bash -lc, USBMUXD_SOCKET_ADDRESS), INSTALACAO.md (install TelaDois on iPhone without paid account), iTunes/iCloud web installers (not Microsoft Store) for Apple Mobile Device Support, iPhone Developer Mode and trusting the Apple ID, Sideloadly install method (alternative), WSL mirrored networking so Ubuntu reaches Windows usbmuxd 127.0.0.1:27015 (+2 more)

### Community 18 - "project.yml (XcodeGen project definition)"
Cohesion: 0.13
Nodes (13): Etapa 2: native Modo Descanso (done), Etapa 3: Spotify (deferred), Open-Meteo weather API, BatteryService (UIDevice), RestScreen (Clock and Music pages), SpotifyService (App Remote SDK), WeatherService (Open-Meteo + CoreLocation), project.yml (XcodeGen project definition) (+5 more)

### Community 19 - "RestScreenView"
Cohesion: 0.10
Nodes (6): Darwin, Foundation, AppSettings, RestScreenView, TelaDoisApp, .body

### Community 20 - "Clock Page & Battery Ring"
Cohesion: 0.11
Nodes (7): BatteryRingView, .body, .percent, ClockPageView, .body, Theme, SwiftUI

### Community 21 - "Design spec: iPhone second screen + Modo Descanso"
Cohesion: 0.13
Nodes (15): Etapa 4: USB tunnel (done, tested on iPhone), Etapa 5: Segunda Tela (next: virtual monitor + FFmpeg + video), Segunda Tela (PC second monitor over USB), Virtual Display Driver (pipe MTTVirtualDisplayPipe), AppState (rest <-> secondScreen state machine), PC <-> iPhone architecture over USB (usbmuxd), CaptureEncoder (FFmpeg ddagrab + hardware H.264, libx264 fallback), DeviceWatcher (PC usbmuxd client, attach/detach) (+7 more)

### Community 23 - "LinkStatsMeter"
Cohesion: 0.19
Nodes (4): DurationTally, .average, LinkStatsMeter, LinkStatsMeterTests

### Community 25 - "EncodeSettings"
Cohesion: 0.05
Nodes (28): 5. Observações, 1. Decisões do usuário, 2. Bancada de medição (ferramenta, não vai no TelaDois), 3. Experimentos (Intel QSV, PC do trabalho), 4. iPhone, 5. Metas e critério de pronto, 6. Fora desta etapa, Etapa 5d — menos atraso e menos engasgos na Segunda Tela — Especificação de Design (+20 more)

### Community 26 - "ios-test workflow (swift test + XCUITest + screenshots)"
Cohesion: 0.18
Nodes (7): Step 5: commit and push (never force, no merge to main), ios-build workflow (unsigned Release .ipa), ios-test workflow (swift test + XCUITest + screenshots), screenshots artifact (renamed simulator prints), UI-test launch arguments (-TDFixedDate, -TDDimSeconds, ...), install-xcodegen.sh script, pick-simulator.sh script

### Community 31 - "FakeProcessRunner"
Cohesion: 0.18
Nodes (13): FfmpegCapture, Config, Dropped, Meters, BlockingStream, IsDisposed, ReadStarted, FakeProcess (+5 more)

### Community 32 - "LinkSessionState"
Cohesion: 0.19
Nodes (4): Action, send, LinkSessionState, LinkSessionStateTests

### Community 33 - "TimeSpan"
Cohesion: 0.08
Nodes (20): 1. Objetivo, 2. Ambiente de referência, 3.1 PC (C#, `windows/src/TelaDois.Core`), 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, 4. Tratamento de erros, 5. Testes, 6. Riscos (+12 more)

### Community 34 - "TelaDoisCore"
Cohesion: 0.16
Nodes (7): AVFoundation, CoreLocation, Network, Observation, TelaDoisCore, UIKit, XCTest

### Community 36 - "VirtualDisplayTests"
Cohesion: 0.20
Nodes (8): IDeviceSwitch, VirtualDisplayTests, FakeDeviceSwitch, Accepts, Available, HoldDisable, OnRun, Runs

### Community 37 - "VideoConfig"
Cohesion: 0.09
Nodes (8): LinkFrame, MessageType, LinkFrameDecoder, HelloInfo, LinkMessages, LinkStats, LinkProtocolTests, DisplayCatalogTests

### Community 39 - "BenchStatsTests"
Cohesion: 0.12
Nodes (13): BenchStatsTests, BenchResult, IdealMedianMs, IdealP95Ms, IdealStill, Still, Suspect, BenchStats (+5 more)

### Community 40 - "MonitorMode"
Cohesion: 0.12
Nodes (10): IScreenSwitch, WantsDark, MonitorMode, IsConnected, WantsDark, WantsVideo, State, IVideoSwitch (+2 more)

### Community 41 - "LinkProtocol"
Cohesion: 0.10
Nodes (8): 3.2 iPhone (Swift), bigEndianBytes(), LinkFrame, LinkFrameDecoder, LinkProtocol, LinkVideoFrame, readBigEndian(), LinkProtocolTests

### Community 42 - "LinkSessionOptions"
Cohesion: 0.40
Nodes (5): LinkEndReason, Bye, Cancelled, Closed, Timeout

### Community 45 - "LinkProtocol.swift"
Cohesion: 0.15
Nodes (3): TelaDois.Core.Tests.Video, TelaDois.Core.Video, FfmpegLocator

### Community 46 - "DriverPipe"
Cohesion: 0.37
Nodes (3): DriverPipe, Timeout, DriverPipeTests

### Community 47 - "LinkMessageType"
Cohesion: 0.18
Nodes (11): LinkMessageType, Bye, Config, Hello, KeyframeRequest, Ping, Pong, Screen (+3 more)

### Community 50 - "EncoderProbe"
Cohesion: 0.33
Nodes (3): EncoderProbe, ProbeTimeout, EncoderProbeTests

### Community 52 - "TrayContext"
Cohesion: 0.09
Nodes (7): FileLog, Program, TrayContext, TraySettings, FilePath, Monitor, FileLogTests

### Community 53 - "SlowToDisposeFeed"
Cohesion: 0.18
Nodes (9): LinkStatus, VideoKind, Extend, Mirror, SlowToDisposeFeed, Config, Disposals, Disposed (+1 more)

### Community 54 - "DisplayCatalog.cs"
Cohesion: 0.19
Nodes (5): H264NALType, H264NALUnit, .isVCL, H264Parser, H264ParserTests

### Community 55 - "AccessUnitReader"
Cohesion: 0.15
Nodes (9): 1. Condições, 2. Tabela completa, 3. Parado: tela ociosa × com atividade, 4. Comparação com a base, 6. Decisão provisória (confirmar depois da rodada 2), 7. Rodada 2 (a rodar), Bancada da Etapa 5d — 1ª matriz completa (2026-10-21), AccessUnitReader (+1 more)

### Community 56 - "LinkSession"
Cohesion: 0.15
Nodes (12): 1. Objetivo, 2. O driver (lido no código-fonte da release 25.7.23), 3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`), 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, 5. Testes, 6. Riscos, Comportamento (Parte 1, aprovada) (+4 more)

### Community 57 - "FakeVirtualMonitor"
Cohesion: 0.09
Nodes (28): VirtualMonitorFeed, Config, Kind, Meters, FakeVirtualMonitor, Attached, AttachError, AttachTakes (+20 more)

### Community 58 - "Win32DisplayApi"
Cohesion: 0.09
Nodes (18): 4. Tratamento de erros (Parte 3, aprovada), 1. Objetivos, 2.1 Medições que sustentam a decisão (driver 25.7.23, PC Intel, 2026-10-20), 2.2 Instalador (`tools/vdd/instalar-vdd.ps1`, administrador, uma vez por PC), 2.3 TelaDois (sem administrador), 2.4 Riscos a validar no 1º passo do plano (driver real), 2. Parte A — o monitor só existe enquanto está em uso, 3.1 Protocolo (Swift e C#, bytes de referência nos dois testes) (+10 more)

### Community 59 - "FakeDisplayApi"
Cohesion: 0.15
Nodes (9): DisplayBounds, DisplayDeviceInfo, IDisplayApi, FakeDisplayApi, Applied, BoundsList, DeviceList, ModeList (+1 more)

### Community 60 - "List"
Cohesion: 0.23
Nodes (8): DisplayOutput, Left, Top, BenchOptions, DurationGiven, Seed, BenchRun, Program

### Community 62 - "VideoConfig"
Cohesion: 0.21
Nodes (4): Dispatch, MainLagProbe, VideoStats, os

### Community 63 - "ScheduledTaskSwitch"
Cohesion: 0.11
Nodes (5): TelaDois.Core.Tests.Display, TelaDois.Core.Display, IDriverPipe, LocalDriverGate, LocalDriverGateTests

### Community 64 - "Fact"
Cohesion: 0.13
Nodes (3): FileRecorder, Files, ScheduledTaskSwitchTests

### Community 65 - "VirtualDisplay"
Cohesion: 0.22
Nodes (5): VirtualDisplay, PollInterval, PowerOffWait, PowerOnWait, VirtualDisplayLocalTests

### Community 66 - "TunnelState"
Cohesion: 0.26
Nodes (3): TelaDois.Core.Tests.Fakes, TelaDois.Core.Usbmux, Loopback

### Community 67 - "IReadOnlyList"
Cohesion: 0.13
Nodes (10): ProcessStopper, IProcessRunner, IRunningProcess, Output, RecentErrors, SystemProcess, Output, RecentErrors (+2 more)

### Community 68 - "MonitorMode.cs"
Cohesion: 0.22
Nodes (8): DarkToggleResult, NoVideo, TurnedDark, TurnedLit, ToggleResult, NoPhone, TurnedOff, TurnedOn

### Community 69 - "BenchScreen"
Cohesion: 0.19
Nodes (3): BenchScreen, DwmTimingInfo, WindowRect

### Community 70 - "Arrival"
Cohesion: 0.24
Nodes (4): LatencyMatcherTests, Arrival, DrawEvent, LatencyMatcher

### Community 71 - ".Parse"
Cohesion: 0.21
Nodes (4): BenchOptionsTests, TempRepo, Deep, Root

### Community 72 - ".Attach"
Cohesion: 0.24
Nodes (3): DisplayChangeException, Code, NoPipe

### Community 74 - "SceneWindows.cs"
Cohesion: 0.13
Nodes (3): NoiseWindow, SceneWindow, ScrollWindow

### Community 75 - "TimestampedStream"
Cohesion: 0.15
Nodes (8): ReadStamp, TimestampedStream, CanRead, CanSeek, CanWrite, Length, Position, Reads

### Community 76 - ".ListenAsync"
Cohesion: 0.25
Nodes (6): DeviceAttached, DeviceDetached, ListenStarted, UsbmuxEvent, UsbmuxMessages, UsbmuxTests

### Community 77 - "TunnelStatus"
Cohesion: 0.22
Nodes (8): TunnelStatusText, TunnelState, Connected, ServiceMissing, WaitingForApp, WaitingForIPhone, TunnelStatus, TunnelStatusTextTests

### Community 78 - "PixelRect"
Cohesion: 0.26
Nodes (4): BenchLayoutTests, BenchLayout, Marker, PixelRect

### Community 80 - "Sendable"
Cohesion: 0.30
Nodes (6): LinkDecodeError, payloadTooLarge, LinkHello, LinkStats, .fields, LinkVideoConfig

### Community 81 - "Stream"
Cohesion: 0.25
Nodes (3): FakePhone, SteadyFeed, Config

### Community 82 - "UsbTunnelService"
Cohesion: 0.23
Nodes (6): LinkSessionOptions, Default, FirstFrameTimeout, StatsInterval, UsbTunnelService, Retry

### Community 83 - ".IdealTimeIsNeverLaterThanWhenTheReaderYields"
Cohesion: 0.29
Nodes (3): ChunkedStream, ReaderTimingTests, ReaderTiming

### Community 84 - "BenchForm"
Cohesion: 0.22
Nodes (5): BenchForm, CreateParams, ShowWithoutActivation, MarkerWindow, Code

### Community 89 - "UsbmuxClient"
Cohesion: 0.25
Nodes (4): UsbmuxClient, DefaultEndpoint, UsbmuxException, Code

### Community 95 - "EndlessFeed"
Cohesion: 0.50
Nodes (3): EndlessFeed, Config, Disposed

## Knowledge Gaps
- **219 isolated node(s):** `PackageDescription`, `passThrough`, `wokeUp`, `.isVCL`, `hello` (+214 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 411 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **27 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `LinkSessionState` connect `LinkSessionState` to `UsbLink`, `Sendable`, `RestScreenView`, `LinkSession`, `Win32DisplayApi`?**
  _High betweenness centrality (0.214) - this node is a cross-community bridge._
- **Are the 44 inferred relationships involving `MonitorMode` (e.g. with `3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`)` and `4. Tratamento de erros (Parte 3, aprovada)`) actually correct?**
  _`MonitorMode` has 44 INFERRED edges - model-reasoned connections that need verification._
- **What connects `PackageDescription`, `passThrough`, `wokeUp` to the rest of the system?**
  _219 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.RunAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.14689265536723164 - nodes in this community are weakly interconnected._
- **Why does `5. Testes` connect `LinkSession` to `LinkSessionState`, `VirtualDisplay`, `MonitorMode`, `AccessUnitReader`, `FakeDisplayApi`?**
  _High betweenness centrality (0.189) - this node is a cross-community bridge._
- **Are the 29 inferred relationships involving `FakeVirtualMonitor` (e.g. with `.TurningOnAgainWhileTheOldFeedDetachesKeepsTheSameVirtualMonitorOn()` and `.AttachFailureFallsBackToMirror()`) actually correct?**
  _`FakeVirtualMonitor` has 29 INFERRED edges - model-reasoned connections that need verification._
- **Should `CancellationToken` be split into smaller, more focused modules?**
  _Cohesion score 0.11746031746031746 - nodes in this community are weakly interconnected._