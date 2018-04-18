using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

public class MenuCtrl : MonoBehaviour {

	public GameObject restartBtn;
	public GameObject resumeBtn;
	public GameObject historyBtn;

	float _waitTime = 2f;

	void OnGUI() {
        if (_waitTime < 2) {
        	GUIStyle guiStyle = new GUIStyle();
        	guiStyle.fontSize = 40;
        	guiStyle.normal.textColor = new Color(162, 167, 202);
            GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 30, 200, 100), "再按一次退出", guiStyle);
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

	public void gotoMain() {
		//Application.LoadLevel("main");
		print("gotoMain");
		MasterSceneManager.Instance.mainPause = false;
		MasterSceneManager.Instance.LoadNext("main");
	}

	public void gotoPlayMultiMan() {
		//Application.LoadLevel("main");
		print("gotoPlayMultiMan");
		MasterSceneManager.Instance.LoadNext("lobby");
	}

	public void gotoHistory() {
		MasterSceneManager.Instance.loadScrollView();
	}

	public void resumeGame() {
		MasterSceneManager.Instance.resumeMain();
	}

	// Use this for initialization
	void Start () {
		if (MasterSceneManager.Instance.mainPause) {
			resumeBtn.SetActive(true);
			restartBtn.GetComponentInChildren<Text>().text = "重新开始";
			// historyBtn.SetActive(false);
		} else {
			resumeBtn.SetActive(false);
			restartBtn.GetComponentInChildren<Text>().text = "单人跳";
			// historyBtn.SetActive(true);
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
