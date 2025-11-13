using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Nela.Flux {
    public class CommandCache {
        private static Dictionary<string, Command> _commandMap;

        public static Command FindCommand(string command) {
            BuildCache();
            return _commandMap.GetValueOrDefault(command);
        }

        private static void BuildCache() {
            if (_commandMap != null) return;

            _commandMap = new Dictionary<string, Command>();
            var thisAssembly = typeof(CommandAttribute).Assembly;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                if (assembly.GetReferencedAssemblies().Any(a => a.FullName == thisAssembly.FullName)) {
                    foreach (var type in assembly.GetTypes()) {
                        foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public |
                                                               BindingFlags.NonPublic)) {
                            var attr = method.GetCustomAttribute<CommandAttribute>();
                            if (attr != null) {
                                if (method.GetParameters().Length == 1 &&
                                    method.GetParameters()[0].ParameterType == typeof(CommandContext)) {
                                    _commandMap.Add(attr.name, GenerateCommandObject(method, attr.name));
                                } else {
                                    Debug.LogError($"Incorrect method signature of {method.DeclaringType}.{method.Name}, which should follows this: AnyType CommandMethod(CommandContext).");
                                }
                            }
                        }
                    }
                }
            }
        }

        private static Command GenerateCommandObject(MethodInfo method, string attrName) {
            return new Command(method);
        }
    }

    public class Command {
        private readonly MethodInfo _method;

        public Command(MethodInfo method) {
            _method = method;
        }

        public void Execute(CommandContext commandContext) {
            _method.Invoke(null, new object[] { commandContext });
        }
    }
}