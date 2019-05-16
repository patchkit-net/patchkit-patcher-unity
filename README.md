# PatchKit Unity Patcher

* [Documentation](http://docs.patchkit.net/unity_custom_patcher.html)

In this repository you can find PatchKit Patcher Unity project.

## Getting the stable version

It's pretty important to use a stable and tester patcher version. You will find stable releases in [here](https://github.com/patchkit-net/patchkit-patcher-unity/releases). Be careful, **pre-releases** are not suitable for production usage. **Master** branch is also not suitable for production usage. Always look for tags if you want to do an upgrade.

## Executing without the Launcher
Sometimes, usually for the purpose of testing, we might want to start the Patcher without using the Launcher. It's possible through the use of 2 command line options `--secret` and `--installdir`, and most of the time also `--readable` which forces the Patcher to interpret given secret without decoding it.

Example:
```
> Patcher.exe --readable --secret 123 --installdir "directory/to/install/the/app/in"
```


## getdiskspaceosx
The native library is already precompiled.

If you need to recompile it, simply open the project file (`getdiskspaceosx/getdiskspaceosx.xcodeproj`) in XCode and build it. The bundle will be automatically copied into `Assets/Plugins/PatchKit/x86_64`.

## Environment variables

* **PK_PATCHER_FORCE_SECRET** - force certain app secret
* **PK_PATCHER_FORCE_VERSION** - force certain app version
* **PK_PATCHER_API_URL** - change used url of API
* **PK_PATCHER_API_CACHE_URL** - changed used url of API cache
* **PK_PATCHER_KEYS_URL** - change used url of keys API
* **PK_PATCHER_KEEP_FILES_ON_ERROR** - keep temporary directories and files in case of patcher error

## Troubleshooting

### My patcher is opening in fullscreen mode

Head to `File/Build settings...` and click `Player Settings`. Now, open section titled `Resolution and Presentation` and make sure that `Fullscreen Mode` is set to **Windowed**. There you can also change the default resolution of the patcher, by modifying `Default Screen Width/Height`. You might also want to set `Resizable Window` to off.

### My patcher is not working when I minimize it

Head to `File/Build settings...` and click `Player Settings`. Now, open section titled `Resolution and Presentation` and make sure that `Run In Background` is turned on.

### My patcher is displaying *Resolution Dialog*

Head to `File/Build settings...` and click `Player Settings`. Now, open section titled `Resolution and Presentation` and make sure that `Display Resolution Dialog` is set to `Disabled`.
