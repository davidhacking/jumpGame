using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
public class MasterSceneManager : MonoBehaviour {

	public bool mainPause = false;

	public static Stack previouslevel;
	public static MasterSceneManager Instance;
	private void Awake() {
		print("MasterSceneManager awake");
		DontDestroyOnLoad( this );
		Instance = this;
		previouslevel = new Stack( );
		print("MasterSceneManager init");
		mainPause = false;
	}

	public void pauseMain() {
    	SceneManager.LoadScene("menu", LoadSceneMode.Additive);
    	while (previouslevel.Count > 0) {
    		previouslevel.Pop();
    	}
    	Instance.mainPause = true;
	}

	public void resumeMain() {
		print("resumeMain");

		for (int i = 0; i < SceneManager.sceneCount; i++) {
			print("scene queue: " + SceneManager.GetSceneAt(i).name);
		}
		
	    Instance.mainPause = false;
	    string name = SceneManager.sceneCount > 0 ? SceneManager.GetSceneAt(SceneManager.sceneCount - 1).name : "";
	    // 从日志看，如果SceneManager中只有一个scene时，时是不能被unload的
	    int flag = 0;
	    int maxPop = 4;
        while (SceneManager.sceneCount > 0 && name.Equals("menu")) {
    		SceneManager.UnloadScene(name);
        	name = SceneManager.sceneCount > 0 ? SceneManager.GetSceneAt(SceneManager.sceneCount - 1).name : "";
        	if (flag++ > maxPop) {
        		break;
        	}
        }
	    previouslevel.Push("menu");
	    
	}

	public void loadScrollView() {
		if (!Instance.mainPause) {
			MasterSceneManager.Instance.LoadNext("scrollView");
		} else {
			SceneManager.LoadScene("scrollView", LoadSceneMode.Additive);
		}
	}

	public void backInScrollView() {
		if (!Instance.mainPause) {
			MasterSceneManager.Instance.LoadPrevious();
		} else {
			string name = SceneManager.sceneCount > 0 ? SceneManager.GetSceneAt(SceneManager.sceneCount - 1).name : "";
		    // 从日志看，如果SceneManager中只有一个scene时，时是不能被unload的
		    int flag = 0;
		    int maxPop = 4;
	        while (SceneManager.sceneCount > 0 && name.Equals("scrollView")) {
	    		SceneManager.UnloadScene(name);
	        	name = SceneManager.sceneCount > 0 ? SceneManager.GetSceneAt(SceneManager.sceneCount - 1).name : "";
	        	if (flag++ > maxPop) {
	        		break;
	        	}
	        }
		}
	}

	public void LoadPrevious() {
		print("LoadPrevious");
		//just pop last scene
		if ( previouslevel.Count > 0 )
			SceneManager.LoadScene( previouslevel.Pop( ).ToString( ) );
		else
			Debug.Log( "no previous scene in the stack" );
	}
	public void LoadNext(string scene) {
		print("LoadNext: " + scene);
		if ( SceneManager.GetActiveScene( ).name != "loading"  ||  SceneManager.GetActiveScene( ).name != "loses" ) {
			previouslevel.Push( SceneManager.GetActiveScene( ).name );
		} else
			Debug.Log( "scene restart is not added to stack " );

		SceneManager.LoadScene( scene );

		/*while (scene == "main" && SceneManager.sceneCount > 0 && SceneManager.GetSceneAt(SceneManager.sceneCount - 1).name == "menu") {
        	SceneManager.UnloadScene("menu");
        }*/

	}
         
}