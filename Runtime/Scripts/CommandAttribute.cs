using System;

namespace Nela.Flux {
    /// <summary>
    /// Add this attribute to static functions to make it a command.
    /// </summary>
    /// <example>
    /// Create a command with CommandContext argument.
    /// <code>
    /// [Command("echo", description = "Echoes the input after the command name.")]
    /// private static void Echo(CommandContext context) {
    ///     context.Output(context.input.ReadLine(), false);
    ///     context.Output("\n");
    /// }
    /// </code>
    /// <code>
    /// [Command("add", description = "Adds two integers and outputs the sum.")]
    /// private static int Add(int a, int b) {
    ///     return a + b;
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CommandAttribute : Attribute {
        public string name;
        public string description;
        public string manual;

        public CommandAttribute(string name) {
            this.name = name;
        }
    }
}