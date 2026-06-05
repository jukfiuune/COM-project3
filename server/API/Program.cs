using API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCleanMapApi(builder.Configuration);

var app = builder.Build();

app.UseCleanMapApi();

app.Run();
