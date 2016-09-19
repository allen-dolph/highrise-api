using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using RestSharp;
using RestSharp.Deserializers;
using RestSharp.Extensions;

namespace HighriseApi.Serializers
{
    public class XmlDebuggingDeserializer : IDeserializer
    {
        public string RootElement { get; set; }

        public string Namespace { get; set; }

        public string DateFormat { get; set; }

        public CultureInfo Culture { get; set; }

        public XmlDebuggingDeserializer()
        {
            this.Culture = CultureInfo.InvariantCulture;
        }

        public virtual T Deserialize<T>(IRestResponse response)
        {
            if (string.IsNullOrEmpty(response.Content))
                return default(T);
            XDocument xdoc = XDocument.Parse(response.Content);
            XElement root = xdoc.Root;
            if (StringExtensions.HasValue(this.RootElement) && xdoc.Root != null)
                root = xdoc.Root.Element(XmlExtensions.AsNamespaced(this.RootElement, this.Namespace));
            if (!StringExtensions.HasValue(this.Namespace))
                XmlDebuggingDeserializer.RemoveNamespace(xdoc);
            T instance = Activator.CreateInstance<T>();
            Type type = instance.GetType();
            return !ReflectionExtensions.IsSubclassOfRawGeneric(type, typeof(List<>)) ? (T)this.Map((object)instance, root) : (T)this.HandleListDerivative(root, type.Name, type);
        }

        private static void RemoveNamespace(XDocument xdoc)
        {
            if (xdoc.Root == null)
                return;
            foreach (XElement xelement in xdoc.Root.DescendantsAndSelf())
            {
                if (xelement.Name.Namespace != XNamespace.None)
                    xelement.Name = XNamespace.None.GetName(xelement.Name.LocalName);
                if (Enumerable.Any<XAttribute>(xelement.Attributes(), (Func<XAttribute, bool>)(a =>
                {
                    if (!a.IsNamespaceDeclaration)
                        return a.Name.Namespace != XNamespace.None;
                    return true;
                })))
                    xelement.ReplaceAttributes((object)Enumerable.Select<XAttribute, XAttribute>(xelement.Attributes(), (Func<XAttribute, XAttribute>)(a =>
                    {
                        if (a.IsNamespaceDeclaration)
                            return (XAttribute)null;
                        if (!(a.Name.Namespace != XNamespace.None))
                            return a;
                        return new XAttribute(XNamespace.None.GetName(a.Name.LocalName), (object)a.Value);
                    })));
            }
        }

