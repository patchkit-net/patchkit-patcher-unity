# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

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
