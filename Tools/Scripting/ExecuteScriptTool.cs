using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RevitClaudeMCP.Core.MCPServer;
using RevitClaudeMCP.Core.RevitContext;
using RevitClaudeMCP.Tools.Base;
using RevitClaudeMCP.Scripting;
using RevitClaudeMCP.Utils;

namespace RevitClaudeMCP.Tools.Scripting
{
    /// <summary>
    /// Execute dynamic C# script with validation and approval
    /// </summary>
    public class ExecuteScriptTool : ToolBase
    {
        public override string Name => "execute_script";

        public override string Description =>
            "Execute a C# script in Revit. The script will be validated for security, compiled, " +
            "and requires user approval before execution. Scripts have full access to Revit API but " +
            "file I/O, network access, and process spawning are blocked.";

        public override JObject InputSchema => CreateSchema(
            "object",
            "Parameters for script execution",
            new JObject
            {
                ["code"] = CreateProperty("string",
                    "C# code to execute. Should contain implementation without class wrapper (wrapper will be added automatically)."),
                ["description"] = CreateProperty("string",
                    "Description of what the script does (shown to user for approval)", null),
                ["requireApproval"] = CreateProperty("boolean",
                    "Whether to require user approval before execution (default: true)", true)
            },
            required: new[] { "code" }
        );

        public ExecuteScriptTool(RevitContextManager context) : base(context)
        {
        }

        protected override async Task<ToolsCallResult> ExecuteToolAsync(JObject arguments)
        {
            string code = GetRequiredParam<string>(arguments, "code");
            string description = GetOptionalParam<string>(arguments, "description", "Execute C# script");
            bool requireApproval = GetOptionalParam<bool>(arguments, "requireApproval", true);

            Logger.Log($"Script execution requested: {description}");

            // Step 1: Validate script
            var validationResult = ScriptValidator.Validate(code);
            if (!validationResult.IsValid)
            {
                string errorMsg = validationResult.GetErrorMessage();
                Logger.LogWarning($"Script validation failed:\n{errorMsg}");
                return CreateErrorResult(errorMsg);
            }

            Logger.Log("Script validation passed");

            // Step 2: Request approval if required
            if (requireApproval)
            {
                bool approved = await RequestApproval(code, description);
                if (!approved)
                {
                    Logger.Log("Script execution rejected by user");
                    return CreateErrorResult("Script execution rejected by user");
                }

                Logger.Log("Script execution approved by user");
            }

            // Step 3: Wrap and compile script
            string wrappedCode = ScriptCompiler.WrapScriptCode(code);
            var compilationResult = ScriptCompiler.Compile(wrappedCode);

            if (!compilationResult.Success)
            {
                string errorMsg = compilationResult.GetErrorMessage();
                Logger.LogError($"Script compilation failed:\n{errorMsg}");
                return CreateErrorResult(errorMsg);
            }

            Logger.Log("Script compiled successfully");

            // Step 4: Execute script in Revit context
            var executionResult = await _context.ExecuteWithTransactionAsync("Execute Script", doc =>
            {
                var execResult = ScriptExecutor.Execute(
                    compilationResult.Assembly,
                    _context.GetUIApplication()
                );

                if (!execResult.Success)
                {
                    throw new Exception(execResult.Message);
                }

                return execResult.Message;
            });

            Logger.Log($"Script executed successfully: {description}");

            return CreateTextResult($"Script executed successfully!\n\n{executionResult}");
        }

        /// <summary>
        /// Request user approval for script execution
        /// </summary>
        private async Task<bool> RequestApproval(string code, string description)
        {
            // Show approval dialog on UI thread
            return await _context.ExecuteAsync(uiApp =>
            {
                var dialog = new UI.ScriptApprovalDialog(code, description);
                bool? result = dialog.ShowDialog();
                return result == true;
            });
        }
    }
}
