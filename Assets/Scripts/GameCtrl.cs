using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using HttpHelper;
using PlayerJson;

public class GameCtrl : MonoBehaviour {

	private static readonly string CONF_FILE = "config";
	private static float DEAD_TIME = 6.0f;
	public Text log;
	public float Speed;
	public GameObject Platform;
	public GameObject playerAsset;
	public GameObject playAgainBtn;
	public GameObject quitGameBtn;
	public Rigidbody Rig;
	public AudioSource pressingAudio;
	public AudioSource deadAudio;
	public AudioSource bonusAudio;
	public AudioSource stepAudio;
	public AudioSource gameOverAudio;

	public Text[] scoreText;
	public Text timeText;

	public FileUtils fileUtils;

	bool gameSyncFlag;
	int syncPlatIndex;

	string playerName;

	Vector3 Point1;
	Vector3 Point2;
	float Timer;
	float Scale;
	bool IsPressing;
	int Bonus;
	GameStatus gameStatus;
	// all players data struct
	Dictionary<string, PlayerJson.JsonHelper.Player> playerList;
	List<GameObject> platforms;
	public Object platformLock = new Object();
	public List<string> platformQueue;
	public int toBeDestoryPlatformIndex = 0;
	public double platformInitTime = double.NaN;
	CameraCtrl cameraCtrl;
	bool isShowRecord = false;
	float playAgainTime = 1.5f;
	float playDeltaTime = 0;
	float gameTime = 0;

	int extraBonus = 0;

	enum GameStatus {
		INIT,
		CREATE_PLATFORM,
		SHOW_PLATFORM,
		TAPING,
		REBOUND,
		PLAYER_JUMPING,
		GAME_OVER,
		PLAY_AGAIN,
		GAME_WIN,
	}

	int ANIMATION_HEIGHT = 9;

	float _waitTime = 2f;
    void OnGUI() {
        if (Input.GetKeyUp(KeyCode.Escape)) {
        	onBackBtnClick();
        }
    }

	public void clearPlayer() {
		if (playerList == null) {
			return;
		}
		List<string> keys = new List<string>(playerList.Keys);
		foreach (string key in keys) {
			PlayerJson.JsonHelper.Player p = playerList[key];
			destroyPlayer(ref p);
			playerList[key] = p;
		}
	}

	public void destroyPlayer(ref PlayerJson.JsonHelper.Player p) {
		if (p.player != null) {
			Destroy(p.player);
		}
		if (p.player != null) {
			Destroy(p.playerLabel);
		}
	}

	public void newPlayerList() {

		// set score position
		playerList = new Dictionary<string, PlayerJson.JsonHelper.Player>();
		yourName.playerList.Sort();
		for (int i = 0; i < yourName.playerList.Count; i++) {
			PlayerJson.JsonHelper.Player p = new PlayerJson.JsonHelper.Player(); 
			string[] ss = Regex.Split(yourName.playerList[i], ";", RegexOptions.IgnoreCase);
			string id = ss[0];
			p.index = i;
			p.playerId = ss[0];
			p.playerName = ss[1];
			p.player = Instantiate(playerAsset, new Vector3 (0, 1.25f, randomPos(0, i)), Quaternion.Euler (Vector3.zero));
			p.playerPos = p.player.transform.Find("position").gameObject;
			p.playerLabel = new GameObject();
			p.playerLabel.transform.position = p.player.transform.position + new Vector3(0, p.player.transform.position.y + 0.4f * i, 0);
			p.playerLabelTextMesh = p.playerLabel.AddComponent<TextMesh>() as TextMesh;
			p.playerLabel.AddComponent<MeshRenderer>();
			p.playerLabel.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
			p.playerLabelTextMesh.text = p.playerName;
			p.currentPlatformIndex = 0;
			p.animationQueue = new List<string>();
			p.queueLock = new Object();
			p.alive = true;
			p.status = "ok";
			if (i < scoreText.Length) {
				p.scoreText = scoreText[i];
				print("i score r g b" + i + " " + p.scoreText.color.r);
				p.score = 0;
				Material materialColored = new Material(Shader.Find("Diffuse"));
		        materialColored.color = p.scoreText.color;
		        p.playerPos.GetComponent<Renderer>().material = materialColored;
		        p.playerLabelTextMesh.color = p.scoreText.color;
				PlayerJson.JsonHelper.displayPlayerScore(ref p);
			} else {
				p.scoreText = null;
			}
			displayPlayerLabel(ref p, p.playerName);
			playerList[id] = p;
		}
		// set extra score text disable
		for (int i = yourName.playerList.Count; i < scoreText.Length; i++) {
			scoreText[i].gameObject.SetActive(false);
		}
	}

