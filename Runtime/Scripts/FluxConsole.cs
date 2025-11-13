using System.Text;
using UnityEngine;

namespace Nela.Flux {
    public class FluxConsole : MonoBehaviour {
        private const int MAX_HISTORY_LENGTH = 2048;
        private const int MAX_HISTORY_LENGTH_MARGIN = 256;
        private static FluxConsole _console;

        // resources
        private static Texture2D _backgroundTexture;
        private static GUIStyle _inputTextStyle;
        private static GUIStyle _historyStyle;
        private static GUIStyle _scrollBarStyle;
        private static GUIStyle _scrollBarThumbStyle;
        private static GUIStyle _scrollBarUpButtonStyle;
        private static GUIStyle _scrollBarDownButtonStyle;
        private static GUISkin _skin;

        private bool _isOpen;
        private string _inputText;

        private StringBuilder _outputHistory = new StringBuilder("Flux Console\n\n");
        private string _outputCache;
        private Vector2 _scrollPosition;

        public FluxConsole() {
            _outputHistory.EnsureCapacity(MAX_HISTORY_LENGTH + MAX_HISTORY_LENGTH_MARGIN);
            FlushOutput();
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.BackQuote) && Input.GetKey(KeyCode.LeftControl)) {
                this.Toggle();
            }
        }

        private void OnGUI() {
            if (!_isOpen) return;

            if (_skin == null) {
                _skin = Instantiate(GUI.skin);
                _skin.customStyles = new[]
                {
                    _scrollBarStyle,
                    _scrollBarThumbStyle,
                    _scrollBarUpButtonStyle,
                    _scrollBarDownButtonStyle,
                };
            }
            
            var originalSkin = GUI.skin;
            GUI.skin = _skin;

            if (Event.current.type == EventType.KeyDown) {
                if (Event.current.keyCode == KeyCode.BackQuote && Event.current.control) {
                    Toggle();
                    Event.current.Use();
                }

                if (Event.current.keyCode == KeyCode.Return) {
                    Submit(_inputText);
                    _inputText = "";
                    Event.current.Use();
                }

                if (Event.current.keyCode == KeyCode.Tab) {
                    TabComplete(_inputText);
                    Event.current.Use();
                }
            }

            if (Event.current.type == EventType.ScrollWheel) {
                _scrollPosition += Event.current.delta * 10;
                Event.current.Use();
            }

            var historyRect = new Rect(0, 0, Screen.width, Screen.height - 330);
            GUI.DrawTexture(historyRect, _backgroundTexture, ScaleMode.StretchToFill, true);

            var contentViewWidth = Screen.width - 8;
            var contentViewHeight = _historyStyle.CalcHeight(new GUIContent(_outputCache), contentViewWidth);
            contentViewHeight = Mathf.Max(contentViewHeight, historyRect.height);

            _scrollPosition.y += contentViewHeight;

            _scrollPosition = GUI.BeginScrollView(historyRect, _scrollPosition,
                new Rect(0, 0, contentViewWidth, contentViewHeight)
                ,false, true,
                GUIStyle.none, _scrollBarStyle);

            _scrollPosition.y -= contentViewHeight;

            GUI.Label(new Rect(4, 0, contentViewWidth - 4, contentViewHeight), _outputCache, _historyStyle);
            GUI.EndScrollView();
            GUI.SetNextControlName("Command");
            _inputText = GUI.TextField(new Rect(0, Screen.height - 330, Screen.width, 24), _inputText, _inputTextStyle);

            GUI.FocusControl("Command"); // always focus on the input field

            GUI.skin = originalSkin;
        }

        private void Submit(string command) {
            if (command == "") return;
            _outputHistory.Append("<b>></b> ");
            _outputHistory.AppendLine(command);
            FlushOutput();

            ExecuteCommand(command);
        }

        private string TabComplete(string inputText) {
            return inputText;
        }

        private void Toggle() {
            _isOpen = !_isOpen;
        }

        private void ScrollToBottom() {
            _scrollPosition = Vector2.zero;
        }

        public void FlushOutput() {
            _outputCache = _outputHistory.ToString();
            ScrollToBottom();
        }

        public void Output(string content, bool flush = true) {
            var outputHistory = _outputHistory;
            outputHistory.Append(content);
            if (outputHistory.Length > MAX_HISTORY_LENGTH + MAX_HISTORY_LENGTH_MARGIN) {
                outputHistory.Remove(0, outputHistory.Length - MAX_HISTORY_LENGTH);
            }
            if (flush) FlushOutput();
        }

        public static bool isOpen => _console != null && _console._isOpen;

        public static void ExecuteCommand(string commandLine) {
            var tokenizer = new CommandTokenizer(commandLine);
            if (tokenizer.TryNextToken(out var command)) {
                var com = CommandCache.FindCommand(command);
                if (com != null) {
                    com.Execute(new CommandContext(_console, tokenizer));
                } else {
                    _console.Output($"<b><color=#ff0000>Error</color></b>: Can't find command {command}\n");
                }
            }
        }

        public static void Flush() {
            _console.FlushOutput();
        }

        [RuntimeInitializeOnLoadMethod]
        private static void StartUp() {
            CreateResources();

            var fluxConsoleObj = new GameObject("FluxConsole");
            fluxConsoleObj.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(fluxConsoleObj);
            _console = fluxConsoleObj.AddComponent<FluxConsole>();
        }

        private static void CreateResources() {
            _backgroundTexture = new Texture2D(1, 1);
            _backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.5f));
            _backgroundTexture.Apply();

            _inputTextStyle = new GUIStyle();
            _inputTextStyle.normal.background = _backgroundTexture;
            _inputTextStyle.normal.textColor = Color.white;
            _inputTextStyle.padding = new RectOffset(4, 4, 0, 0);
            _inputTextStyle.fontSize = 16;

            _historyStyle = new GUIStyle();
            _historyStyle.alignment = TextAnchor.LowerLeft;
            _historyStyle.normal.textColor = Color.white;
            _historyStyle.richText = true;
            _historyStyle.fontSize = 16;

            _scrollBarStyle = new GUIStyle();
            _scrollBarStyle.name = "fluxconsoleverticalscrollbar";
            _scrollBarStyle.fixedWidth = 8;

            _scrollBarThumbStyle = new GUIStyle();
            _scrollBarThumbStyle.name = "fluxconsoleverticalscrollbarthumb";
            _scrollBarThumbStyle.fixedWidth = 8;
            _scrollBarThumbStyle.normal.background = _backgroundTexture;

            _scrollBarUpButtonStyle = new GUIStyle(GUIStyle.none);
            _scrollBarUpButtonStyle.name = "fluxconsoleverticalscrollbarupbutton";

            _scrollBarDownButtonStyle = GUIStyle.none;
            _scrollBarDownButtonStyle.name = "fluxconsoleverticalscrollbardownbutton";
        }
    }
}