﻿using System.IO;
using System.Xml;

namespace Xtate.DataModel.XPath
{
	internal static class XmlConverter
	{
		public static DataModelValue FromXml(string xml, object? entity = null)
		{
			entity.Is<XmlNameTable>(out var nameTable);

			nameTable ??= new NameTable();
			var namespaceManager = new XmlNamespaceManager(nameTable);

			if (entity.Is<IXmlNamespacesInfo>(out var namespacesInfo))
			{
				foreach (var prefixUri in namespacesInfo.Namespaces)
				{
					namespaceManager.AddNamespace(prefixUri.Prefix, prefixUri.Namespace);
				}
			}

			var context = new XmlParserContext(nameTable, namespaceManager, xmlLang: null, XmlSpace.None);

			using var reader = new StringReader(xml);
			using var xmlReader = XmlReader.Create(reader, settings: null, context);

			return LoadValue(xmlReader);
		}

		private static DataModelValue LoadValue(XmlReader xmlReader)
		{
			DataModelObject? obj = null;

			do
			{
				xmlReader.MoveToContent();
				switch (xmlReader.NodeType)
				{
					case XmlNodeType.Element:

						var metadata = GetMetaData(xmlReader);

						obj ??= new DataModelObject();

						if (!xmlReader.IsEmptyElement)
						{
							var name = xmlReader.LocalName;
							xmlReader.ReadStartElement();
							var value = LoadValue(xmlReader);

							obj.Add(name, value, metadata);
						}
						else
						{
							obj.Add(xmlReader.LocalName, string.Empty, metadata);
						}

						break;

					case XmlNodeType.EndElement:
						xmlReader.ReadEndElement();

						return obj;

					case XmlNodeType.Text:
						var text = xmlReader.Value;
						xmlReader.Read();

						return text;

					default:
						Infrastructure.UnexpectedValue();
						break;
				}
			} while (xmlReader.Read());

			return obj;
		}

		private static DataModelList? GetMetaData(XmlReader xmlReader)
		{
			var elementNs = xmlReader.NamespaceURI ?? string.Empty;
			var elementPrefix = xmlReader.Prefix ?? string.Empty;

			if (elementNs.Length == 0 && elementPrefix.Length == 0 && !xmlReader.HasAttributes)
			{
				return null;
			}

			var metadata = new DataModelArray();

			if (elementNs.Length > 0 || elementPrefix.Length > 0)
			{
				metadata.Add(elementNs);
			}

			if (elementPrefix.Length > 0)
			{
				metadata.Add(elementPrefix);
			}

			if (!xmlReader.HasAttributes)
			{
				return metadata;
			}

			for (var ok = xmlReader.MoveToFirstAttribute(); ok; ok = xmlReader.MoveToNextAttribute())
			{
				var name = xmlReader.LocalName;

				metadata.Add(name, xmlReader.Value, metadata: default);

				var attrNs = xmlReader.NamespaceURI ?? string.Empty;
				var attrPrefix = xmlReader.Prefix ?? string.Empty;

				if (attrNs.Length > 0 || attrPrefix.Length > 0)
				{
					metadata.Add(name, attrNs, metadata: default);
				}

				if (attrPrefix.Length > 0)
				{
					metadata.Add(name, attrPrefix, metadata: default);
				}
			}

			xmlReader.MoveToElement();

			return metadata;
		}
	}
}