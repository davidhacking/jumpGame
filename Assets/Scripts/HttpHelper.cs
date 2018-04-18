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

    	public static readonly string WEB_SERVER_URL = "http://10.251.110.192:5000";

    	public static IEnumerator syncPlayerStatus(string status, string roomId, int playerId) {
            // for test2
            if (playerId == 0) playerId = 1;
        	// validate
            string url = WEB_SERVER_URL + "/syncPlayerStatus?status=" + status 
                + "&roomId=" + roomId 
                + "&playerId=" + playerId;
            UnityWebRequest www = UnityWebRequest.Get(url);
            Debug.Log("url: " + url);
    		yield return www.Send();
    		if (www.isError) {
                Debug.Log(www.error);
            } else {
            	Debug.Log(www.downloadHandler.text);
            }
        }

        public static IEnumerator updatePlayerStatus(string roomId, int playerId, System.Action<string, int, string> callback) {
            UnityWebRequest www = UnityWebRequest.Get(WEB_SERVER_URL + "/getPlayerStatus?roomId=" + roomId 
                + "&playerId=" + playerId);
            yield return www.Send();
            if (www.isError) {
                Debug.Log(www.error);
            } else {
                Debug.Log("updatePlayerStatus response: " + www.downloadHandler.text);
                // 100ms 同步一次
                //yield return new WaitForSeconds(0.1f);
                yield return new WaitForSeconds(0.1f);
                callback(roomId, playerId, www.downloadHandler.text);
            }
        }

        public static IEnumerator updatePlatformStatus(string roomId, System.Action<string> callback) {
            UnityWebRequest www = UnityWebRequest.Get(WEB_SERVER_URL + "/getPlatformStatus?roomId=" + roomId);
            yield return www.Send();
            if (www.isError) {
                Debug.Log(www.error);
            } else {
                Debug.Log("updatePlatformStatus response: " + www.downloadHandler.text);
                // 100ms 同步一次
                //yield return new WaitForSeconds(0.1f);
                yield return new WaitForSeconds(0.1f);
                callback(www.downloadHandler.text);
            }
        }

        public static IEnumerator roomList(System.Action<string> callback) {
            UnityWebRequest www = UnityWebRequest.Get(WEB_SERVER_URL + "/roomList");
            yield return www.Send();
            if (www.isError) {
                Debug.Log(www.error);
            } else {
                Debug.Log("roomList response: " + www.downloadHandler.text);
                callback(www.downloadHandler.text);
            }
        }

        public static IEnumerator createRoom(string playerId, string playerName, System.Action<string> callback) {
            UnityWebRequest www = UnityWebRequest.Get(WEB_SERVER_URL + "/createRoom?playerId=" + playerId + "&playerName=" + playerName);
            yield return www.Send();
            if (www.isError) {
                Debug.Log(www.error);
            } else {
                Debug.Log("createRoom response: " + www.downloadHandler.text);
                callback(www.downloadHandler.text);
            }
        }

        public static IEnumerator canStart(string roomId, string playerId, System.Action<string> callback) {
            UnityWebRequest www = UnityWebRequest.Get(WEB_SERVER_URL + "/canStart?playerId=" + playerId + "&roomId=" + roomId);
            yield return www.Send();
            if (www.isError) {
                Debug.Log(www.error);
            } else {
                Debug.Log("canStart response: " + www.downloadHandler.text);
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
