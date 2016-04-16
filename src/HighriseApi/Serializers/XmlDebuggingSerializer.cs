using RestSharp.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using RestSharp.Serializers;

namespace HighriseApi.Serializers
{
    /// <summary>
    /// Default XML Serializer
    /// 
    /// </summary>
    public class XmlDebuggingSerializer : ISerializer
    {
        /// <summary>
        /// Name of the root element to use when serializing
        /// 
        /// </summary>
        public string RootElement { get; set; }

        /// <summary>
        /// XML namespace to use when serializing
        /// 
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Format string to use when serializing dates
        /// 
        /// </summary>
        public string DateFormat { get; set; }

        /// <summary>
        /// Content type for serialized content
        /// 
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Default constructor, does not specify namespace
        /// 
        /// </summary>
        public XmlDebuggingSerializer()
        {
            this.ContentType = "text/xml";
        }

        /// <summary>
        /// Specify the namespaced to be used when serializing
        /// 
        /// </summary>
        /// <param name="namespace">XML namespace</param>
        public XmlDebuggingSerializer(string @namespace)
        {
            this.Namespace = @namespace;
            this.ContentType = "text/xml";
        }

        /// <summary>
        /// Serialize the object as XML
        /// 
        /// </summary>
        /// <param name="obj">Object to serialize</param>
        /// <returns>
        /// XML as string
        /// </returns>
        public string Serialize(object obj)
        {
            XDocument xdocument = new XDocument();
            Type type1 = obj.GetType();
            string name1 = type1.Name;
            SerializeAsAttribute attribute1 = ReflectionExtensions.GetAttribute<SerializeAsAttribute>(type1);
            if (attribute1 != null)
                name1 = attribute1.TransformName(attribute1.Name ?? name1);
            XElement xelement1 = new XElement(XmlExtensions.AsNamespaced(name1, this.Namespace));
            if (obj is IList)
            {
                string name2 = "";
                foreach (object obj1 in (IEnumerable)obj)
                {
                    Type type2 = obj1.GetType();
                    SerializeAsAttribute attribute2 = ReflectionExtensions.GetAttribute<SerializeAsAttribute>(type2);
                    if (attribute2 != null)
                        name2 = attribute2.TransformName(attribute2.Name ?? name1);
                    if (name2 == "")
                        name2 = type2.Name;
                    XElement xelement2 = new XElement(XmlExtensions.AsNamespaced(name2, this.Namespace));
                    this.Map((XContainer)xelement2, obj1);
                    xelement1.Add((object)xelement2);
                }
            }
            else
                this.Map((XContainer)xelement1, obj);
            if (StringExtensions.HasValue(this.RootElement))
            {
                XElement xelement2 = new XElement(XmlExtensions.AsNamespaced(this.RootElement, this.Namespace), (object)xelement1);
                xdocument.Add((object)xelement2);
            }
            else
                xdocument.Add((object)xelement1);
            return xdocument.ToString();
        }

