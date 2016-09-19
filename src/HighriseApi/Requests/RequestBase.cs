using RestSharp;
using System.Net;

namespace HighriseApi.Requests
{
    public class RequestBase
    {
        protected readonly IRestClient Client;

        public RequestBase() 
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, certificate, chain, sslPolicyErrors) => true;
        }

        public RequestBase(IRestClient client)
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, certificate, chain, sslPolicyErrors) => true;

            Client = client;
        }
    }
}
