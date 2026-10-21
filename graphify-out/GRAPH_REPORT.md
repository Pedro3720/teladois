# Graph Report - Aplicativo celular  (2026-10-21)

## Corpus Check
- 163 files · ~93,681 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 10 file(s) not represented in the graph (top: (none) 3, .h264 3, .css 1)

## Summary
- 2040 nodes · 6273 edges · 92 communities (66 shown, 26 thin omitted)
- Extraction: 81% EXTRACTED · 19% INFERRED · 0% AMBIGUOUS · INFERRED: 1168 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `c7414d1b`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .RunAsync
- FakeUsbmuxd
- DimState
- CancellationToken
- Dictionary
- VideoLayerView
- Open-Meteo Weather
- StillSamplesTests
- Design Tokens & Icon
- Clock Formatting
- Task
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
- UsbLink.swift
- DllImport
- LinkStatsMeter
- Prototype Static Server
- EncodeSettings
- ios-test workflow (swift test + XCUITest + screenshots)
- BatteryMonitor
- Swift Package Manifest
- FfmpegCapture
- LinkSessionState
- TimeSpan
- TelaDoisCore
- .RunAsync
- FakeProcess
- LinkFrame
- RestScreenUITests
- BenchStatsTests
- MonitorMode
- LinkProtocol
- LinkEndReason
- IVirtualMonitor
- TelaDois.Core.Link
- LinkProtocol.swift
- .ListenAsync
- LinkMessageType
- Foundation
- EncoderProfile
- Theory
- TrayContext
- HelloInfo
- DisplayCatalog.cs
- TunnelStatus
- Win32DisplayApi
- FakeVirtualMonitor
- Etapa 5c — o 3º monitor some quando desligado + tela preta no iPhone — Especificação de Design
- LinkMessageType
- List
- UsbmuxClient
- VideoConfig
- ScheduledTaskSwitch
- IReadOnlyList
- IRunningProcess
- TunnelState
- EndlessFeed
- BenchStatsTests
- BenchScreen
- Arrival
- .Parse
- VirtualDisplay
- MarkerCodeTests
- SceneWindows.cs
- TimestampedStream
- TelaDois.Core.Tests.Fakes
- BenchVariantsTests
- PixelRect
- /encerrar skill (end-of-session routine)
- LinkProtocolTests
- XCTestCase
- WeatherProvider
- TelaDois.LatencyBench
- AccessUnitReaderTests
- MonitorMode.cs
- Hotkey
- UsbTunnelService

## God Nodes (most connected - your core abstractions)
1. `FakeVirtualMonitor` - 74 edges
2. `MonitorMode` - 65 edges
3. `VirtualDisplay` - 62 edges
4. `VideoFeedFactory` - 58 edges
5. `VideoFeedFactoryTests` - 49 edges
6. `FakeProcess` - 47 edges
7. `VideoConfig` - 45 edges
8. `EncodeSettings` - 45 edges
9. `VirtualDisplayTests` - 44 edges
10. `TelaDois.Core.Video` - 43 edges

## Surprising Connections (you probably didn't know these)
- `3.2 iPhone (Swift)` --references--> `H264Parser`  [INFERRED]
  docs/superpowers/specs/2026-10-19-etapa-5a-video-pelo-cabo-design.md → ios/Packages/TelaDoisCore/Sources/TelaDoisCore/H264Parser.swift
- `5. Testes` --references--> `H264Parser`  [INFERRED]
  docs/superpowers/specs/2026-10-19-etapa-5a-video-pelo-cabo-design.md → ios/Packages/TelaDoisCore/Sources/TelaDoisCore/H264Parser.swift
- `3.2 iPhone (Swift)` --references--> `LinkProtocol`  [INFERRED]
  docs/superpowers/specs/2026-10-19-etapa-5b-monitor-virtual-design.md → ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkProtocol.swift
- `5. Testes` --references--> `LinkSessionState`  [INFERRED]
  docs/superpowers/specs/2026-10-19-etapa-5b-monitor-virtual-design.md → ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkSessionState.swift
