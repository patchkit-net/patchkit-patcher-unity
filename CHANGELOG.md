# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [Unreleased]
### Added
- Support for LZMA2 compression using XZ
- Sending all events to Statistics Reporting Service
- Added processing of --online or --offline command line argument

### Changed
- Linux launch script

### Fixed
- Invalid display of progress value when unarchiving

## [3.10.1]
### Fixed
- Updated torrent-client to fix the issue with paths with spaces in them
- Missing unpacking suffix when repairing files

## [3.10.0]
### Added
- Support for PK_PATCHER_API_CACHE_URL environmental variable
- Skipping patches for files that content remained unchanged in newer version (during diff installation)
- Support for partial pack1 processing
- Support for new lowest_version_diff property
- Displaying the application display name
- An inspector warning if the default editor app secret has been modified
- Repairing invalid files before diff update
- Including version information in Sentry reports
- Support for second progress bar (can show minor operation like downloading, unarchiving etc.)
- Example scenes with double progress bars
- A clickable PatchKit logo in non whitelabel patchers
- Support for background image set in PatchKit Panel
- Support for PK_PATCHER_KEEP_FILES_ON_ERROR environment variable
- Light integrity checking every time the Patcher is launched
- Descriptive integrity check messages
- Support for resuming torrent downloading
- Animated progress bar during initialization and connecting
- New manifest format support
- Sending 'patcher_started' event to Statistics Reporting Service
- Handling torrent-client crashes
- Attaching "system-info" to Sentry events as tag
- Support for PK_OFFICIAL define
- Sending 'patcher_started' event to Statistics Reporting Service
- A launch script on Linux platforms

### Changed
- Update API servers configuration
- Rename PK_PATCHER_MAIN_URL environmental variable to PK_PATCHER_API_URL
- The patcher will now delete the lockfile when quitting
- Sending the key secrets to content and diff url requests
- Split the RepairAndDiff strategy into separate strategies.
- Pre update integrity checking uses the Repair strategy

### Fixed
- Availability of user action buttons (update, start & check for updates)
- Handling of the ZLib exception
- Invalid handling of patcher-data-location argument with spaces

## [3.9.2]
### Added
- Logging the probable cause of the Zlib exception when unpacking

## [3.9.1]
### Fixed
- Use 'any' instead of 'all' for publish method
- Checking available space on Linux
- Compatibility issue with getdiskspaceosx library on older versions of macOS

## [3.9.0]
### Added
- Add support for download speed unit parameter from application info
- Protecting patcher from being started without launcher (with attempt to actually start the launcher in that case)

### Changed
- Improved the network errors handling in downloaders

### Fixed
- Fix usage of Unity WWW network connection
- Broken windows librsync binary files

## [3.8.2]
### Added
- Added download speed unit switcher (if download speed is higher than 1 MB/s then MB/s are used, otherwise KB/s)
- Progress monitoring for unarchiving each of the files from Pack1 package

## [3.8.1]
### Fixed
- Block license key cache sharing between all non-custom patchers
- Properly handle situation when WWW response doesn't contain status header - previously it was assumed to be 200 (OK), right now response is marked as timed out

## [3.8.0]
### Added
- Patcher now keeps track of keeping only one it's instance running
- Game meta data file
- Time estimation for completing download of the patch

### Changed
- Redesign error button label
- Stability improvements for resource downloader
- Stability improvements for license dialog

### Fixed
- Fix issue with patching files which size is 2GB or more
- Properly handle timeouts when using chuncked HTTP downloader
- Fix occasional torrent downloader failures due to sharing violation issues with downloaded file
- Fix log file sending

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
