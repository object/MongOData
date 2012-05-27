using System.Configuration;
using System.Xml;

namespace Mongo.Context
{
    public sealed class MongoConfiguration : IConfigurationSectionHandler
    {
        public class Metadata
        {
            public int PrefetchRows { get; set; }
            public bool UpdateDynamically { get; set; }
            public bool PersistSchema { get; set; }

            public static Metadata Default
            {
                get { return new Metadata { PrefetchRows = 1, UpdateDynamically = false, PersistSchema = false }; }
            }
        }

        public const string SectionName = "MongOData";

        public Metadata MetadataBuildStrategy { get; set; }

        public MongoConfiguration()
        {
            this.MetadataBuildStrategy = Metadata.Default;
        }

        public object Create(object parent, object configContext, XmlNode section)
        {
            var configuration = new MongoConfiguration();
            if (section != null)
            {
                int iResult;
                bool bResult;
                if (TryReadConfigurationValue(section, "metadataBuildStrategy/prefetchRows", out iResult))
                    configuration.MetadataBuildStrategy.PrefetchRows = iResult;
                if (TryReadConfigurationValue(section, "metadataBuildStrategy/updateDynamically", out bResult))
                    configuration.MetadataBuildStrategy.UpdateDynamically = bResult;
                if (TryReadConfigurationValue(section, "metadataBuildStrategy/persistSchema", out bResult))
                    configuration.MetadataBuildStrategy.PersistSchema = bResult;
            }
            return configuration;
        }

        private bool TryReadConfigurationValue(XmlNode parentNode, string elementName, out int result)
        {
            result = 0;
            var element = parentNode.SelectSingleNode(elementName);
            if (element != null && !string.IsNullOrEmpty(element.InnerText))
            {
                return int.TryParse(element.InnerText, out result);
            }
            return false;
        }

        private bool TryReadConfigurationValue(XmlNode parentNode, string elementName, out bool result)
        {
            result = false;
            var element = parentNode.SelectSingleNode(elementName);
            if (element != null && !string.IsNullOrEmpty(element.InnerText))
            {
                return bool.TryParse(element.InnerText, out result);
            }
            return false;
        }
    }
}