using Microsoft.AspNetCore.Identity;
using AkarangaBank.Web.Models;
namespace AkarangaBank.Web.Services;
public class CurrentUserService : ICurrentUserService {
 private readonly IHttpContextAccessor _accessor;
 private readonly UserManager<ApplicationUser> _userManager;
 public CurrentUserService(
 IHttpContextAccessor accessor,
 UserManager<ApplicationUser> userManager){
 _accessor = accessor;
 _userManager = userManager;
 }
 public async Task<int> GetUserIdAsync(){
 var user = await _userManager.GetUserAsync(_accessor.HttpContext!.User);
 return user?.Id ?? throw new InvalidOperationException("Utilisateur non connecté.");
 }
 public async Task<int> GetAgenceIdAsync(){
 var user = await _userManager.GetUserAsync(_accessor.HttpContext!.User);
 return user?.AgenceId ?? throw new InvalidOperationException("Utilisateur non connecté.");
 }
}