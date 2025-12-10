using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Nela.Flux {
    public static class BuiltinCommands {
        private const string HELP_MANUAL =
@"<b>SYNOPSIS</b>
help
help <command>

<b>DESCRIPTION</b>
Show list of available commands.
If <command> is provided, show the manual of the command.

<b>REMARKS</b>
The description and manual is set via the CommandAttribute on the function:
[Command(<name>, description = <description>, manual = <manual>)]
";
        [Command("help", description = "Show list of available commands or the manual of a specific command.", manual = HELP_MANUAL)]
        public static void Help(CommandContext context) {
            var args = context.ReadRemainingArguments();
            if (args.Count == 0) {
                context.Print("The available commands are listed below. Use `help <command>` for details of a specific command.\n");
                foreach (var command in CommandCache.GetAllCommands()) {
                    if (string.IsNullOrEmpty(command.description)) {
                        context.Print($"<b>{command.name}</b>\n");
                    } else {
                        context.Print($"<b>{command.name}</b>: {command.description}\n");
                    }
                }
            } else if (args.Count == 1) {
                if (CommandCache.TryFindCommandInfo(args[0], out var info)) {
                    context.Print($"{info.manual}\n");
                }
            } else {
                context.Error("help | help <command>");
            }
        }

        [Command("shell", description = "Open the default shell in the console.")]
        public static void OpenShell(CommandContext context) {
            var process = new Process();
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            string shellPath = Environment.GetEnvironmentVariable("SHELL");
            if (string.IsNullOrEmpty(shellPath)) {
                shellPath = "/bin/bash";
            }
            process.StartInfo.FileName = shellPath;
#else
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/K";
#endif
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;

            if (!process.Start()) {
                context.Error("Shell could not be started.");
                return;
            }

            var cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() =>
            {
                while (!process.HasExited)
                {
                    context.Println(process.StandardOutput.ReadLine());
                    Thread.Sleep(1); // if the process is exiting, we could keep fetching the same output from the buffer, causing a number of blank lines. So just wait for 1 millisecond before the next fetch
                }
                cancellationTokenSource.Cancel();
            });

            ReadShell();

            void ReadShell() {
                context.AcceptInput("shell", command => {
                    process.StandardInput.WriteLine(command);
                    process.StandardInput.Flush();
                    ReadShell();
                }, cancellationTokenSource.Token,
                () => {
                    process.StandardInput.Close();
                });
            }
        }
    }
}