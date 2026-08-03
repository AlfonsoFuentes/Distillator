namespace Client.Pages.UnitOperations.HeatExchangers.Design;

public sealed class DesignUnitPopupContext
{
    public string? OpenKey { get; private set; }

    public event Action? Changed;

    public void Toggle(string key)
    {
        OpenKey = OpenKey == key ? null : key;
        Changed?.Invoke();
    }

    public void Close(string key)
    {
        if (OpenKey != key)
        {
            return;
        }

        OpenKey = null;
        Changed?.Invoke();
    }
}
