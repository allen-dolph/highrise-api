using System;
using System.Collections.Generic;
using HighriseApi.Interfaces;
using HighriseApi.Models.Enums;
using RestSharp;

namespace HighriseApi.Requests
{
    public class EmailRequest : RequestBase, IEmailRequest
    {
        public EmailRequest(IRestClient client) : base(client) { }


        public IEnumerable<Email> Get(SubjectType subjectType, int subjectId, int? offset = null)
        {
            var url = offset.HasValue
                ? string.Format("{0}/{1}/emails.xml?n={2}", subjectType.ToString().ToLower(), subjectId, offset.Value)
                : string.Format("{0}/{1}/emails.xml", subjectType.ToString().ToLower(), subjectId);                

            var response = Client.Execute<List<Email>>(new RestRequest(url, Method.GET));
            return response.Data;
        }
     
        public IEnumerable<Email> Get(SubjectType subjectType, int subjectId, DateTime startDate, int? offset = null)
        {
            throw new NotImplementedException();
        }
    }
}
