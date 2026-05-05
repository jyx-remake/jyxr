using System.Collections;
using System.Collections.Generic;
using XLua;


namespace JyGame
{
	public static class LuaTool
	{
		public static CommonSettings.VoidCallBack MakeVoidCallBack(LuaFunction fun)
		{
			return delegate
			{
				fun.Call();
			};
		}

		public static CommonSettings.StringCallBack MakeStringCallBack(LuaFunction fun)
		{
			return delegate(string rst)
			{
				fun.Call(new object[1] { rst }, null);
			};
		}

		public static CommonSettings.IntCallBack MakeIntCallBack(LuaFunction fun)
		{
			return delegate(int rst)
			{
				fun.Call(new object[1] { rst }, null);
			};
		}

		public static CommonSettings.ObjectCallBack MakeObjectCallBack(LuaFunction fun)
		{
			return delegate(object rst)
			{
				fun.Call(new object[1] { rst }, null);
			};
		}

		public static string[] MakeStringArray(LuaTable table)
		{
			List<string> list = new List<string>();
			table.ForEach(delegate(object key, object value)
			{
				list.Add(value.ToString());
			});

			return list.ToArray();
		}

		public static LuaTable CreateLuaTable()
		{
			return (LuaTable)LuaManager._lua.DoString("return {}")[0];
		}

		public static LuaTable CreateLuaTable(IEnumerable objs)
		{
			LuaTable luaTable = CreateLuaTable();
			int num = 0;
			foreach (object obj in objs)
			{
				luaTable[num.ToString()] = obj;
				num++;
			}
			return luaTable;
		}

		public static LuaTable CreateLuaTable(IList objs)
		{
			LuaTable luaTable = CreateLuaTable();
			int num = 0;
			foreach (object obj in objs)
			{
				luaTable[num.ToString()] = obj;
				num++;
			}
			return luaTable;
		}

		public static LuaTable CreateLuaTable(IDictionary objs)
		{
			LuaTable luaTable = CreateLuaTable();
			foreach (object key in objs.Keys)
			{
				luaTable[key] = objs[key];
			}
			return luaTable;
		}

		public static LuaTable toLuaTable(this IEnumerable objs)
		{
			return CreateLuaTable(objs);
		}

		public static LuaTable toLuaTable(this IList objs)
		{
			return CreateLuaTable(objs);
		}

		public static LuaTable toLuaTable(this IDictionary objs)
		{
			return CreateLuaTable(objs);
		}
	}
}
