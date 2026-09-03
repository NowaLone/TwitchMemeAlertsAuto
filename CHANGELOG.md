# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.4.8] - 2026-09-03

### Added

- Return meme with text reward if search return no result

### Fixed

- No memes were given if the user was found using the input text

## [2.4.7] - 2026-09-01

### Added

- Add memes redemption history for future experements

## [2.4.6] - 2026-08-30

### Fixed

- 400 error when sending meme with text

### Changed

- Search logic for send meme with text

## [2.4.5] - 2026-08-29

### Added

- Ability to logout
- Send meme with text

### Fixed

- Wrong memer name when sending random meme
- Add/Remove rewards errors
- Add/Remove rewards button state doesn't refresh
- Sometimes unable to connect to memealerts

### Changed

- Disable reward buttons if not logged in
- Show minimal changelog on release page
 
## [2.4.4] - 2026-08-07

### Fixed

- Forget to save memealerts token... now fixed

## [2.4.3] - 2026-07-26

### Fixed

- Migrate db at first

## [2.4.2] - 2026-06-27

### Added

- Reopen currently launched app copy if user tries to launch another one

## [2.4.1] - 2026-06-25

### Added

- Silent mode checkbox

## [2.4.0] - 2026-06-15

### Added

- Send randmom meme reward creation

### Fixed

- Twitch token expiration while app is open

### Changed

- Only one app copy can be launched simultaneously

## [2.3.1] - 2026-05-21

### Added

- Show last memer reward creation

## [2.3.0] - 2026-04-25

### Added

- Show username on connect buttons 
- Single file application
- Fallback image for twitch rewards
- Ability to log to file with ```--Logging:LogLevel:Default=Debug``` startup parameter
- Http requests retry policy
- AI generated unit tests

### Fixed

- Some null refrences
- Supporters load
- Some typos

### Changed

- Connect buttons moved
- Increased http timeouts
- Bettetr error handling

### Removed

- Unecessary code

## [2.2.3] - 2026-02-01

### Added

- Ability to reward by Twitch nickname

## [2.2.2] - 2026-02-01

### Fixed

- Fixed WPF exe version assignment

## [2.2.1] - 2026-02-01

### Fixed

- Fixed CLI build and runtime errors

## [2.2.0] - 2026-02-01

### Added

- Add silent startup
- Add ability to start app on system login
- Add tray icon

### Fixed

- Improved instability and vulnerability

## [2.1.0] - 2026-02-01

### Added

- Display of supporters with the ability to reward each one individually
- Ability to reward the most recent meme submitters or the most recent reward recipients
- Refresh buttons
- Tooltips
- One-click updates

### Fixed
- Improved stability and security

## [2.0.1] - 2026-01-27

### Added

- About message with current app version
- Work() restarts after reward cost change
- Work() starts after all services connected in any order

### Fixed

- Missed CLI exe publication
- IndexOutOfRange when no rewards passed to Work() method
- Incorrect Work() restart

## [2.0.0] - 2026-01-27

### Added

- WPF project

### Changed

- CLI tool rewrite

### Removed

- streamer-id parameter

## [1.0.0] - 2025-08-04

### Added

- First public release.

[2.4.8]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v2.4.8
[2.4.7]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v2.4.7
[2.4.6]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v2.4.6
[2.4.5]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v2.4.5
[2.4.4]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v2.4.4
[2.4.3]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v2.4.3
[2.4.2]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v2.4.2
[2.4.1]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v2.4.1
[2.4.0]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v2.4.0
[2.3.1]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v2.3.1
[2.3.0]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v2.3.0
[2.2.3]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v2.2.3
[2.2.2]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v2.2.2
[2.2.1]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v2.2.1
[2.2.0]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v2.2.0
[2.1.0]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v2.1.0
[2.0.1]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v2.0.1
[2.0.0]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v2.0.0
[1.0.0]: https://github.com/NowaLone/TwitchMemeAlertsAuto/releases/tag/v1.0.0