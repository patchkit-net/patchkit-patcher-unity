using System.Threading.Tasks;

public partial class Patcher
{
    private async Task<bool> StartAppAndQuitAsync()
    {
        if (!await StartAppAsync())
        {
            return false;
        }

        return await QuitAsync();
    }
}