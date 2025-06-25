using Amazon.DynamoDBv2;
using Amazon.Runtime;
using FileStorage.Models;
using FileStorage.Services.Contracts;
using Microsoft.AspNetCore.Http.Features;
using sf_backend_ass_test.services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<AWSModels>(builder.Configuration.GetSection("AWS"));
builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
{
    var awsConfig = builder.Configuration.GetSection("AWS").Get<AWSModels>();
    var config = new AmazonDynamoDBConfig
    {
        RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsConfig.Region),
        ServiceURL = awsConfig.ServiceURL,
    };

    var credentials = new BasicAWSCredentials(awsConfig.AccessKey, awsConfig.SecretKey);
    return new AmazonDynamoDBClient(credentials, config);
});

// Add services to the container.
builder.Services.AddScoped<IS3BucketUtil, S3BucketUtil>();
builder.Services.AddScoped<IAwsS3Service, AwsS3Service>();
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddScoped<IDynamoTableService, DynamoTableService>();

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024; // 2GB
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();