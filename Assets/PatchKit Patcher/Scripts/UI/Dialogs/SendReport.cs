using System.Collections;
using PatchKit.Unity.Patcher.Debug;
using UnityEngine;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.UI;

namespace PatchKit_Patcher.Scripts.UI.Dialogs
{
    public class SendReport : MonoBehaviour
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(SendReport));

        private Rect _popUp, _sendReportMenu;
        private bool _show;
        private bool _showPopUp;
        private string _message;
        private readonly bool _isSended = false;
        private bool _sending = true;
        int tmp = 0; //test
        private bool _wait;
        private GraphicRaycaster _graphicRaycaster;

        private new string name;
        private string _email;
        private string _description;

        void Start()
        {
            int windowWidth = 250;
            int windowHeight = 300;
            int x = (Screen.width - windowWidth) / 2;
            int y = (Screen.height - windowHeight) / 2;
            int yPopUp = (Screen.height - 120) / 2;
            _sendReportMenu = new Rect(x, y, windowWidth, windowHeight);
            _popUp = new Rect(x, yPopUp, windowWidth, 120);
            _graphicRaycaster = FindObjectOfType<GraphicRaycaster>();
        }

        void OnGUI()
        {
            if (_show)
            {
                GUI.DrawTexture(_sendReportMenu, Texture2D.whiteTexture);
                GUI.Window(0, _sendReportMenu, Menu, "Send report");
            }
            else if (_showPopUp)
            {
                GUI.DrawTexture(_popUp, Texture2D.whiteTexture);
                GUI.Window(1, _popUp, PopUp, "Information");
            }
        }

        void Menu(int i)
        {
            GUILayout.Label("Your Name:");

            name = GUILayout.TextField(name);

            GUILayout.Label("Contact e-mail address:");

            _email = GUILayout.TextField(_email);

            GUILayout.Label("Please describe in details what actions did you take:");

            _description = GUILayout.TextField(_description, GUILayout.Height(100));

            GUILayout.BeginHorizontal("box");
            if (GUILayout.Button("Send Report"))
            {
                _show = false;
                _showPopUp = true;
            }

            if (GUILayout.Button("Cancel"))
            {
                _show = false;
                _graphicRaycaster.enabled = true;
            }

            GUILayout.EndHorizontal();
        }

        public IEnumerator Wait()
        {
            _wait = true;
            yield return new WaitForSeconds(1);
            _wait = false;
            _message += ".";
            if (_message.Length > 3)
            {
                _message = ".";
            }

            if (tmp > 10)
            {
                _sending = false;

                if (_isSended)
                    _message = "The report has been sent";
                else
                    _message = "The report could not be sent";
            }
            
            tmp++;
        }

        void PopUp(int i)
        {
            if (_sending)
            {
                if (!_wait)
                    StartCoroutine(Wait());
                GUILayout.Label(_message, new GUIStyle() {fontSize = 40, alignment = TextAnchor.MiddleCenter},
                    GUILayout.Height(60));
            }
            else
            {
                GUILayout.Label(_message, GUILayout.Height(60));
                if (_isSended)
                {
                    if (GUILayout.Button("Ok"))
                    {
                        _showPopUp = false;
                        _graphicRaycaster.enabled = true;
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal("box");
                    if (GUILayout.Button("Try Again"))
                    {
                        _sending = true;
                        tmp = 0;
                        _message = ".";
                    }

                    if (GUILayout.Button("Cancel"))
                    {
                        _showPopUp = false;
                        _graphicRaycaster.enabled = true;
                    }

                    GUILayout.EndHorizontal();
                }
            }
        }

        public void ShowReport()
        {
            _show = true;
            _graphicRaycaster.enabled = false;
        }
    }
}
