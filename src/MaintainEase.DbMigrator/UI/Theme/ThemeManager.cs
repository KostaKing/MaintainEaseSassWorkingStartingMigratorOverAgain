using System;
using System.Collections.Generic;
using Spectre.Console;
using MaintainEase.DbMigrator.UI.ConsoleHelpers;

namespace MaintainEase.DbMigrator.UI.Theme
{
    /// <summary>
    /// Centralized theme management for the application UI
    /// </summary>
    public static class ThemeManager
    {
        // Color schemes
        public static class ColorSchemes
        {
            public static readonly ColorScheme Default = new ColorScheme
            {
                Primary = Color.Blue,
                Secondary = Color.Cyan1,
                Success = Color.Green,
                Warning = Color.Yellow,
                Danger = Color.Red,
                Info = Color.Cyan1,
                Muted = Color.Grey,
                Highlight = Color.Magenta1,
                Background = Color.Default,
                Foreground = Color.White
            };

            public static readonly ColorScheme Dark = new ColorScheme
            {
                Primary = Color.DeepSkyBlue1,
                Secondary = Color.Aqua,
                Success = Color.LightGreen_1,
                Warning = Color.Gold1,
                Danger = Color.OrangeRed1,
                Info = Color.SkyBlue1,
                Muted = Color.Grey,
                Highlight = Color.HotPink,
                Background = Color.Default,
                Foreground = Color.Grey93
            };

            public static readonly ColorScheme Light = new ColorScheme
            {
                Primary = Color.Blue,
                Secondary = Color.Teal,
                Success = Color.Green,
                Warning = Color.Orange1,
                Danger = Color.Red,
                Info = Color.DeepSkyBlue1,
                Muted = Color.Grey58,
                Highlight = Color.Purple,
                Background = Color.Default,
                Foreground = Color.Black
            };

            public static readonly ColorScheme Azure = new ColorScheme
            {
                Primary = Color.RoyalBlue1,
                Secondary = Color.SteelBlue,
                Success = Color.SpringGreen2,
                Warning = Color.Yellow3,
                Danger = Color.DarkRed_1,
                Info = Color.DeepSkyBlue3,
                Muted = Color.DarkSlateGray1,
                Highlight = Color.MediumPurple1,
                Background = Color.Default,
                Foreground = Color.White
            };
        }

        // Style collections
        private static readonly Dictionary<string, Styles> _availableStyles = new Dictionary<string, Styles>();

        // Current active theme
        private static string _currentTheme = "Default";
        private static Styles _currentStyles;

        // Initialize with static constructor instead of direct assignments
        static ThemeManager()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize the theme manager with available themes
        /// </summary>
        public static void Initialize()
        {
            // Register built-in themes
            RegisterTheme("Default", new Styles(ColorSchemes.Default));
            RegisterTheme("Dark", new Styles(ColorSchemes.Dark));
            RegisterTheme("Light", new Styles(ColorSchemes.Light));
            RegisterTheme("Azure", new Styles(ColorSchemes.Azure));

            // Set default theme
            SetTheme("Default");
        }

        /// <summary>
        /// Register a new theme
        /// </summary>
        public static void RegisterTheme(string name, Styles styles)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Theme name cannot be empty.", nameof(name));

            _availableStyles[name] = styles ?? throw new ArgumentNullException(nameof(styles));
        }

        /// <summary>
        /// Set the active theme
        /// </summary>
        public static bool SetTheme(string name)
        {
            if (string.IsNullOrEmpty(name) || !_availableStyles.ContainsKey(name))
                return false;

            _currentTheme = name;
            _currentStyles = _availableStyles[name];

            // Instead of directly assigning to readonly fields, use the update method
            UpdateThemeStyles(_currentStyles);

            return true;
        }

        /// <summary>
        /// Get the name of the current theme
        /// </summary>
        public static string GetCurrentTheme() => _currentTheme;

        /// <summary>
        /// Get the list of available themes
        /// </summary>
        public static IEnumerable<string> GetAvailableThemes() => _availableStyles.Keys;

        /// <summary>
        /// Get the current styles
        /// </summary>
        public static Styles GetCurrentStyles() => _currentStyles;

        // Helper method to update styles without direct assignment
        private static void UpdateThemeStyles(Styles styles)
        {
            SafeMarkup.Themes.UpdateStyles(
                styles.Primary,
                styles.Success,
                styles.Warning,
                styles.Danger,
                styles.Info,
                styles.Muted,
                styles.Highlight);
        }
    }

    /// <summary>
    /// Color scheme for UI themes
    /// </summary>
    public class ColorScheme
    {
        public Color Primary { get; set; }
        public Color Secondary { get; set; }
        public Color Success { get; set; }
        public Color Warning { get; set; }
        public Color Danger { get; set; }
        public Color Info { get; set; }
        public Color Muted { get; set; }
        public Color Highlight { get; set; }
        public Color Background { get; set; }
        public Color Foreground { get; set; }
    }

    /// <summary>
    /// Collection of styles for a theme
    /// </summary>
    public class Styles
    {
        public Style Primary { get; set; }
        public Style Secondary { get; set; }
        public Style Success { get; set; }
        public Style Warning { get; set; }
        public Style Danger { get; set; }
        public Style Info { get; set; }
        public Style Muted { get; set; }
        public Style Highlight { get; set; }

        /// <summary>
        /// Create styles from a color scheme
        /// </summary>
        public Styles(ColorScheme colorScheme)
        {
            if (colorScheme == null)
                throw new ArgumentNullException(nameof(colorScheme));

            Primary = new Style(colorScheme.Primary, colorScheme.Background, Decoration.Bold);
            Secondary = new Style(colorScheme.Secondary, colorScheme.Background, Decoration.Bold);
            Success = new Style(colorScheme.Success, colorScheme.Background, Decoration.Bold);
            Warning = new Style(colorScheme.Warning, colorScheme.Background, Decoration.Bold);
            Danger = new Style(colorScheme.Danger, colorScheme.Background, Decoration.Bold);
            Info = new Style(colorScheme.Info, colorScheme.Background, Decoration.Bold);
            Muted = new Style(colorScheme.Muted, colorScheme.Background, Decoration.None);
            Highlight = new Style(colorScheme.Highlight, colorScheme.Background, Decoration.Bold);
        }
    }
}
