namespace Nela.Flux {
    public static class BuiltinCommands {
        [Command("help", description = "Show list of available commands or help of a specific command.")]
        public static void Help(CommandContext context) {
            var args = context.ReadAllArguments();
            if (args.Count == 0) {
                context.Output("The available commands are listed below. Use `help <command>` for details of a specific command.\n");
                foreach (var command in CommandCache.GetAllCommands()) {
                    if (string.IsNullOrEmpty(command.description)) {
                        context.Output($"<b>{command.name}</b>\n", false);
                    } else {
                        context.Output($"<b>{command.name}</b>: {command.description}\n", false);
                    }
                }

                context.Flush();
            } else if (args.Count == 1) {
                if (CommandCache.TryFindCommandInfo(args[0], out var info)) {
                    context.Output($"{info.description}\n");
                }
            } else {
                context.Error("help | help <command>");
            }
        }
    }
}