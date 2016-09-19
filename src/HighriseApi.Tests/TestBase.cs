namespace HighriseApi.Tests
{
    public class TestBase
    {
        public TestBase()
        {
            _highriseApiRequest = new ApiRequest("company", "key");
        }

        private readonly ApiRequest _highriseApiRequest;
        public ApiRequest HighriseApiRequest { get { return _highriseApiRequest; } }
    }
}
