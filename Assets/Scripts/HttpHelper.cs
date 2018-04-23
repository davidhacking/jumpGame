using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using HttpHelper;
using PlayerJson;


namespace HttpHelper {

    public class HttpHelper {

    	public static string WEB_SERVER_URL = "http://192.168.45.130:5000";
        public static float waitTime = 0.1f;

        public static IEnumerator hello(string ip, System.Action<string> successCB = null,
            System.Action<string> failCB = null) {
            // validate
            string url = ip + "/hello";
            UnityWebRequest www = UnityWebRequest.Get(url);
            Debug.Log("url: " + url);
            yield return www.Send();
            if (www.isError) {
                Debug.Log(www.error);
                if (failCB != null) {
                    failCB(www.error.ToString());
                }
            } else {
                Debug.Log(www.downloadHandler.text);
                if (successCB != null) {
                    successCB(www.downloadHandler.text);
                }
            }
        }

    	public static IEnumerator syncPlayerStatus(string status, string roomId, string playerId, System.Action<string> callback = null) {
        	// validate
            string url = WEB_SERVER_URL + "/syncPlayerStatus?status=" + status 
                + "&roomId=" + roomId 
                + "&playerId=" + playerId;
            UnityWebRequest www = UnityWebRequest.Get(url);
            Debug.Log("url: " + url);
    		yield return www.Send();
    		if (www.isError) {
                Debug.Log(www.error);
                if (onHttpError != null) {
                    onHttpError("syncPlayerStatus");
                }
            } else {
            	Debug.Log(www.downloadHandler.text);
                if (callback != null) {
                    callback(www.downloadHandler.text);
                }
            }
        }

        public static IEnumerator updatePlayerStatus(string roomId, string playerId, System.Action<string, string, string> callback) {
            UnityWebRequest www = UnityWebRequest.Get(WEB_SERVER_URL + "/getPlayerStatus?roomId=" + roomId 
                + "&playerId=" + playerId + "&myId=" + yourName.playerId);
            yield return www.Send();
            if (www.isError) {
                Debug.Log(www.error);
                if (onHttpError != null) {
                    onHttpError("updatePlayerStatus");
                }
            } else {
                Debug.Log("updatePlayerStatus response: " + www.downloadHandler.text);
                // 100ms 同步一次
                yield return new WaitForSeconds(waitTime);
                callback(roomId, playerId, www.downloadHandler.text);
            }
        }

        public static IEnumerator updatePlatformStatus(string roomId, 
            int currPlatIndex,
            int currPlayerIndex,
            System.Action<string> callback) {
            string url = WEB_SERVER_URL + "/getPlatformStatus?roomId=" + roomId +
                "&currPlatIndex=" + currPlatIndex + 
                "&currPlayerIndex=" + currPlayerIndex;
            //Debug.Log("updatePlatformStatus url: " + url);
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.Send();
            if (www.isError) {
                Debug.Log(www.error);
                if (onHttpError != null) {
                    onHttpError("updatePlatformStatus");
                }
            } else {
                //Debug.Log("updatePlatformStatus response: " + www.downloadHandler.text);
                // 100ms 同步一次
                yield return new WaitForSeconds(waitTime);
                callback(www.downloadHandler.text);
            }
        }

        public static IEnumerator roomList(System.Action<string> callback) {
            UnityWebRequest www = UnityWebRequest.Get(WEB_SERVER_URL + "/roomList");
            yield return www.Send();
            if (www.isError) {
                Debug.Log(www.error);
                if (onHttpError != null) {
                    onHttpError("roomList");
                }
            } else {
                Debug.Log("roomList response: " + www.downloadHandler.text);
                callback(www.downloadHandler.text);
            }
        }

        public static IEnumerator createRoom(string playerId, string playerName, System.Action<string> callback) {
            Debug.Log("updatePlatformStatus start time: " + PlayerJson.JsonHelper.nowTimestamp().ToString("0.00000"));
            UnityWebRequest www = UnityWebRequest.Get(WEB_SERVER_URL + "/createRoom?playerId=" + playerId + "&playerName=" + playerName);
            yield return www.Send();
            Debug.Log("time: " + PlayerJson.JsonHelper.nowTimestamp().ToString("0.00000"));
            if (www.isError) {
                Debug.Log(www.error);
                if (onHttpError != null) {
                    onHttpError("createRoom");
                }
            } else {
                //Debug.Log("createRoom response: " + www.downloadHandler.text);
                // 100ms 同步一次
                yield return new WaitForSeconds(waitTime);
                callback(www.downloadHandler.text);
            }
        }

        public static System.Action<string> onHttpError = null;

        public static IEnumerator canStart(string roomId, string playerId, System.Action<string> callback) {
            UnityWebRequest www = UnityWebRequest.Get(WEB_SERVER_URL + "/canStart?playerId=" + playerId + "&roomId=" + roomId);
            yield return www.Send();
            if (www.isError) {
                Debug.Log(www.error);
                if (onHttpError != null) {
                    onHttpError("canStart");
                }
            } else {
                //Debug.Log("canStart response: " + www.downloadHandler.text);
                callback(www.downloadHandler.text);
            }
        }

        public static IEnumerator delPlayerInRoom(string playerId, string roomId, System.Action<string> callback) {
            UnityWebRequest www = UnityWebRequest.Get(WEB_SERVER_URL + "/delPlayerInRoom?playerId=" + playerId + "&roomId=" + roomId);
            yield return www.Send();
            if (www.isError) {
                Debug.Log(www.error);
                if (onHttpError != null) {
                    onHttpError("delPlayerInRoom");
                }
            } else {
                Debug.Log("delPlayerInRoom response: " + www.downloadHandler.text);
                callback(www.downloadHandler.text);
            }
        }

        public static string md5(string tempString) {
            System.Security.Cryptography. MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] bs = System.Text.Encoding.UTF8.GetBytes(tempString);
            byte[] hs = md5.ComputeHash(bs);
            return System.BitConverter.ToString(hs).Replace("-", "");
        }
    }
}