- `5. Testes` --references--> `LinkSessionState`  [INFERRED]
  docs/superpowers/specs/2026-10-20-etapa-5c-monitor-some-e-tela-preta-design.md → ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkSessionState.swift

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **GitHub Actions CI workflows gating the TelaDois branches** — _github_workflows_ios_test, _github_workflows_ios_build, _github_workflows_windows_test, _github_workflows_prototype_test, _github_workflows_pages [INFERRED 0.85]
- **PC <-> iPhone USB link: usbmuxd transport, protocol and endpoints** — docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_usbmuxd_transport, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_protocol, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_usblink, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_devicewatcher, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_streamsender, claude_rule_protocol_parity [INFERRED 0.85]
- **Unsigned .ipa delivery: CI build, inspection, xtool/Sideloadly install** — _github_workflows_ios_build, _github_workflows_ios_build_ipa_artifact, claude_rule_ipa_inspection, claude_decision_unsigned_ipa, docs_instalacao_xtool_method, docs_instalacao_sideloadly_method [INFERRED 0.85]

## Communities (92 total, 26 thin omitted)

### Community 0 - ".RunAsync"
Cohesion: 0.15
Nodes (19): LinkFrameReader, VideoConfig, IVideoFeed, Config, Kind, Meters, FixedFeed, Config (+11 more)

### Community 1 - "FakeUsbmuxd"
Cohesion: 0.29
Nodes (5): FakeUsbmuxd, Endpoint, EventsAfterListen, Requests, UsbTunnelServiceTests

### Community 2 - "DimState"
Cohesion: 0.23
Nodes (6): DimState, TouchResult, passThrough, wokeUp, DimStateTests, .body

### Community 3 - "CancellationToken"
Cohesion: 0.10
Nodes (3): AccessUnit, FailingDisposeFeed, Config

### Community 4 - "Dictionary"
Cohesion: 0.16
Nodes (4): Plist, UsbmuxPacket, Phone, PlistTests

### Community 6 - "Open-Meteo Weather"
Cohesion: 0.13
Nodes (7): CodingKeys, temperature, Current, MissingTemperature, OpenMeteo, Response, OpenMeteoTests

### Community 8 - "Design Tokens & Icon"
Cohesion: 0.13
Nodes (18): App icon check step (Assets.car, CFBundleIconName=AppIcon), Modo Descanso (StandBy-style rest mode), Design tokens (Etapa 1), Behavior tokens (60 s dim with 1.2 s fade, wake tap does not press buttons), Color tokens (black, clock #7FA9F2, ink, muted, battery, track, bars), App icon (icone-1024.png, Canva), Landscape layout (clock left, date/temp/battery right, mini player), Typography tokens (Sofia Sans Extra Condensed stretched 1.5x, rounded system font) (+10 more)

### Community 9 - "Clock Formatting"
Cohesion: 0.21
Nodes (3): ClockFormatter, ClockText, ClockFormatterTests

### Community 10 - "Task"
Cohesion: 0.24
Nodes (3): FileRecorder, Files, ScheduledTaskSwitchTests

### Community 12 - "Prototype Package Config"
Cohesion: 0.12
Nodes (11): devDependencies, @playwright/test, name, private, scripts, serve, test, type (+3 more)

### Community 13 - "Task"
Cohesion: 0.15
Nodes (3): VideoFeedFactory, OutputWait, VideoFeedFactoryTests

### Community 14 - "TelaDois.LatencyBench.Tests.csproj"
Cohesion: 0.09
Nodes (18): net10.0, Microsoft.NET.Sdk, net10.0-windows, Microsoft.NET.Sdk, net10.0, coverlet.collector (6.0.4), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3) (+10 more)

### Community 15 - "FakeReadStream"
Cohesion: 0.11
Nodes (9): FakeReadStream, CanRead, CanSeek, CanWrite, Length, Position, GatedStream, AtGate (+1 more)

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
Nodes (6): AppSettings, RestScreenView, ScreenBrightness, .screen, TelaDoisApp, .body

### Community 20 - "Clock Page & Battery Ring"
Cohesion: 0.11
Nodes (7): BatteryRingView, .body, .percent, ClockPageView, .body, Theme, SwiftUI

### Community 21 - "UsbLink.swift"
Cohesion: 0.13
Nodes (15): Etapa 4: USB tunnel (done, tested on iPhone), Etapa 5: Segunda Tela (next: virtual monitor + FFmpeg + video), Segunda Tela (PC second monitor over USB), Virtual Display Driver (pipe MTTVirtualDisplayPipe), AppState (rest <-> secondScreen state machine), PC <-> iPhone architecture over USB (usbmuxd), CaptureEncoder (FFmpeg ddagrab + hardware H.264, libx264 fallback), DeviceWatcher (PC usbmuxd client, attach/detach) (+7 more)

