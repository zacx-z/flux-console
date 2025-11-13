using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Nela.Flux {
    public static class CommandCache {
        private static Dictionary<string, CommandInfo> _commandMap;

        public static Command FindCommand(string command) {
            BuildCache();
            return _commandMap.GetValueOrDefault(command).command;
        }

        public static bool TryFindCommandInfo(string command, out CommandInfo info) {
            BuildCache();
            return _commandMap.TryGetValue(command, out info);
        }

        private static void BuildCache() {
            if (_commandMap != null) return;

            _commandMap = new Dictionary<string, CommandInfo>();
            var thisAssembly = typeof(CommandAttribute).Assembly;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                if (assembly == thisAssembly || assembly.GetReferencedAssemblies().Any(a => a.FullName == thisAssembly.FullName)) {
                    foreach (var type in assembly.GetTypes()) {
                        foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public |
                                                               BindingFlags.NonPublic)) {
                            var attrs = method.GetCustomAttributes(typeof(CommandAttribute), true);
                            if (attrs.Length > 0) {
                                if (method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(CommandContext)) {
                                    var commandObject = GenerateCommandObject(method);
                                    foreach (CommandAttribute attr in attrs) {
                                        if (!_commandMap.TryAdd(attr.name, new CommandInfo()
                                            {
                                                name = attr.name,
                                                description = attr.description,
                                                command = commandObject,
                                            })) {
                                            Debug.LogError($"Can't add {method.DeclaringType}.{method.Name} because Command {attr.name} already exists for {_commandMap[attr.name].command.methodFullName}!");
                                        }
                                    }
                                } else {
                                    Debug.LogError($"Incorrect method signature of {method.DeclaringType}.{method.Name}, which should follows this: AnyType CommandMethod(CommandContext).");
                                }
                            }
                        }
                    }
                }
            }
        }

        public static IEnumerable<CommandInfo> GetAllCommands() {
            BuildCache();
            foreach (var command in _commandMap.Values) {
                yield return command;
            }
        }

        private static Command GenerateCommandObject(MethodInfo method) {
            return new Command(method);
        }
    }

    public class Command {
        private readonly MethodInfo _method;

        public Command(MethodInfo method) {
            _method = method;
        }

        public string methodFullName => $"{_method.DeclaringType}.{_method.Name}";

        public void Execute(CommandContext commandContext) {
            _method.Invoke(null, new object[] { commandContext });
        }
    }

    public struct CommandInfo {
        public string name;
        public string description;
        public Command command;
    }
}