# PatchKit Unity Patcher

* [Documentation](http://docs.patchkit.net/unity_custom_patcher.html)

In this repository you can find PatchKit Patcher Unity project.

## Getting the stable version

It's pretty important to use a stable and tester patcher version. You will find stable releases in [here](https://github.com/patchkit-net/patchkit-patcher-unity/releases). Be careful, **pre-releases** are not suitable for production usage. **Master** branch is also not suitable for production usage. Always look for tags if you want to do an upgrade.

## getdiskspaceosx
The native library is already precompiled.

If you need to recompile it, simply open the project file (`getdiskspaceosx/getdiskspaceosx.xcodeproj`) in XCode and build it. The bundle will be automatically copied into `Assets/Plugins/PatchKit/x86_64`.

## Environment variables

* **PK_PATCHER_FORCE_SECRET** - force certain app secret
* **PK_PATCHER_FORCE_VERSION** - force certain app version
* **PK_PATCHER_KEYS_URL** - change used url of keys API
* **PK_PATCHER_MAIN_URL** - change used url of main API