        private void Map(XContainer root, object obj)
        {
            Type type1 = obj.GetType();
            IEnumerable<PropertyInfo> enumerable = Enumerable.Select(Enumerable.OrderBy(Enumerable.Where(Enumerable.Select((IEnumerable<PropertyInfo>)type1.GetProperties(), p => new
            {
                p = p,
                indexAttribute = ReflectionExtensions.GetAttribute<SerializeAsAttribute>((MemberInfo)p)
            }), param0 =>
            {
                if (param0.p.CanRead)
                    return param0.p.CanWrite;
                return false;
            }), param0 =>
            {
                if (param0.indexAttribute != null)
                    return param0.indexAttribute.Index;
                return int.MaxValue;
            }), param0 => param0.p);
            SerializeAsAttribute attribute1 = ReflectionExtensions.GetAttribute<SerializeAsAttribute>(type1);
            foreach (PropertyInfo propertyInfo in enumerable)
            {
                string str = propertyInfo.Name;
                object obj1 = propertyInfo.GetValue(obj, (object[])null);
                if (obj1 != null)
                {
                    string serializedValue = this.GetSerializedValue(obj1);
                    Type propertyType = propertyInfo.PropertyType;
                    bool flag = false;
                    SerializeAsAttribute attribute2 = ReflectionExtensions.GetAttribute<SerializeAsAttribute>((MemberInfo)propertyInfo);
                    if (attribute2 != null)
                    {
                        str = StringExtensions.HasValue(attribute2.Name) ? attribute2.Name : str;
                        flag = attribute2.Attribute;
                    }
                    SerializeAsAttribute attribute3 = ReflectionExtensions.GetAttribute<SerializeAsAttribute>((MemberInfo)propertyInfo);
                    if (attribute3 != null)
                        str = attribute3.TransformName(str);
                    else if (attribute1 != null)
                        str = attribute1.TransformName(str);
                    XElement xelement1 = new XElement(XmlExtensions.AsNamespaced(str, this.Namespace));
                    if (propertyType.IsPrimitive || propertyType.IsValueType || propertyType == typeof(string))
                    {
                        if (flag)
                        {
                            root.Add((object)new XAttribute((XName)str, (object)serializedValue));
                            continue;
                        }
                        xelement1.Value = serializedValue;
                    }
                    else if (obj1 is IList)
                    {
                        string name = "";
                        foreach (object obj2 in (IEnumerable)obj1)
                        {
                            if (name == "")
                            {
                                Type type2 = obj2.GetType();
                                SerializeAsAttribute attribute4 = ReflectionExtensions.GetAttribute<SerializeAsAttribute>(type2);
                                name = attribute4 == null || !StringExtensions.HasValue(attribute4.Name) ? type2.Name : attribute4.Name;
                            }
                            XElement xelement2 = new XElement(XmlExtensions.AsNamespaced(name, this.Namespace));
                            this.Map((XContainer)xelement2, obj2);
                            xelement1.Add((object)xelement2);
                        }
                    }
                    else
                        this.Map((XContainer)xelement1, obj1);
                    root.Add((object)xelement1);
                }
            }
        }

        private string GetSerializedValue(object obj)
        {
            object obj1 = obj;
            if (obj is DateTime && StringExtensions.HasValue(this.DateFormat))
                obj1 = (object)((DateTime)obj).ToString(this.DateFormat, (IFormatProvider)CultureInfo.InvariantCulture);
            if (obj is bool)
                obj1 = (object)((bool)obj).ToString((IFormatProvider)CultureInfo.InvariantCulture).ToLower();
            if (XmlDebuggingSerializer.IsNumeric(obj))
                return XmlDebuggingSerializer.SerializeNumber(obj);
            return obj1.ToString();
        }

        private static string SerializeNumber(object number)
        {
            if (number is long)
                return ((long)number).ToString((IFormatProvider)CultureInfo.InvariantCulture);
            if (number is ulong)
                return ((ulong)number).ToString((IFormatProvider)CultureInfo.InvariantCulture);
            if (number is int)
                return ((int)number).ToString((IFormatProvider)CultureInfo.InvariantCulture);
            if (number is uint)
                return ((uint)number).ToString((IFormatProvider)CultureInfo.InvariantCulture);
            if (number is Decimal)
                return ((Decimal)number).ToString((IFormatProvider)CultureInfo.InvariantCulture);
            if (number is float)
                return ((float)number).ToString((IFormatProvider)CultureInfo.InvariantCulture);
            return Convert.ToDouble(number, (IFormatProvider)CultureInfo.InvariantCulture).ToString("r", (IFormatProvider)CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Determines if a given object is numeric in any way
        ///             (can be integer, double, null, etc).
        /// 
        /// </summary>
        private static bool IsNumeric(object value)
        {
            return value is sbyte || value is byte || (value is short || value is ushort) || (value is int || value is uint || (value is long || value is ulong)) || (value is float || value is double || value is Decimal);
        }
    }
}
