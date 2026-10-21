# Etapa 5c — o 3º monitor some quando desligado + tela preta no iPhone — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Com o modo monitor desligado o Windows não lista o monitor virtual (o dispositivo do driver é desativado por tarefas agendadas criadas pelo instalador); Win+Shift+[ (ou o menu) apaga a tela do iPhone com o monitor funcionando, e 1 toque no iPhone acende.

**Architecture:** Uma interface `IDeviceSwitch` (implementada por `ScheduledTaskSwitch`, que roda `schtasks.exe` pelo `IProcessRunner` já existente) liga/desliga o dispositivo `Root\MttVDD`; o `VirtualDisplay` ganha `PowerOnAsync`/`PowerOffAsync` e a fábrica de vídeo liga antes do `Attach` e desliga depois do `Detach`. A tela preta é um estado do `MonitorMode` (contrato `IScreenSwitch`, consumido pela `LinkSession` como o `IVideoSwitch`) e uma mensagem nova `SCREEN` (tipo 10) nos dois sentidos.

**Tech Stack:** C# 14 / .NET 10 (SDK 10.0.402), WinForms, xUnit 2.9.3; PowerShell 5.1 + Agendador de Tarefas (COM `Schedule.Service`) no instalador; `pnputil.exe`; Swift 6 (pacote) / Swift 5 + concorrência estrita (app), XCTest/XCUITest.

**Spec:** `docs/superpowers/specs/2026-10-20-etapa-5c-monitor-some-e-tela-preta-design.md`. **Branch:** `etapa-5b-monitor-virtual` (continua nele).

## Global Constraints