        protected virtual object Map(object x, XElement root)
        {
            foreach (PropertyInfo prop in x.GetType().GetProperties())
            {
                try
                {
                    Type type = prop.PropertyType;
                    if ((type.IsPublic || type.IsNestedPublic) && prop.CanWrite)
                    {
                        object[] customAttributes = prop.GetCustomAttributes(typeof (DeserializeAsAttribute), false);
                        XName name = customAttributes.Length <= 0
                            ? XmlExtensions.AsNamespaced(prop.Name, this.Namespace)
                            : XmlExtensions.AsNamespaced(((DeserializeAsAttribute) customAttributes[0]).Name,
                                this.Namespace);
                        object valueFromXml = this.GetValueFromXml(root, name, prop);
                        if (valueFromXml == null)
                        {
                            if (type.IsGenericType)
                            {
                                Type t = type.GetGenericArguments()[0];
                                XElement elementByName = this.GetElementByName(root, (XName) t.Name);
                                IList list = (IList) Activator.CreateInstance(type);
                                if (elementByName != null && root != null)
                                {
                                    IEnumerable<XElement> elements = root.Elements(elementByName.Name);
                                    this.PopulateListFromElements(t, elements, list);
                                }
                                prop.SetValue(x, (object) list, (object[]) null);
                            }
                        }
                        else
                        {
                            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>))
                            {
                                if (string.IsNullOrEmpty(valueFromXml.ToString()))
                                {
                                    prop.SetValue(x, (object) null, (object[]) null);
                                    continue;
                                }
                                type = type.GetGenericArguments()[0];
                            }
                            if (type == typeof (bool))
                            {
                                string s = valueFromXml.ToString().ToLower();
                                prop.SetValue(x, (object) (bool) (XmlConvert.ToBoolean(s) ? true : false),
                                    (object[]) null);
                            }
                            else if (type.IsPrimitive)
                                prop.SetValue(x, ReflectionExtensions.ChangeType(valueFromXml, type, this.Culture),
                                    (object[]) null);
                            else if (type.IsEnum)
                            {
                                object enumValue = ReflectionExtensions.FindEnumValue(type, valueFromXml.ToString(),
                                    this.Culture);
                                prop.SetValue(x, enumValue, (object[]) null);
                            }
                            else if (type == typeof (Uri))
                            {
                                Uri uri = new Uri(valueFromXml.ToString(), UriKind.RelativeOrAbsolute);
                                prop.SetValue(x, (object) uri, (object[]) null);
                            }
                            else if (type == typeof (string))
                                prop.SetValue(x, valueFromXml, (object[]) null);
                            else if (type == typeof (DateTime))
                            {
                                object obj =
                                    (object)
                                        (StringExtensions.HasValue(this.DateFormat)
                                            ? DateTime.ParseExact(valueFromXml.ToString(), this.DateFormat,
                                                (IFormatProvider) this.Culture)
                                            : DateTime.Parse(valueFromXml.ToString(), (IFormatProvider) this.Culture));
                                prop.SetValue(x, obj, (object[]) null);
                            }
                            else if (type == typeof (DateTimeOffset))
                            {
                                string str = valueFromXml.ToString();
                                if (!string.IsNullOrEmpty(str))
                                {
                                    try
                                    {
                                        DateTimeOffset dateTimeOffset = XmlConvert.ToDateTimeOffset(str);
                                        prop.SetValue(x, (object) dateTimeOffset, (object[]) null);
                                    }
                                    catch (Exception ex)
                                    {
                                        object result;
                                        if (XmlDebuggingDeserializer.TryGetFromString(str, out result, type))
                                        {
                                            prop.SetValue(x, result, (object[]) null);
                                        }
                                        else
                                        {
                                            DateTimeOffset dateTimeOffset = DateTimeOffset.Parse(str);
                                            prop.SetValue(x, (object) dateTimeOffset, (object[]) null);
                                        }
                                    }
                                }
                            }
                            else if (type == typeof (Decimal))
                            {
                                object obj =
                                    (object) Decimal.Parse(valueFromXml.ToString(), (IFormatProvider) this.Culture);
                                prop.SetValue(x, obj, (object[]) null);
                            }
                            else if (type == typeof (Guid))
                            {
                                object obj =
                                    (object)
                                        (string.IsNullOrEmpty(valueFromXml.ToString())
                                            ? Guid.Empty
                                            : new Guid(valueFromXml.ToString()));
                                prop.SetValue(x, obj, (object[]) null);
                            }
                            else if (type == typeof (TimeSpan))
                            {
                                TimeSpan timeSpan = XmlConvert.ToTimeSpan(valueFromXml.ToString());
                                prop.SetValue(x, (object) timeSpan, (object[]) null);
                            }
                            else if (type.IsGenericType)
                            {
                                Type t = type.GetGenericArguments()[0];
                                IList list = (IList) Activator.CreateInstance(type);
                                XElement elementByName = this.GetElementByName(root,
                                    XmlExtensions.AsNamespaced(prop.Name, this.Namespace));
                                if (elementByName.HasElements)
                                {
                                    XElement xelement = Enumerable.FirstOrDefault<XElement>(elementByName.Elements());
                                    if (xelement != null)
                                    {
                                        IEnumerable<XElement> elements = elementByName.Elements(xelement.Name);
                                        this.PopulateListFromElements(t, elements, list);
                                    }
                                }
                                prop.SetValue(x, (object) list, (object[]) null);
                            }
                            else if (ReflectionExtensions.IsSubclassOfRawGeneric(type, typeof (List<>)))
                            {
                                object obj = this.HandleListDerivative(root, prop.Name, type);
                                prop.SetValue(x, obj, (object[]) null);
                            }
                            else
                            {
                                object result;
                                if (XmlDebuggingDeserializer.TryGetFromString(valueFromXml.ToString(), out result, type))
                                    prop.SetValue(x, result, (object[]) null);
                                else if (root != null)
                                {
                                    XElement elementByName = this.GetElementByName(root, name);
                                    if (elementByName != null)
                                    {
                                        object andMap = this.CreateAndMap(type, elementByName);
                                        prop.SetValue(x, andMap, (object[]) null);
                                    }
                                }
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    throw;
                }

            }
            return x;
        }

        private static bool TryGetFromString(string inputString, out object result, Type type)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertFrom(typeof(string)))
            {
                result = converter.ConvertFromInvariantString(inputString);
                return true;
            }
            result = (object)null;
            return false;
        }

        private void PopulateListFromElements(Type t, IEnumerable<XElement> elements, IList list)
        {
            foreach (object obj in Enumerable.Select<XElement, object>(elements, (Func<XElement, object>)(element => this.CreateAndMap(t, element))))
                list.Add(obj);
        }

