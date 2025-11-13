using System.Text;

namespace Nela.Flux {
    public class CommandTokenizer {
        private readonly string _line;
        private int _current;
        private StringBuilder _stringCache;

        public CommandTokenizer(string line) {
            _line = line;
            _current = 0;
        }

        public bool TryNextToken(out string token) {
            _stringCache ??= new StringBuilder();

            while (_current < _line.Length && char.IsWhiteSpace(_line[_current])) _current++;
            if (_current >= _line.Length) {
                token = null;
                return false;
            }

            _stringCache.Clear();
            while (_current < _line.Length && !char.IsWhiteSpace(_line[_current])) {
                _stringCache.Append(_line[_current]);
                _current++;
            }

            token = _stringCache.ToString();

            // move to the beginning of the next token
            while (_current < _line.Length && char.IsWhiteSpace(_line[_current])) _current++;

            return true;
        }

        public string ReadLine() {
            var result = _line.Substring(_current);
            _current = _line.Length;
            return result;
        }
    }
}