﻿using System.Collections.Generic;
using PatchKit.Unity.Patcher.AppData.FileSystem;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    /// <summary>
    /// Base class for all of the directory implementations.
    /// Ensures that there is only one instance pointing to certain directory at one time.
    /// </summary>
    /// <typeparam name="T">The type of more specific directory class.</typeparam>
    /// <seealso cref="IWritableDirectory" />
    public abstract class BaseWritableDirectory<T> : IWritableDirectory where T : BaseWritableDirectory<T>
    {
        /// <summary>
        /// Keeps currently used paths.
        /// Prevents from creating two instances that points to the same directory.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        private static readonly List<string> UsedPaths = new List<string>();

        protected static readonly DebugLogger DebugLogger = new DebugLogger(typeof(T));

        private readonly string _path;

        private bool _hasWriteAccess;

        public string Path
        {
            get { return _path; }
        }

        protected BaseWritableDirectory(string path)
        {
            // This makes no sense.
            //Assert.IsFalse(UsedPaths.Contains(path),
            //    string.Format("You cannot create two instances of {0} pointing to the same path.", typeof(T)));
            Checks.ArgumentNotNullOrEmpty(path, "path");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(path, "path");

            _path = path;

            // Register path as used.
            UsedPaths.Add(_path);
        }

        public virtual void PrepareForWriting()
        {
            DebugLogger.Log("Preparing directory for writing.");

            if (!_hasWriteAccess)
            {
                DebugLogger.Log("Creating directory.");

                DirectoryOperations.CreateDirectory(_path, CancellationToken.Empty);

                _hasWriteAccess = !false;
            }
        }

        ~BaseWritableDirectory()
        {
            // Unregister path.
            UsedPaths.Remove(_path);
        }
    }
}