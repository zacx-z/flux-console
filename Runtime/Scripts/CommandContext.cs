using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
                catch (Exception) {
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

        /// <summary>
        /// Make the console wait for the task to finish before it accepts input again
        /// </summary>
        public void Attach(Task task, CancellationTokenSource cancellationTokenSource = null, string label = null) {
            _console.Attach(task, cancellationTokenSource, label);
        }

        /// <summary>
        /// Show a special prompt and handle the next input. If `AcceptInput` is called again before `handler` is called, `onSkipped` will be called.
        /// </summary>
        public void AcceptInput(string prompt, Action<string> handler, CancellationToken cancellationToken, Action onCanceled = null) {
            _console.SetInputHandler(new FluxConsole.InputHandler(prompt, handler, cancellationToken, onCanceled));
        }

        public void ExecuteCommand(string command) {
            _console.ExecuteOnMainThread(() => _console.ExecuteCommand(command));
        }

        public void SetAlternativeBufferEnabled(bool enabled) {
            _console.SetAlternativeBufferEnabled(enabled);
        }
    }
}