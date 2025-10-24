using System;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.UI;
using RevitClaudeMCP.Utils;

namespace RevitClaudeMCP.Scripting
{
    /// <summary>
    /// Executes compiled scripts
    /// </summary>
    public class ScriptExecutor
    {
        /// <summary>
        /// Execute a compiled script
        /// </summary>
        public static ExecutionResult Execute(Assembly assembly, UIApplication uiApp, string[] args = null)
        {
            try
            {
                if (assembly == null)
                    throw new ArgumentNullException(nameof(assembly));

                if (uiApp == null)
                    throw new ArgumentNullException(nameof(uiApp));

                // Find the Execute method
                Type scriptType = assembly.GetTypes().FirstOrDefault(t => t.Name == "DynamicScript");
                if (scriptType == null)
                {
                    throw new Exception("Script class 'DynamicScript' not found");
                }

                MethodInfo executeMethod = scriptType.GetMethod("Execute",
                    BindingFlags.Public | BindingFlags.Static);

                if (executeMethod == null)
                {
                    throw new Exception("Execute method not found in DynamicScript class");
                }

                // Execute the method
                args = args ?? new string[0];
                executeMethod.Invoke(null, new object[] { uiApp, args });

                return new ExecutionResult
                {
                    Success = true,
                    Message = "Script executed successfully"
                };
            }
            catch (TargetInvocationException ex)
            {
                // Unwrap the inner exception
                Exception innerEx = ex.InnerException ?? ex;
                Logger.LogError($"Script execution error: {innerEx.Message}", innerEx);

                return new ExecutionResult
                {
                    Success = false,
                    Message = $"Script execution failed: {innerEx.Message}",
                    Exception = innerEx
                };
            }
            catch (Exception ex)
            {
                Logger.LogError($"Script execution error: {ex.Message}", ex);

                return new ExecutionResult
                {
                    Success = false,
                    Message = $"Script execution failed: {ex.Message}",
                    Exception = ex
                };
            }
        }
    }

    /// <summary>
    /// Script execution result
    /// </summary>
    public class ExecutionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }
}
