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

        static void Main(string[] args)
        {

            // Create a client.
            ITextAnalyticsClient client = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
            {
                Endpoint="https://southeastasia.api.cognitive.microsoft.com"
            };

            /*Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Extracting language.
            Console.WriteLine("===== LANGUAGE EXTRACTION ======");

            var result = client.DetectLanguageAsync(new BatchInput(
                    new List<Input>()
                        {
                          new Input("1", "This is a document written in English."),
                          new Input("2", "Este es un document escrito en Español."),
                          new Input("3", "这是一个用中文写的文件")
                    })).Result;

            // Printing language results.
            foreach (var document in result.Documents)
            {
                Console.WriteLine("Document ID: {0} , Language: {1}", document.Id, document.DetectedLanguages[0].Name);
            }

            // Getting key phrases.
            Console.WriteLine("\n\n===== KEY-PHRASE EXTRACTION ======");

            KeyPhraseBatchResult result2 = client.KeyPhrasesAsync(new MultiLanguageBatchInput(
                        new List<MultiLanguageInput>()
                        {
                          new MultiLanguageInput("ja", "1", "猫は幸せ"),
                          new MultiLanguageInput("de", "2", "Fahrt nach Stuttgart und dann zum Hotel zu Fu."),
                          new MultiLanguageInput("en", "3", "My cat is stiff as a rock."),
                          new MultiLanguageInput("es", "4", "A mi me encanta el fútbol!")
                        })).Result;

            // Printing key phrases.
            foreach (var document in result2.Documents)
            {
                Console.WriteLine("Document ID: {0} ", document.Id);

                Console.WriteLine("\t Key phrases:");

                foreach (string keyphrase in document.KeyPhrases)
                {
                    Console.WriteLine("\t\t" + keyphrase);
                }
            }
            */
            string bodyContent = File.ReadAllText("EmailTemplate.txt");
            string news = "";
            string result = "";
            string currPair = "EURUSD";
            // Extracting sentiment.
            Console.WriteLine("\n\n===== SENTIMENT ANALYSIS ======");
            List<MultiLanguageInput> input = GetNews(ref news, currPair);
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
                result += "<br><br>Recommended Buy";
                Console.WriteLine("Recommended Buy");
            }
            else if(totalPredictedScore <= 0.45)
            {
                result += "<br><br>Recommended Sell";
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
            SendEmail("jufren@gmail.com", currPair + " Recommendation for "+ DateTime.Now.ToString(), bodyContent);
            Console.ReadLine();
        }
        public static List<MultiLanguageInput> GetNews(ref string news,string q)
        {
            // init with your API key
            var newsApiClient = new NewsApiClient("288e5576cf624fec9fed057fc290fb29");

            var articlesResponse = newsApiClient.GetEverything(new EverythingRequest
            {
                Q =q,
                SortBy = SortBys.Popularity,
                Language = Languages.EN,
                From = DateTime.Today
            });
            List<MultiLanguageInput> result = new List<MultiLanguageInput>();
            news = "<table><tr><td>ID</td><td>News Title</td><td>News URL</td></tr>";
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
                    result.Add(new MultiLanguageInput("en",i.ToString(), article.Title));
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
            string st = "Test";
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