	void displayPlayerLabel(ref PlayerJson.JsonHelper.Player p, string text) {
		setEnable(ref p);
		p.playerLabel.transform.position = p.player.transform.position + new Vector3(0, p.player.transform.position.y + 0.4f * p.index, 0);
		if (p.playerId != yourName.playerId) {
			text = text.Length > 5 ? text.Substring(0, 5) : text;
		} else {
			text = "*" + (text.Length > 4 ? text.Substring(0, 4) : text);
		}
		p.playerLabelTextMesh.text = text;
	}

	void setDisable(ref PlayerJson.JsonHelper.Player p) {
		p.playerLabel.SetActive(false);
	}

	void setEnable(ref PlayerJson.JsonHelper.Player p) {
		p.playerLabel.SetActive(true);
	}

	GameObject nextPlatform(string index) {
		// debug
		/*
		print("nextPlatform playerList[index].currentPlatformIndex: " + playerList[index].currentPlatformIndex);
		for (int i = 0; i < platforms.Count; i++) {
			if (platforms[i] == null) continue; 
			print(" " + i);
			print("Vector3: " + platforms[i].transform.position.x);
			print("Vector3: " + platforms[i].transform.position.y);
			print("Vector3: " + platforms[i].transform.position.z);
		}
		*/
		return platforms[playerList[index].currentPlatformIndex + 1];
	}

	GameObject currPlatform(string index) {
		return platforms[playerList[index].currentPlatformIndex];
	}

	Vector3 currPlatformPos(string index) {
		LitJson.JsonData x = LitJson.JsonMapper.ToObject(platformQueue[playerList[index].currentPlatformIndex]);
		Vector3 pos = new Vector3(System.Convert.ToSingle(x["x"].ToString()), 
			System.Convert.ToSingle(x["y"].ToString()), 
			System.Convert.ToSingle(x["z"].ToString()));
		return pos;
	}