- O usuário fala português: textos para ele (bandeja, registro, docs, mensagens do instalador) em português; código e nomes em inglês.
- Não há Mac: nunca rodar `xcodebuild`/`xcodegen`/`swift` localmente; iOS só pelo GitHub Actions (`ios-test`, `ios-build`). Um push com mudança em `ios/**` cancela o `ios-test` em andamento: para ver o vermelho, espere a execução anterior terminar. Confirme com `gh run view <id> --json status,conclusion`.
- PC: `dotnet test windows/TelaDois.sln` local + `windows-test` no CI. Zero avisos (C# `TreatWarningsAsErrors`; Swift `SWIFT_STRICT_CONCURRENCY: complete`).
- Bytes de referência nos dois testes (Swift e C#), além dos existentes: **`SCREEN(1)` = `0A0000000101`**, **`SCREEN(0)` = `0A0000000100`** (tipo 10, 1 byte de dados).
- Tarefas agendadas: pasta **`\TelaDois\`**, nomes **`Ligar monitor virtual`** e **`Desligar monitor virtual`**, rodam como `SYSTEM`, ação `pnputil.exe /enable-device "<InstanceId>"` / `/disable-device "<InstanceId>"`, sem gatilho, leitura+execução para o grupo Usuários (`BU`), tempo-limite 1 min, sem instâncias paralelas.
- Esperas: ligar o dispositivo até o monitor aparecer em `VirtualDisplay.Find()` — **20 s**; desligar até sumir — **5 s**.
- Atalho da tela preta: **Win+Shift+[**, tecla achada pelo caractere `[` no layout atual (ABNT2 ≠ US). O atalho da 5b (Win+Shift+]) continua.
- Item da bandeja: **`Tela preta no iPhone (Win+Shift+[)`**, com marcação.
- Print novo do teste de UI: **`08-tela-preta`**. Versão do app iPhone: `MARKETING_VERSION` **0.5.0**.
- Invariantes já provados (não quebrar): PING sempre sai (nada demorado no laço principal da `LinkSession`; abrir/descartar vídeo roda em `Task.Run`); cada feed descartado exatamente 1 vez; monitores só pelo `VirtualDisplay`/`IVirtualMonitor` e nunca num monitor físico; handlers de evento nunca lançam; `FileLog.Write` nunca lança; testes da `LinkSession` com as opções `Guarded`/`Patient`/`Fast` como estão.
- O driver está instalado no PC do trabalho: testes automáticos **nunca** tocam monitores nem rodam `pnputil`/`schtasks` de verdade (só falsos); o teste local com o driver real só roda com `TELADOIS_LOCAL_DRIVER_TEST=1` e quem roda é o controlador.
- Antes de baixar arquivo ou instalar algo, pedir autorização ao usuário; rodar o instalador como administrador é com o usuário.
- No Windows não editar arquivos com `Get-Content`/`Set-Content`; o `instalar-vdd.ps1` fica em UTF-8 **com BOM**.
- Commits: mensagem em `.git/TD_MSG`, `git commit -F .git/TD_MSG`, apagar o arquivo; trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; adicionar arquivos pelo nome; sem amend/force.

## Estrutura de arquivos

| Arquivo | Responsabilidade |
|---|---|
| `tools/vdd/instalar-vdd.ps1` | + cria as 2 tarefas e desativa o dispositivo no fim |
| `windows/src/TelaDois.Core/Display/IDeviceSwitch.cs` (novo) | `IDeviceSwitch` (disponível? ligar, desligar) |
| `windows/src/TelaDois.Core/Display/ScheduledTaskSwitch.cs` (novo) | `IDeviceSwitch` com `schtasks.exe` |
| `windows/src/TelaDois.Core/Display/IVirtualMonitor.cs`, `VirtualDisplay.cs` | + `PowerOnAsync`/`PowerOffAsync`, `CanPowerOn` |
| `windows/src/TelaDois.Core/Video/VideoFeedFactory.cs`, `VirtualMonitorFeed.cs` | liga antes do `Attach`, desliga depois do `Detach`; `DeviceSwitchMissing` |
| `windows/src/TelaDois.Core/Display/IScreenSwitch.cs` (novo), `MonitorMode.cs` | estado da tela preta |
| `windows/src/TelaDois.Core/Link/LinkFrame.cs`, `LinkMessages.cs`, `LinkSession.cs` | `SCREEN` (tipo 10) |
| `windows/src/TelaDois.Tray/TrayContext.cs` | atalho Win+Shift+[, item do menu, desligar o dispositivo ao abrir/sair, balão de tarefas ausentes |
| `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkProtocol.swift` | `screen(dark:)`/`parseScreen` |
| `ios/TelaDois/UsbLink.swift`, `RestScreenView.swift` | capa preta, toque acende e avisa o PC |
| `ios/TelaDoisUITests/RestScreenUITests.swift` | teste + print `08-tela-preta` |
| Testes novos em `windows/tests/TelaDois.Core.Tests/` | `Display/ScheduledTaskSwitchTests.cs`, `Fakes/FakeDeviceSwitch.cs`, casos novos em `VirtualDisplayTests`, `VideoFeedFactoryTests`, `VirtualMonitorFeedTests`, `MonitorModeTests`, `LinkProtocolTests`, `LinkSessionTests` |

---

### Task 1: Instalador cria as tarefas e deixa o dispositivo desativado (+ o usuário roda de novo)

**Files:**
- Modify: `tools/vdd/instalar-vdd.ps1`

**Interfaces:**
- Produces: tarefas `\TelaDois\Ligar monitor virtual` e `\TelaDois\Desligar monitor virtual` neste PC; dispositivo `Root\MttVDD` desativado ao fim da instalação.

- [ ] **Step 1: Acrescentar, depois da instalação/atualização do driver e antes da mensagem final, este bloco**

```powershell
    # O Windows só deixa de listar o monitor com o dispositivo desativado (precisa de administrador).
    # Duas tarefas sob demanda, como SYSTEM, que qualquer usuário do PC pode disparar: o TelaDois liga/desliga sem senha.
    $device = Get-PnpDevice -Class Display -ErrorAction SilentlyContinue | Where-Object { $_.HardwareID -contains $HardwareId } | Select-Object -First 1
    if (-not $device) { throw 'O dispositivo do monitor virtual não apareceu depois da instalação.' }
    $instanceId = $device.InstanceId
    $service = New-Object -ComObject 'Schedule.Service'
    $service.Connect()
    $root = $service.GetFolder('\')
    try { $folder = $root.GetFolder('\TelaDois') } catch { $folder = $root.CreateFolder('TelaDois') }
    foreach ($pair in @(@('Ligar monitor virtual', '/enable-device'), @('Desligar monitor virtual', '/disable-device'))) {
        $definition = $service.NewTask(0)
        $definition.RegistrationInfo.Description = 'TelaDois: liga/desliga o monitor virtual (Virtual Display Driver).'
        $definition.Settings.ExecutionTimeLimit = 'PT1M'
        $definition.Settings.MultipleInstances = 2   # TASK_INSTANCES_IGNORE_NEW
        $definition.Settings.DisallowStartIfOnBatteries = $false
        $definition.Settings.StopIfGoingOnBatteries = $false
        $definition.Principal.UserId = 'SYSTEM'
        $definition.Principal.LogonType = 5          # TASK_LOGON_SERVICE_ACCOUNT
        $definition.Principal.RunLevel = 1           # TASK_RUNLEVEL_HIGHEST
        $action = $definition.Actions.Create(0)      # TASK_ACTION_EXEC
        $action.Path = "$env:SystemRoot\System32\pnputil.exe"
        $action.Arguments = "$($pair[1]) `"$instanceId`""
        # SYSTEM e administradores: tudo; Usuários (BU): ler e executar.
        $sddl = 'D:(A;;FA;;;SY)(A;;FA;;;BA)(A;;GRGX;;;BU)'
        $folder.RegisterTaskDefinition($pair[0], $definition, 6, 'SYSTEM', $null, 5, $sddl) | Out-Null   # 6 = criar ou atualizar
        Write-Host "Tarefa \TelaDois\$($pair[0]) pronta."
    }
    pnputil /disable-device "$instanceId" | Write-Host
    Write-Host 'Monitor virtual desativado: o Windows só vai mostrá-lo quando o TelaDois ligar o modo monitor.'
```

- [ ] **Step 2: Conferir a sintaxe e manter o BOM**

```powershell
$errors = $null; [System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path tools\vdd\instalar-vdd.ps1), [ref]$null, [ref]$errors) | Out-Null; $errors.Count
```
Esperado: `0`. Conferir com Python que o arquivo começa com `EF BB BF`.

- [ ] **Step 3: Commit** — `git add tools/vdd/instalar-vdd.ps1`; mensagem: `tools: instalador cria as tarefas que ligam/desligam o monitor virtual e o deixa desativado`.

- [ ] **Step 4: O usuário roda o instalador de novo (PowerShell como administrador)** — `powershell -ExecutionPolicy Bypass -File tools\vdd\instalar-vdd.ps1`. Esperado: as duas linhas "Tarefa … pronta." e "Monitor virtual desativado".

- [ ] **Step 5: Conferir sem administrador (controlador)**

```powershell
schtasks /query /tn "\TelaDois\Ligar monitor virtual"; schtasks /query /tn "\TelaDois\Desligar monitor virtual"
Get-PnpDevice -FriendlyName 'Virtual Display Driver' | Select-Object Status, InstanceId
```
Esperado: as duas tarefas listadas; `Status` = `Error`/`Unknown` (desativado). Disparar `schtasks /run /tn "\TelaDois\Ligar monitor virtual"` e medir em quanto tempo `Status` vira `OK` e o adaptador "Virtual Display Driver" aparece em `EnumDisplayDevices`; depois `…\Desligar…` e medir o sumiço. Anotar os tempos e conferir que Configurações → Tela mostra só 2 monitores com ele desligado.

---

### Task 2: `IDeviceSwitch` + `ScheduledTaskSwitch` + `VirtualDisplay.PowerOnAsync/PowerOffAsync`

**Files:**
- Create: `windows/src/TelaDois.Core/Display/IDeviceSwitch.cs`, `windows/src/TelaDois.Core/Display/ScheduledTaskSwitch.cs`
- Modify: `windows/src/TelaDois.Core/Display/IVirtualMonitor.cs`, `windows/src/TelaDois.Core/Display/VirtualDisplay.cs`
- Create: `windows/tests/TelaDois.Core.Tests/Fakes/FakeDeviceSwitch.cs`, `windows/tests/TelaDois.Core.Tests/Display/ScheduledTaskSwitchTests.cs`
- Modify: `windows/tests/TelaDois.Core.Tests/Display/VirtualDisplayTests.cs`, `windows/tests/TelaDois.Core.Tests/Fakes/FakeVirtualMonitor.cs`, `windows/tests/TelaDois.Core.Tests/Display/VirtualDisplayLocalTests.cs`

**Interfaces:**
- Produces:
  - `public interface IDeviceSwitch { Task<bool> IsAvailableAsync(CancellationToken ct); Task<bool> RunAsync(bool enable, CancellationToken ct); }` — `RunAsync` dispara a tarefa e devolve se o `schtasks` aceitou (não espera o dispositivo); nunca lança (exceto `OperationCanceledException` do chamador).
  - `public sealed class ScheduledTaskSwitch(IProcessRunner runner, Action<string> log) : IDeviceSwitch` com `public const string EnableTask = @"\TelaDois\Ligar monitor virtual"; public const string DisableTask = @"\TelaDois\Desligar monitor virtual";` — `IsAvailableAsync` = `schtasks /query /tn <EnableTask>` e `<DisableTask>` com código 0; `RunAsync` = `schtasks /run /tn <tarefa>` com código 0.
  - `IVirtualMonitor` ganha: `Task<bool> PowerOnAsync(CancellationToken ct)` — true quando o monitor do driver está presente (`Find() != null`) ao fim; `Task PowerOffAsync(CancellationToken ct)` — desliga o dispositivo (só se as tarefas existem); `Task<bool> CanPowerOffAsync(CancellationToken ct)` — as tarefas existem.
  - `VirtualDisplay(IDisplayApi api, IDriverPipe pipe, IDeviceSwitch? device = null, Action<string>? log = null)` com `TimeSpan PowerOnWait { get; init; } = 20 s` (era 8 s; o `Enable-PnpDevice` do PowerShell leva ~3–4 s e a 1ª vez depois do boot pode levar muito mais), `TimeSpan PowerOffWait { get; init; } = 5 s`, `TimeSpan PollInterval { get; init; } = 100 ms`.
  - Regras do `PowerOnAsync`: se `Find() != null` → true na hora (já ativo); senão, se `device` disponível → `RunAsync(true)` e espera `Find() != null` até `PowerOnWait`; senão → false. Registra `monitor virtual ativado em N ms` ou o motivo da falha (pelo `log` do construtor).
  - Regras do `PowerOffAsync`: se `device` indisponível → nada; senão `RunAsync(false)` e espera `Find() == null` até `PowerOffWait`; registra o tempo ou "não desativou em 5 s".

- [ ] **Step 1: `FakeDeviceSwitch`** (em `Fakes/`): propriedades `Available` (padrão true), `Runs` (`ConcurrentQueue<bool>`), `Accepts` (padrão true) e `Action<bool>? OnRun` (o teste usa para fazer o `FakeDisplayApi` ganhar/perder o adaptador do driver).

- [ ] **Step 2: Testes do `ScheduledTaskSwitch`** com o `FakeProcessRunner` existente (código de saída configurável):
  - `IsAvailableAsync` true só quando as duas consultas dão 0; argumentos exatos `["/query", "/tn", EnableTask]` e `["/query", "/tn", DisableTask]`, arquivo `schtasks.exe`.
  - `RunAsync(true)` roda `["/run", "/tn", EnableTask]`; `RunAsync(false)` roda `DisableTask`; código ≠ 0 → false e uma linha no registro; o `Start` lançando → false (nunca propaga).
- [ ] **Step 3: Testes do `VirtualDisplay`** (com `FakeDisplayApi` + `FakeDeviceSwitch`):
  - `PowerOnAsync` com o adaptador já presente → true sem rodar a tarefa.
  - Adaptador ausente + tarefa que, em `OnRun(true)`, acrescenta o adaptador depois de 300 ms → true, `Runs` = `[true]`.
  - Tarefa aceita mas o adaptador nunca aparece (`PowerOnWait` = 500 ms) → false.
  - Sem `IDeviceSwitch` (ou indisponível) e adaptador ausente → false sem rodar nada.
  - `PowerOffAsync` roda `false` e espera sumir; indisponível → não roda nada.
  - **Nenhum caminho chama `Apply` num monitor físico** (o teste `DetachIgnoresPhysicalMonitor` continua verde).
- [ ] **Step 4: Rodar e ver falhar** — `dotnet test windows/TelaDois.sln --filter "FullyQualifiedName~ScheduledTaskSwitch|FullyQualifiedName~VirtualDisplayTests"`; esperado: erro de compilação (tipos novos).
- [ ] **Step 5: Implementar** `IDeviceSwitch.cs`, `ScheduledTaskSwitch.cs` e os métodos novos no `VirtualDisplay`/`IVirtualMonitor` conforme as regras acima; `FakeVirtualMonitor` ganha `PowerOnAsync` (padrão true, contador `PowerOns`), `PowerOffAsync` (contador `PowerOffs`), `CanPowerOffAsync` (padrão true).
- [ ] **Step 6: Teste local com o driver real** — em `VirtualDisplayLocalTests`, um caso novo (mesma trava `TELADOIS_LOCAL_DRIVER_TEST=1`) que usa `new VirtualDisplay(Win32DisplayApi.ForVirtualMonitor(), new DriverPipe(), new ScheduledTaskSwitch(new SystemProcessRunner(), output.WriteLine), output.WriteLine)`: `PowerOffAsync` → `Find()` nulo; `PowerOnAsync` → true; escreve os tempos medidos na saída; termina com `PowerOffAsync`. (Quem roda é o controlador.)
- [ ] **Step 7: Rodar tudo** — `dotnet test windows/TelaDois.sln`; esperado: todos verdes, 0 avisos.
- [ ] **Step 8: Commit** — mensagem: `feat(windows): liga/desliga o dispositivo do monitor virtual pelas tarefas do Windows`.

---

### Task 3: A fábrica liga o dispositivo antes de estender e desliga depois

**Files:**
- Modify: `windows/src/TelaDois.Core/Video/VideoFeedFactory.cs`, `windows/src/TelaDois.Core/Video/VirtualMonitorFeed.cs`
- Test: `windows/tests/TelaDois.Core.Tests/Video/VideoFeedFactoryTests.cs`, `windows/tests/TelaDois.Core.Tests/Video/VirtualMonitorFeedTests.cs`

**Interfaces:**
- Consumes: `IVirtualMonitor.PowerOnAsync/PowerOffAsync/CanPowerOffAsync` (Task 2).
- Produces: `VideoFeedFactory.event Action? DeviceSwitchMissing` (disparado quando o driver está instalado mas as tarefas não existem — o monitor fica listado).

- [ ] **Step 1: Testes da fábrica** (com `FakeVirtualMonitor`):
  - Estender chama `PowerOnAsync` **antes** de `Attach` (o falso registra a ordem).
  - `PowerOnAsync` false e `IsInstalledAsync` false → Duplicar + `DriverMissing` (como hoje).
  - `PowerOnAsync` false com `CanPowerOffAsync` true (tarefas existem, mas o monitor não apareceu) → Duplicar, registro "o monitor virtual não ativou", **sem** `DriverMissing`.
  - Toda falha depois do `Attach` (DXGI não vê, sondagem lança, cancelamento) → `Detach` **e** `PowerOffAsync`, cada um 1 vez.
  - Driver instalado (`IsInstalledAsync` true) e `CanPowerOffAsync` false → estende e dispara `DeviceSwitchMissing` 1 vez.
- [ ] **Step 2: Teste do feed** — `VirtualMonitorFeed.DisposeAsync` faz `Detach` e depois `PowerOffAsync` (1 vez cada, mesmo com descarte duplo); se `PowerOffAsync` lançar, o descarte termina e registra.
- [ ] **Step 3: Rodar e ver falhar.**
- [ ] **Step 4: Implementar** — em `TryOpenAsync`, trocar o `IsInstalledAsync` por: `if (await virtualMonitor.PowerOnAsync(ct)) { if (!await virtualMonitor.CanPowerOffAsync(ct)) DeviceSwitchMissing?.Invoke(); … TryExtendAsync … } else if (await virtualMonitor.CanPowerOffAsync(ct)) { log("o monitor virtual não ativou: duplicando um monitor"); } else if (!await virtualMonitor.IsInstalledAsync(ct)) { /* mensagem e DriverMissing atuais */ }`. Em todo `DetachAndVerify` dos caminhos de falha do `TryExtendAsync`, acrescentar `await PowerOffQuietlyAsync(monitor)` (try/catch, registra); no `VirtualMonitorFeed.DisposeAsync`, depois do `DetachAndVerify`, `await _monitor.PowerOffAsync(CancellationToken.None)` dentro de try/catch.
- [ ] **Step 5: Rodar tudo; 0 avisos.**
- [ ] **Step 6: Commit** — `feat(windows): monitor virtual ativado só enquanto estende; desativado ao terminar`.

---

### Task 4: Bandeja — desativar ao abrir/sair, balão das tarefas ausentes

**Files:**
- Modify: `windows/src/TelaDois.Tray/TrayContext.cs`

**Interfaces:**
- Consumes: `ScheduledTaskSwitch` (Task 2), `VirtualDisplay(api, pipe, device, log)`, `VideoFeedFactory.DeviceSwitchMissing` (Task 3).

- [ ] **Step 1: Criar o `VirtualDisplay` com o `ScheduledTaskSwitch`** (`new ScheduledTaskSwitch(new SystemProcessRunner(), _log.Write)`) e o registro.
- [ ] **Step 2: Ao abrir:** depois do `DetachLeftover("ao abrir")` já existente, `PowerOffAsync` em segundo plano (não bloquear a thread da bandeja; registrar o resultado). Ao sair: depois do desligamento do túnel e do `DetachLeftover("ao sair")`, `PowerOffAsync` com limite de 5 s (`Wait` com tempo) — registrar quando estourar.
- [ ] **Step 3: Balão único** em `DeviceSwitchMissing`: "Rode o instalador do monitor virtual de novo (como administrador) para o Windows esconder o monitor quando ele não estiver em uso." (mesmo padrão `Interlocked` do `DriverMissing`).
- [ ] **Step 4:** `dotnet build windows/TelaDois.sln` com 0 avisos e `dotnet test windows/TelaDois.sln` verde.
- [ ] **Step 5: Commit** — `feat(windows): bandeja desativa o monitor virtual ao abrir e ao sair`.

---

### Task 5: Protocolo — `SCREEN` (tipo 10) nos dois lados

**Files:**
- Modify: `windows/src/TelaDois.Core/Link/LinkFrame.cs`, `windows/src/TelaDois.Core/Link/LinkMessages.cs`, `ios/Packages/TelaDoisCore/Sources/TelaDoisCore/LinkProtocol.swift`
- Test: `windows/tests/TelaDois.Core.Tests/LinkProtocolTests.cs`, `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/LinkProtocolTests.swift`, `ios/Packages/TelaDoisCore/Tests/TelaDoisCoreTests/LinkSessionStateTests.swift`

**Interfaces:**
- Produces: C# `LinkMessageType.Screen = 10`, `LinkMessages.Screen(bool dark)`, `LinkMessages.TryParseScreen(LinkFrame, out bool dark)`; Swift `LinkMessageType.screen = 10`, `LinkProtocol.screen(dark: Bool) -> LinkFrame`, `LinkProtocol.parseScreen(_:) -> Bool?`.

- [ ] **Step 1: Testes C#**

```csharp
    [Fact]
    public void ScreenMatchesGoldenBytes()
    {
        Assert.Equal(Convert.FromHexString("0A0000000101"), LinkMessages.Screen(true).Encode());
        Assert.Equal(Convert.FromHexString("0A0000000100"), LinkMessages.Screen(false).Encode());
    }

    [Fact]
    public void ScreenParsesAndRejectsWrongLength()
    {
        Assert.True(LinkMessages.TryParseScreen(new LinkFrame(10, [1]), out var dark));
        Assert.True(dark);
        Assert.False(LinkMessages.TryParseScreen(new LinkFrame(10, []), out _));
        Assert.False(LinkMessages.TryParseScreen(new LinkFrame(10, [1, 0]), out _));
        Assert.Equal(LinkMessageType.Screen, new LinkFrame(10, [0]).MessageType);
    }
```

- [ ] **Step 2: Testes Swift**

```swift
    func testScreenMatchesGoldenBytes() {
        XCTAssertEqual(LinkProtocol.screen(dark: true).encoded(), hex("0A0000000101"))
        XCTAssertEqual(LinkProtocol.screen(dark: false).encoded(), hex("0A0000000100"))
        XCTAssertEqual(LinkProtocol.parseScreen(LinkFrame(type: 10, payload: [1])), true)
        XCTAssertEqual(LinkProtocol.parseScreen(LinkFrame(type: 10, payload: [0])), false)
        XCTAssertNil(LinkProtocol.parseScreen(LinkFrame(type: 10, payload: [])))
        XCTAssertEqual(LinkFrame(type: 10).messageType, .screen)
    }
```
e em `LinkSessionStateTests`: `SCREEN` não gera resposta e renova o tempo-limite (mesmo padrão do teste de `videoEnd`). Ajustar os nomes (`LinkFrame(type:payload:)`) ao que o pacote realmente usa.

- [ ] **Step 3: Vermelho** — C# local (compilação); Swift no `ios-test` (esperar o anterior terminar).
- [ ] **Step 4: Implementar** nos dois lados (payload = 1 byte; `TryParse` exige tamanho 1; valor ≠ 0 = apagar).
- [ ] **Step 5: Verde** — `dotnet test` local + `ios-test`/`windows-test` verdes; 0 avisos.
- [ ] **Step 6: Commit** — `feat(protocolo): SCREEN (tipo 10) apaga e acende a tela do iPhone`.

---

### Task 6: iPhone — capa preta, toque acende e avisa o PC

**Files:**
- Modify: `ios/TelaDois/UsbLink.swift`, `ios/TelaDois/RestScreenView.swift`, `ios/project.yml` (versão 0.5.0)
- Test: `ios/TelaDoisUITests/RestScreenUITests.swift`

**Interfaces:**
- Consumes: `LinkProtocol.screen(dark:)`, `parseScreen` (Task 5).
- Produces: `UsbLink.isScreenDark: Bool` (observável) e `UsbLink.wakeScreen()` (acende e manda `SCREEN(0)` ao PC).

- [ ] **Step 1: Teste de UI** (depois do teste do `VIDEO_END`): conectar como o PC, `HELLO`, `CONFIG` + vídeo `bars-640x360.h264`, esperar `second-screen`; mandar `LinkProtocol.screen(dark: true)`; esperar o elemento `screen-dark-overlay` (até 2 s) e o brilho do print baixo (mesmo método do print `03`); `attachScreenshot("08-tela-preta")`; tocar no overlay; esperar ele sumir e `second-screen` continuar; ler quadros até chegar um `SCREEN` e afirmar `parseScreen == false`; mandar `bye()`.
- [ ] **Step 2: Vermelho no CI** (falha esperando `screen-dark-overlay`).
- [ ] **Step 3: `UsbLink`** — `case .screen:` com Segunda Tela ativa: `isScreenDark = parseScreen(frame) ?? isScreenDark`; sem Segunda Tela: ignora. `wakeScreen()`: se `isScreenDark`, `isScreenDark = false` e `send(LinkProtocol.screen(dark: false))`. `VIDEO_END`, `CONFIG` novo e `disconnect()` zeram `isScreenDark`.
- [ ] **Step 4: `RestScreenView`** — dentro do ramo da Segunda Tela, se `link.isScreenDark`: `Color.black` em tela cheia por cima do vídeo, `.contentShape(Rectangle())`, `.onTapGesture { link.wakeScreen() }`, `.accessibilityIdentifier("screen-dark-overlay")`; `onChange(of: link.isScreenDark)`: true → `brightness.dim()`, false → `brightness.restore()`. O ticker da Segunda Tela não pode restaurar o brilho enquanto `isScreenDark` (hoje ele chama `brightness.restore()` a cada segundo). Ao sair da Segunda Tela o Descanso começa com 60 s cheios (`dim.touch`).
- [ ] **Step 5: `MARKETING_VERSION` 0.5.0.**
- [ ] **Step 6: Verde no CI** (`ios-test` + `ios-build`), 0 avisos; baixar os prints e **revisar** `08-tela-preta.png` (preto/escuro, sem barras de vídeo visíveis) e `05-segunda-tela.png`.
- [ ] **Step 7: Commit** — `feat(ios): tela preta na Segunda Tela (SCREEN); 1 toque acende e avisa o PC`.

---

### Task 7: PC — estado da tela preta no `MonitorMode` e `SCREEN` na `LinkSession`

**Files:**
- Create: `windows/src/TelaDois.Core/Display/IScreenSwitch.cs`
- Modify: `windows/src/TelaDois.Core/Display/MonitorMode.cs`, `windows/src/TelaDois.Core/Link/LinkSession.cs`
- Test: `windows/tests/TelaDois.Core.Tests/Display/MonitorModeTests.cs`, `windows/tests/TelaDois.Core.Tests/LinkSessionTests.cs`

**Interfaces:**
- Consumes: `LinkMessages.Screen/TryParseScreen` (Task 5).
- Produces:
  - `public interface IScreenSwitch { bool WantsDark { get; } Task WaitForDarkChangeAsync(bool seen, CancellationToken ct); void PhoneWoke(); }` (namespace `TelaDois.Core.Link`).
  - `public enum DarkToggleResult { TurnedDark, TurnedLit, NoVideo }`.
  - `MonitorMode : IVideoSwitch, IScreenSwitch` com `DarkToggleResult ToggleDark()`, `bool WantsDark`, `event Action<bool>? DarkChanged` (fora da trava; os assinantes releem `WantsDark`).
  - `LinkSession.RunAsync(..., IVideoSwitch? videoSwitch = null, IScreenSwitch? screenSwitch = null)` (parâmetro novo no fim, opcional).
- Regras:
  - `ToggleDark()` só funciona com iPhone conectado **e** modo ligado; senão `NoVideo` e nada muda.
  - `WantsDark` volta a false sozinho em: `Toggle()` que desliga, `VideoEnded()`, `SessionEnded()`, `SessionStarted()`.
  - `PhoneWoke()` põe `WantsDark = false` (evento `DarkChanged(false)`), sem mandar nada de volta.
  - `LinkSession`: no laço principal, um `darkChanged = screenSwitch?.WaitForDarkChangeAsync(seenDark, ct)` junto dos outros (mesma técnica do `switchChanged`, relendo o valor); quando muda **e** o `CONFIG` desta imagem já saiu → `SendAsync(LinkMessages.Screen(dark))`; quando o `CONFIG` sai e `WantsDark` já é true → manda `SCREEN(1)` logo depois do `CONFIG`. Ao receber `SCREEN` do iPhone com `dark == false` → `screenSwitch?.PhoneWoke()`. Nada disso pode ficar esperando no laço principal (escritas já são limitadas por `options.Timeout`).

- [ ] **Step 1: Testes do `MonitorMode`**: `ToggleDark` sem iPhone/desligado → `NoVideo`; ligado → `TurnedDark` e depois `TurnedLit`; desligar o modo, `VideoEnded`, `SessionEnded` zeram; `PhoneWoke` zera e dispara `DarkChanged(false)`; `WaitForDarkChangeAsync` não perde mudança entre ler e esperar (mesmo padrão dos testes do `WaitForChangeAsync`).
- [ ] **Step 2: Testes da `LinkSession`** (opções `Patient`, iPhone falso):
  - Vídeo ligado, `CONFIG` já saiu, `ToggleDark()` → o iPhone recebe `0A0000000101`; de novo → `0A0000000100`.
  - `ToggleDark()` antes da 1ª imagem → o `SCREEN(1)` chega **depois** do `CONFIG`.
  - O iPhone manda `SCREEN(0)` → `WantsDark` vira false e o PC não responde nada.
  - Desligar o vídeo com a tela apagada → `VIDEO_END` e `WantsDark == false`; nenhum `SCREEN` depois do `VIDEO_END`.
  - PINGs continuam (usar o mesmo verificador de silêncio dos testes `Guarded`).
- [ ] **Step 3: Vermelho → implementar → verde**; `--filter FullyQualifiedName~LinkSessionTests` 10× seguidas; suíte inteira; 0 avisos.
- [ ] **Step 4: Commit** — `feat(windows): tela preta no MonitorMode e SCREEN na sessão`.

---

### Task 8: Bandeja — atalho Win+Shift+[ e item "Tela preta no iPhone"

**Files:**
- Modify: `windows/src/TelaDois.Tray/TrayContext.cs`, `windows/src/TelaDois.Core/UsbTunnelService.cs` (passar o `MonitorMode` como `screenSwitch`)

**Interfaces:**
- Consumes: `MonitorMode.ToggleDark/WantsDark/DarkChanged` (Task 7), `Hotkey(char, Action)` (5b), `LinkSession.RunAsync(..., screenSwitch)`.

- [ ] **Step 1:** `UsbTunnelService` passa o mesmo `MonitorMode` como `videoSwitch` e `screenSwitch`. Teste no `UsbTunnelServiceTests` (padrão dos existentes): com o iPhone falso e vídeo ligado, `ToggleDark()` faz o iPhone falso receber `SCREEN(1)`.
- [ ] **Step 2:** `TrayContext`: segundo `Hotkey('[', ToggleDark)` (cada `Hotkey` tem a própria janela); item `Tela preta no iPhone (Win+Shift+[)` com `Checked` relido de `WantsDark` na thread da bandeja (via `PostToUi`, igual ao `StateChanged`); `ToggleDark()` → registro `tela preta: ligada`/`desligada`/`modo monitor desligado`; atalho não registrado → balão + registro (mensagem neutra: "o atalho Win+Shift+[ não está disponível"); descartar o `Hotkey` ao sair.
- [ ] **Step 3:** `dotnet build` (0 avisos) e `dotnet test` verdes; teste manual rápido: com o TelaDois aberto e sem iPhone, Win+Shift+[ registra "tela preta: modo monitor desligado".
- [ ] **Step 4: Commit** — `feat(windows): atalho Win+Shift+[ e item "Tela preta no iPhone" na bandeja`.

---

### Task 9: Teste no iPhone e documentação

**Files:**
- Modify: `CLAUDE.md` (Estado atual, Estrutura de pastas, Como rodar, Decisões), `docs/superpowers/specs/2026-10-16-iphone-segunda-tela-descanso-design.md` (§8), `docs/INSTALACAO.md` (seção do driver: rodar o instalador de novo)

- [ ] **Step 1:** Controlador roda o teste local com o driver real (`TELADOIS_LOCAL_DRIVER_TEST=1`, casos da Task 2) e anota os tempos de ligar/desligar.
- [ ] **Step 2:** Publicar o PC (`dotnet publish … -o builds/windows-5c`), baixar o `.ipa` do `ios-build` verde do SHA final, inspecionar (Python isolado) e instalar pelo xtool numa aba do Terminal.
- [ ] **Step 3 (com o usuário):** (1) modo desligado → Configurações → Tela mostra só os 2 monitores físicos; (2) ligar (cabo/atalho) → o monitor aparece e o vídeo chega (anotar o tempo pelo registro); (3) Win+Shift+[ apaga o iPhone, o monitor continua usável; 1 toque acende e a marcação da bandeja some; (4) Win+Shift+] desliga → o "3" some de Configurações; (5) reiniciar o Windows com o modo desligado → o "3" não volta; (6) "Sair" → monitor desativado, nenhum `ffmpeg.exe`.
- [ ] **Step 4:** Atualizar os docs (decisão "dispositivo desativado por tarefas agendadas", tempos medidos, atalho Win+Shift+[, mensagem `SCREEN` e seus bytes na regra do protocolo, print `08`), `graphify update .`, commit e push; confirmar `ios-test`, `ios-build` e `windows-test` verdes no SHA final e **perguntar ao usuário** se junta `etapa-5b-monitor-virtual` ao `main`.
