using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using Tweetinvi;

var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
var connectionString = config["ConnectionString"];

var tableClient = new TableClient(
    connectionString,
    "posts");

var posts = tableClient.Query<PostEntity>(filter: $"Shared eq false").OrderBy(p => Guid.NewGuid()).Take(1);

// If they're all set to 'Shared', reset the flag on all the posts
if (!posts.Any())
{
    var allPosts = tableClient.Query<PostEntity>();
    foreach (var singlePost in allPosts)
    {
        singlePost.Shared = false;
        tableClient.UpdateEntity(singlePost, ETag.All, TableUpdateMode.Replace);
    }
}

var post = posts.FirstOrDefault();

Console.WriteLine($"Good choice! Picking {post.Title}");

var status =
    $"From the archives ({post.PublishDate}): \"{post.Title}.\" RTs and feedback are always appreciated! {post.Url} #dotnet #csharp #azure #aspnetcore #dotnetcore";

var consumerKey = config["ConsumerKey"];
var consumerSecret = config["ConsumerSecret"];
var accessToken = config["AccessToken"];
var accessTokenSecret = config["AccessSecret"];

try
{
    var client = new TwitterClient(consumerKey, consumerSecret, accessToken, accessTokenSecret);
    var result = await client.Tweets.PublishTweetAsync(status);

    // When this succeeds, set 'Shared' flag in table to true
    post.Shared = true;
    tableClient.UpdateEntity(post, ETag.All, TableUpdateMode.Replace);

    Console.WriteLine($"Successfully sent tweet at: {result.Url}");
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message, ex);
}

class PostEntity : ITableEntity
{
    public string Title { get; set; }
    public string PublishDate { get; set; }
    public string Url { get; set; }
    public bool Shared { get; set; }
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
