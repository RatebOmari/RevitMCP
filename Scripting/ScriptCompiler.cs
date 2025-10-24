using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using RevitClaudeMCP.Utils;

namespace RevitClaudeMCP.Scripting
{
    /// <summary>
    /// Compiles C# scripts at runtime using Roslyn
    /// </summary>
    public class ScriptCompiler
    {
        /// <summary>
        /// Compile script code to assembly
        /// </summary>
        public static CompilationResult Compile(string scriptCode, string scriptName = "DynamicScript")
        {
            try
            {
                // Create syntax tree
                var syntaxTree = CSharpSyntaxTree.ParseText(scriptCode);

                // Get references
                var references = GetReferences();

                // Create compilation
                var compilation = CSharpCompilation.Create(
                    assemblyName: scriptName,
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        optimizationLevel: OptimizationLevel.Release
                    )
                );

                // Compile to memory
                using (var ms = new System.IO.MemoryStream())
                {
                    EmitResult emitResult = compilation.Emit(ms);

                    if (!emitResult.Success)
                    {
                        // Compilation failed
                        var failures = emitResult.Diagnostics
                            .Where(diagnostic => diagnostic.IsWarningAsError ||
                                                diagnostic.Severity == DiagnosticSeverity.Error);

                        var errors = failures.Select(diagnostic =>
                            $"{diagnostic.Id}: {diagnostic.GetMessage()} at {diagnostic.Location}"
                        ).ToList();

                        return new CompilationResult
                        {
                            Success = false,
                            Errors = errors
                        };
                    }

                    // Success - load assembly
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    byte[] assemblyBytes = ms.ToArray();
                    Assembly assembly = Assembly.Load(assemblyBytes);

                    return new CompilationResult
                    {
                        Success = true,
                        Assembly = assembly
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Compilation error: {ex.Message}", ex);
                return new CompilationResult
                {
                    Success = false,
                    Errors = new List<string> { $"Compilation exception: {ex.Message}" }
                };
            }
        }

        /// <summary>
        /// Get assembly references for compilation
        /// </summary>
        private static List<MetadataReference> GetReferences()
        {
            var references = new List<MetadataReference>();

            // Core .NET assemblies
            references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location)); // mscorlib
            references.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location)); // System.Core
            references.Add(MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location)); // System.Collections
            references.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location));

            // Revit API assemblies
            references.Add(MetadataReference.CreateFromFile(typeof(Autodesk.Revit.DB.Document).Assembly.Location)); // RevitAPI
            references.Add(MetadataReference.CreateFromFile(typeof(Autodesk.Revit.UI.UIApplication).Assembly.Location)); // RevitAPIUI

            // Additional useful assemblies
            try
            {
                references.Add(MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location));
            }
            catch { } // netstandard might not be available in all environments

            return references;
        }

        /// <summary>
        /// Wrap script code in a class if not already wrapped
        /// </summary>
        public static string WrapScriptCode(string scriptCode)
        {
            // Check if code already has a class definition
            if (scriptCode.Contains("class ") && scriptCode.Contains("public static void Execute"))
            {
                return scriptCode;
            }

            // Wrap in class
            return $@"
using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

public class DynamicScript
{{
    public static void Execute(UIApplication uiApp, string[] args)
    {{
{scriptCode}
    }}
}}
";
        }
    }

    /// <summary>
    /// Compilation result
    /// </summary>
    public class CompilationResult
    {
        public bool Success { get; set; }
        public Assembly Assembly { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public string GetErrorMessage()
        {
            if (Success)
                return string.Empty;

            return "Compilation failed:\n" + string.Join("\n", Errors.Select(e => $"  • {e}"));
        }
    }
}
