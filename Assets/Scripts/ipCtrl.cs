using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

public class ipCtrl : MonoBehaviour {

	public InputField ip;
	public Text tip;
	public Button go;

	float _waitTime = 2f;
	void OnGUI() {
        if (_waitTime < 2) {
        	GUIStyle guiStyle = new GUIStyle();
        	guiStyle.fontSize = 70;
        	guiStyle.normal.textColor = Color.red;
            GUI.Label(new Rect(Screen.width / 2 - 240, Screen.height / 2 - 150, 200, 100), "再按一次退出", guiStyle);
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

    public void onDefaultServerClick() {
    	ip.text = "192.168.45.130:5000";
    	onGoBtnClick();
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

	public void failCB(string s) {
		setTips("无法连接服务器");
	}

	public void successCB(string s) {
		if (s != "success") {
			setTips("无法连接服务器");
		}
		HttpHelper.HttpHelper.WEB_SERVER_URL = "http://" + ip.text;
		Application.LoadLevel("login");
	}

	public void onGoBtnClick() {
		print("go clicked");
		if (string.IsNullOrEmpty(ip.text)) {
			setTips("还不能走，请输入服务器地址");
			return;
		}
		Match match = Regex.Match(ip.text, @"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5]):([0-9]{1,4}|[1-5][0-9]{4}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5])$");
		if (!match.Success) {
			setTips("请输入正确的ip和port");
			return;
		}
		setTips("尝试连接服务器");
		StartCoroutine(HttpHelper.HttpHelper.hello("http://" + ip.text, 
			delegate(string s) { successCB(s); }, 
			delegate(string s) { failCB(s); }));
	}

	// Use this for initialization
	void Start () {
		ip.text = "192.168.45.130:5000";
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
