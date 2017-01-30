﻿

namespace Wires
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Net;
	using System.Security.Cryptography;
	using System.Text;
	using System.Threading.Tasks;

	public static class FileCache
	{
		public const string Folder = "./.file-cache";

		private static string CreateHash(string input)
		{
			using (var alg = SHA256.Create())
			{
				byte[] data = alg.ComputeHash(Encoding.UTF8.GetBytes(input));
				var builder = new StringBuilder();
				for (int i = 0; i < data.Length; i++)
				{
					builder.Append(data[i].ToString("x2"));
				}
				return builder.ToString();
			}
		}

		public static string GetCachePath(string url) => Path.Combine(Folder, $"{CreateHash(url)}");

		public static async Task<string> DownloadCachedFile(string url, TimeSpan expiration)
		{
			var cachePath = GetCachePath(url);

			DateTime lastWrite = DateTime.MinValue;
			if (!File.Exists(cachePath) || (lastWrite = File.GetLastWriteTimeUtc(cachePath)) + expiration < DateTime.UtcNow)
			{
				if (!Directory.Exists(Folder))
				{
					Directory.CreateDirectory(Folder);
				}

				try
				{
					Debug.WriteLine($"[Cache][Images]({cachePath}) Start downloading from \"{url}\" ...");
					var request = HttpWebRequest.Create(url);
					using (var res = (await request.GetResponseAsync()) as HttpWebResponse)
					{
						if (res.LastModified > lastWrite)
						{
							using (var content = res.GetResponseStream())
							{
								using (var filestream = File.OpenWrite(cachePath))
								{

									await content.CopyToAsync(filestream);
									Debug.WriteLine($"[Cache][Images]({cachePath}) Updated cache");
								}
							}
						}
						else Debug.WriteLine($"[Cache][Images]({cachePath}) Not updating cache because last write is more recent that request last modified date ({res.LastModified} > {lastWrite}).");
					}
				}
				catch (Exception ex)
				{
					if (!File.Exists(cachePath))
					{
						throw ex;
					}
					Debug.WriteLine($"[Cache][Images]({cachePath}) Download failed, but a cached version exists.");
				}
			}

			return cachePath;
		}
	}
}