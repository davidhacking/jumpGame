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

    	private static readonly string WEB_SERVER_URL = "http://127.0.0.1:5000";

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
    }
}
