using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace JyGame
{
	public class SaveManager
	{
		public static string SaveDir
		{
			get
			{
				string empty = string.Empty;
				empty = ((GlobalData.CurrentMod != null) ? (CommonSettings.persistentDataPath + "/modcache/" + GlobalData.CurrentMod.key + "/saves/") : (CommonSettings.persistentDataPath + "/saves/"));
				if (!Directory.Exists(empty))
				{
					Directory.CreateDirectory(empty);
				}
				return empty;
			}
		}

		public static string GetSave(string saveName)
		{
			string path = SaveDir + saveName;
			if (!File.Exists(path))
			{
				return string.Empty;
			}
			using (StreamReader streamReader = new StreamReader(SaveDir + saveName))
			{
				return streamReader.ReadToEnd();
			}
		}

		public static void SetSave(string saveName, string content)
		{
			string text = SaveDir + saveName;
			using (StreamWriter streamWriter = new StreamWriter(SaveDir + saveName))
			{
				streamWriter.Write(content);
			}
		}

		public static void DeleteSave(string saveName)
		{
			string path = SaveDir + saveName;
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}

		public static bool ExistSave(string saveName)
		{
			string path = SaveDir + saveName;
			return File.Exists(path);
		}

		public static string crcjm(string input)
		{
			string text = jm(input);
			string text2 = CRC16_C(input);
			return text2 + "@" + text;
		}

		public static string crcm(string input)
		{
			string[] array = input.Split('@');
			if (array.Length != 2)
			{
				return string.Empty;
			}
			string text = array[0];
			string text2 = m(array[1]);
			string text3 = CRC16_C(text2);
			if (text != text3)
			{
				return string.Empty;
			}
			return text2;
		}

		private static string jm(string Message)
		{
			UTF8Encoding uTF8Encoding = new UTF8Encoding();
			MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
			byte[] key = mD5CryptoServiceProvider.ComputeHash(uTF8Encoding.GetBytes("$t611@mods"));
			TripleDESCryptoServiceProvider tripleDESCryptoServiceProvider = new TripleDESCryptoServiceProvider();
			tripleDESCryptoServiceProvider.Key = key;
			tripleDESCryptoServiceProvider.Mode = CipherMode.ECB;
			tripleDESCryptoServiceProvider.Padding = PaddingMode.PKCS7;
			TripleDESCryptoServiceProvider tripleDESCryptoServiceProvider2 = tripleDESCryptoServiceProvider;
			byte[] bytes = uTF8Encoding.GetBytes(Message);
			byte[] inArray;
			try
			{
				inArray = tripleDESCryptoServiceProvider2.CreateEncryptor().TransformFinalBlock(bytes, 0, bytes.Length);
			}
			finally
			{
				tripleDESCryptoServiceProvider2.Clear();
				mD5CryptoServiceProvider.Clear();
			}
			return Convert.ToBase64String(inArray);
		}

		private static string m(string Message)
		{
			UTF8Encoding uTF8Encoding = new UTF8Encoding();
			MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
			byte[] key = mD5CryptoServiceProvider.ComputeHash(uTF8Encoding.GetBytes("$t611@mods"));
			TripleDESCryptoServiceProvider tripleDESCryptoServiceProvider = new TripleDESCryptoServiceProvider();
			tripleDESCryptoServiceProvider.Key = key;
			tripleDESCryptoServiceProvider.Mode = CipherMode.ECB;
			tripleDESCryptoServiceProvider.Padding = PaddingMode.PKCS7;
			TripleDESCryptoServiceProvider tripleDESCryptoServiceProvider2 = tripleDESCryptoServiceProvider;
			byte[] array = Convert.FromBase64String(Message);
			byte[] bytes;
			try
			{
				bytes = tripleDESCryptoServiceProvider2.CreateDecryptor().TransformFinalBlock(array, 0, array.Length);
			}
			finally
			{
				tripleDESCryptoServiceProvider2.Clear();
				mD5CryptoServiceProvider.Clear();
			}
			return uTF8Encoding.GetString(bytes);
		}

		private static string CRC16_C(string str)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(str);
			byte b = byte.MaxValue;
			byte b2 = byte.MaxValue;
			byte b3 = 1;
			byte b4 = 160;
			byte[] array = bytes;
			for (int i = 0; i < array.Length; i++)
			{
				b ^= array[i];
				for (int j = 0; j <= 7; j++)
				{
					byte b5 = b2;
					byte b6 = b;
					b2 >>= 1;
					b >>= 1;
					if ((b5 & 1) == 1)
					{
						b |= 0x80;
					}
					if ((b6 & 1) == 1)
					{
						b2 ^= b4;
						b ^= b3;
					}
				}
			}
			return string.Format("{0}{1}", b2, b);
		}
	}
}
