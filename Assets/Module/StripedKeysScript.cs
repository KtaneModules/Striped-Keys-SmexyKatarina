using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class StripedKeysScript : MonoBehaviour {

	public KMBombModule _module;
	public KMBombInfo _bomb;
	public KMAudio _audio;

	public KMSelectable[] _keys;

	public MeshRenderer[] _keyRenderers;
	public Material[] _colors;


	// Specifically for Logging
	static int _modIDCount = 1;
	int _modID;
	private bool _modSolved;

	int[][] _chosenKeys = new int[][]
	{
		new int[] { -1, -1 },
		new int[] { -1, -1 },
		new int[] { -1, -1 }
	};

	string[][] _colorRows = new string[][]
	{
		new string[] { "Red", "Yellow", "Magenta" },
		new string[] { "Blue", "Magenta", "Green" },
		new string[] { "Blue", "Red", "Yellow" },
		new string[] { "Magenta", "Blue", "Red" },
		new string[] { "Yellow", "Red", "Green" },
		new string[] { "Green", "Red", "Blue" },
		new string[] { "Magenta", "Green", "Red" },
		new string[] { "Blue", "Yellow", "Red" },
		new string[] { "Magenta", "Green", "Yellow" },
		new string[] { "Red", "Magenta", "Blue" },
		new string[] { "Yellow", "Green", "Red" },
		new string[] { "Green", "Blue", "Magenta" },
		new string[] { "Yellow", "Magenta", "Green" },
		new string[] { "Magenta", "Yellow", "Blue" },
		new string[] { "Red", "Blue", "Yellow" },
		new string[] { "Yellow", "Green", "Blue" },
		new string[] { "Red", "Green", "Magenta" },
		new string[] { "Blue", "Yellow", "Magenta" },
	};

	string[] _chosenRow;
	int _chosenRowIndex;

	List<int> _keysToPress = new List<int>();
	int _currentPressIndex = 0;

	void Awake() {
		_modID = _modIDCount++;

		foreach (KMSelectable km in _keys) 
		{
			km.OnInteract = delegate () { if (_modSolved) return false; HandleKeyPress(km); return false; };
		}

	}

	void Start() {
		List<int> check = new List<int>();
		while (check.Distinct().Count() < 4) 
		{
			check.Clear();
			_chosenKeys = new int[][]
			{
				new int[] { -1, -1 },
				new int[] { -1, -1 },
				new int[] { -1, -1 }
			};
			for (int i = 0; i < 3; i++)
			{
				int x = rnd.Range(0, _colors.Length);
				int y = rnd.Range(0, _colors.Length);
				while (y == x) y = rnd.Range(0, _colors.Length);
				_chosenKeys[i][0] = x;
				_chosenKeys[i][1] = y;
				check.Add(x);
				check.Add(y);
				_keyRenderers[i].materials = new Material[] { _colors[x], _colors[y] };
			}
			if (CheckingForTooManyDupes(check)) { check.Clear(); continue; }
		}
		Debug.LogFormat("[Striped Keys #{0}]: The key colors from left to right are: {1}.", _modID, _chosenKeys.Select(x => x.Select(y => _colors[y].name).Join(", ")).Join(" | "));
	
		for (int i = 0; i < _colorRows.Length; i++) 
		{
			string c1 = _colorRows[i][0];
			string c2 = _colorRows[i][1];
			string c3 = _colorRows[i][2];
			if (_chosenKeys[0].Select(x => _colors[x].name).Any(x => x == c1)
				&& _chosenKeys[1].Select(x => _colors[x].name).Any(x => x == c2)
				&& _chosenKeys[2].Select(x => _colors[x].name).Any(x => x == c3)) 
			{
				_chosenRow = _colorRows[(i + 1) % _colorRows.Length];
				_chosenRowIndex = (i + 1) % _colorRows.Length;
				break;
			}
		}

		if (_chosenRowIndex - 1 == -1) 
		{
			_chosenRow = _colorRows[1];
			_chosenRowIndex = 1;
			Debug.LogFormat("[Striped Keys #{0}]: No row exists with this set of keys, using the first row as the 'first true row'. Meaning that row 2 will be for submission.", _modID);
		}

		Debug.LogFormat("[Striped Keys #{0}]: The row that matches the keys is {1} (Row {2}) and the row that is used to solve it will be {3} (Row {4}).", _modID, _colorRows[_chosenRowIndex - 1].Join(", "), _chosenRowIndex, _chosenRow.Join(", "), (_chosenRowIndex + 1) % _colorRows.Length);

		int[] colorNums = _chosenRow.Select(x => Array.IndexOf(_colors.Select(y => y.name).ToArray(), x)).ToArray();
		List<int> keyNums = CombinedIntArray(_chosenKeys);

		foreach (int i in colorNums) 
		{
			for (int x = 0; x < 6; x++) 
			{
				if (keyNums[x] == i) 
				{
					switch (x) 
					{
						case 0:
						case 1:
							_keysToPress.Add(0);
							break;
						case 2:
						case 3:
							_keysToPress.Add(1);
							break;
						default:
							_keysToPress.Add(2);
							break;
					}
				}
			}
		}
		Debug.LogFormat("[Striped Keys #{0}]: The keys to be pressed, in order, are: {1}.", _modID, _keysToPress.Select(x => x + 1).Join(", "));

	}

	void HandleKeyPress(KMSelectable key) 
	{
		int index = Array.IndexOf(_keys, key);
		_audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, key.transform);
		if (_keysToPress[_currentPressIndex] != index)
		{
			_module.HandleStrike();
			Debug.LogFormat("[Striped Keys #{0}]: Incorrect key pressed. Expected {1} but was given {2}.", _modID, _keysToPress[_currentPressIndex] + 1, index + 1);
			return;
		}
		else
		{
			_currentPressIndex++;
			if (_currentPressIndex >= _keysToPress.Count()) 
			{
				_module.HandlePass();
				_modSolved = true;
				Debug.LogFormat("[Striped Keys #{0}]: All keys have been pressed. Module Solved.", _modID);
				return;
			}
			return;
		}

	}

	bool CheckingForTooManyDupes(List<int> check) 
	{
		for (int i = 0; i < 5; i++) 
		{
			int count = 0;
			foreach (int x in check) 
			{
				if (i == x) count++;
			}
			if (count > 2) 
				return true;
		}
		return false;
	}

	List<int> CombinedIntArray(int[][] aa) 
	{
		List<int> l = new List<int>();
		foreach (int[] a in aa) 
		{
			l.Add(a[0]);
			l.Add(a[1]);
		}
		return l;
	}

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} 123 [Presses the keys in order as they were given without spaces]";
#pragma warning restore 414

	IEnumerator ProcessTwitchCommand(string command) 
	{
		string[] args = command.ToLower().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
		if (args.Length > 1) 
		{
			yield return "sendtochaterror Incorrect number of arguments for this command, please try again.";
			yield break;
		}
		if (args[0].Any(x => !x.EqualsAny('1', '2', '3'))) 
		{
			yield return "sendtochaterror Incorrect submit format, please try again.";
			yield break;
		}
		yield return "solve";
		foreach (char c in args[0]) 
		{
			yield return null;
			int i = int.Parse(c.ToString());
			_keys[i-1].OnInteract();
			yield return new WaitForSeconds(0.25f);
		}
	}

	IEnumerator TwitchHandleForcedSolve() 
	{
		yield return null;
		foreach (int key in _keysToPress) 
		{
			_keys[key].OnInteract();
			yield return new WaitForSeconds(0.1f);
		}
		yield break;
	}

}
