namespace ConsoleApplication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    public class Program
    {
        public static void Main(string[] args)
        {
            const string query = "#ndcoslo -filter:retweets";
            Task.Run(async () => {
                var authentication = await Authenticate(args[0], args[1]);
                using(var client = new HttpClient()){
                    client.BaseAddress = new Uri("https://api.twitter.com");
                    client.DefaultRequestHeaders.Authorization = authentication;
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var responseJson = await client.GetStringAsync($"/1.1/search/tweets.json?q={WebUtility.UrlEncode(query)}&count=100&result_type=recent&include_entities=false");

                    var response = JsonConvert.DeserializeObject<TwitterResponse>(responseJson);
                    var texts = response.statuses.Select(status => status.text);

                    var markov = new TextMarkovChain();
                    foreach (var text in texts)
                    {
                        markov.feed(text);

                        Console.WriteLine(text);
                        Console.WriteLine("--");
                    }

                    Console.WriteLine("");
                    Console.WriteLine("==========");
                    Console.WriteLine("");

                    var tweetText = markov.generateSentence();
                    if(tweetText.Length > 140){
                        tweetText = tweetText.Substring(0, tweetText.LastIndexOf(' ', 140));
                    }
                    Console.WriteLine(tweetText);

                    var formData = new List<KeyValuePair<string, string>>();
                    var tweetResponse = await client.PostAsync($"/1.1/statuses/update.json?status={WebUtility.UrlEncode(tweetText)}", new FormUrlEncodedContent(formData));
                    tweetResponse.EnsureSuccessStatusCode();
                }
            }).Wait();
        }

        private static async Task<AuthenticationHeaderValue> Authenticate(string key, string secret){
            var consumerKey = WebUtility.UrlEncode(key);
            var consumerSecret = WebUtility.UrlEncode(secret);
            var token = Base64Encode($"{consumerKey}:{consumerSecret}");
            Console.WriteLine($"Hello World! {token}");
            using(var client = new HttpClient()){
                client.BaseAddress = new Uri("https://api.twitter.com");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var formData = new List<KeyValuePair<string, string>>();
                formData.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));

                var response = await client.PostAsync("/oauth2/token", new FormUrlEncodedContent(formData));

                response.EnsureSuccessStatusCode();

                var bearerTokenJson = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response: {bearerTokenJson}");

                var bearerToken = JsonConvert.DeserializeObject<TokenResponse>(bearerTokenJson);
                Console.WriteLine($"Response: {bearerToken.access_token}");

                return new AuthenticationHeaderValue("Bearer", bearerToken.access_token);
            }
        }

        public static string Base64Encode(string plainText) {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
