var parser = Default.ParseArguments<ActionInputs>(args);
parser.WithNotParsed(
	errors =>
	{
		Console.Error.WriteLine("Errors occurred while parsing inputs: {0}",
			string.Join(Environment.NewLine, errors.Select(error => error.ToString())));

		Environment.Exit(1);
	});

await parser.WithParsedAsync(RunAction);

static async Task RunAction(ActionInputs inputs)
{
	var lambda = CreateLambdaClient(inputs);
	inputs.FunctionVersion = await FindLatestVersionAsync(lambda, inputs);
	await UpdateOrCreateAliasAsync(lambda, inputs);

	if (inputs.WaitUntilConcurrencyUpdated)
	{
		await WaitUntilConcurrencyUpdatedAsync(lambda, inputs);
	}

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
	var paginator = lambda.Paginators.ListVersionsByFunction(new ListVersionsByFunctionRequest
	{
		FunctionName = inputs.FunctionName
	});

	var versions = new List<FunctionConfiguration>();

	try
	{
		await foreach (var version in paginator.Versions)
		{
			versions.Add(version);
		}
	}
	catch (ResourceNotFoundException)
	{
		Console.Error.WriteLine("Function with name: {0} does not exist", inputs.FunctionVersion);
		Environment.Exit(3);
	}

	if (versions.Count == 0)
	{
		Console.Error.WriteLine("No versions found");
		Environment.Exit(2);
	}

	if (!string.IsNullOrEmpty(inputs.FunctionVersion))
	{
		if (versions.Any(v => v.Version == inputs.FunctionVersion))
		{
			return inputs.FunctionVersion;
		}

		Console.Error.WriteLine("Version `{0}` does not exist", inputs.FunctionVersion);
		Environment.Exit(3);
	}

	versions.Sort((a, b) =>
	{
		static int GetVersion(FunctionConfiguration fc) =>
			fc.Version == "$LATEST" ? 0 : int.Parse(fc.Version);

		var aVersion = GetVersion(a);
		var bVersion = GetVersion(b);
		return bVersion - aVersion;
	});

	return versions[0].Version;
}

static async Task UpdateOrCreateAliasAsync(IAmazonLambda lambda, ActionInputs inputs)
{
	try
	{
		await lambda.UpdateAliasAsync(new UpdateAliasRequest
		{
			FunctionName = inputs.FunctionName,
			Name = inputs.AliasName,
			FunctionVersion = inputs.FunctionVersion
		});
	}
	catch (ResourceNotFoundException)
	{
		await lambda.CreateAliasAsync(new CreateAliasRequest
		{
			FunctionName = inputs.FunctionName,
			Name = inputs.AliasName,
			FunctionVersion = inputs.FunctionVersion
		});
	}
}

static async Task WaitUntilConcurrencyUpdatedAsync(IAmazonLambda lambda, ActionInputs inputs)
{
	Console.WriteLine("Wait until concurrency updated");

	using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

	var request = new GetAliasRequest
	{
		FunctionName = inputs.FunctionName,
		Name = inputs.AliasName
	};

	var delay = TimeSpan.FromSeconds(inputs.MaxWaitUntilConcurrencyUpdated);
	using var cts = new CancellationTokenSource(delay);

	while (!cts.IsCancellationRequested)
	{
		var response = await lambda.GetAliasAsync(request, cts.Token);

		if (!response.RoutingConfig.AdditionalVersionWeights.Any())
		{
			return;
		}

		await timer.WaitForNextTickAsync(cts.Token);
	}

	Console.Error.WriteLine("Timeout {0} was exceeded. To increase the time use the option 'max_wait_until_concurrency_updated'",
		inputs.MaxWaitUntilConcurrencyUpdated);

	Environment.Exit(4);
}