	void displayPlayerInNextPlatform(ref PlayerJson.JsonHelper.Player p) {
		p.player.transform.position = new Vector3(p.player.transform.position.x, 1.25f, p.player.transform.position.z);
		p.playerPos.transform.rotation = Quaternion.Euler(0, 0, 0);
	}
	bool isBonus(ref PlayerJson.JsonHelper.Player p) {
		if (p.direction) {
			// z
			return Mathf.Abs(p.player.transform.position.z - 
				nextPlatform(p.playerId).transform.position.z) < 0.2;
		} else {
			return Mathf.Abs(p.player.transform.position.x - 
				nextPlatform(p.playerId).transform.position.x) < 0.2;
		}
	}
	bool playerJump(string index) {
		float myScale = 1.5f;
		PlayerJson.JsonHelper.Player p = playerList[index];
		setDisable(ref p);
		p.status = "jumping";
		playerList[index] = p;
		p.VSpeed -= Time.fixedDeltaTime * myScale;
		print("index: " + index + "; playerList[index].direction: " + p.direction);
		print("index: " + index + "; p.playerPos.transform.position.x: " + p.playerPos.transform.position.x.ToString("0.0000000000"));
		print("index: " + index + "; p.playerPos.transform.position.y: " + p.playerPos.transform.position.y.ToString("0.0000000000"));
		print("index: " + index + "; p.playerPos.transform.position.z: " + p.playerPos.transform.position.z.ToString("0.0000000000"));
		print("index: " + index + "; p.power: " + p.power);
		if (p.direction) {
			p.player.transform.Translate(new Vector3(0, p.VSpeed / 2, 
				p.power / 0.6f * Time.fixedDeltaTime));
			p.playerPos.transform.Rotate(new Vector3(720 * Time.fixedDeltaTime * myScale, 0));
		} else {
			p.player.transform.Translate(new Vector3(-p.power / 0.6f * (Time.fixedDeltaTime * myScale), 
				p.VSpeed / 2, 0));
			p.playerPos.transform.Rotate(new Vector3(0, 0, 720 * (Time.fixedDeltaTime * myScale)));
		}
		if (p.player.transform.position.y <= 1) {
			displayPlayerInNextPlatform(ref p);
			print("debug: p.player is null? " + (p.player == null));
			print("debug: p.player.transform.position.x " + p.player.transform.position.x);
			if (Mathf.Abs(p.player.transform.position.x - currPlatformPos(index).x) < 0.5 && 
				Mathf.Abs(p.player.transform.position.z - currPlatformPos(index).z) < 0.5) {
				gameStatus = GameStatus.TAPING;
			} else {
				if (Mathf.Abs(p.player.transform.position.x - nextPlatform(index).transform.position.x) > 0.5 || 
					Mathf.Abs (p.player.transform.position.z - nextPlatform(index).transform.position.z) > 0.5) {
					print("player: " + index + " game over");
					if (index == yourName.playerId) {
						playMusic(deadAudio);
						p.power = 0;
						Timer = 0;
						StartCoroutine(HttpHelper.HttpHelper.syncPlayerStatus(
							PlayerJson.JsonHelper.animJson("game_over", playerList, yourName.playerId), 
							yourName.roomId, 
							playerList[yourName.playerId].playerId, 
							delegate(string ret) { checkGameOver(ret); }));
					}
					//p.playerPos.AddComponent<Rigidbody> ();
				} else {
					if (index == yourName.playerId) {
						if (isBonus(ref p)) {
							playMusic(bonusAudio);
							Bonus++;
							p.score += Bonus * 2 + extraBonus;
							// white yellow purple
							if (extraBonus < 2)
								extraBonus++;
							PlayerJson.JsonHelper.displayPlayerScore(ref p);
						} else {
							playMusic(stepAudio);
							Bonus = 0;
							p.score = p.score + 1 + extraBonus;
							extraBonus = 0;
							PlayerJson.JsonHelper.displayPlayerScore(ref p);
						}
						p.power = 0;
						gameStatus = GameStatus.CREATE_PLATFORM;
					}
					// 当前platform+1
					p.currentPlatformIndex++;
				}
			}
			p.status = "ok";
			playerList[index] = p;
			displayPlayerLabel(ref p, p.playerName);
			return true;
		}
		playerList[index] = p;
		return false;
	}

	public PlayerJson.JsonHelper.Player syncPos(string index, ref LitJson.JsonData animJson) {
		print("xxxxxxxxxxxx syncPos: ");
		PlayerJson.JsonHelper.Player p = playerList[index];
		p.player.transform.position = new Vector3(System.Convert.ToSingle(animJson["x"].ToString()), 
			System.Convert.ToSingle(animJson["y"].ToString()), 
			System.Convert.ToSingle(animJson["z"].ToString()));
		p.prePlayerPosition = p.player.transform.position;
		p.VSpeed = System.Convert.ToSingle(animJson["vspeed"].ToString());
		p.power = System.Convert.ToSingle(animJson["power"].ToString());
		p.currentPlatformIndex = System.Convert.ToInt32(animJson["index"].ToString());
		p.direction = animJson["direction"].ToString() == "False" ? false : true;
		return playerList[index] = p;
	}

	public void syncPlatform(string platformInfo) {
		print("syncPlatform platformInfo: " + platformInfo);
		LitJson.JsonData x = LitJson.JsonMapper.ToObject(platformInfo);
		Vector3 pos = new Vector3(System.Convert.ToSingle(x["x"].ToString()), 
			System.Convert.ToSingle(x["y"].ToString()), 
			System.Convert.ToSingle(x["z"].ToString()));
		GameObject newPlatform = Instantiate(Platform, pos, Quaternion.Euler(Vector3.zero));
		platforms.Add(newPlatform);
        //重新设置相机的位置
		cameraCtrl.SetPosition((currPlatformPos(yourName.playerId) + platforms[playerList[yourName.playerId].currentPlatformIndex + 1].transform.position) / 2);
		Timer = 0;
		gameSyncFlag = false;
	}

