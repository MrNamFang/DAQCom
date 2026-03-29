using System.Web.Mvc;
using System.Web.Routing;
using DAQCom.Models;

namespace DAQCom.Areas.Admin.Controllers
{
    public class BaseController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Kiểm tra session tên là "user"
            // Lưu ý: Ép kiểu về (admin) hoặc kiểm tra null thôi cũng được
            var session = Session["user"];

            if (session == null)
            {
                // Nếu chưa đăng nhập -> Đá về trang Đăng Nhập chung
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new
                {
                    controller = "QuanAn",
                    action = "DangNhap",
                    area = ""
                }));
            }

            base.OnActionExecuting(filterContext);
        }
    }
}