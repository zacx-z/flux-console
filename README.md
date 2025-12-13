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
private const string ECHO_MANUAL = @"echo [<string> ...]
Writes the texts followed by the command.
";

[Command("echo", description = "Echoes the input after the command name."), manual = ECHO_MANUAL]
private static void Echo(CommandContext context) {
    context.Println(context.input.ReadLine());
}
```

```csharp
[Command("add", description = "Adds two integers and outputs the sum.")]
private static int Add(int a, int b) {
    return a + b;
}
```

## Advanced Topics

### Async Command

If a command takes time to finish, you can block the input with `CommandContext.Attach()` function while it is executing.

It accepts a `Task` object, and while it is not finished, the console will stops accepting input.

The following code delay executing an command for `delay` milliseconds, where the user can press CTRL-C to interrupt it:

```csharp
var cancellationTokenSource = new CancellationTokenSource();
var task = Task.Delay(delay, cancellationTokenSource.Token);
task.ContinueWith(_ => {
        context.ExecuteCommand(command);
    }, TaskContinuationOptions.OnlyOnRanToCompletion);
task.ContinueWith(t => {
        context.Println("Delay command cancelled.");
    }, TaskContinuationOptions.OnlyOnCanceled);
context.Attach(task, cancellationTokenSource);
```

### Taking Over Input

Use `CommandContext.AcceptInput()` to intercept the input for once and handle it with your own handlers.

Provide a `CancellationToken` to request cancelling accepting input.

If you want to receiving input for more than once, you can call `AcceptInput()` again from the input handler.

### Alternative Buffer

Flux Console simulates an alternative buffer similar to that in a shell terminal.

After you enable the alternative buffer by calling `CommandContext.SetAlternativeBufferEnabled()`, any subsequent printing will output the text to the alternative buffer,
which will clear the screen and show the text from the top left. You can use this in combination with `CommandContext.Attach()` or `CommandContext.AcceptInput()` to show status text while your code is executing.

Remember to disable the alternative buffer after you finish with your command.
