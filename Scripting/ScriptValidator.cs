using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitClaudeMCP.Scripting
{
    /// <summary>
    /// Validates C# scripts for security concerns before compilation
    /// </summary>
    public class ScriptValidator
    {
        private static readonly List<ValidationRule> _rules = new List<ValidationRule>
        {
            // File I/O operations
            new ValidationRule("System.IO.File", "File I/O operations are not allowed"),
            new ValidationRule("System.IO.Directory", "Directory operations are not allowed"),
            new ValidationRule("System.IO.FileInfo", "File operations are not allowed"),
            new ValidationRule("System.IO.DirectoryInfo", "Directory operations are not allowed"),
            new ValidationRule("System.IO.FileStream", "File stream operations are not allowed"),
            new ValidationRule("System.IO.StreamWriter", "Stream writing is not allowed"),
            new ValidationRule("System.IO.StreamReader", "Stream reading is not allowed"),

            // Network operations
            new ValidationRule("System.Net.", "Network operations are not allowed"),
            new ValidationRule("System.Net.Http", "HTTP operations are not allowed"),
            new ValidationRule("System.Net.Sockets", "Socket operations are not allowed"),
            new ValidationRule("System.Net.WebClient", "Web client operations are not allowed"),
            new ValidationRule("HttpClient", "HTTP client operations are not allowed"),
            new ValidationRule("WebClient", "Web client operations are not allowed"),

            // Process spawning
            new ValidationRule("System.Diagnostics.Process", "Process spawning is not allowed"),
            new ValidationRule("Process.Start", "Starting processes is not allowed"),

            // Reflection (dynamic loading)
            new ValidationRule("Assembly.Load", "Dynamic assembly loading is not allowed"),
            new ValidationRule("Assembly.LoadFrom", "Dynamic assembly loading is not allowed"),
            new ValidationRule("Assembly.LoadFile", "Dynamic assembly loading is not allowed"),
            new ValidationRule("Activator.CreateInstance", "Dynamic type instantiation is not allowed"),
            new ValidationRule("Type.GetType(", "Dynamic type loading is not allowed"),

            // Registry access
            new ValidationRule("Microsoft.Win32.Registry", "Registry access is not allowed"),
            new ValidationRule("RegistryKey", "Registry access is not allowed"),

            // Environment manipulation
            new ValidationRule("Environment.Exit", "Terminating the application is not allowed"),
            new ValidationRule("Application.Exit", "Terminating the application is not allowed"),

            // Unsafe code
            new ValidationRule("unsafe ", "Unsafe code is not allowed"),
            new ValidationRule("DllImport", "Native interop is not allowed"),

            // Thread manipulation (dangerous patterns)
            new ValidationRule("Thread.Abort", "Aborting threads is not allowed"),
        };

        /// <summary>
        /// Validate script code
        /// </summary>
        public static ValidationResult Validate(string scriptCode)
        {
            if (string.IsNullOrWhiteSpace(scriptCode))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { "Script code is empty" }
                };
            }

            var errors = new List<string>();

            // Check against all rules
            foreach (var rule in _rules)
            {
                if (scriptCode.Contains(rule.Pattern, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"{rule.Message} (found: '{rule.Pattern}')");
                }
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        /// <summary>
        /// Add custom validation rule
        /// </summary>
        public static void AddRule(string pattern, string message)
        {
            _rules.Add(new ValidationRule(pattern, message));
        }

        /// <summary>
        /// Get all validation rules
        /// </summary>
        public static IReadOnlyList<ValidationRule> GetRules()
        {
            return _rules.AsReadOnly();
        }
    }

    /// <summary>
    /// Validation rule
    /// </summary>
    public class ValidationRule
    {
        public string Pattern { get; set; }
        public string Message { get; set; }

        public ValidationRule(string pattern, string message)
        {
            Pattern = pattern;
            Message = message;
        }
    }

    /// <summary>
    /// Validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public string GetErrorMessage()
        {
            if (IsValid)
                return string.Empty;

            return $"Script validation failed:\n" + string.Join("\n", Errors.Select(e => $"  • {e}"));
        }
    }
}
