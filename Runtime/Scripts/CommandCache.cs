using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Nela.Flux {
    public static class CommandCache {
        private static Dictionary<string, CommandInfo> _commandMap;

        public static ICommand FindCommand(string command) {
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
                                var commandObject = GenerateCommandObject(method);
                                if (commandObject != null) {
                                    foreach (CommandAttribute attr in attrs) {
                                        if (!_commandMap.TryAdd(attr.name, new CommandInfo()
                                            {
                                                name = attr.name,
                                                description = attr.description,
                                                manual = string.IsNullOrEmpty(attr.manual) ? attr.description : attr.manual,
                                                command = commandObject,
                                            })) {
                                            Debug.LogError($"Can't add {method.DeclaringType}.{method.Name} because Command {attr.name} already exists for {_commandMap[attr.name].command.debugName}!");
                                        }
                                    }
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

        private static ICommand GenerateCommandObject(MethodInfo method) {
            if (method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(CommandContext)) {
                return new NativeCommand(method);
            } else {
                return new MethodInvocationCommand(method);
            }
        }
    }

    public interface ICommand {
        string debugName { get; }
        void Execute(CommandContext context);
    }

    public class NativeCommand : ICommand { 
        private readonly MethodInfo _method;

        public NativeCommand(MethodInfo method) {
            _method = method;
        }

        public string debugName => $"{_method.DeclaringType}.{_method.Name}";

        public void Execute(CommandContext commandContext) {
            _method.Invoke(null, new object[] { commandContext });
        }
    }

    public class MethodInvocationCommand : ICommand {
        private readonly MethodInfo _method;

        public MethodInvocationCommand(MethodInfo method) {
            _method = method;
        }

        public string debugName => $"{_method.DeclaringType}.{_method.Name}";

        public void Execute(CommandContext context) {
            var parameters = _method.GetParameters();
            var args = new object[parameters.Length];

            for (int i = 0; i < args.Length; i++) {
                var param = parameters[i];
                if (context.input.TryNextToken(out var token) ) {
                    if (!TryParse(token, param.ParameterType, out args[i])) {
                        context.Error($"Invalid value \"{token}\" is given for <i>{param.Name}</i> of {debugName}({string.Join(", ", parameters.Select(p => p.Name))})!");
                        return;
                    }
                } else {
                    if (param.HasDefaultValue) {
                        args[i] = param.DefaultValue;
                    } else {
                        context.Error($"No value is given for <i>{param.Name}</i> of {debugName}({string.Join(", ", parameters.Select(p => p.Name))})!");
                        return;
                    }
                }
            }

            var ret = _method.Invoke(null, args);
            if (_method.ReturnType != typeof(void)) {
                context.Print($"{ret}\n");
            }
        }

        private bool TryParse(string token, Type type, out object result) {
            try {
                result = Convert.ChangeType(token, type);
                return true;
            }
            catch (OverflowException) {
                result = null;
                return false;
            }
            catch (FormatException) {
                result = null;
                return false;
            }
            catch (InvalidCastException) {
                result = null;
                return false;
            }
        }
    }

    public struct CommandInfo {
        public string name;
        public string description;
        public string manual;
        public ICommand command;
    }
}