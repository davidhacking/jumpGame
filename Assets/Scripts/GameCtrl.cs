using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using HttpHelper;
using PlayerJson;
using MqHelper;

public class GameCtrl : MonoBehaviour
{

	private static readonly string CONF_FILE = "config";
	//private static readonly string WEB_SERVER_URL = "http://192.168.45.130:5000";
	private static readonly string WEB_SERVER_URL = "http://127.0.0.1:5000";
	/*
	-68 205
	527 -57
	-80 100
	590 -150
	-80 45
	590 -210
	-80 -10
	590	-270
	*/
	public float Speed;
	public GameObject Platform;
	public GameObject playerAsset;
	public GameObject playAgainBtn;
	public GameObject quitGameBtn;
	public GameObject historyBtn;
	public Text scoretext;
	public Rigidbody Rig;
	public AudioSource pressingAudio;
	public AudioSource deadAudio;
	public AudioSource bonusAudio;
	public AudioSource stepAudio;
	public AudioSource gameOverAudio;

	public Text firstScore;
	public Text firstName;
	public Text secondScore;
	public Text secondName;
	public Text thirdScore;
	public Text thirdName;

	public FileUtils fileUtils;

	bool gameSyncFlag;

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
	public Object platformLock;
	public List<string> platformQueue;
	CameraCtrl cameraCtrl;
	int score;
	bool sentScore;
	bool isShowRecord = false;
	int animationScore = 0;
	float playAgainTime = 1.5f;
	float playDeltaTime = 0;

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
			playerList[id] = p;
		}
	}

	void displayPlayerLabel(string index, string text) {
		setEnable(index);
		playerList[index].playerLabel.transform.position = playerList[index].player.transform.position + new Vector3(0, playerList[index].player.transform.position.y + 0.4f * playerList[index].index, 0);
		playerList[index].playerLabelTextMesh.text = text;
	}

	void setDisable(string index) {
		playerList[index].playerLabel.SetActive(false);
	}

	void setEnable(string index) {
		playerList[index].playerLabel.SetActive(true);
	}

	GameObject nextPlatform(string index) {
		return platforms[playerList[index].currentPlatformIndex + 1];
	}

	GameObject currPlatform(string index) {
		return platforms[playerList[index].currentPlatformIndex];
	}

	void displayPlayerInNextPlatform(string index) {
		playerList[index].player.transform.position = new Vector3(playerList[index].player.transform.position.x, 1.25f, playerList[index].player.transform.position.z);
		playerList[index].playerPos.transform.rotation = Quaternion.Euler(0, 0, 0);
	}
	bool playerJump(string index) {
		setDisable(index);
		PlayerJson.JsonHelper.Player p = playerList[index];
		p.VSpeed -= Time.deltaTime;
		print("index: " + index + "; playerList[index].direction: " + p.direction);
		if (p.direction) {
			//playerList[index].player.transform.Translate(new Vector3((nextPlatform(index).transform.position.x - playerList[index].prePlayerPosition.x) / 0.6f * Time.deltaTime, playerList[index].VSpeed / 2, playerList[index].power / 0.6f * Time.deltaTime));
			p.player.transform.Translate(new Vector3(0, p.VSpeed / 2, 
				p.power / 0.6f * Time.deltaTime));
			p.playerPos.transform.Rotate(new Vector3(720 * Time.deltaTime, 0));
		} else {
			//playerList[index].player.transform.Translate(new Vector3(-playerList[index].power / 0.6f * Time.deltaTime, playerList[index].VSpeed / 2, (nextPlatform(index).transform.position.z - playerList[index].prePlayerPosition.z) / 0.6f * Time.deltaTime));
			p.player.transform.Translate(new Vector3(-p.power / 0.6f * Time.deltaTime, 
				p.VSpeed / 2, 0));
			p.playerPos.transform.Rotate(new Vector3(0, 0, 720 * Time.deltaTime));
		}
		if (p.player.transform.position.y <= 1) {
			displayPlayerInNextPlatform(index);
			if (Mathf.Abs(p.player.transform.position.x - currPlatform(index).transform.position.x) < 0.5 && 
				Mathf.Abs (p.player.transform.position.z - currPlatform(index).transform.position.z) < 0.5) {
				gameStatus = GameStatus.TAPING;
			} else {
				if (Mathf.Abs(p.player.transform.position.x - nextPlatform(index).transform.position.x) > 0.5 || 
					Mathf.Abs (p.player.transform.position.z - nextPlatform(index).transform.position.z) > 0.5) {
					if (index == yourName.playerId) {
						playMusic(deadAudio);
						gameStatus = GameStatus.GAME_OVER;
						Timer = 0;
					}
					// for test
					//playerList[index].animationQueue.Insert(playerList[index].animationQueue.Count, "game_over");
				} else {
					if (index == yourName.playerId) {
						if (Mathf.Abs(p.player.transform.position.x - nextPlatform(index).transform.position.x) < 0.2 && 
							Mathf.Abs(p.player.transform.position.z - nextPlatform(index).transform.position.z) < 0.2) {
							playMusic(bonusAudio);
							Bonus++;
							score += Bonus * 2 + extraBonus;
							// white yellow purple
							if (extraBonus < 2)
								extraBonus++;
							printScore();
						} else {
							playMusic(stepAudio);
							Bonus = 0;
							score = score + 1 + extraBonus;
							extraBonus = 0;
							printScore();
						}
						gameStatus = GameStatus.CREATE_PLATFORM;
					}
					// 当前platform+1
					p.currentPlatformIndex++;
				}
			}
			playerList[index] = p;
			displayPlayerLabel(index, "jump");
			return true;
		}
		playerList[index] = p;
		return false;
	}

	public void syncPos(string index, ref LitJson.JsonData animJson) {
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
		playerList[index] = p;
	}

	public void syncAnimation() {
		// other player status
		int t = playerList[yourName.playerId].currentPlatformIndex;
		List<string> keys = new List<string>(playerList.Keys);
		foreach (string key in keys) {
			if (key == yourName.playerId) {
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
							syncPos(key, ref animJson);
							p.syncPosFlag = true;
						}
						bool endFlag = playerJump(key);
						if (endFlag) {
							p.syncPosFlag = false;
							p.animationQueue.RemoveAt(0);
						}
					} else if (animJson["action"].ToString() == "game_over") { // game over animation
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
	}

    public void onBackBtnClick() {
    	stopMusic(pressingAudio);
		if (gameStatus == GameStatus.PLAY_AGAIN) {
			print("game ctrl click back LoadPrevious");
        	MasterSceneManager.Instance.LoadPrevious();
        } else {
        	print("game ctrl click back pauseMain");
            MasterSceneManager.Instance.pauseMain();
        }
	}

	public void setRecondDisable() {
		firstScore.gameObject.SetActive(false);
		firstName.gameObject.SetActive(false);
		secondScore.gameObject.SetActive(false);
		secondName.gameObject.SetActive(false);
		thirdScore.gameObject.SetActive(false);
		thirdName.gameObject.SetActive(false);
	}

	public void setRecord(Text textScore, Text textName, string scoreStr, string nameStr) {
		if (!isShowRecord) {
			return;
		}
		textScore.text = scoreStr;
		textName.text = nameStr;
		textScore.gameObject.SetActive(true);
		textName.gameObject.SetActive(true);
	}

	public void playAgain() {
		playAgainBtn.SetActive(false);
		quitGameBtn.SetActive(false);
		historyBtn.SetActive(false);
		gameStatus = GameStatus.INIT;
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

	void printScore() {
		scoretext.text = score.ToString();
	}

	void printScoreAnimation(float t) {
		playDeltaTime += t;
		if (animationScore < score && playDeltaTime > (playAgainTime / score)) {
			playDeltaTime = 0;
			scoretext.text = animationScore.ToString();
			animationScore++;
		}
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

	public IEnumerator topNScore(int n) {
    	// validate
        UnityWebRequest www = UnityWebRequest.Get(WEB_SERVER_URL + "/topNScore?n=" + n);
		yield return www.Send();
		if (www.isError) {
            Debug.Log(www.error);
        } else {
        	print(www.downloadHandler.text);
        	if (!string.IsNullOrEmpty(www.downloadHandler.text)) {
        		string[] data = Regex.Split(www.downloadHandler.text, "\n", RegexOptions.IgnoreCase);
        		for (int i = 0; i < data.Length; i++) {
        			print(data[i]);
        			string[] ss = Regex.Split(data[i], ",", RegexOptions.IgnoreCase);
        			if (ss.Length != 2) {
        				break;
        			}
        			if (i == 0) {
        				setRecord(firstScore, firstName, ss[1], ss[0]);
        			} else if (i == 1) {
						setRecord(secondScore, secondName, ss[1], ss[0]);
        			} else if (i == 2) {
						setRecord(thirdScore, thirdName, ss[1], ss[0]);
        			}
        		}
        	}
        }
    }

	public IEnumerator recordScore(int score) {
    	// validate
        UnityWebRequest www = UnityWebRequest.Get(WEB_SERVER_URL + "/winGame?name=" + playerName + "&score=" + score);
        isShowRecord = true;
		yield return www.Send();
		if (www.isError) {
            Debug.Log(www.error);
        } else {
        	print(www.downloadHandler.text);
        	StartCoroutine(topNScore(3));
        }
    }

    public void updatePlayer(string roomId, string playerId, string ret) {
    	if (ret != "{}") {
    		Debug.Log("updatePlayer ret: " + ret);
			lock (playerList[playerId].queueLock) {
				playerList[playerId].animationQueue.Insert(playerList[playerId].animationQueue.Count, ret);
			}
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
		print("updatePlatform ret: " + ret);
		if (x["return"].ToString() != "success") {
			print("updatePlatform failed");
		}
		lock (platformLock) {
			for (int i = platformQueue.Count; i < x["data"].Count; i++) {
				platformQueue.Insert(platformQueue.Count, x["data"][i].ToString());
			}
		}
        
        System.Action<string> callback = updatePlatform;
        StartCoroutine(HttpHelper.HttpHelper.updatePlatformStatus(yourName.roomId, callback));
    }

	// Use this for initialization
	void Start() {
		ArrayList lines = fileUtils.readFileToLines(CONF_FILE);
    	if (lines == null) {
    		//Application.LoadLevel("login");
    		MasterSceneManager.Instance.LoadNext("login");
    	} else {
    		playerName = (string) lines[0];
    	}

		playAgainBtn.SetActive(false);
		quitGameBtn.SetActive(false);
		historyBtn.SetActive(false);
		cameraCtrl = GetComponent<CameraCtrl>();
		platforms = new List<GameObject>();
		scoretext = scoretext.GetComponent<Text>();
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
        StartCoroutine(HttpHelper.HttpHelper.updatePlatformStatus(roomId, callback2));
	}

	private GameObject playerLabel;
	private TextMesh playerLabelTextMesh;

	void initGame() {
		isShowRecord = false;
		gameSyncFlag = false;
		setRecondDisable();
		extraBonus = 0;
		score = 0;
		sentScore = false;
		animationScore = 0;
		playDeltaTime = 0;
		Bonus = 0;
		printScore();
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

	void createPlatform() {
		PlayerJson.JsonHelper.Player p = playerList[yourName.playerId];
		bool f = nextDirection();
		p.direction = f;
		platforms.Insert(platforms.Count, 
			Instantiate(Platform, 
				currPlatform(yourName.playerId).transform.position + randomDelta(p.direction), 
				Quaternion.Euler (Vector3.zero)));
		// render color
		if (extraBonus > 0) {
			Material materialColored = new Material(Shader.Find("Diffuse"));
	        materialColored.color = extraBonus == 1 ? Color.green : Color.yellow;
	        nextPlatform(yourName.playerId).GetComponent<Renderer>().material = materialColored;
	    }

		Point1 = nextPlatform(yourName.playerId).transform.position;
		Point2 = Point1;
		Point2.y = 0.5f;

        //重新设置相机的位置
		cameraCtrl.SetPosition((currPlatform(yourName.playerId).transform.position + Point2) / 2);
		nextPlatform(yourName.playerId).transform.position = Point2;
		Timer = 0;
		p.power = 0;
		gameSyncFlag = false;
		playerList[yourName.playerId] = p;
	}

	void playAgainFunc() {
		if (!sentScore) {
			StartCoroutine(recordScore(score));
			sentScore = true;
		}

		float t = Time.deltaTime;
		Timer += t;
		if (Timer < playAgainTime) playMusic(gameOverAudio);
		else stopMusic(gameOverAudio);
		// score animation
		if (Timer < playAgainTime) printScoreAnimation(t);
		else printScore();
		playAgainBtn.SetActive(true);
		quitGameBtn.SetActive(true);
		historyBtn.SetActive(true);
	}

	public void gotoHistory() {
		//Application.LoadLevel("scrollView");
		MasterSceneManager.Instance.LoadNext("scrollView");
    	print("scrollView");
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
	}

	// Update is called once per frame
	void Update() {

		updateLoginTime += Time.deltaTime;
        if (updateLoginTime > 2) {
        	updateLoginTime = 0;
        	// 匹配房间
        	System.Action<string> callback = unfriendlyCSharpFunction;
			StartCoroutine(HttpHelper.HttpHelper.canStart(yourName.roomId, yourName.playerId, callback));
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
            createPlatform();
            gameStatus = GameStatus.TAPING;
            // if playerList[yourName.playerId].currentPlatformIndex + 1 <= platformQueue.Count - 1
            // gameStatus = GameStatus.TAPING;
			break;
		/*
		case GameStatus.SHOW_PLATFORM:
                
			Timer += Time.deltaTime;
			nextPlatform(yourName.playerId).transform.position = Vector3.Lerp(Point1, Point2, Timer * Speed);

			if (Timer * Speed > 1) {
				gameStatus = GameStatus.TAPING;
				Timer = 0;
			}
			break;
		*/
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
						// for test 
						//playerList[1].power = Timer * 3;
						currPlatform(yourName.playerId).transform.localScale = new Vector3 (1, 1 - 0.2f * Timer, 1);
						currPlatform(yourName.playerId).transform.Translate (0, -0.1f * Time.deltaTime, 0);
						p.player.transform.Translate (0, -0.2f * Time.deltaTime, 0);
					}
				} else {
					stopMusic(pressingAudio);
					IsPressing = false;
					gameStatus = GameStatus.REBOUND;
					Scale = currPlatform(yourName.playerId).transform.localScale.y;
				}
				playerList[yourName.playerId] = p;
			}
			break;

		case GameStatus.REBOUND:
			PlayerJson.JsonHelper.Player f = playerList[yourName.playerId];
			Timer += Time.deltaTime;
			currPlatform(yourName.playerId).transform.localScale = Vector3.Lerp (new Vector3 (1, Scale, 1), new Vector3 (1, 1, 1), Timer);
			currPlatform(yourName.playerId).transform.Translate (0, 0.5f * Time.deltaTime, 0);
			f.player.transform.Translate (0, 1.0f * Time.deltaTime, 0);

			if (currPlatform(yourName.playerId).transform.position.y >= 0.5) {
				gameStatus = GameStatus.PLAYER_JUMPING;
				f.VSpeed = 0.3f;
				f.prePlayerPosition = f.player.transform.position;
				playerList[yourName.playerId] = f;
				// 记录动画
				StartCoroutine(HttpHelper.HttpHelper.syncPlayerStatus(PlayerJson.JsonHelper.animJson("jump", playerList, yourName.playerId), yourName.roomId, f.playerId));
			}
			playerList[yourName.playerId] = f;
			break;

		case GameStatus.PLAYER_JUMPING:
			playerJump(yourName.playerId);
			break;

		case GameStatus.GAME_OVER:
			if (!gameSyncFlag) {
				gameSyncFlag = true;
				StartCoroutine(HttpHelper.HttpHelper.syncPlayerStatus(PlayerJson.JsonHelper.animJson("game_over", playerList, yourName.playerId), yourName.roomId, playerList[yourName.playerId].playerId));
			}
			if (Timer == 0) {
				Rig = playerList[yourName.playerId].playerPos.AddComponent<Rigidbody> ();
			}
			Timer += Time.deltaTime;
			if (Timer >= 1) {
				Timer = 0;
				gameStatus = GameStatus.PLAY_AGAIN;
			}
			break;

		case GameStatus.PLAY_AGAIN:
			playAgainFunc();
			break;
		default:
			break;
		}

	}
	
}
