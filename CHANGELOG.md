# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [3.17.2.0]
### Added
- Attempting to repair content on failed installation

### Fixed
- Ability to test patcher in the editor

## [3.17.1.0]
### Fixed
- Installing diff which contains the same entry in added_files & removed_files

## [3.17.0.0]
### Added
- Support for custom start app arguments (#1486)

## [3.16.0.0]
### Changed
- Improved stability of downloading (#1422)
- Improved stability of saving app meta data file (#1423)

### Fixed
- Checking available disk space on OSX (#1424)

## [3.15.0.0]
### Added
- Caching application display name so it can be displayed in offline mode (#1350)
- Caching application changelog so it can be displayed in offline mode (#1361)
- Skip unchanged files while patching (#994)

### Changed
- Set process priority to Low (#1375)
- Adjust wages of installation steps (#1379)

### Fixed
- Repairing files placed at long path (#1369)
- Fix download directory recreation after removing it during patcher execution (#1360)

## [3.14.0.0]
### Added
- Automated scripting runtime changing to .NET 3.5 on Unity 2017 or newer (#1241)
- Automated Unity API compatiblity change from .NET 2.0 subset to .NET 2.0 (#1242)
- Displaying proper message when in offline mode (#1217)
- Support for Chinese, Japanese, Korean and all other regions characters (#1171)

### Changed
- Disabled automated update when in offline mode (#1217)
- More descriptive error messages (#1313)
- Disk space error message now tells how much of additional space is needed (#1314)

### Fixed
- Unity 2017 and newer compilation issues (#1243)
- Cancelling update (#1222)
- Displaying transparent banner images (#1249)
- Patcher would always uninstall an old version of the game for repairs, event if it wasn't broken (#1273)
- Patcher freeze after uninstalling app (#1276)

## [3.13.0]
### Added
- Speed up unpacking app data with multithreading (#1270)

## [3.12.0]
### Added
- Support for newer versions of Unity (#1191)

### Changed
- New status messages for repairing process (more user-friendly) (#1177)

### Fixed
- Displaying "Stalled" state at the begininng of the download (#1179)
- Using repair strategy when content of currently installed version is not available (#1190)
- Starting game when patcher is in offline mode (#1201)
- A launch script on Linux platforms (#1199)

## [3.11.0]
### Added
- Support for LZMA2 compression using XZ
- Sending all events to Statistics Reporting Service
- Support for main_executable and main_executable_args fields in AppVersion
- Added processing of --online or --offline command line argument
- Sending 'patcher_started' event to Statistics Reporting Service
- Custom building options under `Tools/Build`
- Handling failures when accessing the file system
- Attaching "system-info" to Sentry events as tag
- Support for PK_OFFICIAL define
- Displaying "Stalled..." instead of "Downloading package..." if the download speed reaches 0 B/s
- capabilities field in the manifest

### Changed
- Linux launch script
- Download speed will now be displayed based on the average of download speeds in the last 2 seconds
- Patcher will no longer clear the progress bar after updating

### Fixed
- Invalid display of progress value when unarchiving
- Wrapping the GZipStream input to avoid errors
- Fixed all warnings that appear when launching on 5.3.4f1
- Freeze or crash after closing the patcher
- Window size issue on Linux
- Stalling issue due to high request timeout and delays

### Removed
- Torrent downloading
- StandaloneOSXUniversal architecture from building options

## [3.10.3]
### Fixed
- Fix issue with locating screensize file

## [3.10.2]
### Fixed
- An edge case which caused the download to never terminate
- Patcher wouldn't quit upon starting the Launcher
- Patcher wouldn't re-download a manually removed banner file
- Fix issues with incorrect patcher window sizing

### Changed
- Patcher will timeout if the downloading stopped sooner than after 5 minutes

## [3.10.1p3]
### Fixed
- Fix repairing files with invalid version id

## [3.10.1p2]
### Fixed
- Slow download performance due to small buffer size

## [3.10.1p1]
### Fixed
- Missing unpacking suffix when repairing files (3.10.1 failed to fix this issue)

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
