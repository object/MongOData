using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using NUnit.Framework;
using Simple.Data;
using Simple.Data.OData;
using Simple.OData.Client;

namespace Mongo.Context.Tests
{
    public abstract class TestBase<T>
    {
        protected TestService service;
        protected dynamic ctx;

        protected abstract void PopulateTestData();

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            PopulateTestData();
            service = new TestService(typeof(T));
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            if (service != null)
            {
                service.Dispose();
                service = null;
            }

            TestData.Clean();
        }

        [SetUp]
        public virtual void SetUp()
        {
            ctx = Database.Opener.Open(service.ServiceUri);
        }

        protected void RequestAndValidateMetadata()
        {
            var request = (HttpWebRequest)WebRequest.Create(service.ServiceUri + "$metadata");
            var response = (HttpWebResponse)request.GetResponse();
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The $metadata didn't return success.");
        }

        protected ISchema GetSchema(string uri = null)
        {
            var client = new ODataClient(uri ?? service.ServiceUri.AbsoluteUri);
            return client.Schema;
        }

        protected void ValidateColumnNullability(ISchema schema)
        {
            Action<EdmProperty> validator = x => {
                    var message = string.Format("Property {0} of type {1} should {2}be marked as nullable",
                                  x.Name, x.Type, x.Nullable ? "not " : "");

                    if (x.Name == MongoMetadata.MappedObjectIdName || x.Type.Name.StartsWith("Collection("))
                        Assert.False(x.Nullable, message);
                    else
                        Assert.True(x.Nullable, message);
                };

            foreach (var table in schema.Tables)
            {
                var key = table.PrimaryKey.AsEnumerable();
                foreach (var column in table.Columns)
                {
                    if (key.Contains(column.ActualName))
                        Assert.False(column.IsNullable, "Column {0} belongs to a primary key and should not be marked as nullable", column.ActualName);

                    ValidateProperty(new EdmProperty
                        {
                            Name = column.ActualName,
                            Nullable = column.IsNullable,
                            Type = column.PropertyType
                        }, schema, validator);
                }
            }
        }

        protected void ValidatePropertyNames(ISchema schema)
        {
            Action<EdmProperty> validator = x => Assert.False(x.Name.StartsWith("_"), "Property {0} begins with invalid character", x.Name);

            foreach (var table in schema.Tables)
            {
                foreach (var column in table.Columns)
                {
                    ValidateProperty(new EdmProperty
                        {
                            Name = column.ActualName,
                            Nullable = column.IsNullable,
                            Type = column.PropertyType
                        }, schema, validator);
                }
            }
        }

        private void ValidateProperty(EdmProperty property, ISchema schema, Action<EdmProperty> validator)
        {
            validator(property);

            if (property.Type is EdmComplexPropertyType)
            {
                var complexType = schema.ComplexTypes.Single(x => x.Name == property.Type.Name);
                foreach (var prop in complexType.Properties)
                {
                    ValidateProperty(prop, schema, validator);
                }
            }
            else if (property.Type is EdmCollectionPropertyType)
            {
                var baseType = (property.Type as EdmCollectionPropertyType).BaseType;
                if (baseType is EdmComplexPropertyType)
                {
                    var complexType = schema.ComplexTypes.Single(x => x.Name == baseType.Name);
                    foreach (var prop in complexType.Properties)
                    {
                        ValidateProperty(prop, schema, validator);
                    }
                }
            }
        }
    }
}
