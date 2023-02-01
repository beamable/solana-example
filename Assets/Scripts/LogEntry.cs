using TMPro;
using UnityEngine;

public class LogEntry : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI _text;

	public void Setup(string value)
	{
		_text.text = value;
	}
}