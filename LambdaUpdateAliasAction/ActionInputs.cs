namespace LambdaUpdateAliasAction;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ActionInputs
{
	[Option("aws_access_key_id", Required = true)]
	public string AwsAccessKeyId { get; set; } = default!;

	[Option("aws_secret_access_key", Required = true)]
	public string AwsSecretAccessKey { get; set; } = default!;

	[Option("aws_region", Required = true)]
	public string AwsRegion { get; set; } = default!;

	[Option("function_name", Required = true)]
	public string FunctionName { get; set; } = default!;

	[Option("alias_name", Required = true)]
	public string AliasName { get; set; } = default!;

	[Option("function_version", Required = false)]
	public string? FunctionVersion { get; set; }

	[Option("wait_until_concurrency_updated", Required = false)]
	public bool WaitUntilConcurrencyUpdated { get; set; }

	[Option("max_wait_until_concurrency_updated", Required = false)]
	public int MaxWaitUntilConcurrencyUpdated { get; set; }
}