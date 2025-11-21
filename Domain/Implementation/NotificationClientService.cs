using Domain.Interface;
using Domain.Models;
using Domain.ViewModel;
using Newtonsoft.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Domain.Implementation
{
	public class NotificationClientService
	{
		private readonly HttpClient _httpClient;
		public NotificationClientService(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}
		public async Task<string> SendOtpSms(string ApiKey, string PhoneNumber, SMSTemplateType TemplateType)
		{
			try
			{

				var otp = GenerateSecureOtp(6);
				var Message = "";
				var TemplateId = "";
				if (ApiKey == "apm_forgetkey_nuraherbex")
				{
					if (TemplateType == SMSTemplateType.Registration)
					{
						Message = $"{otp} is your registration process OTP. DO NOT SHARE this with anyone. OTP valid for 15 mins. Regards, nuraherbex";
						TemplateId = "1007664828601612405";
					}
				}
				if (!string.IsNullOrEmpty(Message))
				{
					var smsJson = this.SmsData(Message, PhoneNumber, TemplateId);
					var jsonContent = JsonConvert.SerializeObject(smsJson);
					var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
					var response = await _httpClient.PostAsync("https://digimate.airtel.in:44111/BulkPush/InstantJsonPush", content);
					if (response.IsSuccessStatusCode)
					{
						return otp;
					}
					else
					{
						// You can log or throw an error depending on your needs
						return ("Error While sending API.");
					}
				}
				else
				{
					return ("Api Key is Invalid.");
				}
			}
			catch (Exception ex)
			{
				return ("Error: " + ex.Message);
			}
		}
		private string GenerateSecureOtp(int length)
		{
			using (var rng = new RNGCryptoServiceProvider())
			{
				var bytes = new byte[length];
				rng.GetBytes(bytes);
				var otp = string.Concat(bytes.Select(b => (b % 10).ToString()));
				return otp.Substring(0, length);
			}
		}

		private SmsJson SmsData(string message, string number, string templateId)
		{
			var currentTime = DateTime.Now.ToString("yyyyMMddHHmmss");

			var smsData = new SmsJson
			{
				keyword = "DEMO",
				timestamp = currentTime,
				dataSet = new List<SmsDataSet>
				{
					new SmsDataSet
					{
						UNIQUE_ID = Guid.NewGuid().ToString(),
						MESSAGE = message,
						OA = "APMGVT",
						MSISDN = number,
						CHANNEL = "SMS",
						CAMPAIGN_NAME = "NuraHerbex",
						DLT_CT_ID = templateId,
						DLT_PE_ID = "1001820015129873313",
						DLT_TM_ID = "1002466620000023188",
						CIRCLE_NAME = "DLT_SERVICE_IMPLICT",
						USER_NAME = "Apmgroup_hsi"
					}
				}
			};

			return smsData;
		}
	}
}
