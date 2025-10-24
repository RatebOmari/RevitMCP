using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RevitClaudeMCP.Utils
{
    /// <summary>
    /// Helper methods for element operations
    /// </summary>
    public static class ElementHelper
    {
        /// <summary>
        /// Get element by ID
        /// </summary>
        public static Element GetElementById(Document doc, ElementId id)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            if (id == null || id == ElementId.InvalidElementId)
                throw new ArgumentException("Invalid element ID");

            return doc.GetElement(id);
        }

        /// <summary>
        /// Get element by ID (integer)
        /// </summary>
        public static Element GetElementById(Document doc, int id)
        {
            return GetElementById(doc, new ElementId(id));
        }

        /// <summary>
        /// Get all elements of category
        /// </summary>
        public static List<Element> GetElementsByCategory(Document doc, BuiltInCategory category)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            return new FilteredElementCollector(doc)
                .OfCategory(category)
                .WhereElementIsNotElementType()
                .ToList();
        }

        /// <summary>
        /// Get all elements of type
        /// </summary>
        public static List<Element> GetElementsByType<T>(Document doc) where T : Element
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            return new FilteredElementCollector(doc)
                .OfClass(typeof(T))
                .ToList();
        }

        /// <summary>
        /// Get parameter value as string
        /// </summary>
        public static string GetParameterValueAsString(Element element, string paramName)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            Parameter param = element.LookupParameter(paramName);
            if (param == null)
                return null;

            return GetParameterValueAsString(param);
        }

        /// <summary>
        /// Get parameter value as string
        /// </summary>
        public static string GetParameterValueAsString(Parameter param)
        {
            if (param == null)
                return null;

            if (!param.HasValue)
                return string.Empty;

            switch (param.StorageType)
            {
                case StorageType.String:
                    return param.AsString() ?? string.Empty;

                case StorageType.Integer:
                    return param.AsInteger().ToString();

                case StorageType.Double:
                    return param.AsDouble().ToString("F3");

                case StorageType.ElementId:
                    ElementId id = param.AsElementId();
                    return id == ElementId.InvalidElementId ? string.Empty : id.IntegerValue.ToString();

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Set parameter value
        /// </summary>
        public static bool SetParameterValue(Parameter param, object value)
        {
            if (param == null)
                throw new ArgumentNullException(nameof(param));

            if (param.IsReadOnly)
                return false;

            try
            {
                switch (param.StorageType)
                {
                    case StorageType.String:
                        param.Set(value?.ToString() ?? string.Empty);
                        return true;

                    case StorageType.Integer:
                        param.Set(Convert.ToInt32(value));
                        return true;

                    case StorageType.Double:
                        param.Set(Convert.ToDouble(value));
                        return true;

                    case StorageType.ElementId:
                        if (value is ElementId elemId)
                            param.Set(elemId);
                        else if (value is int intId)
                            param.Set(new ElementId(intId));
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
        /// Get all parameters of element
        /// </summary>
        public static List<Parameter> GetAllParameters(Element element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return element.Parameters.Cast<Parameter>().ToList();
        }

        /// <summary>
        /// Get parameter info as dictionary
        /// </summary>
        public static Dictionary<string, string> GetParametersAsDictionary(Element element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var dict = new Dictionary<string, string>();

            foreach (Parameter param in element.Parameters)
            {
                string name = param.Definition?.Name ?? "Unknown";
                string value = GetParameterValueAsString(param);
                dict[name] = value;
            }

            return dict;
        }

        /// <summary>
        /// Get element category name
        /// </summary>
        public static string GetCategoryName(Element element)
        {
            if (element == null)
                return "Unknown";

            return element.Category?.Name ?? "Unknown";
        }

        /// <summary>
        /// Get element type name
        /// </summary>
        public static string GetElementTypeName(Element element, Document doc)
        {
            if (element == null)
                return "Unknown";

            ElementId typeId = element.GetTypeId();
            if (typeId == ElementId.InvalidElementId)
                return "No Type";

            Element type = doc.GetElement(typeId);
            return type?.Name ?? "Unknown";
        }

        /// <summary>
        /// Get element location point
        /// </summary>
        public static XYZ GetLocationPoint(Element element)
        {
            if (element?.Location is LocationPoint locationPoint)
            {
                return locationPoint.Point;
            }

            return null;
        }

        /// <summary>
        /// Get element location curve
        /// </summary>
        public static Curve GetLocationCurve(Element element)
        {
            if (element?.Location is LocationCurve locationCurve)
            {
                return locationCurve.Curve;
            }

            return null;
        }

        /// <summary>
        /// Move element to new location
        /// </summary>
        public static bool MoveElement(Element element, XYZ translation)
        {
            if (element == null || translation == null)
                return false;

            try
            {
                ElementTransformUtils.MoveElement(element.Document, element.Id, translation);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Rotate element
        /// </summary>
        public static bool RotateElement(Element element, XYZ origin, XYZ axis, double angleDegrees)
        {
            if (element == null || origin == null || axis == null)
                return false;

            try
            {
                double angleRadians = GeometryHelper.DegreesToRadians(angleDegrees);
                Line axisLine = Line.CreateBound(origin, origin + axis);
                ElementTransformUtils.RotateElement(element.Document, element.Id, axisLine, angleRadians);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Delete element
        /// </summary>
        public static bool DeleteElement(Document doc, ElementId id)
        {
            if (doc == null || id == null || id == ElementId.InvalidElementId)
                return false;

            try
            {
                doc.Delete(id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get element info as formatted string
        /// </summary>
        public static string GetElementInfo(Element element, Document doc)
        {
            if (element == null)
                return "Element is null";

            string info = $"Element ID: {element.Id.IntegerValue}\n";
            info += $"Name: {element.Name}\n";
            info += $"Category: {GetCategoryName(element)}\n";
            info += $"Type: {GetElementTypeName(element, doc)}\n";

            XYZ location = GetLocationPoint(element);
            if (location != null)
            {
                info += $"Location: {GeometryHelper.FormatPoint(location)}\n";
            }

            return info;
        }

        /// <summary>
        /// Find elements by name
        /// </summary>
        public static List<Element> FindElementsByName(Document doc, string name, bool exactMatch = false)
        {
            if (doc == null || string.IsNullOrEmpty(name))
                return new List<Element>();

            var collector = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType();

            if (exactMatch)
            {
                return collector.Where(e => e.Name != null && e.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                return collector.Where(e => e.Name != null && e.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
        }
    }
}
