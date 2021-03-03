using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Azure.Data.Tables;
using Tweetinvi;

namespace TweetOldPosts
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            var connectionString = config["ConnectionString"];

            var tableClient = new TableClient(
                connectionString,
                "posts");

            var posts = tableClient.Query<TableEntity>(filter: $"Shared eq false").OrderBy(p => Guid.NewGuid()).Take(1);

            var model = new PostModel();
            foreach (var post in posts)
            {
                model.Title = post.GetString("Title");
                model.Url = post.GetString("Url");
                model.PublishDate = post.GetString("PublishDate");
            }

            Console.WriteLine($"Good choice! Picking {model.Title}");

            var status =
                $"From the archives ({model.PublishDate}): \"{model.Title}.\" RTs and feedback are always appreciated! {model.Url} #dotnet #csharp #dotnetcore";

            var consumerKey = config["ConsumerKey"];
            var consumerSecret = config["ConsumerSecret"];
            var accessToken = config["AccessToken"];
            var accessTokenSecret = config["AccessSecret"];

            try
            {
                var client = new TwitterClient(consumerKey, consumerSecret, accessToken, accessTokenSecret);
                var result = await client.Tweets.PublishTweetAsync(status);
                Console.WriteLine($"Successfully sent tweet at: {result.Url}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, ex);
            }
        }
    }
}