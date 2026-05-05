using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JyGame
{
	public class Tools
	{
		private static System.Random rnd = new System.Random();

		public static string[] chineseNumber = new string[32]
		{
			"零", "一", "二", "三", "四", "五", "六", "七", "八", "九",
			"十", "十一", "十二", "十三", "十四", "十五", "十六", "十七", "十八", "十九",
			"二十", "二十一", "二十二", "二十三", "二十四", "二十五", "二十六", "二十七", "二十八", "二十九",
			"三十", "三十一"
		};

		public static char[] chineseTime = new char[12]
		{
			'子', '丑', '寅', '卯', '辰', '巳', '午', '未', '申', '酉',
			'戌', '亥'
		};

		public static double GetRandom(double a, double b)
		{
			double num = rnd.NextDouble();
			double num2 = 0.0;
			if (b > a)
			{
				num2 = a;
				a = b;
				b = num2;
			}
			return b + (a - b) * num;
		}

		public static int GetRandomInt(int a, int b)
		{
			return (int)GetRandom(a, b + 1);
		}

		public static bool ProbabilityTest(double p)
		{
			if (p < 0.0)
			{
				return false;
			}
			if (p >= 1.0)
			{
				return true;
			}
			return rnd.NextDouble() < p;
		}

		public static string StringToMultiLine(string content, int lineLength, string enterFlag = "\n")
		{
			string text = string.Empty;
			string text2 = content;
			while (text2.Length > 0)
			{
				if (text2.Length > lineLength)
				{
					string text3 = text2.Substring(0, lineLength);
					text2 = text2.Substring(lineLength, text2.Length - lineLength);
					text = text + text3 + "\n";
				}
				else
				{
					text += text2;
					text2 = string.Empty;
				}
			}
			return text;
		}

		public static int StringHashtoInt(string str)
		{
			int num = 0;
			foreach (char value in str)
			{
				num += Convert.ToInt32(value);
			}
			return num;
		}

		public static string DateToString(DateTime date)
		{
			return chineseNumber[date.Year] + "年" + chineseNumber[date.Month] + "月" + chineseNumber[date.Day] + "日";
		}

		public static bool IsChineseTime(DateTime t, char time)
		{
			return chineseTime[t.Hour / 2] == time;
		}

		public static T LoadObjectFromUrl<T>(string url) where T : BasePojo
		{
			string xml = Resources.Load(url.Split('.')[0]).ToString();
			return LoadObjectFromXML<T>(xml);
		}

		public static T LoadObjectFromXML<T>(string xml) where T : BasePojo
		{
			return DeserializeXML<T>(xml);
		}

		public static T DeserializeXML<T>(string xmlObj)
		{
			try
			{
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
				using (StringReader textReader = new StringReader(xmlObj))
				{
					return (T)xmlSerializer.Deserialize(textReader);
				}
			}
			catch (Exception ex)
			{
				Debug.Log(ex.ToString());
				Debug.Log("xml 解析错误:" + xmlObj);
				return default(T);
			}
		}

		public static string SerializeXML<T>(T obj)
		{
			StringBuilder stringBuilder = new StringBuilder();
			XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
			xmlWriterSettings.OmitXmlDeclaration = true;
			using (XmlWriter writer = XmlWriter.Create(stringBuilder, xmlWriterSettings))
			{
				XmlSerializerNamespaces xmlSerializerNamespaces = new XmlSerializerNamespaces();
				xmlSerializerNamespaces.Add(string.Empty, string.Empty);
				new XmlSerializer(obj.GetType()).Serialize(writer, obj, xmlSerializerNamespaces);
				return stringBuilder.ToString();
			}
		}

		public static IEnumerator ServerRequest(string path, Hashtable paramTable, CommonSettings.ObjectCallBack callback)
		{
			int i = 0;
			StringBuilder buffer = new StringBuilder();
			if (paramTable != null)
			{
				foreach (string key in paramTable.Keys)
				{
					if (i > 0)
					{
						buffer.AppendFormat("&{0}={1}", key, WWW.EscapeURL(paramTable[key] as string));
					}
					else
					{
						buffer.AppendFormat("?{0}={1}", key, WWW.EscapeURL(paramTable[key] as string));
					}
					i++;
				}
			}
			WWW www = new WWW(path + buffer.ToString());
			yield return www;
			int timeout = 100;
			while (!www.isDone && timeout-- > 0)
			{
				Thread.Sleep(100);
			}
			if (string.IsNullOrEmpty(www.error))
			{
				string response = www.text;
				callback(JsonUtility.FromJson<Object>(response));
				yield return response;
			}
			www.Dispose();
			yield return null;
		}

		public static void openURL(string url)
		{
			if (Application.platform == RuntimePlatform.WebGLPlayer)
			{
				Application.ExternalEval("window.open('" + url + "');");
			}
			else
			{
				Application.OpenURL(url);
			}
		}

		public static void getFileContentFromUrl(MonoBehaviour parent, string url, CommonSettings.StringCallBack callback, CommonSettings.VoidCallBack failCallback = null)
		{
			parent.StartCoroutine(DownloadFile(url, callback, failCallback));
		}

		public static IEnumerator DownloadFile(string url, CommonSettings.StringCallBack callback, CommonSettings.VoidCallBack failCallback = null)
		{
			WWW www = new WWW(url);
			Debug.Log("downloading file:" + url);
			yield return www;
			if (!string.IsNullOrEmpty(www.error))
			{
				Debug.LogError(www.error);
				if (failCallback != null)
				{
					failCallback();
				}
				www.Dispose();
			}
			else
			{
				string text = www.text;
				www.Dispose();
				callback(text);
			}
		}

		public static IEnumerator DownloadImage(string url, CommonSettings.ObjectCallBack callback, CommonSettings.VoidCallBack failCallback, Vector2 pivot, float pixerPerUnit = 1f)
		{
			WWW www = new WWW(url);
			Debug.Log("downloading file:" + url);
			yield return www;
			if (!string.IsNullOrEmpty(www.error))
			{
				Debug.LogError(www.error);
				if (failCallback != null)
				{
					failCallback();
				}
			}
			else
			{
				Sprite tmp = Sprite.Create(www.texture, new Rect(0f, 0f, www.texture.width, www.texture.height), pivot, pixerPerUnit);
				tmp.texture.Compress(true);
				tmp.texture.mipMapBias = 0f;
				www.Dispose();
				callback(tmp);
			}
		}

		public static IEnumerator Download(string url, string savepath, CommonSettings.VoidCallBack callback, CommonSettings.VoidCallBack failCallback)
		{
			WWW www = new WWW(url);
			Debug.Log("downloading file:" + url);
			yield return www;
			if (!string.IsNullOrEmpty(www.error))
			{
				Debug.LogError(www.error);
				www.Dispose();
				if (failCallback != null)
				{
					failCallback();
				}
			}
			else
			{
				using (FileStream fs = new FileStream(savepath, FileMode.OpenOrCreate))
				{
					fs.Write(www.bytes, 0, www.bytes.Length);
				}
				www.Dispose();
				callback();
			}
		}

		public static IEnumerator DownloadIntoTexture(Texture2D t, string url, Action callback)
		{
			WWW www = new WWW(url);
			yield return www;
			try
			{
				www.LoadImageIntoTexture(t);
				callback();
			}
			catch
			{
				foreach (Texture2D tt in ModEditorResourceManager.textureCache.Values)
				{
					UnityEngine.Object.DestroyImmediate(tt);
				}
				ModEditorResourceManager.textureCache.Clear();
			}
		}

		public static string GetMD5HashFromFile(string fileName)
		{
			try
			{
				FileStream fileStream = new FileStream(fileName, FileMode.Open);
				MD5 mD = new MD5CryptoServiceProvider();
				byte[] array = mD.ComputeHash(fileStream);
				fileStream.Close();
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = 0; i < array.Length; i++)
				{
					stringBuilder.Append(array[i].ToString("x2"));
				}
				return stringBuilder.ToString();
			}
			catch (Exception ex)
			{
				throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
			}
		}
	}
}
