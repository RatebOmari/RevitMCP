using System;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitClaudeMCP.Utils;

namespace RevitClaudeMCP.Core.RevitContext
{
    /// <summary>
    /// Provides thread-safe access to Revit API using ExternalEvent mechanism
    /// </summary>
    public class RevitContextManager : IDisposable
    {
        private readonly ExternalEvent _externalEvent;
        private readonly RevitEventHandler _eventHandler;
        private UIControlledApplication _application;

        public UIControlledApplication Application => _application;

        /// <summary>
        /// Constructor
        /// </summary>
        public RevitContextManager(UIControlledApplication application)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
            _eventHandler = new RevitEventHandler();
            _externalEvent = ExternalEvent.Create(_eventHandler);

            Logger.Log("RevitContextManager initialized");
        }

        /// <summary>
        /// Execute an operation on the Revit main thread
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="operation">Operation to execute</param>
        /// <param name="transactionName">Transaction name (null for read-only)</param>
        /// <returns>Operation result</returns>
        public Task<T> ExecuteAsync<T>(Func<UIApplication, T> operation, string transactionName = null)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            // Create operation wrapper
            var wrapper = new RevitOperation<T>(operation, transactionName);

            // Queue operation
            _eventHandler.EnqueueOperation(wrapper);

            // Raise external event to execute on Revit thread
            var raiseResult = _externalEvent.Raise();

            if (raiseResult != ExternalEventRequest.Accepted)
            {
                throw new InvalidOperationException($"Failed to raise external event: {raiseResult}");
            }

            // Wait for completion
            return wrapper.Task;
        }

        /// <summary>
        /// Execute an operation on the Revit main thread (void return)
        /// </summary>
        public Task ExecuteAsync(Action<UIApplication> operation, string transactionName = null)
        {
            return ExecuteAsync<object>(uiApp =>
            {
                operation(uiApp);
                return null;
            }, transactionName);
        }

        /// <summary>
        /// Get the active UIApplication
        /// </summary>
        public UIApplication GetUIApplication()
        {
            // Note: This should only be called from the Revit main thread
            return new UIApplication(_application.ControlledApplication);
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _externalEvent?.Dispose();
            Logger.Log("RevitContextManager disposed");
        }
    }

    /// <summary>
    /// External event handler that executes queued operations
    /// </summary>
    public class RevitEventHandler : IExternalEventHandler
    {
        private readonly object _lock = new object();
        private IRevitOperation _currentOperation;

        /// <summary>
        /// Enqueue operation for execution
        /// </summary>
        public void EnqueueOperation(IRevitOperation operation)
        {
            lock (_lock)
            {
                if (_currentOperation != null)
                {
                    throw new InvalidOperationException("An operation is already queued");
                }

                _currentOperation = operation;
            }
        }

        /// <summary>
        /// Execute the queued operation (called by Revit on main thread)
        /// </summary>
        public void Execute(UIApplication app)
        {
            IRevitOperation operation;

            lock (_lock)
            {
                operation = _currentOperation;
                _currentOperation = null;
            }

            if (operation != null)
            {
                try
                {
                    operation.Execute(app);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error executing operation: {ex.Message}", ex);
                    operation.SetException(ex);
                }
            }
        }

        /// <summary>
        /// Get event handler name
        /// </summary>
        public string GetName()
        {
            return "RevitClaudeMCP Event Handler";
        }
    }

    /// <summary>
    /// Interface for Revit operations
    /// </summary>
    public interface IRevitOperation
    {
        void Execute(UIApplication app);
        void SetException(Exception ex);
    }

    /// <summary>
    /// Revit operation wrapper
    /// </summary>
    public class RevitOperation<T> : IRevitOperation
    {
        private readonly Func<UIApplication, T> _operation;
        private readonly string _transactionName;
        private readonly TaskCompletionSource<T> _taskCompletionSource;

        public Task<T> Task => _taskCompletionSource.Task;

        public RevitOperation(Func<UIApplication, T> operation, string transactionName)
        {
            _operation = operation ?? throw new ArgumentNullException(nameof(operation));
            _transactionName = transactionName;
            _taskCompletionSource = new TaskCompletionSource<T>();
        }

        /// <summary>
        /// Execute the operation
        /// </summary>
        public void Execute(UIApplication app)
        {
            try
            {
                T result;

                if (string.IsNullOrEmpty(_transactionName))
                {
                    // Read-only operation (no transaction)
                    result = _operation(app);
                }
                else
                {
                    // Operation requires transaction
                    Document doc = app.ActiveUIDocument?.Document;

                    if (doc == null)
                    {
                        throw new InvalidOperationException("No active document");
                    }

                    using (Transaction transaction = new Transaction(doc, _transactionName))
                    {
                        transaction.Start();

                        try
                        {
                            result = _operation(app);
                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.RollBack();
                            throw;
                        }
                    }
                }

                _taskCompletionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                _taskCompletionSource.SetException(ex);
            }
        }

        /// <summary>
        /// Set exception result
        /// </summary>
        public void SetException(Exception ex)
        {
            _taskCompletionSource.SetException(ex);
        }
    }

    /// <summary>
    /// Helper class for common Revit context operations
    /// </summary>
    public static class RevitContextExtensions
    {
        /// <summary>
        /// Get active document
        /// </summary>
        public static Task<Document> GetActiveDocumentAsync(this RevitContextManager context)
        {
            return context.ExecuteAsync(uiApp =>
            {
                var doc = uiApp.ActiveUIDocument?.Document;
                if (doc == null)
                    throw new InvalidOperationException("No active document");
                return doc;
            });
        }

        /// <summary>
        /// Get document by name
        /// </summary>
        public static Task<Document> GetDocumentByNameAsync(this RevitContextManager context, string name)
        {
            return context.ExecuteAsync(uiApp =>
            {
                foreach (Document doc in uiApp.Application.Documents)
                {
                    if (doc.Title.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return doc;
                    }
                }
                throw new InvalidOperationException($"Document not found: {name}");
            });
        }

        /// <summary>
        /// Get all open documents
        /// </summary>
        public static Task<System.Collections.Generic.List<string>> GetAllDocumentNamesAsync(
            this RevitContextManager context)
        {
            return context.ExecuteAsync(uiApp =>
            {
                var names = new System.Collections.Generic.List<string>();
                foreach (Document doc in uiApp.Application.Documents)
                {
                    names.Add(doc.Title);
                }
                return names;
            });
        }

        /// <summary>
        /// Execute with automatic transaction
        /// </summary>
        public static Task<T> ExecuteWithTransactionAsync<T>(
            this RevitContextManager context,
            string transactionName,
            Func<Document, T> operation)
        {
            return context.ExecuteAsync(uiApp =>
            {
                var doc = uiApp.ActiveUIDocument?.Document;
                if (doc == null)
                    throw new InvalidOperationException("No active document");

                return operation(doc);
            }, transactionName);
        }

        /// <summary>
        /// Execute read-only operation
        /// </summary>
        public static Task<T> ExecuteReadOnlyAsync<T>(
            this RevitContextManager context,
            Func<Document, T> operation)
        {
            return context.ExecuteAsync(uiApp =>
            {
                var doc = uiApp.ActiveUIDocument?.Document;
                if (doc == null)
                    throw new InvalidOperationException("No active document");

                return operation(doc);
            });
        }
    }
}