### Community 23 - "LinkStatsMeter"
Cohesion: 0.19
Nodes (4): DurationTally, .average, LinkStatsMeter, LinkStatsMeterTests

### Community 25 - "EncodeSettings"
Cohesion: 0.08
Nodes (15): EncodeSettings, Bitrate, Bufsize, CodecOptionOverrides, Default, ExtraCodecOptions, FilterComplex, GopSeconds (+7 more)

### Community 26 - "ios-test workflow (swift test + XCUITest + screenshots)"
Cohesion: 0.18
Nodes (7): Step 5: commit and push (never force, no merge to main), ios-build workflow (unsigned Release .ipa), ios-test workflow (swift test + XCUITest + screenshots), screenshots artifact (renamed simulator prints), UI-test launch arguments (-TDFixedDate, -TDDimSeconds, ...), install-xcodegen.sh script, pick-simulator.sh script

### Community 31 - "FfmpegCapture"
Cohesion: 0.18
Nodes (14): FfmpegCapture, Config, Dropped, Meters, BlockingStream, IsDisposed, ReadStarted, FailingStream (+6 more)

### Community 32 - "LinkSessionState"
Cohesion: 0.18
Nodes (7): 3.2 iPhone (Swift), 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, Action, send, LinkSessionState, LinkSessionStateTests

