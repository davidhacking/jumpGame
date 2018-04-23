using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using UnityEngine.Networking;
using System.Text.RegularExpressions;


public class yourName : MonoBehaviour {

	public static string playerName = null;
	public static string playerId = null;
	public static string roomId = null;
	public static string roomType = null;
	public static List<string> playerList = null;
	public bool iknow = false;

	public InputField name;
	public Text tip;
	public Button go;

	FileUtils fileUtils;
	HttpUtils httpUtils;
	/*
	name
	*/
	private static readonly string CONF_FILE = "config";

	float _waitTime = 2f;
	void OnGUI() {
        if (_waitTime < 2) {
        	GUIStyle guiStyle = new GUIStyle();
        	guiStyle.fontSize = 70;
        	guiStyle.normal.textColor = Color.red;
            GUI.Label(new Rect(Screen.width / 2 - 240, Screen.height / 2 - 50, 200, 100), "再按一次退出", guiStyle);
            _waitTime -= Time.deltaTime;
            if (_waitTime < 0) {
                _waitTime = 2;
            }
            if (Input.GetKeyDown(KeyCode.Escape)) {
                Application.Quit();
            }
        }
        if (Input.GetKeyUp(KeyCode.Escape) && _waitTime == 2) {
            _waitTime -= Time.deltaTime;
        }
    }

	public IEnumerator register(string nameStr) {
    	// validate
        UnityWebRequest www = UnityWebRequest.Get(HttpHelper.HttpHelper.WEB_SERVER_URL + "/register?name=" + nameStr);
		yield return www.Send();
		if (www.isError) {
            Debug.Log(www.error);
            setTips("无法连接服务器");
        } else {
            if (www.downloadHandler.text == "fail") {
            	setTips("请换一个名字");
            } else if (www.downloadHandler.text == "success") {
            	ArrayList lines = fileUtils.readFileToLines(CONF_FILE);
            	if (lines == null) {
            		fileUtils.writeFile(CONF_FILE, name.text);
            	} else {
            		fileUtils.writeFile(CONF_FILE, name.text + "\n");
            		foreach (string line in lines) {
            			fileUtils.writeFile(CONF_FILE, line + "\n");
            		}
            	}
            	//Application.LoadLevel("menu");
            	playerName = nameStr;
            	playerId = HttpHelper.HttpHelper.md5(playerName);
            	MasterSceneManager.Instance.LoadNext("menu");
            	setTips("Application.LoadLeve menu");
            }
        }
    }

	void initUI() {
		tip.gameObject.SetActive(false);
	}

	IEnumerator closeTips() {
		yield return new WaitForSeconds(1);
		tip.text = "";
		tip.gameObject.SetActive(false);
	}

	void setTips(string text) {
		tip.text = text;
		tip.gameObject.SetActive(true);
		StartCoroutine(closeTips());
	}

	// Use this for initialization
	void Start () {
		playerName = null;
		initUI();
		fileUtils = GetComponent<FileUtils>();
		ArrayList lines = fileUtils.readFileToLines(CONF_FILE);
		string nameTxt = "你的名字";
		bool flag = false;
		if (lines != null) {
			print("conf: ");
			for (int i = 0; i < lines.Count; i++) {
				print(lines[i]);
				if (i == 0) {
					nameTxt = (string) lines[i];
					flag = true;
				}
			}
		}
		name.placeholder.GetComponent<Text>().text = nameTxt;
		if (flag) {
			playerName = nameTxt;
			playerId = HttpHelper.HttpHelper.md5(playerName);
			MasterSceneManager.Instance.LoadNext("menu");
		}
	}

	public void onGoBtnClick() {
		print("go clicked");
		if (string.IsNullOrEmpty(name.text)) {
			setTips("还不能走，请输入你的名字");
			return;
		}
		if (name.text.Length > 16) {
			setTips("名字不能大于16个字符");
			return;
		}
		if (name.text.Length > 5 && iknow == false) {
			iknow = true;
			setTips("名字大于5会被截断,确认再点go");
			return;
		}
		Match match = Regex.Match(name.text, @"^[a-zA-Z0-9]+([_ -]?[a-zA-Z0-9])*$",
            RegexOptions.IgnoreCase);
		if (!match.Success) {
			setTips("名字只能是数字和字母");
			return;
		}

		StartCoroutine(register(name.text));
	}

	public void onBackBtnClick() {
		MasterSceneManager.Instance.LoadPrevious();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
