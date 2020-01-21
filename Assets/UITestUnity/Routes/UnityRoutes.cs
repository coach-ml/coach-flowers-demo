using Xamarin.GameTestServer.Unity;

public static class UnityRoutes
{
	public static void Enable ()
    {
        DeviceInfoRoute.Enable();
        CurrentScreenRoute.Enable();
        GameObjectFind.Enable();
        InvokeButtonRoute.Enable();
        InvokeInputRoute.Enable();
    }
}