	float destoryPlatTime = 0;

	public void syncAnimation() {
		// other player status
		//print("yourName.playerId: " + yourName.playerId);
		int t = playerList[yourName.playerId].currentPlatformIndex;
		List<string> keys = new List<string>(playerList.Keys);
		foreach (string key in keys) {
			if (key == yourName.playerId || playerList[key].alive == false) {
				continue;
			}
			lock (playerList[key].queueLock) {
				PlayerJson.JsonHelper.Player p = playerList[key];
				if (p.animationQueue.Count > 0) {
					// play jump animation
					LitJson.JsonData animJson = LitJson.JsonMapper.ToObject(p.animationQueue[0]);
					if (animJson["action"].ToString() == "jump") {
						print("jump action: " + p.animationQueue[0]);
						if (!p.syncPosFlag) {
							p = syncPos(key, ref animJson);
							p.syncPosFlag = true;
							playerList[key] = p;
						}
						bool endFlag = playerJump(key);
						p = playerList[key];
						if (endFlag) {
							p.syncPosFlag = false;
							p.animationQueue.RemoveAt(0);
						}
					} else if (animJson["action"].ToString() == "update_score") {
						p.score = System.Convert.ToInt32(animJson["score"].ToString());
						PlayerJson.JsonHelper.displayPlayerScore(ref p);
						p.animationQueue.RemoveAt(0);
					} else if (animJson["action"].ToString() == "game_over") { // game over animation
						print("game_over action: " + p.animationQueue[0]);
						p.playerPos.AddComponent<Rigidbody> ();
						p.animationQueue.RemoveAt(0);
					} else {
						print("unknow action: " + p.animationQueue[0]);
						p.animationQueue.RemoveAt(0);
					}
					playerList[key] = p;
					continue;
				}
			}
			if (playerList[key].currentPlatformIndex == t || playerList[key].currentPlatformIndex == (t + 1)) {
				//displayOtherPlayer(i);
				//displayPlayerLabel(i, "sync");
			}
		}
		// create platform
		lock (platformLock) {
			for (int i = syncPlatIndex + 1; i < platformQueue.Count; i++) {
				syncPlatform(platformQueue[i]);
			}
			syncPlatIndex = platformQueue.Count - 1 > 0 ? platformQueue.Count - 1 : 0;
		}
		// move camera
		if (platforms.Count > playerList[yourName.playerId].currentPlatformIndex + 1) {
			if (currPlatform(yourName.playerId) != null) {
				cameraCtrl.SetPosition((currPlatform(yourName.playerId).transform.position + 
					platforms[playerList[yourName.playerId].currentPlatformIndex + 1].transform.position) / 2);
			}
		}
		// destory platform
		if (platformQueue.Count > 0 && gameStatus != GameStatus.GAME_OVER && gameStatus != GameStatus.PLAY_AGAIN
			&& gameStatus != GameStatus.GAME_WIN) {
			if (platforms.Count > toBeDestoryPlatformIndex && platforms[toBeDestoryPlatformIndex] != null) {
				if (double.IsNaN(platformInitTime)) {
					LitJson.JsonData x = LitJson.JsonMapper.ToObject(platformQueue[0]);
					print("platformInitTime string: " + x["timestamp"].ToString());
					platformInitTime = double.Parse(x["timestamp"].ToString());
					print("platformInitTime: " + platformInitTime);
				}
				//print("now: " + PlayerJson.JsonHelper.nowTimestamp().ToString("0.000"));
				//print("platformInitTime: " + platformInitTime.ToString("0.000"));
				//print("compare: " + (PlayerJson.JsonHelper.nowTimestamp() - platformInitTime - DEAD_TIME * (toBeDestoryPlatformIndex + 1)));
				if (destoryPlatTime < DEAD_TIME) {
					destoryPlatTime += Time.deltaTime;
				} else {
					destoryPlatTime = 0;
					platforms[toBeDestoryPlatformIndex].SetActive(false);
					Destroy(platforms[toBeDestoryPlatformIndex]);
					platforms[toBeDestoryPlatformIndex] = null;
					toBeDestoryPlatformIndex++;
					killPlayer();
				}
			}
		}
		displayCurrPlayerGameInfo();
	}

