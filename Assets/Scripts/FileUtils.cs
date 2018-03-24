using UnityEngine;  
using System.Collections;
using System.IO;
using System;  
  
public class FileUtils : MonoBehaviour {

	private static readonly string filePathSep = "/";

	ArrayList lines;
	void Start() {
		print("file store path: " + Application.persistentDataPath);
		deleteFile("test.txt");
		writeFile("test.txt", "test data");
		lines = readFileToLines("test.txt");
		foreach (string line in lines) {
			print("FileUtils.readdata: " + line);
		}
	}
	
	public void writeFile(string path, string data) {
		StreamWriter sw = null;
		FileInfo t = new FileInfo(Application.persistentDataPath + filePathSep + path);
		if(!t.Exists) {  
			sw = t.CreateText();
		} else {
			sw = t.AppendText();
		}
		sw.WriteLine(data);
		sw.Close();
		sw.Dispose();
	}

	public ArrayList readFileToLines(string path) {
		StreamReader sr = null;
		try {
			sr = File.OpenText(Application.persistentDataPath + filePathSep + path);
		} catch (Exception e) {
			print("read file error, file path: " + path);
			return null;    
		}    
		string line;
		ArrayList arrlist = new ArrayList();
		while ((line = sr.ReadLine()) != null) { 
			arrlist.Add(line);    
		}
		sr.Close();
		sr.Dispose();
		return arrlist;
	}

	public void deleteFile(string path) {    
		File.Delete(Application.persistentDataPath + filePathSep + path);    
	}
}  