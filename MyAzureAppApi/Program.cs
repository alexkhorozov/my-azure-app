using Azure;
using Azure.Storage.Blobs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapPost("/upload", async (IFormFile[] files) =>
{
    var blobContainerConnectionString = "DefaultEndpointsProtocol=https;AccountName=stcloudshare;AccountKey=UfFCKdntQ9wirZE7wl1KkBGiLm/qJCdsVzC18yXDNH/g/tvLNkBj5TBba8ZA5nZuyMPXUsleu05a+AStcWwh8w==;EndpointSuffix=core.windows.net";
    // Create a BlobServiceClient object which will be used to create a container client
    BlobServiceClient blobServiceClient = new BlobServiceClient(blobContainerConnectionString);

    string containerName = "your-container-name";
    // Create the container and return a container client object
    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

    // Create the container if it doesn't exist
    await containerClient.CreateIfNotExistsAsync();

    foreach (var file in files)
    {
        // Limit the number of files
        if (files.Length > 10)
        {
            Console.WriteLine("Too many files. You can upload up to 10 files at once.");
            return;
        }

        // Validate the file
        if (file.Length > 2 * 1024 * 1024) // 2 MB
        {
            Console.WriteLine("File size exceeded the limit.");
            continue;
        }

        if (!file.ContentType.StartsWith("image/"))
        {
            Console.WriteLine("Invalid file type.");
            continue;
        }

        try
        {
            // Get a reference to a blob
            BlobClient blobClient = containerClient.GetBlobClient(file.FileName);

            // Open the file and upload its data
            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, true);
        }
        catch (RequestFailedException ex)
        {
            // Handle exceptions related to Azure Storage
            Console.WriteLine($"An error occurred when uploading to Azure Storage: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Handle any other exceptions
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
})
.WithName("UploadFiles")
.WithOpenApi();



app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
