using Domain.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Implementation
{
	public class EmailService : IEmailService
	{
		private readonly string _fromEmail = "info.apmtechnologies@gmail.com";
		private readonly string _appPassword = "lcvxobxxdkybwvwz"; // From Google App Passwords
		//private readonly string _fromEmail = "info@tracole.com";
		//private readonly string _appPassword = "vqpfrxjmlynxjjzx"; // From Google App Passwords
		public async Task SendAsync(string toEmail, string subject, string body)
		{
			using var smtpClient = new SmtpClient("smtp.gmail.com")
			{
				Port = 587,
				Credentials = new NetworkCredential(_fromEmail, _appPassword),
				EnableSsl = true
			};

			var mailMessage = new MailMessage(_fromEmail, toEmail, subject, body)
			{
				IsBodyHtml = true
			};

			await smtpClient.SendMailAsync(mailMessage);
		}
	}
}
