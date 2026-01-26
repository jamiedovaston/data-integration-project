using Best.HTTP;
using System;
using System.Collections;
using UnityEngine;

public static class PlayerServices
{
    public struct UserData_ResultBody
    {
        public string id, username;
    }

    public static IEnumerator C_AuthorizeNewPlayer(Action<UserData_ResultBody> OnSuccess, Action OnFail)
    {
        HTTPRequest request = new HTTPRequest(new Uri($"http://jamie-portfolio-nextjs-ybde9q-9f73ad-82-68-47-77.traefik.me/api/users/"), HTTPMethods.Post)
        {
            UploadSettings = new Best.HTTP.Request.Settings.UploadSettings()
            {
                DisposeStream = true
            },
            DownloadSettings = new Best.HTTP.Request.Settings.DownloadSettings()
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
                    UserData_ResultBody body = JsonUtility.FromJson<UserData_ResultBody>(request.Response.DataAsText);
                    Debug.Log($"Success: {request.Response.DataAsText}");
                    OnSuccess?.Invoke(body);
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
            case HTTPRequestStates.TimedOut :
                Debug.Log("Timed out!");
                OnFail?.Invoke();
                break;
            default:
                break;
        }
    }
}
