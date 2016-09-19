using HighriseApi.Interfaces;
using HighriseApi.Models;
using HighriseApi.Models.Enums;
using HighriseApi.Serializers;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;


namespace HighriseApi.Requests
{
    public class NoteRequest : RequestBase, INoteRequest
    {
        public NoteRequest(IRestClient client) : base(client) { }

        public Note Get(int id)
        {
            var response = Client.Execute<Note>(new RestRequest(String.Format("notes/{0}.xml", id), Method.GET));
            return response.Data;            
        }

        public IEnumerable<Note> Get(int subjectId, SubjectType subjectType)
        {
            var response = Client.Execute<List<Note>>(new RestRequest(String.Format("{0}/{1}/notes.xml", subjectType.ToString().ToLower(), subjectId), Method.GET));
            return response.Data;            
        }
        
        public Note Create(Note note)
        {
            var request = new RestRequest("notes.xml", Method.POST) { XmlSerializer = new XmlIgnoreSerializer() };
            request.AddBody(note);

            var response = Client.Execute<Note>(request);
            return response.Data;
        }
        
        public bool Update(Note note)
        {
            var request = new RestRequest("notes/{id}.xml", Method.PUT) { XmlSerializer = new XmlIgnoreSerializer() };
            request.AddParameter("id", note.Id, ParameterType.UrlSegment);
            request.AddBody(note);

            var response = Client.Execute<Note>(request);
            return response.StatusCode == HttpStatusCode.OK;
        }

        public bool Delete(int id)
        {
            var response = Client.Execute<Note>(new RestRequest(String.Format("notes/{0}.xml", id), Method.DELETE));
            return response.StatusCode == HttpStatusCode.OK;
        }
    }
}
