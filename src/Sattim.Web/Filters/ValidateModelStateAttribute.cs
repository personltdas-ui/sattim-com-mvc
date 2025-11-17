using Microsoft.AspNetCore.Mvc.Filters;

namespace Sattim.Web.Filters
{
    public class ValidateModelStateAttribute : ActionFilterAttribute
    {
        
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            
            if (context.ModelState.IsValid)
            {
                base.OnActionExecuting(context);
                return;
            }

            if (context.Controller is Controller controller)
            {
                
                var model = context.ActionArguments.Values.FirstOrDefault();
                context.Result = controller.View(model);
            }
            else
            {
                context.Result = new BadRequestObjectResult(context.ModelState);
            }
        }
    }

}
