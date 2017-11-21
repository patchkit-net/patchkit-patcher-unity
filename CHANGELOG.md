# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [3.7.0]
### Changed
- Checking files hash before diff update only if content size is less that certain treshold (by default set to 1 GB)

## [3.6.0]
### Added
- Sentry messages are now sent with download link for log file and include additional data (log file guid, local version id, remote version id and app secret)
- API operations logging
- Support for PK_PATCHER_MAIN_URL environment variable

### Changed
- HTTP downloading timeouts changed from 10 to 30 seconds
- Temp folders have more randomized names now
- New log format
- Switch to Unity web requests for main API

### Fixed
- Unpacking error when AV software would block files (now for content packages)
- Fix delaying patcher quit due to log sending
- Fix displaying no internet connection error in case of API connection problems
- Fix Unity wrapped requests to return status code and correct exceptions
- Fix bug with applying patches (CRITICAL FIX!)

## [3.5.0]
### Added
- HTTPS support for keys server
- Fallback for content strategy in case of diff strategy failure

### Changed
- Geolocate: Increased timeout from 5s to 10s

### Fixed
- Compilation error on Unity 5.6 or higher
- Unpacking error when AV software would block files
- Geolocate: NullReferenceException on timeout
- Error when Patcher would appear to be unresponding during downloading

## [3.4.0]
### Added
- Sentry integration
- File parts support

### Changed
- Refactor log sending service
- Move configuration option for switching whether to use diffs to defines
- Use diffs only when publish method is set to "all"

### Fixed
- Fix progress reporting for unarchiving Pack1 packages
- Free space calculation algorithm was using for smaller free space values that it should be
- Fix error that occured when wrong license key was submitted

## [3.3.0]
### Added
- getdiskspaceosx a native library for determining the amount of free space available on Mac OSX.
- Status descriptions for updating app patcher state
- Add configuration option to switch whether to use diffs or not

### Changed
- Separate version integrity check progress from update progress

## [3.2.0]
### Added
- Using geolocation to find closest http server available
- Decides on torrents based on publish_method remote app property

### Changed
- Display warning message when auto-update fails and app is installed

### Fixed
- HTTP downloader resumes the download on error
- Moving files after installation instead of copying them
- Fix keys API connection problem
- Fix API cache usage problems
- Fix problem with remaining ".app" directories on Mac OSX

## [3.1.4]
### Fixed
- Fix TorrentDownloader progress reporting for big files

## [3.1.3]
### Added
- Add milliseconds information to log date and time
- Add KNOWNBUGS.md

### Removed
- Remove stack trace information from logs sent to server

### Changed
- Improved overall stability
- API Cache servers urls

### Fixed
- Fix problem with resource validation
- Fix problems with overwriting files by downloader and unarchiver
- Fix situation when user was asked for license key once again
- Fix sending patcher logs to server
- Fix problem with shared access violation when downloading torrents

## [3.1.2]
### Fixed
- Fix situation when user was asked for license key once again

### Changed
- Add milliseconds information to log date and time
- Improved overall stability

## [3.1.1]
### Fixed
- torrent-client.exe required MSVCP140.dll to work correctly

## [3.1.0]
### Added
- Sending logs to S3 on error
- Error if there's no enough space on the device
- Security check against application secret
- Add Pack1 support
- Add command line --readable to pass readable secret

## [3.0.2]
### Added
- Info how to fix JSON dll issue
- Asset Store tools and internal tools

### Changed
- Forcing torrent-client executable flag before start

### Fixed
- TorrentDownloader: Possible NullReferenceException on Dispose()

## [3.0.1]
### Fixed
- Linux application were not started correctly
- Version info tags reading
