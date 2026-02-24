using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace TalkToMe.Controllers
{
    [Route("[Controller]/[action]")]
    public class CultureController : Controller
    {
        public IActionResult Set(string culture, string redirectUri)
        {
            if (!string.IsNullOrWhiteSpace(culture))
            {
                var requestCulture = new RequestCulture(culture);
                var cookieName = CookieRequestCultureProvider.DefaultCookieName;
                var cookieValue = CookieRequestCultureProvider.MakeCookieValue(requestCulture);

                var cookieOptions = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddMonths(1),
                    Path = "/"
                };

                HttpContext.Response.Cookies.Append(cookieName, cookieValue, cookieOptions);
            }

            return LocalRedirect(redirectUri ?? "/");
        }
    }

}
