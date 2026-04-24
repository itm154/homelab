using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FileFlows.Common;
using FileFlows.Managers;
using FileFlows.ServerModels;
using FileFlows.Services;
using FileFlows.Shared;
using FileFlows.Shared.Models;

namespace FileFlows.Helpers
{
	// Token: 0x02000008 RID: 8
	internal class LicenseService
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x06000027 RID: 39
		// (remove) Token: 0x06000028 RID: 40
		public event Action<License> OnLicenseUpdated;

		// Token: 0x06000029 RID: 41
		internal static bool IsLicensed(LicenseFlags feature)
		{
			return true;
		}

		// Token: 0x0600002A RID: 42
		internal static bool IsLicensed()
		{
			return true;
		}

		// Token: 0x0600002B RID: 43
		internal static int GetLicensedNodes()
		{
			License license = ServiceLoader.Load<LicenseService>().GetLicense();
			if (license == null || license.Status != LicenseStatus.Valid)
			{
				return 1;
			}
			if (license.ExpirationDateUtc < DateTime.UtcNow)
			{
				return 1;
			}
			if (license.ProcessingNodes < 2)
			{
				return 1;
			}
			return license.ProcessingNodes;
		}

		// Token: 0x0600002C RID: 44
		internal int GetLicensedProcessingNodes()
		{
			return LicenseService.GetLicensedNodes();
		}

		// Token: 0x0600002D RID: 45
		internal License GetLicense()
		{
			return new License
			{
				Status = LicenseStatus.Valid,
				Level = LicenseLevel.Enterprise,
				Flags = (LicenseFlags)2147483647,
				ExpirationDateUtc = DateTime.UtcNow.AddYears(50),
				ProcessingNodes = 100,
				FileDropUsers = 100,
				Files = int.MaxValue
			};
		}

		// Token: 0x0600002E RID: 46
		internal License FromCode(string code)
		{
			if (string.IsNullOrWhiteSpace(code))
			{
				return null;
			}
			License license;
			try
			{
				license = JsonSerializer.Deserialize<License>(this.Decrypt("MIIEowIBAAKCAQEAqdurpq85Xtg0haj0LHl//hKBlAFyX4Vsuo2xhIScoYqHUEGoljEZUZdiOe764kiNKOQEqpzbXijoyxGLy4mDtDkEa90L21gbn31mekiIuODdEW9IKmPftrB182hTWYUUg1VlTunkVLVln/ywdf1BfcAHeQn9EcmxwDbqovmMTspKqxrVYgbtEpZZybqbwnyo9m+AKeU+xag3zoAnw2q4OejMLm00BF/uhYDK6uSMQC1Qy15j06KIjY65YT5meDqfkfWsxOEpZ2uCiollFOYsvZPyrNGd6fvKGDD2fKAlvAwSn0PAsMAOUEm+QlZtne4pIADiVnMM9RX7f4OtvVtaXQIDAQABAoIBAAguqc0kwbm85oLNyb1euPivQYi0rSLG1Z8C9lsw3C638p6+GvXpNZQFm9i4l2NRJWOj4EmrtrGJfPVTSg2q+So0WO8tPcX6L5J2Qvp/Bf8J7fxKBQrttrghNf1cuC8mxv8wnOm5QKOH/XZAgOueIIqDNpjxDzzNH3/n5VOme8jLx8PGMCwJWMFK0XdNObD8Dvzv3J9y3X1EVrB/Jm9pcc/2OMieL1kKrJmX3YvA5aXHB6cjbFDL5xwRb9wEOJlx/KlRQQVg7dLVhyMNRXc+gxLPvOR3cbeZ3nb8nGTJeJDSxq1p8+z61wQF5L5Kxche/hZdu1D2rGFOQHUjbrHEJE0CgYEAyFcoChhv9q9RgVwbk26ErIOejcVILIFlMcxbpRoJ/PD4ltXoz7u3UhBT+zsSPRxvT5gPZ6qqEIG5uWlnd1YOdS0KrtSM7K/nBI9IQoN65gTBTMzXSDkuZuZGvs726b4rhvuhDJa6uRyDkKpcsNQkCQ+KUIfnWWYkKq5oJba7S/cCgYEA2QyDI1UkuVtGkWJLL7h1g95abfLOIreV0czMc9uVBCbL+2EyQC44vIwqsRDZkx5/XlvY4bT+o6zAKmwdmCa/QOmWY1SkGBVkDwD6i4mOHnmrV+5icF2OiaSmjtFFOOLteq0SauIWhjEGQgNgoldj6kXVtqVvIUY8rj2yDBrUb0sCgYEAiCYDBelZnbHDmD/6VZVUANFp3TrnM6e0F8Wjum4Zv5YbupYgo5wUl2aVTDT2ziUW2GakgXUQIiunBgRF1mnbZXJ4whucsfVQ8F5XYyxrRwqQOxsyatjBWhjAl0ebsXoVpqQ27JE60DY6iwPb/igNXUL8YoIZjT3G8mKYUJkAbD0CgYBlE+aeNbB8gX1DhzrsZkKTvqDuQvysPkKPCYjNC51B6a9kycbVDLFvXPckrmwkjzdRggRmWBudrX1wRBkkGidG24ElkO06KfwG4LXM9aoxlwesU1+UZH1UrFDEgcBy1Xsyfhbtn4xNwdbgNyJxd7EYEJ2OCUzPeh4YJrMb4AK+MQKBgBXolFhtJWGrKHSfAgXdLMltDErrAO0t0vvQzdZEQExXRhnx3KPfpHMwg5x13bN95aCvodR+136csEOk6UqSuH5xAtZWWkZQE5umBNm1DJs68cERIC6XDishlXkXgAL9qkurmSJ7RK5dcCfZI/uM6BBjs43IqfVUUKmt8Gqbedry", code), null);
			}
			catch (Exception)
			{
				license = null;
			}
			return license;
		}

