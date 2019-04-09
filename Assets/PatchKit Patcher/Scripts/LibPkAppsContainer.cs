using System;
using Autofac;
using PatchKit.Core;
using PatchKit.Core.CSharp;
using UnityEngine;

namespace PatchKit_Patcher.Scripts
{
    public static class LibPkAppsContainer
    {
        private static IContainer _container;

        public static PlatformType GetPlatformType()
        {
            bool is64Bit = IntPtr.Size == 8;

            if (Application.platform == RuntimePlatform.LinuxEditor ||
                Application.platform == RuntimePlatform.LinuxPlayer)
            {
                return is64Bit ? PlatformType.Linux64bit : PlatformType.Linux86bit;
            }

            if (Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.WindowsPlayer)
            {
                return is64Bit ? PlatformType.Win64bit : PlatformType.Win86bit;
            }

            if (Application.platform == RuntimePlatform.OSXEditor ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                return PlatformType.OSX64bit;
            }

            throw new InvalidOperationException("Not supported platform.");
        }

        static LibPkAppsContainer()
        {
            ContainerBuilder builder = new ContainerBuilder();

            var platformType = GetPlatformType();

            int indentLevel = 0;
            string indent = "";

            int disabledLevel = 0;
            bool disabled = false;

            Action refreshIndent = () =>
            {
                indent = "";
                for (int i = 0; i < indentLevel; i++)
                {
                    indent += " ";
                }
            };

            PatchKit.Core.Properties.AssemblyModule coreModule = new PatchKit.Core.Properties.AssemblyModule(
                platformType, x =>
                {
                    if (disabled)
                        return;
                    string str;
                    switch (x.Type)
                    {
                        case LogMessageType.Trace:
                            str = "[TRACE]";
                            break;
                        case LogMessageType.Info:
                            str = "[ LOG ]";
                            break;
                        case LogMessageType.Warning:
                            str = "[ WAR ]";
                            break;
                        case LogMessageType.Error:
                            str = "[ERROR]";
                            break;
                        default:
                            str = "[?????]";
                            break;
                    }

                    Debug.Log(str + " " + x.Message);

                    if (x.Exception == null)
                        return;

                    Debug.LogException(x.Exception);
                }, enabled =>
                {
                    ++indentLevel;
                    if (!enabled)
                        ++disabledLevel;

                    refreshIndent();

                    disabled = disabledLevel > 0;
                }, enabled =>
                {
                    --indentLevel;
                    if (!enabled)
                        --disabledLevel;

                    refreshIndent();

                    disabled = disabledLevel > 0;
                });

            PatchKit.Network.Properties.AssemblyModule networkModule =
                new PatchKit.Network.Properties.AssemblyModule(coreModule);

            PatchKit.Api.Properties.AssemblyModule apiModule =
                new PatchKit.Api.Properties.AssemblyModule(
                    //Settings.GetMainApiConnectionSettings(),
                    //Settings.GetKeysApiConnectionSettings(),
                    coreModule,
                    networkModule);

            PatchKit.Apps.Properties.AssemblyModule
                appsModule = new PatchKit.Apps.Properties.AssemblyModule(coreModule);

            PatchKit.Apps.Updating.Properties.AssemblyModule appsUpdatingModule =
                new PatchKit.Apps.Updating.Properties.AssemblyModule(coreModule, networkModule, appsModule);

            PatchKit.Librsync.Properties.AssemblyModule librsyncModule =
                new PatchKit.Librsync.Properties.AssemblyModule();

            builder.RegisterModule(coreModule);
            builder.RegisterModule(networkModule);
            builder.RegisterModule(apiModule);
            builder.RegisterModule(librsyncModule);
            builder.RegisterModule(appsModule);
            builder.RegisterModule(appsUpdatingModule);

            _container = builder.Build();
        }

        public static T Resolve<T>()
        {
            return _container.Resolve<T>();
        }
    }
}