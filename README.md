# TwitchMemeAlertsAuto

This repository contains two related projects for automatically rewarding MemeAlerts supporters via Twitch channel point redemptions:

- `TwitchMemeAlertsAuto.CLI` — a command-line utility
- `TwitchMemeAlertsAuto.WPF` — a WPF GUI application

Both projects target .NET 10.

## Features

- Listens to Twitch chat for custom reward redemptions.
- Matches Twitch users to MemeAlerts supporters.
- Automatically grants MemeAlerts bonuses based on reward configuration.
- Configurable via command-line options.
- Publishes as a self-contained Windows x64 executable.

- ## Requirements

- .NET 10 SDK (for building; published EXE is self-contained)
- Twitch channel with custom rewards configured
- MemeAlerts account and API token

## Usage

CLI usage

After building or downloading the published CLI EXE, run:

```cmd
tmaa.exe --channel <twitch_channel> --token <memealerts_token> --rewards <reward_id1:value1,reward_id2:value2,...>
```

WPF usage

The WPF application provides a graphical interface for connecting to Twitch and configuring reward mappings. It is a standalone GUI and can be launched by double-clicking the published EXE (no command line required). To build and run from source use Visual Studio or:

```cmd
dotnet run --project src/TwitchMemeAlertsAuto.WPF
```

After publishing, launch the GUI by double-clicking the executable at:

```cmd
src/TwitchMemeAlertsAuto.WPF/bin/Release/net10.0/win-x64/publish/TwitchMemeAlertsAuto.WPF.exe
```

### Options

- `--channel`, `-c`  
  Twitch channel name to monitor (required).

- `--token`, `-t`  
  MemeAlerts API token (required).

- `--rewards`, `-r`  
  Comma-separated list of reward IDs and their values, e.g. `id1:100,id2:200` (required).

## Example
```cmd
tmaa.exe -c mychannel -t mytoken -s 123456 -r reward1:100,reward2:200
```

## Build & Publish

To build and publish the CLI using the provided profile:

```cmd
dotnet publish src/TwitchMemeAlertsAuto.CLI/TwitchMemeAlertsAuto.CLI.csproj /p:PublishProfile=FolderProfile
```

The CLI executable will be located at:

```cmd
src/TwitchMemeAlertsAuto.CLI/bin/Release/net10.0/publish/win-x64/tmaa.exe
```

To build and publish the WPF application (self-contained Windows x64):

```cmd
dotnet publish src/TwitchMemeAlertsAuto.WPF/TwitchMemeAlertsAuto.WPF.csproj -c Release -r win-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true
```

The WPF executable will be located at:

```cmd
src/TwitchMemeAlertsAuto.WPF/bin/Release/net10.0/win-x64/publish/TwitchMemeAlertsAuto.WPF.exe
```

## GitHub Actions Release

On every push of a tag starting with `v` (e.g., `v1.0.0`), a GitHub Release is created with the published EXE attached.

## License

See [LICENSE](LICENSE) for details.
