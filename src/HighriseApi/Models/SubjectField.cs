using RestSharp.Serializers;

namespace HighriseApi.Models
{
    [SerializeAs(Name = "subject-field")]
    public class SubjectField : BaseModel
    {
        [SerializeAs(Name = "label")]
        public string Label { get; set; }        
    }
}
