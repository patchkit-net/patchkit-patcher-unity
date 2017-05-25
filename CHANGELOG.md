# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [3.1.2]
### Fixed
- Fix situation when user was asked for license key once again

### Changed
- Add miliseconds information to log date and time
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
