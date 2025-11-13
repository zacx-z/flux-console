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
            var args = context.ReadAllArguments();
            if (args.Count == 0) {
                context.Output("The available commands are listed below. Use `help <command>` for details of a specific command.\n");
                foreach (var command in CommandCache.GetAllCommands()) {
                    if (string.IsNullOrEmpty(command.description)) {
                        context.Output($"<b>{command.name}</b>\n");
                    } else {
                        context.Output($"<b>{command.name}</b>: {command.description}\n");
                    }
                }
            } else if (args.Count == 1) {
                if (CommandCache.TryFindCommandInfo(args[0], out var info)) {
                    context.Output($"{info.manual}\n");
                }
            } else {
                context.Error("help | help <command>");
            }
        }
    }
}