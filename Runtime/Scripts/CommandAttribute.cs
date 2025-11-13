using System;

namespace Nela.Flux {
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CommandAttribute : Attribute {
        public string name;
        public string description;

        public CommandAttribute(string name) {
            this.name = name;
        }
    }
}