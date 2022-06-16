
public class GlobalRenderSetting
{
    private static GlobalRenderSetting _setting;
    public static GlobalRenderSetting Setting
    {
        get
        {
            if (_setting == null)
                _setting = new GlobalRenderSetting();
            return _setting;
        }
    }

    public bool useDepthPriming = false;
    
}
