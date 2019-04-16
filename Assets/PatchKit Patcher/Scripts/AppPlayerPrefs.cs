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
        [NotNull] string key,
        [NotNull] string appSecret)
    {
        return $"{GetHashedAppSecret(appSecret: appSecret)}-{key}";
    }

    public static void SetString(
        [NotNull] string key,
        [NotNull] string appSecret,
        string value)
    {
        PlayerPrefs.SetString(
            key: GetFormattedKey(
                key: key,
                appSecret: appSecret),
            value: value);
    }

    public static string GetString(
        [NotNull] string key,
        [NotNull] string appSecret)
    {
        return PlayerPrefs.GetString(
            key: GetFormattedKey(
                key: key,
                appSecret: appSecret),
            defaultValue: null);
    }
}