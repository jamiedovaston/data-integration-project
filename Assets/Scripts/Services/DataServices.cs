using Best.HTTP;
using Best.HTTP.Request.Settings;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class DataServices
{
    public struct KillPlayer_RequestBody
    {
        public string uuid;
        public float x;
        public float y;
    }

    public static IEnumerator C_PlayerKilledDataPing(string uuid, float x, float y, Action OnSuccess = null, Action OnFail = null)
    {
        KillPlayer_RequestBody requestBody = new KillPlayer_RequestBody()
        {
            uuid = uuid,
            x = x,
            y = y,
        };

        var json = JsonConvert.SerializeObject(requestBody);
        byte[] body = Encoding.UTF8.GetBytes(json);

        HTTPRequest request = new HTTPRequest(new Uri($"http://jamie-portfolio-nextjs-ybde9q-9f73ad-82-68-47-77.traefik.me/api/positions/kill/"), HTTPMethods.Post)
        {
            UploadSettings = new Best.HTTP.Request.Settings.UploadSettings()
            {
                UploadStream = new MemoryStream(body),
                DisposeStream = true,
                UploadChunkSize = body.Length
            },
            DownloadSettings = new DownloadSettings()
        };


        request.TimeoutSettings.Timeout = TimeSpan.FromSeconds(10);

        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Authorization", "Bearer gsk_J7xR2mN9pQ4wF8vL3cB6hT1yK5aD0eZi");

        request.Send();

        yield return request;

        switch (request.State)
        {
            case HTTPRequestStates.Finished:
                if (request.Response.IsSuccess)
                {
                    Debug.Log($"Success: {request.Response.DataAsText}");
                    OnSuccess?.Invoke();
                }
                else
                {
                    Debug.Log($"Failed: {request.Response.DataAsText}");
                    OnFail?.Invoke();
                }
                break;
            case HTTPRequestStates.Error:
                Debug.Log("Error!");
                OnFail?.Invoke();
                break;
            case HTTPRequestStates.ConnectionTimedOut:
                Debug.Log("Connection timed out!");
                OnFail?.Invoke();
                break;
            case HTTPRequestStates.TimedOut:
                Debug.Log("Timed out!");
                OnFail?.Invoke();
                break;
            default:
                break;
        }
    }

}