		// Token: 0x0600002F RID: 47
		internal static string GetLicenseEncryptedCode(string key, string email)
		{
			if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(email))
			{
				return string.Empty;
			}
			string json = JsonSerializer.Serialize<LicenseService.LicenseValidationModel>(new LicenseService.LicenseValidationModel
			{
				Key = key,
				EmailAddress = email
			}, null);
			return LicenseService.Encrypt("MIIBCgKCAQEAtMPKGqGr2pYyaMvoxE8d6rlL//Rl7be9AqA4inKvAc0MWmGy6MaiWvX2YHJfaddNSo3CXIgt48KQAUte/+ZM5Nja/cYECPDIS51ragsTfSK/jW5WVsOw8GzZlCV0rcQHQJ+MtNb6lBZD89ffOkQZHAQuC8lh4ptHmnQ3nupnUhlQGOAfnHQSqiDV/BUKcJINQAYMmrVHQJwAm1iXz6xq+dOhzaf+aJ28oRLanEsPcfwZpfkhlxCavMIkQNfIiVJBX89aw4U9yAgMbNhwFr9Zy6lOLyjjHNitOGrgEl1CEgsE04DUQWx2OHmN44rrxv1CQn/vam0G8PHzognbqtw0EwIDAQAB", json);
		}

		// Token: 0x06000030 RID: 48
		internal async Task Update(bool startUp = false)
		{
			AppSettingsService service = ServiceLoader.Load<AppSettingsService>();
			string email = service.Settings.LicenseEmail;
			string key = service.Settings.LicenseKey;
			if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(key))
			{
				AuditManager.PerformAudits = false;
				this._License = null;
				this._LastLicenseEmail = string.Empty;
				this._LastLicenseKey = string.Empty;
				service.Settings.LicenseCode = string.Empty;
				service.Save();
				Action<License> onLicenseUpdated = this.OnLicenseUpdated;
				if (onLicenseUpdated != null)
				{
					onLicenseUpdated(this._License);
				}
			}
			else if (!(this._LastLicenseEmail == email) || !(this._LastLicenseKey == key) || !(this._LastUpdate > DateTime.UtcNow.AddMinutes(-5.0)))
			{
				try
				{
					if (startUp)
					{
						AppSettings settings = service.Settings;
						License settingsLicense = this.FromCode(((settings != null) ? settings.LicenseCode : null) ?? string.Empty);
						if (settingsLicense != null && settingsLicense.Status == LicenseStatus.Valid && settingsLicense.ExpirationDateUtc > DateTime.UtcNow.AddDays(28.0) && settingsLicense.ExpirationDateUtc < DateTime.UtcNow.AddDays(45.0))
						{
							Logger instance = Logger.Instance;
							if (instance != null)
							{
								instance.ILog(new object[] { "Skipping startup license request" });
							}
							this._License = settingsLicense;
							this._LastLicenseEmail = email;
							this._LastLicenseKey = key;
							this._LastUpdate = DateTime.UtcNow;
							return;
						}
					}
					string requestCode = LicenseService.GetLicenseEncryptedCode(key, email);
					RequestResult<string> result = await HttpHelper.Post(Globals.FileFlowsDotComUrl + "/licensing/validate", new
					{
						Code = requestCode
					}, false);
					if (result.Success)
					{
						string licenseCode = result.Body;
						License license = this.FromCode(licenseCode);
						if (license != null)
						{
							this._License = license;
							this._LastLicenseEmail = email;
							this._LastLicenseKey = key;
							this._LastUpdate = DateTime.UtcNow;
							AuditManager.PerformAudits = (license.Flags & LicenseFlags.Auditing) == LicenseFlags.Auditing;
							service.Settings.LicenseCode = licenseCode;
							service.Save();
							Action<License> onLicenseUpdated2 = this.OnLicenseUpdated;
							if (onLicenseUpdated2 != null)
							{
								onLicenseUpdated2(this._License);
							}
						}
					}
				}
				catch (Exception)
				{
				}
			}
		}

		// Token: 0x06000031 RID: 49
		private static string Encrypt(string key, string data)
		{
			if (string.IsNullOrEmpty(data))
			{
				return string.Empty;
			}
			RSACryptoServiceProvider rsacryptoServiceProvider = new RSACryptoServiceProvider();
			byte[] publicKey = Convert.FromBase64String(key);
			int bytesRead;
			rsacryptoServiceProvider.ImportRSAPublicKey(publicKey, out bytesRead);
			byte[] bData = Encoding.UTF8.GetBytes(data);
			return Convert.ToBase64String(rsacryptoServiceProvider.Encrypt(bData, false));
		}

		// Token: 0x06000032 RID: 50
		private string Decrypt(string key, string data)
		{
			if (string.IsNullOrEmpty(data))
			{
				return string.Empty;
			}
			string text;
			try
			{
				RSACryptoServiceProvider rsacryptoServiceProvider = new RSACryptoServiceProvider();
				byte[] privateKey = Convert.FromBase64String(key);
				int bytesRead;
				rsacryptoServiceProvider.ImportRSAPrivateKey(privateKey, out bytesRead);
				byte[] ciphterText = Convert.FromBase64String(data);
				byte[] plainText = rsacryptoServiceProvider.Decrypt(ciphterText, false);
				text = Encoding.UTF8.GetString(plainText);
			}
			catch (Exception)
			{
				text = string.Empty;
			}
			return text;
		}

		// Token: 0x0400000F RID: 15
		private const string LicenseResponse_DecryptionKey = "MIIEowIBAAKCAQEAqdurpq85Xtg0haj0LHl//hKBlAFyX4Vsuo2xhIScoYqHUEGoljEZUZdiOe764kiNKOQEqpzbXijoyxGLy4mDtDkEa90L21gbn31mekiIuODdEW9IKmPftrB182hTWYUUg1VlTunkVLVln/ywdf1BfcAHeQn9EcmxwDbqovmMTspKqxrVYgbtEpZZybqbwnyo9m+AKeU+xag3zoAnw2q4OejMLm00BF/uhYDK6uSMQC1Qy15j06KIjY65YT5meDqfkfWsxOEpZ2uCiollFOYsvZPyrNGd6fvKGDD2fKAlvAwSn0PAsMAOUEm+QlZtne4pIADiVnMM9RX7f4OtvVtaXQIDAQABAoIBAAguqc0kwbm85oLNyb1euPivQYi0rSLG1Z8C9lsw3C638p6+GvXpNZQFm9i4l2NRJWOj4EmrtrGJfPVTSg2q+So0WO8tPcX6L5J2Qvp/Bf8J7fxKBQrttrghNf1cuC8mxv8wnOm5QKOH/XZAgOueIIqDNpjxDzzNH3/n5VOme8jLx8PGMCwJWMFK0XdNObD8Dvzv3J9y3X1EVrB/Jm9pcc/2OMieL1kKrJmX3YvA5aXHB6cjbFDL5xwRb9wEOJlx/KlRQQVg7dLVhyMNRXc+gxLPvOR3cbeZ3nb8nGTJeJDSxq1p8+z61wQF5L5Kxche/hZdu1D2rGFOQHUjbrHEJE0CgYEAyFcoChhv9q9RgVwbk26ErIOejcVILIFlMcxbpRoJ/PD4ltXoz7u3UhBT+zsSPRxvT5gPZ6qqEIG5uWlnd1YOdS0KrtSM7K/nBI9IQoN65gTBTMzXSDkuZuZGvs726b4rhvuhDJa6uRyDkKpcsNQkCQ+KUIfnWWYkKq5oJba7S/cCgYEA2QyDI1UkuVtGkWJLL7h1g95abfLOIreV0czMc9uVBCbL+2EyQC44vIwqsRDZkx5/XlvY4bT+o6zAKmwdmCa/QOmWY1SkGBVkDwD6i4mOHnmrV+5icF2OiaSmjtFFOOLteq0SauIWhjEGQgNgoldj6kXVtqVvIUY8rj2yDBrUb0sCgYEAiCYDBelZnbHDmD/6VZVUANFp3TrnM6e0F8Wjum4Zv5YbupYgo5wUl2aVTDT2ziUW2GakgXUQIiunBgRF1mnbZXJ4whucsfVQ8F5XYyxrRwqQOxsyatjBWhjAl0ebsXoVpqQ27JE60DY6iwPb/igNXUL8YoIZjT3G8mKYUJkAbD0CgYBlE+aeNbB8gX1DhzrsZkKTvqDuQvysPkKPCYjNC51B6a9kycbVDLFvXPckrmwkjzdRggRmWBudrX1wRBkkGidG24ElkO06KfwG4LXM9aoxlwesU1+UZH1UrFDEgcBy1Xsyfhbtn4xNwdbgNyJxd7EYEJ2OCUzPeh4YJrMb4AK+MQKBgBXolFhtJWGrKHSfAgXdLMltDErrAO0t0vvQzdZEQExXRhnx3KPfpHMwg5x13bN95aCvodR+136csEOk6UqSuH5xAtZWWkZQE5umBNm1DJs68cERIC6XDishlXkXgAL9qkurmSJ7RK5dcCfZI/uM6BBjs43IqfVUUKmt8Gqbedry";

		// Token: 0x04000010 RID: 16
		private const string LicenseRequest_EncryptionKey = "MIIBCgKCAQEAtMPKGqGr2pYyaMvoxE8d6rlL//Rl7be9AqA4inKvAc0MWmGy6MaiWvX2YHJfaddNSo3CXIgt48KQAUte/+ZM5Nja/cYECPDIS51ragsTfSK/jW5WVsOw8GzZlCV0rcQHQJ+MtNb6lBZD89ffOkQZHAQuC8lh4ptHmnQ3nupnUhlQGOAfnHQSqiDV/BUKcJINQAYMmrVHQJwAm1iXz6xq+dOhzaf+aJ28oRLanEsPcfwZpfkhlxCavMIkQNfIiVJBX89aw4U9yAgMbNhwFr9Zy6lOLyjjHNitOGrgEl1CEgsE04DUQWx2OHmN44rrxv1CQn/vam0G8PHzognbqtw0EwIDAQAB";

		// Token: 0x04000011 RID: 17
		private License _License;

		// Token: 0x04000012 RID: 18
		private DateTime _LastUpdate = DateTime.MinValue;

		// Token: 0x04000013 RID: 19
		private string _LastLicenseEmail;

		// Token: 0x04000014 RID: 20
		private string _LastLicenseKey;

		// Token: 0x02000066 RID: 102
		private class LicenseValidationModel
		{
			// Token: 0x1700009F RID: 159
			// (get) Token: 0x06000458 RID: 1112
			// (set) Token: 0x06000459 RID: 1113
			public string EmailAddress { get; set; } = string.Empty;

			// Token: 0x170000A0 RID: 160
			// (get) Token: 0x0600045A RID: 1114
			// (set) Token: 0x0600045B RID: 1115
			public string Key { get; set; } = string.Empty;
		}
	}
}
