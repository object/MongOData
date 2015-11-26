

using System;
using System.ServiceModel.Web;
using System.Threading;

namespace Mongo.Context.Tests
{
    public class TestService : IDisposable
    {
        private WebServiceHost _host;
        private Uri _serviceUri;
        private static int s_lastHostId = 1;

        public TestService(Type serviceType)
        {
            for (int i = 0; i < 100; i++)
            {
                int hostId = Interlocked.Increment(ref s_lastHostId);
                _serviceUri = new Uri("http://" + Environment.MachineName + "/Temporary_Listen_Addresses/MongoTestService" + hostId.ToString() + "/");
                _host = new WebServiceHost(serviceType, _serviceUri);
                try
                {
                    _host.Open();
                    break;
                }
                catch (Exception)
                {
                    _host.Abort();
                    _host = null;
                }
            }

            if (_host == null)
            {
                throw new InvalidOperationException("Could not open a service even after 100 tries.");
            }
        }

        public void Dispose()
        {
            if (_host != null)
            {
                _host.Close();
                _host = null;
            }
        }

        public Uri ServiceUri
        {
            get { return _serviceUri; }
        }

        public static MongoConfiguration Configuration { get; set; }
    }
}
