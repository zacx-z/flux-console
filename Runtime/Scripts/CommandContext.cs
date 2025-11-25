using System;
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

        public void Print(string text) {
            _console.Output(text);
        }

        public void Println(string text) {
            _console.Output(text);
            _console.Output("\n");
        }

        public void Error(string message) {
            _console.Error(message);
        }

        public string ReadLine() {
            return _input.ReadLine();
        }

        public bool TryReadArgument<T>(out T arg) {
            if (_input.TryNextToken(out var token)) {
                try {
                    arg = (T)Convert.ChangeType(token, typeof(T));
                    return true;
                }
                catch (Exception _) {
                    // ignored
                }
            }

            arg = default(T);
            return false;
        }

        public List<string> ReadRemainingArguments() {
            var args = new List<string>();
            while (_input.TryNextToken(out var arg)) {
                args.Add(arg);
            }

            return args;
        }
    }
}