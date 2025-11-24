using System;
using UnityEngine;

namespace Nela.Flux {
    /// <summary>
    /// The settings.
    /// </summary>
    /// Store your configure at ~/.config/flux-console.json or your-app-directory/.flux-console.json in JSON format. You can leave out fields for default values.
    /// <example>
    /// Example configuration:
    /// <code>
    /// {
    ///     "margin": {
    ///         "left": 0,
    ///         "right": 0,
    ///         "top": 0,
    ///         "bottom": 300
    ///     },
    ///     "fontSize": 16,
    ///     "historySize": 256,
    ///     "outputBufferSize": 8192,
    ///     "backgroundColor": "#00000080",
    ///     "textColor": "#FFFFFFFF"
    /// }
    /// </code>
    /// </example>
    [Serializable]
    public class FluxSettings {
        [Serializable]
        public struct Margin {
            public int left;
            public int right;
            public int top;
            public int bottom;
        }

        public Margin margin;
        public int fontSize;
        public int historySize;
        public int outputBufferSize;
        public string backgroundColor;
        public string textColor;

        public static Color GetColor(string colorString, Color defaultValue) {
            if (ColorUtility.TryParseHtmlString(colorString, out var color))
                return color;
            return defaultValue;
        }
    }
}