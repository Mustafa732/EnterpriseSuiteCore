using Common.Config;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace Common.Utils
{
    public class Message
    {
        public static void DisplayMessage(Controller currentPage, Enumaration.MessageCode MessageCode)
        {
            switch (MessageCode)
            {
                case Enumaration.MessageCode.Success_Add:
                    DisplayMessage(currentPage, MessageCode, Constants.Success_AddMessage.ToString());
                    break;
                case Enumaration.MessageCode.Success_Update:
                    DisplayMessage(currentPage, MessageCode, Constants.Success_UpdateMessage.ToString());
                    break;
                case Enumaration.MessageCode.Success_Delete:
                    DisplayMessage(currentPage, MessageCode, Constants.Success_DeleteMessage.ToString());
                    break;
                case Enumaration.MessageCode.Failure_Add:
                    DisplayMessage(currentPage, MessageCode, Constants.Failure_AddMessage.ToString(), Enumaration.MessageType.Error);
                    break;
                case Enumaration.MessageCode.Failure_Update:
                    DisplayMessage(currentPage, MessageCode, Constants.Failure_UpdateMessage.ToString(), Enumaration.MessageType.Error);
                    break;
                case Enumaration.MessageCode.Failure_Delete:
                    DisplayMessage(currentPage, MessageCode, Constants.Failure_DeleteMessage.ToString(), Enumaration.MessageType.Error);
                    break;
                case Enumaration.MessageCode.Unknown:
                    DisplayMessage(currentPage, MessageCode, Constants.Unkown_Message.ToString(), Enumaration.MessageType.Error);
                    break;
            }
        }

        public static void DisplayMessage(Controller currentPage, Enumaration.MessageCode customMessageCode, string messageText, Enumaration.MessageType messageType = Enumaration.MessageType.Message, bool showIcon = true, bool? permanent = null, int disappearTime = 30000)
        {
            string message = messageText ?? "";
            message = Regex.Replace(message, "\n", " ");
            message = Regex.Replace(message, "\r", " ");
            message = Regex.Replace(message, "'", " ");

            // .NET 8 me ScriptManager ki jagah TempData use hoga
            // _Layout.cshtml me isko swal se show karna
            currentPage.TempData["MessageCode"] = customMessageCode.ToString();
            currentPage.TempData["MessageText"] = message;
            currentPage.TempData["MessageType"] = messageType == Enumaration.MessageType.Message ? "success" : "error";
            currentPage.TempData["ShowIcon"] = showIcon.ToString();
            currentPage.TempData["DisappearTime"] = disappearTime.ToString();
        }

        // Extra helper - agar tumhe direct JSON response chahiye ho
        public static object GetMessageJson(string messageText, Enumaration.MessageType messageType = Enumaration.MessageType.Message)
        {
            string message = Regex.Replace(messageText, "\n", " ");
            message = Regex.Replace(message, "\r", " ");
            message = Regex.Replace(message, "'", " ");

            return new
            {
                text = message,
                type = messageType == Enumaration.MessageType.Message ? "success" : "error"
            };
        }
    }
}