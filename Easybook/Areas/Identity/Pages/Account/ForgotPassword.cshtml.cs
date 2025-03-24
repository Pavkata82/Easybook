// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Easybook.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel;

namespace Easybook.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            [DisplayName("Имейл")]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code = code },
                    protocol: Request.Scheme);


                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Смяна на паролата",
                    $@"
                    <html>
                        <body style=""font-family: Arial, sans-serif; color: #333; margin: 0; padding: 0;"">
                            <div style=""max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f4f4f4; border-radius: 8px;"">
                                <h2 style=""color: #4CAF50; text-align: center;"">Забравена парола - Easybook</h2>
                                <p style=""font-size: 16px; line-height: 1.5; text-align: center;"">Моля, променете паролата си, като кликнете върху линка по-долу:</p>
                    
                                <!-- Center the button and add padding and margin for spacing -->
                                <div style=""text-align: center; margin-top: 20px;"">
                                    <a href=""{HtmlEncoder.Default.Encode(callbackUrl)}"" style=""background-color: #4CAF50; color: #fff; padding: 12px 30px; text-decoration: none; border-radius: 4px; font-size: 16px; display: inline-block;"">Променете паролата си</a>
                                </div>
                    
                                <p style=""font-size: 16px; line-height: 1.5; text-align: center; margin-top: 20px;"">Ако не сте поискали смяна на паролата, можете да игнорирате този имейл.</p>
                    
                                <!-- Footer section with a smaller font size and spacing -->
                                <footer style=""margin-top: 40px; text-align: center; color: #777; font-size: 14px; line-height: 1.5;"">
                                    <p>С уважение, <br>Екипът на Easybook</p>
                                </footer>
                            </div>
                        </body>
                    </html>"
                );

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