### Community 33 - "TimeSpan"
Cohesion: 0.08
Nodes (20): 1. Objetivo, 2. Ambiente de referência, 3.1 PC (C#, `windows/src/TelaDois.Core`), 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, 4. Tratamento de erros, 5. Testes, 6. Riscos (+12 more)

### Community 34 - "TelaDoisCore"
Cohesion: 0.15
Nodes (7): AVFoundation, CoreLocation, Network, Observation, TelaDoisCore, UIKit, XCTest

### Community 36 - "FakeProcess"
Cohesion: 0.33
Nodes (3): EncoderProbe, ProbeTimeout, EncoderProbeTests

### Community 37 - "LinkFrame"
Cohesion: 0.09
Nodes (9): LinkFrame, MessageType, LinkFrameDecoder, LinkMessages, LinkStats, LinkProtocolTests, SteadyFeed, Config (+1 more)

### Community 39 - "BenchStatsTests"
Cohesion: 0.15
Nodes (12): BenchResult, IdealMedianMs, IdealP95Ms, IdealStill, Still, Suspect, BenchStats, CsvHeader (+4 more)

### Community 40 - "MonitorMode"
Cohesion: 0.12
Nodes (10): IScreenSwitch, WantsDark, MonitorMode, IsConnected, WantsDark, WantsVideo, State, IVideoSwitch (+2 more)

### Community 41 - "LinkProtocol"
Cohesion: 0.11
Nodes (11): 3.2 iPhone (Swift), bigEndianBytes(), LinkDecodeError, payloadTooLarge, LinkFrame, LinkFrameDecoder, LinkHello, LinkProtocol (+3 more)

### Community 42 - "LinkEndReason"
Cohesion: 0.40
Nodes (5): LinkEndReason, Bye, Cancelled, Closed, Timeout

### Community 45 - "LinkProtocol.swift"
Cohesion: 0.16
Nodes (3): TelaDois.Core, TelaDois.Core.Tests.Video, TelaDois.Core.Video

### Community 46 - ".ListenAsync"
Cohesion: 0.21
Nodes (6): DeviceAttached, DeviceDetached, ListenStarted, UsbmuxEvent, UsbmuxMessages, UsbmuxTests

### Community 47 - "LinkMessageType"
Cohesion: 0.18
Nodes (11): LinkMessageType, Bye, Config, Hello, KeyframeRequest, Ping, Pong, Screen (+3 more)

### Community 48 - "Foundation"
Cohesion: 0.18
Nodes (4): Darwin, Foundation, close, FakePC

### Community 49 - "EncoderProfile"
Cohesion: 0.12
Nodes (13): 1. Decisões do usuário, 2. Bancada de medição (ferramenta, não vai no TelaDois), 3. Experimentos (Intel QSV, PC do trabalho), 4. iPhone, 5. Metas e critério de pronto, 6. Fora desta etapa, 7. Monitor do iPhone em 100% com resolução menor (decisão do usuário, 2026-10-21), Etapa 5d — menos atraso e menos engasgos na Segunda Tela — Especificação de Design (+5 more)

### Community 52 - "TrayContext"
Cohesion: 0.07
Nodes (10): DriverPipe, Timeout, FileLog, Program, TrayContext, TraySettings, FilePath, Monitor (+2 more)

### Community 53 - "HelloInfo"
Cohesion: 0.15
Nodes (11): HelloInfo, LinkStatus, VideoKind, Extend, Mirror, FakePhone, SlowToDisposeFeed, Config (+3 more)

### Community 54 - "DisplayCatalog.cs"
Cohesion: 0.22
Nodes (5): H264NALType, H264NALUnit, .isVCL, H264Parser, H264ParserTests

### Community 55 - "TunnelStatus"
Cohesion: 0.22
Nodes (8): TunnelStatusText, TunnelState, Connected, ServiceMissing, WaitingForApp, WaitingForIPhone, TunnelStatus, TunnelStatusTextTests

### Community 56 - "Win32DisplayApi"
Cohesion: 0.10
Nodes (15): 1. Objetivo, 2. O driver (lido no código-fonte da release 25.7.23), 4. Tratamento de erros (Parte 3, aprovada), 6. Riscos, Comportamento (Parte 1, aprovada), Etapa 5b — Monitor estendido (monitor virtual) + atalho — Especificação de Design, Fora do escopo da 5b, Instalação (uma vez por PC, com administrador) (+7 more)

### Community 57 - "FakeVirtualMonitor"
Cohesion: 0.08
Nodes (30): VirtualMonitorFeed, Config, Kind, Meters, FakeVirtualMonitor, Attached, AttachError, AttachRequested (+22 more)

### Community 58 - "Etapa 5c — o 3º monitor some quando desligado + tela preta no iPhone — Especificação de Design"
Cohesion: 0.15
Nodes (12): 1. Objetivos, 2.1 Medições que sustentam a decisão (driver 25.7.23, PC Intel, 2026-10-20), 2.2 Instalador (`tools/vdd/instalar-vdd.ps1`, administrador, uma vez por PC), 2.3 TelaDois (sem administrador), 2.4 Riscos a validar no 1º passo do plano (driver real), 2. Parte A — o monitor só existe enquanto está em uso, 3.1 Protocolo (Swift e C#, bytes de referência nos dois testes), 3.2 PC (+4 more)

### Community 59 - "LinkMessageType"
Cohesion: 0.17
Nodes (12): .messageType, LinkMessageType, bye, config, hello, keyframeRequest, ping, pong (+4 more)

### Community 60 - "List"
Cohesion: 0.19
Nodes (9): DisplayOutput, Left, Top, BenchOptions, DurationGiven, MonitorSize, Seed, BenchRun (+1 more)

### Community 61 - "UsbmuxClient"
Cohesion: 0.20
Nodes (4): UsbmuxClient, DefaultEndpoint, UsbmuxException, Code

### Community 62 - "VideoConfig"
Cohesion: 0.13
Nodes (5): Dispatch, VideoRenderer, MainLagProbe, VideoStats, os

### Community 63 - "ScheduledTaskSwitch"
Cohesion: 0.11
Nodes (5): TelaDois.Core.Tests.Display, TelaDois.Tray, TelaDois.Core.Display, LocalDriverGate, LocalDriverGateTests

### Community 64 - "IReadOnlyList"
Cohesion: 0.23
Nodes (7): BenchScenarios, All, PhaseBucket, StillSample, StillSamples, StillThresholds, Reference

### Community 65 - "IRunningProcess"
Cohesion: 0.07
Nodes (14): IDeviceSwitch, ScheduledTaskSwitch, Timeout, ProcessStopper, FfmpegLocator, IProcessRunner, IRunningProcess, Output (+6 more)

### Community 67 - "EndlessFeed"
Cohesion: 0.50
Nodes (3): EndlessFeed, Config, Disposed

### Community 69 - "BenchScreen"
Cohesion: 0.18
Nodes (3): BenchScreen, DwmTimingInfo, WindowRect

### Community 70 - "Arrival"
Cohesion: 0.25
Nodes (4): LatencyMatcherTests, Arrival, DrawEvent, LatencyMatcher

### Community 71 - ".Parse"
Cohesion: 0.19
Nodes (4): BenchOptionsTests, TempRepo, Deep, Root

### Community 72 - "VirtualDisplay"
Cohesion: 0.05
Nodes (33): 3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`), 5. Testes, IDriverPipe, DisplayBounds, DisplayChangeException, Code, DisplayDeviceInfo, DisplayModeInfo (+25 more)

### Community 74 - "SceneWindows.cs"
Cohesion: 0.09
Nodes (8): BenchForm, CreateParams, ShowWithoutActivation, MarkerWindow, Code, NoiseWindow, SceneWindow, ScrollWindow

### Community 75 - "TimestampedStream"
Cohesion: 0.07
Nodes (23): 1. Condições, 2. Tabela completa, 3. Parado: tela ociosa × com atividade, 4. Comparação com a base, 5. Observações, 6. Decisão provisória (confirmar depois da rodada 2), 7. Rodada 2 (preparação; resultados na §8), 8. Rodada 2 — resultados (2026-10-21, 17:46, PC do trabalho parado) (+15 more)

### Community 77 - "BenchVariantsTests"
Cohesion: 0.16
Nodes (4): BenchVariantsTests, BenchVariant, BenchVariants, All

### Community 78 - "PixelRect"
Cohesion: 0.30
Nodes (4): BenchLayoutTests, BenchLayout, Marker, PixelRect

### Community 79 - "/encerrar skill (end-of-session routine)"
Cohesion: 0.13
Nodes (20): /encerrar skill (end-of-session routine), Step 3: handoff between computers, Step 4: pre-commit secret/security check, Step 6: summary for the user, Step 1: update CLAUDE.md, Step 2: verify Claude Code environment for real, pages workflow (publish prototype to GitHub Pages), prototype-test workflow (Playwright WebKit) (+12 more)

### Community 80 - "LinkProtocolTests"
Cohesion: 0.16
Nodes (3): LinkStats, .fields, LinkProtocolTests

### Community 87 - "AccessUnitReaderTests"
Cohesion: 0.11
Nodes (9): AccessUnitReaderTests, FeedStream, CanRead, CanSeek, CanWrite, Length, Position, RandomChunkStream (+1 more)

### Community 88 - "MonitorMode.cs"
Cohesion: 0.22
Nodes (8): DarkToggleResult, NoVideo, TurnedDark, TurnedLit, ToggleResult, NoPhone, TurnedOff, TurnedOn

### Community 91 - "Hotkey"
Cohesion: 0.18
Nodes (4): KeyboardLayout, Hotkey, Registered, KeyboardLayoutLocalTests

### Community 94 - "UsbTunnelService"
Cohesion: 0.23
Nodes (6): LinkSessionOptions, Default, FirstFrameTimeout, StatsInterval, UsbTunnelService, Retry

## Knowledge Gaps
- **237 isolated node(s):** `PackageDescription`, `passThrough`, `wokeUp`, `.isVCL`, `hello` (+232 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 435 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **26 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `LinkSessionState` connect `LinkSessionState` to `VirtualDisplay`, `UsbLink`, `Foundation`, `Win32DisplayApi`, `VideoConfig`?**
  _High betweenness centrality (0.221) - this node is a cross-community bridge._
- **Are the 33 inferred relationships involving `FakeVirtualMonitor` (e.g. with `.TurningOnAgainWhileTheOldFeedDetachesKeepsTheSameVirtualMonitorOn()` and `.AttachFailureFallsBackToMirror()`) actually correct?**
  _`FakeVirtualMonitor` has 33 INFERRED edges - model-reasoned connections that need verification._
- **What connects `PackageDescription`, `passThrough`, `wokeUp` to the rest of the system?**
  _237 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.RunAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.14754098360655737 - nodes in this community are weakly interconnected._
- **Why does `5. Testes` connect `VirtualDisplay` to `LinkSessionState`, `Win32DisplayApi`, `MonitorMode`, `TimestampedStream`?**
  _High betweenness centrality (0.196) - this node is a cross-community bridge._
- **Are the 44 inferred relationships involving `MonitorMode` (e.g. with `3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`)` and `4. Tratamento de erros (Parte 3, aprovada)`) actually correct?**
  _`MonitorMode` has 44 INFERRED edges - model-reasoned connections that need verification._
- **Should `CancellationToken` be split into smaller, more focused modules?**
  _Cohesion score 0.0975609756097561 - nodes in this community are weakly interconnected._