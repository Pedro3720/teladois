# Graph Report - TelaDois  (2026-10-30)

## Corpus Check
- 178 files · ~101,500 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 12 file(s) not represented in the graph (top: (none) 3, .h264 3, .iss 1)

## Summary
- 2318 nodes · 6575 edges · 128 communities (96 shown, 32 thin omitted)
- Extraction: 82% EXTRACTED · 18% INFERRED · 0% AMBIGUOUS · INFERRED: 1168 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `4b0f8929`
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
- .Create
- UsbLink
- Prototype Package Config
- Task
- TelaDois.LatencyBench.Tests.csproj
- FakeReadStream
- Prototype Rest Mode JS
- Install & Tunnel Decisions
- Foundation
- RestScreenView
- Clock Page & Battery Ring
- PC<->iPhone protocol (HELLO, CONFIG, VIDEO, KEYFRAME_REQ, PING/PONG, BYE)
- DllImport
- LinkStatsMeter
- Prototype Static Server
- EncodeSettings
- ios-test workflow (swift test + XCUITest + screenshots)
- BatteryMonitor
- Swift Package Manifest
- FakeProcess
- LinkSessionState
- TimeSpan
- TelaDoisCore
- TelaDois
- EncoderProbe
- Fact
- RestScreenUITests
- BenchResult
- MonitorMode
- LinkFrame
- LinkEndReason
- IVirtualMonitor
- TelaDois.Core.Link
- TelaDois.Core.Video
- .ListenAsync
- LinkMessageType
- FakePC
- EncoderProfile
- Theory
- TrayContext
- HelloInfo
- H264NALUnit
- TunnelStatus
- Win32DisplayApi
- FakeVirtualMonitor
- Etapa 5c — o 3º monitor some quando desligado + tela preta no iPhone — Especificação de Design
- LinkMessageType
- List
- UsbTunnelService
- Sendable
- TelaDois.Core.Display
- IReadOnlyList
- IRunningProcess
- UsbTunnelService.cs
- ValueTask
- BenchStatsTests
- BenchScreen
- Arrival
- .Parse
- VirtualDisplayTests
- MarkerCodeTests
- SceneWindows.cs
- AccessUnitReader
- steps
- InlineData
- PixelRect
- CLAUDE.md (TelaDois project guide)
- LinkStats
- XCTestCase
- WeatherProvider
- ios-build.yml
- TelaDois.LatencyBench
- prototype-test.yml
- AccessUnitReaderTests
- MonitorMode.cs
- VirtualDisplay
- Hotkey
- steps
- Etapa 6a — Instalador do Windows (driver + app) — Especificação de Design
- LinkSessionOptions
- FakeDisplayApi
- windows-test.yml
- steps
- BenchForm
- .Attach
- DriverPipe
- FeedStream
- 3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`)
- project.yml
- VirtualMonitorFeed
- Stream
- TimestampedStream
- base
- environment
- Etapa 5d — menos atraso e menos engasgos na Segunda Tela — Especificação de Design
- main
- IScreenSwitch
- IVideoSwitch
- .Start
- Bancada da Etapa 5d — 1ª matriz completa (2026-10-21)
- steps
- 3. Mudanças no código
- TelaDois
- 2. Parte A — o monitor só existe enquanto está em uso
- .RunAsync
- verificar-apple.ps1
- NOTAS-DA-VERSAO.md

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
- `Rotate hint overlay (portrait message)` --semantically_similar_to--> `Landscape-only, full-screen, hidden status bar, iPhone-only settings`  [INFERRED] [semantically similar]
  prototype/index.html → ios/project.yml
- `2. Tabela completa` --references--> `AccessUnitReader`  [INFERRED]
  docs/medicoes/2026-10-21-bancada-5d.md → windows/src/TelaDois.Core/Video/AccessUnitReader.cs
- `Decisão (fecha a §6)` --references--> `AccessUnitReader`  [INFERRED]
  docs/medicoes/2026-10-21-bancada-5d.md → windows/src/TelaDois.Core/Video/AccessUnitReader.cs
- `Etapa 5b — Monitor estendido (monitor virtual) + atalho — Especificação de Design` --references--> `main()`  [INFERRED]
  docs/superpowers/specs/2026-10-19-etapa-5b-monitor-virtual-design.md → installer/gerar-imagens.py
- `Etapa 5c — o 3º monitor some quando desligado + tela preta no iPhone — Especificação de Design` --references--> `main()`  [INFERRED]
  docs/superpowers/specs/2026-10-20-etapa-5c-monitor-some-e-tela-preta-design.md → installer/gerar-imagens.py

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **GitHub Actions CI workflows gating the TelaDois branches** — _github_workflows_ios_test, _github_workflows_ios_build, _github_workflows_windows_test, _github_workflows_prototype_test, _github_workflows_pages [INFERRED 0.85]
- **PC <-> iPhone USB link: usbmuxd transport, protocol and endpoints** — docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_usbmuxd_transport, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_protocol, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_usblink, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_devicewatcher, docs_superpowers_specs_2026_10_16_iphone_segunda_tela_descanso_design_streamsender, claude_rule_protocol_parity [INFERRED 0.85]
- **Unsigned .ipa delivery: CI build, inspection, xtool/Sideloadly install** — _github_workflows_ios_build, _github_workflows_ios_build_ipa_artifact, claude_rule_ipa_inspection, claude_decision_unsigned_ipa, docs_instalacao_xtool_method, docs_instalacao_sideloadly_method [INFERRED 0.85]

## Communities (128 total, 32 thin omitted)

### Community 0 - ".RunAsync"
Cohesion: 0.14
Nodes (22): LinkFrameReader, VideoConfig, IVideoFeed, Config, Kind, Meters, EndlessFeed, Config (+14 more)

### Community 1 - "FakeUsbmuxd"
Cohesion: 0.18
Nodes (6): FakeUsbmuxd, Endpoint, EventsAfterListen, Phone, Requests, UsbTunnelServiceTests

### Community 2 - "DimState"
Cohesion: 0.28
Nodes (5): DimState, TouchResult, passThrough, wokeUp, DimStateTests

### Community 4 - "Dictionary"
Cohesion: 0.19
Nodes (3): Plist, UsbmuxPacket, PlistTests

### Community 6 - "Open-Meteo Weather"
Cohesion: 0.13
Nodes (7): CodingKeys, temperature, Current, MissingTemperature, OpenMeteo, Response, OpenMeteoTests

### Community 7 - "StillSamplesTests"
Cohesion: 0.13
Nodes (7): StillSamplesTests, PhaseBucket, StillSample, StillSamples, StillSchedule, StillThresholds, Reference

### Community 8 - "Design Tokens & Icon"
Cohesion: 0.18
Nodes (12): App icon check step (Assets.car, CFBundleIconName=AppIcon), Design tokens (Etapa 1), Color tokens (black, clock #7FA9F2, ink, muted, battery, track, bars), App icon (icone-1024.png, Canva), Landscape layout (clock left, date/temp/battery right, mini player), Typography tokens (Sofia Sans Extra Condensed stretched 1.5x, rounded system font), Landscape-only, full-screen, hidden status bar, iPhone-only settings, Battery ring SVG (battery-level circle) (+4 more)

### Community 9 - "Clock Formatting"
Cohesion: 0.21
Nodes (3): ClockFormatter, ClockText, ClockFormatterTests

### Community 10 - ".Create"
Cohesion: 0.17
Nodes (5): ScheduledTaskSwitch, Timeout, FileRecorder, Files, ScheduledTaskSwitchTests

### Community 12 - "Prototype Package Config"
Cohesion: 0.13
Nodes (11): devDependencies, @playwright/test@1.64.1, name, private, scripts, serve, test, type (+3 more)

### Community 13 - "Task"
Cohesion: 0.15
Nodes (3): VideoFeedFactory, OutputWait, VideoFeedFactoryTests

### Community 14 - "TelaDois.LatencyBench.Tests.csproj"
Cohesion: 0.09
Nodes (18): net10.0, Microsoft.NET.Sdk, net10.0-windows, Microsoft.NET.Sdk, net10.0, coverlet.collector (6.0.4), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3) (+10 more)

### Community 15 - "FakeReadStream"
Cohesion: 0.14
Nodes (10): FailingStream, FakeReadStream, CanRead, CanSeek, CanWrite, Length, Position, GatedStream (+2 more)

### Community 16 - "Prototype Rest Mode JS"
Cohesion: 0.23
Nodes (12): armDimTimer(), changeTrack(), fetchTemp(), keepScreenOn(), renderClock(), renderMusic(), showTemp(), start() (+4 more)

### Community 17 - "Install & Tunnel Decisions"
Cohesion: 0.19
Nodes (10): TelaDois-ipa artifact (TelaDois.ipa + BUILD_INFO.txt), usbmuxd USB tunnel (127.0.0.1:7100), Install on iPhone via xtool in WSL (bash -lc, USBMUXD_SOCKET_ADDRESS), INSTALACAO.md (install TelaDois on iPhone without paid account), iTunes/iCloud web installers (not Microsoft Store) for Apple Mobile Device Support, iPhone Developer Mode and trusting the Apple ID, Sideloadly install method (alternative), WSL mirrored networking so Ubuntu reaches Windows usbmuxd 127.0.0.1:27015 (+2 more)

### Community 18 - "Foundation"
Cohesion: 0.15
Nodes (12): Etapa 2: native Modo Descanso (done), Etapa 3: Spotify (deferred), Modo Descanso (StandBy-style rest mode), Open-Meteo weather API, Behavior tokens (60 s dim with 1.2 s fade, wake tap does not press buttons), BatteryService (UIDevice), DimController (60 s inactivity, black screen, brightness 0), RestScreen (Clock and Music pages) (+4 more)

### Community 19 - "RestScreenView"
Cohesion: 0.11
Nodes (7): AppSettings, RestScreenView, .body, ScreenBrightness, .screen, TelaDoisApp, .body

### Community 20 - "Clock Page & Battery Ring"
Cohesion: 0.11
Nodes (7): BatteryRingView, .body, .percent, ClockPageView, .body, Theme, SwiftUI

### Community 21 - "PC<->iPhone protocol (HELLO, CONFIG, VIDEO, KEYFRAME_REQ, PING/PONG, BYE)"
Cohesion: 0.13
Nodes (15): Etapa 4: USB tunnel (done, tested on iPhone), Etapa 5: Segunda Tela (next: virtual monitor + FFmpeg + video), Segunda Tela (PC second monitor over USB), Virtual Display Driver (pipe MTTVirtualDisplayPipe), AppState (rest <-> secondScreen state machine), PC <-> iPhone architecture over USB (usbmuxd), CaptureEncoder (FFmpeg ddagrab + hardware H.264, libx264 fallback), DeviceWatcher (PC usbmuxd client, attach/detach) (+7 more)

### Community 22 - "DllImport"
Cohesion: 0.18
Nodes (3): KeyboardLayout, CreateDXGIFactory1(), OutputDesc

### Community 23 - "LinkStatsMeter"
Cohesion: 0.19
Nodes (4): DurationTally, .average, LinkStatsMeter, LinkStatsMeterTests

### Community 25 - "EncodeSettings"
Cohesion: 0.07
Nodes (16): EncodeSettings, Bitrate, Bufsize, CodecOptionOverrides, Default, ExtraCodecOptions, FilterComplex, GopSeconds (+8 more)

### Community 26 - "ios-test workflow (swift test + XCUITest + screenshots)"
Cohesion: 0.13
Nodes (7): Step 5: commit and push (never force, no merge to main), ios-build workflow (unsigned Release .ipa), ios-test workflow (swift test + XCUITest + screenshots), screenshots artifact (renamed simulator prints), UI-test launch arguments (-TDFixedDate, -TDDimSeconds, ...), install-xcodegen.sh script, pick-simulator.sh script

### Community 31 - "FakeProcess"
Cohesion: 0.16
Nodes (9): FfmpegCapture, Config, Dropped, Meters, ProcessStopper, BlockingStream, IsDisposed, ReadStarted (+1 more)

### Community 32 - "LinkSessionState"
Cohesion: 0.18
Nodes (4): Action, send, LinkSessionState, LinkSessionStateTests

### Community 33 - "TimeSpan"
Cohesion: 0.08
Nodes (20): 1. Objetivo, 2. Ambiente de referência, 3.1 PC (C#, `windows/src/TelaDois.Core`), 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, 4. Tratamento de erros, 5. Testes, 6. Riscos (+12 more)

### Community 34 - "TelaDoisCore"
Cohesion: 0.15
Nodes (7): AVFoundation, CoreLocation, Network, Observation, TelaDoisCore, UIKit, XCTest

### Community 35 - "TelaDois"
Cohesion: 0.12
Nodes (18): package, target, buildPhase, excludes, path, targets, TelaDois, TelaDoisUITests (+10 more)

### Community 36 - "EncoderProbe"
Cohesion: 0.27
Nodes (8): EncoderProbe, ProbeTimeout, FakeProcess, Output, RecentErrors, FakeProcessRunner, Started, EncoderProbeTests

### Community 37 - "Fact"
Cohesion: 0.10
Nodes (7): LinkFrame, MessageType, LinkFrameDecoder, LinkMessages, LinkStats, LinkProtocolTests, DisplayCatalogTests

### Community 39 - "BenchResult"
Cohesion: 0.15
Nodes (12): BenchResult, IdealMedianMs, IdealP95Ms, IdealStill, Still, Suspect, BenchStats, CsvHeader (+4 more)

### Community 40 - "MonitorMode"
Cohesion: 0.16
Nodes (6): MonitorMode, IsConnected, WantsDark, WantsVideo, State, MonitorModeTests

### Community 41 - "LinkFrame"
Cohesion: 0.11
Nodes (7): 3.2 iPhone (Swift), LinkFrame, LinkFrameDecoder, LinkProtocol, LinkVideoFrame, readBigEndian(), LinkProtocolTests

### Community 42 - "LinkEndReason"
Cohesion: 0.40
Nodes (5): LinkEndReason, Bye, Cancelled, Closed, Timeout

### Community 43 - "IVirtualMonitor"
Cohesion: 0.13
Nodes (5): IVirtualMonitor, DisplayOutput, Left, Top, OrderedMonitor

### Community 46 - ".ListenAsync"
Cohesion: 0.21
Nodes (6): DeviceAttached, DeviceDetached, ListenStarted, UsbmuxEvent, UsbmuxMessages, UsbmuxTests

### Community 47 - "LinkMessageType"
Cohesion: 0.18
Nodes (11): LinkMessageType, Bye, Config, Hello, KeyframeRequest, Ping, Pong, Screen (+3 more)

### Community 48 - "FakePC"
Cohesion: 0.19
Nodes (3): Darwin, Foundation, FakePC

### Community 49 - "EncoderProfile"
Cohesion: 0.40
Nodes (5): 3. Experimentos (Intel QSV, PC do trabalho), EncoderProfile, CropsToEven, EncoderProfiles, Candidates

### Community 52 - "TrayContext"
Cohesion: 0.11
Nodes (6): FileLog, TrayContext, TraySettings, FilePath, Monitor, FileLogTests

### Community 53 - "HelloInfo"
Cohesion: 0.16
Nodes (10): HelloInfo, LinkStatus, VideoKind, Extend, Mirror, SlowToDisposeFeed, Config, Disposals (+2 more)

### Community 54 - "H264NALUnit"
Cohesion: 0.20
Nodes (6): H264NALType, H264NALUnit, .isVCL, H264Parser, bigEndianBytes(), H264ParserTests

### Community 55 - "TunnelStatus"
Cohesion: 0.22
Nodes (8): TunnelStatusText, TunnelState, Connected, ServiceMissing, WaitingForApp, WaitingForIPhone, TunnelStatus, TunnelStatusTextTests

### Community 56 - "Win32DisplayApi"
Cohesion: 0.14
Nodes (7): DevMode, DisplayDevice, MonitorEnumProc, MonitorInfoEx, Rect, Win32DisplayApi, Persist

### Community 57 - "FakeVirtualMonitor"
Cohesion: 0.08
Nodes (22): FakeVirtualMonitor, Attached, AttachError, AttachRequested, AttachTakes, AvailableModes, Calls, CanPowerOff (+14 more)

### Community 58 - "Etapa 5c — o 3º monitor some quando desligado + tela preta no iPhone — Especificação de Design"
Cohesion: 0.22
Nodes (8): 1. Objetivos, 3.1 Protocolo (Swift e C#, bytes de referência nos dois testes), 3.2 PC, 3.3 iPhone, 3. Parte B — tela preta no iPhone, 4. Erros, 5. Testes, Etapa 5c — o 3º monitor some quando desligado + tela preta no iPhone — Especificação de Design

### Community 59 - "LinkMessageType"
Cohesion: 0.17
Nodes (12): .messageType, LinkMessageType, bye, config, hello, keyframeRequest, ping, pong (+4 more)

### Community 60 - "List"
Cohesion: 0.21
Nodes (6): BenchOptions, DurationGiven, MonitorSize, Seed, BenchRun, Program

### Community 61 - "UsbTunnelService"
Cohesion: 0.21
Nodes (6): UsbmuxClient, DefaultEndpoint, UsbmuxException, Code, UsbTunnelService, Retry

### Community 62 - "Sendable"
Cohesion: 0.12
Nodes (6): Dispatch, 3.2 iPhone (Swift), VideoRenderer, MainLagProbe, VideoStats, os

### Community 63 - "TelaDois.Core.Display"
Cohesion: 0.14
Nodes (4): TelaDois.Core.Tests.Display, TelaDois.Core.Tests.Fakes, TelaDois.Core.Display, KeyboardLayoutLocalTests

### Community 65 - "IRunningProcess"
Cohesion: 0.36
Nodes (4): IProcessRunner, IRunningProcess, Output, RecentErrors

### Community 66 - "UsbTunnelService.cs"
Cohesion: 0.24
Nodes (3): TelaDois.Core, TelaDois.Core.Usbmux, Loopback

### Community 69 - "BenchScreen"
Cohesion: 0.20
Nodes (3): BenchScreen, DwmTimingInfo, WindowRect

### Community 70 - "Arrival"
Cohesion: 0.24
Nodes (4): LatencyMatcherTests, Arrival, DrawEvent, LatencyMatcher

### Community 71 - ".Parse"
Cohesion: 0.19
Nodes (4): BenchOptionsTests, TempRepo, Deep, Root

### Community 72 - "VirtualDisplayTests"
Cohesion: 0.20
Nodes (8): IDeviceSwitch, VirtualDisplayTests, FakeDeviceSwitch, Accepts, Available, HoldDisable, OnRun, Runs

### Community 74 - "SceneWindows.cs"
Cohesion: 0.14
Nodes (3): NoiseWindow, SceneWindow, ScrollWindow

### Community 75 - "AccessUnitReader"
Cohesion: 0.16
Nodes (4): AccessUnitReader, H264, ReaderTimingTests, ReaderTiming

### Community 76 - "steps"
Cohesion: 0.07
Nodes (28): concurrency, cancel-in-progress, group, env, DEVELOPER_DIR, jobs, test, name (+20 more)

### Community 77 - "InlineData"
Cohesion: 0.15
Nodes (5): 5. Observações, BenchVariantsTests, BenchVariant, BenchVariants, All

### Community 78 - "PixelRect"
Cohesion: 0.26
Nodes (4): BenchLayoutTests, BenchLayout, Marker, PixelRect

### Community 79 - "CLAUDE.md (TelaDois project guide)"
Cohesion: 0.12
Nodes (22): /encerrar skill (end-of-session routine), Step 3: handoff between computers, Step 4: pre-commit secret/security check, Step 6: summary for the user, Step 1: update CLAUDE.md, Step 2: verify Claude Code environment for real, pages workflow (publish prototype to GitHub Pages), prototype-test workflow (Playwright WebKit) (+14 more)

### Community 80 - "LinkStats"
Cohesion: 0.21
Nodes (6): LinkDecodeError, payloadTooLarge, LinkHello, LinkStats, .fields, LinkVideoConfig

### Community 84 - "ios-build.yml"
Cohesion: 0.07
Nodes (26): runs-on, steps, timeout-minutes, concurrency, cancel-in-progress, group, env, DEVELOPER_DIR (+18 more)

### Community 85 - "TelaDois.LatencyBench"
Cohesion: 0.18
Nodes (4): TelaDois.LatencyBench, TelaDois.LatencyBench.Tests, BenchScenarios, All

### Community 86 - "prototype-test.yml"
Cohesion: 0.08
Nodes (25): concurrency, cancel-in-progress, group, run, jobs, test, name, on (+17 more)

### Community 88 - "MonitorMode.cs"
Cohesion: 0.22
Nodes (8): DarkToggleResult, NoVideo, TurnedDark, TurnedLit, ToggleResult, NoPhone, TurnedOff, TurnedOn

### Community 89 - "VirtualDisplay"
Cohesion: 0.18
Nodes (7): 2. Bancada de medição (ferramenta, não vai no TelaDois), VirtualDisplay, PollInterval, PowerOffWait, PowerOnWait, SystemProcessRunner, VirtualDisplayLocalTests

### Community 91 - "Hotkey"
Cohesion: 0.11
Nodes (5): TelaDois.Tray, AppIcon, Hotkey, Registered, Program

### Community 92 - "steps"
Cohesion: 0.13
Nodes (14): concurrency, cancel-in-progress, group, jobs, name, on, push, workflow_dispatch (+6 more)

### Community 93 - "Etapa 6a — Instalador do Windows (driver + app) — Especificação de Design"
Cohesion: 0.18
Nodes (9): 1. Objetivo, 2. Peças, 4. Experiência (revisão apple-design), 5. Tratamento de erros, 6. Testes e verificação, 7. Riscos, Etapa 6a — Instalador do Windows (driver + app) — Especificação de Design, Fora do escopo (+1 more)

### Community 94 - "LinkSessionOptions"
Cohesion: 0.50
Nodes (4): LinkSessionOptions, Default, FirstFrameTimeout, StatsInterval

### Community 95 - "FakeDisplayApi"
Cohesion: 0.12
Nodes (13): DisplayBounds, DisplayDeviceInfo, IDisplayApi, FakeDisplayApi, Applied, BoundsList, DeviceList, DpiAsked (+5 more)

### Community 97 - "windows-test.yml"
Cohesion: 0.09
Nodes (21): concurrency, cancel-in-progress, group, jobs, test, name, on, push (+13 more)

### Community 98 - "steps"
Cohesion: 0.06
Nodes (38): concurrency, cancel-in-progress, group, GH_TOKEN, outputs, runs-on, steps, timeout-minutes (+30 more)

### Community 99 - "BenchForm"
Cohesion: 0.15
Nodes (5): BenchForm, CreateParams, ShowWithoutActivation, MarkerWindow, Code

### Community 100 - ".Attach"
Cohesion: 0.18
Nodes (4): IDriverPipe, DisplayChangeException, Code, NoPipe

### Community 101 - "DriverPipe"
Cohesion: 0.37
Nodes (3): DriverPipe, Timeout, DriverPipeTests

### Community 102 - "FeedStream"
Cohesion: 0.12
Nodes (6): FeedStream, CanRead, CanSeek, CanWrite, Length, Position

### Community 103 - "3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`)"
Cohesion: 0.12
Nodes (13): 1. Objetivo, 2. O driver (lido no código-fonte da release 25.7.23), 3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`), 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, 4. Tratamento de erros (Parte 3, aprovada), 5. Testes, 6. Riscos (+5 more)

### Community 104 - "project.yml"
Cohesion: 0.15
Nodes (14): TelaDoisCore local package dependency, iOS, name, options, bundleIdPrefix, createIntermediateGroups, deploymentTarget, packages (+6 more)

### Community 105 - "VirtualMonitorFeed"
Cohesion: 0.22
Nodes (10): VirtualMonitorFeed, Config, Kind, Meters, FailingDisposeFeed, Config, VirtualMonitorFeedTests, WaitingFeed (+2 more)

### Community 106 - "Stream"
Cohesion: 0.28
Nodes (3): FakePhone, SteadyFeed, Config

### Community 107 - "TimestampedStream"
Cohesion: 0.14
Nodes (8): ReadStamp, TimestampedStream, CanRead, CanSeek, CanWrite, Length, Position, Reads

### Community 108 - "base"
Cohesion: 0.12
Nodes (16): ASSETCATALOG_COMPILER_APPICON_NAME, CURRENT_PROJECT_VERSION, GENERATE_INFOPLIST_FILE, INFOPLIST_KEY_CFBundleDisplayName, INFOPLIST_KEY_NSLocationWhenInUseUsageDescription, INFOPLIST_KEY_UILaunchScreen_Generation, INFOPLIST_KEY_UIRequiresFullScreen, INFOPLIST_KEY_UIStatusBarHidden (+8 more)

### Community 109 - "environment"
Cohesion: 0.16
Nodes (5): environment, name, url, LocalDriverGate, LocalDriverGateTests

### Community 110 - "Etapa 5d — menos atraso e menos engasgos na Segunda Tela — Especificação de Design"
Cohesion: 0.29
Nodes (6): 1. Decisões do usuário, 4. iPhone, 5. Metas e critério de pronto, 6. Fora desta etapa, 7. Monitor do iPhone em 100% com resolução menor (decisão do usuário, 2026-10-21), Etapa 5d — menos atraso e menos engasgos na Segunda Tela — Especificação de Design

### Community 111 - "main"
Cohesion: 0.23
Nodes (6): 3.6 GitHub, artwork(), fit(), main(), tile(), wizard()

### Community 114 - ".Start"
Cohesion: 0.23
Nodes (4): SystemProcess, Output, RecentErrors, SystemProcessRunnerTests

### Community 115 - "Bancada da Etapa 5d — 1ª matriz completa (2026-10-21)"
Cohesion: 0.20
Nodes (9): 1. Condições, 2. Tabela completa, 3. Parado: tela ociosa × com atividade, 4. Comparação com a base, 6. Decisão provisória (confirmar depois da rodada 2), 7. Rodada 2 (preparação; resultados na §8), 8. Rodada 2 — resultados (2026-10-21, 17:46, PC do trabalho parado), Bancada da Etapa 5d — 1ª matriz completa (2026-10-21) (+1 more)

### Community 116 - "steps"
Cohesion: 0.20
Nodes (10): runs-on, steps, timeout-minutes, deploy, id, name, run, uses (+2 more)

### Community 118 - "3. Mudanças no código"
Cohesion: 0.33
Nodes (6): 3.1 App do PC (C#), 3.2 Driver (`tools/vdd/`), 3.3 Apple (`tools/instalador/verificar-apple.ps1`, novo), 3.4 Instalador (`installer/TelaDois.iss`, Inno Setup 6), 3.5 Montagem (`installer/montar.ps1`, novo), 3. Mudanças no código

### Community 120 - "TelaDois"
Cohesion: 0.33
Nodes (6): targets, schemes, TelaDois, build, test, targets

### Community 121 - "2. Parte A — o monitor só existe enquanto está em uso"
Cohesion: 0.40
Nodes (5): 2.1 Medições que sustentam a decisão (driver 25.7.23, PC Intel, 2026-10-20), 2.2 Instalador (`tools/vdd/instalar-vdd.ps1`, administrador, uma vez por PC), 2.3 TelaDois (sem administrador), 2.4 Riscos a validar no 1º passo do plano (driver real), 2. Parte A — o monitor só existe enquanto está em uso

### Community 123 - "verificar-apple.ps1"
Cohesion: 0.83
Nodes (3): Find-CabOffset(), Get-AppleDriverMsi(), Save-FromOffset()

## Knowledge Gaps
- **237 isolated node(s):** `PackageDescription`, `passThrough`, `wokeUp`, `.isVCL`, `hello` (+232 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 592 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **32 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `LinkSessionState` connect `LinkSessionState` to `3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`)`, `UsbLink`, `FakePC`, `Etapa 5c — o 3º monitor some quando desligado + tela preta no iPhone — Especificação de Design`, `Sendable`?**
  _High betweenness centrality (0.137) - this node is a cross-community bridge._
- **Are the 33 inferred relationships involving `FakeVirtualMonitor` (e.g. with `.TurningOnAgainWhileTheOldFeedDetachesKeepsTheSameVirtualMonitorOn()` and `.AttachFailureFallsBackToMirror()`) actually correct?**
  _`FakeVirtualMonitor` has 33 INFERRED edges - model-reasoned connections that need verification._
- **What connects `PackageDescription`, `passThrough`, `wokeUp` to the rest of the system?**
  _237 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.RunAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.14426229508196722 - nodes in this community are weakly interconnected._
- **Why does `5. Testes` connect `3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`)` to `LinkSessionState`, `MonitorMode`, `AccessUnitReader`, `VirtualDisplay`, `FakeDisplayApi`?**
  _High betweenness centrality (0.132) - this node is a cross-community bridge._
- **Are the 44 inferred relationships involving `MonitorMode` (e.g. with `3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`)` and `4. Tratamento de erros (Parte 3, aprovada)`) actually correct?**
  _`MonitorMode` has 44 INFERRED edges - model-reasoned connections that need verification._
- **Should `CancellationToken` be split into smaller, more focused modules?**
  _Cohesion score 0.1362126245847176 - nodes in this community are weakly interconnected._