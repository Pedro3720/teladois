# Graph Report - Teladois  (2026-10-19)

## Corpus Check
- 100 files · ~36,105 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 7 file(s) not represented in the graph (top: (none) 3, .h264 2, .css 1)

## Summary
- 957 nodes · 2374 edges · 43 communities (32 shown, 11 thin omitted)
- Extraction: 85% EXTRACTED · 15% INFERRED · 0% AMBIGUOUS · INFERRED: 364 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `c4a4884c`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- PC Link Session Core
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
- PC<->iPhone protocol (HELLO, CONFIG, VIDEO, KEYFRAME_REQ, PING/PONG, BYE)
- TrayContext
- Etapa 4: USB tunnel (done, tested on iPhone)
- Prototype Static Server
- ios-test workflow (swift test + XCUITest + screenshots)
- BatteryMonitor
- Swift Package Manifest
- IReadOnlyList
- LinkSessionState
- FrameQueue
- TelaDoisCore
- RestScreenView
- Etapa 5b — Monitor estendido (monitor virtual) + atalho — Especificação de Design
- Foundation
- LinkProtocol
- LinkProtocolTests
- VideoRenderer
- Equatable
- LinkEndReason

## God Nodes (most connected - your core abstractions)
1. `FakeUsbmuxd` - 33 edges
2. `FakeProcessRunner` - 28 edges
3. `FakeProcess` - 28 edges
4. `TelaDois.Core.Video` - 25 edges
5. `FfmpegCapture` - 25 edges
6. `LinkFrame` - 24 edges
7. `UsbLink` - 23 edges
8. `LinkFrame` - 21 edges
9. `VideoConfig` - 21 edges
10. `UsbTunnelService` - 21 edges

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

## Communities (43 total, 11 thin omitted)

### Community 0 - "PC Link Session Core"
Cohesion: 0.08
Nodes (22): LinkFrame, MessageType, LinkFrameDecoder, LinkFrameReader, HelloInfo, LinkMessages, VideoConfig, LinkSession (+14 more)

### Community 1 - "LinkFrame"
Cohesion: 0.16
Nodes (11): LinkFrame, .messageType, LinkMessageType, bye, config, hello, keyframeRequest, ping (+3 more)

### Community 2 - "DimState"
Cohesion: 0.28
Nodes (5): DimState, TouchResult, passThrough, wokeUp, DimStateTests

### Community 3 - "TelaDois.Core.Video"
Cohesion: 0.05
Nodes (18): TelaDois.Core, TelaDois.Core.Link, TelaDois.Core.Tests.Fakes, TelaDois.Core.Tests, TelaDois.Core.Tests.Video, TelaDois.Core.Video, TelaDois.Core.Usbmux, LinkMessageType (+10 more)

### Community 4 - "FakeUsbmuxd"
Cohesion: 0.06
Nodes (23): LinkSessionOptions, Default, Plist, UsbmuxClient, DefaultEndpoint, UsbmuxException, Code, DeviceAttached (+15 more)

### Community 6 - "Open-Meteo Weather"
Cohesion: 0.09
Nodes (8): CodingKeys, temperature, Current, MissingTemperature, OpenMeteo, Response, OpenMeteoTests, WeatherProvider

### Community 7 - "H264NALUnit"
Cohesion: 0.08
Nodes (9): H264NALType, H264NALUnit, .isVCL, H264Parser, close, H264ParserTests, FakePC, RestScreenUITests (+1 more)

