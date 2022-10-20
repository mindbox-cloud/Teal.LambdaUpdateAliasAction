var parser = Default.ParseArguments<ActionInputs>(args);
parser.WithNotParsed(
    errors =>
    {
        Console.Error.WriteLine("Errors occurred while parsing inputs: {0}", 
            string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        
        Environment.Exit(2);
    });

await parser.WithParsedAsync(UpdateAliasAsync);

static async Task UpdateAliasAsync(ActionInputs inputs)
{
    var lambda = CreateLambdaClient(inputs);
    var latestVersion = await FindLatestVersionAsync(lambda, inputs);
    
    Console.WriteLine($"::set-output name=latest-version::{latestVersion}");

    Environment.Exit(0);
}

static IAmazonLambda CreateLambdaClient(ActionInputs inputs)
{
    var credentials = new BasicAWSCredentials(inputs.AwsAccessKeyId, inputs.AwsSecretAccessKey);
    var region = RegionEndpoint.GetBySystemName(inputs.AwsRegion);
    
    return new AmazonLambdaClient(credentials, region);
}

static async Task<string> FindLatestVersionAsync(IAmazonLambda lambda, ActionInputs inputs)
{
    var data = await lambda.ListVersionsByFunctionAsync(new ListVersionsByFunctionRequest
    {
        FunctionName = inputs.FunctionName
    });

    if (data.Versions.Count == 0)
    {
        Console.Error.WriteLine("No versions found");
        Environment.Exit(3);
    }

    if (inputs.FunctionVersion is not null && data.Versions.Any(v => v.Version == inputs.FunctionVersion))
    {
        return inputs.FunctionVersion;
    }

    data.Versions.Sort((a, b) =>
    {
        int GetVersion(FunctionConfiguration fc) => fc.Version == "$LATEST" ? 0 : int.Parse(fc.Version);
        var aVersion = GetVersion(a);
        var bVersion = GetVersion(b);
        return bVersion - aVersion;
    });

    return data.Versions[0].Version;
}