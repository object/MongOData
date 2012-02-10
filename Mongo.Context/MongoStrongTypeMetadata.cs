using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Text;
using DataServiceProvider;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Mongo.Context
{
    public class MongoStrongTypeMetadata : MongoMetadataBase
    {
        private List<Tuple<ResourceType, string, string>> resourceReferences = new List<Tuple<ResourceType, string, string>>();
        private List<Tuple<ResourceType, string, string>> resourceSetReferences = new List<Tuple<ResourceType, string, string>>();

        protected override void PopulateMetadata(DSPMetadata metadata, MongoContext context)
        {
            foreach (var collectionName in GetCollectionNames(context))
            {
                var collection = context.Database.GetCollection(collectionName);
                var document = collection.FindOne();
                if (document != null)
                {
                    var collectionType = metadata.AddEntityType(collectionName);

                    foreach (var element in document.Elements)
                    {
                        var elementType = GetElementType(context, element);
                        if (element.Name == "_id")
                        {
                            metadata.AddKeyProperty(collectionType, "ID", elementType);
                        }
                        else if (elementType == typeof(BsonDocument))
                        {
                            string referencedCollectionName = GetDocumentCollection(context, element.Value.AsBsonDocument).Name;
                            resourceReferences.Add(new Tuple<ResourceType, string, string>(collectionType, element.Name, referencedCollectionName));
                        }
                        else if (elementType == typeof(BsonArray))
                        {
                            var bsonArray = element.Value.AsBsonArray;
                            if (bsonArray != null && bsonArray.Count > 0)
                            {
                                string referencedCollectionName = GetDocumentCollection(context, bsonArray[0].AsBsonDocument).Name;
                                resourceSetReferences.Add(new Tuple<ResourceType, string, string>(collectionType, element.Name, referencedCollectionName));
                            }
                        }
                        else
                        {
                            metadata.AddPrimitiveProperty(collectionType, element.Name, elementType);
                        }
                    }
                    metadata.AddResourceSet(collectionName, collectionType);
                }
            }

            foreach (var reference in resourceReferences)
            {
                var referencedResourceSet = metadata.ResourceSets.Where(x => x.Name == reference.Item3).SingleOrDefault();
                if (referencedResourceSet != null)
                {
                    metadata.AddResourceSetReferenceProperty(reference.Item1, reference.Item2, referencedResourceSet);
                }
            }

            foreach (var reference in resourceSetReferences)
            {
                var referencedResourceSet = metadata.ResourceSets.Where(x => x.Name == reference.Item3).SingleOrDefault();
                if (referencedResourceSet != null)
                {
                    metadata.AddResourceSetReferenceProperty(reference.Item1, reference.Item2, referencedResourceSet);
                }
            }
        }

        private Type GetElementType(MongoContext context, BsonElement element)
        {
            if (element.Value.RawValue != null)
            {
                if (element.Value.RawValue.GetType() == typeof(ObjectId))
                    return typeof(string);
                else
                    return element.Value.RawValue.GetType();
            }
            else if (element.Value.GetType() == typeof(BsonArray) || element.Value.GetType() == typeof(BsonDocument))
            {
                return element.Value.GetType();
            }
            else
            {
                return typeof(string);
            }
        }

        private MongoCollection<BsonDocument> GetDocumentCollection(MongoContext context, BsonDocument document)
        {
            var id = (ObjectId)document["_id"];
            foreach (var collectionName in GetCollectionNames(context))
            {
                var collection = context.Database.GetCollection(collectionName);
                if (collection.FindOne(Query.EQ("_id", BsonValue.Create(id))) != null)
                {
                    return collection;
                }
            }
            return null;
        }
    }
}
