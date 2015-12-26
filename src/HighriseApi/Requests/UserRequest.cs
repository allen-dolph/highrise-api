using System;
using System.Collections.Generic;
using HighriseApi.Interfaces;
using HighriseApi.Models;
using RestSharp;

namespace HighriseApi.Requests
{
    public class UserRequest : RequestBase, IUserRequest
    {
        public UserRequest(IRestClient client) : base(client) { }

        public User Me()
        {
            var response = Client.Execute<User>(new RestRequest("me.xml", Method.GET));
            return response.Data;
        }

        public User Get(int id)
        {
            var response = Client.Execute<User>(new RestRequest(String.Format("users/{0}.xml", id), Method.GET));
            return response.Data;
        }

        public IEnumerable<User> Get()
        {
            var response = Client.Execute<List<User>>(new RestRequest("users.xml", Method.GET));
            return response.Data;
        }
    }
}
