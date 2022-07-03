using System;
using TMPro;
using UnityEngine;

public enum CameraAngle
{
    menu = 0,
    white = 1,
    black = 2
}

public class UI : MonoBehaviour
{
    public static UI Instance {set; get;}

    public Server server;
    public Client client;

    [SerializeField] private Animator menuAnimator;
    [SerializeField] private TMP_InputField addressInput;

    public Action<bool> setOffline;

    public void Awake()
    {
        Instance = this;
        RegisterEvents();
    }

    public void ChangeCamera(CameraAngle angle)
    {
        switch (angle)
        {
            case CameraAngle.menu: Camera.main.transform.Translate(0, 0, -10); break;
            case CameraAngle.white: Camera.main.transform.Translate(0, 1500, -10); break;
            case CameraAngle.black:
                Camera.main.transform.Translate(0, 1500, -10);
                Camera.main.transform.Rotate(0, 0, 180);
                break;
        }
    }

    // Handles Buttons
    public void Offline()
    {
        menuAnimator.SetTrigger("Game");
        setOffline?.Invoke(true);
        server.Init(8007);
        client.Init("127.0.0.1", 8007);
    }
    public void Online()
    {
        menuAnimator.SetTrigger("OnlineMenu");
    }
    public void OnlineHost()
    {
        setOffline?.Invoke(false);
        server.Init(8007);
        client.Init("127.0.0.1", 8007);
        menuAnimator.SetTrigger("HostMenu");
    }
    public void OnlineJoin()
    {
        setOffline?.Invoke(false);
        client.Init(addressInput.text, 8007);
    }
    public void OnlineBack()
    {
        menuAnimator.SetTrigger("StartMenu");
    }
    public void HostBack()
    {
        server.ShutDown();
        client.ShutDown();
        menuAnimator.SetTrigger("OnlineMenu");
    }

    public void ExitMenu()
    {
        ChangeCamera(CameraAngle.menu);
        menuAnimator.SetTrigger("StartMenu");
    }

    private void RegisterEvents()
    {
        NetUtility.C_START_GAME += StartGameClient;
    }
    private void UnRegisterEvents()
    {
        NetUtility.C_START_GAME -= StartGameClient;
    }

    private void StartGameClient(NetMessage msg)
    {
        menuAnimator.SetTrigger("Game");
    }
}
