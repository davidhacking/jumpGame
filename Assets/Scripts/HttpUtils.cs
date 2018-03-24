using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
 
public class HttpUtils : MonoBehaviour {

	private static readonly string WEB_SERVER_URL = "http://127.0.0.1:5000";

    void Start() {
        StartCoroutine(getJson("/topNScore", "?n=3"));
    }
 
    public IEnumerator getJson(string path, string formStr) {
        UnityWebRequest www = UnityWebRequest.Get(WEB_SERVER_URL + path + formStr);
		yield return www.Send();
		if (www.isError) {
            Debug.Log(www.error);
        } else {
            Debug.Log(www.downloadHandler.text);
        }
    }
}
