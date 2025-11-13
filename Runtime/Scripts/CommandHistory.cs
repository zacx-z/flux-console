using System.Collections.Generic;

namespace Nela.Flux {
    public class CommandHistory {
        private List<string> _commands = new List<string>();
        private int _cursorIndex = 0;

        public CommandHistory() {
        }

        public void ResetCursor() {
            _cursorIndex = 0;
        }

        public void Add(string command) {
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

    }
}