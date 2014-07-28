// XmlHlps.cs
//
// Copyright 2014 Wave Software Limited.
//

using System;
using System.Xml;

sealed class XmlHlps
{
	public static XmlNode CreateNode(XmlNode parentNode, string name, string namespaceUri)
	{
		if (null == parentNode)
			throw new ArgumentNullException("parentNode");

		XmlNode child_node = parentNode.OwnerDocument.CreateNode(
			XmlNodeType.Element, 
			name, 
			namespaceUri);

		parentNode.AppendChild(child_node);

		return child_node;
	}

	public static XmlNode CreateNode(XmlNode parentNode, string name)
	{
		return CreateNode(parentNode, name, null);
	}

	public static void SetValue(XmlNode node, string value)
	{
		if (null == node)
			throw new ArgumentNullException("node");

		node.InnerText = value;
	}

	public static void SetValue(XmlNode node, int value)
	{
		SetValue(node, value.ToString());
	}

	public static void SetAttr(XmlNode node, string name, string value)
	{
		XmlElement e = node as XmlElement;

		e.SetAttribute(name, value);
	}

	public static void SetAttr(XmlNode node, string name, int value)
	{
		SetAttr(node, name, value.ToString());
	}

	public static bool GetAttr(XmlNode node, string name, ref string value, bool mustExist)
	{
		if (null == node)
			throw new ArgumentNullException("node");

		XmlNamedNodeMap attrs = node.Attributes;

		if (null == attrs)
		{
			if (mustExist)
				throw new Exception("attrs null");

			return false;
		}

		XmlNode attr_node = attrs.GetNamedItem(name);

		if (null == attr_node)
		{
			if (mustExist)
				throw new Exception("attr not found: " + name);

			return false;
		}

		value = attr_node.Value;

		return true;
	}
}
