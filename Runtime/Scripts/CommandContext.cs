namespace Nela.Flux {
    public class CommandContext {
        private readonly FluxConsole _console;
        private readonly CommandTokenizer _input;

        public CommandTokenizer input => _input;

        public CommandContext(FluxConsole console, CommandTokenizer input) {
            _console = console;
            _input = input;
        }

        public void Output(string text, bool flush = true) {
            _console.Output(text, flush);
        }
    }
}