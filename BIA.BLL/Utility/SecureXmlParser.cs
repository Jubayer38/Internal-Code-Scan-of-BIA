using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace BIA.BLL.Utility;

public static class SecureXmlParser
{
    public static XDocument Parse(string xmlString) 
    {
        if (string.IsNullOrWhiteSpace(xmlString))
            throw new ArgumentException("XML string cannot be null or empty.", nameof(xmlString));

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };

        using var stringReader = new StringReader(xmlString);
        using var reader = XmlReader.Create(stringReader, settings);
        return XDocument.Load(reader);
    }
}
