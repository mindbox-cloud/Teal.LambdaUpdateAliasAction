using CommandLine;
using LambdaUpdateAliasAction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static CommandLine.Parser;

using var host = Host.CreateDefaultBuilder(args)
    .Build();
    
static TService Get<TService>(IHost host)
    where TService : notnull =>
    host.Services.GetRequiredService<TService>();

var parser = Default.ParseArguments(() => new ActionInputs(), args);
parser.WithNotParsed(
    errors =>
    {
        Get<ILoggerFactory>(host)
            .CreateLogger(nameof(Program))
            .LogError("Errors occurred while parsing inputs: {Errors}", 
            string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        
        Environment.Exit(2);
    });
    
await parser.WithParsedAsync(inputs => UpdateAliasAsync(inputs, host));
await host.RunAsync();

static Task UpdateAliasAsync(ActionInputs inputs, IHost host)
{
    var logger = Get<ILoggerFactory>(host).CreateLogger(nameof(UpdateAliasAsync));
    
    logger.LogInformation("Hello world!");

    Environment.Exit(0);
    
    return Task.CompletedTask;
}