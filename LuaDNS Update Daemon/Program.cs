//ShMD CSLDU csharp luadns updater

using Newtonsoft.Json.Linq;

namespace LuaDNSUpdateDaemon
{
	class miniconfig
	{
		public string id { get; set; }
		public string token { get; set; }
		public string url { get; set; }

		public int zoneid { get; set; }

		public int recordid { get; set; }
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
					//https://stackoverflow.com/questions/58428375/cannot-get-original-executable-path-for-net-core-3-0-single-file-ppublishsin
					path = Path.Combine(System.AppContext.BaseDirectory, "csldu.json");
				}
			}

			if (!File.Exists(path))
				return null;

			JObject jCfg = JObject.Parse(File.ReadAllText(path));
			r.id = (String)jCfg["id"];
			r.token = (String)jCfg["token"];
			r.url = (String)jCfg["url"];
			try
			{
				r.zoneid = (int)jCfg["zoneid"];
				r.recordid = (int)jCfg["recordid"];
			}
			catch (Exception)
			{
				r.zoneid = -1;
				r.recordid = -1;
			}
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
			//TODO error handling
			string domname = cfg.url;
			int zoneid = 0;
			dnsrecord record = null;
			var cred = new System.Net.NetworkCredential(cfg.id, cfg.token);
			string ip = httpgetwrap("http://ifconfig.me", null, null).ToString();

			//search for zone id, but use given one if known.
			if (cfg.zoneid == -1 || cfg.recordid == -1)
			{
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
			}
			//Question - do Record ID change? Even after continuous updates?
			else
			{
				zoneid = cfg.zoneid;
				//get current ip anyway.
				record = Newtonsoft.Json.JsonConvert.DeserializeObject<dnsrecord>(httpgetwrap("https://api.luadns.com/v1/zones/" + zoneid.ToString() + "/records/" + cfg.recordid.ToString(), cred));
			}

			
			//DHCP IP did not change
			if (record.content == ip) return;

			record.content = ip;
			try
			{
				httpputwrap("https://api.luadns.com/v1/zones/" + zoneid.ToString() + "/records/" + record.id.ToString(), Newtonsoft.Json.JsonConvert.SerializeObject(record), cred);
			}
			catch (Exception)
			{
				//TODO... if records do change and update fails somehow, it should re-run record search again and update somhow.
				//experiment - if id is wrong, raises 404 error, with exception. 

				//just run search again. again IDK whether zone and record id changes, so... paste it!
				if (cfg.zoneid == -1 || cfg.recordid == -1)
				{
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
					httpputwrap("https://api.luadns.com/v1/zones/" + zoneid.ToString() + "/records/" + record.id.ToString(), Newtonsoft.Json.JsonConvert.SerializeObject(record), cred);
				}
				//if it wasn't id problem... then just exit!
			}

			return;
		}
	}
}