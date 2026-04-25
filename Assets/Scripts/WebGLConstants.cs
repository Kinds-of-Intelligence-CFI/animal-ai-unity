/// <summary>
/// Centralized configuration for cloud service endpoints.
/// </summary>
public static class WebGLConstants
{
    public const string S3_BUCKET = "https://animal-ai-website-experiment-data-storage.s3.eu-north-1.amazonaws.com";
    public const string CSV_UPLOAD_LAMBDA_ENDPOINT = "https://x314h4yzq5.execute-api.eu-north-1.amazonaws.com/Prod/telemetry";

    public const string GET_USER_LIFECYCLE_STATE_LAMBDA_ENDPOINT = "https://ovqx9h40zg.execute-api.eu-north-1.amazonaws.com/test/telemetry";

    public const string TUTORIAL_CONFIG_FILENAME = "tutorial_config.yaml";
    public const string EXPERIMENT_CONFIG_FILENAME = "config.yaml";
}
