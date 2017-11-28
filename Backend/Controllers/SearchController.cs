using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.Caching.Memory;
using Models;
using Newtonsoft.Json;

namespace Backend.Controllers
{
    [EnableCors("AllowAll")]
    [Route("api/[controller]")]
    public class SearchController : Controller
    {
        private readonly ReadOnlyDictionary<string, string> HEADERS;

        private readonly Uri BASE_URL = new Uri("http://eksercise-api.herokuapp.com");

        private readonly IMemoryCache _cache;

        public SearchController(IMemoryCache cache)
        {
            _cache = cache;
            var t = new Dictionary<string, string>
            {
                {"X-KLARNA-TOKEN", Environment.GetEnvironmentVariable("TOKEN")}

            };
            HEADERS = new ReadOnlyDictionary<string, string>(t);
        }

        // GET api/search/query=<query>[&page=page]
        [HttpGet]
        public async Task<IActionResult> Get(string query, int page = 1)
        {
            var key = Tuple.Create(query, page);
            var keyPrev = Tuple.Create(query, page - 1);
            var keyNext = Tuple.Create(query, page + 1);

            Tuple<int, SearchResult> result;

            var tasks = new List<Task>();
            Task<Tuple<int, SearchResult>> taskMain = GetResult(key);
            Task<Tuple<int, SearchResult>> taskPrev = null;
            Task<Tuple<int, SearchResult>> taskNext = GetResult(keyNext);
            tasks.Add(taskMain);
            if (page > 1)
            {
                taskPrev = GetResult(key);
                tasks.Add(taskPrev);
            }
            tasks.Add(taskNext);
            await Task.WhenAll(tasks.ToArray());

            result = taskMain.Result;
            if (result.Item2.data != null && result.Item2.data.Any())
            {
                result.Item2.hasPrev = (page > 1) && taskPrev.Result.Item2.data != null && taskPrev.Result.Item2.data.Any();
                result.Item2.hasNext = taskNext.Result.Item2.data != null && taskNext.Result.Item2.data.Any();
            }

            return StatusCode(result.Item1, result.Item2);
        }

        public async Task<Tuple<int, SearchResult>> GetResult(Tuple<string, int> key)
        {
            Tuple<int, SearchResult> result;
            if (_cache.TryGetValue(key, out result))
            {
                return result;
            }

            var log = new StringBuilder();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            ParsedRequest parsedRequest;
            string error;
            if (ParsedRequest.Parse(key.Item1, key.Item2, out parsedRequest, out error))
            {
                var jsonParsedRequest = JsonConvert.SerializeObject(parsedRequest);
                log.AppendFormat("Parsed request: {0}\n", jsonParsedRequest);
                var peopleSearchUri = GetPeopleSearchUri(parsedRequest);

                CancellationTokenSource cts1 = new CancellationTokenSource();
                var peopleSearchResult = await CreateClientRequest()
                    .PostAsync(peopleSearchUri, new StringContent(jsonParsedRequest), cts1.Token);
                string requestId;
                try
                {
                    dynamic data =
                        JsonConvert.DeserializeObject(await peopleSearchResult.Content.ReadAsStringAsync());
                    requestId = data.id;
                }
                catch (RuntimeBinderException)
                {
                    requestId = null;
                }

                log.AppendFormat("Request id: {0}\n", requestId);

                if (requestId != null)
                {
                    var peopleSearchResultUri = GetPeopleSearchResultUri(requestId);

                    CancellationTokenSource cts2 = new CancellationTokenSource();
                    HttpResponseMessage result2 = null;
                    var retries = 0;
                    while (result2 == null)
                    {
                        try
                        {
                            result2 = await CreateClientRequest().GetAsync(peopleSearchResultUri, cts2.Token);
                        }
                        catch (HttpRequestException ex)
                        {
                            if (retries++ > 20)
                                throw;

                            log.AppendFormat("Search result not ready yet ({0}), retrying...\n", ex.Message);
                            Thread.Sleep(500);
                        }
                    }

                    var strData = await result2.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<Person[]>(strData);
                    sw.Stop();
                    var resultContent = new SearchResult
                    {
                        data = data,
                        page = key.Item2,
                        log = log.ToString(),
                        requestId = requestId,
                        timestamp = DateTime.UtcNow,
                        duration = sw.ElapsedMilliseconds
                    };

                    result = Tuple.Create(200, resultContent);
                }
                else
                {
                    log.AppendFormat("Cannot get requestId\n");
                    sw.Stop();
                    result = Tuple.Create(500, new SearchResult
                    {
                        data = null,
                        log = log.ToString(),
                        requestId = null,
                        timestamp = DateTime.UtcNow,
                        duration = sw.ElapsedMilliseconds

                    });
                }
            }
            else
            {
                log.AppendFormat("Cannot parse query. Error: {0}\n", error);
                sw.Stop();

                result = Tuple.Create(400, new SearchResult
                {
                    data = null,
                    log = log.ToString(),
                    requestId = null,
                    timestamp = DateTime.UtcNow,
                    duration = sw.ElapsedMilliseconds
                });
            }

            var options = new MemoryCacheEntryOptions()
                // Keep in cache for this time, reset time if accessed.
                .SetSlidingExpiration(TimeSpan.FromHours(1));

            _cache.Set(key, result, options);
            return result;
        }

        private Uri GetPeopleSearchUri(ParsedRequest parsedRequest)
        {
            var relativePath = string.Format("/people/search?{0}{1}{2}{3}",
                GetParameterString("name", parsedRequest.name),
                GetParameterString("age", parsedRequest.age.HasValue ? (object)parsedRequest.age.Value : null),
                GetParameterString("phone", parsedRequest.phone),
                GetParameterString("page", parsedRequest.page)
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