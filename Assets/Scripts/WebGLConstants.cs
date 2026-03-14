/// <summary>
/// Centralized configuration for cloud service endpoints.
/// </summary>
public static class WebGLConstants
{
    public const string S3_BUCKET = "https://test-experiment-data-storage.s3.eu-north-1.amazonaws.com";
    public const string CSV_UPLOAD_LAMBDA_ENDPOINT = "https://mjlo2ftcn3.execute-api.eu-north-1.amazonaws.com/Prod/telemetry";

    public const string GET_USER_LIFECYCLE_STATE_LAMBDA_ENDPOINT = "https://ijxyyr92x3.execute-api.eu-north-1.amazonaws.com/test/telemetry";

    public const string TUTORIAL_CONFIG_FILENAME = "tutorial_config.yaml";
    public const string EXPERIMENT_CONFIG_FILENAME = "config.yaml";
}
