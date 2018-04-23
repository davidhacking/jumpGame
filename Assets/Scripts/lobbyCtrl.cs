using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using HttpHelper;
using PlayerJson;
using LitJson;

public class lobbyCtrl : MonoBehaviour {

	public GameObject createRoomBtn;
	public GameObject roomList;
	public GameObject itemTemplate;
	public Text tip;
	public Text title;
	public AudioSource bgm;
	public bool displayFlag = true;

	float deltaTime;

	void playMusic(AudioSource audio) {
		if (!audio.isPlaying){
			audio.Play();
		}
	}

	void stopMusic(AudioSource audio) {
		if (audio.isPlaying){
			audio.Stop();
		}
	}

	public void createRoomCB(string ret) {
		displayFlag = true;
		LitJson.JsonData x = LitJson.JsonMapper.ToObject(ret);
		//print("createRoomCB retrun: " + x["return"]);
		if (x["return"].ToString() == "success" && x["data"] != null) {
			title.text = x["data"]["roomId"].ToString() + "号房间";
			yourName.roomId = x["data"]["roomId"].ToString();
			clearRoomList();
			yourName.playerList = new List<string>();
			for (int i = 0; i < x["data"]["playerList"].Count; i++) {
				print("i: " + i + "; data: " + x["data"]["playerList"][i]["playerId"].ToString() + ", " + x["data"]["playerList"][i]["playerName"].ToString());
				bool firstItemFlag = false;
				if (i == 0) {
					firstItemFlag = true;
				}
				addItemToList(x["data"]["playerList"][i]["playerId"].ToString(), x["data"]["playerList"][i]["playerName"].ToString(), firstItemFlag);
				yourName.playerList.Add(x["data"]["playerList"][i]["playerId"].ToString() + ";" +x["data"]["playerList"][i]["playerName"].ToString());
			}
			if (x["data"]["status"].ToString() == "playing") {
				print("start playing");
				MasterSceneManager.Instance.LoadNext("main");
				stopMusic(bgm);
				return;
			}
		} else {
			if (deltaTime > 1) {
				setTips("加入房间失败");
			}
		}
		if (quitRoomClicked == false) {
			System.Action<string> callback = createRoomCB;
			StartCoroutine(HttpHelper.HttpHelper.createRoom(yourName.playerId, yourName.playerName, callback));
		}
	}

	public void onHttpError(string httpError) {
		displayFlag = false;
		setTips("无法连接服务器");
		System.Action<string> callback = createRoomCB;
		StartCoroutine(HttpHelper.HttpHelper.createRoom(yourName.playerId, yourName.playerName, callback));
	}

	bool quitRoomClicked;

	// Use this for initialization
	void Start() {
		playMusic(bgm);
		deltaTime = 0;
		quitRoomClicked = false;
		tip.gameObject.SetActive(false);
		// 匹配房间
		System.Action<string> callback = createRoomCB;
		HttpHelper.HttpHelper.onHttpError = onHttpError;
		StartCoroutine(HttpHelper.HttpHelper.createRoom(yourName.playerId, yourName.playerName, callback));
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

	public void clearRoomList() {
		foreach (Transform child in roomList.transform) {
			if (child.gameObject == itemTemplate) {
				itemTemplate.SetActive(false);
		    	continue;
		    }
		    Destroy(child.gameObject);
		}
	}

	public void addItemToList(string playerId, string playerName, bool firstItemFlag) {
		GameObject item = null;
		if (firstItemFlag) {
			item = itemTemplate;
		} else {
			item = Instantiate(itemTemplate);
			item.transform.SetParent(roomList.transform);
			item.transform.localPosition = new Vector3(item.transform.localPosition.x,
				item.transform.localPosition.y,
				0);
			item.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
		}
		Text roomName = item.transform.Find("text").GetComponent<Text>();
		Button btn = item.transform.Find("btn").GetComponent<Button>();
		roomName.text = playerName;
		btn.GetComponentInChildren<Text>().text = "加入";
		btn.gameObject.SetActive(false);
		item.SetActive(true);
	}

	public void updateRoomList(string ret) {
		print("updateRoomList ret: " + ret);
		LitJson.JsonData x = LitJson.JsonMapper.ToObject(ret);
		print(x["return"]);
		clearRoomList();
		for (int i = 0; i < x["data"].Count; i++) {
			print("i: " + i + "; data: " + x["data"][i]);
			bool firstItemFlag = false;
			if (i == 0) {
				firstItemFlag = true;
			}
			addItemToList(x["data"][i]["roomName"].ToString(), 
				x["data"][i]["roomId"].ToString(), 
				firstItemFlag);
		}
	}

	public void gotoMenu(string p) {
		print("gotoMenu clicked");
		MasterSceneManager.Instance.LoadNext("menu");
		stopMusic(bgm);
		yourName.roomId = null;
	}

	public void quitRoom() {
		print("playerId: " + yourName.playerId);
		print("roomId: " + yourName.roomId);
		if (quitRoomClicked == false) {
			quitRoomClicked = true;
		}
		if (yourName.roomId != null) {
			System.Action<string> callback = gotoMenu;
			StartCoroutine(HttpHelper.HttpHelper.delPlayerInRoom(yourName.playerId, yourName.roomId, callback));
		}
		gotoMenu(null);
	}
	
	// Update is called once per frame
	void Update() {
		deltaTime += Time.deltaTime;
        if (deltaTime > 2) {
        	deltaTime = 0;
        	// 匹配房间
        	if (displayFlag) setTips("正在匹配。。。");
		}
	}
}
