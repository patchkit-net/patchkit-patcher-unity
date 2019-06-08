using System.Linq;
using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

public static class AppPlayerPrefs
{
    [NotNull]
    private static string GetHashedAppSecret([NotNull] string appSecret)
    {
        using (var md5 = MD5.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(s: appSecret);
            return string.Join(
                separator: string.Empty,
                value: md5.ComputeHash(buffer: bytes)
                    .Select(selector: b => b.ToString(format: "x2"))
                    .ToArray());
        }
    }

    [NotNull]
    private static string GetFormattedKey(
        [NotNull] string appSecret,
        [NotNull] string key)
    {
        return $"{GetHashedAppSecret(appSecret: appSecret)}-{key}";
    }

    public static void SetString(
        [NotNull] string appSecret,
        [NotNull] string key,
        string value)
    {
        PlayerPrefs.SetString(
            key: GetFormattedKey(
                appSecret: appSecret,
                key: key),
            value: value);
    }

    public static string GetString(
        [NotNull] string appSecret,
        [NotNull] string key)
    {
        return PlayerPrefs.GetString(
            key: GetFormattedKey(
                appSecret: appSecret,
                key: key),
            defaultValue: null);
    }
}