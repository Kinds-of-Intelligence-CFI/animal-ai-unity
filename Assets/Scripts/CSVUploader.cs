using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Uploads CSV telemetry files to AWS Lambda endpoint
/// Add this component to the same GameObject as CSVWriter
/// TODO: If running on browser, don't create a file at all
/// </summary>
public class CSVUploader : MonoBehaviour
{
    [Header("Lambda Endpoint Configuration")]
    [SerializeField] private string lambdaUrl = "https://mjlo2ftcn3.execute-api.eu-north-1.amazonaws.com/Prod/telemetry";
    
    // private CSVWriter csvWriter;
    private string sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    private bool isUploading = false;

    // void Start()
    // {
    //     // Find the CSVWriter component
    //     // csvWriter = GetComponent<CSVWriter>();
    //     // if (csvWriter == null)
    //     // {
    //     //     Debug.LogError("CSVUploader: CSVWriter component not found on this GameObject!");
    //     //     enabled = false;
    //     //     return;
    //     // }
        
    //     // Generate unique session ID for this play session
    //     sessionId = Guid.NewGuid().ToString("N").Substring(0, 12);
    //     Debug.Log($"CSV Upload Session ID: {sessionId}");
    // }

    /// <summary>
    /// Call this method to manually trigger an upload
    /// </summary>
    public void TriggerUpload(string user_id)
    {
        if (!isUploading)
        {
            Debug.Log("Pre --");
            StartCoroutine(UploadCSV(user_id));
            Debug.Log("Post --");
        }
        else
        {
            Debug.LogWarning("Upload already in progress");
        }
    }

    private IEnumerator UploadCSV(string user_id)
    {
        Debug.Log("During --");
        isUploading = true;
        Debug.Log("Starting CSV upload...");
        
        // Flush any pending logs to the CSV file
        // csvWriter.FlushLogQueue();
        
        // Wait a frame to ensure file write completes
        yield return new WaitForEndOfFrame();
        
        // Read the CSV file content
        string csvContent = ReadCurrentCSV();
        
        if (string.IsNullOrEmpty(csvContent))
        {
            Debug.LogWarning("No CSV content to upload");
            isUploading = false;
            yield break;
        }
        
        Debug.Log($"CSV content loaded: {csvContent.Length} characters");
        
		// Create the JSON payload
		string jsonPayload = CreateJsonPayload(csvContent, user_id);

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
                Debug.Log($"✓ CSV uploaded successfully!");
                Debug.Log($"Response: {request.downloadHandler.text}");
                
                // Parse response to get details
                try
                {
                    UploadResponse response = JsonUtility.FromJson<UploadResponse>(request.downloadHandler.text);
                    Debug.Log($"S3 Key: {response.s3_key}");
                    Debug.Log($"Rows uploaded: {response.row_count}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not parse response JSON: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"✗ CSV upload failed!");
                Debug.LogError($"Error: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                Debug.LogError($"Response Body: {request.downloadHandler.text}");
            }
        }
        
        isUploading = false;
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

	private string CreateJsonPayload(string csvContent, string userId)
    {
		// Escape special characters for JSON - do in correct order!
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

        // Create JSON using StringBuilder for better performance with large strings
		StringBuilder sb = new StringBuilder();
		sb.Append("{");
		sb.Append("\"csv_data\":\"").Append(escapedCsv).Append("\",");
		sb.Append("\"encoding\":\"plain\",");
		sb.Append("\"session_id\":\"").Append(sessionId).Append("\",");
		sb.Append("\"user_id\":\"").Append(escapedUserId).Append("\"");
		sb.Append("}");

        return sb.ToString();
    }
}

/// <summary>
/// Response structure from Lambda
/// </summary>
[Serializable]
public class UploadResponse
{
    public string message;
    public string s3_key;
    public int row_count;
    public string session_id;
}