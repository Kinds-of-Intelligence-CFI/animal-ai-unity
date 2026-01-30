using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Uploads CSV telemetry files to AWS Lambda endpoint
/// Add this component to the same GameObject as CSVWriter
/// </summary>
public class CSVUploader : MonoBehaviour
{
    [Header("Lambda Endpoint Configuration")]
    [SerializeField] private string lambdaUrl = CloudEndpoints.LAMBDA_ENDPOINT;
    
    private string sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");

    /// <summary>
    /// Upload the CSV file to S3 via a lambda
    /// </summary>
    public IEnumerator UploadCSV(string experimentId, string userId)
    {
        Debug.Log("Starting CSV upload...");
        
        // Wait a frame to ensure file write completes
        yield return new WaitForEndOfFrame();
        
        // Read the CSV file content
        string csvContent = ReadCurrentCSV();
        
        if (string.IsNullOrEmpty(csvContent))
        {
            Debug.LogWarning("No CSV content to upload");
            yield break;
        }

        // Check if CSV has data rows (not just header)
        string[] lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        int dataRowCount = lines.Length - 1; // Subtract 1 for header row

        if (dataRowCount <= 0)
        {
            Debug.LogWarning($"CSV only contains header row with no data. Skipping upload. Lines found: {lines.Length}");
            Debug.LogWarning($"CSV preview: {csvContent.Substring(0, Math.Min(200, csvContent.Length))}");
            yield break;
        }

        Debug.Log($"CSV content loaded: {csvContent.Length} characters, {dataRowCount} data rows");
        
		// Create the JSON payload
		string jsonPayload = CreateJsonPayload(csvContent, experimentId, userId);

        Debug.Log($"JSON payload preview (first 500 chars): {jsonPayload.Substring(0, Math.Min(500, jsonPayload.Length))}");

        // Send the request
        using (UnityWebRequest request = new UnityWebRequest(lambdaUrl, "POST"))
        {
            // Set the request body
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            // Set headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 30;
            
            Debug.Log($"Sending request to: {lambdaUrl}");
            
            // Send the request
            yield return request.SendWebRequest();
            
            // Handle the response
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Response: {request.downloadHandler.text}");

                // Parse response to check Lambda's statusCode
                try
                {
                    LambdaResponse lambdaResponse = JsonUtility.FromJson<LambdaResponse>(request.downloadHandler.text);

                    if (lambdaResponse.statusCode == 200)
                    {
                        // Parse the nested body JSON
                        UploadResponse uploadResponse = JsonUtility.FromJson<UploadResponse>(lambdaResponse.body);
                    }
                    else
                    {
                        Debug.LogError($"Lambda returned error status {lambdaResponse.statusCode}");
                        Debug.LogError($"Lambda response body: {lambdaResponse.body}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not parse Lambda response JSON: {e.Message}");
                    Debug.LogWarning($"Raw response: {request.downloadHandler.text}");
                }
            }
            else
            {
                Debug.LogError($"HTTP request failed!");
                Debug.LogError($"Error: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                Debug.LogError($"Response Body: {request.downloadHandler.text}");
            }
        }
        
    }

    private string ReadCurrentCSV()
    {
        CSVWriter csvWriter = GetComponent<CSVWriter>();
        csvWriter.Shutdown();
        // Determine the base path (same logic as CSVWriter)
        string basePath;
        
        if (Application.isEditor)
        {
            basePath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }
        else
        {
            basePath = Path.GetDirectoryName(Application.dataPath);
        }
        
        string directoryPath = Path.Combine(basePath, "ObservationLogs");
        
        if (!Directory.Exists(directoryPath))
        {
            Debug.LogWarning($"ObservationLogs directory not found at: {directoryPath}");
            return null;
        }
        
        // Find the most recent CSV file
        DirectoryInfo dirInfo = new DirectoryInfo(directoryPath);
        FileInfo[] files = dirInfo.GetFiles("Observations_*.csv");
        
        if (files.Length == 0)
        {
            Debug.LogWarning("No CSV files found in ObservationLogs");
            return null;
        }
        
        // Get the most recently modified file
        FileInfo mostRecent = files[0];
        foreach (FileInfo file in files)
        {
            if (file.LastWriteTime > mostRecent.LastWriteTime)
            {
                mostRecent = file;
            }
        }
        
        Debug.Log($"Reading CSV: {mostRecent.Name}");
        
        // Read the file content
        return File.ReadAllText(mostRecent.FullName);
    }

	private string CreateJsonPayload(string csvContent, string experimentId, string userId)
    {
		// Escape special characters for JSON
		string escapedCsv = csvContent
			.Replace("\\", "\\\\")   // Backslashes first
			.Replace("\"", "\\\"")   // Then quotes
			.Replace("\n", "\\n")    // Then newlines (handles both \r\n and \n)
			.Replace("\r", "\\r")    // Then carriage returns
			.Replace("\t", "\\t")    // Tabs
			.Replace("\b", "\\b")    // Backspace
			.Replace("\f", "\\f");   // Form feed

		string escapedUserId = userId
			.Replace("\\", "\\\\")
			.Replace("\"", "\\\"");

        // Use StringBuilder for better performance with large strings
		StringBuilder sb = new StringBuilder();
		sb.Append("{");
		sb.Append("\"csv_data\":\"").Append(escapedCsv).Append("\",");
		sb.Append("\"encoding\":\"plain\",");
		sb.Append("\"session_id\":\"").Append(sessionId).Append("\",");
        sb.Append("\"experiment_id\":\"").Append(experimentId).Append("\",");
        sb.Append("\"user_id\":\"").Append(escapedUserId).Append("\"");
		sb.Append("}");

        return sb.ToString();
    }
}

/// <summary>
/// Lambda API Gateway response wrapper
/// </summary>
[Serializable]
public class LambdaResponse
{
    public int statusCode;
    public string body;
}

/// <summary>
/// Response structure from Lambda (nested in body)
/// </summary>
[Serializable]
public class UploadResponse
{
    public string message;
    public string s3_key;
    public int row_count;
    public string session_id;
}