using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace FnI.Core.Web.UI
{
    // Old: public class UnAuthorizedPage : BasePage
    // New: UnAuthorizedController - No menu authorization check
    public class UnAuthorizedController : BaseController
    {
        public UnAuthorizedController(IConfiguration config, IHttpContextAccessor accessor)
            : base(config, accessor) { }

        // Page_Load ki jagah OnActionExecuting, aur isme AuthorizePage call nahi hai
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Sirf BaseController wala connection decrypt logic chalega
            // MenuID check skip ho jayega - yehi UnAuthorized ka matlab hai
            base.OnActionExecuting(context);
        }
    }

    // Backward compatibility ke liye alias - agar kahi purana naam use ho raha ho
    public class UnAuthorizedPage : UnAuthorizedController
    {
        public UnAuthorizedPage(IConfiguration config, IHttpContextAccessor accessor)
            : base(config, accessor) { }
    }
}