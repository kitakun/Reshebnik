var builder = WebApplication.CreateSlimBuilder(args);

var app = builder.Build();


app.MapGet("/", () => "❤️");

await app.RunAsync();
