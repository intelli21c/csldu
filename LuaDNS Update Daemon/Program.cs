﻿//ShMD CSLDU csharp luadns updater

using Newtonsoft.Json.Linq;

namespace LuaDNSUpdateDaemon
{
	class miniconfig
	{
		public string id { get; set; }
		public string token { get; set; }
		public string url { get; set; }
	}

	class minizone
	{
		public int id;
		public string name;
	}
	class dnsrecord
	{
		public int id { get; set; }
		public string name { get; set; }
		public string type { get; set; }
		public string content { get; set; }
		public int ttl { get; set; }
	}

	class Module
	{

		static string GetConfigDirectoryName()
		{
			string configDirectoryName;

			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				// Windows path
				string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
				configDirectoryName = Path.Combine(commonAppData, "CSLDU");
			}
			else if (Environment.OSVersion.Platform == PlatformID.Unix ||
					 Environment.OSVersion.Platform == PlatformID.MacOSX)
			{
				// Linux/Unix/Mac path
				configDirectoryName = "/usr/local/etc";
			}
			else
			{
				// Handle other platforms if needed
				throw new NotSupportedException("Unsupported operating system");
			}

			return configDirectoryName;
		}

		private static miniconfig parseconfig(string path = null)
		{
			var r = new miniconfig();
			if (path == null)
			{
				string configDirectoryName = GetConfigDirectoryName();
				Path.Combine(configDirectoryName, "csldu.json");
				if (!File.Exists(path))
				{
					path = Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "csldu.json");
				}
			}

			if (!File.Exists(path))
				return null;

			JObject jCfg = JObject.Parse(File.ReadAllText(path));
			r.id = (String)jCfg["id"];
			r.token = (String)jCfg["token"];
			r.url = (String)jCfg["url"];

			return r;
		}

		private static string httpgetwrap(string url, System.Net.NetworkCredential credential = null, string hdrtype = null)
		{
			System.Net.HttpWebRequest request;
			request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
			request.Method = "GET";
			if (hdrtype == null) request.Headers.Add("Accept: application/json");
			if (credential != null) request.Credentials = credential;
			try
			{
				return ((new StreamReader(((System.Net.HttpWebResponse)request.GetResponse()).GetResponseStream())).ReadToEnd());
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}
		private static string httpputwrap(string url, string payload, System.Net.NetworkCredential credential = null, string hdrtype = null)
		{
			System.Net.HttpWebRequest request;
			request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
			request.Method = "PUT";
			if (hdrtype == null) request.Headers.Add("Accept: application/json");
			if (credential != null) request.Credentials = credential;

			try
			{
				var rs = request.GetRequestStream();
				rs.Write(System.Text.ASCIIEncoding.ASCII.GetBytes(payload!));
				rs.Close();

				return ((new StreamReader(((System.Net.HttpWebResponse)request.GetResponse()).GetResponseStream())).ReadToEnd());
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}
		public static void Main(String[] args)
		{
			var cfg = parseconfig(args.Length == 0 ? null : args[0]);
			string domname = cfg.url;
			int zoneid = 0;
			dnsrecord record = null;
			var cred = new System.Net.NetworkCredential(cfg.id, cfg.token);
			foreach (var x in Newtonsoft.Json.JsonConvert.DeserializeObject<List<minizone>>(httpgetwrap("https://api.luadns.com/v1/zones", cred)))
			{
				if (x.name == domname)
				{
					zoneid = x.id;
					break;
				}
			}
			foreach (var x in Newtonsoft.Json.JsonConvert.DeserializeObject<List<dnsrecord>>(httpgetwrap("https://api.luadns.com/v1/zones/" + zoneid.ToString() + "/records", cred)))
			{
				if ((x.name == (domname + ".")) && (x.type == "A"))
				{
					record = x;
					break;
				}
			}
			record.content = httpgetwrap("http://ifconfig.me", null, null).ToString();
			httpputwrap("https://api.luadns.com/v1/zones/" + zoneid.ToString() + "/records/" + record.id.ToString(), Newtonsoft.Json.JsonConvert.SerializeObject(record), cred);
			return;
		}
	}
}