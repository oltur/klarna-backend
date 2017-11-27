using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Backend.Controllers
{
    [EnableCors("AllowAll")]
    [Route("api/[controller]")]
    public class SearchController : Controller
    {
        private readonly ReadOnlyDictionary<string, string> HEADERS;

        private readonly Uri BASE_URL = new Uri("http://eksercise-api.herokuapp.com");

        public SearchController()
        {
            var t = new Dictionary<string, string>
            {
                {"X-KLARNA-TOKEN", Environment.GetEnvironmentVariable("TOKEN")}

            };
            HEADERS = new ReadOnlyDictionary<string, string>(t);
        }


        private Uri GetPeopleSearchUri(string name = null, int? age = null, string phone = null, int page = 1)
        {
            if (name == null && !age.HasValue && phone == null)
                throw new ArgumentException("Non-empty query is required");

            var relativePath = string.Format("/people/search?{0}{1}{2}{3}",
                GetParameterString("name", name),
                GetParameterString("age", age.HasValue ? (object)age.Value : null),
                GetParameterString("phone", phone),
                GetParameterString("page", page)
                );
            var uri = new Uri(BASE_URL, relativePath);
            return uri;
        }

        private Uri GetPeopleSearchResultUri(string searchRequestId)
        {

            var relativePath = string.Format("people?{0}",
                GetParameterString("searchRequestId", searchRequestId)
            );
            var uri = new Uri(BASE_URL, relativePath);
            return uri;
        }

        private string GetParameterString(string name, object value)
        {
            return value == null ? "" : string.Format("{0}={1}&", name, value);
        }

        private bool Parse(string query, out string name, out int? age, out string phone)
        {
            name = null;
            age = null;
            phone = null;

            name = query;

            return true;
        }

        // GET api/search/[query=query][&page=page]
        [HttpGet]
        public async Task<object> Get(string query, int page = 1)
        {
            string name;
            int? age;
            string phone;

            if (Parse(query, out name, out age, out phone))
            {
                var peopleSearchUri = GetPeopleSearchUri(name, age, phone, page);

                dynamic content = new ExpandoObject();
                content.name = name;
                CancellationTokenSource cts1 = new CancellationTokenSource();
                var result = await CreateClientRequest().PostAsync(peopleSearchUri, new StringContent(JsonConvert.SerializeObject(content)), cts1.Token);
                string requestId;
                try
                {
                    dynamic data = JsonConvert.DeserializeObject(await result.Content.ReadAsStringAsync());
                    requestId = data.id;
                }
                catch (RuntimeBinderException)
                {
                    requestId = null;
                }

                Debug.WriteLine("Request id: {0}", requestId);

                if (requestId != null)
                {

                    var peopleSearchResultUri = GetPeopleSearchResultUri(requestId);

                    CancellationTokenSource cts2 = new CancellationTokenSource();
                    HttpResponseMessage result2 = null;
                    while (result2 == null)
                    {
                        try
                        {
                            result2 = await CreateClientRequest().GetAsync(peopleSearchResultUri, cts2.Token);
                        }
                        catch (HttpRequestException ex)
                        {
                            //if(ex.)
                        }
                    }
                    try
                    {
                        var strData = await result2.Content.ReadAsStringAsync();
                        dynamic data = JsonConvert.DeserializeObject(strData);
                        return new Dictionary<string, object> { { "id", requestId }, { "data", data } };
                    }
                    catch (RuntimeBinderException)
                    {
                    }

                }

            }


            return null;
        }

        private HttpClient CreateClientRequest()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            foreach (var key in HEADERS.Keys)
            {
                client.DefaultRequestHeaders.Add(key, HEADERS[key]);
            }
            return client;
        }

        //// GET api/values/5
        //[HttpGet]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        //// POST api/values
        //[HttpPost]
        //public void Post([FromBody]string value)
        //{
        //}

        //// PUT api/values/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody]string value)
        //{
        //}

        //// DELETE api/values/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
