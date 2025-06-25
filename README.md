# Pre-Requisites:
    • Docker Desktop
    • .NET SDK 8.0+
    • Make
    • Postman

# Senario: Upload File in slow internet connection.
    As a user, I want to upload a file to S3 for long-term storage. The file can be any size from 128KB to 2GB. Unfortunately, I have a slow internet connection, and sometimes there might be network interruptions, but we should still be able to upload files efficiently. Along with uploading file, I want to calculate its SHA-256 value and store it in DynamoDB for further analysis, but kindly asking you to consider two important points:
    1. Memory used. We have small servers, and cannot have more than 2GB of memory on a server.
    2. By security reasons we cannot store the whole file in-memory or on a disk, let's say even minimum size files should not be stored in-memory or persisted on a disk, even for a fraction of a second.



# Directions to run the project:
1. Clone the repository.
2. Makesure Docker Desktop is running in the background.
3. Run the make  up  command to compose localstack into docker containers.
4. Run the project (sf-backend-assignment\FileStorage) in Visual Studio or using the command line with `dotnet run`.
4. Send a Request to Upload endpoint though the Postman.
5. Verity the uploaded file details in DynamoDb Admin.

Cheers!