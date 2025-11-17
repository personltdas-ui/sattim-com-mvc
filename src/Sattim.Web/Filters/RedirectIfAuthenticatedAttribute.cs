using Microsoft.AspNetCore.Mvc.Filters;

namespace Sattim.Web.Filters
{
    public class RedirectIfAuthenticatedAttribute : ActionFilterAttribute
    {
        private readonly string _redirectAction;
        private readonly string _redirectController;

        public RedirectIfAuthenticatedAttribute(string action = "Index", string controller = "Home")
        {
            _redirectAction = action;
            _redirectController = controller;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            
            if (context.HttpContext.User.Identity?.IsAuthenticated ?? false)
            {
                context.Result = new RedirectToActionResult(_redirectAction, _redirectController, null);
            }

            base.OnActionExecuting(context);
        }



    }
}
