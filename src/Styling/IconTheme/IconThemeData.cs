using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using Newtonsoft.Json;


namespace SidebarDiagnostics.Styling.IconTheme
{
    public class IconThemeData
    {
        static IconThemeData _default;

        private static IconThemeData _loadDefault()
        {
            if (_default != null)
            {
                return _default;
            }
            _default = Load("Default");
            return _default;
        }
        public string Name { get; set; }

        public Dictionary<string, string> Icons { get; set; }

        private static readonly string _namespace = typeof(IconThemeData).Assembly.GetName().Name + ".Styling.IconTheme.";

        /// <summary>
        /// Load an icon theme by name from embedded resources.
        /// </summary>
        public static IconThemeData Load(string themeName)
        {
            if (_default != null && themeName == "Default")
            {
                return _default;
            }
            
            string resourceName = _namespace + themeName + ".json";
            Assembly assembly = typeof(IconThemeData).Assembly;

            Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                resourceName = _namespace + "Default.json";
                stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    throw new FileNotFoundException($"Icon theme resource '{resourceName}' not found.");
                }
            }

            using StreamReader reader = new StreamReader(stream);
            var result = JsonConvert.DeserializeObject<IconThemeData>(reader.ReadToEnd());
            stream.Dispose();
            return result;
        }

        /// <summary>
        /// Try to load an icon theme by name. Returns null if not found.
        /// </summary>
        public static IconThemeData TryLoad(string themeName)
        {
            try
            {
                return Load(themeName);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get the list of available icon theme names.
        /// </summary>
        public static List<IconThemeData> GetAvailableThemes()
        {
            List<IconThemeData> result = [];
            var themes = typeof(IconThemeData).Assembly
                .GetManifestResourceNames()
                .Where(name => name.StartsWith(_namespace) && name.EndsWith(".json"));
            
            Assembly assembly = typeof(IconThemeData).Assembly;
            foreach (var resourceName in themes)
            {
                using Stream stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    throw new FileNotFoundException($"Icon theme resource '{resourceName}' not found.");
                }

                using StreamReader reader = new StreamReader(stream);
                var data = JsonConvert.DeserializeObject<IconThemeData>(reader.ReadToEnd());
                result.Add(data);


            }

            return result;
        }

        public static void ReplaceColor(Drawing drawing, Brush newBrush)
        {
            switch (drawing)
            {
                case DrawingGroup group:
                    foreach (var child in group.Children)
                        ReplaceColor(child, newBrush);
                    break;

                case GeometryDrawing geo:
                    if (geo.Brush != null)
                        geo.Brush = newBrush;
                    if (geo.Pen != null)
                        geo.Pen = new Pen(newBrush, geo.Pen.Thickness);
                    break;
            }
        }

    }
}
