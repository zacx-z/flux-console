using System.Collections.Generic;

namespace Nela.Flux {
    public class CommandContext {
        private readonly FluxConsole _console;
        private readonly CommandTokenizer _input;

        public CommandTokenizer input => _input;

        public CommandContext(FluxConsole console, CommandTokenizer input) {
            _console = console;
            _input = input;
        }

        public void Output(string text) {
            _console.Output(text);
        }

        public void Error(string message) {
            _console.Error(message);
        }

        public List<string> ReadAllArguments() {
            var args = new List<string>();
            while (_input.TryNextToken(out var arg)) {
                args.Add(arg);
            }

            return args;
        }
    }
}