using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers
{
    [Authorize]
    [Route("profile")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class ProfileController : Controller
    {
        private readonly UserIconEffectService userIconEffectService;

        public ProfileController(UserIconEffectService _userIconEffectService)
        {
            userIconEffectService = _userIconEffectService;
        }

        [Route("my-effects")]
        public IActionResult MyEffects()
        {
            ViewBag.HideSubBar = true;

            int userId = int.Parse(User.FindFirst("UserId")!.Value);
            var effects = userIconEffectService.GetByUser(userId);

            return View("~/Views/Profile/MyEffects.cshtml", effects);
        }

        [HttpPost]
        [Route("equip-effect/{id}")]
        public IActionResult EquipEffect(int id)
        {
            int userId = int.Parse(User.FindFirst("UserId")!.Value);

            if (userIconEffectService.Equip(userId, id))
            {
                TempData["ToastMessage"] = "Trang bị effect thành công!";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = "Không thể trang bị effect.";
                TempData["ToastType"] = "error";
            }

            return RedirectToAction("MyEffects");
        }

        [HttpPost]
        [Route("unequip-effect")]
        public IActionResult UnequipEffect()
        {
            int userId = int.Parse(User.FindFirst("UserId")!.Value);

            if (userIconEffectService.Unequip(userId))
            {
                TempData["ToastMessage"] = "Đã gỡ effect hiện tại!";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = "Không thể gỡ effect.";
                TempData["ToastType"] = "error";
            }

            return RedirectToAction("MyEffects");
        }
    }
}