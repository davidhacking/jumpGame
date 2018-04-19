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

	float deltaTime;

	public void createRoomCB(string ret) {
		LitJson.JsonData x = LitJson.JsonMapper.ToObject(ret);
		print("createRoomCB retrun: " + x["return"]);
		if (x["return"].ToString() == "success") {
			setTips("正在匹配。。。");
			print("createRoomCB data: " + x["data"]);
			if (x["data"] == null) {
				System.Action<string> callback = createRoomCB;
				StartCoroutine(HttpHelper.HttpHelper.createRoom(yourName.playerId, yourName.playerName, callback));
			}
			title.text = x["data"]["roomId"].ToString() + "号房间";
			yourName.roomId = x["data"]["roomId"].ToString();

			clearRoomList();
			yourName.playerList = new List<string>();
			for (int i = 0; i < x["data"]["playerList"].Count; i++) {
				print("i: " + i + "; data: " + x["data"]["playerList"][i]);
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
			}
		} else {
			setTips("加入房间失败");
			System.Action<string> callback = createRoomCB;
			StartCoroutine(HttpHelper.HttpHelper.createRoom(yourName.playerId, yourName.playerName, callback));
		}
	}

	public void onCreateRoomBtnClick() {
		print("createRoomBtn Click");
	}

	// Use this for initialization
	void Start() {
		deltaTime = 0;
		tip.gameObject.SetActive(false);
		// 匹配房间
		System.Action<string> callback = createRoomCB;
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
				print("=============");
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
		}
		Text roomName = item.transform.Find("text").GetComponent<Text>();
		Button btn = item.transform.Find("btn").GetComponent<Button>();
		roomName.text = playerName;
		btn.GetComponentInChildren<Text>().text = "加入";
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
			addItemToList(x["data"][i]["roomName"].ToString(), x["data"][i]["roomId"].ToString(), firstItemFlag);
		}
	}
	
	// Update is called once per frame
	void Update() {
		deltaTime += Time.deltaTime;
        if (deltaTime > 2) {
        	deltaTime = 0;
        	// 匹配房间
			System.Action<string> callback = createRoomCB;
			StartCoroutine(HttpHelper.HttpHelper.canStart(yourName.roomId, yourName.playerId, callback));
        }
	}
}
