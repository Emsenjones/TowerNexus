using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageWindowBehaviour : WindowBehaviour
{
    [SerializeField] TextMeshProUGUI contentText;
    [SerializeField] Button closeButton;
    public class Data
    {
        public readonly string ContentString;
        event Action OnClose;
        public Data(string contentString, Action onClose)
        {
            ContentString = contentString;
            OnClose += onClose;
        }
        public void InvokeOnClose() => OnClose?.Invoke();
    }
    public override void Initialize(object data = null)
    {
        if (data is Data messageWindowData)
        {
            contentText.text = messageWindowData.ContentString;
            closeButton.onClick.AddListener(() =>
            {
                messageWindowData.InvokeOnClose();
                Close();
            });
        }
    }
    void OnDisable()
    {
        closeButton.onClick.RemoveAllListeners();
    }

}
