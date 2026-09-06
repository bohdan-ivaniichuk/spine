var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/", () => "InterviewPrep API (skeleton)");
app.Run();

public partial class Program;
