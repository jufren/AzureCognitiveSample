using System;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using System.Collections.Generic;
using Microsoft.Rest;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NewsAPI;
using NewsAPI.Models;
using NewsAPI.Constants;
using System.Net.Mail;
using System.Net;
using System.IO;
using YahooFinanceApi;
using BingSearchHelper;
using System.Globalization;

namespace ConsoleApp1
{
    class Program2
    {
        /// <summary>
        /// Container for subscription credentials. Make sure to enter your valid key.
        /// </summary>
        class ApiKeyServiceClientCredentials : ServiceClientCredentials
        {
            public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Add("Ocp-Apim-Subscription-Key", "94650262681e448cb771cc79c4a1f375");
                return base.ProcessHttpRequestAsync(request, cancellationToken);
            }
        }
        static string ss,news;
        static void Main(string[] args)
        {
            
            
            // Create a client.
            ITextAnalyticsClient client = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
            {
                Endpoint="https://southeastasia.api.cognitive.microsoft.com"
            };

            
            string bodyContent = File.ReadAllText("EmailTemplate.txt");
            string result = "";
            string currPair = "ethereum price";
            
            // Extracting sentiment.
            Console.WriteLine("\n\n===== SENTIMENT ANALYSIS ======");
            List<MultiLanguageInput> input=null;
            Task.Run(async () =>
            {
                input = await GetNewsAsync("\"" + currPair + "\"");
                // Do any async anything you need here without worry
            }).GetAwaiter().GetResult();
            SentimentBatchResult result3 = client.SentimentAsync(
                    new MultiLanguageBatchInput(
                        input)).Result;

            double? totalScore = 0;
            int i = 0;
            // Printing sentiment results.
            foreach (var document in result3.Documents)
            {
                
                Console.WriteLine("Document ID: {0} , Sentiment Score: {1:0.00}", document.Id, document.Score);
                totalScore += document.Score;
                i++;
            }
            double? totalPredictedScore = totalScore / i;
            Console.WriteLine("Predicted Score: {0}", totalPredictedScore);
            result+= "<br><br>Predicted Score:" + totalPredictedScore;
            if (totalPredictedScore>=0.55)
            {
                Task.Run(async () =>
                {
                    await GetEntryPriceFromYahooAsync("ETHUSD=X");
                    // Do any async anything you need here without worry
                }).GetAwaiter().GetResult();
                result += "<br><br>Recommended Buy";
                result += "<br><br>Entry Price:" + ss;
                WriteEntryPrice("Buy", ss);
                Console.WriteLine("Recommended Buy");
            }
            else if(totalPredictedScore <= 0.45)
            {
                Task.Run(async () =>
                {
                    await GetEntryPriceFromYahooAsync("ETHUSD=X");
                    // Do any async anything you need here without worry
                }).GetAwaiter().GetResult();
                result += "<br><br>Recommended Sell";

                result += "<br><br>Entry Price:" + ss;
                WriteEntryPrice("Sell", ss);

                Console.WriteLine("Recommended Sell");
            }
            else
            {
                result += "<br><br>No Recommendation";
                Console.WriteLine("No Recommendation");
            }
            bodyContent = bodyContent.Replace("{0}", currPair);
            bodyContent = bodyContent.Replace("{1}", DateTime.Now.ToString());
            bodyContent = bodyContent.Replace("{2}", news);
            bodyContent = bodyContent.Replace("{3}", result);
            //SendEmail("jufren@gmail.com", currPair + " Recommendation for "+ DateTime.Now.ToString(), bodyContent);
            Console.ReadLine();
        }
        public static async Task<List<MultiLanguageInput>> GetNewsAsync( string q)
        {
            BingSearchHelper.BingSearchHelper.SearchApiKey = "4c530c331b8a4b8099123cd1616afd70";
            var newsResult = await BingSearchHelper.BingSearchHelper.GetNewsSearchResults(q, 10);
            List<MultiLanguageInput> result = new List<MultiLanguageInput>();

            
            if (newsResult!=null)
            {
                // total results found
                //Console.WriteLine(response.webPages.totalEstimatedMatches);
                int i = 0;
                news = "<table><tr><td>ID</td><td>News Title</td><td>News URL</td></tr>";
                foreach (var item in newsResult)
                {
                    
                    //System.Net.WebClient wc = new System.Net.WebClient();
                    //string webData = wc.DownloadString(webPage.url);
                   
                    DateTime nowMin4Hour = DateTime.Now.AddHours(-12);
                    DateTime publishedDate = DateTime.ParseExact(item.DatePublished, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                    if (publishedDate.CompareTo(nowMin4Hour)>0)
                    {
                        
                        i++;
                        result.Add(new MultiLanguageInput("en", i.ToString(), item.Description));
                        news += "<tr>";
                        news += "<td>" + (i + 1) + "</td>";
                        news += "<td>" + item.Title + "</td>";
                        news += "<td>" + item.Description + "</td>";
                        news += "</tr>";
                    }
                    
                }
                news += "</table>";
               
            }
            return result;
        }
        public static async Task<string> GetEntryPriceFromYahooAsync(string curr)
        {
            // You could query multiple symbols with multiple fields through the following steps:
            var securities = await Yahoo.Symbols(curr).Fields(Field.Currency, Field.RegularMarketPrice).QueryAsync();
            var aapl = securities[curr.ToUpper()].RegularMarketPrice;
            //var price = aapl.Values[Field.RegularMarketPrice];
            ss = aapl.ToString();
            return ss;
        }
        public static void WriteEntryPrice(string type, string price)
        {
            File.WriteAllText("price.txt", type + "," + price);
        }
        
            public static List<MultiLanguageInput> GetNewsNewsApi(ref string news,string q)
        {
            // init with your API key
            var newsApiClient = new NewsApiClient("288e5576cf624fec9fed057fc290fb29");

            
             var articlesResponse = newsApiClient.GetEverything(new EverythingRequest
            {
                Q =q,
                
                SortBy = SortBys.Popularity,
                Language = Languages.EN,
                From = DateTime.Today.AddDays(0)
            });
            List<MultiLanguageInput> result = new List<MultiLanguageInput>();
            news = "<table><tr><td>ID</td><td>News Title</td><td>News URL</td></tr>";
            string[] strToReplace = new string[]{ "USD/JPY", "USD-JPY" };
            if (articlesResponse.Status == Statuses.Ok)
            {
                // total results found
                Console.WriteLine(articlesResponse.TotalResults);
                int i = 0;
                // here's the first 20
                foreach (var article in articlesResponse.Articles)
                {
                    // title
                    Console.WriteLine(article.Title);
                    news += "<tr>";
                    // author
                    Console.WriteLine(article.Author);
                    // description
                    Console.WriteLine(article.Description);
                    // url
                    Console.WriteLine(article.Url);
                    // image
                    Console.WriteLine(article.UrlToImage);
                    // published at
                    Console.WriteLine(article.PublishedAt);

                    string articleStr = "";
                    foreach (string str in strToReplace)
                    {
                        articleStr=CleanUpTitle(article.Description, str, q);
                    }
                    result.Add(new MultiLanguageInput("en",i.ToString(), articleStr));
                    news += "<td>" + (i+1) + "</td>";
                    news += "<td>" + article.Title + "</td>";
                    news += "<td>" + article.Url + "</td>";
                    news += "</tr>";
                    i++;
                }
                news += "</table>";
            }
            return result;
        }
        public static string CleanUpTitle(string src, string toreplace,string replacewith)
        {
            return src.Replace(toreplace, replacewith);
            
        }
        public static void SendEmail(string to,string subject,string body)
        {
            MailMessage mail = new MailMessage();
            mail.From = new System.Net.Mail.MailAddress("testazurecognitive@gmail.com");

            // The important part -- configuring the SMTP client
            SmtpClient smtp = new SmtpClient();
            smtp.Port = 587;   // [1] You can try with 465 also, I always used 587 and got success
            smtp.EnableSsl = true;
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network; // [2] Added this
            smtp.UseDefaultCredentials = false; // [3] Changed this
            smtp.Credentials = new NetworkCredential(mail.From.ToString(), "t3st@zur3");  // [4] Added this. Note, first parameter is NOT string.
            smtp.Host = "smtp.gmail.com";

            //recipient address
            mail.To.Add(new MailAddress(to));

            //Formatted mail body
            mail.IsBodyHtml = true;
            mail.Subject = subject;
            mail.Body = body;
            smtp.Send(mail);
        }
    }
    class SentimentResult
    {
        public string URL { get; set; }
        public string Title { get; set; }
        public double SentimentScore { get; set; }
    }
}