using System.Collections.Generic;

namespace JyGame
{
	public class ModManager
	{
		public static List<ModInfo> mods;

		public static string ModBaseUrl
		{
			get
			{
				return "file:///" + CommonSettings.persistentDataPath + "/modcache/" + GlobalData.CurrentMod.key + "/";
			}
		}

		public static string ModBaseUrlPath
		{
			get
			{
				return CommonSettings.persistentDataPath + "/modcache/" + GlobalData.CurrentMod.key + "/";
			}
		}

		public static void SetCurrentMod(ModInfo mod)
		{
			ResourceManager.ResetInitFlag();
			AssetBundleManager.IsInited = false;
			GlobalData.CurrentMod = mod;
			ModData.Load();
			LuaManager.Init(true);
		}
	}
}
