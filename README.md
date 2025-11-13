Flux console is a minimalistic console for use at runtime in your Unity Projects.

 - Press CTRL+Backquote to toggle the console.
 - Tab completion
 - Up/down arrow to navigate in the command history that is persistent over sessions, in a zsh-like way
 - Two ways to create your custom commands.

## Installation

Add this as a Unity package. It will function automatically in the editor and the build.

## Examples

```csharp
[Command("echo", description = "Echoes the input after the command name.")]
private static void Echo(CommandContext context) {
    context.Output(context.input.ReadLine(), false);
    context.Output("\n");
}
```

```csharp
[Command("add", description = "Adds two integers and outputs the sum.")]
private static int Add(int a, int b) {
    return a + b;
}
```
