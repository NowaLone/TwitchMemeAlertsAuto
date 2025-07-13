# TwitchMemeAlertsAuto.CLI

A .NET 8 command-line utility for automatically rewarding MemeAlerts supporters via Twitch channel point redemptions.

## Features

- Listens to Twitch chat for custom reward redemptions.
- Matches Twitch users to MemeAlerts supporters.
- Automatically grants MemeAlerts bonuses based on reward configuration.
- Configurable via command-line options.
- Publishes as a self-contained Windows x64 executable.

## Requirements

- .NET 8 SDK (for building; published EXE is self-contained)
- Twitch channel with custom rewards configured
- MemeAlerts account and API token

## Usage

After building or downloading the published EXE, run:

```cmd
TwitchMemeAlertsAuto.CLI.exe --channel <twitch_channel> --token <memealerts_token> --streamerId <memealerts_streamer_id> --rewards <reward_id1:value1,reward_id2:value2,...>.exe --channel <twitch_channel> --token <memealerts_token> --streamerId <memealerts_streamer_id> --rewards <reward_id1:value1,reward_id2:value2,...>
```

### Options

- `--channel`, `-c`  
  Twitch channel name to monitor (required).

- `--token`, `-t`  
  MemeAlerts API token (required).

- `--streamerId`, `-s`  
  MemeAlerts streamer ID (required).

- `--rewards`, `-r`  
  Comma-separated list of reward IDs and their values, e.g. `id1:100,id2:200` (required).

## Example
```cmd
tmaa.exe -c mychannel -t mytoken -s 123456 -r reward1:100,reward2:200
```

## Build & Publish

To build and publish using the provided profile:

```cmd
dotnet publish src/TwitchMemeAlertsAuto.CLI/TwitchMemeAlertsAuto.CLI.csproj /p:PublishProfile=FolderProfile
```

The executable will be located at:

```cmd
src/TwitchMemeAlertsAuto.CLI/bin/Release/net8.0/publish/win-x64/tmaa.exe
```

## GitHub Actions Release

On every push of a tag starting with `v` (e.g., `v1.0.0`), a GitHub Release is created with the published EXE attached.

## License

See [LICENSE](LICENSE) for details.
