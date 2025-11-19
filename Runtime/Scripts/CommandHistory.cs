using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nela.Flux {
    public class CommandHistory {
        private List<string> _commands;
        private int _cursorIndex = 0;

        public CommandHistory() {
            _commands = new List<string>(PlayerPrefs.GetString("nela.flux-history", string.Empty).Split('\n'));
            _commands.RemoveAll(string.IsNullOrEmpty);
        }

        public void ResetCursor() {
            _cursorIndex = 0;
        }

        public void Add(string command) {
            if (_commands.Count > 0 && _commands[^1] == command) return; // avoid repetition
            _commands.Add(command);
        }

        public string Older(string hint) {
            var originalIndex = _cursorIndex;
            _cursorIndex++;
            while (_cursorIndex <= _commands.Count && !_commands[^_cursorIndex].StartsWith(hint))
                _cursorIndex++;
            if (_cursorIndex > _commands.Count) {
                _cursorIndex = originalIndex;
                if (_cursorIndex == 0)
                    return hint;
            }

            return _commands[^_cursorIndex];
        }

        public string Newer(string hint) {
            var originalIndex = _cursorIndex;
            _cursorIndex--;
            while (_cursorIndex > 0 && !_commands[^_cursorIndex].StartsWith(hint))
                _cursorIndex--;
            if (_cursorIndex <= 0) {
                _cursorIndex = originalIndex;
                if (_cursorIndex == 0)
                    return hint;
            }

            return _commands[^_cursorIndex];
        }

        public void MakePersistent(int maxHistory) {
            var historyString = new StringBuilder();
            for (int i = Mathf.Max(0, _commands.Count - maxHistory); i < _commands.Count; i++) {
                historyString.Append(_commands[i]);
                historyString.Append("\n");
            }

            PlayerPrefs.SetString("nela.flux-history", historyString.ToString());
            PlayerPrefs.Save();
        }
    }
}