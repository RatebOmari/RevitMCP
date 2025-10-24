using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitClaudeMCP.Scripting
{
    /// <summary>
    /// Provides helper methods for script execution
    /// This class can be used by dynamic scripts to simplify common operations
    /// </summary>
    public static class ScriptContext
    {
        /// <summary>
        /// Get active document
        /// </summary>
        public static Document GetActiveDocument(UIApplication uiApp)
        {
            return uiApp.ActiveUIDocument?.Document;
        }

        /// <summary>
        /// Get all elements of category
        /// </summary>
        public static List<Element> GetElementsByCategory(Document doc, BuiltInCategory category)
        {
            return new FilteredElementCollector(doc)
                .OfCategory(category)
                .WhereElementIsNotElementType()
                .ToList();
        }

        /// <summary>
        /// Get element by ID
        /// </summary>
        public static Element GetElement(Document doc, int id)
        {
            return doc.GetElement(new ElementId(id));
        }

        /// <summary>
        /// Set parameter value
        /// </summary>
        public static bool SetParameter(Element element, string paramName, object value)
        {
            Parameter param = element.LookupParameter(paramName);
            if (param == null || param.IsReadOnly)
                return false;

            try
            {
                switch (param.StorageType)
                {
                    case StorageType.String:
                        param.Set(value?.ToString() ?? "");
                        return true;
                    case StorageType.Integer:
                        param.Set(Convert.ToInt32(value));
                        return true;
                    case StorageType.Double:
                        param.Set(Convert.ToDouble(value));
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get parameter value
        /// </summary>
        public static string GetParameter(Element element, string paramName)
        {
            Parameter param = element.LookupParameter(paramName);
            if (param == null || !param.HasValue)
                return null;

            return param.StorageType switch
            {
                StorageType.String => param.AsString(),
                StorageType.Integer => param.AsInteger().ToString(),
                StorageType.Double => param.AsDouble().ToString("F3"),
                StorageType.ElementId => param.AsElementId().IntegerValue.ToString(),
                _ => null
            };
        }

        /// <summary>
        /// Show message to user
        /// </summary>
        public static void ShowMessage(string title, string message)
        {
            TaskDialog.Show(title, message);
        }

        /// <summary>
        /// Log message (for debugging)
        /// </summary>
        public static void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[Script] {message}");
        }
    }
}
