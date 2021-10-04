﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace TabletFriend
{
	public static class UpdateChecker
	{
		private static Version _version = Assembly.GetExecutingAssembly().GetName().Version;

		private static readonly HttpClient _client = new HttpClient();

		private const string _repoLink = "https://api.github.com/repos/Martenfur/TaletFriend/releases/latest";

		private static string _downloadsPath => 
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads\\tablet_friend");

		public static async Task Check()
		{
			var response = await Get(_repoLink);

			var newVersion = new Version(response["name"].ToString());
			var changes = response["body"].ToString();

			if (_version.CompareTo(newVersion) >= 0)
			{
				return;
			}
			var result = MessageBox.Show(
				"A new version of Tablet Friend is available." 
				+ Environment.NewLine
				+ Environment.NewLine
				+ "v" + newVersion
				+ Environment.NewLine
				+ Environment.NewLine
				+ changes
				+ Environment.NewLine
				+ Environment.NewLine
				+ "Would you like to download it?",
				"Update!",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question
			);

			if (result != MessageBoxResult.Yes)
			{
				return;
			}

			await DownloadUpdate(response, newVersion.ToString());
		}

		private static async Task DownloadUpdate(JObject response, string version)
		{
			var links = new List<string>();

			var versionedDownloadsPath = _downloadsPath + "_" + version;

			foreach (var asset in (JArray)response["assets"])
			{
				links.Add(asset["browser_download_url"].ToString());
			}
			Directory.CreateDirectory(versionedDownloadsPath);

			using (var cliente = new WebClient())
			{
				foreach (var link in links)
				{
					var outFile = Path.Combine(versionedDownloadsPath, Path.GetFileName(link));
					await cliente.DownloadFileTaskAsync(new Uri(link), outFile);
				}
			}

			var startInfo = new ProcessStartInfo()
			{
				Arguments = versionedDownloadsPath,
				FileName = "explorer.exe"
			};
			Process.Start(startInfo);

		}

		private async static Task<JObject> Get(string uri)
		{
			var request = new HttpRequestMessage()
			{
				RequestUri = new Uri(uri),
				Method = HttpMethod.Get
			};
			request.Headers.Add("User-Agent", "foxe");

			var response = await _client.SendAsync(request);
			response.EnsureSuccessStatusCode();
			return JObject.Parse(await response.Content.ReadAsStringAsync());
		}
	}
}
