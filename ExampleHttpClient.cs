using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PoC.HttpRequest.Throttling
{
    public class ExampleHttpClient
    {
        private readonly HttpClient _httpClient;

        public ExampleHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> TestAsync()
        {
            return await _httpClient.GetStringAsync("http://dummy.restapiexample.com/api/v1/employees");
        }
    }
}
