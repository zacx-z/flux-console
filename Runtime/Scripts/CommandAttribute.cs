using System;

namespace Nela.Flux {
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute {
        public string name;

        public CommandAttribute(string name) {
            this.name = name;
        }
    }
}