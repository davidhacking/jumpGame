using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

public class MenuCtrl : MonoBehaviour {

	public Button twoManEasy;
	public Button twoManHard;
	public Button threeManEasy;
	public Button threeManHard;

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

	public void quitGame() {
		Application.Quit();
	}

	public UnityEngine.Events.UnityAction btnClick(string id) {
		return delegate() { gotoPlayMultiMan(id); };
	}

	public void gotoPlayMultiMan(string id) {
		print("gotoPlayMultiMan");
		if (id == "two_man_easy") {
			yourName.roomType = "two_man_easy";
			Application.LoadLevel("lobby");
		} else if (id == "three_man_easy") {
			yourName.roomType = "three_man_easy";
			Application.LoadLevel("lobby");
		} else if (id == "two_man_hard") {
			yourName.roomType = "two_man_hard";
			Application.LoadLevel("lobby");
		} else if (id == "three_man_hard") {
			yourName.roomType = "three_man_hard";
			Application.LoadLevel("lobby");
		} else {
			print("some id not correct id: " + id);
		}
	}
	// Use this for initialization
	void Start () {
		twoManEasy.onClick.AddListener(btnClick("two_man_easy"));
		twoManHard.onClick.AddListener(btnClick("two_man_hard"));
		threeManEasy.onClick.AddListener(btnClick("three_man_easy"));
		threeManHard.onClick.AddListener(btnClick("three_man_hard"));
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
