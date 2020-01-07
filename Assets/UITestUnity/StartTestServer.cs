using UnityEngine;
using Xamarin.GameTestServer;

public class StartTestServer : MonoBehaviour
{
    void Start()
    {
        if (Debug.isDebugBuild) {
            GameServer.Shared.Start();
        }
	}
}
