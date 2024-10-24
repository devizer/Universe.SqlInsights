namespace ErgoFab.DataAccess.IntegrationTests.Library;

internal static class TryAndForget
{
    public static T Evaluate<T>(Func<T> factory)
    {
        try
        {
            return factory();
        }
        catch
        {
            return default;
        }
    }

    public static bool Execute(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch
        {
            return false;
        }
    }
}