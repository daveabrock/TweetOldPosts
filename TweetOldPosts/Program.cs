using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Tweetinvi;

namespace TweetOldPosts
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            using var reader = XmlReader.Create(config["FeedUrl"]);
            var feed = SyndicationFeed.Load(reader);

            var cutOffDate = new DateTime(2017, 02, 04);

            var randoPost = feed.Items.Where(p => p.PublishDate > cutOffDate &&
                                             !p.Categories.Any(cat => cat.Name.Contains("dotnet-stacks")) &&
                                             !p.Categories.Any(cat => cat.Name.Contains("personal")) &&
                                             !p.Categories.Any(cat => cat.Name.Contains("what-i-am-reading")))
                            .OrderBy(p => Guid.NewGuid())
                            .FirstOrDefault();

            Console.WriteLine($"Good choice! Picking {randoPost.Title.Text}");

            var status =
                $"From the archives ({randoPost.PublishDate.Date.ToShortDateString()}): \"{randoPost.Title.Text}.\" RTs and feedback are always appreciated! {randoPost.Links[0].Uri} #dotnet #csharp #dotnetcore";

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