	public void displayCurrPlayerGameInfo() {
		// 显示当前所在方块剩余秒数
		// player alive
		// if int(second) == 0 display float
		// 显示吃鸡 或第几名
		if (gameStatus != GameStatus.PLAY_AGAIN && gameStatus != GameStatus.GAME_OVER
			&& gameStatus != GameStatus.GAME_WIN) {
			double resetTime = -gameTime
				+ DEAD_TIME * (playerList[yourName.playerId].currentPlatformIndex + 1);
			int t = (int) resetTime;
			string s = "" + t + "s";
			if (t == 0) {
				s = resetTime.ToString("0.0") + "s";
			}
			if (resetTime < 0) {
				s = "0s";
			}
			timeText.text = s;
		}
	}

	public void killPlayer() {
		List<string> keys = new List<string>(playerList.Keys);
		foreach (string key in keys) {
			PlayerJson.JsonHelper.Player p = playerList[key];
			if (p.status == "jumping") continue;
			if (p.currentPlatformIndex < toBeDestoryPlatformIndex) {
				p.alive = false;
				p.playerPos.AddComponent<Rigidbody>();
			}
			playerList[key] = p;
		}
		if (playerList[yourName.playerId].alive == false) {
			if (gameStatus != GameStatus.PLAY_AGAIN &&
				gameStatus != GameStatus.GAME_OVER &&
				gameStatus != GameStatus.GAME_WIN) {
				StartCoroutine(HttpHelper.HttpHelper.syncPlayerStatus(
					PlayerJson.JsonHelper.animJson("game_over", playerList, yourName.playerId), 
					yourName.roomId, 
					playerList[yourName.playerId].playerId, 
					delegate(string ret) { checkGameOver(ret); }));
			}
		}
	}

	float backBtnClickTime;

    public void onBackBtnClick() {
    	stopMusic(pressingAudio);
		if (gameStatus == GameStatus.PLAY_AGAIN ||
			gameStatus == GameStatus.GAME_OVER ||
			gameStatus == GameStatus.GAME_WIN) {
			MasterSceneManager.Instance.LoadNext("menu");
        } else {
        	if (PlayerJson.JsonHelper.nowTimestamp() - backBtnClickTime < 2) {
        		StartCoroutine(HttpHelper.HttpHelper.syncPlayerStatus(
        			"{'action': 'game_over'}", 
        			yourName.roomId, 
        			playerList[yourName.playerId].playerId));
        		MasterSceneManager.Instance.LoadNext("menu");
        	} else {
        		setTips("双击退出");
        	}
        }
	}

	public void playAgain() {
		print("loading lobby");
		MasterSceneManager.Instance.LoadNext("lobby");
	}

	public void quitGame() {
		Application.Quit();
	}

	Vector3 randomDelta(bool direction) {
		if (direction) {
			return new Vector3(0, ANIMATION_HEIGHT, Random.Range (1.2f, 4));
		}
		return new Vector3(-Random.Range (1.2f, 4), ANIMATION_HEIGHT, 0);
	}

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

    public void updatePlayer(string roomId, string playerId, string ret) {
    	if (ret != "{}") {
    		Debug.Log("updatePlayer ret: " + ret);
			lock (playerList[playerId].queueLock) {
				playerList[playerId].animationQueue.Insert(playerList[playerId].animationQueue.Count, ret);
			}
    	}
        if (gameStatus == GameStatus.GAME_OVER || gameStatus == GameStatus.PLAY_AGAIN
        	|| gameStatus == GameStatus.GAME_WIN) {
        	return;
        }
        System.Action<string, string, string> callback = updatePlayer;
        StartCoroutine(HttpHelper.HttpHelper.updatePlayerStatus(roomId, playerId, callback));
    }
    /*
    {
		"x":
		"y":
		"z":
		"direction"
    }
    */
    public void updatePlatform(string ret) {
    	LitJson.JsonData x = LitJson.JsonMapper.ToObject(ret);
		//print("updatePlatform ret: " + ret);
		if (x["return"].ToString() != "success") {
			print("updatePlatform failed");
		}
		if (x["data"] != null) {
			lock (platformLock) {
				for (int i = platformQueue.Count; i < x["data"].Count; i++) {
					platformQueue.Insert(platformQueue.Count, x["data"][i].ToString());
				}
			}
		}

		if (gameStatus == GameStatus.GAME_OVER || gameStatus == GameStatus.PLAY_AGAIN
        	|| gameStatus == GameStatus.GAME_WIN) {
        	return;
        }
        
        System.Action<string> callback = updatePlatform;
        StartCoroutine(HttpHelper.HttpHelper.updatePlatformStatus(yourName.roomId, 
        	syncPlatIndex, 
        	playerList[yourName.playerId].currentPlatformIndex,
        	callback));
    }

