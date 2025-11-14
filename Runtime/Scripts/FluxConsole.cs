using System;
using System.Text;
using UnityEngine;

namespace Nela.Flux {
    public class FluxConsole : MonoBehaviour {
        private const int MAX_COMMAND_HISTORY = 256;
        private const int MAX_OUTPUT_HISTORY_SIZE = 8192;
        private const int MAX_OUTPUT_HISTORY_SIZE_MARGIN = 512;
        private const int BOTTOM_SPACE = 330;
        private static readonly string[] PREFERRED_FONTS = new[]
        {
            "Monaco",
            "Consolas",
            "SF Mono",
            "DejaVu Sans Mono",
            "Roboto Mono"
        };

        private static FluxConsole _console;

        // resources
        private static Texture2D _backgroundTexture;
        private static GUIStyle _inputTextStyle;
        private static GUIStyle _historyStyle;
        private static GUIStyle _promptStyle;
        private static GUIStyle _scrollBarStyle;
        private static GUIStyle _scrollBarThumbStyle;
        private static GUIStyle _scrollBarUpButtonStyle;
        private static GUIStyle _scrollBarDownButtonStyle;
        private static GUISkin _skin;

        private bool _isOpen;
        private string _inputText = string.Empty;
        private string _inputNavHint = string.Empty;

        private StringBuilder _outputHistory = new StringBuilder($"<b>Flux Console</b>\n<color=#70ff90><i>{DateTime.Now}</i></color>\n");
        private string _outputCache;
        private bool _outputDirty;
        private Vector2 _scrollPosition;
        private CommandHistory _commandHistory;

        public FluxConsole() {
            _outputHistory.EnsureCapacity(MAX_OUTPUT_HISTORY_SIZE + MAX_OUTPUT_HISTORY_SIZE_MARGIN);
            Flush();
        }

