# Project rules for Claude

## What this is

MQTTnet.TestApp.WinForm is a Windows Forms test application for
[MQTTnet](https://github.com/dotnet/MQTTnet). It hosts an MQTT broker, a publisher client and a
subscriber client in one single window, all of them against `localhost`, so that MQTTnet can be
tried out without any other software. The application was originally created by
[@grammyleung](https://github.com/grammyleung).

The repository is an application, it is **not** published as a NuGet package and it has **no**
installer: no `GeneratePackageOnBuild`, no push script, no `Setup` folder, no Inno Setup script.
A release ends with the pushed tag.

One solution `src/MQTTnet.TestApp.WinForm.sln` with exactly one project:

- `src/MQTTnet.TestApp.WinForm/MQTTnet.TestApp.WinForm.csproj`, `OutputType` `WinExe`,
  `UseWindowsForms`, the whole application.

Layout inside `src/MQTTnet.TestApp.WinForm`:

- `Program.cs`: `Main` with `[STAThread]`, sets the high DPI mode and runs `Form1`.
- `Form1.cs`: everything else. The three MQTT parts live side by side as fields
  (`mqttServer`, `mqttClientPublisher`, `mqttClientSubscriber`), each with its own start and stop
  button handler. Keep new logic in that shape, one handler per button.
- `Form1.Designer.cs`: generated designer code, tabs and block scoped namespace, it does not follow
  `src/.editorconfig`. Change it through the designer or with matching hand edits, do not reformat
  it.
- `GlobalUsings.cs`: all usings of the project, including the alias `Timer`.

There is no `.resx` file, the designer works without resources.

Repository root: `README.md` (the only user documentation, badges and a link to the changelog),
`Changelog.md`, `License.txt` (MIT), `.gitattributes`, `.gitignore` and `src/.editorconfig`. There
is no `Updating.md`, no `HowToUse.md` and no screenshots.

## Build

```powershell
dotnet build src/MQTTnet.TestApp.WinForm.sln
```

- Single target framework `net9.0-windows` in the project, no multi-targeting. `RuntimeIdentifiers`
  is `win-x64`.
- All build properties live directly in the `.csproj`. There is **no** `Directory.Build.props` in
  this repository.
- `TreatWarningsAsErrors` is enabled, so every warning breaks the build, NuGet warnings (`NU****`)
  from restore included. A clean build reports zero warnings, keep it that way.
- `NU1803` (HTTP source usage during restore) is the one warning suppressed via `NoWarn`. Fix
  warnings instead of extending that list. `NuGetAudit` and `NuGetAuditMode=all` are on, so a
  vulnerable transitive package fails the build too.
- Versions come from GitVersion.MsBuild out of the git tags, for example `1.0.8-1` for the first
  commit after tag `1.0.7`. Never edit a version property or an assembly version by hand.
- Restore needs nuget.org. If a private feed is configured globally on the machine and cannot be
  reached, the audit data lookup fails with `NU1900`, which `TreatWarningsAsErrors` turns into a
  build error. Then build with an explicit source:
  `dotnet build src/MQTTnet.TestApp.WinForm.sln --source https://api.nuget.org/v3/index.json`.
  `dotnet list package --outdated` cannot be rescued that way, it restores against the configured
  sources regardless of `--source`. Query nuget.org directly in that case.
- There are no tests. A behaviour change is verified by starting the application, pressing
  `Server Start`, then `Client Publisher Start` and `Client Subscriber Start`, subscribing to the
  preset topic and publishing a message. The message has to appear in the lower text box.

## Code conventions

Follow the surrounding code, it is consistent in every hand written file:

- File header comment block with `<copyright file="..." company="Hämmer Electronics">` and a
  `<summary>`, then the file-scoped namespace.
- XML doc comments on every type and every member, private members included, no exceptions. Event
  handlers document `<param name="sender">` and `<param name="e">`.
- `Nullable`, `ImplicitUsings` and `LangVersion latest` are enabled. The MQTT fields are nullable
  and every use is guarded with `is null` or `is not null`, that is what makes stop and start
  repeatable.
- New `using` directives go into `GlobalUsings.cs`, inside the existing `#pragma warning disable
  IDE0065` block, never at the top of a file. The editorconfig requires usings inside the namespace
  (`csharp_using_directive_placement=inside_namespace:warning`), which global usings cannot satisfy,
  that is what the pragma is for. Do not add other pragmas. The comment text in that block is German
  because Visual Studio generated it, leave it alone.
- Fields, properties, methods and events are always accessed with `this.` qualification
  (`dotnet_style_qualification_for_*` at severity `warning`). Static methods are called without it,
  which is why the four connect handlers are `static`.
- `src/.editorconfig` also enforces braces everywhere, no multiple blank lines, four spaces, CRLF,
  UTF-8, file scoped namespaces, `System` usings sorted first and `IDE0005` as warning. Analyzer
  warnings are fixed, not silenced.
- `Form1.Designer.cs` is exempt from all of this, it is generated.

## Known quirks

Do not silently "clean up" these, they are existing behaviour:

- **Broker and clients in one process.** The form starts a real `MqttServer` on the port from the
  text box and connects its own two clients to `localhost` on the same port. Nothing here talks to a
  remote broker, and the port text box is disabled while the server runs.
- **Hardcoded credentials.** Both clients send `username` / `password`, and
  `ValidateConnectionAsync` accepts exactly those. This is a test application, the values are meant
  to be visible in the source.
- **Client ids shorter than ten characters are rejected.** `ValidateConnectionAsync` answers with
  `ClientIdentifierNotValid`. The two built-in ids `ClientPublisher` and `ClientSubscriber` pass, an
  external client connecting to this broker needs a long id as well.
- **The publisher also receives.** `ButtonPublisherStartClick` attaches
  `HandleReceivedApplicationMessage` to the publisher client, and that handler writes into
  `TextBoxSubscriber`, the same box the subscriber writes to. The publisher never subscribes to
  anything, so in practice the handler stays silent.
- **Published messages are retained.** `WithRetainFlag()` is set on every publish, so a subscriber
  that connects later immediately gets the last message of the topic.
- **Polling instead of events.** A `System.Timers.Timer` with a one second interval enables and
  disables the buttons in `TimerElapsed`. Button states therefore lag up to one second behind the
  actual state. There is no event based enabling, do not mix both.
- **`maskedTextBox1`.** The designer creates the control but never adds it to `this.Controls`. It is
  invisible and dead, it only exists in the generated code.
- **MQTT 3.1.1 and no TLS.** Both clients use `MqttProtocolVersion.V311` and an
  `MqttClientTlsOptions` with `UseTls = false`, and they still set every "ignore certificate" flag.
  Those flags do nothing while TLS is off.
- **AppVeyor badge without CI in the repository.** `README.md` links an AppVeyor build that is
  configured outside of this repository. There is no `.github` folder and no pipeline file here.
- **The solution has an orphan configuration block.** `ProjectConfigurationPlatforms` lists the GUID
  `{54305F2A-6706-442F-AD66-F7AF6961B8D5}`, for which no `Project` entry exists. It is the leftover
  of a removed project, Visual Studio ignores it.
- **`src/MQTTnet.TestApp.WinForm.sln.DotSettings`** is tracked and holds nothing but a ReSharper
  user dictionary (`Haemmer`, `H_00E4mmer`, `mqtt`, `Tnet`). Leave it alone.
- **`.gitattributes` sets `* text=auto`**, every rule of the Visual Studio template below it is
  commented out. A binary file added later needs its own rule.

## Releasing

1. Make the change.
2. Add an entry at the top of `Changelog.md` in the existing format:
   `* **Version 1.0.8.0 (2026-08-13)** : Short description.`
3. Commit that.
4. Tag the commit with the plain version number, no `v` prefix (`1.0.7`, `1.0.6`, ...). The existing
   tags are lightweight tags, create new ones the same way.
5. Push the commits and the tag.

The version in the `Changelog.md` has four parts (`1.0.8.0`), the tag has three (`1.0.8`).
GitVersion turns the tag into the assembly version, so an untagged commit produces something like
`1.0.8-1+Branch.master.Sha...`. There is no installer to build and no package to push, so the
release ends with the push.

## Git

- **Never amend a commit.** No `git commit --amend`, not for a typo in the message, not to add a
  forgotten file, not even when the commit is still local. Write a follow-up commit instead. The
  release versions come from tags on exact commits, an amended commit leaves its tag pointing at a
  commit that no longer exists in the branch.

## Writing style

- Commit messages are written **in English only**: short, precise subject line, explanatory body
  when needed.
- Code comments and comments in project files such as `.csproj` are **always English**, regardless
  of the language used in the conversation.
- **No em dashes or en dashes** (`—`, `–`), neither in prose, commit messages, code comments nor
  documentation. Use a regular hyphen, comma, colon, parentheses or a separate sentence.
- German texts (documentation, chat replies) always use real umlauts and ß, never ASCII
  transliterations such as `ae`, `oe`, `ue` or `ss`. Identifiers, file names and configuration keys
  stay unchanged where umlauts are technically undesirable.
