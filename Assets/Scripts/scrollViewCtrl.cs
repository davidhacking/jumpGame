using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;

public class scrollViewCtrl : MonoBehaviour {

	public GameObject content;
	public UnityEngine.UI.Text text;
	public UnityEngine.UI.Text text2;
	private int i;

	private static readonly string WEB_SERVER_URL = "http://10.251.110.192:5000";

	float deltaStep = 125.0f;

	float nextPos() {
		return deltaStep * i++ + 1700;
	}

	List<UnityEngine.UI.Text> textList = new List<UnityEngine.UI.Text>();

	float deltaTime = 0;

	Color[] colors = new Color[] {
		new Color(254.0f/255.0f, 0.0f/255.0f, 0.0f/255.0f), 
		new Color(255.0f/255.0f, 165.0f/255.0f, 0.0f/255.0f),
		new Color(255.0f/255.0f, 255.0f/255.0f, 0.0f/255.0f),
		new Color(0.0f/255.0f, 255.0f/255.0f, 0.0f/255.0f),
		new Color(0.0f/255.0f, 127.0f/255.0f, 255.0f/255.0f),
		new Color(0.0f/255.0f, 0.0f/255.0f, 255.0f/255.0f),
		new Color(139.0f/255.0f, 0.0f/255.0f, 255.0f/255.0f),
	};

	public IEnumerator topNScore(int n) {
    	// validate
    	print("h1");
        UnityWebRequest www = UnityWebRequest.Get(WEB_SERVER_URL + "/topNScore?n=" + n);
        print("h1");
		yield return www.Send();
		print("h2");
		if (www.isError) {
            Debug.Log(www.error);
        } else {
        	print(www.downloadHandler.text);
        	if (!string.IsNullOrEmpty(www.downloadHandler.text)) {
        		string[] data = Regex.Split(www.downloadHandler.text, "\n", RegexOptions.IgnoreCase);
        		if (textList.Count <= 0) {
	        		for (int i = 0; i < data.Length; i++) {
	        			print(data[i]);
	        			string[] ss = Regex.Split(data[i], ",", RegexOptions.IgnoreCase);
	        			if (ss.Length != 2) {
	        				continue;
	        			}
	        			float step = nextPos();
						UnityEngine.UI.Text t = Instantiate(text, text.transform.position + new Vector3(0, deltaStep, 0), Quaternion.Euler(Vector3.zero));
						t.text = ss[0];
						t.color = colors[i % colors.Length];
						t.transform.SetParent(content.transform);
						t.transform.Translate(0, text.transform.position.y - step, 0);
						textList.Add(t);

						UnityEngine.UI.Text t2 = Instantiate(text2, text2.transform.position + new Vector3(0, deltaStep, 0), Quaternion.Euler(Vector3.zero));
						t2.text = ss[1];
						t2.color = colors[i % colors.Length];
						t2.transform.SetParent(content.transform);
						t2.transform.Translate(0, text2.transform.position.y - step, 0);
						textList.Add(t2);
	        		}
	        	} else {
	        		for (int i = 0; i < data.Length; i++) {
	        			print(data[i]);
	        			string[] ss = Regex.Split(data[i], ",", RegexOptions.IgnoreCase);
	        			print("length: " + ss.Length);
	        			if (ss.Length != 2) {
	        				continue;
	        			}
	        			int k = i * 2;
						textList[k].text = ss[0];
						textList[k+1].text = ss[1];
					}
	        	}
        	}
        }
    }

	// Use this for initialization
	void Start () {
		print("scrollViewCtrl");
		i = 1;
		StartCoroutine(topNScore(20));
	}

	public void onBackBtnClick() {
		MasterSceneManager.Instance.backInScrollView();
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.Escape)) {
            onBackBtnClick();
        }
        deltaTime += Time.deltaTime;
        if (deltaTime > 2) {
        	print(deltaTime);
        	deltaTime = 0;
        	i = 1;
			StartCoroutine(topNScore(20));
        }
	}
}
