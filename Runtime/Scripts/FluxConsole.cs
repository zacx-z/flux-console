using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Nela.Flux {
    public class FluxConsole : MonoBehaviour {
        private const int MAX_OUTPUT_HISTORY_SIZE_MARGIN = 512;
        private static readonly Color DEFAULT_BACKGROUND_COLOR = new Color(0, 0, 0, 0.5f);
        private static readonly Color DEFAULT_TEXT_COLOR = new Color(1, 1, 1);
        private static readonly string[] PREFERRED_FONTS = new[]
        {
            "Monaco",
            "Consolas",
            "SF Mono",
            "DejaVu Sans Mono",
            "Roboto Mono"
        };

        private static FluxConsole _console;

        private static FluxSettings _settings;
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
        private Task _currentTask;
        private InputHandler _inputHandler;

        public FluxConsole() {
            _outputHistory.EnsureCapacity(_settings.outputBufferSize + MAX_OUTPUT_HISTORY_SIZE_MARGIN);
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
            _commandHistory.MakePersistent(_settings.historySize);
        }

        private void OnGUI() {
            if (!_isOpen) return;
            if (_inputHandler != null && _inputHandler.canceled) {
                _inputHandler.Cancel();
                _inputHandler = null;
            }

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

                if (currentEvent.keyCode == KeyCode.D && currentEvent.control && _inputText == string.Empty) {
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

            var margin = _settings.margin;
            const int LINE_HEIGHT = 24;

            var historyRect = new Rect(margin.left, margin.top,
                Screen.width - margin.left - margin.right,
                Screen.height - margin.top - margin.bottom - LINE_HEIGHT);
            GUI.DrawTexture(historyRect, _backgroundTexture, ScaleMode.StretchToFill, true);

            var contentViewWidth = Screen.width - 8 - margin.left - margin.right;
            var contentViewHeight = _historyStyle.CalcHeight(new GUIContent(_outputCache), contentViewWidth);
            contentViewHeight = Mathf.Max(contentViewHeight, historyRect.height);

            _scrollPosition.y += contentViewHeight;

            _scrollPosition = GUI.BeginScrollView(historyRect, _scrollPosition,
                new Rect(0, 0, contentViewWidth, contentViewHeight)
                ,false, true,
                GUIStyle.none, _scrollBarStyle);

            _scrollPosition.y -= contentViewHeight;

            GUI.Label(new Rect(margin.left, margin.top, contentViewWidth, contentViewHeight), _outputCache, _historyStyle);
            GUI.EndScrollView();
            GUI.SetNextControlName("Command");

            if (_currentTask == null || _currentTask.Status != TaskStatus.Running) {
                var originalChanged = GUI.changed;
                GUI.changed = false;

                // draw prompt
                var prompt = GetCurrentPrompt();
                var promptWidth = _historyStyle.CalcSize(new GUIContent(prompt)).x - 2;
                GUI.Label(new Rect(margin.left, Screen.height - margin.bottom - LINE_HEIGHT, promptWidth, LINE_HEIGHT),
                    prompt, _promptStyle);

                _inputText = GUI.TextField(new Rect(promptWidth, Screen.height - margin.bottom - LINE_HEIGHT
                        , Screen.width - promptWidth - margin.left - margin.right, LINE_HEIGHT)
                    , _inputText, _inputTextStyle);
                if (GUI.changed) {
                    _inputNavHint = _inputText;
                    _commandHistory.ResetCursor();
                }

                GUI.changed = originalChanged;

                GUI.FocusControl("Command"); // always focus on the input field
            }

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
            if (_inputHandler != null) {
                Output($"{GetCurrentPrompt()}{command}\n");

                var handler = _inputHandler;
                _inputHandler = null;
                _commandHistory.ResetCursor();

                handler.Handle(command);

                _inputNavHint = _inputText = string.Empty;
                return;
            }

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
            string promptText;
            if (_inputHandler != null) {
                promptText = _inputHandler.prompt;
            } else {
                promptText = DateTime.Now.ToShortTimeString();
            }
            return $"<color=#70ff90><i>{promptText}</i></color> <b>></b> ";
        }

        /// <summary>
        /// Write output to the console. Thread-safe.
        /// </summary>
        public void Output(string content) {
            lock (_outputHistory) {
                _outputDirty = true;
                var outputHistory = _outputHistory;
                outputHistory.Append(content);
                if (outputHistory.Length > _settings.outputBufferSize + MAX_OUTPUT_HISTORY_SIZE_MARGIN) {
                    outputHistory.Remove(0, outputHistory.Length - _settings.outputBufferSize);
                }
            }
        }

        public void Error(string message) {
            Output($"<b><color=#ff0000>Error</color></b>: {message}\n");
        }

        public void Attach(Task task, string label) {
            _currentTask = task;
        }

        public void SetInputHandler(InputHandler inputHandler) {
            if (_inputHandler != null) _inputHandler.Cancel();
            _inputHandler = inputHandler;
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
            var userProfileDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _settings = GetDefaultSettings();
            TryLoadSettings(Path.Combine(userProfileDirectory, ".config/flux-console.json"), ref _settings);
            TryLoadSettings(Path.Combine(Directory.GetCurrentDirectory(), ".flux-console.json"), ref _settings);

            CreateResources();

            var fluxConsoleObj = new GameObject("FluxConsole");
            fluxConsoleObj.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(fluxConsoleObj);
            _console = fluxConsoleObj.AddComponent<FluxConsole>();
        }

        private static FluxSettings GetDefaultSettings() {
            return new FluxSettings()
            {
                margin = new FluxSettings.Margin()
                {
                    bottom = 300
                },
                backgroundColor = ColorUtility.ToHtmlStringRGBA(DEFAULT_BACKGROUND_COLOR),
                textColor = ColorUtility.ToHtmlStringRGBA(DEFAULT_TEXT_COLOR),
                fontSize = 16,
                historySize = 256,
                outputBufferSize = 8192,
            };
        }

        /// <summary>
        /// Load and overwrite the settings object.
        /// </summary>
        private static bool TryLoadSettings(string path, ref FluxSettings settings) {
            if (File.Exists(path)) {
                var content = File.ReadAllText(path);
                try {
                    JsonUtility.FromJsonOverwrite(content, settings);
                    return true;
                }
                catch (Exception e) {
                    Debug.LogException(e);
                }
            }
            return false;
        }

        private static void CreateResources() {
            var font = CreateMonospaceFont(16);
            font.hideFlags = HideFlags.HideAndDontSave;

            _backgroundTexture = new Texture2D(1, 1);
            _backgroundTexture.SetPixel(0, 0, FluxSettings.GetColor(_settings.backgroundColor, DEFAULT_BACKGROUND_COLOR));
            _backgroundTexture.Apply();

            Color textColor = FluxSettings.GetColor(_settings.textColor, DEFAULT_TEXT_COLOR);

            _inputTextStyle = new GUIStyle();
            _inputTextStyle.normal.background = _backgroundTexture;
            _inputTextStyle.normal.textColor = textColor;
            _inputTextStyle.padding = new RectOffset(0, 4, 0, 0);
            _inputTextStyle.fontSize = _settings.fontSize;
            if (font != null) _inputTextStyle.font = font;

            _historyStyle = new GUIStyle();
            _historyStyle.alignment = TextAnchor.LowerLeft;
            _historyStyle.normal.textColor = textColor;
            _historyStyle.wordWrap = true;
            _historyStyle.richText = true;
            _historyStyle.padding = new RectOffset(4, 4, 0, 4);
            _historyStyle.fontSize = _settings.fontSize;
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

        public class InputHandler {
            private readonly string _prompt;
            private readonly Action<string> _handler;
            private readonly CancellationToken _cancellationToken;
            private readonly Action _onCanceled;

            public InputHandler(string prompt, Action<string> handler, CancellationToken cancellationToken,
                Action onCanceled) {
                _prompt = prompt;
                _handler = handler;
                _cancellationToken = cancellationToken;
                _onCanceled = onCanceled;
            }

            public string prompt => _prompt;
            public bool canceled => _cancellationToken.IsCancellationRequested;

            public void Handle(string command) {
                _handler.Invoke(command);
            }

            public void Cancel() {
                _onCanceled?.Invoke();
            }
        }
    }
}