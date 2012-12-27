using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Text;

namespace SampleWcfDataServiceClient
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Connecting to MongoDB sample OData service...");
                var context = new ServiceReference.MongoContext(new Uri("http://localhost:50336/MongoDataService.svc/"));

                Console.WriteLine("Retrieving categories...");
                Console.WriteLine();
                foreach (var category in context.Categories)
                {
                    Console.WriteLine("Category: ID=[{0}], Name=[{1}]",
                        category.ID,
                        category.Name);
                }
                Console.WriteLine();

                Console.WriteLine("Retrieving products...");
                Console.WriteLine();
                foreach (var product in context.Products)
                {
                    Console.WriteLine("Product: ID=[{0}], Name=[{1}], Category=[{2}], Quantity=[{3} {4}], ReleaseDate=[{5}], DiscontinueDate=[{6}]",
                        product.ID,
                        product.Name,
                        product.Category.Name,
                        product.Quantity.Value,
                        product.Quantity.Units,
                        product.ReleaseDate,
                        product.DiscontinueDate);
                }

                Console.WriteLine();
                Console.WriteLine("Completed.");
            }
            catch (Exception exception)
            {
                if (exception is DataServiceClientException || exception.InnerException is DataServiceClientException)
                {
                    Console.WriteLine("The application was unable to process OData service response. This program doesn't support OData v.3 protocol. Check service metadata, in case its DataServiceVersion is set to 3, its data may not be consumed by this program.");
                }
                else
                {
                    Console.WriteLine("Error: {0}", exception.Message);
                }
            }
        }
    }
}
