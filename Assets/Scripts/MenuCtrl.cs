using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

public class MenuCtrl : MonoBehaviour {

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

	public void quitGame() {
		Application.Quit();
	}

	public void gotoPlayMultiMan() {
		//Application.LoadLevel("main");
		print("gotoPlayMultiMan");
		MasterSceneManager.Instance.LoadNext("lobby");
	}
	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