        private object HandleListDerivative(XElement root, string propName, Type type)
        {
            Type type1 = type.IsGenericType ? type.GetGenericArguments()[0] : type.BaseType.GetGenericArguments()[0];
            IList list1 = (IList)Activator.CreateInstance(type);
            IList<XElement> list2 = (IList<XElement>)Enumerable.ToList<XElement>(root.Descendants(XmlExtensions.AsNamespaced(type1.Name, this.Namespace)));
            string name = type1.Name;
            DeserializeAsAttribute attribute = ReflectionExtensions.GetAttribute<DeserializeAsAttribute>(type1);
            if (attribute != null)
                name = attribute.Name;
            if (!Enumerable.Any<XElement>((IEnumerable<XElement>)list2))
            {
                XName name1 = XmlExtensions.AsNamespaced(name.ToLower(), this.Namespace);
                list2 = (IList<XElement>)Enumerable.ToList<XElement>(root.Descendants(name1));
            }
            if (!Enumerable.Any<XElement>((IEnumerable<XElement>)list2))
            {
                XName name1 = XmlExtensions.AsNamespaced(StringExtensions.ToCamelCase(name, this.Culture), this.Namespace);
                list2 = (IList<XElement>)Enumerable.ToList<XElement>(root.Descendants(name1));
            }
            if (!Enumerable.Any<XElement>((IEnumerable<XElement>)list2))
                list2 = (IList<XElement>)Enumerable.ToList<XElement>(Enumerable.Where<XElement>(root.Descendants(), (Func<XElement, bool>)(e => StringExtensions.RemoveUnderscoresAndDashes(e.Name.LocalName) == name)));
            if (!Enumerable.Any<XElement>((IEnumerable<XElement>)list2))
            {
                XName lowerName = XmlExtensions.AsNamespaced(name.ToLower(), this.Namespace);
                list2 = (IList<XElement>)Enumerable.ToList<XElement>(Enumerable.Where<XElement>(root.Descendants(), (Func<XElement, bool>)(e => (XName)StringExtensions.RemoveUnderscoresAndDashes(e.Name.LocalName) == lowerName)));
            }
            this.PopulateListFromElements(type1, (IEnumerable<XElement>)list2, list1);
            if (!type.IsGenericType)
                this.Map((object)list1, root.Element(XmlExtensions.AsNamespaced(propName, this.Namespace)) ?? root);
            return (object)list1;
        }

        protected virtual object CreateAndMap(Type t, XElement element)
        {
            object x;
            if (t == typeof(string))
                x = (object)element.Value;
            else if (t.IsPrimitive)
            {
                x = ReflectionExtensions.ChangeType((object)element.Value, t, this.Culture);
            }
            else
            {
                x = Activator.CreateInstance(t);
                this.Map(x, element);
            }
            return x;
        }

        protected virtual object GetValueFromXml(XElement root, XName name, PropertyInfo prop)
        {
            object obj = (object)null;
            if (root != null)
            {
                XElement elementByName = this.GetElementByName(root, name);
                if (elementByName == null)
                {
                    XAttribute attributeByName = this.GetAttributeByName(root, name);
                    if (attributeByName != null)
                        obj = (object)attributeByName.Value;
                }
                else if (!elementByName.IsEmpty || elementByName.HasElements || elementByName.HasAttributes)
                    obj = (object)elementByName.Value;
            }
            return obj;
        }

        protected virtual XElement GetElementByName(XElement root, XName name)
        {
            XName name1 = XmlExtensions.AsNamespaced(name.LocalName.ToLower(), name.NamespaceName);
            XName name2 = XmlExtensions.AsNamespaced(StringExtensions.ToCamelCase(name.LocalName, this.Culture), name.NamespaceName);
            if (root.Element(name) != null)
                return root.Element(name);
            if (root.Element(name1) != null)
                return root.Element(name1);
            if (root.Element(name2) != null)
                return root.Element(name2);
            if (name == XmlExtensions.AsNamespaced("Value", name.NamespaceName))
                return root;
            return Enumerable.FirstOrDefault<XElement>((IEnumerable<XElement>)Enumerable.OrderBy<XElement, int>(root.Descendants(), (Func<XElement, int>)(d => Enumerable.Count<XElement>(d.Ancestors()))), (Func<XElement, bool>)(d => StringExtensions.RemoveUnderscoresAndDashes(d.Name.LocalName) == name.LocalName)) ?? Enumerable.FirstOrDefault<XElement>((IEnumerable<XElement>)Enumerable.OrderBy<XElement, int>(root.Descendants(), (Func<XElement, int>)(d => Enumerable.Count<XElement>(d.Ancestors()))), (Func<XElement, bool>)(d => StringExtensions.RemoveUnderscoresAndDashes(d.Name.LocalName) == name.LocalName.ToLower()));
        }

        protected virtual XAttribute GetAttributeByName(XElement root, XName name)
        {
            List<XName> names = new List<XName>()
      {
        (XName) name.LocalName,
        XmlExtensions.AsNamespaced(name.LocalName.ToLower(), name.NamespaceName),
        XmlExtensions.AsNamespaced(StringExtensions.ToCamelCase(name.LocalName, this.Culture), name.NamespaceName)
      };
            return Enumerable.FirstOrDefault<XAttribute>(Extensions.Attributes((IEnumerable<XElement>)Enumerable.OrderBy<XElement, int>(root.DescendantsAndSelf(), (Func<XElement, int>)(d => Enumerable.Count<XElement>(d.Ancestors())))), (Func<XAttribute, bool>)(d => names.Contains((XName)StringExtensions.RemoveUnderscoresAndDashes(d.Name.LocalName))));
        }
    }
}