        private void Awake() {
            _commandHistory = new CommandHistory();
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.BackQuote) && Input.GetKey(KeyCode.LeftControl)) {
                this.Toggle();
            }
        }

        private void OnDestroy() {
            _commandHistory.MakePersistent(MAX_COMMAND_HISTORY);
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

            if (_outputDirty) Flush();

            var originalSkin = GUI.skin;
            GUI.skin = _skin;
            
            var currentEvent = Event.current;

            if (currentEvent.type == EventType.KeyDown) {
                if (currentEvent.keyCode == KeyCode.BackQuote && currentEvent.control) {
                    Toggle();
                    currentEvent.Use();
                }

                if (currentEvent.keyCode == KeyCode.Return) {
                    Submit(_inputText);
                    currentEvent.Use();
                }

                if (currentEvent.keyCode == KeyCode.Tab) {
                    var newInput = TabComplete(_inputText);
                    if (newInput != _inputText) {
                        SetNewInput(newInput);
                        _inputNavHint = _inputText;
                    }
                }

                if (currentEvent.keyCode == KeyCode.UpArrow) {
                    SetNewInput(_commandHistory.Older(_inputNavHint));
                    currentEvent.Use();
                }

                if (currentEvent.keyCode == KeyCode.DownArrow) {
                    SetNewInput(_commandHistory.Newer(_inputNavHint));
                    currentEvent.Use();
                }

                if (currentEvent.keyCode == KeyCode.C && currentEvent.control) {
                    _inputText = string.Empty;
                    currentEvent.Use();
                }
            }

            if (currentEvent.type == EventType.ScrollWheel) {
                _scrollPosition += currentEvent.delta * 10;
                currentEvent.Use();
            }

            var historyRect = new Rect(0, 0, Screen.width, Screen.height - BOTTOM_SPACE);
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

            GUI.Label(new Rect(0, 0, contentViewWidth, contentViewHeight), _outputCache, _historyStyle);
            GUI.EndScrollView();
            GUI.SetNextControlName("Command");

            var originalChanged = GUI.changed;
            GUI.changed = false;

            // draw prompt
            var prompt = GetCurrentPrompt();
            var promptWidth = _historyStyle.CalcSize(new GUIContent(prompt)).x - 2;
            GUI.Label(new Rect(0, Screen.height - BOTTOM_SPACE, promptWidth, 24), prompt, _promptStyle);

            _inputText = GUI.TextField(new Rect(promptWidth, Screen.height - BOTTOM_SPACE, Screen.width - promptWidth, 24), _inputText, _inputTextStyle);
            if (GUI.changed) {
                _inputNavHint = _inputText;
                _commandHistory.ResetCursor();
            }
            GUI.changed = originalChanged;

            GUI.FocusControl("Command"); // always focus on the input field

            GUI.skin = originalSkin;
        }

        private void SetNewInput(string newInput) {
            var textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if (textEditor != null) {
                textEditor.text = _inputText = newInput;
                textEditor.selectIndex = textEditor.cursorIndex = _inputText.Length;
            }
        }

        private void Submit(string command) {
            if (command == "") return;
            Output($"<color=#70ff90><i>{DateTime.Now.ToShortTimeString()}</i></color> <b>></b> {command}\n");

            Flush();

            ExecuteCommand(command);

            _commandHistory.Add(_inputText);
            _inputNavHint = _inputText = string.Empty;
            _commandHistory.ResetCursor();
        }

        private string TabComplete(string inputText) {
            foreach (var com in CommandCache.GetAllCommands()) {
                if (com.name.StartsWith(inputText)) {
                    return com.name;
                }
            }
            return inputText;
        }

        private void Toggle() {
            _isOpen = !_isOpen;
        }

        private void ScrollToBottom() {
            _scrollPosition = Vector2.zero;
        }

        private void Flush() {
            lock (_outputHistory) {
                if (_outputHistory[^1] == '\n') {
                    _outputCache = _outputHistory.ToString(0, _outputHistory.Length - 1);
                } else {
                    _outputCache = _outputHistory.ToString();
                }

                _outputDirty = false;
            }

            ScrollToBottom();
        }

        private string GetCurrentPrompt() {
            return $"<color=#70ff90><i>{DateTime.Now.ToShortTimeString()}</i></color> <b>></b> ";
        }

        /// <summary>
        /// Write output to the console. Thread-safe.
        /// </summary>
        public void Output(string content) {
            lock (_outputHistory) {
                _outputDirty = true;
                var outputHistory = _outputHistory;
                outputHistory.Append(content);
                if (outputHistory.Length > MAX_OUTPUT_HISTORY_SIZE + MAX_OUTPUT_HISTORY_SIZE_MARGIN) {
                    outputHistory.Remove(0, outputHistory.Length - MAX_OUTPUT_HISTORY_SIZE);
                }
            }
        }

        public void Error(string message) {
            Output($"<b><color=#ff0000>Error</color></b>: {message}\n");
        }

        public static bool isOpen => _console != null && _console._isOpen;

        public static void ExecuteCommand(string commandLine) {
            var tokenizer = new CommandTokenizer(commandLine);
            if (tokenizer.TryNextToken(out var command)) {
                var com = CommandCache.FindCommand(command);
                if (com != null) {
                    try {
                        com.Execute(new CommandContext(_console, tokenizer));
                    }
                    catch (Exception e) {
                        _console.Error(e.ToString());
                    }
                } else {
                    _console.Error($"Can't find command {command}");
                }
            }
        }

        [RuntimeInitializeOnLoadMethod]
        private static void StartUp() {
            CreateResources();

            var fluxConsoleObj = new GameObject("FluxConsole");
            fluxConsoleObj.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(fluxConsoleObj);
            _console = fluxConsoleObj.AddComponent<FluxConsole>();
        }

        private static void CreateResources() {
            var font = CreateMonospaceFont(16);

            _backgroundTexture = new Texture2D(1, 1);
            _backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.5f));
            _backgroundTexture.Apply();

            _inputTextStyle = new GUIStyle();
            _inputTextStyle.normal.background = _backgroundTexture;
            _inputTextStyle.normal.textColor = Color.white;
            _inputTextStyle.padding = new RectOffset(0, 4, 0, 0);
            _inputTextStyle.fontSize = 16;
            if (font != null) _inputTextStyle.font = font;

            _historyStyle = new GUIStyle();
            _historyStyle.alignment = TextAnchor.LowerLeft;
            _historyStyle.normal.textColor = Color.white;
            _historyStyle.wordWrap = true;
            _historyStyle.richText = true;
            _historyStyle.padding = new RectOffset(4, 4, 0, 4);
            _historyStyle.fontSize = 16;
            if (font != null) _historyStyle.font = font;

            _promptStyle = new GUIStyle(_historyStyle);
            _promptStyle.normal.background = _backgroundTexture;
            _promptStyle.alignment = TextAnchor.UpperLeft;
            _promptStyle.padding = new RectOffset(4, 0, 0, 0);

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

        private static Font CreateMonospaceFont(int size) {
            var fonts = Font.GetOSInstalledFontNames();
            foreach (var font in PREFERRED_FONTS) {
                if (Array.IndexOf(fonts, font) != -1)
                    return Font.CreateDynamicFontFromOSFont(font, size);
            }

            return null;
        }
    }
}