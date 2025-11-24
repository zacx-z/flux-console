Flux console is a minimalistic console for use at runtime in your Unity Projects.

 - Press CTRL+Backquote to toggle the console.
 - Tab completion
 - Up/down arrow to navigate in the command history that is persistent over sessions, in a zsh-like way
 - Two ways to create your custom commands.

## Installation

Add this as a Unity package. It will function automatically in the editor and the build.

## Configuration

Create a file at `~/.config/flux-console.json` or `<your-app-directory>/.flux-console.json` and configure the settings in JSON. You can omit fields for their default values. Example configuration:

```json
{
    "margin": {
        "left": 0,
        "right": 0,
        "top": 0,
        "bottom": 300
    },
    "fontSize": 16,
    "historySize": 256,
    "outputBufferSize": 8192,
    "backgroundColor": "#00000080",
    "textColor": "#FFFFFFFF"
}
```

## Writing Commands

You can write commands in two ways:
 - Generic command: with `CommandContext` as the only argument, you have the most flexibility to interact with the console.
 - Method Invocation: Flux Console tries to parse the command arguments to invoke the method, and prints the return value to the console.

Examples are below:

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
