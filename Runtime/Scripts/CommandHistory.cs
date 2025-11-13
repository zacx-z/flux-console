using System.Collections.Generic;
using UnityEngine;

namespace Nela.Flux {
    public class CommandHistory {
        private List<string> _commands = new List<string>();
        private int _cursorIndex = -1;

        public CommandHistory() {
        }

        public void ResetCursor() {
            _cursorIndex = 0;
        }

        public void Add(string command) {
            _commands.Add(command);
        }

        public string Older(string hint) {
            _cursorIndex++;
            if (_cursorIndex >= _commands.Count) {
                _cursorIndex = _commands.Count;
                if (_cursorIndex == 0)
                    return hint;
            }

            return _commands[^_cursorIndex];
        }

        public string Newer(string hint) {
            _cursorIndex--;
            if (_cursorIndex <= 0) {
                _cursorIndex = 0;
                return hint;
            }

            return _commands[^_cursorIndex];
        }

    }
}