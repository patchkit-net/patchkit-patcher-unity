using UnityEditor;
using UnityEngine;

namespace PatchKit.Unity.Editor
{
public class InternalSettings : EditorWindow
{
    private const string EmailKey = "pk_as_email";
    private const string PasswordKey = "pk_as_password";

    public static string Email
    {
        get { return EditorPrefs.GetString(EmailKey); }
        set { EditorPrefs.SetString(EmailKey, value); }
    }

    public static string Password
    {
        get { return EditorPrefs.GetString(PasswordKey); }
        set { EditorPrefs.SetString(PasswordKey, value); }
    }

    public static void Show()
    {
        EditorWindow window = GetWindow<InternalSettings>();
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Provide Asset Store username and password.");

        Email = EditorGUILayout.TextField("Email", Email);
        Password = EditorGUILayout.PasswordField("Password", Password);
    }
}

} // namespace