### Community 8 - "Design Tokens & Icon"
Cohesion: 0.17
Nodes (15): Design tokens (Etapa 1), Behavior tokens (60 s dim with 1.2 s fade, wake tap does not press buttons), Color tokens (black, clock #7FA9F2, ink, muted, battery, track, bars), Landscape layout (clock left, date/temp/battery right, mini player), Typography tokens (Sofia Sans Extra Condensed stretched 1.5x, rounded system font), DimController (60 s inactivity, black screen, brightness 0), Landscape-only, full-screen, hidden status bar, iPhone-only settings, index.html (Modo Descanso web prototype) (+7 more)

### Community 9 - "Clock Formatting"
Cohesion: 0.21
Nodes (3): ClockFormatter, ClockText, ClockFormatterTests

### Community 10 - "Encerrar Session Routine"
Cohesion: 0.13
Nodes (19): /encerrar skill (end-of-session routine), Step 5: commit and push (never force, no merge to main), Step 3: handoff between computers, Step 4: pre-commit secret/security check, Step 6: summary for the user, Step 1: update CLAUDE.md, Step 2: verify Claude Code environment for real, pages workflow (publish prototype to GitHub Pages) (+11 more)

### Community 12 - "Prototype Package Config"
Cohesion: 0.12
Nodes (11): devDependencies, @playwright/test, name, private, scripts, serve, test, type (+3 more)

### Community 13 - "FakeProcess"
Cohesion: 0.09
Nodes (22): 3.1 PC (C#, `windows/src/TelaDois.Core`), 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, AccessUnitReader, DisplayOutput, EncoderProbe, ProbeTimeout, FfmpegCapture (+14 more)

### Community 14 - ".NET Project Setup"
Cohesion: 0.14
Nodes (10): net10.0-windows, coverlet.collector (6.0.4), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), net10.0, Microsoft.NET.Sdk, Microsoft.NET.Sdk (+2 more)

### Community 15 - "FakeReadStream"
Cohesion: 0.07
Nodes (13): BlockingStream, IsDisposed, ReadStarted, FailingStream, FakeReadStream, CanRead, CanSeek, CanWrite (+5 more)

### Community 16 - "Prototype Rest Mode JS"
Cohesion: 0.23
Nodes (12): armDimTimer(), changeTrack(), fetchTemp(), keepScreenOn(), renderClock(), renderMusic(), showTemp(), start() (+4 more)

### Community 17 - "Install & Tunnel Decisions"
Cohesion: 0.23
Nodes (9): usbmuxd USB tunnel (127.0.0.1:7100), Install on iPhone via xtool in WSL (bash -lc, USBMUXD_SOCKET_ADDRESS), INSTALACAO.md (install TelaDois on iPhone without paid account), iTunes/iCloud web installers (not Microsoft Store) for Apple Mobile Device Support, iPhone Developer Mode and trusting the Apple ID, Sideloadly install method (alternative), WSL mirrored networking so Ubuntu reaches Windows usbmuxd 127.0.0.1:27015, xtool install method (primary) (+1 more)

### Community 18 - "RestScreen (Clock and Music pages)"
Cohesion: 0.18
Nodes (8): Etapa 2: native Modo Descanso (done), Etapa 3: Spotify (deferred), Open-Meteo weather API, BatteryService (UIDevice), RestScreen (Clock and Music pages), SpotifyService (App Remote SDK), WeatherService (Open-Meteo + CoreLocation), Location when-in-use permission string (for weather)

### Community 20 - "Clock Page & Battery Ring"
Cohesion: 0.11
Nodes (7): BatteryRingView, .body, .percent, ClockPageView, .body, Theme, SwiftUI

### Community 21 - "PC<->iPhone protocol (HELLO, CONFIG, VIDEO, KEYFRAME_REQ, PING/PONG, BYE)"
Cohesion: 0.24
Nodes (9): AppState (rest <-> secondScreen state machine), PC <-> iPhone architecture over USB (usbmuxd), CaptureEncoder (FFmpeg ddagrab + hardware H.264, libx264 fallback), DeviceWatcher (PC usbmuxd client, attach/detach), PC<->iPhone protocol (HELLO, CONFIG, VIDEO, KEYFRAME_REQ, PING/PONG, BYE), StreamPlayer (H.264 decode via VideoToolbox), StreamSender (opens tunnel, sends messages, measures latency), UsbLink (iPhone listener on port 7100) (+1 more)

### Community 22 - "TrayContext"
Cohesion: 0.06
Nodes (15): TelaDois.Tray, TunnelStatusText, TunnelState, Connected, ServiceMissing, WaitingForApp, WaitingForIPhone, TunnelStatus (+7 more)

### Community 23 - "Etapa 4: USB tunnel (done, tested on iPhone)"
Cohesion: 0.18
Nodes (9): Etapa 4: USB tunnel (done, tested on iPhone), Etapa 5: Segunda Tela (next: virtual monitor + FFmpeg + video), Modo Descanso (StandBy-style rest mode), Segunda Tela (PC second monitor over USB), TelaDois app (iPhone second screen + standby), Virtual Display Driver (pipe MTTVirtualDisplayPipe), DisplayManager (Virtual Display Driver control, cleanup of leftover monitors), Error handling table (cable removed, app left, encoder missing, decode error) (+1 more)

### Community 25 - "ios-test workflow (swift test + XCUITest + screenshots)"
Cohesion: 0.12
Nodes (14): ios-build workflow (unsigned Release .ipa), App icon check step (Assets.car, CFBundleIconName=AppIcon), TelaDois-ipa artifact (TelaDois.ipa + BUILD_INFO.txt), ios-test workflow (swift test + XCUITest + screenshots), screenshots artifact (renamed simulator prints), UI-test launch arguments (-TDFixedDate, -TDDimSeconds, ...), App icon (icone-1024.png, Canva), install-xcodegen.sh script (+6 more)

### Community 31 - "IReadOnlyList"
Cohesion: 0.09
Nodes (15): EncoderProfile, EncoderProfiles, Candidates, FfmpegArguments, ProcessStopper, IProcessRunner, IRunningProcess, Output (+7 more)

### Community 32 - "LinkSessionState"
Cohesion: 0.20
Nodes (4): Action, send, LinkSessionState, LinkSessionStateTests

### Community 33 - "FrameQueue"
Cohesion: 0.19
Nodes (10): 1. Objetivo, 2. Ambiente de referência, 4. Tratamento de erros, 5. Testes, 6. Riscos, Etapa 5a — Vídeo pelo cabo (monitor existente → iPhone) — Especificação de Design, Fora do escopo da 5a, FrameQueue (+2 more)

### Community 34 - "TelaDoisCore"
Cohesion: 0.15
Nodes (7): AVFoundation, CoreLocation, Network, Observation, TelaDoisCore, UIKit, XCTest

### Community 35 - "RestScreenView"
Cohesion: 0.23
Nodes (4): RestScreenView, .body, ScreenBrightness, .screen

### Community 36 - "Etapa 5b — Monitor estendido (monitor virtual) + atalho — Especificação de Design"
Cohesion: 0.12
Nodes (13): 1. Objetivo, 2. O driver (lido no código-fonte da release 25.7.23), 3.1 PC (C#, `windows/src/TelaDois.Core` e `TelaDois.Tray`), 3.3 Protocolo (os dois lados; bytes de referência nos testes de Swift e C#), 3. Arquitetura, 4. Tratamento de erros (Parte 3, aprovada), 5. Testes, 6. Riscos (+5 more)

### Community 37 - "Foundation"
Cohesion: 0.18
Nodes (3): Darwin, Foundation, AppSettings

### Community 38 - "LinkProtocol"
Cohesion: 0.26
Nodes (3): 3.2 iPhone (Swift), LinkProtocol, LinkVideoFrame

### Community 41 - "Equatable"
Cohesion: 0.33
Nodes (5): bigEndianBytes(), LinkDecodeError, payloadTooLarge, LinkHello, LinkVideoConfig

### Community 42 - "LinkEndReason"
Cohesion: 0.40
Nodes (5): LinkEndReason, Bye, Cancelled, Closed, Timeout

## Knowledge Gaps
- **90 isolated node(s):** `PackageDescription`, `passThrough`, `wokeUp`, `.isVCL`, `hello` (+85 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 226 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **11 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Etapa 4: USB tunnel (done, tested on iPhone)` connect `Etapa 4: USB tunnel (done, tested on iPhone)` to `Encerrar Session Routine`, `Install & Tunnel Decisions`, `TelaDoisCore`, `TelaDois.Core.Video`?**
  _High betweenness centrality (0.169) - this node is a cross-community bridge._
- **Are the 10 inferred relationships involving `FakeUsbmuxd` (e.g. with `.ConnectIsRefusedWhenAppIsClosed()` and `.ConnectReturnsTunnelToThePhone()`) actually correct?**
  _`FakeUsbmuxd` has 10 INFERRED edges - model-reasoned connections that need verification._
- **What connects `PackageDescription`, `passThrough`, `wokeUp` to the rest of the system?**
  _90 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `PC Link Session Core` be split into smaller, more focused modules?**
  _Cohesion score 0.07667525773195877 - nodes in this community are weakly interconnected._
- **Why does `LinkSessionState` connect `LinkSessionState` to `VideoRenderer`, `UsbLink`, `Etapa 5b — Monitor estendido (monitor virtual) + atalho — Especificação de Design`, `Foundation`?**
  _High betweenness centrality (0.130) - this node is a cross-community bridge._
- **Are the 20 inferred relationships involving `FakeProcessRunner` (e.g. with `.AlreadyCancelledTokenStartsNothing()` and `.CancellingDuringWaitDisposesRunningProcess()`) actually correct?**
  _`FakeProcessRunner` has 20 INFERRED edges - model-reasoned connections that need verification._
- **Should `TelaDois.Core.Video` be split into smaller, more focused modules?**
  _Cohesion score 0.05407925407925408 - nodes in this community are weakly interconnected._