    public void logOnScreen(string str) {
    	log.text = log.text + "\n" + str;
    }

    int isRenew = 0;

    public void onHttpError(string httpError) {
		setTips("无法连接服务器");
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

	public Text tip;

	// Use this for initialization
	void Start() {
		//log.gameObject.SetActive(false);
		if (yourName.roomType == "two_man_easy") {
			DEAD_TIME = 6.0f;
		} else if (yourName.roomType == "three_man_easy") {
			DEAD_TIME = 6.0f;
		} else if (yourName.roomType == "two_man_hard") {
			DEAD_TIME = 3.0f;
		} else if (yourName.roomType == "three_man_hard") {
			DEAD_TIME = 3.0f;
		}
		logOnScreen("start isRenew: " + isRenew++);
		platformLock = new Object();
		updateScoreTime = 0;
		updateLoginTime = 0;
		destoryPlatTime = 0;
		gameTime = 0;
		backBtnClickTime = 0;
		syncGameWinFlag = false;
		System.Action<string> errorCB = onHttpError;
		HttpHelper.HttpHelper.onHttpError = errorCB;
		tip.gameObject.SetActive(false);
		ArrayList lines = fileUtils.readFileToLines(CONF_FILE);
    	if (lines == null) {
    		//Application.LoadLevel("login");
    		MasterSceneManager.Instance.LoadNext("login");
    	} else {
    		playerName = (string) lines[0];
    	}

		playAgainBtn.SetActive(false);
		quitGameBtn.SetActive(false);
		cameraCtrl = GetComponent<CameraCtrl>();
		platforms = new List<GameObject>();
		IsPressing = false;
		Bonus = 0;

		initGame();
		System.Action<string, string, string> callback = updatePlayer;
		List<string> keys = new List<string>(playerList.Keys);
		foreach (string key in keys) {
			if (key == yourName.playerId) {
				continue;
			}
            StartCoroutine(HttpHelper.HttpHelper.updatePlayerStatus(yourName.roomId, key, callback));
        }
        // update platform
        System.Action<string> callback2 = updatePlatform;
        StartCoroutine(HttpHelper.HttpHelper.updatePlatformStatus(yourName.roomId, 
        	syncPlatIndex, 
        	playerList[yourName.playerId].currentPlatformIndex,
        	callback2));
	}

	private GameObject playerLabel;
	private TextMesh playerLabelTextMesh;

	void initGame() {
		isShowRecord = false;
		gameSyncFlag = false;
		extraBonus = 0;
		playDeltaTime = 0;
		Bonus = 0;
		syncPlatIndex = 0;
		if (platforms != null) {
			foreach (GameObject current in platforms) {
				Destroy(current);
			}
		}
		platforms = new List<GameObject>();
		clearPlayer();
		newPlayerList();
		Vector3 position = new Vector3 (0, 0.52f, 0);
		platforms.Add(Instantiate(Platform, position, Quaternion.Euler (Vector3.zero)));
		gameStatus = GameStatus.CREATE_PLATFORM;
	}

	bool nextDirection() {
		return (Random.Range (0, 10) > 5);
		//return true;
	}

	float randomPos(float pos, int index) {
		float[] posList = {pos - 0.4f, pos - 0.2f, pos, pos + 0.2f, pos + 0.4f};
		if (index >= 0 && index <= 4) {
			return posList[index];
		}
		return pos;
	}

	void playAgainFunc() {
		float t = Time.deltaTime;
		Timer += t;
		if (Timer < playAgainTime) playMusic(gameOverAudio);
		else stopMusic(gameOverAudio);
		playAgainBtn.SetActive(true);
		quitGameBtn.SetActive(true);
	}

	/*
	https://answers.unity.com/questions/1065971/how-do-you-detect-a-mouse-button-click-on-a-game-o.html
	检测鼠标点击的gameobject避免button click事件也被检测成pressing事件
	*/
	bool isPressing(bool flag) {
		if (Input.touchCount <= 0) {
			if (flag)
				return Input.GetMouseButtonDown(0);
			else
				return !Input.GetMouseButtonUp(0);
		} else {
			if (flag) {
				for (int i = 0; i < Input.touchCount; ++i) {
	            	if (Input.GetTouch(i).phase == TouchPhase.Began)
	                	return true;
	        	}
	        	return false;
	        } else {
	        	for (int i = 0; i < Input.touchCount; ++i) {
	            	if (Input.GetTouch(i).phase == TouchPhase.Ended)
	                	return false;
	        	}
	        	return true;
	        }
		}
	}

	float updateLoginTime = 0;

	public void unfriendlyCSharpFunction(string ret) {
		print("unfriendlyCSharpFunction gameStatus: " + gameStatus);
		print("unfriendlyCSharpFunction ret: " + ret);
		LitJson.JsonData x = LitJson.JsonMapper.ToObject(ret);
		if (x["return"].ToString() == "success"
			&& x["data"]["playerList"] != null
			&& x["data"]["playerList"].Count == 1
			&& x["data"]["playerList"][0]["playerId"] != null
			&& x["data"]["playerList"][0]["playerId"].ToString() == yourName.playerId) {
			gameStatus = GameStatus.GAME_WIN;
		}
		// 数据拿回来是0由服务器提供谁吃鸡
	}

	float updateScoreTime = 0;

	void updateFunc() {
		updateLoginTime += Time.deltaTime;
		updateScoreTime += Time.deltaTime;
		gameTime += Time.deltaTime;

        if (updateLoginTime > 1) {
        	updateLoginTime = 0;
        	// 匹配房间
        	if (gameStatus != GameStatus.GAME_OVER && gameStatus != GameStatus.PLAY_AGAIN && 
        		gameStatus != GameStatus.GAME_WIN) {
	        	System.Action<string> callback = unfriendlyCSharpFunction;
				StartCoroutine(HttpHelper.HttpHelper.canStart(yourName.roomId, yourName.playerId, callback));
        	}
        }

        if (updateScoreTime > 0.5f) {
        	updateScoreTime = 0;
        	if (gameStatus != GameStatus.GAME_OVER && gameStatus != GameStatus.PLAY_AGAIN
        		&& gameStatus != GameStatus.GAME_WIN) {
	        	StartCoroutine(HttpHelper.HttpHelper.syncPlayerStatus(
					PlayerJson.JsonHelper.animJson("update_score", playerList, yourName.playerId), 
					yourName.roomId, yourName.playerId));	
	        }
        }

		if (MasterSceneManager.Instance.mainPause) {
			return;
		}

		syncAnimation();

		//print("current status: " + gameStatus);
		switch (gameStatus) {
		case GameStatus.INIT:
			initGame();
			cameraCtrl.CameraInit();
			break;

		case GameStatus.CREATE_PLATFORM:
            //createPlatform();
            //gameStatus = GameStatus.TAPING;
            if ((playerList[yourName.playerId].currentPlatformIndex + 1) <= (platformQueue.Count - 1)) {
            	// render color
				if (extraBonus > 0) {
					Material materialColored = new Material(Shader.Find("Diffuse"));
			        materialColored.color = extraBonus == 1 ? Color.green : Color.yellow;
			        platforms[playerList[yourName.playerId].currentPlatformIndex + 1].GetComponent<Renderer>().material = materialColored;
			    }
            	PlayerJson.JsonHelper.Player p = playerList[yourName.playerId];
            	LitJson.JsonData x = LitJson.JsonMapper.ToObject(platformQueue[p.currentPlatformIndex + 1]);
            	p.direction = x["direction"].ToString() == "True" ? true : false;
            	playerList[yourName.playerId] = p;
            	gameStatus = GameStatus.TAPING;
            	Timer = 0;
            }
			break;
		case GameStatus.TAPING:
			if (isPressing(true)) {
				playMusic(pressingAudio);
				IsPressing = true;
			}
			if (IsPressing) {
				PlayerJson.JsonHelper.Player p = playerList[yourName.playerId];
				if (isPressing(false)) {
					playMusic(pressingAudio);
					Timer += Time.deltaTime;
					if (Timer < 4) {
						p.power = Timer * 3;
						// currPlatform 可能返回空，因为当前木块可能为空
						if (currPlatform(yourName.playerId) != null) {
							currPlatform(yourName.playerId).transform.localScale = new Vector3 (1, 1 - 0.2f * Timer, 1);
							currPlatform(yourName.playerId).transform.Translate (0, -0.1f * Time.fixedDeltaTime, 0);
						}
						p.player.transform.Translate(0, -0.2f * Time.deltaTime, 0);
					}
				} else {
					stopMusic(pressingAudio);
					IsPressing = false;
					gameStatus = GameStatus.REBOUND;
					if (currPlatform(yourName.playerId) != null)
						Scale = currPlatform(yourName.playerId).transform.localScale.y;
				}
				playerList[yourName.playerId] = p;
			}
			break;

		case GameStatus.REBOUND:
			PlayerJson.JsonHelper.Player f = playerList[yourName.playerId];
			Timer += Time.deltaTime;
			if (currPlatform(yourName.playerId) != null) {
				currPlatform(yourName.playerId).transform.localScale = Vector3.Lerp (new Vector3 (1, Scale, 1), new Vector3 (1, 1, 1), Timer);
				currPlatform(yourName.playerId).transform.Translate (0, 0.5f * Time.deltaTime, 0);
			}
			f.player.transform.Translate (0, 1.0f * Time.deltaTime, 0);

			if (currPlatform(yourName.playerId) == null || currPlatform(yourName.playerId).transform.position.y >= 0.5) {
				gameStatus = GameStatus.PLAYER_JUMPING;
				f.VSpeed = 0.3f;
				f.prePlayerPosition = f.player.transform.position;
				playerList[yourName.playerId] = f;
				// 记录动画
				StartCoroutine(HttpHelper.HttpHelper.syncPlayerStatus(
					PlayerJson.JsonHelper.animJson("jump", playerList, yourName.playerId), 
					yourName.roomId, f.playerId));
			}
			playerList[yourName.playerId] = f;
			break;

		case GameStatus.PLAYER_JUMPING:
			playerJump(yourName.playerId);
			break;

		case GameStatus.GAME_OVER:
			stopMusic(pressingAudio);
			print("game_over");
			Timer += Time.deltaTime;
			if (Timer >= 1) {
				Timer = 0;
				gameStatus = GameStatus.PLAY_AGAIN;
			}
			timeText.text = "再接再厉";
			break;

		case GameStatus.PLAY_AGAIN:
			stopMusic(pressingAudio);
			playAgainFunc();
			break;
		case GameStatus.GAME_WIN:
			stopMusic(pressingAudio);
			print("chijichijichijichijichiji");
			timeText.text = "吃鸡";
			if (!syncGameWinFlag) {
				syncGameWinFlag = true;
				StartCoroutine(HttpHelper.HttpHelper.syncPlayerStatus(
					PlayerJson.JsonHelper.animJson("game_win", playerList, yourName.playerId), yourName.roomId, playerList[yourName.playerId].playerId, 
					delegate(string s) { playAgainFunc(); }
				));
			}
			break;
		default:
			break;
		}
	}

	bool logFlag = false;
	bool syncGameWinFlag = false;

	public void checkGameOver(string ret) {
		if (ret == "true") {
			// game winner
			gameStatus = GameStatus.GAME_WIN;
		} else {
			// game over
			gameStatus = GameStatus.GAME_OVER;
			playerList[yourName.playerId].playerPos.AddComponent<Rigidbody>();
		}
	}

	// Update is called once per frame
	void Update() {
		try {
			updateFunc();
		} catch (System.Exception e) {
			if (!logFlag) {
				logOnScreen(e.ToString());
				logFlag = true;
			}
		}
	}
	
}
