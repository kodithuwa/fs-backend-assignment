using Amazon.DynamoDBv2;
using Amazon.Runtime;
using FileStorage.Services.Contracts;
using sf_backend_ass_test.services;

var builder = WebApplication.CreateBuilder(args);

// Load AWS config from appsettings
var awsOptions = builder.Configuration.GetSection("AWS");
var serviceUrl = awsOptions["ServiceURL"];
var region = awsOptions["Region"];
var accessKey = awsOptions["AccessKey"];
var secretKey = awsOptions["SecretKey"];

builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
{
    var config = new AmazonDynamoDBConfig
    {
        RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region),
        ServiceURL = serviceUrl
    };

    var credentials = new BasicAWSCredentials(accessKey, secretKey);
    return new AmazonDynamoDBClient(credentials, config);
});

// Add services to the container.
builder.Services.AddTransient<IAwsS3Service, AwsS3Service>();
builder.Services.AddAWSService<IAmazonDynamoDB>();

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();