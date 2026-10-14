using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;

namespace FnI.BusinessAccessLayer
{
    public class JDWMail_Helper
    {
        /// <summary>
        /// Sends an mail message
        /// </summary>
        /// <param name="from">Sender address</param>
        /// <param name="to">Recepient address</param>
        /// <param name="bcc">Bcc recepient</param>
        /// <param name="cc">Cc recepient</param>
        /// <param name="subject">Subject of mail message</param>
        /// <param name="body">Body of mail message</param>
        public static void SendMailMessage(string from, string to, string bcc, string cc, string subject, string body)
        {
            try
            {
                MailMessage mMailMessage = new MailMessage();
                mMailMessage.From = new MailAddress(from);
                mMailMessage.To.Add(new MailAddress(to));

                if ((bcc != null) && (bcc != string.Empty))
                    mMailMessage.Bcc.Add(new MailAddress(bcc));
                if ((cc != null) && (cc != string.Empty))
                    mMailMessage.CC.Add(new MailAddress(cc));

                mMailMessage.Subject = subject;
                mMailMessage.Body = body;
                mMailMessage.IsBodyHtml = true;
                mMailMessage.Priority = MailPriority.Normal;

                SmtpClient mSmtpClient = new SmtpClient();
                mSmtpClient.Send(mMailMessage);
            }
            catch
            {
                throw new Exception();
            }
        }

        /// <summary>
        /// send email with attachement
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="bcc"></param>
        /// <param name="cc"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        public static void SendMailMessageWithAttachement(string from, string to, string bcc, string cc, string subject, string body, string strAttachementPath)
        {
            try
            {
                MailMessage mMailMessage = new MailMessage();
                mMailMessage.From = new MailAddress(from);
                mMailMessage.To.Add(new MailAddress(to));

                if ((bcc != null) && (bcc != string.Empty))
                    mMailMessage.Bcc.Add(new MailAddress(bcc));
                if ((cc != null) && (cc != string.Empty))
                    mMailMessage.CC.Add(new MailAddress(cc));

                mMailMessage.Subject = subject;
                mMailMessage.Body = body;
                mMailMessage.IsBodyHtml = true;
                mMailMessage.Priority = MailPriority.Normal;

                SmtpClient mSmtpClient = new SmtpClient();
                //System.Net.Mail.Attachment attachment;
                //attachment = new System.Net.Mail.Attachment(strAttachementPath);
                //mMailMessage.Attachments.Add(attachment);
                mSmtpClient.Send(mMailMessage);
            }
            catch (Exception ex)
            {
                //System.IO.File.AppendAllText(@"D:\QOPS\Log\Error.txt", ex.Message);
            }
        }
    
    }
}
