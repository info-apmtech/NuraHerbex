using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interface
{
	public interface INotificationClientService
	{
		Task<string> SendOtpSms(string ApiKey, string PhoneNumber, SMSTemplateType TemplateType);

	}
}
