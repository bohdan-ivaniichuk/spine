namespace InterviewPrep.Api.Cli;

/// <summary>`import-content` verb. Filled in by the import feature; the stub keeps the host branch compiling.</summary>
public static class ImportContentCommand
{
    public static Task<int> RunAsync(string[] args)
    {
        Console.Error.WriteLine("import-content is not implemented yet");
        return Task.FromResult(3);
    }
}
