﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevBetterWeb.Core;
using DevBetterWeb.Core.Events;
using DevBetterWeb.Core.Interfaces;
using DevBetterWeb.Web.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DevBetterWeb.Web.Pages.Admin
{
    [Authorize(Roles = AuthConstants.Roles.ADMINISTRATORS)]
    public class RoleModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IDomainEventDispatcher _dispatcher;

        public RoleModel(UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager,
            IDomainEventDispatcher dispatcher)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _dispatcher = dispatcher;
        }
        public IdentityRole? Role { get; set; }
        public List<ApplicationUser> UsersInRole { get; set; } = new List<ApplicationUser>();
        public List<SelectListItem> UsersNotInRole { get; set; } = new List<SelectListItem>();

        public async Task<IActionResult> OnGetAsync(string roleId)
        {
            var role = _roleManager.Roles.Single(x => x.Id == roleId);

            if (role == null)
            {
                return BadRequest();
            }

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);

            var userIdsInRole = usersInRole.Select(X => X.Id).ToList();
            var usersNotInRole = _userManager.Users.Where(x => !userIdsInRole.Contains(x.Id)).ToList();

            Role = role;
            UsersInRole = usersInRole.ToList();
            UsersNotInRole = usersNotInRole.Select(x => new SelectListItem(x.Email, x.Id)).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAddUserToRoleAsync(string userId, string roleId)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
            var role = await _roleManager.Roles.FirstOrDefaultAsync(x => x.Id == roleId);

            if (user == null || role == null)
            {
                return BadRequest();
            }

            await _userManager.AddToRoleAsync(user, role.Name);

            var userAddedToRoleEvent = new UserAddedToRoleEvent(user.Email, role.Name);
            await _dispatcher.Dispatch(userAddedToRoleEvent);

            return RedirectToPage("./Role", new { roleId });
        }

        public async Task<IActionResult> OnPostRemoveUserFromRole(string userId, string roleId)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
            var role = await _roleManager.Roles.FirstOrDefaultAsync(x => x.Id == roleId);

            if (user == null || role == null)
            {
                return BadRequest();
            }

            await _userManager.RemoveFromRoleAsync(user, role.Name);

            var userRemovedEvent = new UserRemovedFromRoleEvent(user.Email, role.Name);
            await _dispatcher.Dispatch(userRemovedEvent);

            return RedirectToPage("./Role", new { roleId });
        }
    }
}