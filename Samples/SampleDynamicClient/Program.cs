using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simple.Data;
using Simple.Data.OData;

namespace SampleDynamicClient
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Connecting to MongoDB sample OData service...");
                dynamic context = Database.Opener.Open("http://localhost:50336/MongoDataService.svc/");

                Console.WriteLine("Retrieving categories...");
                Console.WriteLine();
                foreach (var category in context.Categories.All().ToList())
                {
                    Console.WriteLine("Category: ID=[{0}], Name=[{1}]",
                        category.ID,
                        category.Name);
                }
                Console.WriteLine();

                Console.WriteLine("Retrieving products...");
                Console.WriteLine();
                foreach (var product in context.Products.All().ToList())
                {
                    Console.WriteLine("Product: ID=[{0}], Name=[{1}], Category=[{2}], Quantity=[{3} {4}], "
                                    + "ReleaseDate=[{5}], DiscontinueDate=[{6}], Suppier=[{7}]",
                        product.ID,
                        product.Name,
                        product.Category.Name,
                        product.Quantity.Value,
                        product.Quantity.Units,
                        product.ReleaseDate,
                        product.DiscontinueDate,
                        product.Supplier == null ? null : product.Supplier.Name);
                }

                Console.WriteLine("Retrieving JSON samples...");
                Console.WriteLine();
                Console.WriteLine("Retrieved {0} ArrayOfNested documents", context.ArrayOfNested.All().Count());
                Console.WriteLine("Retrieved {0} Colors documents", context.Colors.All().Count());
                Console.WriteLine("Retrieved {0} EmptyArray documents", context.EmptyArray.All().Count());
                Console.WriteLine("Retrieved {0} Facebook documents", context.Facebook.All().Count());
                Console.WriteLine("Retrieved {0} Flickr documents", context.Flickr.All().Count());
                Console.WriteLine("Retrieved {0} GoogleMaps documents", context.GoogleMaps.All().Count());
                Console.WriteLine("Retrieved {0} iPhone documents", context.iPhone.All().Count());
                Console.WriteLine("Retrieved {0} Nested documents", context.Nested.All().Count());
                Console.WriteLine("Retrieved {0} NullArray documents", context.NullArray.All().Count());
                Console.WriteLine("Retrieved {0} Twitter documents", context.Twitter.All().Count());
                Console.WriteLine("Retrieved {0} YouTube documents", context.YouTube.All().Count());

                Console.WriteLine();
                Console.WriteLine("Completed.");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: {0}", exception.Message);
            }
        }
    }
}
